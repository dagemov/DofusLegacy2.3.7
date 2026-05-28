using Dapper;
using MySql.Data.MySqlClient;
using Sunshine.Mysql.Database;
using System;

namespace Sunshine.MySql.Database.Managers
{
    public static class CharacterPaddockBootstrap
    {
        private static bool _ensured;
        private static readonly object Locker = new object();

        public static void EnsurePaddockIdColumn()
        {
            if (_ensured)
                return;

            lock (Locker)
            {
                if (_ensured)
                    return;

                try
                {
                    DatabaseManager.Connection.Execute("ALTER TABLE `characters` ADD COLUMN `PaddockId` int(11) NULL DEFAULT NULL AFTER `HouseId`;");
                }
                catch (MySqlException ex)
                {
                    // 1060 = duplicate column. The migration is idempotent at runtime.
                    if (ex.Number != 1060)
                        Logs.Logger.WriteError($"[ CHARACTERS ] Impossible d'ajouter characters.PaddockId : {ex.Message}");
                }
                catch (Exception ex)
                {
                    Logs.Logger.WriteError($"[ CHARACTERS ] Impossible de verifier characters.PaddockId : {ex.Message}");
                }

                try
                {
                    DatabaseManager.Connection.Execute("ALTER TABLE `characters` ADD INDEX `idx_characters_paddockid` (`PaddockId`);");
                }
                catch (MySqlException ex)
                {
                    // 1061 = duplicate key name.
                    if (ex.Number != 1061)
                        Logs.Logger.WriteError($"[ CHARACTERS ] Impossible d'ajouter idx_characters_paddockid : {ex.Message}");
                }
                catch
                {
                }

                _ensured = true;
            }
        }
    }
}
