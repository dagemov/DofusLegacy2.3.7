using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Mounts;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using Sunshine.WorldServer.Game.Maps.Interactives.Skills;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Exchanges
{
    public class MountStockTrade : Trade
    {
        public Trader Trader { get; private set; }
        public Mount Mount { get; private set; }

        public MountStockTrade(Character trader, Mount mount)
        {
            Trader = new Trader(trader);
            Mount = mount;
        }

        public override ExchangeTypeEnum Type => ExchangeTypeEnum.MOUNT;

        public override void Open(List<object> parameters = null)
        {
            if (Trader?.Client == null || Mount == null)
                return;

            Trader.Client.Character.LastViewedMountId = Mount.Id;
            Trader.Client.Character.LastTargetedMountId = Mount.Id;

            InventoryHandler.SendExchangeStartedWithStorageMessage(Trader.Client, ExchangeTypeEnum.MOUNT, Mount.MaxPods);
            Refresh();
        }

        public void Refresh()
        {
            if (Trader?.Client == null || Mount == null)
                return;

            Trader.Client.Send(new Protocol.Messages.ExchangeStartedMountStockMessage(MountManager.Instance.GetMountInventory(Mount.Id).ToArray()));
            InventoryHandler.SendInventoryContentMessage(Trader.Client);
            InventoryHandler.SendInventoryWeightMessage(Trader.Client);
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
        }

        public override void Apply()
        {
        }

        public override void SetKamas(Character trader, int quantity)
        {
        }

        public override void MoveItem(Character trader, int objectUid, int quantity)
        {
            if (trader == null || Mount == null || quantity == 0)
                return;

            if (quantity < 0)
            {
                var moveQuantity = -quantity;
                var mountItem = MountManager.Instance.GetMountInventoryPlayerItems(Mount.Id).FirstOrDefault(x => x.Id == objectUid);
                if (mountItem == null || mountItem.Stack < moveQuantity)
                    return;

                if (trader.Inventory.IsFull(mountItem, moveQuantity))
                    return;

                MountManager.Instance.RemoveMountInventoryItem(Mount.Id, mountItem, moveQuantity);
                trader.Inventory.AddItem(ItemManager.Instance.CreatePlayerItem(mountItem, moveQuantity), moveQuantity);
                Refresh();
                return;
            }

            var inventoryItem = trader.Inventory.GetItemUid(objectUid);
            if (inventoryItem == null || inventoryItem.Stack < quantity)
                return;

            if (!MountManager.Instance.CanStoreInMount(Mount.Id, inventoryItem, quantity))
            {
                trader.SendServerMessage("L'inventaire de la monture ne peut pas dépasser sa capacité en pods.");
                return;
            }

            MountManager.Instance.AddMountInventoryItem(Mount.Id, inventoryItem, quantity);
            trader.Inventory.RemoveItem(inventoryItem, quantity);
            Refresh();
        }
    }
}
