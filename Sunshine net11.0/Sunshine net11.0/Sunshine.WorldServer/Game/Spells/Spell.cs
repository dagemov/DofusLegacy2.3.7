using Sunshine.MySql.Database.World.Spells;
using Sunshine.WorldServer.Game.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Spells
{
    public class Spell
    {
        public int Id { get; set; }

        public sbyte Level { get; set; }

        public short[] StatesForbidden { get; set; }

        public short[] StatesRequired { get; set; }

        public SpellTemplate Template { get; set; }

        public List<Effect> Effects { get; set; }

        public List<Effect> CriticalEffects { get; set; }

        public Spell(int id, sbyte level)
        {
           Id = id;
           Level = level;
        }
    }
}
