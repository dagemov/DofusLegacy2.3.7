using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Sunshine.WorldServer.Game.Fights.Triggers
{
    public sealed class BombWallBinding
    {
        public event Action<BombWallBinding> Removed;

        private readonly Color _color;
        private readonly List<BombWall> _walls = new List<BombWall>();
        private readonly List<BombWallBinding> _intersections = new List<BombWallBinding>();

        public BombWallBinding(BombFighter bomb1, BombFighter bomb2, Color color)
        {
            Bomb1 = bomb1;
            Bomb2 = bomb2;
            _color = color;
        }

        public BombFighter Bomb1 { get; }
        public BombFighter Bomb2 { get; }
        public int Length { get; private set; }

        public bool IsValid() => Bomb1 != null && Bomb2 != null && Bomb1.IsAlive && Bomb2.IsAlive && !Bomb1.IsExploded && !Bomb2.IsExploded && Bomb1.IsBoundWith(Bomb2);
        public bool Contains(short cellId)
        {
            if (_walls.Any(x => x != null && x.CenterCell == cellId))
                return true;

            var fight = Bomb1?.Fight;
            var caster = Bomb1?.Summoner ?? Bomb1;
            if (fight == null || caster == null)
                return false;

            return fight.GetTriggers(cellId)
                .OfType<BombWall>()
                .Any(x => x != null && x.Caster == caster && (x.Binding == this || _intersections.Contains(x.Binding)));
        }
        public bool MustBeAdjusted()
        {
            if (Bomb1?.Position == null || Bomb2?.Position == null)
                return false;

            var dist = Bomb1.Position.Point.DistanceToCell(Bomb2.Position.Point);
            if (dist != Length + 1)
                return true;

            var fight = Bomb1.Fight;
            var caster = Bomb1.Summoner ?? Bomb1;
            if (fight == null || caster == null)
                return false;

            var expectedCells = new HashSet<short>(Bomb1.Position.Point
                .GetCellsOnLineBetween(Bomb2.Position.Point)
                .Select(x => x.CellId));

            if (_walls.Any(x => x == null || !expectedCells.Contains(x.CenterCell)))
                return true;

            foreach (var cellId in expectedCells)
            {
                bool hasWall = fight.GetTriggers(cellId)
                    .OfType<BombWall>()
                    .Any(x => x != null && x.Caster == caster && (x.Binding == this || _intersections.Contains(x.Binding)));

                if (!hasWall)
                    return true;
            }

            return false;
        }

        public void AdjustWalls()
        {
            if (Bomb1?.Fight == null || Bomb1.Position == null || Bomb2?.Position == null)
                return;

            var fight = Bomb1.Fight;
            var caster = Bomb1.Summoner ?? Bomb1;
            var dist = Bomb1.Position.Point.DistanceToCell(Bomb2.Position.Point);


            var cells = Bomb1.Position.Point
                .GetCellsOnLineBetween(Bomb2.Position.Point)
                .Select(x => x.CellId)
                .Distinct()
                .ToArray();

            foreach (var wall in _walls.Where(x => x == null || !cells.Contains(x.CenterCell)).ToArray())
            {
                wall?.Remove();
                _walls.Remove(wall);
            }

            var currentIntersections = new HashSet<BombWallBinding>();
            var wallsToRefresh = new HashSet<BombWall>();

            foreach (var cellId in cells)
            {
                var localWall = _walls.FirstOrDefault(x => x != null && x.CenterCell == cellId);
                if (localWall != null)
                    continue;

                var existingWall = fight.GetTriggers(cellId)
                    .OfType<BombWall>()
                    .FirstOrDefault(x => x != null && x.Caster == caster);

                if (existingWall == null)
                {
                    var wall = new BombWall(
                        (short)fight.PopNextTriggerId(),
                        caster,
                        Bomb1.WallSpell,
                        null,
                        cellId,
                        this,
                        _color);

                    fight.AddTrigger(wall);
                    _walls.Add(wall);
                    continue;
                }

                if (existingWall.Binding == this)
                {
                    if (!_walls.Contains(existingWall))
                        _walls.Add(existingWall);

                    continue;
                }

                if (existingWall.Binding != null)
                {
                    currentIntersections.Add(existingWall.Binding);

                    if (!_intersections.Contains(existingWall.Binding))
                    {
                        _intersections.Add(existingWall.Binding);
                        existingWall.Binding.Removed -= OnIntersectionRemoved;
                        existingWall.Binding.Removed += OnIntersectionRemoved;
                    }
                }

                wallsToRefresh.Add(existingWall);
            }

            foreach (var intersection in _intersections.Where(x => x == null || !currentIntersections.Contains(x)).ToArray())
            {
                if (intersection != null)
                    intersection.Removed -= OnIntersectionRemoved;

                _intersections.Remove(intersection);
            }

            var viewers = fight.GetAllFighters().OfType<CharacterFighter>().ToArray();

            foreach (var wall in wallsToRefresh.Where(x => x != null))
            {
                ContextHandler.SendGameActionFightUnmarkCellsMessage(fight.Clients, wall);

                foreach (var viewer in viewers)
                    ContextHandler.SendGameActionFightMarkCellsMessage(viewer.Client, wall, wall.DoesSeeTrigger(viewer));
            }

            var occupiedWalls = _walls
                .Where(x => x != null)
                .Select(x => new { Wall = x, Fighter = fight.GetOneFighter(x.CenterCell) })
                .Where(x => x.Fighter != null && x.Fighter.IsAlive)
                .ToArray();

            if (occupiedWalls.Length > 0)
                fight.StartSequence(SequenceTypeEnum.SEQUENCE_GLYPH_TRAP);

            bool firstOccupiedWallTrigger = true;
            foreach (var entry in occupiedWalls)
            {
                fight.TriggerMarks(entry.Wall.CenterCell, entry.Fighter, TriggerTypeEnum.MOVE, !firstOccupiedWallTrigger);
                firstOccupiedWallTrigger = false;
            }

            if (occupiedWalls.Length > 0)
                fight.EndSequence(SequenceTypeEnum.SEQUENCE_GLYPH_TRAP, ActionsEnum.ACTION_FIGHT_TRIGGER_TRAP);

            Length = dist > 0 ? (int)dist - 1 : 0;
        }
        private void OnIntersectionRemoved(BombWallBinding obj)
        {
            obj.Removed -= OnIntersectionRemoved;
            _intersections.Remove(obj);
            AdjustWalls();
        }

        public void Delete()
        {
            foreach (var wall in _walls.ToArray())
                wall.Remove();

            _walls.Clear();
            Removed?.Invoke(this);
        }
    }

    public class BombWall : MarkTrigger
    {
        public BombWall(short id, FightActor caster, Spell castedSpell, Effect originEffect, short centerCell, BombWallBinding binding, Color color)
            : base(id, caster, castedSpell, originEffect, centerCell, new MarkShape(caster.Fight, centerCell, GameActionMarkCellsTypeEnum.CELLS_CIRCLE, 0, color))
        {
            Binding = binding;
        }

        public BombWallBinding Binding { get; }
        public BombFighter[] Bombs => new[] { Binding.Bomb1, Binding.Bomb2 };

        public override GameActionMarkTypeEnum Type => GameActionMarkTypeEnum.WALL;
        public override TriggerTypeEnum TriggerType => TriggerTypeEnum.MOVE | TriggerTypeEnum.TURN_BEGIN | TriggerTypeEnum.TURN_END;

        public override void Trigger(FightActor trigger, ObjectPosition firstPosition = null, int countPushed = 0)
        {
            if (!IsAffected(trigger))
                return;

            if (Fight != null && Fight.CurrentMarkTriggerEventId > 0 && trigger != null && Caster != null)
            {
                if (trigger.LastBombWallTriggerEventId == Fight.CurrentMarkTriggerEventId &&
                    trigger.LastBombWallTriggerCasterId == Caster.Id)
                    return;

                trigger.LastBombWallTriggerEventId = Fight.CurrentMarkTriggerEventId;
                trigger.LastBombWallTriggerCasterId = Caster.Id;
            }

            NotifyTriggered(trigger, CastedSpell);

            var activeBomb = Bombs.FirstOrDefault(x => x != null && x.IsAlive && !x.IsExploded);
            if (activeBomb == null)
                return;

            var linkedBombs = activeBomb.GetBombsBoundedWith().Where(x => x != null && x.IsAlive && !x.IsExploded).ToArray();
            int comboBonus = linkedBombs.Sum(x => x.Combo);
            double comboMultiplier = 1d + (comboBonus / 100d);

            bool kaboomTarget = activeBomb.IsFriendlyWith(trigger) && trigger.HasState(SpellStatesEnum.Kaboom);
            if (!kaboomTarget)
            {
                var damage = Game.Fights.Bombs.BombManager.CreatePrimaryDamageForTarget(activeBomb, trigger, comboMultiplier, true);
                if (damage != null)
                    trigger.InflictDamage(damage);
            }

            Game.Fights.Bombs.BombManager.Instance.ApplyWallSecondaryEffects(activeBomb, trigger);

            if (Fight != null && Fight.FighterPlaying != trigger && Caster != null && Caster.SpellHistory != null && CastedSpell != null)
                Caster.SpellHistory.RegisterCastedSpell(CastedSpell, trigger);
        }

        public override bool IsAffected(FightActor actor)
        {
            var bomb = Bombs.FirstOrDefault();
            if (actor == null || !actor.IsAlive)
                return false;

            if (bomb == null)
                return true;

            if (actor is BombFighter triggerBomb)
            {
                if (bomb.IsFriendlyWith(triggerBomb))
                    return false;

                if (bomb.Element == triggerBomb.Element)
                    return false;
            }

            return true;
        }

        // SaveKrosmoz/Stump behavior for roublard walls:
        // - the ADD action sent by ContextHandler is ACTION_FIGHT_ADD_TRAP_CASTING_SPELL
        // - but the mark payload itself must keep markType = WALL
        // Sending markType = TRAP makes some 2.3.7 clients render the wall only when it is triggered.
        public override GameActionMark GetGameActionMark() => new GameActionMark(Caster.Id, CastedSpell != null ? CastedSpell.Id : 0, Id, (sbyte)Type, Shapes.Select(entry => entry.GetGameActionMarkedCell()));
        public override GameActionMark GetHiddenGameActionMark() => GetGameActionMark();
        public override bool DoesSeeTrigger(FightActor fighter) => true;
        public override bool DecrementDuration() => false;

        public override void Remove()
        {
            Fight?.RemoveTrigger(this);
        }
    }
}
