using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using Dapper.Contrib.Extensions;
using System.Threading.Tasks;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World;
using Sunshine.MySql.Database.World.Breeds;
using Sunshine.MySql.Database.World.Characters.Shortcuts;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Utils;

namespace Sunshine.MySql.Database.Managers
{
    public class BreedManager : Singleton<BreedManager>
    {
        public Dictionary<int, List<Tuple<byte, int, int, int>>> BreedStats = new Dictionary<int, List<Tuple<byte, int, int, int>>>();

        public Dictionary<int, Protocol.Tools.D2o.Classes.Breed> BreedColors = new Dictionary<int, Protocol.Tools.D2o.Classes.Breed>();

        public Dictionary<int, List<BreedSpell>> BreedSpells = new Dictionary<int, List<BreedSpell>>();

        public string GetLook(int breedId, bool sex)
        {
            if (!sex)
                return DatabaseManager.Connection.QueryFirstOrDefault<string>($"SELECT MaleLook FROM breeds WHERE Id = '{breedId}'");
            else
                return DatabaseManager.Connection.QueryFirstOrDefault<string>($"SELECT FemaleLook FROM breeds WHERE Id = '{breedId}'");
        }

        public int GetStartMap(int breedId)
        {
            return DatabaseManager.Connection.QueryFirstOrDefault<int>($"SELECT StartMap FROM breeds WHERE Id = '{breedId}'");
        }

        public short GetStartCell(int breedId)
        {
            return DatabaseManager.Connection.QueryFirstOrDefault<short>($"SELECT StartCell FROM breeds WHERE Id = '{breedId}'");
        }

        public int GetStartDirection(int breedId)
        {
            return DatabaseManager.Connection.QueryFirstOrDefault<int>($"SELECT StartDirection FROM breeds WHERE Id = '{breedId}'");
        }

        public Dictionary<int, List<BreedSpell>> GetSpellsByBreed()
        {
            var spells = DatabaseManager.Connection.Query<BreedSpell>($"SELECT * FROM breeds_spells");
            Dictionary<int, List<BreedSpell>> breedSpells = new Dictionary<int, List<BreedSpell>>();
            foreach(var spell in spells)
            {
                if (breedSpells.ContainsKey(spell.Breed))
                    breedSpells[spell.Breed].Add(spell);
                else
                    breedSpells.Add(spell.Breed, new List<BreedSpell> { spell });
            }
            return breedSpells;
        }

        public IEnumerable<BreedSpell> GetSpells(int breedId)
        {
            return BreedSpells[breedId];
        }  

        public List<short> GetSpellIds(int breedId, int level)
        {
            return DatabaseManager.Connection.Query<short>($"SELECT Spell FROM breeds_spells WHERE Breed = '{breedId}' && ObtainLevel <= '{level}'").ToList();
        }

        public IEnumerable<SpellShortcut> GetSpellShortcuts(int characterId, int breedId)
        {
            var spells = GetSpellIds(breedId, 1);
            List<SpellShortcut> spellShortcuts = new List<SpellShortcut>();
            for (int i = 0; i < spells.Count; i++)
                spellShortcuts.Add(new SpellShortcut { OwnerId = characterId, Spell = spells[i], Slot = i});

            return spellShortcuts;
        }

        public string GetStatsFormulas(int breedId, StatsBoostTypeEnum statsBoost)
        {
            switch (statsBoost)
            {
                case StatsBoostTypeEnum.Agility:
                    return DatabaseManager.Connection.QueryFirstOrDefault<string>($"SELECT StatsPointsForAgilityCSV FROM breeds WHERE Id = '{breedId}'");

                case StatsBoostTypeEnum.Chance:
                    return DatabaseManager.Connection.QueryFirstOrDefault<string>($"SELECT StatsPointsForChanceCSV FROM breeds WHERE Id = '{breedId}'");

                case StatsBoostTypeEnum.Intelligence:
                    return DatabaseManager.Connection.QueryFirstOrDefault<string>($"SELECT StatsPointsForIntelligenceCSV FROM breeds WHERE Id = '{breedId}'");

                case StatsBoostTypeEnum.Strength:
                    return DatabaseManager.Connection.QueryFirstOrDefault<string>($"SELECT StatsPointsForStrengthCSV FROM breeds WHERE Id = '{breedId}'");

                default:
                    return null;
            }
        }

        public int SetStatsPoints(int boostPoint, int breedId, StatsBoostTypeEnum statsBoost, short currentPoint)
        {
            switch(statsBoost)
            {
                case StatsBoostTypeEnum.Vitality:
                    return breedId == 11 ? boostPoint * 2 : boostPoint;
                case StatsBoostTypeEnum.Wisdom:
                    return boostPoint / 3;
                default:
                    {
                        double nombrePtsBoost = 0;
                        var breedStats = BreedStats[breedId].Where(x => x.Item1 == (byte)statsBoost).OrderBy(el => el.Item3).Where(el => el.Item3 >= currentPoint);
                        foreach (var stats in breedStats)
                        {
                            nombrePtsBoost += (double)(((stats.Item4 - stats.Item3) * stats.Item2)) > boostPoint ? (double)(boostPoint / stats.Item2) : (double)((stats.Item4 - stats.Item3));
                            boostPoint -= (stats.Item4 - stats.Item3) * stats.Item2 > boostPoint ? boostPoint : (stats.Item4 - stats.Item3) * stats.Item2;
                        }
                        return (int)nombrePtsBoost;
                    }
            }                       
        }
    }
}
