using Sunshine.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.AI.Types
{
    public class AIManager : Singleton<AIManager>
    {
        public Dictionary<AIEnum, Func<AITypeHandler>> AITypes = new Dictionary<AIEnum, Func<AITypeHandler>>();
    }
}
