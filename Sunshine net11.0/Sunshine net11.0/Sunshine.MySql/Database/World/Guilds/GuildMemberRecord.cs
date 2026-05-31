using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Guilds
{
    [Table("guilds_members")]
    public class GuildMemberRecord
    {
        public int Owner { get; set; }
        public int Account { get; set; }
        public int Guild { get; set; }
        public int Rank { get; set; }
        public uint Rights { get; set; }
        public long GivenExperience { get; set; }
        public sbyte GivenPercent { get; set; }
    }
}