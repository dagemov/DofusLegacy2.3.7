using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Monsters
{
    [Table("worlds_monsters")]
    public class MonsterSpawn
    {
        public string MonstersCSV { get; set; }
        public int SubArea { get; set; }
    }
}
