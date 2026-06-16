using Sunshine.Protocol.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Damages
{
    [EffectHandler(EffectsEnum.Effect_DamageEarth), EffectHandler(EffectsEnum.Effect_DamageAir),
     EffectHandler(EffectsEnum.Effect_DamageFire), EffectHandler(EffectsEnum.Effect_DamageNeutral),
     EffectHandler(EffectsEnum.Effect_DamageWater), EffectHandler(EffectsEnum.Effect_1012),
     EffectHandler(EffectsEnum.Effect_1013), EffectHandler(EffectsEnum.Effect_1014),
     EffectHandler(EffectsEnum.Effect_1015), EffectHandler(EffectsEnum.Effect_1016)]
    public class DirectDamage : SpellEffectHandler
    {       
        public DirectDamage()
        {
        }

        public override void Apply()
        {
            var effectSchool = GetEffectSchool(Id);
            int effectiveDuration = Duration;
            var (resolvedDiceNum, resolvedDiceFace, fixedBonus) = EffectDamageResolver.ResolveDice(Effect);

            foreach(var actor in GetAffectedActors())
            {
                uint baseDiceNum = resolvedDiceNum;

                // Duration > 0 means damage over time (poison/trap): apply as a ticking buff.
                if (effectiveDuration > 0)
                {
                    var dotBuff = new Fights.Buffs.Spells.DamageOverTimeBuff(Caster, actor, Spell, Effect,
                        (short)effectiveDuration, effectSchool, baseDiceNum, resolvedDiceFace, fixedBonus);
                    actor.AddBuff(dotBuff);
                    continue;
                }

                // Iop's Wrath (Yopuka) logic
                if (Spell.Id == 159) // Colère de Iop
                {
                    string formulaNotes = "IopWrath:first_cast_state51";
                    // Check if it's the second cast (charge)
                    // We can use a state or spell history.
                    if (Caster.HasState(SpellStatesEnum.State_51)) // Let's use State 51 as "Charged"
                    {
                        baseDiceNum += 80; // Massive boost for charge
                        Caster.RemoveState(SpellStatesEnum.State_51);
                        formulaNotes = "IopWrath:charged_cast_diceNum+80";
                    }
                    else
                    {
                        Caster.AddState(SpellStatesEnum.State_51); // Add charge state for next time
                        // In 2.x, the state usually lasts 3 turns
                    }

                    Damage damage = new Damage(effectSchool, baseDiceNum, resolvedDiceFace, Spell, Caster)
                    {
                        FixedBonus = fixedBonus,
                        TelemetryFormulaNotes = formulaNotes
                    };
                    actor.InflictDamage(damage);
                    continue;
                }

                Damage damageDefault = new Damage(effectSchool, baseDiceNum, resolvedDiceFace, Spell, Caster)
                {
                    FixedBonus = fixedBonus
                };
                actor.InflictDamage(damageDefault);
            }
        }

        private EffectSchoolEnum GetEffectSchool(EffectsEnum effect)
        {
            EffectSchoolEnum result;
            switch (effect)
            {
                case EffectsEnum.Effect_DamageWater:
                case EffectsEnum.Effect_1014:
                    result = EffectSchoolEnum.Water;
                    break;
                case EffectsEnum.Effect_DamageEarth:
                case EffectsEnum.Effect_1016:
                    result = EffectSchoolEnum.Earth;
                    break;
                case EffectsEnum.Effect_DamageAir:
                case EffectsEnum.Effect_1013:
                    result = EffectSchoolEnum.Air;
                    break;
                case EffectsEnum.Effect_DamageFire:
                case EffectsEnum.Effect_1015:
                    result = EffectSchoolEnum.Fire;
                    break;
                case EffectsEnum.Effect_DamageNeutral:
                case EffectsEnum.Effect_1012:
                    result = EffectSchoolEnum.Neutral;
                    break;
                default:
                    throw new System.Exception(string.Format("Effect {0} has not associated School Type", effect));
            }
            return result;
        }
    }
}
