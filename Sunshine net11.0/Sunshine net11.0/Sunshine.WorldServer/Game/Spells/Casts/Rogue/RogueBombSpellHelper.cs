using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Maps.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Spells.Casts.Rogue
{
    internal static class RogueBombSpellHelper
    {
        public static IEnumerable<Effect> GetResolvedEffects(Spell spell, bool critical)
        {
            if (spell == null)
                return Enumerable.Empty<Effect>();

            if (critical && spell.CriticalEffects != null && spell.CriticalEffects.Count > 0)
                return spell.CriticalEffects;

            return spell.Effects ?? Enumerable.Empty<Effect>();
        }

        public static Effect GetFirstEffect(Spell spell, bool critical, EffectsEnum effectId, Predicate<Effect> predicate = null)
        {
            return GetResolvedEffects(spell, critical)
                .FirstOrDefault(x => x != null && x.Id == effectId && (predicate == null || predicate(x)));
        }

        public static int GetComboBonus(Spell spell, bool critical)
        {
            var effect = GetFirstEffect(spell, critical, EffectsEnum.Effect_AddComboDamage);
            if (effect == null)
                return 0;

            return effect.DiceNum > 0 ? (int)effect.DiceNum : Math.Max(0, effect.Value);
        }

        public static IEnumerable<BombFighter> GetAffectedFriendlyBombs(FightActor caster, Spell spell, bool critical, short targetedCell, params EffectsEnum[] relevantEffects)
        {
            var bombs = new HashSet<BombFighter>();
            if (caster == null || caster.Fight == null)
                return bombs;

            var effects = GetResolvedEffects(spell, critical)
                .Where(x => x != null && (relevantEffects == null || relevantEffects.Length == 0 || relevantEffects.Contains(x.Id)))
                .ToArray();

            foreach (var effect in effects)
            {
                foreach (var cell in GetAffectedCells(caster, effect, targetedCell))
                {
                    var bomb = caster.Fight.GetOneFighter(cell) as BombFighter;
                    if (bomb != null && bomb.IsAlive && !bomb.IsExploded && caster.IsFriendlyWith(bomb))
                        bombs.Add(bomb);
                }
            }

            var directTarget = caster.Fight.GetOneFighter(targetedCell) as BombFighter;
            if (directTarget != null && directTarget.IsAlive && !directTarget.IsExploded && caster.IsFriendlyWith(directTarget))
                bombs.Add(directTarget);

            return bombs;
        }

        public static void ApplyOrRefreshState(FightActor caster, FightActor target, Spell spell, Effect sourceEffect, SpellStatesEnum state, short duration, bool dispellable = true)
        {
            if (caster == null || target == null || spell == null || duration <= 0)
                return;

            var existing = target.GetBuffs(x => x is StateBuff stateBuff && stateBuff.State == state).ToArray();
            foreach (var buff in existing)
                target.RemoveBuff(buff);

            if (target.HasState(state))
                target.RemoveState(state);

            target.AddBuff(new StateBuff(caster, target, spell, sourceEffect, duration, dispellable, state));
        }

        private static IEnumerable<short> GetAffectedCells(FightActor caster, Effect effect, short targetedCell)
        {
            if (caster == null || caster.Map == null || effect == null)
                return new[] { targetedCell };

            if ((int)effect.ZoneShape == 0 && effect.ZoneSize == 0)
                return new[] { targetedCell };

            try
            {
                var zone = new Zone(effect.ZoneShape, (byte)effect.ZoneSize, (byte)effect.ZoneMinSize)
                {
                    Direction = ResolveDirection(caster, targetedCell)
                };
                return zone.GetCells(targetedCell, caster.Map) ?? new[] { targetedCell };
            }
            catch
            {
                return new[] { targetedCell };
            }
        }

        private static DirectionsEnum ResolveDirection(FightActor caster, short targetedCell)
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
