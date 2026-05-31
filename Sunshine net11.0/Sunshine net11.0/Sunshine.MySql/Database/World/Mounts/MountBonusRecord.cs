using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Mounts
{
    [Table("mounts_bonus")]
    public class MountBonusRecord
    {
        [ExplicitKey]
        public int Id { get; set; }
        public int MountTemplateId { get; set; }
        public int EffectId { get; set; }
        public int Amount { get; set; }
    }
}
