using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Maps.Merchants
{
    [Table("world_maps_merchant")]
    public class WorldMapMerchantRecord
    {
        [ExplicitKey]
        public int CharacterId { get; set; }
        public int AccountId { get; set; }
        public int MapId { get; set; }
        public short CellId { get; set; }
        public int Direction { get; set; }
        public string Name { get; set; }
        public string LookString { get; set; }
        public bool IsActive { get; set; }
        public System.DateTime MerchantSince { get; set; }
    }
}
