using Dapper.Contrib.Extensions;
using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.Maps;
using System;
using System.Linq;

namespace Sunshine.MySql.Database.World.Maps.PaddockInstances
{
    [Table("world_maps_paddock_instance")]
    public class WorldMapPaddockInstanceRecord
    {
        private Map _map;
        private Map _enterMap;
        private Map _interactiveMap;
        private uint[] _interiors;

        public int Id { get; set; }
        public int? GuildId { get; set; }
        public int MapId { get; set; }
        public int EnterMapId { get; set; }
        public int EnterCellId { get; set; }
        public string Zone { get; set; }
        public int InteractiveId { get; set; }
        public string InterorMapsIdsCSV { get; set; }

        [Write(false)]
        public int InteractiveMapId { get; set; }

        [Write(false)]
        public int InteractiveElementId { get; set; }

        [Write(false)]
        public int InteractiveType { get; set; }

        [Write(false)]
        public string InteractiveSkillsCSV { get; set; }

        [Write(false)]
        public string InteractiveParametersCSV { get; set; }

        [Write(false)]
        public Map Map => _map ?? (_map = MapManager.Instance.GetMap(MapId));

        [Write(false)]
        public Map EnterMap => _enterMap ?? (_enterMap = MapManager.Instance.GetMap(EnterMapId));

        [Write(false)]
        public Map InteractiveMap => _interactiveMap ?? (_interactiveMap = MapManager.Instance.GetMap(InteractiveMapId > 0 ? InteractiveMapId : EnterMapId));

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
