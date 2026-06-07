using System;
using Sunshine.BaseServer.Configuration;

namespace Sunshine.WorldServer.Game.Fights
{
    internal static class CombatReadyCheckerSettings
    {
        public static bool Enabled => ResolveBool("CombatReadyCheckerEnabled", true);

        public static int TimeoutMs
        {
            get
            {
                var raw = GameConfig.GetString("CombatReadyCheckerTimeoutMs", "5000");
                return int.TryParse(raw, out var value) && value > 0 ? value : 5000;
            }
        }

        private static bool ResolveBool(string key, bool defaultValue)
        {
            var raw = GameConfig.GetString(key, defaultValue ? "true" : "false");
            if (string.IsNullOrWhiteSpace(raw))
                return defaultValue;

            if (bool.TryParse(raw, out var parsed))
                return parsed;

            if (raw == "1")
                return true;

            if (raw == "0")
                return false;

            return defaultValue;
        }
    }
}
