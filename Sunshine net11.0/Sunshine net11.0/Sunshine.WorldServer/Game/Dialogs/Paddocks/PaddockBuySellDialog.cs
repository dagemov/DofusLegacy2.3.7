using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Maps.Paddocks;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using Sunshine.WorldServer.Handlers.Paddocks;
using System;

namespace Sunshine.WorldServer.Game.Dialogs.Paddocks
{
    public class PaddockBuySellDialog : IDialog
    {
        public Character Character { get; }
        public Paddock Paddock { get; }
        public bool IsSellDialog { get; }
        public int Price { get; private set; }

        public PaddockBuySellDialog(Character character, Paddock paddock, bool isSellDialog, int price)
        {
            Character = character;
            Paddock = paddock;
            IsSellDialog = isSellDialog;
            Price = Math.Max(0, price);
        }

        public void Open()
        {
            if (Character?.Client == null || Paddock == null)
                return;

            Character.Dialog = this;
            PaddockHandler.SendPaddockPropertiesMessage(Character.Client, Paddock);
            var ownerId = Paddock.OwnerId.HasValue && Paddock.OwnerId.Value > 0 ? Paddock.OwnerId.Value : 0;
            InventoryHandler.SendExchangeStartPaddockBuySell(Character.Client, IsSellDialog, ownerId, ResolvePrice());
        }

        public void Close()
        {
            if (Character?.Client == null)
                return;

            if (ReferenceEquals(Character.Dialog, this))
                Character.Dialog = null;

            Character.Client.Send(new LeaveDialogMessage());
        }

        private int ResolvePrice()
        {
            if (Price > 0)
                return Price;

            if (Paddock != null && Paddock.SalePrice > 0)
                return Paddock.SalePrice;

            return 0;
        }
    }
}
