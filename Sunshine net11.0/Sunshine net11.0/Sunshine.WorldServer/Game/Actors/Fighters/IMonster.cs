using Sunshine.WorldServer.Game.Actors.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Fighters
{
    public interface IMonster
    {
        Monster Monster { get; set; }
    }
}
