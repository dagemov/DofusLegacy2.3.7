using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.Maps;
using System;
using System.Linq;

namespace Sunshine.WorldServer.Game.Maps.Houses.Storage
{
    public class HouseStorageEntry
    {
        private Map _map;
        private Map _enterMap;
        private uint[] _interiors;

        public int Id { get; set; }
        public int MapId { get; set; }
        public int EnterMapId { get; set; }
        public int EnterCellId { get; set; }
        public int EndMapIdInstance { get; set; }
        public int EndCellIdIInstance { get; set; }
        public int InstanceMapCellId { get; set; }
        public int ModelId { get; set; }
        public int InteractiveId { get; set; }
        public string InteriorMapsCSV { get; set; }
        public string SkillListIdsCSV { get; set; }
        public int DefaultPrice { get; set; }
        public bool HasChest { get; set; }

        public int? OwnerId { get; set; }
        public string OwnerName { get; set; }
        public bool OnSale { get; set; }
        public bool SaleLocked { get; set; }
        public bool Locked { get; set; }
        public string Code { get; set; }
        public int? Price { get; set; }
        public string ChestCode { get; set; }

        public Map Map => _map ?? (_map = MapManager.Instance.GetMap(MapId));
        public Map EnterMap => _enterMap ?? (_enterMap = MapManager.Instance.GetMap(EnterMapId));

        public uint[] Interiors
        {
            get
            {
                if (_interiors != null)
                    return _interiors;

                if (string.IsNullOrWhiteSpace(InteriorMapsCSV))
                    return _interiors = new uint[0];

                _interiors = InteriorMapsCSV.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => uint.TryParse(x, out _))
                    .Select(uint.Parse)
                    .ToArray();
                return _interiors;
            }
        }
    }
}
