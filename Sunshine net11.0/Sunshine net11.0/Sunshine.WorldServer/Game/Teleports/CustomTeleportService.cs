using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Teleports;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Maps;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Teleports
{
    public enum CustomTeleportCategory
    {
        Maps,
        Dungeons,
        XpZones,
    }

    public static class CustomTeleportService
    {
        public const string MapsTable = "teleports_maps";
        public const string DungeonsTable = "teleports_donjons_maps";
        public const string XpZonesTable = "teleports_zones_maps";

        private static bool _tablesEnsured;
        private static readonly object _locker = new object();

        public static string GetTableName(CustomTeleportCategory category)
        {
            switch (category)
            {
                case CustomTeleportCategory.Dungeons:
                    return DungeonsTable;
                case CustomTeleportCategory.XpZones:
                    return XpZonesTable;
                default:
                    return MapsTable;
            }
        }

        public static void EnsureTables()
        {
            if (_tablesEnsured)
                return;

            lock (_locker)
            {
                if (_tablesEnsured)
                    return;

                EnsureTable(MapsTable);
                EnsureTable(DungeonsTable);
                EnsureTable(XpZonesTable);
                _tablesEnsured = true;
            }
        }

        public static List<CustomTeleportDestinationRecord> GetDestinations(CustomTeleportCategory category)
        {
            EnsureTables();

            string table = GetTableName(category);
            var results = DatabaseManager.Connection.Query<CustomTeleportDestinationRecord>($@"
SELECT Id, TeleportMapId, TeleportCellId, DestinationName, DestinationDescription, KamasCost,
       CASE
           WHEN EXISTS (
               SELECT 1
               FROM information_schema.columns
               WHERE table_schema = DATABASE()
                 AND table_name = @TableName
                 AND column_name = 'RequiredItemId'
           ) THEN COALESCE(RequiredItemId, 0)
           ELSE 0
       END AS RequiredItemId
FROM {table}
ORDER BY Id ASC", new { TableName = table }).ToList();

            return results
                .Where(x => x != null)
                .GroupBy(x => x.TeleportMapId)
                .Select(x => x.First())
                .Where(x => MapManager.Instance.GetMap(x.TeleportMapId) != null)
                .ToList();
        }

        public static bool HasRequiredItem(Character character, CustomTeleportDestinationRecord record)
        {
            if (character == null || record == null)
                return false;

            if (record.RequiredItemId <= 0)
                return true;

            return character.Inventory
                .GetItems(CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED)
                .Any(x => x.Template != null && x.Template.Id == record.RequiredItemId);
        }

        public static bool TryConsumeRequiredItem(Character character, CustomTeleportDestinationRecord record)
        {
            if (character == null || record == null)
                return false;

            if (record.RequiredItemId <= 0)
                return true;

            var item = character.Inventory
                .GetItems(CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED)
                .FirstOrDefault(x => x != null && x.Template != null && x.Template.Id == record.RequiredItemId);

            if (item == null)
                return false;

            character.Inventory.RemoveItem(item, 1);
            return true;
        }

        public static short ResolveTeleportCell(Map map, CustomTeleportDestinationRecord record)
        {
            if (map?.Cells == null || map.Cells.Length == 0)
                return 0;

            short configuredCell = record != null && record.TeleportCellId > 0
                ? (short)record.TeleportCellId
                : (short)-1;

            if (IsValidTeleportCell(map, configuredCell))
                return configuredCell;

            if (configuredCell >= 0)
            {
                var nearestValidCell = map.Cells
                    .Where(x => IsValidTeleportCell(map, x.Id))
                    .OrderBy(x => Math.Abs(x.Id - configuredCell))
                    .FirstOrDefault();

                if (!nearestValidCell.Equals(default(Protocol.Tools.Dlm.DlmCellData)))
                    return nearestValidCell.Id;
            }

            var firstWalkable = map.Cells.FirstOrDefault(x => IsValidTeleportCell(map, x.Id));
            return !firstWalkable.Equals(default(Protocol.Tools.Dlm.DlmCellData)) ? firstWalkable.Id : (short)0;
        }

        private static bool IsValidTeleportCell(Map map, short cellId)
        {
            if (map?.Cells == null || cellId <= 0 || cellId >= map.Cells.Length)
                return false;

            var cell = map.Cells[cellId];
            return cell.Walkable && !cell.NonWalkableDuringRP;
        }

        private static void EnsureTable(string tableName)
        {
            DatabaseManager.Connection.Execute($@"
CREATE TABLE IF NOT EXISTS `{tableName}` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `TeleportMapId` int NOT NULL,
    `TeleportCellId` int NOT NULL DEFAULT '0',
    `DestinationName` varchar(191) DEFAULT NULL,
    `DestinationDescription` varchar(191) DEFAULT NULL,
    `KamasCost` int NOT NULL DEFAULT '0',
    `RequiredItemId` int NOT NULL DEFAULT '0',
    PRIMARY KEY (`Id`),
    KEY `idx_{tableName}_mapid` (`TeleportMapId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            EnsureColumn(tableName, "TeleportCellId", "ALTER TABLE `{0}` ADD COLUMN `TeleportCellId` int NOT NULL DEFAULT '0';");
            EnsureColumn(tableName, "DestinationName", "ALTER TABLE `{0}` ADD COLUMN `DestinationName` varchar(191) DEFAULT NULL;");
            EnsureColumn(tableName, "DestinationDescription", "ALTER TABLE `{0}` ADD COLUMN `DestinationDescription` varchar(191) DEFAULT NULL;");
            EnsureColumn(tableName, "KamasCost", "ALTER TABLE `{0}` ADD COLUMN `KamasCost` int NOT NULL DEFAULT '0';");
            EnsureColumn(tableName, "RequiredItemId", "ALTER TABLE `{0}` ADD COLUMN `RequiredItemId` int NOT NULL DEFAULT '0';");
        }

        private static void EnsureColumn(string tableName, string columnName, string alterTemplate)
        {
            int exists = DatabaseManager.Connection.ExecuteScalar<int>(@"
SELECT COUNT(*)
FROM information_schema.columns
WHERE table_schema = DATABASE()
  AND table_name = @TableName
  AND column_name = @ColumnName;", new { TableName = tableName, ColumnName = columnName });

            if (exists <= 0)
                DatabaseManager.Connection.Execute(string.Format(alterTemplate, tableName));
        }
    }
}
