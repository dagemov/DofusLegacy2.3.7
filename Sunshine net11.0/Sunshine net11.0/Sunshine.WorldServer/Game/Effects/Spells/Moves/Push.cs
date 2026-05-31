using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Bombs;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Fights.Mechanics;
using System;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.Moves
{
    [EffectHandler(EffectsEnum.Effect_Push), EffectHandler(EffectsEnum.Effect_Push_1103)]
    public class Push : SpellEffectHandler
    {
        public bool DamagesDisabled { get; set; }

        public FightActor SubRangeForActor { get; set; }

        public override void Apply()
        {
            short referenceCellId = TrapCell != -1 ? TrapCell : TargetedCell;
            var targetedPoint = new MapPoint(referenceCellId);
            foreach (var actor in GetAffectedActors().Where(x => x != null).OrderByDescending(x => x.Position.Point.DistanceToCell(targetedPoint)))
            {
                if (actor.HasState(SpellStatesEnum.Gravity) || actor.HasState(SpellStatesEnum.Rooted) || actor.HasState(SpellStatesEnum.Unmovable))
                    continue;

                var point = referenceCellId == actor.Position.Cell ? new MapPoint(Caster.Position.Cell) : targetedPoint;
                if (point.CellId == actor.Position.Cell)
                    continue;

                var direction = (FirstPosition != null && CountPushed > 1)
                    ? point.OrientationTo(FirstPosition.Point, false)
                    : point.OrientationTo(actor.Position.Point, false);

                var startCell = actor.Position.Point;
                var current = startCell;
                bool tookPushbackDamage = false;
                FightActor collisionTarget = null;
                int range = Math.Max(0, (int)Effect.DiceNum - (SubRangeForActor == actor ? 1 : 0));

                for (int i = 0; i < range; i++)
                {
                    var next = current.GetNearestCellInDirection(direction);
                    if (next == null)
                        break;

                    if (!Fight.IsCellFree(next.CellId))
                    {
                        collisionTarget = Fight.GetOneFighter(next.CellId);

                        if (!DamagesDisabled)
                        {
                            int damage = ((8 + new AsyncRandom().Next(1, 8) * Caster.Level / 50) * (range - i)
                                         + Caster.Stats.PushDamageBonus.TotalMax
                                         - actor.Stats.PushDamageBonusReduction.TotalMax);

                            damage = Math.Max(0, damage);
                            if (damage > 0)
                            {
                                actor.InflictDamage(damage, Caster);
                                tookPushbackDamage = true;
                            }
                        }

                        break;
                    }

                    if (Fight.ShouldTriggerOnMove(next.CellId, actor))
                    {
                        current = next;
                        break;
                    }

                    current = next;
                }

                if (startCell.CellId == current.CellId && !tookPushbackDamage)
                    continue;

                ForcedMovementHelper.DropCarriedActorIfNeeded(actor);

                var slideClients = ForcedMovementHelper.SendSlide(Caster, actor, startCell.CellId, current.CellId);

                actor.Position.Cell = current.CellId;
                actor.Position.Direction = direction;
                FrigostBossMechanics.OnForcedMove(Caster, actor, startCell.CellId, current.CellId, true, tookPushbackDamage, collisionTarget);
                ForcedMovementHelper.RefreshAfterForcedMove(Caster, actor, slideClients);

                if (actor is BombFighter movedBomb)
                    BombManager.Instance.CheckWalls(Fight, movedBomb.Summoner);

                Fight.TriggerMarks(actor.Position.Cell, actor, TriggerTypeEnum.MOVE);
            }

            Fight.CheckFightEnd();
        }
    }
}
