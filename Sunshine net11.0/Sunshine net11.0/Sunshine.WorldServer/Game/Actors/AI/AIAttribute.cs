using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.AI
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AIHandler : Attribute
    {

        public AIEnum AI;
        public AIHandler(AIEnum ai)
        {
            AI = ai;
        }
    }
}
