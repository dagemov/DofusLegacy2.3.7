using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Social
{
    [Table("character_enemies")]
    public class EnemyRelationRecord
    {
        [ExplicitKey]
        public int CharacterId { get; set; }
        [ExplicitKey]
        public int EnemyCharacterId { get; set; }
        public string EnemyName { get; set; }
    }
}
