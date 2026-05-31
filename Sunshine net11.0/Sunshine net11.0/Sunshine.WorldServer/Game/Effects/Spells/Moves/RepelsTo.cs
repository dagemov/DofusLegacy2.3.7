using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Bombs;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Fights.Mechanics;

namespace Sunshine.WorldServer.Game.Effects.Spells.Moves
{
    [EffectHandler(EffectsEnum.Effect_RepelsTo)]
    public class RepelsTo : SpellEffectHandler
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
            var firstCell = castPoint.GetNearestCellInDirection(direction);
            if (firstCell == null)
                return;

            var target = Fight.GetOneFighter(firstCell.CellId);
            if (target == null || !target.IsAlive)
                return;

            if (target.HasState(SpellStatesEnum.Gravity) || target.HasState(SpellStatesEnum.Rooted) || target.HasState(SpellStatesEnum.Unmovable))
                return;

            var start = target.Position.Point;
            var end = targetedPoint;
            var cells = start.GetCellsOnLineBetween(targetedPoint);

            for (int i = 0; i < cells.Length; i++)
            {
                var cell = cells[i];
                if (!Fight.IsCellFree(cell.CellId))
                {
                    end = i > 0 ? cells[i - 1] : start;
                    break;
                }

                if (Fight.ShouldTriggerOnMove(cell.CellId, target))
                {
                    end = cell;
                    break;
                }
            }

            if (start.CellId == end.CellId)
                return;

            ForcedMovementHelper.DropCarriedActorIfNeeded(target);
            var slideClients = ForcedMovementHelper.SendSlide(Caster, target, start.CellId, end.CellId);

            target.Position.Cell = end.CellId;
            target.Position.Direction = direction;
            FrigostBossMechanics.OnForcedMove(Caster, target, start.CellId, end.CellId, true, false);
            ForcedMovementHelper.RefreshAfterForcedMove(Caster, target, slideClients);

            if (target is BombFighter movedBomb)
                BombManager.Instance.CheckWalls(Fight, movedBomb.Summoner);

            Fight.TriggerMarks(target.Position.Cell, target, TriggerTypeEnum.MOVE);
            Fight.CheckFightEnd();
        }
    }
}
