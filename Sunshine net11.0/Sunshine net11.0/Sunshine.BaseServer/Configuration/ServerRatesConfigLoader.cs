using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Sunshine.BaseServer.Configuration
{
    public sealed class ServerRatesConfigLoader
    {
        private static readonly string[] DefaultFileContentLines =
        {
            "# Server rates — edit and restart Sunshine to apply.",
            "# 0 on combat limits means unlimited where documented.",
            "XP_RATE=2",
            "DROP_RATE=1",
            "KAMAS_RATE=1",
            "PP_RATE=1",
            "WEAPON_USES_PER_TURN=2",
            "WEAPON_USES_PER_FIGHT=0",
            "SPELL_USES_DEFAULT=0"
        };

        public ServerRatesLoadResult Load(string filePath, Action<string> logInfo, Action<string> logWarning)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                filePath = ResolveDefaultPath();

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var createdFile = false;
            if (!File.Exists(filePath))
            {
                var seed = ServerRatesConfig.CreateFromGameConfigFallback();
                WriteConfigFile(filePath, seed);
                createdFile = true;
                logInfo?.Invoke($"Server rates file created at '{filePath}' with defaults.");
            }

            var defaults = ServerRatesConfig.CreateFromGameConfigFallback();
            var config = new ServerRatesConfig
            {
                XpRate = defaults.XpRate,
                DropRate = defaults.DropRate,
                KamasRate = defaults.KamasRate,
                PpRate = defaults.PpRate,
                WeaponUsesPerTurn = defaults.WeaponUsesPerTurn,
                WeaponUsesPerFight = defaults.WeaponUsesPerFight,
                SpellUsesDefault = defaults.SpellUsesDefault
            };

            var warnings = new List<string>();
            foreach (var rawLine in File.ReadAllLines(filePath))
            {
                var line = (rawLine ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//") || line.StartsWith(";"))
                    continue;

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    warnings.Add($"Ignored malformed line: '{line}'");
                    continue;
                }

                var key = line.Substring(0, separatorIndex).Trim().ToUpperInvariant();
                var value = line.Substring(separatorIndex + 1).Trim();

                if (!TryApplyKey(config, defaults, key, value, out var warning))
                    warnings.Add(warning ?? $"Unknown key '{key}' on line '{line}'");
            }

            foreach (var warning in warnings)
                logWarning?.Invoke($"Server rates config: {warning}");

            return new ServerRatesLoadResult(config, filePath, createdFile, warnings);
        }

        public static string ResolveDefaultPath()
        {
            var configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
            return Path.Combine(configDirectory, ServerRatesConfig.FileName);
        }

        private static void WriteConfigFile(string filePath, ServerRatesConfig seed)
        {
            var lines = new[]
            {
                DefaultFileContentLines[0],
                DefaultFileContentLines[1],
                $"XP_RATE={FormatRate(seed.XpRate)}",
                $"DROP_RATE={FormatRate(seed.DropRate)}",
                $"KAMAS_RATE={FormatRate(seed.KamasRate)}",
                $"PP_RATE={FormatRate(seed.PpRate)}",
                $"WEAPON_USES_PER_TURN={seed.WeaponUsesPerTurn}",
                $"WEAPON_USES_PER_FIGHT={seed.WeaponUsesPerFight}",
                $"SPELL_USES_DEFAULT={seed.SpellUsesDefault}"
            };

            File.WriteAllLines(filePath, lines, Encoding.UTF8);
        }

        private static string FormatRate(double value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        private static bool TryApplyKey(ServerRatesConfig config, ServerRatesConfig defaults, string key, string rawValue, out string warning)
        {
            warning = null;

            switch (key)
            {
                case "XP_RATE":
                    return TrySetRate(rawValue, defaults.XpRate, value => config.XpRate = value, key, out warning);
                case "DROP_RATE":
                    return TrySetRate(rawValue, defaults.DropRate, value => config.DropRate = value, key, out warning);
                case "KAMAS_RATE":
                    return TrySetRate(rawValue, defaults.KamasRate, value => config.KamasRate = value, key, out warning);
                case "PP_RATE":
                    return TrySetRate(rawValue, defaults.PpRate, value => config.PpRate = value, key, out warning);
                case "WEAPON_USES_PER_TURN":
                    return TrySetInt(rawValue, defaults.WeaponUsesPerTurn, value => config.WeaponUsesPerTurn = value, key, out warning);
                case "WEAPON_USES_PER_FIGHT":
                    return TrySetInt(rawValue, defaults.WeaponUsesPerFight, value => config.WeaponUsesPerFight = value, key, out warning);
                case "SPELL_USES_DEFAULT":
                    return TrySetInt(rawValue, defaults.SpellUsesDefault, value => config.SpellUsesDefault = value, key, out warning);
                default:
                    warning = $"Unknown key '{key}'";
                    return false;
            }
        }

        private static bool TrySetRate(string rawValue, double fallback, Action<double> assign, string key, out string warning)
        {
            if (double.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value)
                && !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value >= 0d)
            {
                assign(value);
                warning = null;
                return true;
            }

            assign(fallback);
            warning = $"Invalid value for {key}='{rawValue}', using default {FormatRate(fallback)}";
            return false;
        }

        private static bool TrySetInt(string rawValue, int fallback, Action<int> assign, string key, out string warning)
        {
            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 0)
            {
                assign(value);
                warning = null;
                return true;
            }

            assign(fallback);
            warning = $"Invalid value for {key}='{rawValue}', using default {fallback}";
            return false;
        }
    }

    public sealed class ServerRatesLoadResult
    {
        public ServerRatesLoadResult(ServerRatesConfig config, string filePath, bool createdFile, IReadOnlyList<string> warnings)
        {
            Config = config;
            FilePath = filePath;
            CreatedFile = createdFile;
            Warnings = warnings;
        }

        public ServerRatesConfig Config { get; }

        public string FilePath { get; }

        public bool CreatedFile { get; }

        public IReadOnlyList<string> Warnings { get; }
    }
}
