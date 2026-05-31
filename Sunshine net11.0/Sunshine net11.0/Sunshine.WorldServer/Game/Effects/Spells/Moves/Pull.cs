using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Bombs;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Fights.Mechanics;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.Moves
{
    [EffectHandler(EffectsEnum.Effect_PullForward)]
    public class Pull : SpellEffectHandler
    {
        public override void Apply()
        {
            var targetedPoint = new MapPoint(TargetedCell);

            foreach (var actor in GetAffectedActors().Where(x => x != null).OrderBy(x => x.Position.Point.DistanceToCell(targetedPoint)))
            {
                if (actor.HasState(SpellStatesEnum.Gravity) || actor.HasState(SpellStatesEnum.Rooted) || actor.HasState(SpellStatesEnum.Unmovable))
                    continue;

                var fromPoint = TargetedCell != actor.Position.Cell ? targetedPoint : new MapPoint(Caster.Position.Cell);
                if (fromPoint.CellId == actor.Position.Cell)
                    continue;

                var direction = actor.Position.Point.OrientationTo(fromPoint, false);
                var start = actor.Position.Point;
                var current = start;

                for (int i = 0; i < Effect.DiceNum; i++)
                {
                    var next = current.GetNearestCellInDirection(direction);
                    if (next == null)
                        break;

                    if (Fight.ShouldTriggerOnMove(next.CellId, actor))
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

                ForcedMovementHelper.DropCarriedActorIfNeeded(actor);

                var slideClients = ForcedMovementHelper.SendSlide(Caster, actor, start.CellId, current.CellId);

                actor.Position.Cell = current.CellId;
                actor.Position.Direction = direction;
                FrigostBossMechanics.OnForcedMove(Caster, actor, start.CellId, current.CellId, false, false);
                ForcedMovementHelper.RefreshAfterForcedMove(Caster, actor, slideClients);

                if (actor is BombFighter movedBomb)
                    BombManager.Instance.CheckWalls(Fight, movedBomb.Summoner);

                Fight.TriggerMarks(actor.Position.Cell, actor, TriggerTypeEnum.MOVE);
                Fight.CheckFightEnd();
            }
        }
    }
}
