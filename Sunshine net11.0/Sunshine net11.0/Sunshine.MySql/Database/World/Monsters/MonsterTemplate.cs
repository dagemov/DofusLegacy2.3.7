using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Monsters
{
    [Table("monsters")]
    public class MonsterTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int GfxId { get; set; }
        public int Race { get; set; }
        public string EntityLook { get; set; }
        public bool UseSummonSlot { get; set; }
        public bool UseBombSlot { get; set; }
        public bool CanPlay { get; set; }
        public bool CanTackle { get; set; }
        public string AI { get; set; }
    }
}
