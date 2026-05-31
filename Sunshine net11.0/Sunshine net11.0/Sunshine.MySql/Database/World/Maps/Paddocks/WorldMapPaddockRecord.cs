using Dapper.Contrib.Extensions;
using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.Maps;

namespace Sunshine.MySql.Database.World.Maps.Paddocks
{
    [Table("world_maps_paddock")]
    public class WorldMapPaddockRecord
    {
        private Map _map;

        public int Id { get; set; }
        public int? GuildId { get; set; }
        public int MapId { get; set; }
        public uint MaxOutdoorMount { get; set; }
        public uint MaxItems { get; set; }
        public bool Abandonned { get; set; }
        public bool OnSale { get; set; }
        public bool Locked { get; set; }
        public int Price { get; set; }
        public bool? isPublic { get; set; }
        public int? TpCell { get; set; }
        public int? OwnerId { get; set; }
        public int? CellIdSpawnMount { get; set; }

        [Write(false)]
        public Map Map => _map ?? (_map = MapManager.Instance.GetMap(MapId));
    }
}
