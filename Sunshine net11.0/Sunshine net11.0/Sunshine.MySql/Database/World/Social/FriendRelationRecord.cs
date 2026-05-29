using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Social
{
    [Table("character_friends")]
    public class FriendRelationRecord
    {
        [ExplicitKey]
        public int CharacterId { get; set; }
        [ExplicitKey]
        public int FriendCharacterId { get; set; }
        public string FriendName { get; set; }
        public string FriendAccountNickname { get; set; }
    }
}
