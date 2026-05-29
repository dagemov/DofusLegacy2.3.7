using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Npcs
{
    [Table("npcs_messages")]
    public class NpcMessage
    {
        public int NpcId { get; set; }
        public short MessageId { get; set; }
        public string ParametersCSV { get; set; }
    }
}
