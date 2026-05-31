using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    public static class ReplyDispatcher
    {
        public static bool Dispatch(WorldClient client, Npc npc, List<object> parameters)
        {
            if (client != null && npc != null)
            {
                int typeId = (int)parameters[0];
                if (NpcManager.Instance.Replies.ContainsKey(typeId))
                {
                    parameters.RemoveAt(0);
                    var function = NpcManager.Instance.Replies[typeId]();
                    return function.Initialize(client, npc, parameters);
                }
                else
                {
                    Logs.Logger.WriteError($"Cannot dispatch the npc type {typeId}");
                    return false;
                }
            }
            else
            {
                Logs.Logger.WriteError($"Cannot reply to npc {npc.Id}");
                return false;
            }
        }
    }
}
