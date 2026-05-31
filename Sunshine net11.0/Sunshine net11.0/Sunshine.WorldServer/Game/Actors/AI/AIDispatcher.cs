using Sunshine.WorldServer.Game.Actors.AI.Types;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Actors.TaxCollectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.AI
{
    public static class AIDispatcher
    {
        public static void Dispatch(AIEnum ai, Monster source, FightActor fighter)
        {           
            if (AIManager.Instance.AITypes.ContainsKey(ai))
            {
                AITypeHandler AIHandler = AIManager.Instance.AITypes[ai]();
                AIHandler.Initialize(source, fighter);             
            }
            else
                Logs.Logger.WriteError($"Cannot dispatch the AI {ai} !");
        }

        public static void Dispatch(AIEnum ai, TaxCollector source, FightActor fighter)
        {
            if (AIManager.Instance.AITypes.ContainsKey(ai))
            {
                AITypeHandler AIHandler = AIManager.Instance.AITypes[ai]();
                AIHandler.Initialize(source, fighter);
            }
            else
                Logs.Logger.WriteError($"Cannot dispatch the AI {ai} !");
        }
    }
}
