using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Recipes
{
    [Table("jobs_harvest")]
    public class HarvestTemplate
    {
        public int Item { get; set; }
        public sbyte Level { get; set; }
        public long Experience { get; set; }
        public string Loot { get; set; }
        public double Time { get; set; }
    }
}
