using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Maps.Interactives;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Maps.Interactives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.MySql.Database.Managers
{
    public class InteractiveSkillBinding
    {
        public InteractiveSkillBinding(int mapId, int skillId, int elementId = 0, int houseId = 0, int paddockInstanceId = 0)
        {
            MapId = mapId;
            SkillId = skillId;
            ElementId = elementId;
            HouseId = houseId;
            PaddockInstanceId = paddockInstanceId;
        }

        public int MapId { get; private set; }
        public int SkillId { get; private set; }
        public int ElementId { get; private set; }
        public int HouseId { get; private set; }
        public int PaddockInstanceId { get; private set; }
    }

    public class InteractiveManager : Singleton<InteractiveManager>
    {
        private UniqueIdProvider _idProvider = new UniqueIdProvider();

        public Dictionary<int, InteractiveSkillBinding> Interactives = new Dictionary<int, InteractiveSkillBinding>();

        public Dictionary<int, List<InteractiveSkill>> GetAllInteractiveSkills()
        {
            Dictionary<int, List<InteractiveSkill>> interactives = new Dictionary<int, List<InteractiveSkill>>();
            var skills = DatabaseManager.Connection.Query<InteractiveSkill>("SELECT * FROM interactives_skills");
            foreach (var skill in skills)
            {
                if (interactives.ContainsKey(skill.ParentJob))
                    interactives[skill.ParentJob].Add(skill);
                else
                    interactives.Add(skill.ParentJob, new List<InteractiveSkill> { skill });
            }
            return interactives;
        }

        public Dictionary<int, List<Interactive>> GetAllInteractiveSpawns()
        {
            Dictionary<int, List<Interactive>> interactives = new Dictionary<int, List<Interactive>>();

            // Interactives classiques, dont les portes extérieures de maison.
            var spawns = DatabaseManager.Connection.Query<InteractiveSpawn>(
                "SELECT `Id`, 0 AS `HouseId`, 0 AS `PaddockInstanceId`, `Map`, `Element`, `Type`, `SkillsCSV`, `ParametersCSV` FROM `worlds_interactives`");

            foreach (var spawn in spawns)
                AddInteractiveSpawn(interactives, spawn);

            // Interactives internes de maison seulement : world_maps_house.ElementId + world_maps_house.SkillsCSV.
            foreach (var spawn in GetHouseInteractiveSpawns())
                AddInteractiveSpawn(interactives, spawn);

            // Interactives d'enclos instanciés : système séparé, sans colonnes maison/vente/coffre.
            foreach (var spawn in GetPaddockInstanceInteractiveSpawns())
                AddInteractiveSpawn(interactives, spawn);

            return interactives;
        }

        private void AddInteractiveSpawn(Dictionary<int, List<Interactive>> interactives, InteractiveSpawn spawn)
        {
            if (spawn == null || spawn.Map <= 0 || spawn.Element <= 0)
                return;

            List<Interactive> list;
            if (!interactives.TryGetValue(spawn.Map, out list))
            {
                list = new List<Interactive>();
                interactives.Add(spawn.Map, list);
            }

            // Ne jamais supprimer/remplacer une interactive venant de worlds_interactives.
            // Les conflits sont évités par HouseId + ElementId côté maison.
            list.Add(new Interactive(spawn));
        }

        private IEnumerable<InteractiveSpawn> GetHouseInteractiveSpawns()
        {
            try
            {
                if (!ColumnExists("world_maps_house", "Map") ||
                    !ColumnExists("world_maps_house", "ElementId") ||
                    !ColumnExists("world_maps_house", "SkillsCSV") ||
                    !ColumnExists("world_maps_house", "ParametersCSV"))
                {
                    return Enumerable.Empty<InteractiveSpawn>();
                }

                const string sql = @"
SELECT
    h.`Id` AS `HouseId`,
    0 AS `PaddockInstanceId`,
    CASE
        WHEN h.`Map` IS NULL OR h.`Map` = 0 THEN h.`EnterMapId`
        ELSE h.`Map`
    END AS `Map`,
    h.`ElementId` AS `Element`,
    -1 AS `Type`,
    h.`SkillsCSV` AS `SkillsCSV`,
    h.`ParametersCSV` AS `ParametersCSV`
FROM `world_maps_house` h
WHERE h.`Id` > 0
  AND h.`ElementId` > 0
  AND h.`SkillsCSV` IS NOT NULL
  AND h.`SkillsCSV` <> '';";

                return DatabaseManager.Connection.Query<InteractiveSpawn>(sql)
                    .Where(x => x != null && x.HouseId > 0 && x.Map > 0 && x.Element > 0)
                    .ToArray();
            }
            catch (Exception ex)
            {
                Logs.Logger.WriteError($"[INTERACTIVES] Impossible de charger les interactives internes de maisons depuis world_maps_house : {ex.Message}");
                return Enumerable.Empty<InteractiveSpawn>();
            }
        }

        private IEnumerable<InteractiveSpawn> GetPaddockInstanceInteractiveSpawns()
        {
            try
            {
                PaddockInstanceTableBootstrap.EnsureTable();

                if (!ColumnExists("world_maps_paddock_instance", "EnterMapId") ||
                    !ColumnExists("world_maps_paddock_instance", "EnterCellId") ||
                    !ColumnExists("world_maps_paddock_instance", "Map") ||
                    !ColumnExists("world_maps_paddock_instance", "ElementId") ||
                    !ColumnExists("world_maps_paddock_instance", "SkillsCSV") ||
                    !ColumnExists("world_maps_paddock_instance", "ParametersCSV"))
                {
                    return Enumerable.Empty<InteractiveSpawn>();
                }

                const string sql = @"
SELECT
    0 AS `HouseId`,
    h.`Id` AS `PaddockInstanceId`,
    h.`MapId` AS `Map`,
    h.`InteractiveId` AS `Element`,
    -1 AS `Type`,
    '184' AS `SkillsCSV`,
    CONCAT(h.`EnterMapId`, ',', h.`EnterCellId`, ',3') AS `ParametersCSV`
FROM `world_maps_paddock_instance` h
WHERE h.`Id` > 0
  AND h.`MapId` > 0
  AND h.`EnterMapId` > 0
  AND h.`EnterCellId` > 0
  AND h.`InteractiveId` > 0

UNION ALL

SELECT
    0 AS `HouseId`,
    h.`Id` AS `PaddockInstanceId`,
    CASE
        WHEN h.`Map` IS NULL OR h.`Map` = 0 THEN h.`EnterMapId`
        ELSE h.`Map`
    END AS `Map`,
    h.`ElementId` AS `Element`,
    h.`Type` AS `Type`,
    h.`SkillsCSV` AS `SkillsCSV`,
    h.`ParametersCSV` AS `ParametersCSV`
FROM `world_maps_paddock_instance` h
WHERE h.`Id` > 0
  AND h.`ElementId` > 0
  AND h.`SkillsCSV` IS NOT NULL
  AND h.`SkillsCSV` <> '';";

                return DatabaseManager.Connection.Query<InteractiveSpawn>(sql)
                    .Where(x => x != null && x.PaddockInstanceId > 0 && x.Map > 0 && x.Element > 0)
                    .ToArray();
            }
            catch (Exception ex)
            {
                Logs.Logger.WriteError($"[INTERACTIVES] Impossible de charger les interactives d'enclos instanciés depuis world_maps_paddock_instance : {ex.Message}");
                return Enumerable.Empty<InteractiveSpawn>();
            }
        }

        private bool ColumnExists(string tableName, string columnName)
        {
            const string sql = @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = @TableName
  AND COLUMN_NAME = @ColumnName;";

            return DatabaseManager.Connection.ExecuteScalar<int>(sql, new { TableName = tableName, ColumnName = columnName }) > 0;
        }

        public IEnumerable<InteractiveSpawn> GetInteractiveSpawn(int map)
        {
            return DatabaseManager.Connection.Query<InteractiveSpawn>($"SELECT * FROM worlds_interactives WHERE Map = '{map}'");
        }

        public List<Interactive> GetInteractive(int map)
        {
            List<Interactive> interactives = new List<Interactive>();
            foreach (var interactive in GetInteractiveSpawn(map))
                interactives.Add(new Interactive(interactive));
            return interactives;
        }

        public void RegisterSkill(int skillUid, int mapId, int skillId, int elementId = 0, int houseId = 0, int paddockInstanceId = 0)
        {
            Interactives[skillUid] = new InteractiveSkillBinding(mapId, skillId, elementId, houseId, paddockInstanceId);
        }

        public int GenerateId()
        {
            return _idProvider.Pop();
        }
    }
}
