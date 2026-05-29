using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Handlers.Context;
using Sunshine.WorldServer.Handlers.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    [ReplyHandler(2)]
    public class TeleportReply : Reply
    {
        public TeleportReply()
        {
        }

        public override bool Execute()
        {
            var parameters = (Parameters[0] as string).Split(',');
            int map = int.Parse(parameters[0]);
            short cell = short.Parse(parameters[1]);
            int direction = int.Parse(parameters[2]);

            if (MapManager.Instance.GetMap(map) != null)
            {
                Client.Character.Direction = direction;
                Client.Character.Teleport(map, cell);
                Client.Character.Dialog = null;
                DialogHandler.SendLeaveDialogMessage(Client);
                return true;
            }
            return false;
        }
    }
}
