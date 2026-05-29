using Dapper;
using Sunshine.Mysql.Database;
using System;

namespace Sunshine.MySql.Database.Managers
{
    public static class PaddockInstanceTableBootstrap
    {
        public static void EnsureTable()
        {
            CreateTableIfMissing();
            EnsureCoreColumns();
            DropRemovedColumns();
        }

        private static void CreateTableIfMissing()
        {
            const string sql = @"
CREATE TABLE IF NOT EXISTS `world_maps_paddock_instance` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `GuildId` int(11) DEFAULT NULL,
  `MapId` int(11) NOT NULL,
  `EnterMapId` int(11) NOT NULL DEFAULT '0',
  `EnterCellId` int(11) NOT NULL DEFAULT '426',
  `Zone` varchar(50) DEFAULT '',
  `InteractiveId` int(11) NOT NULL,
  `InterorMapsIdsCSV` mediumtext DEFAULT NULL,
  `Map` int(11) NOT NULL DEFAULT '0',
  `ElementId` int(11) NOT NULL DEFAULT '0',
  `Type` int(11) NOT NULL DEFAULT '-1',
  `SkillsCSV` mediumtext DEFAULT NULL,
  `ParametersCSV` mediumtext DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `idx_world_maps_paddock_instance_map` (`MapId`),
  KEY `idx_world_maps_paddock_instance_enter_map` (`EnterMapId`),
  KEY `idx_world_maps_paddock_instance_interactive` (`InteractiveId`),
  KEY `idx_world_maps_paddock_instance_internal_map` (`Map`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

            DatabaseManager.Connection.Execute(sql);
        }

        private static void EnsureCoreColumns()
        {
            AddColumnIfMissing("GuildId", "ALTER TABLE `world_maps_paddock_instance` ADD COLUMN `GuildId` int(11) DEFAULT NULL AFTER `Id`;");
            AddColumnIfMissing("MapId", "ALTER TABLE `world_maps_paddock_instance` ADD COLUMN `MapId` int(11) NOT NULL DEFAULT '0' AFTER `GuildId`;");
            AddColumnIfMissing("EnterMapId", "ALTER TABLE `world_maps_paddock_instance` ADD COLUMN `EnterMapId` int(11) NOT NULL DEFAULT '0' AFTER `MapId`;");
            AddColumnIfMissing("EnterCellId", "ALTER TABLE `world_maps_paddock_instance` ADD COLUMN `EnterCellId` int(11) NOT NULL DEFAULT '426' AFTER `EnterMapId`;");
            AddColumnIfMissing("Zone", "ALTER TABLE `world_maps_paddock_instance` ADD COLUMN `Zone` varchar(50) DEFAULT '' AFTER `EnterCellId`;");
            AddColumnIfMissing("InteractiveId", "ALTER TABLE `world_maps_paddock_instance` ADD COLUMN `InteractiveId` int(11) NOT NULL DEFAULT '0' AFTER `Zone`;");
            AddColumnIfMissing("InterorMapsIdsCSV", "ALTER TABLE `world_maps_paddock_instance` ADD COLUMN `InterorMapsIdsCSV` mediumtext DEFAULT NULL AFTER `InteractiveId`;");
            AddColumnIfMissing("Map", "ALTER TABLE `world_maps_paddock_instance` ADD COLUMN `Map` int(11) NOT NULL DEFAULT '0' AFTER `InterorMapsIdsCSV`;");
            AddColumnIfMissing("ElementId", "ALTER TABLE `world_maps_paddock_instance` ADD COLUMN `ElementId` int(11) NOT NULL DEFAULT '0' AFTER `Map`;");
            AddColumnIfMissing("Type", "ALTER TABLE `world_maps_paddock_instance` ADD COLUMN `Type` int(11) NOT NULL DEFAULT '-1' AFTER `ElementId`;");
            AddColumnIfMissing("SkillsCSV", "ALTER TABLE `world_maps_paddock_instance` ADD COLUMN `SkillsCSV` mediumtext DEFAULT NULL AFTER `Type`;");
            AddColumnIfMissing("ParametersCSV", "ALTER TABLE `world_maps_paddock_instance` ADD COLUMN `ParametersCSV` mediumtext DEFAULT NULL AFTER `SkillsCSV`;");

            DatabaseManager.Connection.Execute(@"
UPDATE `world_maps_paddock_instance`
SET `EnterMapId` = CAST(SUBSTRING_INDEX(`InterorMapsIdsCSV`, ',', 1) AS UNSIGNED)
WHERE (`EnterMapId` IS NULL OR `EnterMapId` = 0)
  AND `InterorMapsIdsCSV` IS NOT NULL
  AND `InterorMapsIdsCSV` <> '';

UPDATE `world_maps_paddock_instance`
SET `EnterCellId` = 426
WHERE `EnterCellId` IS NULL OR `EnterCellId` = 0;

UPDATE `world_maps_paddock_instance`
SET `Map` = `EnterMapId`
WHERE `Map` IS NULL OR `Map` = 0;");

            DatabaseManager.Connection.Execute(@"
UPDATE `world_maps_paddock_instance`
SET `Type` = -1
WHERE `Type` IS NULL;");
        }

        private static void DropRemovedColumns()
        {
            DropColumnIfExists("EndMapIdInstance");
            DropColumnIfExists("EndCellIdIInstance");
            DropColumnIfExists("InstanceMapCellId");
            DropColumnIfExists("ChestKamas");
            DropColumnIfExists("ChestType");
            DropColumnIfExists("GuildChest");
            DropColumnIfExists("HasChest");
            DropColumnIfExists("ChestCode");
            DropColumnIfExists("DefaultPrice");
            DropColumnIfExists("Price");
            DropColumnIfExists("Code");
            DropColumnIfExists("Locked");
            DropColumnIfExists("SaleLocked");
            DropColumnIfExists("OnSale");
            DropColumnIfExists("Abandonned");
            DropColumnIfExists("OwnerId");
            DropColumnIfExists("OwnerName");
            DropColumnIfExists("GuildShareParams");
            DropColumnIfExists("ModelId");
        }

        private static bool ColumnExists(string columnName)
        {
            const string sql = @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'world_maps_paddock_instance'
  AND COLUMN_NAME = @ColumnName;";

            return DatabaseManager.Connection.ExecuteScalar<int>(sql, new { ColumnName = columnName }) > 0;
        }

        private static void AddColumnIfMissing(string columnName, string alterSql)
        {
            if (!ColumnExists(columnName))
                DatabaseManager.Connection.Execute(alterSql);
        }

        private static void DropColumnIfExists(string columnName)
        {
            if (ColumnExists(columnName))
                DatabaseManager.Connection.Execute($"ALTER TABLE `world_maps_paddock_instance` DROP COLUMN `{columnName}`;");
        }
    }
}
