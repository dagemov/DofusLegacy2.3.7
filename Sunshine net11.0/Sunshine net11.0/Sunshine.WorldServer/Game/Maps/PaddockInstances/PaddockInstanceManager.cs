using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Maps.PaddockInstances;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Maps.PaddockInstances
{
    public class PaddockInstanceManager : Singleton<PaddockInstanceManager>
    {
        public Dictionary<int, PaddockInstance> Instances { get; private set; } = new Dictionary<int, PaddockInstance>();

        public void Load()
        {
            PaddockInstanceTableBootstrap.EnsureTable();

            WorldMapPaddockInstanceRecord[] records;
            try
            {
                records = DatabaseManager.Connection.Query<WorldMapPaddockInstanceRecord>(@"
SELECT
    h.`Id`,
    h.`GuildId`,
    h.`MapId`,
    h.`EnterMapId`,
    h.`EnterCellId`,
    h.`Zone`,
    h.`InteractiveId`,
    h.`InterorMapsIdsCSV`,
    CASE
        WHEN h.`Map` IS NULL OR h.`Map` = 0 THEN h.`EnterMapId`
        ELSE h.`Map`
    END AS `InteractiveMapId`,
    h.`ElementId` AS `InteractiveElementId`,
    h.`Type` AS `InteractiveType`,
    h.`SkillsCSV` AS `InteractiveSkillsCSV`,
    h.`ParametersCSV` AS `InteractiveParametersCSV`
FROM `world_maps_paddock_instance` h").ToArray();
            }
            catch (Exception ex)
            {
                Logs.Logger.WriteError($"[ PADDOCK INSTANCES ] Impossible de charger world_maps_paddock_instance : {ex.Message}");
                Instances = new Dictionary<int, PaddockInstance>();
                return;
            }

            Instances = records
                .Where(IsValid)
                .ToDictionary(x => x.Id, x => new PaddockInstance(x));

            Logs.Logger.WriteInfo($"[ PADDOCK INSTANCES ] {Instances.Count} enclos instanciés chargés depuis world_maps_paddock_instance.");
        }

        private bool IsValid(WorldMapPaddockInstanceRecord record)
        {
            if (record == null)
                return false;

            if (record.Map == null)
            {
                Logs.Logger.WriteError($"[ PADDOCK INSTANCES ] Entrée {record.Id} ignorée : map extérieure {record.MapId} introuvable.");
                return false;
            }

            if (record.EnterMap == null)
            {
                Logs.Logger.WriteError($"[ PADDOCK INSTANCES ] Entrée {record.Id} ignorée : map intérieure EnterMapId={record.EnterMapId} introuvable.");
                return false;
            }

            if (record.EnterCellId <= 0)
            {
                Logs.Logger.WriteError($"[ PADDOCK INSTANCES ] Entrée {record.Id} ignorée : cellule d’entrée EnterCellId={record.EnterCellId} invalide.");
                return false;
            }

            if (record.InteractiveMap == null)
            {
                Logs.Logger.WriteError($"[ PADDOCK INSTANCES ] Entrée {record.Id} ignorée : map interactive {record.InteractiveMapId} introuvable.");
                return false;
            }

            if (record.InteractiveElementId <= 0)
            {
                Logs.Logger.WriteError($"[ PADDOCK INSTANCES ] Entrée {record.Id} ignorée : ElementId interactif absent.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(record.InteractiveSkillsCSV))
            {
                Logs.Logger.WriteError($"[ PADDOCK INSTANCES ] Entrée {record.Id} ignorée : SkillsCSV interactif absent.");
                return false;
            }

            return true;
        }

        public PaddockInstance GetInstance(int id)
        {
            PaddockInstance instance;
            return Instances.TryGetValue(id, out instance) ? instance : null;
        }

        public IEnumerable<PaddockInstance> GetInstancesByInteractiveMap(int mapId)
        {
            return Instances.Values.Where(x => x != null && x.InteractiveMapId == mapId);
        }

        public PaddockInstance GetInstanceByInteractive(int mapId, int elementId)
        {
            return Instances.Values.FirstOrDefault(x => x != null && x.MatchesInteractive(mapId, elementId));
        }

        public IEnumerable<PaddockInstance> GetInstancesByInteriorMap(int mapId)
        {
            return Instances.Values.Where(x => x != null && x.ContainsInteriorMap(mapId));
        }

        public bool CanUsePaddockInstanceInteractive(int instanceId, int mapId, int elementId)
        {
            var instance = GetInstance(instanceId);
            if (instance == null)
                return false;

            return instance.MatchesInteractive(mapId, elementId);
        }

        public bool CanUsePaddockInstanceInteractive(Character character, int instanceId, int mapId, int elementId)
        {
            var instance = GetInstance(instanceId);
            if (instance == null)
                return false;

            if (instance.IsExteriorInteractive(mapId, elementId))
                return true;

            if (!instance.IsInteriorInteractive(mapId, elementId))
                return false;

            return character?.LastTargetedPaddockInstance != null &&
                   character.LastTargetedPaddockInstance.Id == instance.Id;
        }

        public bool CanDisplayPaddockInstanceInteractive(Character character, int instanceId, int mapId, int elementId)
        {
            return CanUsePaddockInstanceInteractive(character, instanceId, mapId, elementId);
        }
    }
}
