using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Quests
{
    [Table("quests_steps")]
    public class QuestStepRecord
    {
        public short Id { get; set; }
        public short Quest { get; set; }
        public string Name { get; set; }
        public string Dialog { get; set; }
        public int OptimalLevel { get; set; }
        public int ExperienceReward { get; set; }
        public int KamasReward { get; set; }
        public string ItemsRewardCSV { get; set; }
        public string EmotesRewardCSV { get; set; }
        public string JobsRewardCSV { get; set; }
        public string SpellsRewardCSV { get; set; }
        public string ObjectiveIdsCSV { get; set; }
    }
}
