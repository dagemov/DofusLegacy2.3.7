using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ReplyHandler : Attribute
    {
        public byte Reply { get; set; }

        public ReplyHandler(byte reply)
        {
            Reply = reply;
        }
    }
}
