using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Breeds
{
    [Table("breeds_spells")]
    public class BreedSpell
    {
        public int Spell { get; set; }
        public int ObtainLevel { get; set; }
        public int Breed { get; set; }
    }
}
