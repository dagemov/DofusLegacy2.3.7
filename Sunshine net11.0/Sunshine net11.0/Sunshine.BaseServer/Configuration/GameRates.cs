using System;

namespace Sunshine.BaseServer.Configuration
{
    public static class GameRates
    {
        private const double MinimumRate = 0d;
        private static readonly Random _random = new Random();

        static GameRates()
        {
            Reload();
        }

        private static double Normalize(double value, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return fallback;

            return Math.Max(MinimumRate, value);
        }

        private static int NormalizeInt(int value, int fallback)
        {
            return value < 0 ? fallback : value;
        }

        public static void Reload()
        {
            Xp = Normalize(GameConfig.GetDouble("RateXp", 5d), 5d);
            Kamas = Normalize(GameConfig.GetDouble("RateKamas", 5d), 5d);
            Drop = Normalize(GameConfig.GetDouble("RateDrop", 5d), 5d);
            JobXp = Normalize(GameConfig.GetDouble("RateJobXp", 5d), 5d);
            MountXp = Normalize(GameConfig.GetDouble("RateMountXp", 5d), 5d);

            FightKamasLevel1Min = NormalizeInt(GameConfig.GetInt("FightKamasLevel1Min", 2500), 2500);
            FightKamasLevel1Max = NormalizeInt(GameConfig.GetInt("FightKamasLevel1Max", 7500), 7500);
            FightKamasLevel50Min = NormalizeInt(GameConfig.GetInt("FightKamasLevel50Min", 8000), 8000);
            FightKamasLevel50Max = NormalizeInt(GameConfig.GetInt("FightKamasLevel50Max", 18000), 18000);
            FightKamasLevel100Min = NormalizeInt(GameConfig.GetInt("FightKamasLevel100Min", 15000), 15000);
            FightKamasLevel100Max = NormalizeInt(GameConfig.GetInt("FightKamasLevel100Max", 30000), 30000);
            FightKamasLevel150Min = NormalizeInt(GameConfig.GetInt("FightKamasLevel150Min", 25000), 25000);
            FightKamasLevel150Max = NormalizeInt(GameConfig.GetInt("FightKamasLevel150Max", 50000), 50000);
            FightKamasLevel190Min = NormalizeInt(GameConfig.GetInt("FightKamasLevel190Min", 40000), 40000);
            FightKamasLevel190Max = NormalizeInt(GameConfig.GetInt("FightKamasLevel190Max", 80000), 80000);
            DropQuantityMultiplier = Normalize(GameConfig.GetDouble("DropQuantityMultiplier", 1d), 1d);
        }

        public static double Xp { get; private set; }
        public static double Kamas { get; private set; }
        public static double Drop { get; private set; }
        public static double JobXp { get; private set; }
        public static double MountXp { get; private set; }

        public static double DropQuantityMultiplier { get; private set; }

        public static int FightKamasLevel1Min { get; private set; }
        public static int FightKamasLevel1Max { get; private set; }
        public static int FightKamasLevel50Min { get; private set; }
        public static int FightKamasLevel50Max { get; private set; }
        public static int FightKamasLevel100Min { get; private set; }
        public static int FightKamasLevel100Max { get; private set; }
        public static int FightKamasLevel150Min { get; private set; }
        public static int FightKamasLevel150Max { get; private set; }
        public static int FightKamasLevel190Min { get; private set; }
        public static int FightKamasLevel190Max { get; private set; }

        public static long ApplyXp(long baseAmount)
        {
            return Apply(baseAmount, Xp);
        }

        public static int ApplyKamas(int baseAmount)
        {
            return (int)Math.Max(0, Math.Min(int.MaxValue, Apply(baseAmount, Kamas)));
        }

        public static int RollFightKamas(int level)
        {
            int min;
            int max;

            if (level >= 190)
            {
                min = FightKamasLevel190Min;
                max = FightKamasLevel190Max;
            }
            else if (level >= 150)
            {
                min = FightKamasLevel150Min;
                max = FightKamasLevel150Max;
            }
            else if (level >= 100)
            {
                min = FightKamasLevel100Min;
                max = FightKamasLevel100Max;
            }
            else if (level >= 50)
            {
                min = FightKamasLevel50Min;
                max = FightKamasLevel50Max;
            }
            else
            {
                min = FightKamasLevel1Min;
                max = FightKamasLevel1Max;
            }

            if (max < min)
                max = min;

            var rolled = _random.Next(min, max + 1);
            return ApplyKamas(rolled);
        }

        public static long ApplyJobXp(long baseAmount)
        {
            return Apply(baseAmount, JobXp);
        }

        public static long ApplyMountXp(long baseAmount)
        {
            return Apply(baseAmount, MountXp);
        }

        public static double ApplyDrop(double basePercent)
        {
            return Math.Max(0d, basePercent * Drop);
        }

        private static long Apply(long baseAmount, double rate)
        {
            if (baseAmount <= 0)
                return 0;

            return Math.Max(0L, (long)Math.Round(baseAmount * rate, MidpointRounding.AwayFromZero));
        }
    }
}
