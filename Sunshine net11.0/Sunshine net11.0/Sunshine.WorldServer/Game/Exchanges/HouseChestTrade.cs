using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Maps.Houses;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using Sunshine.WorldServer.Handlers.Houses;
using System.Collections.Generic;

namespace Sunshine.WorldServer.Game.Exchanges
{
    public class HouseChestTrade : Trade
    {
        public Trader Trader { get; private set; }
        public House House { get; private set; }

        public HouseChestTrade(Character trader, House house)
        {
            Trader = new Trader(trader);
            House = house;
        }

        public override ExchangeTypeEnum Type => ExchangeTypeEnum.STORAGE;

        public override void Open(List<object> parameters = null)
        {
            InventoryHandler.SendStorageInventoryContentMessage(Trader.Client, House);
        }

        public override void Close(List<object> parameters = null)
        {
            Trader.Trade = null;
            InventoryHandler.SendExchangeLeaveMessage(Trader.Client, true);
            HouseHandler.TrySendInsideHousePanel(Trader.Client);
        }

        public override void Cancel(List<object> parameters = null)
        {
            Trader.Trade = null;
            InventoryHandler.SendExchangeLeaveMessage(Trader.Client, false);
            HouseHandler.TrySendInsideHousePanel(Trader.Client);
        }

        public override void SetReadyStatus(Character trader, bool isReady)
        {
        }

        public override void Apply()
        {
        }

        public override void SetKamas(Character trader, int quantity)
        {
            if (trader == null || quantity == 0)
                return;

            if (quantity > 0)
            {
                if (trader.Inventory.Kamas < quantity)
                    return;

                var room = House.MaxChestKamas - House.ChestKamas;
                if (room <= 0)
                    return;

                if (quantity > room)
                    quantity = (int)room;

                trader.Inventory.SetKamas(-quantity);
                House.ChestKamas += quantity;
                House.IsDirty = true;
                HouseManager.Instance.Save();
                InventoryHandler.SendStorageKamasUpdateMessage(trader.Client, (int)House.ChestKamas);
                return;
            }

            int withdraw = -quantity;
            if (House.ChestKamas < withdraw)
                withdraw = (int)House.ChestKamas;

            long inventoryRoom = House.MaxChestKamas - trader.Inventory.Kamas;
            if (inventoryRoom <= 0)
                return;

            if (withdraw > inventoryRoom)
                withdraw = (int)inventoryRoom;

            if (withdraw <= 0)
                return;

            trader.Inventory.SetKamas(withdraw);
            House.ChestKamas -= withdraw;
            House.IsDirty = true;
            HouseManager.Instance.Save();
            InventoryHandler.SendStorageKamasUpdateMessage(trader.Client, (int)House.ChestKamas);
        }

        public override void MoveItem(Character trader, int objectUid, int quantity)
        {
            if (trader == null || quantity == 0)
                return;

            BasePlayerItem item;

            if (quantity < 0)
            {
                int moveQuantity = -quantity;
                item = House.GetChestItemUid(objectUid)?.Clone();
                if (item == null || item.Stack < moveQuantity)
                    return;

                if (trader.Inventory.IsFull(item, moveQuantity))
                    return;

                House.RemoveChestItem(item, moveQuantity);
                trader.Inventory.AddItem(item, moveQuantity);

                InventoryHandler.SendStorageInventoryContentMessage(trader.Client, House);
                return;
            }

            item = trader.Inventory.GetItemUid(objectUid)?.Clone();
            if (item == null || item.Stack < quantity)
                return;

            if (!House.CanStoreInChest(item, quantity))
            {
                trader.SendServerMessage("Le coffre ne peut pas dépasser 1000 pods.");
                return;
            }

            trader.Inventory.RemoveItem(item, quantity);
            House.AddChestItem(item, quantity);

            InventoryHandler.SendStorageInventoryContentMessage(trader.Client, House);
        }
    }
}
