using Dapper;
using Sunshine.Logs;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Maps.Houses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sunshine.MySql.Database.Managers
{
    public static class HouseTableBootstrap
    {
        private static readonly char[] Separator = new[] { ';' };
        private static readonly string SeedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "houses.txt");

        public static void EnsureTableAndSeed()
        {
            EnsureTable();
            DropLegacyChestColumns();
            EnsureHouseInteractiveColumns();
            EnsureExitInstanceColumns();
            EnsureHouseChestItemsTable();
            SeedIfEmpty();
        }

        private static void EnsureTable()
        {
            const string sql = @"
CREATE TABLE IF NOT EXISTS `world_maps_house` (
  `Id` int(11) NOT NULL,
  `GuildId` int(11) DEFAULT NULL,
  `MapId` int(11) NOT NULL,
  `EnterMapId` int(11) NOT NULL,
  `EnterCellId` int(11) NOT NULL DEFAULT '0',
  `EndMapIdInstance` int(11) NOT NULL DEFAULT '0',
  `EndCellIdIInstance` int(11) NOT NULL DEFAULT '0',
  `InstanceMapCellId` int(11) NOT NULL DEFAULT '0',
  `ModelId` int(11) NOT NULL DEFAULT '0',
  `InteractiveId` int(11) NOT NULL,
  `Map` int(11) NOT NULL DEFAULT '0',
  `ElementId` int(11) NOT NULL DEFAULT '0',
  `Type` int(11) NOT NULL DEFAULT '-1',
  `SkillsCSV` mediumtext DEFAULT NULL,
  `ParametersCSV` mediumtext DEFAULT NULL,
  `GuildShareParams` int(10) unsigned DEFAULT NULL,
  `OwnerName` varchar(255) DEFAULT NULL,
  `OwnerId` int(11) DEFAULT NULL,
  `Abandonned` tinyint(1) NOT NULL DEFAULT '0',
  `OnSale` tinyint(1) NOT NULL DEFAULT '0',
  `SaleLocked` tinyint(1) NOT NULL DEFAULT '0',
  `Locked` tinyint(1) NOT NULL DEFAULT '0',
  `Code` varchar(32) DEFAULT NULL,
  `Price` int(11) DEFAULT NULL,
  `DefaultPrice` int(11) NOT NULL DEFAULT '0',
  `InterorMapsIdsCSV` mediumtext DEFAULT NULL,
  `ChestType` int(11) NOT NULL DEFAULT '0',
  `ChestCode` varchar(32) DEFAULT NULL,
  `HasChest` tinyint(1) NOT NULL DEFAULT '0',
  `GuildChest` tinyint(1) NOT NULL DEFAULT '0',
  `ChestKamas` bigint(20) NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`),
  KEY `idx_world_maps_house_map` (`MapId`),
  KEY `idx_world_maps_house_interactive` (`InteractiveId`),
  KEY `idx_world_maps_house_enter_map` (`EnterMapId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

            DatabaseManager.Connection.Execute(sql);
        }

        private static void DropLegacyChestColumns()
        {
            for (int i = 1; i <= 20; i++)
            {
                string suffix = i.ToString("00");
                DropColumnIfExists("ChestKamas" + suffix);
                DropColumnIfExists("ChestCode" + suffix);
            }
        }

        private static void EnsureHouseInteractiveColumns()
        {
            AddColumnIfMissing("Map", "ALTER TABLE `world_maps_house` ADD COLUMN `Map` int(11) NOT NULL DEFAULT '0' AFTER `InteractiveId`;");
            AddColumnIfMissing("ElementId", "ALTER TABLE `world_maps_house` ADD COLUMN `ElementId` int(11) NOT NULL DEFAULT '0' AFTER `Map`;");
            AddColumnIfMissing("Type", "ALTER TABLE `world_maps_house` ADD COLUMN `Type` int(11) NOT NULL DEFAULT '-1' AFTER `ElementId`;");
            AddColumnIfMissing("SkillsCSV", "ALTER TABLE `world_maps_house` ADD COLUMN `SkillsCSV` mediumtext DEFAULT NULL AFTER `Type`;");
            AddColumnIfMissing("ParametersCSV", "ALTER TABLE `world_maps_house` ADD COLUMN `ParametersCSV` mediumtext DEFAULT NULL AFTER `SkillsCSV`;");

            DatabaseManager.Connection.Execute(@"
UPDATE `world_maps_house`
SET `Map` = `EnterMapId`
WHERE `Map` IS NULL OR `Map` = 0;");

            MigrateLegacyElementToElementIdIfPresent();

            DatabaseManager.Connection.Execute(@"
UPDATE `world_maps_house`
SET `Type` = -1
WHERE `Type` IS NULL OR `Type` <> -1;");

            // Ces colonnes provoquaient des conflits avec les interactives stockées dans worlds_interactives.
            // world_maps_house doit uniquement porter les portes internes via ElementId + SkillsCSV, avec Type = -1.
            DropColumnIfExists("Element");
            DropColumnIfExists("SkillListIdsCSV");
            DropColumnIfExists("ChestTemplateId");
        }

        private static void MigrateLegacyElementToElementIdIfPresent()
        {
            if (!ColumnExists("world_maps_house", "Element"))
                return;

            DatabaseManager.Connection.Execute(@"
UPDATE `world_maps_house`
SET `ElementId` = `Element`
WHERE (`ElementId` IS NULL OR `ElementId` = 0)
  AND `Element` IS NOT NULL
  AND `Element` > 0;");
        }

        private static bool ColumnExists(string tableName, string columnName)
        {
            const string existsSql = @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = @TableName
  AND COLUMN_NAME = @ColumnName;";

            return DatabaseManager.Connection.ExecuteScalar<int>(existsSql, new { TableName = tableName, ColumnName = columnName }) > 0;
        }

        private static void AddColumnIfMissing(string columnName, string alterSql)
        {
            if (!ColumnExists("world_maps_house", columnName))
                DatabaseManager.Connection.Execute(alterSql);
        }

        private static void DropColumnIfExists(string columnName)
        {
            if (ColumnExists("world_maps_house", columnName))
                DatabaseManager.Connection.Execute($"ALTER TABLE `world_maps_house` DROP COLUMN `{columnName}`;");
        }

        private static void EnsureExitInstanceColumns()
        {
            if (!ColumnExists("world_maps_house", "EndMapIdInstance"))
                DatabaseManager.Connection.Execute("ALTER TABLE `world_maps_house` ADD COLUMN `EndMapIdInstance` int(11) NOT NULL DEFAULT '0' AFTER `EnterCellId`;");

            if (!ColumnExists("world_maps_house", "EndCellIdIInstance"))
                DatabaseManager.Connection.Execute("ALTER TABLE `world_maps_house` ADD COLUMN `EndCellIdIInstance` int(11) NOT NULL DEFAULT '0' AFTER `EndMapIdInstance`;");

            if (!ColumnExists("world_maps_house", "InstanceMapCellId"))
                DatabaseManager.Connection.Execute("ALTER TABLE `world_maps_house` ADD COLUMN `InstanceMapCellId` int(11) NOT NULL DEFAULT '0' AFTER `EndCellIdIInstance`;");

            DatabaseManager.Connection.Execute(@"
UPDATE `world_maps_house`
SET `EndMapIdInstance` = `MapId`
WHERE `EndMapIdInstance` IS NULL OR `EndMapIdInstance` = 0;");
        }

        private static void EnsureHouseChestItemsTable()
        {
            const string sql = @"
CREATE TABLE IF NOT EXISTS `house_chest_items` (
  `HouseId` int(11) NOT NULL,
  `ItemUid` int(11) NOT NULL,
  `TemplateId` int(11) NOT NULL,
  `Stack` int(11) NOT NULL DEFAULT '1',
  `Position` int(11) NOT NULL DEFAULT '63',
  `Effects` mediumtext DEFAULT NULL,
  PRIMARY KEY (`HouseId`,`ItemUid`),
  KEY `idx_house_chest_items_house` (`HouseId`),
  KEY `idx_house_chest_items_template` (`TemplateId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

            DatabaseManager.Connection.Execute(sql);
        }

        private static void SeedIfEmpty()
        {
            var count = DatabaseManager.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM world_maps_house");
            if (count > 0)
            {
                Logger.WriteInfo($"world_maps_house already contains {count} row(s).");
                return;
            }

            if (!File.Exists(SeedPath))
            {
                Logger.WriteInfo("world_maps_house created. No seed file found at data/houses.txt.");
                return;
            }

            var entries = LoadSeedEntries().ToArray();
            if (entries.Length == 0)
            {
                Logger.WriteInfo("world_maps_house created. Seed file is empty or contains only comments.");
                return;
            }

            const string insertSql = @"
INSERT INTO world_maps_house
(`Id`,`GuildId`,`MapId`,`EnterMapId`,`EnterCellId`,`EndMapIdInstance`,`EndCellIdIInstance`,`InstanceMapCellId`,`ModelId`,`InteractiveId`,`Map`,`ElementId`,`Type`,`SkillsCSV`,`ParametersCSV`,`GuildShareParams`,`OwnerName`,`OwnerId`,`Abandonned`,`OnSale`,`SaleLocked`,`Locked`,`Code`,`Price`,`DefaultPrice`,`InterorMapsIdsCSV`,`ChestType`,`ChestCode`,`HasChest`,`GuildChest`,`ChestKamas`)
VALUES
(@Id,@GuildId,@MapId,@EnterMapId,@EnterCellId,@EndMapIdInstance,@EndCellIdIInstance,@InstanceMapCellId,@ModelId,@InteractiveId,@HouseInteractiveMap,@ElementId,-1,@SkillsCSV,@HouseInteractiveParametersCSV,@GuildShareParams,@OwnerName,@OwnerId,@Abandonned,@OnSale,@SaleLocked,@Locked,@Code,@Price,@DefaultPrice,@InterorMapsIdsCSV,@ChestType,@ChestCode,@HasChest,@GuildChest,@ChestKamas);";

            using (var transaction = DatabaseManager.Connection.BeginTransaction())
            {
                foreach (var entry in entries)
                    DatabaseManager.Connection.Execute(insertSql, entry, transaction);

                transaction.Commit();
            }

            Logger.WriteInfo($"world_maps_house seeded automatically with {entries.Length} row(s) from data/houses.txt.");
        }

        private static IEnumerable<WorldMapHouseRecord> LoadSeedEntries()
        {
            foreach (var rawLine in File.ReadAllLines(SeedPath))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split(Separator, StringSplitOptions.None);
                if (parts.Length < 10)
                {
                    Logger.WriteError($"[HOUSES] Invalid seed line, expected 10 columns: {line}");
                    continue;
                }

                if (!int.TryParse(parts[0].Trim(), out var id) ||
                    !int.TryParse(parts[1].Trim(), out var mapId) ||
                    !int.TryParse(parts[2].Trim(), out var enterMapId) ||
                    !int.TryParse(parts[3].Trim(), out var enterCellId) ||
                    !int.TryParse(parts[4].Trim(), out var modelId) ||
                    !int.TryParse(parts[5].Trim(), out var interactiveId) ||
                    !int.TryParse(parts[6].Trim(), out var defaultPrice))
                {
                    Logger.WriteError($"[HOUSES] Invalid numeric values in seed line: {line}");
                    continue;
                }

                var hasChest = parts[7].Trim() == "1" || parts[7].Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                var interiorsCsv = parts[8].Trim();

                yield return new WorldMapHouseRecord
                {
                    Id = id,
                    GuildId = null,
                    MapId = mapId,
                    EnterMapId = enterMapId,
                    EnterCellId = enterCellId,
                    EndMapIdInstance = mapId,
                    EndCellIdIInstance = 0,
                    InstanceMapCellId = 0,
                    ModelId = modelId,
                    InteractiveId = interactiveId,
                    HouseInteractiveMap = enterMapId,
                    ElementId = 0,
                    HouseInteractiveType = -1,
                    SkillsCSV = string.Empty,
                    HouseInteractiveParametersCSV = string.Empty,
                    GuildShareParams = null,
                    OwnerName = string.Empty,
                    OwnerId = null,
                    Abandonned = false,
                    OnSale = true,
                    SaleLocked = false,
                    Locked = false,
                    Code = string.Empty,
                    Price = null,
                    DefaultPrice = defaultPrice,
                    InterorMapsIdsCSV = interiorsCsv,
                    ChestType = 0,
                    ChestCode = string.Empty,
                    HasChest = hasChest,
                    GuildChest = false,
                    ChestKamas = 0,
                };
            }
        }
    }
}
