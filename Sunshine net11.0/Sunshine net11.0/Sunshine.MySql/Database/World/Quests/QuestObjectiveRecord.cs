using Dapper.Contrib.Extensions;
using Sunshine.Protocol.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Quests
{
    [Table("quests_objectives")]
    public class QuestObjectiveRecord
    {
        public short Id { get; set; }
        public short Step { get; set; }
        public QuestTypeEnum Type { get; set; }
        public string Criteria { get; set; }
        public string ParametersCSV { get; set; }
    }
}
