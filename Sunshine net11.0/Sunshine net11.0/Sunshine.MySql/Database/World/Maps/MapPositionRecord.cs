using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Maps
{
    [Table("worlds_maps")]
    public class MapPositionRecord
    {
        public int Id { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public bool Outdoor { get; set; }
        public int SubArea { get; set; }
        public int Capabilities { get; set; }
        public int WorldMap { get; set; }
        public string Name { get; set; }
    }
}
