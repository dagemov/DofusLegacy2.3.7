using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Handlers.Dialogs;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    [ReplyHandler(4)]
    public class CinematicReply : Reply
    {
        public CinematicReply()
        {
        }

        public override bool Execute()
        {
            var parameters = (Parameters[0] as string).Split(',');
            short cinematic = short.Parse(parameters[0]);
            Client.Send(new CinematicMessage(cinematic));
            Client.Character.Dialog = null;
            DialogHandler.SendLeaveDialogMessage(Client);
            return true;
        }
    }
}
