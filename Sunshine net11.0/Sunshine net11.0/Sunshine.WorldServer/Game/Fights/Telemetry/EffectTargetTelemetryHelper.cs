using System.Collections.Generic;
using System.Globalization;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Maps.Shapes;
using Sunshine.WorldServer.Game.Spells;

namespace Sunshine.WorldServer.Game.Fights.Telemetry
{
    public sealed class EffectTargetEntry
    {
        public FightActor Actor { get; set; }
        public bool Included { get; set; }
        public string FilterReason { get; set; }
    }

    internal static class EffectTargetTelemetryHelper
    {
        public static void Resolve(
            FightActor caster,
            Effect effect,
            short targetedCell,
            out List<EffectTargetEntry> included,
            out List<EffectTargetEntry> filtered)
        {
            included = new List<EffectTargetEntry>();
            filtered = new List<EffectTargetEntry>();

            if (caster?.Fight == null || effect == null || caster.Map == null)
                return;

            var zone = new Zone(effect.ZoneShape, (byte)effect.ZoneSize, (byte)effect.ZoneMinSize)
            {
                Direction = ResolveZoneDirection(caster, targetedCell)
            };

            foreach (short cell in zone.GetCells(targetedCell, caster.Map))
            {
                var target = caster.Fight.GetOneFighter(cell);
                if (target == null)
                    continue;

                if (TryIncludeTarget(caster, effect, target, out var reason))
                {
                    included.Add(new EffectTargetEntry
                    {
                        Actor = target,
                        Included = true
                    });
                }
                else
                {
                    filtered.Add(new EffectTargetEntry
                    {
                        Actor = target,
                        Included = false,
                        FilterReason = reason
                    });
                }
            }
        }

        private static bool TryIncludeTarget(FightActor caster, Effect effect, FightActor target, out string reason)
        {
            reason = null;
            switch (effect.Target)
            {
                case SpellTargetType.ALLY_ALL:
                    if (target.IsFriendlyWith(caster))
                        return true;
                    reason = "not_ally";
                    return false;

                case SpellTargetType.ENEMY_ALL:
                    if (target.IsEnnemyWith(caster))
                        return true;
                    reason = "not_enemy";
                    return false;

                case (SpellTargetType)3840:
                    if (target != caster)
                        return true;
                    reason = "is_caster";
                    return false;

                case SpellTargetType.ONLY_SELF:
                    if (target == caster)
                        return true;
                    reason = "not_self";
                    return false;

                default:
                    return true;
            }
        }

        public static string ResolveTargetMaskLabel(int targetMask)
        {
            switch (targetMask)
            {
                case (int)SpellTargetType.ALLY_ALL:
                    return "ALLY_ALL";
                case (int)SpellTargetType.ENEMY_ALL:
                    return "ENEMY_ALL";
                case (int)SpellTargetType.ONLY_SELF:
                    return "ONLY_SELF";
                case 3840:
                    return "NOT_SELF";
                default:
                    return string.Format(CultureInfo.InvariantCulture, "MASK_{0}", targetMask);
            }
        }

        private static DirectionsEnum ResolveZoneDirection(FightActor caster, short targetedCell)
        {
            if (caster?.Position?.Point == null)
                return DirectionsEnum.DIRECTION_SOUTH_EAST;

            if (caster.Position.Cell == targetedCell)
                return caster.Position.Direction;

            try
            {
                return caster.Position.Point.OrientationTo(MapPoint.GetPoint(targetedCell), true);
            }
            catch
            {
                return caster.Position.Direction;
            }
        }
    }
}
