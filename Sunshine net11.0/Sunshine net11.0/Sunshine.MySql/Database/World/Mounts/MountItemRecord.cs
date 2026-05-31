using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Mounts
{
    [Table("mounts_items")]
    public class MountItemRecord
    {
        public int MountId { get; set; }
        [ExplicitKey]
        public int Id { get; set; }
        public int ItemId { get; set; }
        public uint Stack { get; set; }
        public byte[] SerializedEffects { get; set; }
    }
}
