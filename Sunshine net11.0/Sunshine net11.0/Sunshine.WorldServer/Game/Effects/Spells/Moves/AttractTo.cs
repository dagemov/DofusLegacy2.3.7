using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Bombs;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Fights.Mechanics;
using System;

namespace Sunshine.WorldServer.Game.Effects.Spells.Moves
{
    [EffectHandler(EffectsEnum.Effect_1043)]
    public class AttractTo : SpellEffectHandler
    {
        public override void Apply()
        {
            if (Caster?.Fight == null || Caster.Position?.Point == null)
                return;

            MapPoint targetedPoint;
            try
            {
                targetedPoint = new MapPoint(TargetedCell);
            }
            catch
            {
                return;
            }

            var castPoint = Caster.Position.Point;
            var direction = castPoint.OrientationTo(targetedPoint, true);
            int searchDistance = Math.Max(1, Spell?.Template?.Range ?? 0);
            searchDistance = Math.Max(searchDistance, (int)castPoint.DistanceToCell(targetedPoint));

            FightActor target = null;
            for (int i = 1; i <= searchDistance; i++)
            {
                var cell = castPoint.GetCellInDirection(direction, (short)i);
                if (cell == null)
                    break;

                var fighter = Fight.GetOneFighter(cell.CellId);
                if (fighter == null || !fighter.IsAlive)
                    continue;

                target = fighter;
                break;
            }

            if (target == null)
                return;

            if (target.HasState(SpellStatesEnum.Gravity) || target.HasState(SpellStatesEnum.Rooted) || target.HasState(SpellStatesEnum.Unmovable))
                return;

            int pullDistance = (int)(Effect?.DiceNum > 0 ? Effect.DiceNum : 0);
            if (pullDistance <= 0)
                pullDistance = Math.Max(1, Spell?.Template?.Range ?? 1);

            var start = target.Position.Point;
            var current = start;
            var pullDirection = target.Position.Point.OrientationTo(castPoint, false);

            for (int i = 0; i < pullDistance; i++)
            {
                var next = current.GetNearestCellInDirection(pullDirection);
                if (next == null)
                    break;

                if (Fight.ShouldTriggerOnMove(next.CellId, target))
                {
                    current = next;
                    break;
                }

                if (!Fight.IsCellFree(next.CellId))
                    break;

                current = next;
            }

            if (start.CellId == current.CellId)
                return;

            ForcedMovementHelper.DropCarriedActorIfNeeded(target);
            var slideClients = ForcedMovementHelper.SendSlide(Caster, target, start.CellId, current.CellId);

            target.Position.Cell = current.CellId;
            target.Position.Direction = pullDirection;
            FrigostBossMechanics.OnForcedMove(Caster, target, start.CellId, current.CellId, false, false);
            ForcedMovementHelper.RefreshAfterForcedMove(Caster, target, slideClients);

            if (target is BombFighter movedBomb)
                BombManager.Instance.CheckWalls(Fight, movedBomb.Summoner);

            Fight.TriggerMarks(target.Position.Cell, target, TriggerTypeEnum.MOVE);
            Fight.CheckFightEnd();
        }
    }
}
