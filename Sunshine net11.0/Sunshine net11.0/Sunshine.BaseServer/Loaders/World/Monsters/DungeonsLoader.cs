using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Monsters;
using Sunshine.WorldServer.Game.Actors.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.BaseServer.Loaders.World.Monsters
{
    public static class DungeonsLoader
    {
        public static void Initialize()
        {
            var maps = MapManager.Instance.Maps.Where(x => x.Value.IsDungeon() && x.Value.Cells != null
                                                                             && x.Value.Cells.Count() > 0);

            foreach (var map in maps)
            {
                if (map.Value.Dungeon == null || map.Value.Dungeon.Monsters == null)
                    continue;

                var monsterIds = map.Value.Dungeon.Monsters
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();

                if (monsterIds.Count <= 0)
                    continue;

                var monsters = new List<Monster>();
                foreach (var monsterId in monsterIds)
                {
                    MonsterTemplate template;
                    List<MonsterGrade> grades;

                    if (!MonsterManager.Instance.Monsters.TryGetValue(monsterId, out template) || template == null)
                        continue;

                    if (!MonsterManager.Instance.MonstersGrade.TryGetValue(monsterId, out grades) || grades == null || grades.Count <= 0)
                        continue;

                    monsters.Add(new Monster(template, grades.ToArray()));
                }

                if (monsters.Count > 0)
                    map.Value.EnterActor(new MonsterGroup(monsters, map.Value, true));
            }
        }
    }
}
