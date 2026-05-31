using Dapper.Contrib.Extensions;
using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.Maps;
using System;
using System.Linq;

namespace Sunshine.MySql.Database.World.Maps.Houses
{
    [Table("world_maps_house")]
    public class WorldMapHouseRecord
    {
        private Map _map;
        private Map _enterMap;
        private Map _endMapInstance;
        private uint[] _interiors;

        public int Id { get; set; }
        public int? GuildId { get; set; }
        public int MapId { get; set; }
        public int EnterMapId { get; set; }
        public int EnterCellId { get; set; }
        public int EndMapIdInstance { get; set; }
        public int EndCellIdIInstance { get; set; }
        public int InstanceMapCellId { get; set; }
        public int ModelId { get; set; }
        public int InteractiveId { get; set; }
        public int ElementId { get; set; }
        public string SkillsCSV { get; set; }

        [Write(false)]
        public int HouseInteractiveMap { get; set; }

        [Write(false)]
        public int HouseInteractiveElementId { get; set; }

        [Write(false)]
        public int HouseInteractiveType { get; set; }

        [Write(false)]
        public string HouseInteractiveSkillsCSV { get; set; }

        [Write(false)]
        public string HouseInteractiveParametersCSV { get; set; }

        public uint? GuildShareParams { get; set; }
        public string OwnerName { get; set; }
        public int? OwnerId { get; set; }
        public bool Abandonned { get; set; }
        public bool OnSale { get; set; }
        public bool SaleLocked { get; set; }
        public bool Locked { get; set; }
        public string Code { get; set; }
        public int? Price { get; set; }
        public int DefaultPrice { get; set; }
        public string InterorMapsIdsCSV { get; set; }
        public int ChestType { get; set; }
        public string ChestCode { get; set; }
        public bool HasChest { get; set; }
        public bool GuildChest { get; set; }
        public long ChestKamas { get; set; }

        [Write(false)]
        public Map Map => _map ?? (_map = MapManager.Instance.GetMap(MapId));

        [Write(false)]
        public Map EnterMap => _enterMap ?? (_enterMap = MapManager.Instance.GetMap(EnterMapId));

        [Write(false)]
        public Map EndMapInstance => _endMapInstance ?? (_endMapInstance = MapManager.Instance.GetMap(EndMapIdInstance > 0 ? EndMapIdInstance : MapId));

        [Write(false)]
        public uint[] Interiors
        {
            get
            {
                if (_interiors != null)
                    return _interiors;

                if (string.IsNullOrWhiteSpace(InterorMapsIdsCSV))
                    return _interiors = new uint[0];

                _interiors = InterorMapsIdsCSV.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => uint.TryParse(x, out _))
                    .Select(uint.Parse)
                    .ToArray();
                return _interiors;
            }
        }
    }
}
