using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Exchanges
{
    public class BankTrade : Trade
    {
        public const int MaxStorageWeight = 10000;
        public const long MaxBankKamas = 2000000000L;

        public Trader Trader { get; private set; }
        private readonly Character _character;

        public int AccountId => _character.Client.Account.Id;

        public BankTrade(Character trader)
        {
            _character = trader;
            Trader = new Trader(trader);
            EnsureAccountRow();
        }

        public override ExchangeTypeEnum Type => ExchangeTypeEnum.STORAGE;

        public override void Open(List<object> parameters = null)
        {
            InventoryHandler.SendExchangeStartedWithStorageMessage(
                Trader.Client,
                ExchangeTypeEnum.STORAGE,
                MaxStorageWeight);

            InventoryHandler.SendStorageInventoryContentMessage(
                Trader.Client,
                GetItems().Select(x => x.GetObjectItem()),
                (int)GetKamas());
        }

        public override void Close(List<object> parameters = null)
        {
            Trader.Trade = null;
            InventoryHandler.SendExchangeLeaveMessage(Trader.Client, true);
        }

        public override void Cancel(List<object> parameters = null)
        {
            Trader.Trade = null;
            InventoryHandler.SendExchangeLeaveMessage(Trader.Client, false);
        }

        public override void SetReadyStatus(Character trader, bool isReady) { }
        public override void Apply() { }

        public override void SetKamas(Character trader, int quantity)
        {
            if (trader == null || quantity == 0)
                return;

            long bankKamas = GetKamas();

            if (quantity > 0)
            {
                if (trader.Inventory.Kamas < quantity)
                    return;

                long room = MaxBankKamas - bankKamas;
                if (room <= 0)
                    return;

                if (quantity > room)
                    quantity = (int)room;

                trader.Inventory.SetKamas(-quantity);
                SetKamasInternal(bankKamas + quantity);
            }
            else
            {
                int withdraw = -quantity;

                if (bankKamas < withdraw)
                    withdraw = (int)bankKamas;

                if (withdraw <= 0)
                    return;

                trader.Inventory.SetKamas(withdraw);
                SetKamasInternal(bankKamas - withdraw);
            }

            InventoryHandler.SendStorageKamasUpdateMessage(trader.Client, (int)GetKamas());
        }

        public override void MoveItem(Character trader, int objectUid, int quantity)
        {
            if (trader == null || quantity == 0)
                return;

            // Banque -> inventaire
            if (quantity < 0)
            {
                int moveQuantity = -quantity;
                var item = GetItemUid(objectUid);

                if (item == null || item.Stack < moveQuantity || trader.Inventory.IsFull(item, moveQuantity))
                    return;

                RemoveItem(item, moveQuantity);
                trader.Inventory.AddItem(item.Clone(), moveQuantity);
                Open();
                return;
            }

            // Inventaire -> banque
            var invItem = trader.Inventory.GetItemUid(objectUid);
            if (invItem == null || invItem.Stack < quantity)
                return;

            if (!CanStore(invItem, quantity))
            {
                trader.SendServerMessage("La banque ne peut pas dépasser 10000 pods.");
                return;
            }

            AddItem(invItem, quantity);
            trader.Inventory.RemoveItem(invItem, quantity);
            Open();
        }

        private void EnsureAccountRow()
        {
            DatabaseManager.Connection.Execute(
                "INSERT IGNORE INTO accounts_bank(AccountId, Kamas) VALUES(@AccountId, 0)",
                new { AccountId });
        }

        private long GetKamas()
        {
            return DatabaseManager.Connection.ExecuteScalar<long?>(
                "SELECT Kamas FROM accounts_bank WHERE AccountId=@AccountId",
                new { AccountId }) ?? 0L;
        }

        private void SetKamasInternal(long kamas)
        {
            DatabaseManager.Connection.Execute(
                "UPDATE accounts_bank SET Kamas=@Kamas WHERE AccountId=@AccountId",
                new { AccountId, Kamas = kamas });
        }

        private IEnumerable<BasePlayerItem> GetItems()
        {
            const string sql = @"
SELECT ItemUid, TemplateId, Stack, Position, Effects
FROM account_bank_items
WHERE AccountId=@AccountId
ORDER BY TemplateId, ItemUid";

            var rows = DatabaseManager.Connection.Query(sql, new { AccountId });

            foreach (var row in rows)
            {
                int templateId = (int)row.TemplateId;

                if (!ItemManager.Instance.Items.ContainsKey(templateId))
                    continue;

                var item = ItemManager.Instance.CreateTypedPlayerItem(templateId);
                if (item == null)
                    continue;

                item.Id = (int)row.ItemUid;
                item.Stack = (int)row.Stack;
                item.Position = (CharacterInventoryPositionEnum)(int)row.Position;
                item.EffectSets = ItemManager.Instance.Items[templateId].EffectSets;
                item.Effects = new List<Effect>();
                item.RawObjectEffects = new List<ObjectEffect>();

                var serializedEffects = row.Effects == null ? string.Empty : (string)row.Effects;
                var rawEffects = ObjectEffectSerializer.Deserialize(serializedEffects);

                if (rawEffects != null && rawEffects.Count > 0)
                {
                    item.RawObjectEffects = rawEffects;
                    foreach (var raw in rawEffects.OfType<ObjectEffectInteger>())
                    {
                        item.Effects.Add(new Effect(
                            (EffectsEnum)raw.actionId,
                            0,
                            0,
                            raw.value,
                            0,
                            0,
                            0,
                            SpellShapeEnum.P,
                            0,
                            0));
                    }
                }
                else
                {
                    item.Effects = EffectManager.Instance.GetEffects(serializedEffects) ?? new List<Effect>();
                }

                yield return item;
            }
        }

        private BasePlayerItem GetItemUid(int uid)
        {
            return GetItems().FirstOrDefault(x => x.Id == uid);
        }

        private int GetWeight()
        {
            return GetItems().Sum(x => x.Template.Weight * x.Stack);
        }

        private bool CanStore(BasePlayerItem item, int quantity)
        {
            return GetWeight() + (item.Template.Weight * quantity) <= MaxStorageWeight;
        }

        private void AddItem(BasePlayerItem item, int quantity)
        {
            var same = GetItems().FirstOrDefault(x =>
                x.Template.Id == item.Template.Id &&
                x.Position == item.Position &&
                HaveSameEffects(x, item));

            if (same != null)
            {
                DatabaseManager.Connection.Execute(
                    "UPDATE account_bank_items SET Stack = Stack + @Quantity WHERE AccountId=@AccountId AND ItemUid=@ItemUid",
                    new { AccountId, ItemUid = same.Id, Quantity = quantity });
                return;
            }

            int uid = ItemManager.Instance.GenerateId();

            DatabaseManager.Connection.Execute(
                "INSERT INTO account_bank_items(AccountId, ItemUid, TemplateId, Stack, Position, Effects) VALUES(@AccountId, @ItemUid, @TemplateId, @Stack, @Position, @Effects)",
                new
                {
                    AccountId,
                    ItemUid = uid,
                    TemplateId = item.Template.Id,
                    Stack = quantity,
                    Position = (int)item.Position,
                    Effects = SerializeItemEffects(item)
                });
        }

        private void RemoveItem(BasePlayerItem item, int quantity)
        {
            var same = GetItemUid(item.Id) ?? GetItems().FirstOrDefault(x =>
                x.Template.Id == item.Template.Id &&
                HaveSameEffects(x, item));

            if (same == null)
                return;

            if (same.Stack <= quantity)
            {
                DatabaseManager.Connection.Execute(
                    "DELETE FROM account_bank_items WHERE AccountId=@AccountId AND ItemUid=@ItemUid",
                    new { AccountId, ItemUid = same.Id });
            }
            else
            {
                DatabaseManager.Connection.Execute(
                    "UPDATE account_bank_items SET Stack = Stack - @Quantity WHERE AccountId=@AccountId AND ItemUid=@ItemUid",
                    new { AccountId, ItemUid = same.Id, Quantity = quantity });
            }
        }

        private bool HaveSameEffects(BasePlayerItem first, BasePlayerItem second)
        {
            return SerializeItemEffects(first) == SerializeItemEffects(second);
        }

        private string SerializeItemEffects(BasePlayerItem item)
        {
            if (item != null && item.RawObjectEffects != null && item.RawObjectEffects.Count > 0)
                return ObjectEffectSerializer.Serialize(item.RawObjectEffects);

            return SerializeEffects(item?.Effects);
        }

        private string SerializeEffects(IEnumerable<Effect> effects)
        {
            if (effects == null)
                return string.Empty;

            using (var writer = new BigEndianWriter())
            {
                return EffectManager.Instance.SetEffects(writer, effects.ToList());
            }
        }
    }
}