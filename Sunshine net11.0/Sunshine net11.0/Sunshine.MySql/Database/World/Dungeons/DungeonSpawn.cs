using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Dungeons
{
    [Table("dungeons")]
    public class DungeonSpawn
    {
        public int Map { get; set; }
        public string MonstersCSV { get; set; }
        public string Parameters { get; set; }
    }
}
