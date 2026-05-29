using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Characters.Items;
using Sunshine.MySql.Database.World.Maps.Merchants;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.Characters;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Actors.Merchants;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Spells;
using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.Managers
{
    public class MerchantManager : Singleton<MerchantManager>
    {
        private readonly Dictionary<int, MerchantActor> _activeMerchants = new Dictionary<int, MerchantActor>();
        private readonly Dictionary<int, HashSet<int>> _merchantViewers = new Dictionary<int, HashSet<int>>();

        public void Initialize()
        {
            EnsureTables();
            LoadActiveMerchants();
        }

        public void EnsureTables()
        {
            DatabaseManager.Connection.Execute(@"CREATE TABLE IF NOT EXISTS `world_maps_merchant` (
`CharacterId` int NOT NULL,
`AccountId` int NOT NULL,
`MapId` int NOT NULL,
`CellId` smallint NOT NULL,
`Direction` int NOT NULL,
`Name` varchar(64) NOT NULL,
`LookString` text NULL,
`IsActive` tinyint(1) NOT NULL DEFAULT 1,
`MerchantSince` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
PRIMARY KEY (`CharacterId`),
INDEX `idx_world_maps_merchant_map` (`MapId`),
INDEX `idx_world_maps_merchant_active` (`IsActive`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;");

            DatabaseManager.Connection.Execute(@"CREATE TABLE IF NOT EXISTS `characters_items_merchant` (
`ItemUid` int NOT NULL,
`OwnerId` int NOT NULL,
`TemplateId` int NOT NULL,
`Stack` int NOT NULL,
`Position` int NOT NULL DEFAULT 63,
`Price` int NOT NULL,
`Effects` mediumtext NULL,
PRIMARY KEY (`ItemUid`),
INDEX `idx_characters_items_merchant_owner` (`OwnerId`),
INDEX `idx_characters_items_merchant_template` (`TemplateId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;");
        }

        public IEnumerable<MerchantStockItemRecord> GetStock(int ownerId)
            => DatabaseManager.Connection.Query<MerchantStockItemRecord>(
                "SELECT * FROM characters_items_merchant WHERE OwnerId=@OwnerId ORDER BY TemplateId, ItemUid",
                new { OwnerId = ownerId });

        public MerchantActor GetMerchant(int ownerId)
        {
            _activeMerchants.TryGetValue(ownerId, out var merchant);
            return merchant;
        }

        public MerchantActor GetMerchant(Map map, int ownerId, short cellId)
        {
            if (!_activeMerchants.TryGetValue(ownerId, out var merchant))
                return null;

            if (merchant.Record.MapId != map.Id || merchant.Record.CellId != cellId)
                return null;

            return merchant;
        }

        public void RegisterViewer(int ownerId, Character viewer)
        {
            if (ownerId <= 0 || viewer == null || viewer.Client == null)
                return;

            if (!_merchantViewers.ContainsKey(ownerId))
                _merchantViewers[ownerId] = new HashSet<int>();

            _merchantViewers[ownerId].Add(viewer.Id);
        }

        public void UnregisterViewer(int ownerId, Character viewer)
        {
            if (ownerId <= 0 || viewer == null)
                return;

            if (!_merchantViewers.ContainsKey(ownerId))
                return;

            _merchantViewers[ownerId].Remove(viewer.Id);
            if (_merchantViewers[ownerId].Count == 0)
                _merchantViewers.Remove(ownerId);
        }

        public void RefreshViewers(int ownerId)
        {
            if (!_merchantViewers.ContainsKey(ownerId))
                return;

            int vendorId = ownerId;
            if (_activeMerchants.ContainsKey(ownerId) && _activeMerchants[ownerId] != null)
                vendorId = _activeMerchants[ownerId].Id;

            foreach (var viewerId in _merchantViewers[ownerId].ToList())
            {
                CharacterManager.Instance.Characters.TryGetValue(viewerId, out var viewer);
                if (viewer?.Client == null || !(viewer.Trade is WorldServer.Game.Exchanges.MerchantCustomerTrade))
                {
                    _merchantViewers[ownerId].Remove(viewerId);
                    continue;
                }

                viewer.Client.Send(new Protocol.Messages.ExchangeStartOkHumanVendorMessage(vendorId, GetShopNetworkItems(ownerId)));
            }

            if (_merchantViewers[ownerId].Count == 0)
                _merchantViewers.Remove(ownerId);
        }

        public bool HasStock(int ownerId)
            => DatabaseManager.Connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM characters_items_merchant WHERE OwnerId=@OwnerId",
                new { OwnerId = ownerId }) > 0;

        public int GetMerchantTax(int ownerId)
        {
            long total = DatabaseManager.Connection.ExecuteScalar<long?>(
                "SELECT COALESCE(SUM(Stack * Price), 0) FROM characters_items_merchant WHERE OwnerId=@OwnerId",
                new { OwnerId = ownerId }) ?? 0;

            var tax = (int)Math.Ceiling(total * 0.01d);
            return tax <= 0 && total > 0 ? 1 : tax;
        }

        public void OpenStock(Character character)
        {
            if (character == null || character.Client == null)
                return;

            if (character.IsInFight())
            {
                character.SendServerMessage("Impossible d'ouvrir le magasin en combat.");
                return;
            }

            if (character.IsInDialog())
            {
                character.SendServerMessage("Fermez d'abord le dialogue en cours avant d'ouvrir votre magasin.");
                return;
            }

            if (character.Map == null || character.Map.IsInstance())
            {
                character.SendServerMessage("Impossible d'ouvrir le magasin sur cette carte.");
                return;
            }

            if (character.IsInTrade())
                character.LeaveTrade();

            DeactivateForConnectedCharacter(character);
            character.SetTrade(ExchangeTypeEnum.SHOP_STOCK);
            character.Trade.Open();
        }

        public bool Activate(Character character, out string reason)
        {
            reason = null;

            if (character == null || character.Client == null)
            {
                reason = "Personnage introuvable.";
                return false;
            }

            if (character.IsInFight())
            {
                reason = "Impossible d'activer le mode marchand en combat.";
                return false;
            }

            if (character.Map == null || character.Map.IsInstance())
            {
                reason = "Impossible d'activer le mode marchand sur cette carte.";
                return false;
            }

            if (!HasStock(character.Id))
            {
                reason = "Aucun objet n'est en vente dans votre magasin.";
                return false;
            }

            if (!IsMerchantCellAvailable(character))
            {
                reason = "La cellule est occupée, déplacez-vous avant d'activer le mode marchand.";
                return false;
            }

            int tax = GetMerchantTax(character.Id);
            if (tax > 0 && character.Inventory.Kamas < tax)
            {
                reason = "Vous n'avez pas assez de kamas pour payer la taxe du mode marchand.";
                return false;
            }

            if (character.IsInTrade())
                character.LeaveTrade();

            if (tax > 0)
                character.Inventory.SetKamas(-tax);

            UpsertMerchantRecord(character, true);
            SpawnActiveMerchant(character.Id);
            character.Client.Disconnect();
            return true;
        }

        public void DeactivateForConnectedCharacter(Character character)
        {
            if (character == null)
                return;

            RemoveActiveMerchant(character.Id, false);
        }

        private bool IsMerchantCellAvailable(Character character)
        {
            if (character?.Map == null || character.Cell == null)
                return false;

            return !character.Map.RolePlayActors.Any(actor =>
            {
                if (actor == null || actor == character)
                    return false;

                if (actor is Character otherCharacter)
                    return otherCharacter.Cell != null && otherCharacter.Cell.Id == character.Cell.Id;

                if (actor is MerchantActor merchant)
                    return merchant.Record != null && merchant.Record.CellId == character.Cell.Id;

                return false;
            });
        }

private static bool CanMerchantizeItem(BasePlayerItem item)
{
    if (item == null)
        return false;

    item.EnsureRuntimeEffects();

    if (!item.IsExchangeable())
        return false;

    if (item.Position != CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED)
        return false;

    return true;
}

        public int StoreInventoryItem(Character character, int objectUid, int quantity, int price)
        {
            if (character == null || quantity <= 0 || price <= 0)
                return 0;

            var item = character.Inventory.GetItemUid(objectUid);
            if (!CanMerchantizeItem(item))
                return 0;

            if (item.HasRawObjectEffects())
                quantity = 1;

            quantity = Math.Min(quantity, item.Stack);
            if (quantity <= 0)
                return 0;

            var record = new MerchantStockItemRecord
            {
                ItemUid = ItemManager.Instance.GenerateId(),
                OwnerId = character.Id,
                TemplateId = item.Template.Id,
                Stack = quantity,
                Position = (int)CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED,
                Price = Math.Max(1, price),
                Effects = SerializeItemEffects(item)
            };

            DatabaseManager.Connection.Insert(record);
            character.Inventory.RemoveItem(item, quantity);
            RefreshViewers(character.Id);
            return record.ItemUid;
        }


public void ModifyStockItem(Character character, int objectUid, int quantity, int price)
{
    if (character == null)
        return;

    var record = DatabaseManager.Connection.QueryFirstOrDefault<MerchantStockItemRecord>(
        "SELECT * FROM characters_items_merchant WHERE OwnerId=@OwnerId AND ItemUid=@ItemUid",
        new { OwnerId = character.Id, ItemUid = objectUid });

    if (record == null)
        return;

    record.Price = Math.Max(1, price);

    if (quantity > record.Stack)
    {
        int missing = quantity - record.Stack;
        var candidate = character.Inventory.GetItems()
            .FirstOrDefault(x => x != null &&
                                 CanMerchantizeItem(x) &&
                                 x.Template.Id == record.TemplateId &&
                                 SerializeItemEffects(x) == (record.Effects ?? string.Empty));

        if (candidate != null)
        {
            if (candidate.HasRawObjectEffects())
                missing = Math.Min(missing, 1);
            else
                missing = Math.Min(missing, candidate.Stack);

            if (missing > 0)
            {
                DatabaseManager.Connection.Execute(
                    "UPDATE characters_items_merchant SET Stack = Stack + @Quantity, Price=@Price WHERE ItemUid=@ItemUid",
                    new { Quantity = missing, Price = record.Price, record.ItemUid });

                character.Inventory.RemoveItem(candidate, missing);
                RefreshViewers(character.Id);
                return;
            }
        }
    }
    else if (quantity > 0 && quantity < record.Stack)
    {
        int toReturn = record.Stack - quantity;
        TakeBackItem(character, objectUid, toReturn);
        record.Stack = quantity;
    }

    DatabaseManager.Connection.Execute(
        "UPDATE characters_items_merchant SET Stack=@Stack, Price=@Price WHERE ItemUid=@ItemUid",
        new { record.Stack, record.Price, record.ItemUid });
    RefreshViewers(character.Id);
}

public void TakeBackItem(Character character, int objectUid, int quantity)
        {
            var record = DatabaseManager.Connection.QueryFirstOrDefault<MerchantStockItemRecord>(
                "SELECT * FROM characters_items_merchant WHERE OwnerId=@OwnerId AND ItemUid=@ItemUid",
                new { OwnerId = character.Id, ItemUid = objectUid });

            if (record == null)
                return;

            quantity = Math.Min(quantity <= 0 ? record.Stack : quantity, record.Stack);
            var item = CreateItem(record);

            if (item == null || character.Inventory.IsFull(item, quantity))
                return;

            character.Inventory.AddItem(item, quantity);
            RemoveStockQuantity(record, quantity);
            CleanupMerchantIfEmpty(character.Id);
            RefreshViewers(character.Id);
        }

        public bool BuyFromMerchant(Character buyer, MerchantActor merchant, int objectUid, int quantity, out string error)
        {
            error = null;

            if (buyer == null || merchant == null || quantity <= 0)
            {
                error = "Achat impossible.";
                return false;
            }

            var record = DatabaseManager.Connection.QueryFirstOrDefault<MerchantStockItemRecord>(
                "SELECT * FROM characters_items_merchant WHERE OwnerId=@OwnerId AND ItemUid=@ItemUid",
                new { OwnerId = merchant.Record.CharacterId, ItemUid = objectUid });

            if (record == null)
            {
                error = "Objet introuvable dans le magasin.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(record.Effects))
                quantity = Math.Min(quantity, 1);

            quantity = Math.Min(quantity, record.Stack);
            if (quantity <= 0)
            {
                error = "Quantité invalide.";
                return false;
            }

            long totalPrice = (long)record.Price * quantity;
            if (buyer.Inventory.Kamas < totalPrice)
            {
                error = "Vous n'avez pas assez de kamas.";
                return false;
            }

            var item = CreateItem(record);
            if (item == null)
            {
                error = "Objet invalide.";
                return false;
            }

            if (buyer.Inventory.IsFull(item, quantity))
            {
                error = "Inventaire trop lourd.";
                return false;
            }

            buyer.Inventory.SetKamas(-(int)totalPrice);
            CreditMerchantBank(merchant.Record.AccountId, totalPrice);
            buyer.Inventory.AddItem(item, quantity);
            RemoveStockQuantity(record, quantity);
            CleanupMerchantIfEmpty(merchant.Record.CharacterId);
            RefreshViewers(merchant.Record.CharacterId);
            return true;
        }

        public IEnumerable<ObjectItemToSell> GetStockNetworkItems(int ownerId)
        {
            foreach (var record in GetStock(ownerId))
            {
                var item = CreateItem(record);
                if (item == null)
                    continue;

                item.Price = record.Price;
                yield return new ObjectItemToSell((short)item.Template.Id, 0, false, GetNetworkEffects(item), record.ItemUid, record.Stack, record.Price);
            }
        }

        public ObjectItemToSell GetStockNetworkItem(int ownerId, int objectUid)
        {
            var record = DatabaseManager.Connection.QueryFirstOrDefault<MerchantStockItemRecord>(
                "SELECT * FROM characters_items_merchant WHERE OwnerId=@OwnerId AND ItemUid=@ItemUid",
                new { OwnerId = ownerId, ItemUid = objectUid });

            if (record == null)
                return null;

            var item = CreateItem(record);
            if (item == null)
                return null;

            item.Price = record.Price;
            return new ObjectItemToSell((short)item.Template.Id, 0, false, GetNetworkEffects(item), record.ItemUid, record.Stack, record.Price);
        }

        public IEnumerable<ObjectItemToSellInHumanVendorShop> GetShopNetworkItems(int ownerId)
        {
            foreach (var record in GetStock(ownerId))
            {
                var item = CreateItem(record);
                if (item == null)
                    continue;

                item.Price = record.Price;
                yield return new ObjectItemToSellInHumanVendorShop((short)item.Template.Id, 0, false, GetNetworkEffects(item), record.ItemUid, record.Stack, record.Price, record.Price);
            }
        }

        private void LoadActiveMerchants()
        {
            _activeMerchants.Clear();
            var records = DatabaseManager.Connection.Query<WorldMapMerchantRecord>("SELECT * FROM world_maps_merchant WHERE IsActive = 1").ToList();
            foreach (var record in records)
                SpawnActiveMerchant(record.CharacterId);
        }

        private void SpawnActiveMerchant(int ownerId)
        {
            var record = DatabaseManager.Connection.QueryFirstOrDefault<WorldMapMerchantRecord>(
                "SELECT * FROM world_maps_merchant WHERE CharacterId=@CharacterId AND IsActive=1",
                new { CharacterId = ownerId });

            if (record == null || !HasStock(ownerId))
                return;

            var map = MapManager.Instance.GetMap(record.MapId);
            if (map == null)
                return;

            RemoveActiveMerchant(ownerId, false);

            var look = CreateLook(record.LookString);
            var actor = new MerchantActor(record, look);
            _activeMerchants[ownerId] = actor;
            map.EnterActor(actor);
        }

        private void RemoveActiveMerchant(int ownerId, bool deleteRecord)
        {
            if (_activeMerchants.TryGetValue(ownerId, out var actor))
            {
                try
                {
                    var map = MapManager.Instance.GetMap(actor.Record.MapId);
                    map?.LeaveActor(actor);
                }
                catch
                {
                }

                _activeMerchants.Remove(ownerId);
            }

            if (deleteRecord)
            {
                DatabaseManager.Connection.Execute("DELETE FROM world_maps_merchant WHERE CharacterId=@CharacterId", new { CharacterId = ownerId });
            }
            else
            {
                DatabaseManager.Connection.Execute("UPDATE world_maps_merchant SET IsActive = 0 WHERE CharacterId=@CharacterId", new { CharacterId = ownerId });
            }
        }

        private void CleanupMerchantIfEmpty(int ownerId)
        {
            if (!HasStock(ownerId))
                RemoveActiveMerchant(ownerId, true);
        }

        private void RemoveStockQuantity(MerchantStockItemRecord record, int quantity)
        {
            if (record.Stack - quantity <= 0)
            {
                DatabaseManager.Connection.Execute("DELETE FROM characters_items_merchant WHERE ItemUid=@ItemUid", new { record.ItemUid });
                return;
            }

            DatabaseManager.Connection.Execute(
                "UPDATE characters_items_merchant SET Stack = Stack - @Quantity WHERE ItemUid=@ItemUid",
                new { Quantity = quantity, record.ItemUid });
        }

        private void UpsertMerchantRecord(Character character, bool isActive)
        {
            var lookString = !string.IsNullOrWhiteSpace(character.Record.CustomLook)
                ? character.Record.CustomLook
                : character.Record.EntityLook;

            DatabaseManager.Connection.Execute(@"
INSERT INTO world_maps_merchant(CharacterId, AccountId, MapId, CellId, Direction, Name, LookString, IsActive, MerchantSince)
VALUES(@CharacterId, @AccountId, @MapId, @CellId, @Direction, @Name, @LookString, @IsActive, NOW())
ON DUPLICATE KEY UPDATE
AccountId = VALUES(AccountId),
MapId = VALUES(MapId),
CellId = VALUES(CellId),
Direction = VALUES(Direction),
Name = VALUES(Name),
LookString = VALUES(LookString),
IsActive = VALUES(IsActive),
MerchantSince = NOW()",
                new
                {
                    CharacterId = character.Id,
                    AccountId = character.Client.Account.Id,
                    MapId = character.Map.Id,
                    CellId = character.Cell.Id,
                    Direction = character.Direction,
                    Name = character.Name,
                    LookString = lookString,
                    IsActive = isActive
                });
        }

        private void CreditMerchantBank(int accountId, long kamas)
        {
            DatabaseManager.Connection.Execute("INSERT IGNORE INTO accounts_bank(AccountId, Kamas) VALUES(@AccountId, 0)", new { AccountId = accountId });
            DatabaseManager.Connection.Execute("UPDATE accounts_bank SET Kamas = Kamas + @Kamas WHERE AccountId=@AccountId", new { AccountId = accountId, Kamas = kamas });
        }

        private ActorLook CreateLook(string lookString)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(lookString))
                    return WorldServer.Game.Actors.EntityManager.Instance.GetActorLook(lookString).Clone();
            }
            catch
            {
            }

            return new ActorLook { BonesID = 1 };
        }

        private BasePlayerItem CreateItem(MerchantStockItemRecord record)
        {
            if (!ItemManager.Instance.Items.ContainsKey(record.TemplateId))
                return null;

            var item = ItemManager.Instance.CreateTypedPlayerItem(record.TemplateId);
            if (item == null)
                return null;

            item.Id = ItemManager.Instance.GenerateId();
            item.Stack = record.Stack;
            item.Position = CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED;
            item.EffectSets = ItemManager.Instance.Items[record.TemplateId].EffectSets;
            item.Effects = new List<Effect>();
            item.RawObjectEffects = new List<ObjectEffect>();

            var rawEffects = ObjectEffectSerializer.Deserialize(record.Effects ?? string.Empty);
            if (rawEffects != null && rawEffects.Count > 0)
            {
                item.RawObjectEffects = rawEffects;
                foreach (var raw in rawEffects.OfType<ObjectEffectInteger>())
                {
                    item.Effects.Add(new Effect((EffectsEnum)raw.actionId, 0, 0, raw.value, 0, 0, 0, SpellShapeEnum.P, 0, 0));
                }
            }
            else
            {
                item.Effects = EffectManager.Instance.GetEffects(record.Effects ?? string.Empty) ?? new List<Effect>();
            }

            return ItemManager.Instance.FinalizePlayerItem(item);
        }

        private string SerializeItemEffects(BasePlayerItem item)
        {
            var rawEffects = item.RawObjectEffects;
            if (rawEffects == null || rawEffects.Count == 0)
                rawEffects = (item.Effects ?? new List<Effect>()).Select(x => (ObjectEffect)x.GetObjectEffectInteger()).ToList();

            return ObjectEffectSerializer.Serialize(rawEffects);
        }

        private IEnumerable<ObjectEffect> GetNetworkEffects(BasePlayerItem item)
        {
            if (item.RawObjectEffects != null && item.RawObjectEffects.Count > 0)
                return item.RawObjectEffects;

            return (item.Effects ?? new List<Effect>()).Select(x => (ObjectEffect)x.GetObjectEffectInteger());
        }
    }
}
