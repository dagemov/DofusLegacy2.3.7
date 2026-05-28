using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Monsters;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Maps;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.BaseServer.Loaders.World.Monsters
{
    public static class MonstersLoader
    {
        public static void Initialize()
        {
            Logs.Logger.Write("[ World ] Loading MonsterSpawns...");

            var monsterSpawns = MonsterManager.Instance.GetMonsterSpawns().ToList();
            var monsterSpawnsFix = MonsterManager.Instance.GetMonsterSpawnsFix().ToList();

            MonsterManager.Instance.Monsters = MonsterManager.Instance.GetAllMonsters().ToDictionary(x => x.Id);
            MonsterManager.Instance.MonstersGrade = MonsterManager.Instance.GetAllMonstersGrade();
            MonsterManager.Instance.PreloadMonsterDrops();
            MonsterManager.Instance.MonsterSpells = MonsterManager.Instance.GetMonsterSpells();

            var mapsBySubArea = MapManager.Instance.Maps.Values
                .Where(x => x.Cells != null && x.Cells.Length > 0 && !x.IsDungeon() && x.Record.MapType != 1)
                .GroupBy(x => x.SubAreaId)
                .ToDictionary(x => x.Key, x => x.ToList());

            foreach (var spawn in monsterSpawns)
            {
                if (!mapsBySubArea.TryGetValue(spawn.SubArea, out var maps))
                    continue;

                var monsterIds = spawn.MonstersCSV
                    .Split(',')
                    .Select(x =>
                    {
                        int value;
                        return int.TryParse(x, out value) ? value : -1;
                    })
                    .Where(id => id > 0 && MonsterManager.Instance.Monsters.ContainsKey(id) && MonsterManager.Instance.MonstersGrade.ContainsKey(id))
                    .ToArray();

                if (monsterIds.Length == 0)
                    continue;

                foreach (var map in maps)
                {
                    if (map.Record.MapType == 1)
                        continue;

                    for (int i = 0; i < 3; i++)
                    {
                        var monsters = new List<Monster>(monsterIds.Length);
                        foreach (var monsterId in monsterIds)
                            monsters.Add(new Monster(MonsterManager.Instance.Monsters[monsterId], MonsterManager.Instance.MonstersGrade[monsterId].ToArray()));

                        if (monsters.Count > 0)
                            map.EnterActor(new MonsterGroup(monsters, map));
                    }
                }
            }

            foreach (var spawn in monsterSpawnsFix)
            {
                var map = MapManager.Instance.GetMap(spawn.Map);
                if (map == null || map.Record.MapType == 1)
                    continue;

                var monsters = spawn.MonstersCSV.Split(',');
                var cells = spawn.CellsCSV.Split(',');

                for (int i = 0; i < monsters.Length && i < cells.Length; i++)
                {
                    int monsterId = int.Parse(monsters[i]);

                    if (!MonsterManager.Instance.Monsters.ContainsKey(monsterId) || !MonsterManager.Instance.MonstersGrade.ContainsKey(monsterId))
                        continue;

                    var monster = new Monster(MonsterManager.Instance.Monsters[monsterId], MonsterManager.Instance.MonstersGrade[monsterId].ToArray());
                    var monsterGroup = new MonsterGroup(new List<Monster> { monster }, map, true, 1);
                    monsterGroup.SetFixedRespawnLocation(short.Parse(cells[i]), DirectionsEnum.DIRECTION_SOUTH_EAST);
                    map.EnterActor(monsterGroup);
                }
            }

            Logs.Logger.Write("[ World ] MonsterSpawns Loaded");
        }
    }
}