using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    [ReplyHandler(6)]
    public class UpdateObjectiveReply : Reply
    {
        public UpdateObjectiveReply()
        {
        }

        public override bool Execute()
        {
            string[] parameters = (Parameters[0] as string).Split(',');

            short quest = short.Parse(parameters[0]);
            short step = short.Parse(parameters[1]);
            short objective = short.Parse(parameters[2]);

            if (Client.Character.Quests.HasQuest(quest))
            {
                Client.Character.Quests.UpdateObjective(step, objective, true, parameters.Count() > 3);
                return true;
            }
            return false;
        }
    }
}
