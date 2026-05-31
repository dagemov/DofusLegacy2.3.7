using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.TaxCollectors;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using Sunshine.WorldServer.Handlers.Guilds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Exchanges
{
    public class TaxCollectorTrade : Trade
    {
        public Trader Trader { get; set; }

        public Trader TaxCollector { get; set; }

        public enum ActionEnum
        {
            ADD,
            MODIFY,
            REMOVE
        }

        public TaxCollectorTrade(Character trader, TaxCollector taxCollector)
        {
            Trader = new Trader(trader);
            TaxCollector = new Trader(taxCollector);
        }

        public override ExchangeTypeEnum Type { get { return ExchangeTypeEnum.TAXCOLLECTOR; } }

        public override void Open(List<object> parameters = null)
        {
            InventoryHandler.SendStorageInventoryContentMessage(Trader.Client, TaxCollector.Actor as TaxCollector);
            (Trader.Actor as Character).SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 139, new object[] { 5 });
        }

        public override void Close(List<object> parameters = null)
        {
            Trader.Trade = null;
            InventoryHandler.SendExchangeLeaveMessage(Trader.Client, true);
            for (int i = 0; i < (Trader.Actor as Character).Guild.Members.Count; i++)
            {
                if ((Trader.Actor as Character).Guild.Members[i].IsConnected())
                    TaxCollectorHandler.SendTaxCollectorMovementMessage((Trader.Actor as Character).Guild.Members[i].Client, TaxCollector.Actor as TaxCollector, false, (Trader.Actor as Character).Name);
            }
            (Trader.Actor as Character).Guild.AddEarnedExperience((long)(TaxCollector.Actor as TaxCollector).GatheredExperience);
            TaxCollectorManager.Instance.DeleteTaxCollector(TaxCollector.Actor as TaxCollector, Trader.Actor as Character);
        }

        public override void Cancel(List<object> parameters = null)
        {
            Trader.Trade = null;
            InventoryHandler.SendExchangeLeaveMessage(Trader.Client, false);
        }

        public override void SetReadyStatus(Character trader, bool isReady)
        {
        }

        public override void SetKamas(Character trader, int quantity)
        {
            if (quantity > 0 && Trader.Inventory.Kamas >= quantity) // Trader to perco
            {
                Trader.Inventory.SetKamas(quantity * -1);
                (TaxCollector.Actor as TaxCollector).Inventory.GatheredKamas += quantity;
                InventoryHandler.SendStorageKamasUpdateMessage(Trader.Client, quantity);
            }
            else
            {
                if (quantity < 0 && (TaxCollector.Actor as TaxCollector).Inventory.GatheredKamas >= (quantity * -1))
                {
                    Trader.Inventory.SetKamas(quantity * -1);
                    (TaxCollector.Actor as TaxCollector).Inventory.GatheredKamas += quantity;
                    InventoryHandler.SendStorageInventoryContentMessage(Trader.Client, TaxCollector.Actor as TaxCollector);
                }
            }
            
        }

        public override void MoveItem(Character trader, int objectUid, int quantity)
        {
            BasePlayerItem item = null;
            if (quantity < 0) // Perco to Trader
            {
                item = (TaxCollector.Actor as TaxCollector).Inventory.GetItemUid(objectUid).Clone();
                if (item != null && item.Stack >= (quantity * -1))
                {
                    InventoryHandler.SendStorageObjectRemoveMessage(trader.Client, item);
                    (TaxCollector.Actor as TaxCollector).Inventory.RemoveItem(item, quantity * -1);
                    trader.Inventory.AddItem(item, quantity * -1);
                }
            }
            else
            {
                if (quantity > 0)
                {
                    item = Trader.Inventory.GetItemUid(objectUid).Clone();
                    if (item != null && item.Stack >= quantity)
                    {
                        trader.Inventory.RemoveItem(item, quantity);
                        (TaxCollector.Actor as TaxCollector).Inventory.AddItem(item, quantity);
                        InventoryHandler.SendStorageObjectsUpdateMessage(trader.Client, (TaxCollector.Actor as TaxCollector).Inventory.GetItems());
                    }
                }
            }
        }

        public void SetKamas(int quantity)
        {
            
        }

        public override void Apply()
        {           
        }

        public Trader GetTrader(Character owner)
        {
            return Trader.Actor.Id == owner.Id ? Trader : TaxCollector;
        }
    }
}
