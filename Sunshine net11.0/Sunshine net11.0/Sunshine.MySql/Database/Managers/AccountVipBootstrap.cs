using Dapper;
using MySql.Data.MySqlClient;
using Sunshine.Mysql.Database;
using System;

namespace Sunshine.MySql.Database.Managers
{
    public static class AccountVipBootstrap
    {
        private static bool _ensured;
        private static readonly object Locker = new object();

        public static void EnsureVipColumn()
        {
            if (_ensured)
                return;

            lock (Locker)
            {
                if (_ensured)
                    return;

                try
                {
                    DatabaseManager.Connection.Execute("ALTER TABLE `accounts` ADD COLUMN `Vip` tinyint(1) NOT NULL DEFAULT 0 AFTER `NewTokens`;");
                }
                catch (MySqlException ex)
                {
                    // 1060 = duplicate column. The migration is idempotent at runtime.
                    if (ex.Number != 1060)
                        Logs.Logger.WriteError($"[ ACCOUNTS ] Impossible d'ajouter accounts.Vip : {ex.Message}");
                }
                catch (Exception ex)
                {
                    Logs.Logger.WriteError($"[ ACCOUNTS ] Impossible de verifier accounts.Vip : {ex.Message}");
                }

                _ensured = true;
            }
        }
    }
}
