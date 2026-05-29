using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Characters.Quests
{
    [Table("characters_quests_objectives")]
    public class CharacterQuestObjectiveRecord
    {
        [ExplicitKey]
        public int OwnerId { get; set; }

        [ExplicitKey]
        public short Step { get; set; }

        [ExplicitKey]
        public short Objective { get; set; }

        public short Type { get; set; }
        public bool IsFinished { get; set; }
        public bool IsValided { get; set; }
    }
}
