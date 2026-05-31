using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System.Collections.Generic;

namespace Sunshine.WorldServer.Game.Exchanges
{
    public class MerchantStockTrade : Trade
    {
        private readonly Character _character;
        public Trader Trader { get; }

        public MerchantStockTrade(Character character)
        {
            _character = character;
            Trader = new Trader(character);
        }

        public override ExchangeTypeEnum Type => ExchangeTypeEnum.SHOP_STOCK;

        public override void Open(List<object> parameters = null)
        {
            Trader.Trade = this;
            Trader.Client.Send(new ExchangeStartedMessage((sbyte)ExchangeTypeEnum.SHOP_STOCK));
            Trader.Client.Send(new ExchangeShopStockStartedMessage(MerchantManager.Instance.GetStockNetworkItems(_character.Id)));
            MerchantManager.Instance.RefreshViewers(_character.Id);
        }

        private void RefreshOwnerInventory()
        {
            InventoryHandler.SendInventoryContentMessage(Trader.Client);
            InventoryHandler.SendInventoryWeightMessage(Trader.Client);
        }

        private void RefreshStockItem(int objectUid)
        {
            var updated = MerchantManager.Instance.GetStockNetworkItem(_character.Id, objectUid);
            if (updated != null)
                Trader.Client.Send(new ExchangeShopStockMovementUpdatedMessage(updated));
            else
                Trader.Client.Send(new ExchangeShopStockMovementRemovedMessage(objectUid));
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

        public override void SetKamas(Character trader, int quantity)
        {
        }

        public override void MoveItem(Character trader, int objectUid, int quantity)
        {
            if (quantity < 0)
            {
                MerchantManager.Instance.TakeBackItem(_character, objectUid, -quantity);
                RefreshOwnerInventory();
                RefreshStockItem(objectUid);
                return;
            }

            RefreshOwnerInventory();
        }

        public void StorePricedItem(int objectUid, int quantity, int price)
        {
            int storedUid = MerchantManager.Instance.StoreInventoryItem(_character, objectUid, quantity, price);
            RefreshOwnerInventory();

            if (storedUid > 0)
                RefreshStockItem(storedUid);
        }

        public void ModifyPricedItem(int objectUid, int quantity, int price)
        {
            MerchantManager.Instance.ModifyStockItem(_character, objectUid, quantity, price);
            RefreshOwnerInventory();
            RefreshStockItem(objectUid);
        }

        public override void SetReadyStatus(Character trader, bool isReady)
        {
        }

        public override void Apply()
        {
        }
    }
}
