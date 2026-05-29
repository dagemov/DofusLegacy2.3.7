using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.Npcs;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Exchanges
{
    public class NpcTrade : Trade
    {
        public Trader Trader { get; set; }

        public Trader Npc { get; set; }

        public enum ActionEnum
        {
            ADD,
            MODIFY,
            REMOVE
        }

        public NpcTrade(Character trader, Npc npc)
        {
            Trader = new Trader(trader);
            Npc = new Trader(npc);
            Inventories = new Dictionary<RolePlayActor, List<Tuple<BasePlayerItem, int>>>();
        }

        public override ExchangeTypeEnum Type { get { return ExchangeTypeEnum.NPC_TRADE; } }

        public override void Open(List<object> parameters = null)
        {
            InventoryHandler.SendExchangeStartOkNpcTradeMessage(Trader.Client, Npc.Actor as Npc);
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

        public override void SetReadyStatus(Character trader, bool isReady)
        {
            GetTrader(trader).IsReady = isReady;
            InventoryHandler.SendExchangeIsReadyMessage(Trader.Client, GetTrader(trader), isReady);

            if (Trader.IsReady)
                Apply();
        }

        public override void SetKamas(Character trader, int quantity)
        {
            if (quantity > trader.Inventory.Kamas)
                quantity = (int)trader.Inventory.Kamas;

            GetTrader(trader).Kamas = quantity;
            InventoryHandler.SendExchangeKamaModifiedMessage(Trader.Client, trader != Trader.Actor, quantity);
        }

        public override void MoveItem(Character trader, int objectUid, int quantity)
        {
            #region Move Item
            var item = trader.Inventory.GetItemUid(objectUid).Clone();
            if (item != null)
            {
                if (Inventories.ContainsKey(trader))
                {
                    var itemStock = Inventories[trader].FirstOrDefault(x => x.Item1.Id == item.Id);
                    if (itemStock != null)
                    {
                        if (quantity < 0)
                        {
                            quantity *= -1;
                            if (itemStock.Item2 > quantity) // Remove Stack
                            {
                                item.Stack = itemStock.Item2 - quantity;
                                InventoryHandler.SendExchangeObjectModifiedMessage(Trader.Client, trader != Trader.Actor, item);
                                Inventories[trader].Remove(itemStock);
                                Inventories[trader].Add(new Tuple<BasePlayerItem, int>(item, item.Stack));
                            }
                            else // Removed
                            {
                                InventoryHandler.SendExchangeObjectRemovedMessage(Trader.Client, trader != Trader.Actor, item);
                                Inventories[trader].Remove(itemStock);
                            }
                        }
                        else // Add Stack
                        {
                            if (item.Stack - itemStock.Item2 >= quantity)
                            {
                                item.Stack = itemStock.Item2 + quantity;
                                InventoryHandler.SendExchangeObjectModifiedMessage(Trader.Client, trader != Trader.Actor, item);
                                Inventories[trader].Remove(itemStock);
                                Inventories[trader].Add(new Tuple<BasePlayerItem, int>(item, item.Stack));
                            }
                        }
                    }
                    else // Add
                    {
                        if (quantity < item.Stack)
                            item.Stack = quantity;

                        InventoryHandler.SendExchangeObjectAddedMessage(Trader.Client, trader != Trader.Actor, item);
                        Inventories[trader].Add(new Tuple<BasePlayerItem, int>(item, item.Stack));
                    }
                }
                else // Add
                {
                    if (quantity < item.Stack)
                        item.Stack = quantity;

                    InventoryHandler.SendExchangeObjectAddedMessage(Trader.Client, trader != Trader.Actor, item);
                    Inventories.Add(trader, new List<Tuple<BasePlayerItem, int>>() { new Tuple<BasePlayerItem, int>(item, item.Stack) });
                }
            }
            #endregion
        }

        public void MoveItem(int objectGid, int quantity, ActionEnum action)
        {
            var item = ItemManager.Instance.CreatePlayerItem(objectGid, quantity);
            var itemStock = Inventories[Npc.Actor].FirstOrDefault(x => x.Item1.Template.Id == objectGid);

            if(itemStock != null)
                Inventories[Npc.Actor].Remove(itemStock);

            switch (action)
            {
                case ActionEnum.ADD:
                    InventoryHandler.SendExchangeObjectAddedMessage(Trader.Client, Npc.Actor != Trader.Actor, item);
                    Inventories.Add(Npc.Actor, new List<Tuple<BasePlayerItem, int>>() { new Tuple<BasePlayerItem, int>(item, item.Stack) });
                    break;

                case ActionEnum.MODIFY:
                    InventoryHandler.SendExchangeObjectModifiedMessage(Trader.Client, Npc.Actor != Trader.Actor, item);
                    Inventories[Npc.Actor].Add(new Tuple<BasePlayerItem, int>(item, item.Stack));
                    break;

                case ActionEnum.REMOVE:
                    InventoryHandler.SendExchangeObjectRemovedMessage(Trader.Client, Npc.Actor != Trader.Actor, item);
                    Inventories[Npc.Actor].Remove(itemStock);
                    break;
            }
        }

        public void SetKamas(int quantity)
        {           
            Npc.Kamas = quantity;
            InventoryHandler.SendExchangeKamaModifiedMessage(Trader.Client, Npc.Actor != Trader.Actor, quantity);
        }

        public override void Apply()
        {
            if (Inventories.ContainsKey(Trader.Actor))
            {
                foreach (var items in Inventories[Trader.Actor])
                {
                    Trader.Inventory.RemoveItem(items.Item1, items.Item2);
                    var item = ItemManager.Instance.CreatePlayerItem(items.Item1, items.Item2);
                }
            }

            if (Inventories.ContainsKey(Npc.Actor))
            {
                foreach (var items in Inventories[Npc.Actor])
                {
                    var item = ItemManager.Instance.CreatePlayerItem(items.Item1, items.Item2);
                    Trader.Inventory.AddItem(item, item.Stack);
                }
            }

            Trader.Inventory.SetKamas(-Trader.Kamas);
            Trader.Inventory.SetKamas(Npc.Kamas);
            Close();
        }

        public Trader GetTrader(Character owner)
        {
            return Trader.Actor.Id == owner.Id ? Trader : Npc;
        }
    }
}
