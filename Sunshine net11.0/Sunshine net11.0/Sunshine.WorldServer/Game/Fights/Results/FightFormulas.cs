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
            {0, 0.5 }, {1, 1.0 }, {2, 1.1 }, {3, 1.5 },
            {4, 2.3 }, {5, 3.1 }, {6, 3.6 }, {7, 4.2 },
            {8, 4.7 }
        };

        public static long CalculateWinExp(int totalWisdom, int bonusChall, int ageBonus, int levelMonsters, 
            int levelMembers, long totalExp, byte membersCount)
        {
            return (long)((1 + ((totalWisdom + (ageBonus * 20) + bonusChall) / 100)) * (GroupCoefficients[membersCount] + 
                          GetMultiplicator(levelMonsters, levelMembers)) * (totalExp / membersCount));
        }

        public static int CalculateWinItems(MonsterDrop drop, byte gradeId, int prospection)
        {
            double percent = GameRates.ApplyDrop(GetDropPercent(gradeId, drop));
            double luckTotal = (percent * prospection) / 100;

            AsyncRandom rdn = new AsyncRandom();

            if (rdn.Next(100) < Math.Min(100d, luckTotal))
                return 1;
            else
                return 0;
        }

        private static long GetMultiplicator(int levelGroup, int levelMembers)
        {
            double maxLevel = levelGroup * 1.06;
            double minLevel = levelGroup / 1.11;

            if (levelGroup >= minLevel && levelGroup <= maxLevel)
                return 1;
            else
                return levelGroup / levelMembers;
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
