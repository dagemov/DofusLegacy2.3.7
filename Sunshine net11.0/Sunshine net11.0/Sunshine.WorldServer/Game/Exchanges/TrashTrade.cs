using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Exchanges
{
    public class TrashTrade : Trade
    {
        public const int MaxWeight = 1000;
        public const long MaxKamas = 2000000000L;

        private static bool _tablesEnsured;
        private static readonly object _tableSync = new object();

        private readonly Character _trader;
        private readonly int _mapId;
        private readonly int _elementId;
        private readonly List<BasePlayerItem> _items = new List<BasePlayerItem>();
        private long _kamas;

        public TrashTrade(Character trader, int mapId, int elementId)
        {
            _trader = trader;
            _mapId = mapId;
            _elementId = elementId;
            EnsureTables();
            LoadState();
        }

        public override ExchangeTypeEnum Type => ExchangeTypeEnum.STORAGE;

        public override void Open(List<object> parameters = null)
        {
            RefreshClient();
        }

        public override void Close(List<object> parameters = null)
        {
            SaveState();
            _trader.Trade = null;
            InventoryHandler.SendExchangeLeaveMessage(_trader.Client, true);
        }

        public override void Cancel(List<object> parameters = null)
        {
            SaveState();
            _trader.Trade = null;
            InventoryHandler.SendExchangeLeaveMessage(_trader.Client, false);
        }

        public override void SetReadyStatus(Character trader, bool isReady) { }
        public override void Apply() { }

        public override void SetKamas(Character trader, int quantity)
        {
            if (trader == null || quantity == 0)
                return;

            if (quantity > 0)
            {
                if (trader.Inventory.Kamas < quantity)
                    return;

                long room = MaxKamas - _kamas;
                if (room <= 0)
                    return;

                if (quantity > room)
                    quantity = (int)room;

                trader.Inventory.SetKamas(-quantity);
                _kamas += quantity;
            }
            else
            {
                int withdraw = -quantity;
                if (_kamas < withdraw)
                    withdraw = (int)_kamas;

                long inventoryRoom = int.MaxValue - (long)trader.Inventory.Kamas;
                if (inventoryRoom <= 0)
                    return;

                if (withdraw > inventoryRoom)
                    withdraw = (int)inventoryRoom;

                if (withdraw <= 0)
                    return;

                trader.Inventory.SetKamas(withdraw);
                _kamas -= withdraw;
            }

            SaveState();
            RefreshClient();
        }

        public override void MoveItem(Character trader, int objectUid, int quantity)
        {
            if (trader == null || quantity == 0)
                return;

            if (quantity < 0)
            {
                MoveFromTrashToInventory(trader, objectUid, -quantity);
                return;
            }

            MoveFromInventoryToTrash(trader, objectUid, quantity);
        }

        private void MoveFromInventoryToTrash(Character trader, int objectUid, int quantity)
        {
            var inventoryItem = trader.Inventory.GetItemUid(objectUid);
            if (inventoryItem == null || inventoryItem.Stack < quantity)
                return;

            if (GetCurrentWeight() + (inventoryItem.Weight * quantity) > MaxWeight)
            {
                trader.SendServerMessage("La poubelle ne peut pas dépasser 1000 pods.");
                return;
            }

            var storedItem = inventoryItem.Clone();
            storedItem.Id = ItemManager.Instance.GenerateId();
            storedItem.Stack = quantity;
            storedItem.Position = CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED;
            storedItem.RawObjectEffects = CloneRawEffects(inventoryItem.RawObjectEffects);
            storedItem.Effects = CloneEffects(inventoryItem.Effects);

            AddOrStackStoredItem(storedItem, quantity);
            trader.Inventory.RemoveItem(inventoryItem, quantity);

            SaveState();
            RefreshClient();
        }

        private void MoveFromTrashToInventory(Character trader, int objectUid, int quantity)
        {
            var storedItem = GetStoredItemUid(objectUid);
            if (storedItem == null || storedItem.Stack < quantity)
                return;

            if (trader.Inventory.IsFull(storedItem, quantity))
                return;

            var inventoryItem = storedItem.Clone();
            inventoryItem.Id = ItemManager.Instance.GenerateId();
            inventoryItem.Stack = quantity;
            inventoryItem.Position = CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED;
            inventoryItem.RawObjectEffects = CloneRawEffects(storedItem.RawObjectEffects);
            inventoryItem.Effects = CloneEffects(storedItem.Effects);

            RemoveStoredItem(storedItem, quantity);
            trader.Inventory.AddItem(inventoryItem, quantity);

            SaveState();
            RefreshClient();
        }

        private void AddOrStackStoredItem(BasePlayerItem item, int quantity)
        {
            var same = FindStackableStoredItem(item);
            if (same != null)
            {
                same.Stack += quantity;
                return;
            }

            item.Stack = quantity;
            _items.Add(item);
        }

        private void RemoveStoredItem(BasePlayerItem item, int quantity)
        {
            var same = GetStoredItemUid(item.Id) ?? FindStackableStoredItem(item);
            if (same == null)
                return;

            if (same.Stack <= quantity)
            {
                _items.Remove(same);
                return;
            }

            same.Stack -= quantity;
        }

        private BasePlayerItem GetStoredItemUid(int objectUid)
        {
            return _items.FirstOrDefault(x => x.Id == objectUid);
        }

        private BasePlayerItem FindStackableStoredItem(BasePlayerItem compareItem)
        {
            return _items.FirstOrDefault(x =>
                x.Template.Id == compareItem.Template.Id &&
                x.Position == CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED &&
                HaveSameState(x, compareItem));
        }

        private bool HaveSameState(BasePlayerItem first, BasePlayerItem second)
        {
            if (first == null || second == null)
                return false;

            if (first.HasRawObjectEffects() || second.HasRawObjectEffects())
                return first.HasSameRawEffects(second);

            return BuildEffectsSignature(first.Effects) == BuildEffectsSignature(second.Effects);
        }

        private string BuildEffectsSignature(IEnumerable<Effect> effects)
        {
            if (effects == null)
                return string.Empty;

            return string.Join("|", effects.Select(x => $"{(int)x.Id}:{x.Value}:{x.DiceNum}:{x.DiceFace}:{x.Delay}:{x.Duration}"));
        }

        private List<Protocol.Types.ObjectEffect> CloneRawEffects(List<Protocol.Types.ObjectEffect> rawEffects)
        {
            if (rawEffects == null)
                return new List<Protocol.Types.ObjectEffect>();

            return ObjectEffectSerializer.Deserialize(ObjectEffectSerializer.Serialize(rawEffects));
        }

        private List<Effect> CloneEffects(List<Effect> effects)
        {
            if (effects == null)
                return new List<Effect>();

            return effects.Select(x => x.Clone()).ToList();
        }

        private int GetCurrentWeight()
        {
            return _items.Sum(x => x.Weight * x.Stack);
        }

        private void RefreshClient()
        {
            InventoryHandler.SendExchangeStartedWithStorageMessage(_trader.Client, ExchangeTypeEnum.STORAGE, MaxWeight);
            InventoryHandler.SendStorageInventoryContentMessage(_trader.Client, _items.Select(x => x.GetObjectItem()), (int)_kamas);
        }

        private static void EnsureTables()
        {
            if (_tablesEnsured)
                return;

            lock (_tableSync)
            {
                if (_tablesEnsured)
                    return;

                DatabaseManager.Connection.Execute(@"
CREATE TABLE IF NOT EXISTS `world_trashes` (
  `MapId` int(11) NOT NULL,
  `ElementId` int(11) NOT NULL,
  `Kamas` bigint(20) NOT NULL DEFAULT '0',
  PRIMARY KEY (`MapId`,`ElementId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                DatabaseManager.Connection.Execute(@"
CREATE TABLE IF NOT EXISTS `world_trashes_items` (
  `MapId` int(11) NOT NULL,
  `ElementId` int(11) NOT NULL,
  `ItemUid` int(11) NOT NULL,
  `TemplateId` int(11) NOT NULL,
  `Stack` int(11) NOT NULL DEFAULT '1',
  `Position` int(11) NOT NULL DEFAULT '63',
  `Effects` longtext,
  PRIMARY KEY (`MapId`,`ElementId`,`ItemUid`),
  KEY `idx_world_trashes_items_template` (`TemplateId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                _tablesEnsured = true;
            }
        }

        private void LoadState()
        {
            _items.Clear();

            _kamas = DatabaseManager.Connection.QueryFirstOrDefault<long>(
                "SELECT COALESCE(Kamas,0) FROM world_trashes WHERE MapId=@MapId AND ElementId=@ElementId",
                new { MapId = _mapId, ElementId = _elementId });

            var rows = DatabaseManager.Connection.Query(
                "SELECT ItemUid, TemplateId, Stack, Position, Effects FROM world_trashes_items WHERE MapId=@MapId AND ElementId=@ElementId ORDER BY TemplateId, ItemUid",
                new { MapId = _mapId, ElementId = _elementId });

            foreach (var row in rows)
            {
                BasePlayerItem item = ItemManager.Instance.CreateTypedPlayerItem((int)row.TemplateId);
                if (item == null)
                    continue;

                item.Id = (int)row.ItemUid;
                item.Stack = (int)row.Stack;
                item.Position = (CharacterInventoryPositionEnum)(int)row.Position;
                item.RawObjectEffects = ObjectEffectSerializer.Deserialize((string)row.Effects);
                item.Effects = new List<Effect>();
                _items.Add(item);
            }
        }

        private void SaveState()
        {
            DatabaseManager.Connection.Execute(
                "INSERT INTO world_trashes(MapId, ElementId, Kamas) VALUES(@MapId,@ElementId,@Kamas) ON DUPLICATE KEY UPDATE Kamas=@Kamas",
                new { MapId = _mapId, ElementId = _elementId, Kamas = _kamas });

            DatabaseManager.Connection.Execute(
                "DELETE FROM world_trashes_items WHERE MapId=@MapId AND ElementId=@ElementId",
                new { MapId = _mapId, ElementId = _elementId });

            foreach (var item in _items.Where(x => x != null && x.Stack > 0))
            {
                DatabaseManager.Connection.Execute(
                    "INSERT INTO world_trashes_items(MapId, ElementId, ItemUid, TemplateId, Stack, Position, Effects) VALUES(@MapId,@ElementId,@ItemUid,@TemplateId,@Stack,@Position,@Effects)",
                    new
                    {
                        MapId = _mapId,
                        ElementId = _elementId,
                        ItemUid = item.Id,
                        TemplateId = item.Template.Id,
                        Stack = item.Stack,
                        Position = (int)item.Position,
                        Effects = ObjectEffectSerializer.Serialize(item.RawObjectEffects)
                    });
            }
        }
    }
}
