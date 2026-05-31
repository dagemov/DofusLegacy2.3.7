using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Monsters;
using Sunshine.Protocol.Utils;
using Sunshine.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Maps.Pathfinding;
using Sunshine.WorldServer.Handlers.Context;

namespace Sunshine.WorldServer.Game.Actors.Monsters
{
    public class MonsterGroup : RolePlayActor
    {
        private readonly List<int> _spawnMonsterTemplateIds;

        public MonsterGroup(List<Monster> monsters, Map map, bool isDungeon = false, int count = 0)
        {
            Id = ActorManager.Instance.GenerateId(true);
            Monsters = new List<Monster>();
            Map = map;
            PreserveSpawnComposition = isDungeon;
            RequestedGroupCount = count;
            _spawnMonsterTemplateIds = monsters != null
                ? monsters.Where(x => x != null && x.Record != null).Select(x => x.Record.Id).ToList()
                : new List<int>();
            GenerateGroup(monsters ?? new List<Monster>(), isDungeon, count);
            GenerateLeader();
            GenerateDisposition();
        }

        public override int Id { get; }

        public Monster Leader { get; set; }

        public List<Monster> Monsters { get; set; }

        public Map Map { get; set; }

        public bool IsEngaged { get; set; }

        public bool PreserveSpawnComposition { get; }

        public int RequestedGroupCount { get; }

        public bool CanRespawn { get; set; } = true;

        public short Cell { get; set; }

        public DirectionsEnum Direction { get; set; }

        public short? RespawnCellOverride { get; private set; }

        public DirectionsEnum? RespawnDirectionOverride { get; private set; }

        public IReadOnlyList<int> SpawnMonsterTemplateIds => _spawnMonsterTemplateIds;

        public EntityDispositionInformations GetEntityDisposition()
        {
            return new EntityDispositionInformations(Cell, (sbyte)Direction);
        }

        public const short ForcedStarBonus = 200;

        public override GameRolePlayActorInformations GetGameRolePlayActorInformations()
        {
            return new GameRolePlayGroupMonsterInformations(Id, Leader.Look.GetEntityLook(), GetEntityDisposition(), Leader.Record.Id, (sbyte)Leader.Grade.GradeId, Monsters.Where(x => x.Id != Leader.Id).Select(x => x.GetMonsterInGroupInformations), ForcedStarBonus, 0);
        }

        public void SetFixedRespawnLocation(short cell, DirectionsEnum direction)
        {
            Cell = cell;
            Direction = direction;
            RespawnCellOverride = cell;
            RespawnDirectionOverride = direction;
        }

        public MonsterGroup CreateRespawnGroup(Map map = null)
        {
            var targetMap = map ?? Map;
            if (targetMap == null || _spawnMonsterTemplateIds.Count == 0)
                return null;

            var monsters = new List<Monster>();

            foreach (var monsterId in _spawnMonsterTemplateIds)
            {
                var template = MonsterManager.Instance.GetMonster(monsterId);
                var grades = MonsterManager.Instance.GetMonsterGrades(monsterId);
                if (template == null || grades == null || grades.Length == 0)
                    continue;

                monsters.Add(new Monster(template, grades));
            }

            if (monsters.Count == 0)
                return null;

            var group = new MonsterGroup(monsters, targetMap, PreserveSpawnComposition, RequestedGroupCount)
            {
                CanRespawn = CanRespawn
            };

            if (RespawnCellOverride.HasValue)
                group.SetFixedRespawnLocation(RespawnCellOverride.Value, RespawnDirectionOverride ?? DirectionsEnum.DIRECTION_SOUTH_EAST);

            return group;
        }

        private void GenerateLeader()
        {
            AsyncRandom rdn = new AsyncRandom();
            Leader = Monsters[rdn.Next(0, Monsters.Count)];
        }

        private void GenerateGroup(List<Monster> monsters, bool isDungeon, int count)
        {
            if (isDungeon)
            {
                foreach (var monster in monsters)
                {
                    var newMonster = monster.Clone();
                    newMonster.Id = ActorManager.Instance.GenerateId(true);
                    newMonster.GenerateGrade();
                    newMonster.Stats = new Stats.StatsFields(newMonster);
                    Monsters.Add(newMonster);
                }
            }
            else
            {
                AsyncRandom rdn = new AsyncRandom();
                count = count != 0 ? count : rdn.Next(1, 9);
                for (int i = 0; i < count; i++)
                {
                    Monster monster = monsters[rdn.Next(0, monsters.Count)].Clone();
                    monster.Id = ActorManager.Instance.GenerateId(true, i == 0);
                    monster.GenerateGrade();
                    monster.Stats = new Stats.StatsFields(monster);
                    Monsters.Add(monster);
                }
            }
        }

        public override void StartMove(Path path)
        {
            if (Leader.Record.Id == 2785 || Leader.Record.Id == 494 || Leader.Record.Id == 2781)
                return;

            Cell = path.EndCell;
            Direction = path.GetEndCellDirection();
            ContextHandler.SendGameMapMovementMessage(Map.Clients, Id, path.GetServerPathKeys());
        }

        private void GenerateDisposition()
        {
            AsyncRandom rdn = new AsyncRandom();
            var cells = Map.Cells.Where(x => x.Walkable && Map.RolePlayActors.FirstOrDefault(w => w.GetGameRolePlayActorInformations().disposition.cellId == x.Id) == null).ToList();
            Cell = cells[rdn.Next(0, cells.Count)].Id;
            Direction = (DirectionsEnum)new AsyncRandom().Next(0, 8);
            Leader.GetMonsterGroup = this;
        }
    }
}
