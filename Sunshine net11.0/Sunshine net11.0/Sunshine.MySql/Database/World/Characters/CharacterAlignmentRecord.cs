using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper.Contrib.Extensions;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Characters
{
    [Table("characters_alignments")]
    public class CharacterAlignmentRecord
    {
        public int OwnerId { get; set; }
        public sbyte Side { get; set; }
        public sbyte Value { get; set; }
        public sbyte Grade { get; set; }
        public int Honor { get; set; }
        public int Dishonor { get; set; }
        public bool Enabled { get; set; }
    }
}
