using Sunshine.MySql.Database.World.Maps.PaddockInstances;
using Sunshine.WorldServer.Game.Maps;
using System.Linq;

namespace Sunshine.WorldServer.Game.Maps.PaddockInstances
{
    public class PaddockInstance
    {
        public WorldMapPaddockInstanceRecord Record { get; private set; }

        public PaddockInstance(WorldMapPaddockInstanceRecord record)
        {
            Record = record;
        }

        public int Id => Record.Id;
        public int MapId => Record.MapId;
        public int EnterMapId => Record.EnterMapId;
        public int EnterCellId => Record.EnterCellId;
        public int InteractiveId => Record.InteractiveId;
        public string Zone => Record.Zone ?? string.Empty;
        public int InteractiveMapId => Record.InteractiveMapId > 0 ? Record.InteractiveMapId : EnterMapId;
        public int InteractiveElementId => Record.InteractiveElementId;
        public int InteractiveType => Record.InteractiveType;
        public string InteractiveSkillsCSV => Record.InteractiveSkillsCSV ?? string.Empty;
        public string InteractiveParametersCSV => Record.InteractiveParametersCSV ?? string.Empty;
        public Map Map => Record.Map;
        public Map EnterMap => Record.EnterMap;
        public Map InteractiveMap => Record.InteractiveMap;

        public bool ContainsInteriorMap(int mapId)
        {
            return Record != null &&
                   (EnterMapId == mapId ||
                    Record.Interiors != null && Record.Interiors.Contains((uint)mapId));
        }

        public bool IsExteriorInteractive(int mapId, int elementId)
        {
            if (MapId != mapId)
                return false;

            if (InteractiveId <= 0)
                return true;

            return InteractiveId == elementId;
        }

        public bool IsInteriorInteractive(int mapId, int elementId)
        {
            if (InteractiveMapId != mapId)
                return false;

            if (InteractiveElementId <= 0)
                return true;

            return InteractiveElementId == elementId;
        }

        public bool MatchesInteractive(int mapId, int elementId)
        {
            return IsExteriorInteractive(mapId, elementId) || IsInteriorInteractive(mapId, elementId);
        }
    }
}
