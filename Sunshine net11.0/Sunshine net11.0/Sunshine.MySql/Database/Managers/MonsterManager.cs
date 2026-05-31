using Sunshine.MySql.Database.World.Monsters;
using System;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using Sunshine.Mysql.Database;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Spells;

namespace Sunshine.MySql.Database.Managers
{
    public class MonsterManager : Singleton<MonsterManager>
    {
        public Dictionary<int, MonsterTemplate> Monsters = new Dictionary<int, MonsterTemplate>();

        public Dictionary<int, List<MonsterGrade>> MonstersGrade = new Dictionary<int, List<MonsterGrade>>();

        public Dictionary<int, List<Spell>> MonsterSpells = new Dictionary<int, List<Spell>>(); // By Monster Id

        public Dictionary<int, List<MonsterDrop>> MonsterDropsCache = new Dictionary<int, List<MonsterDrop>>();

        public IEnumerable<MonsterTemplate> GetAllMonsters()
        {
            return DatabaseManager.Connection.Query<MonsterTemplate>("SELECT * FROM monsters");
        }

        public Dictionary<int, List<MonsterGrade>> GetAllMonstersGrade()
        {
            var mgrades = DatabaseManager.Connection.Query<MonsterGrade>("SELECT * FROM monsters_grades");

            Dictionary<int, List<MonsterGrade>> monsters = new Dictionary<int, List<MonsterGrade>>();

            foreach (var mgrade in mgrades)
            {
                if (monsters.ContainsKey(mgrade.MonsterId))
                    monsters[mgrade.MonsterId].Add(mgrade);
                else
                    monsters.Add(mgrade.MonsterId, new List<MonsterGrade> { mgrade });
            }

            return monsters;
        }

        public IEnumerable<MonsterDrop> GetMonsterDrops(int monsterId)
        {
            if (MonsterDropsCache.TryGetValue(monsterId, out var drops))
                return drops;

            drops = DatabaseManager.Connection.Query<MonsterDrop>("SELECT * FROM monsters_drops WHERE MonsterId = @MonsterId", new { MonsterId = monsterId }).ToList();
            MonsterDropsCache[monsterId] = drops;
            return drops;
        }

        public void PreloadMonsterDrops()
        {
            MonsterDropsCache = DatabaseManager.Connection
                .Query<MonsterDrop>("SELECT * FROM monsters_drops")
                .GroupBy(x => x.MonsterId)
                .ToDictionary(x => x.Key, x => x.ToList());
        }

        public MonsterTemplate GetMonster(int monsterId)
        {
            MonsterTemplate monster;
            if (Monsters.TryGetValue(monsterId, out monster) && monster != null)
                return monster;

            return DatabaseManager.Connection.QueryFirstOrDefault<MonsterTemplate>("SELECT * FROM monsters WHERE Id = @MonsterId", new { MonsterId = monsterId });
        }

        public MonsterGrade[] GetMonsterGrades(int monsterId)
        {
            List<MonsterGrade> grades;
            if (MonstersGrade.TryGetValue(monsterId, out grades) && grades != null)
                return grades.ToArray();

            return DatabaseManager.Connection.Query<MonsterGrade>("SELECT * FROM monsters_grades WHERE MonsterId = @MonsterId", new { MonsterId = monsterId }).ToArray();
        }

        public IEnumerable<MonsterSpawn> GetMonsterSpawns()
        {
            return DatabaseManager.Connection.Query<MonsterSpawn>("SELECT * FROM worlds_monsters");
        }

        public IEnumerable<MonsterSpawnFix> GetMonsterSpawnsFix()
        {
            return DatabaseManager.Connection.Query<MonsterSpawnFix>("SELECT * FROM worlds_monsters_fix");
        }

        public Dictionary<int, List<Spell>> GetMonsterSpells()
        {
            var records = DatabaseManager.Connection.Query<MonsterSpell>("SELECT * FROM monsters_spells").ToArray();
            var result = new Dictionary<int, List<Spell>>();

            foreach (var record in records)
            {
                if (record == null)
                    continue;

                var spells = new List<Spell>();
                var ids = (record.SpellsCSV ?? string.Empty)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => int.TryParse(x, out _))
                    .Select(int.Parse)
                    .Distinct()
                    .ToArray();

                foreach (var spellId in ids)
                {
                    if (!SpellManager.Instance.Spells.ContainsKey(spellId))
                        continue;

                    var levels = SpellManager.Instance.Spells[spellId];
                    if (levels == null || levels.Count == 0)
                        continue;

                    var spell = levels[levels.Count - 1];
                    if (spell != null)
                        spells.Add(spell);
                }

                result[record.Monster] = spells;
            }

            return result;
        }
    }
}
