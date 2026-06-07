using Sunshine.MySql.Database.World.Monsters;
using Sunshine.BaseServer.Configuration;
using Sunshine.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Fights.Results
{
    public static class FightFormulas
    {
        private static Dictionary<byte, double> GroupCoefficients = new Dictionary<byte, double>()
        {
            {0, 1.0 }, {1, 1.0 }, {2, 1.1 }, {3, 1.3 },
            {4, 1.5 }, {5, 1.7 }, {6, 1.9 }, {7, 2.1 },
            {8, 2.3 }
        };

        // Instancia compartida para evitar colisiones de semilla
        private static readonly AsyncRandom _rdn = new AsyncRandom();

        public static long CalculateWinExp(int totalWisdom, int bonusChall, int ageBonus, int levelMonsters,
            int levelMembers, long totalExp, byte membersCount)
        {
            double wisdomBonus = 1 + (totalWisdom / 100.0);
            double groupBonus = GroupCoefficients[membersCount];

            double exp =
                wisdomBonus *
                groupBonus *
                (totalExp / (double)membersCount);

            return (long)exp;
        }

        public static int CalculateWinItems(MonsterDrop drop, byte gradeId, int prospection, bool isVip = false)
        {
            double basePercent = GameRates.ApplyDrop(GetDropPercent(gradeId, drop));

            double prospectionMultiplier = prospection / 100.0;
            double realChance = Math.Min(100.0, basePercent * prospectionMultiplier);

            if (isVip)
                realChance = Math.Min(100.0, realChance * 2.0);

            int roll = _rdn.Next(0, 10000);
            int threshold = (int)(realChance * 100.0);

            if (roll < threshold)
            {
                return Math.Max(1, (int)Math.Round(GameRates.DropQuantityMultiplier));
            }

            return 0;
        }

        private static double GetMultiplicator(int levelMonsters, int levelMembers)
        {
            // FIX: comparación correcta entre niveles de monstruos y personajes
            double maxLevel = levelMembers * 1.06;
            double minLevel = levelMembers / 1.11;

            if (levelMonsters >= minLevel && levelMonsters <= maxLevel)
                return 1;
            else
                return levelMonsters / Math.Max(1, levelMembers);
        }

        private static double GetDropPercent(byte gradeId, MonsterDrop drop)
        {
            switch (gradeId)
            {
                case 1:
                    return drop.DropRateForGrade1;
                case 2:
                    return drop.DropRateForGrade2;
                case 3:
                    return drop.DropRateForGrade3;
                case 4:
                    return drop.DropRateForGrade4;
                case 5:
                    return drop.DropRateForGrade5;
                default:
                    return 1;
            }
        }
    }
}
