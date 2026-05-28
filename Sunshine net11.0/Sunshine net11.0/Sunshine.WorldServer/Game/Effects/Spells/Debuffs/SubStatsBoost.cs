using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Debuffs
{
    public class SubStatsBoost
    {
        [EffectHandler(EffectsEnum.Effect_SubLock), EffectHandler(EffectsEnum.Effect_SubDodgeAPProbability), EffectHandler(EffectsEnum.Effect_SubDamageBonusPercent), EffectHandler(EffectsEnum.Effect_SubDodge), EffectHandler(EffectsEnum.Effect_SubStrength), EffectHandler(EffectsEnum.Effect_SubChance), EffectHandler(EffectsEnum.Effect_SubDamageBonus), EffectHandler(EffectsEnum.Effect_SubRange_135), EffectHandler(EffectsEnum.Effect_SubDodgeMPProbability), EffectHandler(EffectsEnum.Effect_SubWisdom), EffectHandler(EffectsEnum.Effect_SubCriticalHit), EffectHandler(EffectsEnum.Effect_SubIntelligence), EffectHandler(EffectsEnum.Effect_SubAgility), EffectHandler(EffectsEnum.Effect_SubRange), EffectHandler(EffectsEnum.Effect_SubVitality),
         EffectHandler(EffectsEnum.Effect_SubHealBonus), EffectHandler(EffectsEnum.Effect_SubNeutralResistPercent), EffectHandler(EffectsEnum.Effect_SubEarthResistPercent), EffectHandler(EffectsEnum.Effect_SubWaterResistPercent), EffectHandler(EffectsEnum.Effect_SubAirResistPercent), EffectHandler(EffectsEnum.Effect_SubFireResistPercent),
         EffectHandler(EffectsEnum.Effect_SubPhysicalDamageReduction), EffectHandler(EffectsEnum.Effect_SubMagicDamageReduction), EffectHandler(EffectsEnum.Effect_SubResistances)]
        public class StatsDebuff : SpellEffectHandler
        {
            public override void Apply()
            {
                foreach (FightActor current in base.GetAffectedActors())
                {
                    if (Duration >= 0)
                    {
                        Effect.GenerateEffect();
                        if (Effect.Id == EffectsEnum.Effect_SubResistances)
                        {
                            short value = (short)-Effect.Value;
                            current.AddBuff(new StatsBuff(Caster, current, Spell, Effect, (short)Duration, true, StatsEnum.NeutralResistPercent, value, true));
                            current.AddBuff(new StatsBuff(Caster, current, Spell, Effect, (short)Duration, true, StatsEnum.EarthResistPercent, value, true));
                            current.AddBuff(new StatsBuff(Caster, current, Spell, Effect, (short)Duration, true, StatsEnum.WaterResistPercent, value, true));
                            current.AddBuff(new StatsBuff(Caster, current, Spell, Effect, (short)Duration, true, StatsEnum.AirResistPercent, value, true));
                            current.AddBuff(new StatsBuff(Caster, current, Spell, Effect, (short)Duration, true, StatsEnum.FireResistPercent, value, true));
                            continue;
                        }
                        current.AddBuff(new StatsBuff(Caster, current, Spell, Effect, (short)Duration, true, GetEffectCaracteristic(Effect.Id), (short)-Effect.Value, true));
                    }
                }
            }
            public static StatsEnum GetEffectCaracteristic(EffectsEnum effect)
            {
                switch (effect)
                {
                    case EffectsEnum.Effect_SubRange:
                    case EffectsEnum.Effect_SubRange_135:
                    case EffectsEnum.Effect_StealRange:
                        return StatsEnum.Range;

                    case EffectsEnum.Effect_SubDamageBonus:
                        return StatsEnum.DamageBonus;

                    case EffectsEnum.Effect_SubChance:
                    case EffectsEnum.Effect_StealChance:
                        return StatsEnum.Chance;

                    case EffectsEnum.Effect_SubVitality:
                    case EffectsEnum.Effect_StealVitality:
                        return StatsEnum.Vitality;

                    case EffectsEnum.Effect_SubAgility:
                    case EffectsEnum.Effect_StealAgility:
                        return StatsEnum.Agility;

                    case EffectsEnum.Effect_SubIntelligence:
                    case EffectsEnum.Effect_StealIntelligence:
                        return StatsEnum.Intelligence;

                    case EffectsEnum.Effect_SubWisdom:
                    case EffectsEnum.Effect_StealWisdom:
                        return StatsEnum.Wisdom;

                    case EffectsEnum.Effect_SubStrength:
                    case EffectsEnum.Effect_StealStrength:
                        return StatsEnum.Strength;

                    case EffectsEnum.Effect_SubDodgeAPProbability:
                        return StatsEnum.DodgeAPProbability;

                    case EffectsEnum.Effect_SubDodgeMPProbability:
                        return StatsEnum.DodgeMPProbability;

                    case EffectsEnum.Effect_SubCriticalHit:
                        return StatsEnum.CriticalHit;

                    case EffectsEnum.Effect_SubDamageBonusPercent:
                        return StatsEnum.DamageBonusPercent;

                    case EffectsEnum.Effect_SubDodge:
                        return StatsEnum.TackleEvade;

                    case EffectsEnum.Effect_SubLock:
                        return StatsEnum.TackleBlock;

                    case EffectsEnum.Effect_SubHealBonus:
                        return StatsEnum.HealBonus;

                    case EffectsEnum.Effect_SubNeutralResistPercent:
                        return StatsEnum.NeutralResistPercent;

                    case EffectsEnum.Effect_SubEarthResistPercent:
                        return StatsEnum.EarthResistPercent;

                    case EffectsEnum.Effect_SubWaterResistPercent:
                        return StatsEnum.WaterResistPercent;

                    case EffectsEnum.Effect_SubAirResistPercent:
                        return StatsEnum.AirResistPercent;

                    case EffectsEnum.Effect_SubFireResistPercent:
                        return StatsEnum.FireResistPercent;

                    case EffectsEnum.Effect_SubPhysicalDamageReduction:
                        return StatsEnum.PhysicalDamageReduction;

                    case EffectsEnum.Effect_SubMagicDamageReduction:
                        return StatsEnum.MagicDamageReduction;

                    default:
                        throw new System.Exception(string.Format("'{0}' has no binded caracteristic", effect));
                }
            }
        }
    }

    [EffectHandler(EffectsEnum.Effect_StealChance)]
    [EffectHandler(EffectsEnum.Effect_StealVitality)]
    [EffectHandler(EffectsEnum.Effect_StealWisdom)]
    [EffectHandler(EffectsEnum.Effect_StealIntelligence)]
    [EffectHandler(EffectsEnum.Effect_StealAgility)]
    [EffectHandler(EffectsEnum.Effect_StealStrength)]
    [EffectHandler(EffectsEnum.Effect_StealRange)]
    public class StatsSteal : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var target in GetAffectedActors())
            {
                if (target == null || !target.IsAlive)
                    continue;

                Effect.GenerateEffect();
                short value = (short)Effect.Value;
                if (value <= 0)
                    continue;

                var characteristic = SubStatsBoost.StatsDebuff.GetEffectCaracteristic(Effect.Id);
                short duration = (short)Duration;

                target.AddBuff(new StatsBuff(
                    Caster,
                    target,
                    Spell,
                    Effect,
                    duration,
                    true,
                    characteristic,
                    (short)-value,
                    GetDisplayedDebuffEffectId(Effect.Id),
                    true));

                Caster.AddBuff(new StatsBuff(
                    Caster,
                    Caster,
                    Spell,
                    Effect,
                    duration,
                    true,
                    characteristic,
                    value,
                    GetDisplayedBuffEffectId(Effect.Id)));
            }
        }

        private static short GetDisplayedBuffEffectId(EffectsEnum effect)
        {
            switch (effect)
            {
                case EffectsEnum.Effect_StealChance:
                    return (short)EffectsEnum.Effect_AddChance;
                case EffectsEnum.Effect_StealVitality:
                    return (short)EffectsEnum.Effect_AddVitality;
                case EffectsEnum.Effect_StealWisdom:
                    return (short)EffectsEnum.Effect_AddWisdom;
                case EffectsEnum.Effect_StealIntelligence:
                    return (short)EffectsEnum.Effect_AddIntelligence;
                case EffectsEnum.Effect_StealAgility:
                    return (short)EffectsEnum.Effect_AddAgility;
                case EffectsEnum.Effect_StealStrength:
                    return (short)EffectsEnum.Effect_AddStrength;
                case EffectsEnum.Effect_StealRange:
                    return (short)EffectsEnum.Effect_AddRange;
                default:
                    return (short)effect;
            }
        }

        private static short GetDisplayedDebuffEffectId(EffectsEnum effect)
        {
            switch (effect)
            {
                case EffectsEnum.Effect_StealChance:
                    return (short)EffectsEnum.Effect_SubChance;
                case EffectsEnum.Effect_StealVitality:
                    return (short)EffectsEnum.Effect_SubVitality;
                case EffectsEnum.Effect_StealWisdom:
                    return (short)EffectsEnum.Effect_SubWisdom;
                case EffectsEnum.Effect_StealIntelligence:
                    return (short)EffectsEnum.Effect_SubIntelligence;
                case EffectsEnum.Effect_StealAgility:
                    return (short)EffectsEnum.Effect_SubAgility;
                case EffectsEnum.Effect_StealStrength:
                    return (short)EffectsEnum.Effect_SubStrength;
                case EffectsEnum.Effect_StealRange:
                    return (short)EffectsEnum.Effect_SubRange;
                default:
                    return (short)effect;
            }
        }
    }

}
