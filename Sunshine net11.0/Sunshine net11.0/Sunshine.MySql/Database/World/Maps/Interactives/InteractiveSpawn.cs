using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Maps.Interactives
{
    [Table("worlds_interactives")]
    public class InteractiveSpawn
    {
        public int Id { get; set; }
        public int HouseId { get; set; }
        public int PaddockInstanceId { get; set; }
        public int Map { get; set; }
        public int Element { get; set; }
        public int Type { get; set; }
        public string SkillsCSV { get; set; }
        public string ParametersCSV { get; set; }
    }
}
