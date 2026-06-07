using Dapper.Contrib.Extensions;
using System;

namespace Sunshine.MySql.Database.World.Characters
{
    [Table("characters_dopeul_cooldown")]
    public class CharacterDopeulCooldown
    {
        public int CharacterId { get; set; }
        public int MonsterId { get; set; }
        public DateTime LastFightTime { get; set; }
    }
}
