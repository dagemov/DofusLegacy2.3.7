using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Monsters
{
    [Table("monsters_spells")]
    public class MonsterSpell
    {
        public int Monster { get; set; }
        public string SpellsCSV { get; set; }
    }
}
