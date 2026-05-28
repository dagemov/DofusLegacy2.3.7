using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Effects.Spells;
using Sunshine.WorldServer.Game.Effects.Spells.Damages;
using System;

namespace Sunshine.WorldServer.Game.Effects.Spells.Damages
{
    [EffectHandler(EffectsEnum.Effect_Punishment_Damage)]
    public class PunishmentDamage : SpellEffectHandler
    {
        public override void Apply()
        {
            // Punishment damage formula (Dofus 2.x):
            // Damage = MaxLife * f(CurrentLife / MaxLife)
            // Where f(x) is a curve that peaks at 50% life.
            
            double lifePercent = (double)Caster.Stats.Health.Total / Caster.Stats.Health.TotalMax;
            double multiplier = 0;

            // Simplified Dofus 2.x punishment curve:
            // Peaks at 50% life with ~25% of Max HP as base damage (at level 6/max)
            // f(x) = 2 * (1 - x) * x * 2 (approximate)
            
            multiplier = 4 * lifePercent * (1 - lifePercent);
            
            // Base damage factor (Value from spell grade, usually around 20-30%)
            double baseFactor = (double)Value / 100.0;
            
            int baseDamage = (int)(Caster.Stats.Health.TotalMax * baseFactor * multiplier);

            foreach (var actor in GetAffectedActors())
            {
                // Neutral school for Punishment usually
                Damage damage = new Damage(baseDamage);
                damage.Source = Caster;
                damage.EffectSchool = EffectSchoolEnum.Neutral;
                
                actor.InflictDamage(damage);
            }
        }
    }

    [EffectHandler(EffectsEnum.Effect_275)]
    [EffectHandler(EffectsEnum.Effect_276)]
    [EffectHandler(EffectsEnum.Effect_277)]
    [EffectHandler(EffectsEnum.Effect_278)]
    [EffectHandler(EffectsEnum.Effect_279)]
    public class DamagePerHPLost : SpellEffectHandler
    {
        public override void Apply()
        {
            double lostLife = System.Math.Max(0, Caster?.Stats?.Health.Taken ?? 0);
            if (lostLife <= 0)
                return;

            int damageAmount = (int)System.Math.Round((lostLife * DiceNum) / 100d);
            if (damageAmount <= 0)
                return;

            foreach (var actor in GetAffectedActors())
            {
                if (actor == null || !actor.IsAlive)
                    continue;

                var damage = new Damage(damageAmount)
                {
                    Source = Caster,
                    Spell = Spell,
                    EffectSchool = GetEffectSchool(Id),
                    Amount = damageAmount
                };

                actor.InflictDamage(damage);
            }
        }

        private static EffectSchoolEnum GetEffectSchool(EffectsEnum effect)
        {
            switch (effect)
            {
                case EffectsEnum.Effect_275:
                    return EffectSchoolEnum.Water;
                case EffectsEnum.Effect_276:
                    return EffectSchoolEnum.Earth;
                case EffectsEnum.Effect_277:
                    return EffectSchoolEnum.Air;
                case EffectsEnum.Effect_278:
                    return EffectSchoolEnum.Fire;
                case EffectsEnum.Effect_279:
                    return EffectSchoolEnum.Neutral;
                default:
                    throw new Exception(string.Format("Effect {0} has not associated School Type", effect));
            }
        }
    }

}
