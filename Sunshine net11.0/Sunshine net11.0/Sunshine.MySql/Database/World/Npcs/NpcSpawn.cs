using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Npcs
{
    [Table("worlds_npcs")]
    public class NpcSpawn
    {
        public int Npc { get; set; }
        public int Map { get; set; }
        public short Cell { get; set; }
        public byte Direction { get; set; }
    }
}
