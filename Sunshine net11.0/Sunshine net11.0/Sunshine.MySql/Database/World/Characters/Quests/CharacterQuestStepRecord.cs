using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Characters.Quests
{
    [Table("characters_quests_steps")]
    public class CharacterQuestStepRecord
    {
        [ExplicitKey]
        public int OwnerId { get; set; }

        [ExplicitKey]
        public short Quest { get; set; }

        [ExplicitKey]
        public short Step { get; set; }

        public bool IsFinished { get; set; }
        public bool isValided { get; set; }
    }
}
