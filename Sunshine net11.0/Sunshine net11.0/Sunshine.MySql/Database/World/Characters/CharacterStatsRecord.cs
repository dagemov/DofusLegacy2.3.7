using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Characters
{
    [Table("characters_stats")]
    public class CharacterStatsRecord
    {
        public int OwnerId { get; set; }
        public int AP { get; set; }
        public int MP { get; set; }
        public int Health { get; set; }
        public int Vitality { get; set; }
        public int Strength { get; set; }
        public int Chance { get; set; }
        public int Wisdom { get; set; }
        public int Intelligence { get; set; }
        public int Agility { get; set; }
        public int DamageTaken { get; set; }
    }
}
