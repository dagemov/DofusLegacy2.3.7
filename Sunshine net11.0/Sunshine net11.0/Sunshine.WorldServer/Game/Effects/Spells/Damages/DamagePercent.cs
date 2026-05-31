using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using System;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.Damages
{
    [EffectHandler(EffectsEnum.Effect_DamagePercentAir)]
    [EffectHandler(EffectsEnum.Effect_DamagePercentEarth)]
    [EffectHandler(EffectsEnum.Effect_DamagePercentFire)]
    [EffectHandler(EffectsEnum.Effect_DamagePercentWater)]
    [EffectHandler(EffectsEnum.Effect_DamagePercentNeutral)]
    public class DamagePercent : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var actor in GetAffectedActors().Where(x => x != null).ToArray())
            {
                Effect.GenerateEffect();

                int percent = Effect.Value;
                if (percent <= 0)
                    continue;

                int casterCurrentLife = Caster?.Stats?.Health != null ? Caster.Stats.Health.Total : 0;
                if (casterCurrentLife <= 0)
                    continue;

                int amount = (int)Math.Round(casterCurrentLife * (percent / 100d), MidpointRounding.AwayFromZero);
                if (amount <= 0)
                    amount = 1;

                var damage = new Damage(GetEffectSchool(Id), (uint)amount, (uint)amount, Spell, Caster)
                {
                    EffectGenerationType = EffectGenerationEnum.Normal
                };

                actor.InflictDamage(damage);
            }
        }

        private static EffectSchoolEnum GetEffectSchool(EffectsEnum effect)
        {
            switch (effect)
            {
                case EffectsEnum.Effect_DamagePercentAir:
                    return EffectSchoolEnum.Air;
                case EffectsEnum.Effect_DamagePercentEarth:
                    return EffectSchoolEnum.Earth;
                case EffectsEnum.Effect_DamagePercentFire:
                    return EffectSchoolEnum.Fire;
                case EffectsEnum.Effect_DamagePercentWater:
                    return EffectSchoolEnum.Water;
                case EffectsEnum.Effect_DamagePercentNeutral:
                    return EffectSchoolEnum.Neutral;
                default:
                    throw new Exception(string.Format("Effect {0} has not associated School Type", effect));
            }
        }
    }
}
