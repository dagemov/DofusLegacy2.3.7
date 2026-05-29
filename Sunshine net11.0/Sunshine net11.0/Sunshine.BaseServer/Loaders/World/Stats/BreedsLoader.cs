using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Tools.D2o;
using Sunshine.Protocol.Tools.D2o.Classes;
using System;
using System.Collections.Generic;
using System.IO;

namespace Sunshine.BaseServer.Loaders.World.Stats
{
    public static class BreedsLoader
    {
        public static void Initialize()
        {
            List<Tuple<byte, int, int, int>> stats;

            for (int i = 1; i < 15; i++) // Breed Count
            {
                stats = new List<Tuple<byte, int, int, int>>();
                for (byte y = 10; y < 16; y++) // Stats Count
                {
                    if (y != (byte)StatsBoostTypeEnum.Vitality && y != (byte)StatsBoostTypeEnum.Wisdom) // 0,1|20,2|40,3|60,4|80,5
                    {
                        string[] formulasB = BreedManager.Instance.GetStatsFormulas(i, (StatsBoostTypeEnum)y).Split('|');
                        for (int x = 0; x < formulasB.Length; x++)
                        {
                            string[] formulas = formulasB[x].Split(',');
                            int coeffStats = int.Parse(formulas[1]);
                            int minStats = int.Parse(formulas[0]);
                            int maxStats = 9999;
                            if (x + 1 < formulasB.Length)
                                maxStats = int.Parse(formulasB[x + 1].Split(',')[0]);

                            stats.Add(new Tuple<byte, int, int, int>(y, coeffStats, minStats, maxStats));
                        }
                    }
                }
                BreedManager.Instance.BreedStats.Add(i, stats);
            }

            var breedsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "d2os", "Breeds.d2o");
            D2OReader reader = new D2OReader(breedsPath);
            var breeds = reader.ReadObjects<Breed>();
            BreedManager.Instance.BreedColors = breeds;
            var breedSpells = BreedManager.Instance.GetSpellsByBreed();
            BreedManager.Instance.BreedSpells = breedSpells;
        }
    }
}
