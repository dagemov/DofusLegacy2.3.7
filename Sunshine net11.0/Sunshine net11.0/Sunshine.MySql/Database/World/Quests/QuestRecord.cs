using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Quests
{
    [Table("quests")]
    public class QuestRecord
    {
        public short Id { get; set; }
        public string Name { get; set; }
        public string StepIdsCSV { get; set; }
    }
}
