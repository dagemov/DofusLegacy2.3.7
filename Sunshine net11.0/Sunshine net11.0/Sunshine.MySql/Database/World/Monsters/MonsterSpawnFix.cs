using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Monsters
{
    [Table("worlds_monsters_fix")]
    public class MonsterSpawnFix
    {
        public int Map { get; set; }
        public string MonstersCSV { get; set; }
        public string CellsCSV { get; set; }
    }
}
