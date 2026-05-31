using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System;
using Sunshine.Protocol.Messages;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;
using Sunshine.MySql.Database.World.Recipes;
using Sunshine.MySql.Database.Managers;

namespace Sunshine.WorldServer.Game.Exchanges
{
    public class CraftTrade : Trade
    {
        public Trader Trader { get; set; }

        public Trader Target { get; set; }

        private int _skill { get; set; }

        private sbyte _job { get; set; }

        private sbyte _nbCase { get; set; }

        private bool _stop { get; set; }

        private Task _task { get; set; }

        public CraftTrade(Character trader, Character target = null)
        {
            Trader = new Trader(trader);
            Target = target != null ? new Trader(target) : null;
            Inventories = new Dictionary<RolePlayActor, List<Tuple<BasePlayerItem, int>>>();
            Replay = 1;
        }

        public override ExchangeTypeEnum Type { get { return ExchangeTypeEnum.CRAFT; } }

        public override void Open(List<object> parameters = null)
        {
            _job = (sbyte)parameters[0];
            InventoryHandler.SendExchangeStartOkCraftWithInformationMessage(Trader.Client, _nbCase = (sbyte)parameters[1], _skill = (int)parameters[2]);
        }

        public override void Close(List<object> parameters = null)
        {
            Trader.Trade = null;
            Trader.Client.Character.Dialog = null;
            InventoryHandler.SendExchangeLeaveMessage(Trader.Client, true);
        }

        public override void Cancel(List<object> parameters = null)
        {
            Trader.Trade = null;
            Trader.Client.Character.Dialog = null;
            InventoryHandler.SendExchangeLeaveMessage(Trader.Client, false);
        }

        public override void SetReadyStatus(Character trader, bool isReady)
        {
            GetTrader(trader).IsReady = isReady;
            if (Trader.IsReady)
                Apply();
        }

        public override void SetKamas(Character trader, int quantity)
        {
        }

        public override void MoveItem(Character trader, int objectUid, int quantity)
        {
            #region Move Item

            if (Inventories.ContainsKey(trader) && Inventories[trader].Count >= _nbCase)
                return;

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

                        var sameTemplateItem = Inventories[trader].FirstOrDefault(x => x.Item1.Template.Id == item.Template.Id);
                        if (sameTemplateItem != null)
                        {
                            item.Stack = sameTemplateItem.Item2 + quantity;
                            InventoryHandler.SendExchangeObjectModifiedMessage(Trader.Client, trader != Trader.Actor, item);
                            Inventories[trader].Remove(sameTemplateItem);
                            Inventories[trader].Add(new Tuple<BasePlayerItem, int>(item, item.Stack));
                        }
                        else
                        {
                            InventoryHandler.SendExchangeObjectAddedMessage(Trader.Client, trader != Trader.Actor, item);
                            Inventories[trader].Add(new Tuple<BasePlayerItem, int>(item, item.Stack));
                        }
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

        public override void Apply()
        {
            if (Inventories.ContainsKey(Trader.Actor))
            {
                var ingredients = Inventories[Trader.Actor].Select(x => x.Item1.Template.Id);
                var quantities = Inventories[Trader.Actor].Select(x => x.Item2);
                var recipe = JobManager.Instance.GetRecipe(ingredients, _skill);
                if (recipe != null)
                {
                    _task = Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < Replay;)
                        {
                            if (_stop)
                            {
                                InventoryHandler.SendExchangeItemAutoCraftStopedMessage(Trader.Client, ExchangeReplayStopReasonEnum.STOPPED_REASON_USER);
                                _stop = false;
                                return;
                            }
                            for (int y = 0; y < Inventories[Trader.Actor].Count; y++)
                            {
                                var item = Inventories[Trader.Actor][y];
                                var baseQuantity = recipe.Item3[y];
                                var itemRemoved = Trader.Inventory.GetItem(item.Item1.Template.Id);
                                if (itemRemoved != null)
                                    Trader.Inventory.RemoveItem(itemRemoved, baseQuantity);
                                else
                                {
                                    InventoryHandler.SendExchangeItemAutoCraftStopedMessage(Trader.Client, ExchangeReplayStopReasonEnum.STOPPED_REASON_MISSING_RESSOURCE);
                                    this.Stop();
                                    Clear(item);
                                    Replay = 0;
                                    return;
                                }
                            }
                            BasePlayerItem newItem = ItemManager.Instance.CreatePlayerItem(recipe.Item1.Result, 1);
                            Trader.Inventory.AddItem(newItem, 1);
                            Replay--;
                            InventoryHandler.SendExchangeReplayCountModifiedMessage(Trader.Client, Replay);
                            InventoryHandler.SendExchangeCraftResultWithObjectDescMessage(Trader.Client, newItem, CraftResultEnum.CRAFT_SUCCESS);
                            (Trader.Actor as Character).Jobs.AddExperience(_job, JobManager.Instance.GetExperience(ingredients.Count()));
                        }
                        InventoryHandler.SendExchangeItemAutoCraftStopedMessage(Trader.Client, ExchangeReplayStopReasonEnum.STOPPED_REASON_OK);
                        Clear();
                    });
                }
                else
                    InventoryHandler.SendExchangeItemAutoCraftStopedMessage(Trader.Client, ExchangeReplayStopReasonEnum.STOPPED_REASON_IMPOSSIBLE_CRAFT);
            }
        }

        public void Stop()
        {
            _stop = true;
            _task.Dispose();
        }

        public void UpdateReplay(int count)
        {            
            Replay = count;
            InventoryHandler.SendExchangeReplayCountModifiedMessage(Trader.Client, count);
        }

        public void Clear(Tuple<BasePlayerItem, int> item)
        {
            InventoryHandler.SendExchangeObjectRemovedMessage(Trader.Client, false, item.Item1);
            Inventories[Trader.Actor].Remove(item);
        }

        public void Clear()
        {
            foreach(var item in Inventories[Trader.Actor])
                InventoryHandler.SendExchangeObjectRemovedMessage(Trader.Client, false, item.Item1);
            Inventories[Trader.Actor].Clear();
        }

        public Trader GetTrader(Character owner)
        {
            return Trader.Actor.Id == owner.Id ? Trader : Target;
        }
    }
}
