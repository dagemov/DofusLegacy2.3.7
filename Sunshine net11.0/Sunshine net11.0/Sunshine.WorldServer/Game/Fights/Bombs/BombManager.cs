using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Effects.Spells;
using Sunshine.WorldServer.Game.Effects.Spells.Damages;
using Sunshine.WorldServer.Game.Fights.Triggers;
using Sunshine.WorldServer.Game.Maps.Shapes;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Fights.Bombs
{
    public class BombManager : Singleton<BombManager>
    {
        public void Explode(BombFighter bomb, int? forcedBonus = null)
        {
            if (bomb?.Fight == null)
                return;

            var linkedBombs = bomb.GetBombsBoundedWith()
                .Where(x => x != null && x.Fight == bomb.Fight && x.IsAlive && (x == bomb || !x.IsExploded))
                .Distinct()
                .ToList();

            if (!linkedBombs.Contains(bomb))
                linkedBombs.Insert(0, bomb);

            if (linkedBombs.Count == 0)
                linkedBombs.Add(bomb);

            int comboBonus = forcedBonus ?? linkedBombs.Sum(x => x.Combo);

            foreach (var linkedBomb in linkedBombs)
                ApplyExplosionDamage(linkedBomb, comboBonus);

            var killer = bomb.Summoner ?? bomb;
            foreach (var linkedBomb in linkedBombs.Where(x => x != null && x != bomb && !x.IsDead() && !x.IsExploded).ToArray())
                linkedBomb.Die(killer);

            CheckWalls(bomb.Fight, bomb.Summoner);
        }

        public void CheckWalls(Fight fight) => CheckWalls(fight, null);

        public void CheckWalls(Fight fight, FightActor summoner)
        {
            if (fight == null || fight.State == FightStateEnum.Ended)
                return;

            var bombs = fight.GetAllFighters()
                .OfType<BombFighter>()
                .Where(x => x != null
                            && x.IsAlive
                            && !x.IsExploded
                            && x.Position != null
                            && (summoner == null || x.Summoner == summoner))
                .ToArray();

            foreach (var bomb in bombs)
            {
                foreach (var otherBomb in bombs)
                {
                    if (otherBomb == null || otherBomb == bomb || bomb.Position == null)
                        continue;

                    var bindingsToDelete = otherBomb.WallBindings
                        .Where(binding => binding != null && binding.Contains(bomb.Position.Cell))
                        .ToArray();

                    foreach (var binding in bindingsToDelete)
                        binding.Delete();
                }
            }

            foreach (var bomb in bombs)
            {
                var invalidBindings = bomb.WallBindings
                    .Where(x => x == null || !x.IsValid())
                    .ToArray();

                foreach (var invalidBinding in invalidBindings)
                    invalidBinding?.Delete();

                var bindingsToAdjust = bomb.WallBindings
                    .Where(x => x != null && x.IsValid() && x.MustBeAdjusted())
                    .ToArray();

                foreach (var binding in bindingsToAdjust)
                    binding.AdjustWalls();
            }

            foreach (var bomb1 in bombs)
            {
                foreach (var bomb2 in bombs)
                {
                    if (bomb2 == null || bomb1 == bomb2)
                        continue;

                    if (!bomb1.IsBoundWith(bomb2))
                        continue;

                    bool bindingAlreadyExists = bomb1.WallBindings.Any(x =>
                        x != null && ((x.Bomb1 == bomb2) || (x.Bomb2 == bomb2)));

                    if (bindingAlreadyExists)
                        continue;

                    var binding = new BombWallBinding(bomb1, bomb2, bomb1.GetWallColor());
                    binding.AdjustWalls();

                    bomb1.AddWallBinding(binding);
                    bomb2.AddWallBinding(binding);
                }
            }
        }

        private void TriggerExistingTargetsOnWalls(Fight fight, FightActor summoner)
        {
            if (fight == null || fight.State == FightStateEnum.Ended)
                return;

            var walls = fight.GetTriggers()
                .OfType<BombWall>()
                .Where(x => x != null && (summoner == null || x.Caster == summoner || x.Bombs.Any(y => y != null && y.Summoner == summoner)))
                .ToArray();

            foreach (var wall in walls)
            {
                var fighter = fight.GetOneFighter(wall.CenterCell);
                if (fighter != null && fighter.IsAlive)
                    fight.TriggerMarks(wall.CenterCell, fighter, TriggerTypeEnum.MOVE);
            }
        }

        private void ApplyExplosionDamage(BombFighter bomb, int comboBonus)
        {
            if (bomb == null || bomb.Fight == null)
                return;

            double comboMultiplier = 1d + (comboBonus / 100d);

            foreach (var target in GetExplosionTargets(bomb))
            {
                var damage = CreatePrimaryDamageForTarget(bomb, target, comboMultiplier, false);
                if (damage != null)
                    target.InflictDamage(CloneDamage(damage));

                ApplySecondaryEffects(bomb, target, false);
            }

            foreach (var ally in GetKaboomTargets(bomb))
                ApplySecondaryEffects(bomb, ally, false);
        }

        public static IEnumerable<FightActor> GetExplosionTargets(BombFighter bomb)
        {
            if (bomb?.Fight == null || bomb.Position == null)
                return Enumerable.Empty<FightActor>();

            var zoneCells = new HashSet<short>(GetExplosionCells(bomb));
            return bomb.Fight.GetAllFighters().Where(x => IsExplosionTarget(bomb, x, zoneCells)).ToArray();
        }

        public static short[] GetExplosionCells(BombFighter bomb)
        {
            if (bomb?.Position == null || bomb.Map == null)
                return new short[0];

            return new Lozenge(0, (byte)BombFighter.ExplosionZone).GetCells(bomb.Position.Cell, bomb.Map);
        }

        public static IEnumerable<Damage> CreateDamages(BombFighter bomb, double comboMultiplier, bool useWallSpell)
        {
            if (bomb == null)
                return Enumerable.Empty<Damage>();

            var spell = ResolveDamageSpell(bomb, useWallSpell);
            var effects = GetDamageEffects(spell, bomb.Element).ToArray();
            return CreateDamagesFromEffects(bomb, spell, effects, comboMultiplier, useWallSpell);
        }

        public static Damage CreatePrimaryDamageForTarget(BombFighter bomb, FightActor target, double comboMultiplier, bool useWallSpell)
        {
            return CreateDamagesForTarget(bomb, target, comboMultiplier, useWallSpell).FirstOrDefault();
        }

        public static IEnumerable<Damage> CreateDamagesForTarget(BombFighter bomb, FightActor target, double comboMultiplier, bool useWallSpell)
        {
            if (bomb == null || target == null)
                return Enumerable.Empty<Damage>();

            var spell = ResolveDamageSpell(bomb, useWallSpell);
            var effects = GetDamageEffects(spell, bomb.Element).ToArray();
            if (effects.Length == 0)
                return CreateDamagesFromEffects(bomb, spell, effects, comboMultiplier, useWallSpell);

            var matchedEffect = SelectBestDamageEffect(bomb, target, effects);
            if (matchedEffect == null)
                return CreateDamagesFromEffects(bomb, spell, Enumerable.Empty<Effect>(), comboMultiplier, useWallSpell, false);

            return CreateDamagesFromEffects(bomb, spell, new[] { matchedEffect }, comboMultiplier, useWallSpell, false);
        }

        private static Spell ResolveDamageSpell(BombFighter bomb, bool useWallSpell)
        {
            if (bomb == null)
                return null;

            return useWallSpell
                ? (bomb.WallSpell ?? bomb.ExplosionDamageSpell ?? bomb.ExplosionSpell)
                : (bomb.ExplosionDamageSpell ?? bomb.ExplosionSpell ?? bomb.WallSpell);
        }

        private static Effect SelectBestDamageEffect(BombFighter bomb, FightActor target, IEnumerable<Effect> effects)
        {
            var candidates = (effects ?? Enumerable.Empty<Effect>())
                .Where(x => x != null)
                .ToArray();

            if (candidates.Length == 0)
                return null;

            var matched = candidates
                .Where(x => DoesEffectHitTarget(bomb, target, x))
                .OrderBy(GetDamageEffectPriority)
                .ThenByDescending(GetDamageEffectWeight)
                .ToArray();

            if (matched.Length > 0)
                return matched[0];

            return candidates
                .OrderBy(GetDamageEffectPriority)
                .ThenByDescending(GetDamageEffectWeight)
                .FirstOrDefault();
        }

        private static int GetDamageEffectPriority(Effect effect)
        {
            if (effect == null)
                return int.MaxValue;

            SpellShapeEnum zoneShape = effect.ZoneShape;
            uint zoneSize = effect.ZoneSize;
            uint zoneMinSize = effect.ZoneMinSize;

            if ((int)zoneShape == 0 && zoneSize == 0)
            {
                zoneShape = SpellShapeEnum.C;
                zoneSize = BombFighter.ExplosionZone;
                zoneMinSize = 0;
            }

            try
            {
                return (int)new Zone(zoneShape, (byte)zoneSize, (byte)zoneMinSize).Surface;
            }
            catch
            {
                return int.MaxValue;
            }
        }

        private static int GetDamageEffectWeight(Effect effect)
        {
            if (effect == null)
                return 0;

            int min = effect.DiceNum > 0 ? (int)effect.DiceNum : Math.Max(1, effect.Value);
            int max = effect.DiceFace > 0 ? (int)effect.DiceFace : Math.Max(min, effect.Value);
            return min + max;
        }

        private static IEnumerable<Damage> CreateDamagesFromEffects(BombFighter bomb, Spell spell, IEnumerable<Effect> effects, double comboMultiplier, bool useWallSpell, bool useFallbackWhenEmpty = true)
        {
            if (bomb == null)
                return Enumerable.Empty<Damage>();

            var resolvedEffects = (effects ?? Enumerable.Empty<Effect>()).Where(x => x != null).ToArray();
            if (resolvedEffects.Length == 0)
            {
                if (!useFallbackWhenEmpty)
                    return Enumerable.Empty<Damage>();

                var fallback = new Damage(GetEffectSchool(bomb.Element), useWallSpell ? 22u : 30u, useWallSpell ? 30u : 42u, spell, bomb.Summoner ?? bomb)
                {
                    EffectGenerationType = EffectGenerationEnum.Normal
                };
                return new[] { fallback };
            }

            var result = new List<Damage>();
            foreach (var effect in resolvedEffects)
            {
                uint min = effect.DiceNum > 0 ? effect.DiceNum : (uint)Math.Max(1, effect.Value);
                uint max = effect.DiceFace > 0 ? effect.DiceFace : (uint)Math.Max(min, effect.Value);
                min = (uint)Math.Max(1, Math.Round(min * comboMultiplier));
                max = (uint)Math.Max(min, Math.Round(max * comboMultiplier));
                result.Add(new Damage(GetEffectSchool(bomb.Element), min, max, spell, bomb.Summoner ?? bomb)
                {
                    EffectGenerationType = EffectGenerationEnum.Normal
                });
            }

            return result;
        }

        private static Damage CloneDamage(Damage damage)
        {
            return new Damage(damage.EffectSchool, (uint)damage.BaseMinDamages, (uint)damage.BaseMaxDamages, damage.Spell, damage.Source)
            {
                EffectGenerationType = damage.EffectGenerationType
            };
        }

        private static IEnumerable<Effect> GetDamageEffects(Spell spell, int element)
        {
            if (spell?.Effects == null)
                return Enumerable.Empty<Effect>();

            return spell.Effects.Where(x => x != null && IsDamageEffect(x.Id)).Select(x => NormalizeDamageEffect(x.Clone(), element)).Where(x => x != null).ToArray();
        }

        private static Effect NormalizeDamageEffect(Effect effect, int element)
        {
            if (effect == null)
                return null;

            switch (element)
            {
                case 3:
                    effect.Id = EffectsEnum.Effect_DamageAir;
                    break;
                case 4:
                    effect.Id = EffectsEnum.Effect_DamageWater;
                    break;
                default:
                    effect.Id = EffectsEnum.Effect_DamageFire;
                    break;
            }

            effect.Delay = 0;
            return effect;
        }


        private static IEnumerable<FightActor> GetKaboomTargets(BombFighter bomb)
        {
            if (bomb?.Fight == null || bomb.Position == null)
                return Enumerable.Empty<FightActor>();

            var zoneCells = new HashSet<short>(GetExplosionCells(bomb));
            return bomb.Fight.GetAllFighters()
                .Where(x => x != null && x.IsAlive && x != bomb && x.Position != null && x.HasState(SpellStatesEnum.Kaboom) && x.IsFriendlyWith(bomb) && zoneCells.Contains(x.Position.Cell))
                .ToArray();
        }

        private static void ApplySecondaryEffects(BombFighter bomb, FightActor target, bool useWallSpell)
        {
            if (bomb == null || target == null || !target.IsAlive)
                return;

            bool kaboomTarget = target.IsFriendlyWith(bomb) && target.HasState(SpellStatesEnum.Kaboom);
            var spell = ResolveSupportSpell(bomb, useWallSpell);
            if (spell?.Effects == null || spell.Effects.Count == 0)
                return;

            var selectedEffect = SelectBestSecondaryEffect(
                bomb,
                target,
                spell.Effects
                    .Where(x => x != null && IsSupportedBombEffect(bomb.Element, x.Id, kaboomTarget))
                    .Select(x => x.Clone())
                    .ToArray());

            if (selectedEffect != null)
                ApplySpellEffectToTarget(bomb, target, spell, selectedEffect);
        }

        private static Effect SelectBestSecondaryEffect(BombFighter bomb, FightActor target, IEnumerable<Effect> effects)
        {
            var candidates = (effects ?? Enumerable.Empty<Effect>())
                .Where(x => x != null)
                .ToArray();

            if (candidates.Length == 0)
                return null;

            var matched = candidates
                .Where(x => DoesEffectHitTarget(bomb, target, x))
                .OrderByDescending(GetSecondaryEffectDuration)
                .ThenByDescending(GetSecondaryEffectWeight)
                .ThenBy(GetDamageEffectPriority)
                .ToArray();

            if (matched.Length > 0)
                return matched[0];

            return candidates
                .OrderByDescending(GetSecondaryEffectDuration)
                .ThenByDescending(GetSecondaryEffectWeight)
                .ThenBy(GetDamageEffectPriority)
                .FirstOrDefault();
        }

        private static int GetSecondaryEffectDuration(Effect effect)
        {
            return effect == null ? 0 : Math.Max(0, effect.Duration);
        }

        private static int GetSecondaryEffectWeight(Effect effect)
        {
            if (effect == null)
                return 0;

            int min = effect.DiceNum > 0 ? (int)effect.DiceNum : Math.Max(1, effect.Value);
            int max = effect.DiceFace > 0 ? (int)effect.DiceFace : Math.Max(min, effect.Value);
            return min + max;
        }

        private static Spell ResolveSupportSpell(BombFighter bomb, bool useWallSpell)
        {
            if (bomb == null)
                return null;

            return useWallSpell
                ? (bomb.WallSpell ?? bomb.ExplosionSpell ?? bomb.ExplosionDamageSpell)
                : (bomb.ExplosionSpell ?? bomb.WallSpell ?? bomb.ExplosionDamageSpell);
        }

        private static bool IsSupportedBombEffect(int element, EffectsEnum effectId, bool kaboomTarget)
        {
            switch (element)
            {
                case 4:
                    return kaboomTarget
                        ? effectId == EffectsEnum.Effect_AddAP_111 || effectId == EffectsEnum.Effect_RegainAP
                        : effectId == EffectsEnum.Effect_RemoveAP || effectId == EffectsEnum.Effect_LosingAP || effectId == EffectsEnum.Effect_SubAP || effectId == EffectsEnum.Effect_SubAP_1079;

                case 3:
                    return kaboomTarget
                        ? effectId == EffectsEnum.Effect_AddMP || effectId == EffectsEnum.Effect_AddMP_128
                        : effectId == EffectsEnum.Effect_LostMP || effectId == EffectsEnum.Effect_LosingMP || effectId == EffectsEnum.Effect_SubMP_1080 || effectId == EffectsEnum.Effect_StealMP_77;

                default:
                    return kaboomTarget
                        && (effectId == EffectsEnum.Effect_AddDamageBonusPercent || effectId == EffectsEnum.Effect_IncreaseDamage_138 || effectId == EffectsEnum.Effect_IncreaseDamage_1054);
            }
        }

        private static void ApplySpellEffectToTarget(BombFighter bomb, FightActor target, Spell spell, Effect effect)
        {
            if (bomb == null || target == null || spell == null || effect == null)
                return;

            if (!EffectManager.Instance.SpellEffects.TryGetValue(effect.Id, out Func<SpellEffectHandler> factory) || factory == null)
                return;

            var handler = factory();
            handler.Prepare(new List<object>
            {
                effect.Id,
                effect.DiceNum,
                effect.DiceFace,
                effect.Value,
                effect.Delay,
                effect.Duration,
                effect.Target,
                target.Position != null ? target.Position.Cell : (short)0,
                new[] { target },
                bomb.Summoner ?? bomb,
                spell,
                effect,
                (short)-1,
                null,
                0
            });
            handler.Apply();
        }

        private static bool IsExplosionTarget(BombFighter bomb, FightActor actor, ISet<short> zoneCells = null)
        {
            if (bomb == null || actor == null || !actor.IsAlive || actor == bomb || actor.Position == null || bomb.Position == null)
                return false;

            if (actor is BombFighter)
                return false;

            if (actor.IsFriendlyWith(bomb) && actor.HasState(SpellStatesEnum.Kaboom))
                return false;

            var cells = zoneCells ?? new HashSet<short>(GetExplosionCells(bomb));
            return cells.Contains(actor.Position.Cell);
        }

        private static bool DoesEffectHitTarget(BombFighter bomb, FightActor target, Effect effect)
        {
            if (bomb?.Position == null || target?.Position == null || bomb.Map == null || effect == null)
                return false;

            SpellShapeEnum zoneShape = effect.ZoneShape;
            uint zoneSize = effect.ZoneSize;
            uint zoneMinSize = effect.ZoneMinSize;

            // Some bomb target helper spells come without a reliable zone in old dumps.
            // Default them to the real rogue bomb area: zone 2 (2 cells in line / 1 diagonal).
            if ((int)zoneShape == 0 && zoneSize == 0)
            {
                zoneShape = SpellShapeEnum.C;
                zoneSize = BombFighter.ExplosionZone;
                zoneMinSize = 0;
            }

            var zone = new Zone(zoneShape, (byte)zoneSize, (byte)zoneMinSize)
            {
                Direction = bomb.Position.Direction
            };

            return zone.GetCells(bomb.Position.Cell, bomb.Map).Contains(target.Position.Cell);
        }

        private void RefreshWallsDisplay(Fight fight, FightActor summoner)
        {
            if (fight == null || fight.State == FightStateEnum.Ended)
                return;

            var walls = fight.GetTriggers()
                .OfType<BombWall>()
                .Where(x => x != null && (summoner == null || x.Caster == summoner || x.Bombs.Any(y => y != null && y.Summoner == summoner)))
                .ToArray();

            var fighters = fight.GetAllFighters().ToArray();
            var characters = fighters.OfType<CharacterFighter>().ToArray();

            // Keep fighter state synchronized first, then send the wall marks last so the client keeps
            // the colored cells displayed immediately after the bombs align.
            ContextHandler.SendGameFightSynchronizeMessage(fight.Clients, fighters);
            ContextHandler.SendGameEntitiesDispositionMessage(fight.Clients, fighters);

            foreach (var wall in walls)
                ContextHandler.SendGameActionFightUnmarkCellsMessage(fight.Clients, wall);

            foreach (var current in characters)
            {
                foreach (var wall in walls)
                    ContextHandler.SendGameActionFightMarkCellsMessage(current.Client, wall, wall.DoesSeeTrigger(current));
            }
        }

        private static bool IsDamageEffect(EffectsEnum effect)
        {
            switch (effect)
            {
                case EffectsEnum.Effect_DamageEarth:
                case EffectsEnum.Effect_DamageAir:
                case EffectsEnum.Effect_DamageFire:
                case EffectsEnum.Effect_DamageNeutral:
                case EffectsEnum.Effect_DamageWater:
                case EffectsEnum.Effect_1012:
                case EffectsEnum.Effect_1013:
                case EffectsEnum.Effect_1014:
                case EffectsEnum.Effect_1015:
                case EffectsEnum.Effect_1016:
                    return true;
                default:
                    return false;
            }
        }

        private static EffectSchoolEnum GetEffectSchool(int element)
        {
            switch (element)
            {
                case 3:
                    return EffectSchoolEnum.Air;
                case 4:
                    return EffectSchoolEnum.Water;
                default:
                    return EffectSchoolEnum.Fire;
            }
        }

        public void ApplyWallSecondaryEffects(BombFighter bomb, FightActor target)
        {
            ApplySecondaryEffects(bomb, target, true);
        }

        public void OnTurnStarted(FightActor fighter)
        {
            if (fighter == null || fighter.Fight == null)
                return;

            var bombs = fighter.Fight.GetAllFighters<BombFighter>(x => x != null && x.IsAlive && !x.IsExploded).ToArray();
            var bombsToExplode = new List<BombFighter>();

            foreach (var bomb in bombs)
            {
                if (bomb.Summoner != fighter)
                    continue;

                bomb.IncreaseCombo();
                if (bomb.AdvanceDelayedExplosion())
                    bombsToExplode.Add(bomb);
            }

            CheckWalls(fighter.Fight, fighter);

            foreach (var bomb in bombsToExplode.Distinct().Where(x => x != null && x.IsAlive && !x.IsExploded).ToArray())
                bomb.Explode(fighter);
        }
    }
}
