using Sunshine.WorldServer.Handlers.Dialogs;
using Sunshine.Logs;
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
            Logger.WriteInfo($"[NpcAction] type=CloseDialog result=success charId={Client.Character.Id} npcId={Npc.Record.Id}");
            Client.Character.Dialog = null;
            DialogHandler.SendLeaveDialogMessage(Client);
            return true;
        }
    }
}
