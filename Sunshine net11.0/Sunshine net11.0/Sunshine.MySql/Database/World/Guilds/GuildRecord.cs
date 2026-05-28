using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Guilds
{
    [Table("guilds")]
    public class GuildRecord
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Name { get; set; }
        public long Experience { get; set; }
        public short Boost { get; set; }
        public short Prospecting { get; set; }
        public short Wisdom { get; set; }
        public short Pods { get; set; }
        public sbyte MaxTaxCollectors { get; set; }
        public string SpellsCSV { get; set; }
        public string SpellsLevelsCSV { get; set; }
        public int EmblemBackgroundShape { get; set; }
        public int EmblemBackgroundColor { get; set; }
        public int EmblemForegroundShape { get; set; }
        public int EmblemForegroundColor { get; set; }
    }
}
