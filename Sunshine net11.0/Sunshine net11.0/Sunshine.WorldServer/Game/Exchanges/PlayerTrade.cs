using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Exchanges
{
    public class PlayerTrade : Trade
    {
        public Trader Trader { get; set; }

        public Trader Target { get; set; }

        public bool IsOpened { get; private set; }

        public PlayerTrade(Character trader, Character target)
        {
            Trader = new Trader(trader);
            Target = new Trader(target);
            Inventories = new Dictionary<RolePlayActor, List<Tuple<BasePlayerItem, int>>>();
        }

        public override ExchangeTypeEnum Type { get { return ExchangeTypeEnum.PLAYER_TRADE; } }

        public override void Open(List<object> parameters = null)
        {
            if (IsOpened)
                return;

            IsOpened = true;
            Trader.IsReady = false;
            Target.IsReady = false;
            Trader.Kamas = 0;
            Target.Kamas = 0;

            InventoryHandler.SendExchangeStartedWithPodsMessage(Trader.Client, Type, Trader, Target);
            InventoryHandler.SendExchangeStartedWithPodsMessage(Target.Client, Type, Trader, Target);
        }

        public override void Close(List<object> parameters = null)
        {
            IsOpened = false;
            Trader.IsReady = false;
            Target.IsReady = false;
            Trader.Kamas = 0;
            Target.Kamas = 0;
            Inventories?.Clear();

            Trader.Trade = null;
            Target.Trade = null;
            if (Trader.Client?.Character != null)
                Trader.Client.Character.Dialog = null;
            if (Target.Client?.Character != null)
                Target.Client.Character.Dialog = null;

            InventoryHandler.SendExchangeLeaveMessage(Trader.Client, true);
            InventoryHandler.SendExchangeLeaveMessage(Target.Client, true);
        }

        public override void Cancel(List<object> parameters = null)
        {
            IsOpened = false;
            Trader.IsReady = false;
            Target.IsReady = false;
            Trader.Kamas = 0;
            Target.Kamas = 0;
            Inventories?.Clear();

            Trader.Trade = null;
            Target.Trade = null;
            if (Trader.Client?.Character != null)
                Trader.Client.Character.Dialog = null;
            if (Target.Client?.Character != null)
                Target.Client.Character.Dialog = null;

            InventoryHandler.SendExchangeLeaveMessage(Trader.Client, false);
            InventoryHandler.SendExchangeLeaveMessage(Target.Client, false);
        }

        public override void SetReadyStatus(Character trader, bool isReady)
        {
            var currentTrader = GetTrader(trader);
            if (currentTrader == null)
                return;

            currentTrader.IsReady = isReady;
            InventoryHandler.SendExchangeIsReadyMessage(Trader.Client, currentTrader, isReady);
            InventoryHandler.SendExchangeIsReadyMessage(Target.Client, currentTrader, isReady);

            if (Trader.IsReady && Target.IsReady)
                Apply();
        }

        public override void SetKamas(Character trader, int quantity)
        {
            if (trader == null || trader.Inventory == null)
                return;

            if (quantity < 0)
                quantity = 0;
            if (quantity > trader.Inventory.Kamas)
                quantity = trader.Inventory.Kamas;

            ResetReadyState();

            var currentTrader = GetTrader(trader);
            if (currentTrader == null)
                return;

            currentTrader.Kamas = quantity;
            InventoryHandler.SendExchangeKamaModifiedMessage(Trader.Client, trader != Trader.Actor, quantity);
            InventoryHandler.SendExchangeKamaModifiedMessage(Target.Client, trader != Target.Actor, quantity);
        }

        public override void MoveItem(Character trader, int objectUid, int quantity)
        {
            var currentTrader = GetTrader(trader);
            if (currentTrader == null || trader?.Inventory == null || quantity == 0)
                return;

            var sourceItem = trader.Inventory.GetItemUid(objectUid);
            if (sourceItem == null)
                return;

            sourceItem.EnsureRuntimeEffects();
            if (!sourceItem.IsExchangeable())
            {
                InventoryHandler.SendExchangeErrorMessage(trader.Client, ExchangeErrorEnum.REQUEST_IMPOSSIBLE);
                trader.SendServerMessage("Cet objet ne peut pas être échangé.");
                return;
            }

            ResetReadyState();

            if (!Inventories.ContainsKey(trader))
                Inventories.Add(trader, new List<Tuple<BasePlayerItem, int>>());

            var traderInventory = Inventories[trader];
            var itemStock = traderInventory.FirstOrDefault(x => x.Item1 != null && x.Item1.Id == sourceItem.Id);
            int currentQuantity = itemStock != null ? itemStock.Item2 : 0;

            if (quantity < 0)
            {
                int removeQuantity = Math.Abs(quantity);
                if (itemStock == null)
                    return;

                int remainingQuantity = currentQuantity - removeQuantity;
                if (remainingQuantity > 0)
                {
                    var previewItem = sourceItem.Clone();
                    previewItem.Stack = remainingQuantity;
                    InventoryHandler.SendExchangeObjectModifiedMessage(Trader.Client, trader != Trader.Actor, previewItem);
                    InventoryHandler.SendExchangeObjectModifiedMessage(Target.Client, trader != Trader.Actor, previewItem);
                    traderInventory.Remove(itemStock);
                    traderInventory.Add(new Tuple<BasePlayerItem, int>(sourceItem, remainingQuantity));
                }
                else
                {
                    InventoryHandler.SendExchangeObjectRemovedMessage(Trader.Client, trader != Trader.Actor, sourceItem);
                    InventoryHandler.SendExchangeObjectRemovedMessage(Target.Client, trader != Trader.Actor, sourceItem);
                    traderInventory.Remove(itemStock);
                }

                return;
            }

            int maxAddable = sourceItem.Stack - currentQuantity;
            if (maxAddable <= 0)
                return;

            int addedQuantity = Math.Min(quantity, maxAddable);
            int finalQuantity = currentQuantity + addedQuantity;
            var addedPreviewItem = sourceItem.Clone();
            addedPreviewItem.Stack = finalQuantity;

            if (itemStock != null)
            {
                InventoryHandler.SendExchangeObjectModifiedMessage(Trader.Client, trader != Trader.Actor, addedPreviewItem);
                InventoryHandler.SendExchangeObjectModifiedMessage(Target.Client, trader != Trader.Actor, addedPreviewItem);
                traderInventory.Remove(itemStock);
                traderInventory.Add(new Tuple<BasePlayerItem, int>(sourceItem, finalQuantity));
            }
            else
            {
                InventoryHandler.SendExchangeObjectAddedMessage(Trader.Client, trader != Trader.Actor, addedPreviewItem);
                InventoryHandler.SendExchangeObjectAddedMessage(Target.Client, trader != Trader.Actor, addedPreviewItem);
                traderInventory.Add(new Tuple<BasePlayerItem, int>(sourceItem, finalQuantity));
            }
        }

        public override void Apply()
        {
            ApplyItems(Trader, Target);
            ApplyItems(Target, Trader);

            int traderKamas = Math.Min(Trader.Kamas, Trader.Inventory.Kamas);
            int targetKamas = Math.Min(Target.Kamas, Target.Inventory.Kamas);

            if (traderKamas > 0)
                Trader.Inventory.SetKamas(-traderKamas);
            if (targetKamas > 0)
                Trader.Inventory.SetKamas(targetKamas);
            if (targetKamas > 0)
                Target.Inventory.SetKamas(-targetKamas);
            if (traderKamas > 0)
                Target.Inventory.SetKamas(traderKamas);

            Close();
        }

        public Trader GetTrader(RolePlayActor owner)
        {
            if (owner == null)
                return null;

            return Trader.Actor.Id == owner.Id ? Trader : Target;
        }

        private void ResetReadyState()
        {
            if (!Trader.IsReady && !Target.IsReady)
                return;

            Trader.IsReady = false;
            Target.IsReady = false;
            InventoryHandler.SendExchangeIsReadyMessage(Trader.Client, Trader, false);
            InventoryHandler.SendExchangeIsReadyMessage(Trader.Client, Target, false);
            InventoryHandler.SendExchangeIsReadyMessage(Target.Client, Trader, false);
            InventoryHandler.SendExchangeIsReadyMessage(Target.Client, Target, false);
        }

        private void ApplyItems(Trader source, Trader destination)
        {
            if (source == null || destination == null || !Inventories.ContainsKey(source.Actor))
                return;

            foreach (var entry in Inventories[source.Actor].ToList())
            {
                var sourceItem = source.Inventory.GetItemUid(entry.Item1.Id);
                if (sourceItem == null)
                    continue;

                int moveQuantity = Math.Min(entry.Item2, sourceItem.Stack);
                if (moveQuantity <= 0)
                    continue;

                source.Inventory.RemoveItem(sourceItem, moveQuantity);
                var newItem = ItemManager.Instance.CreatePlayerItem(sourceItem, moveQuantity);
                if (newItem != null)
                    destination.Inventory.AddItem(newItem, newItem.Stack);
            }
        }
    }
}
