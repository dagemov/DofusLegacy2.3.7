using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    [ReplyHandler(8)]
    public class LearnJobReply : Reply
    {
        public LearnJobReply()
        {
        }

        public override bool Execute()
        {
            sbyte job = sbyte.Parse(Parameters[0] as string);

            if (Client.Character.Jobs.HasJob(job))
                return false;

            Client.Character.Jobs.AddJob(job);
            return true;
        }
    }
}
