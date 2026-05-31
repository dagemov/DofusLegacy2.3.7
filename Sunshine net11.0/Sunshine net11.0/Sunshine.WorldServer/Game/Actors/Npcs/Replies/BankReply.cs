using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Handlers.Dialogs;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    [ReplyHandler(10)]
    public class BankReply : Reply
    {
        public override bool Execute()
        {
            Client.Character.Dialog = null;
            DialogHandler.SendLeaveDialogMessage(Client);

            if (Client.Character.IsInTrade())
                Client.Character.LeaveTrade();

            Client.Character.SetTrade(ExchangeTypeEnum.STORAGE, null, Client.Character);
            Client.Character.Trade.Open();
            return true;
        }
    }
}
