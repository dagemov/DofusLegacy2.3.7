using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Fighters
{
    public interface ISummoned
    {
        FightActor Summoner { get; set; }
        void Die(FightActor byFighter = null);
    }
}
