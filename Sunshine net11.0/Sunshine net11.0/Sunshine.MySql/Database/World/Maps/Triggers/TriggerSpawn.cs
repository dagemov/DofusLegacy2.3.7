using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Maps.Triggers
{
    [Table("worlds_triggers")]
    public class TriggerSpawn
    {
        public int Map { get; set; }
        public short Cell { get; set; }
        public byte Type { get; set; }
        public string ParametersCSV { get; set; }
    }
}
