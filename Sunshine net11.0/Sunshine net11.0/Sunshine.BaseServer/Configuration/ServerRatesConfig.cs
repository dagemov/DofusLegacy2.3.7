using System;

namespace Sunshine.BaseServer.Configuration
{
    public sealed class ServerRatesConfig
    {
        public const string FileName = "config_rates_Server.txt";

        public double XpRate { get; set; } = 2d;
        public double DropRate { get; set; } = 1d;
        public double KamasRate { get; set; } = 1d;
        public double PpRate { get; set; } = 1d;
        public int WeaponUsesPerTurn { get; set; } = 2;
        public int WeaponUsesPerFight { get; set; } = 0;
        public int SpellUsesDefault { get; set; } = 0;

        public static ServerRatesConfig CreateSafeDefaults()
        {
            return new ServerRatesConfig();
        }

        public static ServerRatesConfig CreateFromGameConfigFallback()
        {
            return new ServerRatesConfig
            {
                XpRate = NormalizeRate(GameConfig.GetDouble("RateXp", 2d), 2d),
                DropRate = NormalizeRate(GameConfig.GetDouble("RateDrop", 1d), 1d),
                KamasRate = NormalizeRate(GameConfig.GetDouble("RateKamas", 1d), 1d),
                PpRate = 1d,
                WeaponUsesPerTurn = 2,
                WeaponUsesPerFight = 0,
                SpellUsesDefault = 0
            };
        }

        public int ResolveMaxWeaponUsesPerTurn()
        {
            return WeaponUsesPerTurn <= 0 ? int.MaxValue : WeaponUsesPerTurn;
        }

        public int ResolveMaxWeaponUsesPerFight()
        {
            return WeaponUsesPerFight <= 0 ? int.MaxValue : WeaponUsesPerFight;
        }

        public uint ResolveMaxSpellUsesPerTurn(uint templateMaxCastPerTurn)
        {
            if (templateMaxCastPerTurn > 0u)
                return templateMaxCastPerTurn;

            if (SpellUsesDefault <= 0)
                return 0u;

            return (uint)SpellUsesDefault;
        }

        private static double NormalizeRate(double value, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return fallback;

            return Math.Max(0d, value);
        }
    }
}
