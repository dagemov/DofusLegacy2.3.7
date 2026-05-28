using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Game.Actors.Merchants;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System.Collections.Generic;

namespace Sunshine.WorldServer.Game.Exchanges
{
    public class MerchantCustomerTrade : Trade
    {
        private readonly Character _buyer;
        private readonly MerchantActor _merchant;
        public Trader Trader { get; }

        public MerchantCustomerTrade(Character buyer, MerchantActor merchant)
        {
            _buyer = buyer;
            _merchant = merchant;
            Trader = new Trader(buyer);
        }

        public override ExchangeTypeEnum Type => ExchangeTypeEnum.DISCONNECTED_VENDOR;

        public override void Open(List<object> parameters = null)
        {
            Trader.Trade = this;
            MerchantManager.Instance.RegisterViewer(_merchant.Record.CharacterId, _buyer);
            Trader.Client.Send(new ExchangeStartOkHumanVendorMessage(_merchant.Id, MerchantManager.Instance.GetShopNetworkItems(_merchant.Record.CharacterId)));
        }

        public override void Close(List<object> parameters = null)
        {
            MerchantManager.Instance.UnregisterViewer(_merchant.Record.CharacterId, _buyer);
            Trader.Trade = null;
            InventoryHandler.SendExchangeLeaveMessage(Trader.Client, true);
        }

        public override void Cancel(List<object> parameters = null)
        {
            MerchantManager.Instance.UnregisterViewer(_merchant.Record.CharacterId, _buyer);
            Trader.Trade = null;
            InventoryHandler.SendExchangeLeaveMessage(Trader.Client, false);
        }

        public bool BuyItem(int objectUid, int quantity)
        {
            if (!MerchantManager.Instance.BuyFromMerchant(_buyer, _merchant, objectUid, quantity, out var error))
            {
                if (!string.IsNullOrWhiteSpace(error))
                    _buyer.SendServerMessage(error);
                InventoryHandler.SendExchangeErrorMessage(_buyer.Client, ExchangeErrorEnum.BUY_ERROR);
                return false;
            }

            InventoryHandler.SendExchangeBuyOkMessage(_buyer.Client);

            if (!MerchantManager.Instance.HasStock(_merchant.Record.CharacterId))
            {
                Close();
                return true;
            }

            Open();
            return true;
        }

        public override void SetKamas(Character trader, int quantity)
        {
        }

        public override void MoveItem(Character trader, int objectUid, int quantity)
        {
        }

        public override void SetReadyStatus(Character trader, bool isReady)
        {
        }

        public override void Apply()
        {
        }
    }
}
