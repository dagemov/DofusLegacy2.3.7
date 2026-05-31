using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Npcs
{
    [Table("npcs_replies")]
    public class NpcReplie
    {
        public int Npc { get; set; }
        public int Map { get; set; }
        public int Type { get; set; }
        public short MessageId { get; set; }
        public short ReplieId { get; set; }
        public string ParametersCSV { get; set; }
        public string DialogParamsCSV { get; set; } 
    }
}
