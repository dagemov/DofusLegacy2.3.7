using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.States
{
    [EffectHandler(EffectsEnum.Effect_AddRange_136), EffectHandler(EffectsEnum.Effect_AddAgility), EffectHandler(EffectsEnum.Effect_AddDamageBonus), EffectHandler(EffectsEnum.Effect_AddSummonLimit), EffectHandler(EffectsEnum.Effect_IncreaseDamage_138), EffectHandler(EffectsEnum.Effect_AddCriticalMiss), 
        EffectHandler(EffectsEnum.Effect_AddDamageBonusPercent), EffectHandler(EffectsEnum.Effect_IncreaseDamage_1054), EffectHandler(EffectsEnum.Effect_AddRange), EffectHandler(EffectsEnum.Effect_AddHealth), EffectHandler(EffectsEnum.Effect_AddChance), EffectHandler(EffectsEnum.Effect_AddCriticalHit), EffectHandler(EffectsEnum.Effect_AddDamageBonus_121),
        EffectHandler(EffectsEnum.Effect_AddIntelligence), EffectHandler(EffectsEnum.Effect_AddVitality), EffectHandler(EffectsEnum.Effect_AddWisdom), EffectHandler(EffectsEnum.Effect_AddStrength), EffectHandler(EffectsEnum.Effect_AddPhysicalDamage_137), EffectHandler(EffectsEnum.Effect_AddLock), EffectHandler(EffectsEnum.Effect_AddDodge), 
        EffectHandler(EffectsEnum.Effect_AddDamageReflection), EffectHandler(EffectsEnum.Effect_AddPhysicalDamage_142), EffectHandler(EffectsEnum.Effect_AddPhysicalDamageReduction), EffectHandler(EffectsEnum.Effect_AddMagicDamageReduction), EffectHandler(EffectsEnum.Effect_AddPushDamageBonus), EffectHandler(EffectsEnum.Effect_AddCriticalDamageBonus),
        EffectHandler(EffectsEnum.Effect_AddPushDamageReduction), EffectHandler(EffectsEnum.Effect_AddCriticalDamageReduction), EffectHandler(EffectsEnum.Effect_IncreaseAPAvoid), EffectHandler(EffectsEnum.Effect_IncreaseMPAvoid), EffectHandler(EffectsEnum.Effect_AddResistances),
        EffectHandler(EffectsEnum.Effect_AddHealBonus), EffectHandler(EffectsEnum.Effect_AddProspecting), EffectHandler(EffectsEnum.Effect_AddEarthResistPercent), EffectHandler(EffectsEnum.Effect_AddWaterResistPercent), EffectHandler(EffectsEnum.Effect_AddAirResistPercent), EffectHandler(EffectsEnum.Effect_AddFireResistPercent), EffectHandler(EffectsEnum.Effect_AddNeutralResistPercent)]
    public class StatsBoost : SpellEffectHandler
    {
        public override void Apply()
        {          
            foreach (FightActor current in base.GetAffectedActors())
            {              
                if (Duration >= 0)
                {
                    Effect.GenerateEffect();
                    if (Effect.Id == EffectsEnum.Effect_AddResistances)
                    {
                        short value = (short)Effect.Value;
                        current.AddBuff(new StatsBuff(Caster, current, Spell, Effect, (short)Duration, true, StatsEnum.NeutralResistPercent, value));
                        current.AddBuff(new StatsBuff(Caster, current, Spell, Effect, (short)Duration, true, StatsEnum.EarthResistPercent, value));
                        current.AddBuff(new StatsBuff(Caster, current, Spell, Effect, (short)Duration, true, StatsEnum.WaterResistPercent, value));
                        current.AddBuff(new StatsBuff(Caster, current, Spell, Effect, (short)Duration, true, StatsEnum.AirResistPercent, value));
                        current.AddBuff(new StatsBuff(Caster, current, Spell, Effect, (short)Duration, true, StatsEnum.FireResistPercent, value));
                        continue;
                    }
                    current.AddBuff(new StatsBuff(Caster, current, Spell, Effect, (short)Duration, true, GetEffectCaracteristic(Effect.Id), (short)Effect.Value));
                }
            }
        }
        public StatsEnum GetEffectCaracteristic(EffectsEnum effect)
        {
            StatsEnum result;
            if (effect <= EffectsEnum.Effect_AddDamageBonusPercent)
            {
                switch (effect)
                {
                    case EffectsEnum.Effect_IncreaseAPAvoid:
                        return StatsEnum.DodgeAPProbability;
                    case EffectsEnum.Effect_IncreaseMPAvoid:
                        return StatsEnum.DodgeMPProbability;
                    case EffectsEnum.Effect_AddDamageReflection:
                        return StatsEnum.DamageReflection;
                    case EffectsEnum.Effect_HealHP_108:
                    case EffectsEnum.Effect_109:
                    case EffectsEnum.Effect_AddAP_111:
                    case EffectsEnum.Effect_DoubleDamageOrRestoreHP:
                    case EffectsEnum.Effect_AddDamageMultiplicator:
                    case EffectsEnum.Effect_SubRange:
                    case EffectsEnum.Effect_RegainAP:
                        goto IL_11A;
                    case EffectsEnum.Effect_AddDamageBonus:
                    case EffectsEnum.Effect_AddDamageBonus_121:
                        result = StatsEnum.DamageBonus;
                        return result;
                    case EffectsEnum.Effect_AddCriticalHit:
                        result = StatsEnum.CriticalHit;
                        return result;
                    case EffectsEnum.Effect_AddRange:
                        break;
                    case EffectsEnum.Effect_AddStrength:
                        result = StatsEnum.Strength;
                        return result;
                    case EffectsEnum.Effect_AddAgility:
                        result = StatsEnum.Agility;
                        return result;
                    case EffectsEnum.Effect_AddCriticalMiss:
                        result = StatsEnum.CriticalMiss;
                        return result;
                    case EffectsEnum.Effect_AddChance:
                        result = StatsEnum.Chance;
                        return result;
                    case EffectsEnum.Effect_AddWisdom:
                        result = StatsEnum.Wisdom;
                        return result;
                    case EffectsEnum.Effect_AddHealth:
                        result = StatsEnum.Health;
                        return result;
                    case EffectsEnum.Effect_AddVitality:
                        result = StatsEnum.Vitality;
                        return result;
                    case EffectsEnum.Effect_AddIntelligence:
                        result = StatsEnum.Intelligence;
                        return result;
                    default:
                        switch (effect)
                        {
                            case EffectsEnum.Effect_AddRange_136:
                                break;
                            case EffectsEnum.Effect_AddPhysicalDamage_137:
                            case EffectsEnum.Effect_AddPhysicalDamage_142:
                                result = StatsEnum.PhysicalDamage;
                                return result;
                            case EffectsEnum.Effect_AddPushDamageBonus:
                                result = StatsEnum.PushDamageBonus;
                                return result;
                            case EffectsEnum.Effect_AddPushDamageReduction:
                                result = StatsEnum.PushDamageReduction;
                                return result;
                            case EffectsEnum.Effect_IncreaseDamage_138:
                                goto IL_130;
                            case EffectsEnum.Effect_RestoreEnergyPoints:
                            case EffectsEnum.Effect_SkipTurn:
                            case EffectsEnum.Effect_Kill:
                                goto IL_11A;
                            default:
                                if (effect != EffectsEnum.Effect_AddDamageBonusPercent)
                                {
                                    goto IL_11A;
                                }
                                goto IL_130;
                        }
                        break;
                }
                result = StatsEnum.Range;
                return result;
            }
            switch (effect)
            {
                case EffectsEnum.Effect_AddSummonLimit:
                    result = StatsEnum.SummonLimit;
                    return result;
                case EffectsEnum.Effect_AddMagicDamageReduction:
                    result = StatsEnum.MagicDamageReduction;
                    return result;
                case EffectsEnum.Effect_AddPhysicalDamageReduction:
                    result = StatsEnum.PhysicalDamageReduction;
                    return result;
                case EffectsEnum.Effect_AddHealBonus:
                    result = StatsEnum.HealBonus;
                    return result;
                case EffectsEnum.Effect_AddProspecting:
                    result = StatsEnum.Prospecting;
                    return result;
                case EffectsEnum.Effect_AddEarthResistPercent:
                    result = StatsEnum.EarthResistPercent;
                    return result;
                case EffectsEnum.Effect_AddWaterResistPercent:
                    result = StatsEnum.WaterResistPercent;
                    return result;
                case EffectsEnum.Effect_AddAirResistPercent:
                    result = StatsEnum.AirResistPercent;
                    return result;
                case EffectsEnum.Effect_AddFireResistPercent:
                    result = StatsEnum.FireResistPercent;
                    return result;
                case EffectsEnum.Effect_AddNeutralResistPercent:
                    result = StatsEnum.NeutralResistPercent;
                    return result;
                default:
                    switch (effect)
                    {
                        case EffectsEnum.Effect_AddDodge:
                            result = StatsEnum.TackleEvade;
                            return result;
                        case EffectsEnum.Effect_AddLock:
                            result = StatsEnum.TackleBlock;
                            return result;
                        default:
                            if (effect == EffectsEnum.Effect_IncreaseDamage_1054)
                            {
                                goto IL_130;
                            }
                            break;
                    }
                    break;
            }
        IL_11A:
            throw new System.Exception(string.Format("'{0}' has no binded caracteristic", effect));
        IL_130:
            result = StatsEnum.DamageBonusPercent;
            return result;
        }
    }
}
