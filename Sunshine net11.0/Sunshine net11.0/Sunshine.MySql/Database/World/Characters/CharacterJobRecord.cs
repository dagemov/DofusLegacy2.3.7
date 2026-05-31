using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Characters
{
    [Table("characters_jobs")]
    public class CharacterJobRecord
    {
        public int OwnerId { get; set; }
        public sbyte Job { get; set; }
        public long Experience { get; set; }
    }
}
