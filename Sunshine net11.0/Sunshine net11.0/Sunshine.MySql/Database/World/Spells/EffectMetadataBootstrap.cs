using System.Data;
using MySql.Data.MySqlClient;

namespace Sunshine.MySql.Database.World.Spells
{
    /// <summary>
    /// Idempotent creation of the `effect_metadata` side table. Mirrors HouseDbBootstrap so the
    /// table is guaranteed to exist before the cache loads, without requiring a manual migration.
    /// Additive only: an empty table reproduces the current behavior (every handler falls back).
    /// </summary>
    public static class EffectMetadataBootstrap
    {
        public static void EnsureEffectMetadataTable(MySqlConnection connection)
        {
            const string sql = @"
CREATE TABLE IF NOT EXISTS `effect_metadata` (
  `SpellId` int(11) NOT NULL,
  `EffectId` int(11) NOT NULL,
  `KillTarget` tinyint(4) NOT NULL DEFAULT '0',
  `RequiresState` int(11) NOT NULL DEFAULT '0',
  `BonusIfState` tinyint(4) NOT NULL DEFAULT '0',
  `BonusMultiplier` decimal(4,2) NOT NULL DEFAULT '1.00',
  `GrantsStateOnCast` int(11) NOT NULL DEFAULT '0',
  `AllowEnemyTarget` tinyint(4) NOT NULL DEFAULT '0',
  `TriggerTiming` tinyint(4) NOT NULL DEFAULT '0',
  PRIMARY KEY (`SpellId`,`EffectId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

            if (connection.State != ConnectionState.Open)
                connection.Open();

            using (var command = new MySqlCommand(sql, connection))
                command.ExecuteNonQuery();
        }
    }
}
