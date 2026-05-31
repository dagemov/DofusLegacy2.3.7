using Sunshine.WorldServer.Handlers.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    [ReplyHandler(0)]
    public class EndDialogReply : Reply
    {
        public EndDialogReply()
        {
        }

        public override bool Execute()
        {
            Client.Character.Dialog = null;
            DialogHandler.SendLeaveDialogMessage(Client);
            return true;
        }
    }
}
