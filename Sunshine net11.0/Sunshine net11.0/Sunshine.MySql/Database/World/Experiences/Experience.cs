using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Experiences
{
    [Table("experiences")]
    public class Experience
    {
        public int Level { get; set; }
        public long CharacterExp { get; set; }
        public long GuildExp { get; set; }
        public long MountExp { get; set; }
        public int AlignmentExp { get; set; }
        public long JobExp { get; set; }
    }
}
