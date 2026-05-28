using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World;
using Sunshine.MySql.Database.World.Maps;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Tools.D2p;
using Sunshine.Protocol.Tools.Dlm;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Actors.TaxCollectors;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Maps.Dungeons;
using Sunshine.WorldServer.Game.Maps.Interactives;
using Sunshine.WorldServer.Game.Maps.Prisms;
using Sunshine.WorldServer.Game.Maps.Triggers;
using Sunshine.WorldServer.Handlers;
using Sunshine.WorldServer.Handlers.Context;
using Sunshine.WorldServer.Handlers.Context.Roleplay;
using Sunshine.BaseServer.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Maps
{
    public class Map
    {
        public MapRecord Record { get; set; }

        public DlmCellData[] Cells { get; set; }

        public MapCellsInformationProvider CellsInfoProvider { get; set; }

        public List<WorldClient> Clients;

        public List<RolePlayActor> RolePlayActors { get; set; }

        public List<short> BlueCells { get; set; }

        public List<short> RedCells { get; set; }

        public List<Element> Elements { get; set; }

        public List<Interactive> Interactives { get; set; }

        public List<Trigger> Triggers { get; set; }

        public Dungeon Dungeon { get; set; }

        public List<Fight> Fights { get; set; }

        public MapPoint Point { get; set; }

        public Point Position { get; set; }

        public bool RuntimeInstance { get; set; }

        public int CanonicalMapId { get; set; }

        public bool AllowChallenge { get { return BlueCells != null && RedCells != null && !(BlueCells.Count <= 0 && RedCells.Count <= 0); } }

        public Map(MapRecord record)
        {
            Record = record;
            Cells = new DlmCellData[560];
            CellsInfoProvider = new MapCellsInformationProvider(this); 
            Clients = new List<WorldClient>();
            RolePlayActors = new List<RolePlayActor>();
            BlueCells = new List<short>();
            RedCells = new List<short>();
            Elements = new List<Element>();
            Interactives = new List<Interactive>();
            Triggers = new List<Trigger>();
            Fights = new List<Fight>();
            CanonicalMapId = record != null ? record.Id : 0;
        }

        public int Id
            => Record.Id;

        public int SubAreaId
            => Record.SubAreaId;

        public int LeftNeighbourId
            => Record.LeftNeighbourId;

        public int RightNeighbourId
            => Record.RightNeighbourId;

        public int TopNeighbourId
            => Record.TopNeighbourId;

        public int BottomNeighbourId
            => Record.BottomNeighbourId;

        public bool IsDungeon() { return Dungeon != null; }

        public bool HasDungeonMonsterGroup()
        {
            return RolePlayActors != null && RolePlayActors.Any(x => x is MonsterGroup);
        }

        public bool EnsureDungeonMonsterGroup()
        {
            if (!IsDungeon() || Dungeon == null || Dungeon.Monsters == null || !Dungeon.Monsters.Any())
                return false;

            if (HasDungeonMonsterGroup())
                return false;

            var monsters = Dungeon.Monsters
                .Select(monsterId =>
                {
                    var template = MonsterManager.Instance.GetMonster(monsterId);
                    return template != null ? new Monster(template, MonsterManager.Instance.GetMonsterGrades(monsterId)) : null;
                })
                .Where(x => x != null)
                .ToList();

            if (monsters.Count == 0)
                return false;

            EnterActor(new MonsterGroup(monsters, this, true));
            return true;
        }

        public void ScheduleMonsterGroupRespawn(MonsterGroup defeatedGroup)
        {
            if (defeatedGroup == null || !defeatedGroup.CanRespawn)
                return;

            var targetMap = defeatedGroup.Map ?? this;
            int delayMs = Math.Max(1000, GameConfig.GetInt("MonsterRespawnDelayMs", 15000));

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delayMs);

                    var respawnedGroup = defeatedGroup.CreateRespawnGroup(targetMap);
                    if (respawnedGroup == null)
                        return;

                    targetMap.EnterActor(respawnedGroup);
                }
                catch
                {
                }
            });
        }


        public bool IsInstanceTemplate() { return Record != null && Record.IsInstance; }

        public bool IsInstance() { return IsInstanceTemplate() || RuntimeInstance; } 

        public bool IsCanonicalMap() { return object.ReferenceEquals(MapManager.Instance.GetMap(Id), this); }
        
        public bool IsBug() { return Record.IsBug; }

        public string ParametersCSV { get { return Record.ParametersCSV; } }

        public DlmCellData GetCell(short id)
        {
            return Cells[id];
        }

        public void EnterActor(RolePlayActor actor)
        {
            if (actor == null)
                return;

            lock (actor)
            {
                if (RolePlayActors.Contains(actor))
                    return;

                var staleSameIdActors = RolePlayActors
                    .Where(x => x != actor && x != null && x.Id == actor.Id)
                    .ToList();

                foreach (var staleActor in staleSameIdActors)
                    LeaveActor(staleActor);

                if (actor is PrismActor prismActor)
                {
                    var duplicates = RolePlayActors
                        .OfType<PrismActor>()
                        .Where(x => x != prismActor && x.Record != null && prismActor.Record != null &&
                                   (x.Record.Id == prismActor.Record.Id || x.Record.SubAreaId == prismActor.Record.SubAreaId))
                        .ToList();

                    foreach (var duplicate in duplicates)
                        LeaveActor(duplicate);
                }

                RolePlayActors.Add(actor);

                if (actor is Character)
                {
                    if (!PrismManager.Instance.IsSyncingMap(this))
                        PrismManager.Instance.EnsureMapPrismState(this);

                    var client = (actor as Character).Client;
                    if (client == null)
                        return;

                    if (!Clients.Contains(client))
                        Clients.Add(client);

                    if (Interactives.Any(x => x.Type == 16) && !client.Character.HasZaap(Id))
                        client.Character.DiscoverZaap(this);

                    foreach (var actorClient in Clients)
                    {
                        ContextRoleplayHandler.SendGameRolePlayShowActorMessage(client, actorClient.Character);
                        if (actorClient != client)
                            ContextRoleplayHandler.SendGameRolePlayShowActorMessage(actorClient, actor);
                    }
                }
                else
                {
                    for (int i = 0; i < Clients.Count; i++)
                        ContextRoleplayHandler.SendGameRolePlayShowActorMessage(Clients[i], actor);
                }
            }
        }

        public void LeaveActor(RolePlayActor actor)
        {
            if (actor == null)
                return;

            lock (actor)
            {
                if (!RolePlayActors.Contains(actor))
                    return;

                RolePlayActors.Remove(actor);

                if (actor is Character)
                {
                    if (!PrismManager.Instance.IsSyncingMap(this))
                        PrismManager.Instance.EnsureMapPrismState(this);

                    var client = (actor as Character).Client;
                    if (client != null)
                        Clients.Remove(client);
                }

                for (int i = 0; i < Clients.Count; i++)
                    ContextHandler.SendGameContextRemoveElementMessage(Clients[i], actor);
            }
        }

        public void Refresh(RolePlayActor actor, bool isRemoved = false)
        {
            lock (actor)
            {
                if (isRemoved)
                {
                    for (int i = 0; i < Clients.Count; i++)
                        ContextHandler.SendGameContextRemoveElementMessage(Clients[i], actor);
                }
                else
                {
                    for (int i = 0; i < Clients.Count; i++)
                        ContextRoleplayHandler.SendGameRolePlayShowActorMessage(Clients[i], actor);
                }
            }
        }

        public RolePlayActor GetActor(int id)
        {
            return RolePlayActors.FirstOrDefault(x => x.Id == id);
        }

        public bool HasTaxCollector()
        {
            return RolePlayActors.FirstOrDefault(x => x is TaxCollector) != null;
        }

        public bool EnsureFightCells()
        {
            BlueCells = BlueCells ?? new List<short>();
            RedCells = RedCells ?? new List<short>();

            BlueCells = NormalizePlacementCells(OrderPlacementCellsTowardCenter(BlueCells, true), new HashSet<short>(), 20);

            var reserved = new HashSet<short>(BlueCells);
            RedCells = NormalizePlacementCells(OrderPlacementCellsTowardCenter(RedCells, false), reserved, 20);

            if (BlueCells.Count > 0 && RedCells.Count > 0)
                return true;

            GeneratePatternCells();
            return BlueCells.Count > 0 && RedCells.Count > 0;
        }

        public bool IsValidFightPlacementCell(short cell)
        {
            if (Cells == null || cell < 0 || cell >= Cells.Length)
                return false;

            var cellData = Cells[cell];
            if (!cellData.Walkable || cellData.NonWalkableDuringFight || cellData.NonWalkableDuringRP || cellData.FarmCell)
                return false;

            return !GetBlockedPlacementCells().Contains(cell);
        }

        private HashSet<short> GetBlockedPlacementCells()
        {
            var blockedCells = new HashSet<short>();

            if (Elements == null)
                Elements = new List<Element>();

            if (Interactives == null)
                Interactives = new List<Interactive>();

            foreach (var interactive in Interactives)
            {
                if (interactive == null)
                    continue;

                var element = Elements.FirstOrDefault(x => x != null && x.Id == (uint)interactive.Element);
                if (element != null)
                    blockedCells.Add(element.Cell);

                if (interactive.GetObstacles != null)
                {
                    foreach (var obstacle in interactive.GetObstacles)
                    {
                        if (obstacle != null)
                            blockedCells.Add(obstacle.obstacleCellId);
                    }
                }
            }

            return blockedCells;
        }

        private List<short> NormalizePlacementCells(IEnumerable<short> source, HashSet<short> reserved, int maxCells)
        {
            var normalized = new List<short>();

            if (source == null)
                return normalized;

            foreach (var cell in source.Distinct())
            {
                if (normalized.Count >= maxCells)
                    break;

                if (reserved != null && reserved.Contains(cell))
                    continue;

                if (!IsValidFightPlacementCell(cell))
                    continue;

                normalized.Add(cell);

                if (reserved != null)
                    reserved.Add(cell);
            }

            return normalized;
        }

        private IEnumerable<short> OrderPlacementCellsTowardCenter(IEnumerable<short> source, bool isBlue)
        {
            var cells = source
                .Where(x => x >= 0 && x < 560)
                .Select(x => new { Cell = x, Point = new MapPoint(x) })
                .ToList();

            if (cells.Count == 0)
                return Enumerable.Empty<short>();

            double centerX = cells.Average(x => x.Point.X);
            double centerY = cells.Average(x => x.Point.Y);


            var preferred = cells
                .Where(x => isBlue ? x.Point.X <= centerX : x.Point.X >= centerX)
                .OrderBy(x => Math.Abs(x.Point.X - centerX))
                .ThenBy(x => Math.Abs(x.Point.Y - centerY))
                .ThenBy(x => isBlue ? x.Point.X : -x.Point.X)
                .ThenBy(x => x.Point.Y)
                .ThenBy(x => x.Cell)
                .Select(x => x.Cell);

            var fallback = cells
                .Where(x => isBlue ? x.Point.X > centerX : x.Point.X < centerX)
                .OrderBy(x => Math.Abs(x.Point.X - centerX))
                .ThenBy(x => Math.Abs(x.Point.Y - centerY))
                .ThenBy(x => isBlue ? x.Point.X : -x.Point.X)
                .ThenBy(x => x.Point.Y)
                .ThenBy(x => x.Cell)
                .Select(x => x.Cell);

            return preferred.Concat(fallback);
        }

        private List<short> GetFlagPlacementCells(bool isBlue, HashSet<short> reserved, int maxCells)
        {
            if (Cells == null || Cells.Length == 0)
                return new List<short>();

            var flaggedCells = new List<short>();

            for (short i = 0; i < Cells.Length; i++)
            {
                var cell = Cells[i];
                if ((isBlue ? cell.Blue : cell.Red) && IsValidFightPlacementCell(i))
                    flaggedCells.Add(i);
            }

            return NormalizePlacementCells(OrderPlacementCellsTowardCenter(flaggedCells, isBlue), reserved, maxCells);
        }

        private List<short> GenerateAutomaticPlacementCells(bool isBlue, HashSet<short> reserved, int maxCells)
        {
            if (Cells == null || Cells.Length == 0)
                return new List<short>();

            var validCells = new List<short>();
            for (short i = 0; i < Cells.Length; i++)
            {
                if ((reserved == null || !reserved.Contains(i)) && IsValidFightPlacementCell(i))
                    validCells.Add(i);
            }

            if (validCells.Count == 0)
                return new List<short>();

            var orderedCells = OrderPlacementCellsTowardCenter(validCells, isBlue).ToList();
            return NormalizePlacementCells(orderedCells, reserved, maxCells);
        }

        public void GeneratePatternCells()
        {
            BlueCells = new List<short>();
            RedCells = new List<short>();

            if (Cells == null || Cells.Length <= 0)
                return;

            var reserved = new HashSet<short>();

            var explicitBlueCells = NormalizePlacementCells(MapManager.Instance.GetPatternCells(this, true), reserved, 20);
            var explicitRedCells = NormalizePlacementCells(MapManager.Instance.GetPatternCells(this, false), reserved, 20);

            if (explicitBlueCells.Count > 0 && explicitRedCells.Count > 0)
            {
                BlueCells = explicitBlueCells;
                RedCells = explicitRedCells;
                return;
            }

            reserved.Clear();

            var flaggedBlueCells = GetFlagPlacementCells(true, reserved, 20);
            var flaggedRedCells = GetFlagPlacementCells(false, reserved, 20);

            if (flaggedBlueCells.Count > 0 && flaggedRedCells.Count > 0)
            {
                BlueCells = flaggedBlueCells;
                RedCells = flaggedRedCells;
                return;
            }

            reserved.Clear();

            BlueCells = GenerateAutomaticPlacementCells(true, reserved, 20);
            RedCells = GenerateAutomaticPlacementCells(false, reserved, 20);
        }

        public Map Clone()
        {
            return (Map)this.MemberwiseClone();
        }
    }
}
