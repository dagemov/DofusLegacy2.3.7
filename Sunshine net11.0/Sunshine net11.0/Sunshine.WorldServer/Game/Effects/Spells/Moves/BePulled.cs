using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Maps;

namespace Sunshine.WorldServer.Game.Effects.Spells.Moves
{
    [EffectHandler(EffectsEnum.Effect_BePulled)]
    public class BePulled : SpellEffectHandler
    {
        public override void Apply()
        {
            if (Caster.HasState(SpellStatesEnum.Gravity) || Caster.HasState(SpellStatesEnum.Rooted) || Caster.HasState(SpellStatesEnum.Unmovable))
                return;

            foreach (var actor in GetAffectedActors())
            {
                var point = new MapPoint(TargetedCell);
                if (point.CellId == Caster.Position.Cell)
                    continue;

                var direction = Caster.Position.Point.OrientationTo(point, true);
                var start = Caster.Position.Point;
                var current = start;

                for (var index = 0; index < Effect.DiceNum; ++index)
                {
                    var next = current.GetNearestCellInDirection(direction);
                    if (next == null)
                        break;

                    if (Fight.ShouldTriggerOnMove(next.CellId, Caster))
                    {
                        current = next;
                        break;
                    }

                    if (!Fight.IsCellFree(next.CellId))
                        break;

                    current = next;
                }

                if (start.CellId == current.CellId)
                    continue;

                ForcedMovementHelper.DropCarriedActorIfNeeded(Caster);

                var slideClients = ForcedMovementHelper.SendSlide(Caster, Caster, start.CellId, current.CellId);

                Caster.Position.Cell = current.CellId;
                Caster.Position.Direction = direction;
                ForcedMovementHelper.RefreshAfterForcedMove(Caster, Caster, slideClients);

                Fight.TriggerMarks(Caster.Position.Cell, Caster, TriggerTypeEnum.MOVE);
                Fight.CheckFightEnd();
            }
        }
    }
}
