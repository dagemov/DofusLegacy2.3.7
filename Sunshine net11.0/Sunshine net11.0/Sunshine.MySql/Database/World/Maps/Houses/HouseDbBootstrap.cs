using System.Data;
using MySql.Data.MySqlClient;

namespace Sunshine.MySql.Database.World.Maps.Houses
{
    public static class HouseDbBootstrap
    {
        public static void EnsureWorldMapsHouseTable(MySqlConnection connection)
        {
            const string sql = @"
CREATE TABLE IF NOT EXISTS `world_maps_house` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
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

            if (connection.State != ConnectionState.Open)
                connection.Open();

            using (var command = new MySqlCommand(sql, connection))
                command.ExecuteNonQuery();
        }
    }
}
