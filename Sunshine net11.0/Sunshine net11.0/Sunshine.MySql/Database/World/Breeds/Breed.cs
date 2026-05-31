using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Breeds
{
    [Table("breeds")]
    public class Breed
    {
        public int Id { get; set; }
        public string MaleLook { get; set; }
        public string FemaleLook { get; set; }
        public int StartMap { get; set; }
        public int StartCell { get; set; }
        public int StartDirection { get; set; }
    }
}
