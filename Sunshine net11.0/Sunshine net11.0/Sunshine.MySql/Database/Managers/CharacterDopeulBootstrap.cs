using Dapper;
using MySql.Data.MySqlClient;
using Sunshine.Mysql.Database;
using System;

namespace Sunshine.MySql.Database.Managers
{
    public static class CharacterDopeulBootstrap
    {
        private static bool _ensured;
        private static readonly object Locker = new object();

        public static void EnsureCooldownTable()
        {
            if (_ensured)
                return;

            lock (Locker)
            {
                if (_ensured)
                    return;

                try
                {
                    DatabaseManager.Connection.Execute(
                        @"CREATE TABLE IF NOT EXISTS `characters_dopeul_cooldown` (
                            `CharacterId` int(11) NOT NULL,
                            `MonsterId` int(11) NOT NULL,
                            `LastFightTime` datetime NOT NULL,
                            PRIMARY KEY (`CharacterId`, `MonsterId`)
                          ) ENGINE=InnoDB DEFAULT CHARSET=utf8;");
                }
                catch (Exception ex)
                {
                    Logs.Logger.WriteError($"[ DOPEUL ] Impossible de creer characters_dopeul_cooldown : {ex.Message}");
                }

                _ensured = true;
            }
        }
    }
}
