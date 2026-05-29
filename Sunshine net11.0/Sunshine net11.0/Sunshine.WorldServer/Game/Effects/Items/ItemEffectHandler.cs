using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Items
{
    public static class ItemEffectHandler
    {
        public static readonly HashSet<EffectsEnum> IgnoredEffectsForStats = new HashSet<EffectsEnum>()
        {
            EffectsEnum.Effect_10,
            EffectsEnum.Effect_DamagePercentWater,
            EffectsEnum.Effect_DamagePercentEarth,
            EffectsEnum.Effect_DamagePercentAir,
            EffectsEnum.Effect_DamagePercentFire,
            EffectsEnum.Effect_DamagePercentNeutral,
            EffectsEnum.Effect_StealHPWater,
            EffectsEnum.Effect_StealHPEarth,
            EffectsEnum.Effect_StealHPAir,
            EffectsEnum.Effect_StealHPFire,
            EffectsEnum.Effect_StealHPNeutral,
            EffectsEnum.Effect_DamageWater,
            EffectsEnum.Effect_DamageEarth,
            EffectsEnum.Effect_DamageAir,
            EffectsEnum.Effect_DamageFire,
            EffectsEnum.Effect_DamageNeutral,
            EffectsEnum.Effect_StealAP_84,
            EffectsEnum.Effect_RemoveAP,
            EffectsEnum.Effect_StealMP_77,
            EffectsEnum.Effect_LosingAP,
            EffectsEnum.Effect_LosingMP,
            EffectsEnum.Effect_LivingObjectId,
            EffectsEnum.Effect_LivingObjectMood,
            EffectsEnum.Effect_LivingObjectSkin,
            EffectsEnum.Effect_LivingObjectCategory,
            EffectsEnum.Effect_LivingObjectLevel,
            EffectsEnum.Effect_NonExchangeable_981,
            EffectsEnum.Effect_NonExchangeable_982,
        };

        public static readonly HashSet<EffectsEnum> NegativeEffectsForStats = new HashSet<EffectsEnum>()
        {
            EffectsEnum.Effect_SubInitiative,
            EffectsEnum.Effect_SubProspecting,
            EffectsEnum.Effect_SubStrength,
            EffectsEnum.Effect_SubVitality,
            EffectsEnum.Effect_SubChance,
            EffectsEnum.Effect_SubAgility,
            EffectsEnum.Effect_SubIntelligence,
            EffectsEnum.Effect_SubWisdom,
            EffectsEnum.Effect_SubRange,
            EffectsEnum.Effect_SubRange_135,
            EffectsEnum.Effect_SubCriticalHit,
            EffectsEnum.Effect_SubHealBonus,
            EffectsEnum.Effect_SubDamageBonus,
            EffectsEnum.Effect_SubDamageBonusPercent,
            EffectsEnum.Effect_SubLock,
            EffectsEnum.Effect_SubDodge,
            EffectsEnum.Effect_SubDodgeAPProbability,
            EffectsEnum.Effect_SubDodgeMPProbability,
            EffectsEnum.Effect_SubPushDamageBonus,
            EffectsEnum.Effect_SubPushDamageReduction,
            EffectsEnum.Effect_SubCriticalDamageBonus,
            EffectsEnum.Effect_SubCriticalDamageReduction,
            EffectsEnum.Effect_SubNeutralDamageBonus,
            EffectsEnum.Effect_SubEarthDamageBonus,
            EffectsEnum.Effect_SubWaterDamageBonus,
            EffectsEnum.Effect_SubAirDamageBonus,
            EffectsEnum.Effect_SubFireDamageBonus,
            EffectsEnum.Effect_SubAirResistPercent,
            EffectsEnum.Effect_SubEarthResistPercent,
            EffectsEnum.Effect_SubWaterResistPercent,
        };

        public static Dictionary<EffectsEnum, StatsEnum> EffectsRelations = new Dictionary<EffectsEnum, StatsEnum>()
        {
            {EffectsEnum.Effect_AddAP_111, StatsEnum.AP},
            {EffectsEnum.Effect_AddDamageBonus, StatsEnum.DamageBonus},
            {EffectsEnum.Effect_AddDamageBonus_121, StatsEnum.DamageBonus},
            {EffectsEnum.Effect_AddTrapBonus, StatsEnum.TrapBonus},
            {EffectsEnum.Effect_AddTrapBonusPercent, StatsEnum.TrapBonusPercent},
            {EffectsEnum.Effect_AddSummonLimit, StatsEnum.SummonLimit},
            {EffectsEnum.Effect_IncreaseDamage_138, StatsEnum.DamageBonusPercent},
            {EffectsEnum.Effect_AddDamageBonusPercent, StatsEnum.DamageBonusPercent},
            {EffectsEnum.Effect_AddMP, StatsEnum.MP},
            {EffectsEnum.Effect_AddMP_128, StatsEnum.MP},
            {EffectsEnum.Effect_AddRange, StatsEnum.Range},
            {EffectsEnum.Effect_AddRange_136, StatsEnum.Range},
            {EffectsEnum.Effect_AddProspecting, StatsEnum.Prospecting},
            {EffectsEnum.Effect_IncreaseWeight, StatsEnum.Weight},
            {EffectsEnum.Effect_AddLock, StatsEnum.TackleBlock},
            {EffectsEnum.Effect_AddDodge, StatsEnum.TackleEvade},
            {EffectsEnum.Effect_IncreaseAPAvoid, StatsEnum.DodgeAPProbability},
            {EffectsEnum.Effect_IncreaseMPAvoid, StatsEnum.DodgeMPProbability},
            {EffectsEnum.Effect_412, StatsEnum.MPAttack},
            {EffectsEnum.Effect_APAttack, StatsEnum.APAttack},
            {EffectsEnum.Effect_AddHealth, StatsEnum.Health},
            {EffectsEnum.Effect_AddHealBonus, StatsEnum.HealBonus},
            {EffectsEnum.Effect_AddInitiative, StatsEnum.Initiative},
            {EffectsEnum.Effect_AddIntelligence, StatsEnum.Intelligence},
            {EffectsEnum.Effect_AddAgility, StatsEnum.Agility},
            {EffectsEnum.Effect_AddChance, StatsEnum.Chance},
            {EffectsEnum.Effect_AddVitality, StatsEnum.Vitality},
            {EffectsEnum.Effect_AddStrength, StatsEnum.Strength},
            {EffectsEnum.Effect_AddWisdom, StatsEnum.Wisdom},
            {EffectsEnum.Effect_AddAirDamageBonus, StatsEnum.AirDamageBonus},
            {EffectsEnum.Effect_AddFireDamageBonus, StatsEnum.FireDamageBonus},
            {EffectsEnum.Effect_AddWaterDamageBonus, StatsEnum.WaterDamageBonus},
            {EffectsEnum.Effect_AddEarthDamageBonus, StatsEnum.EarthDamageBonus},
            {EffectsEnum.Effect_AddNeutralDamageBonus, StatsEnum.NeutralDamageBonus},
            {EffectsEnum.Effect_AddCriticalHit, StatsEnum.CriticalHit},
            {EffectsEnum.Effect_AddCriticalMiss, StatsEnum.CriticalMiss},
            {EffectsEnum.Effect_AddCriticalDamageBonus, StatsEnum.CriticalDamageBonus},
            {EffectsEnum.Effect_AddPushDamageBonus, StatsEnum.PushDamageBonus},
            {EffectsEnum.Effect_AddPushDamageReduction, StatsEnum.PushDamageReduction},
            {EffectsEnum.Effect_AddCriticalDamageReduction, StatsEnum.CriticalDamageReduction},
            {EffectsEnum.Effect_SubInitiative, StatsEnum.Initiative},
            {EffectsEnum.Effect_SubProspecting, StatsEnum.Prospecting},
            {EffectsEnum.Effect_SubStrength, StatsEnum.Strength},
            {EffectsEnum.Effect_SubVitality, StatsEnum.Vitality},
            {EffectsEnum.Effect_SubChance, StatsEnum.Chance},
            {EffectsEnum.Effect_SubAgility, StatsEnum.Agility},
            {EffectsEnum.Effect_SubIntelligence, StatsEnum.Intelligence},
            {EffectsEnum.Effect_SubWisdom, StatsEnum.Wisdom},
            {EffectsEnum.Effect_SubRange, StatsEnum.Range},
            {EffectsEnum.Effect_SubRange_135, StatsEnum.Range},
            {EffectsEnum.Effect_SubCriticalHit, StatsEnum.CriticalHit},
            {EffectsEnum.Effect_SubHealBonus, StatsEnum.HealBonus},
            {EffectsEnum.Effect_SubDamageBonus, StatsEnum.DamageBonus},
            {EffectsEnum.Effect_SubDamageBonusPercent, StatsEnum.DamageBonusPercent},
            {EffectsEnum.Effect_SubLock, StatsEnum.TackleBlock},
            {EffectsEnum.Effect_SubDodge, StatsEnum.TackleEvade},
            {EffectsEnum.Effect_SubDodgeAPProbability, StatsEnum.DodgeAPProbability},
            {EffectsEnum.Effect_SubDodgeMPProbability, StatsEnum.DodgeMPProbability},
            {EffectsEnum.Effect_SubPushDamageBonus, StatsEnum.PushDamageBonus},
            {EffectsEnum.Effect_SubPushDamageReduction, StatsEnum.PushDamageReduction},
            {EffectsEnum.Effect_SubCriticalDamageBonus, StatsEnum.CriticalDamageBonus},
            {EffectsEnum.Effect_SubCriticalDamageReduction, StatsEnum.CriticalDamageReduction},
            {EffectsEnum.Effect_SubNeutralDamageBonus, StatsEnum.NeutralDamageBonus},
            {EffectsEnum.Effect_SubEarthDamageBonus, StatsEnum.EarthDamageBonus},
            {EffectsEnum.Effect_SubWaterDamageBonus, StatsEnum.WaterDamageBonus},
            {EffectsEnum.Effect_SubAirDamageBonus, StatsEnum.AirDamageBonus},
            {EffectsEnum.Effect_SubFireDamageBonus, StatsEnum.FireDamageBonus},
            {EffectsEnum.Effect_AddNeutralResistPercent, StatsEnum.NeutralResistPercent},
            {EffectsEnum.Effect_AddEarthResistPercent, StatsEnum.EarthResistPercent},
            {EffectsEnum.Effect_AddFireResistPercent, StatsEnum.FireResistPercent},
            {EffectsEnum.Effect_AddWaterResistPercent, StatsEnum.WaterResistPercent},
            {EffectsEnum.Effect_AddAirResistPercent, StatsEnum.AirResistPercent},
            {EffectsEnum.Effect_SubAirResistPercent, StatsEnum.AirResistPercent},
            {EffectsEnum.Effect_SubEarthResistPercent, StatsEnum.EarthResistPercent},
            {EffectsEnum.Effect_SubWaterResistPercent, StatsEnum.WaterResistPercent},
            {EffectsEnum.Effect_AddPvpNeutralResistPercent, StatsEnum.PvpNeutralResistPercent},
            {EffectsEnum.Effect_AddPvpEarthResistPercent, StatsEnum.PvpEarthResistPercent},
            {EffectsEnum.Effect_AddPvpFireResistPercent, StatsEnum.PvpFireResistPercent},
            {EffectsEnum.Effect_AddPvpWaterResistPercent, StatsEnum.PvpWaterResistPercent},
            {EffectsEnum.Effect_AddPvpAirResistPercent, StatsEnum.PvpAirResistPercent},
            {EffectsEnum.Effect_AddAirElementReduction, StatsEnum.AirElementReduction},
            {EffectsEnum.Effect_AddFireElementReduction, StatsEnum.FireElementReduction},
            {EffectsEnum.Effect_AddWaterElementReduction, StatsEnum.WaterElementReduction},
            {EffectsEnum.Effect_AddNeutralElementReduction, StatsEnum.NeutralElementReduction},
            {EffectsEnum.Effect_AddEarthElementReduction, StatsEnum.EarthElementReduction},
            {EffectsEnum.Effect_AddPvpAirElementReduction, StatsEnum.PvpAirElementReduction},
            {EffectsEnum.Effect_AddPvpEarthElementReduction, StatsEnum.PvpEarthElementReduction},
            {EffectsEnum.Effect_AddPvpNeutralElementReduction, StatsEnum.PvpNeutralElementReduction},
            {EffectsEnum.Effect_AddPvpFireElementReduction,StatsEnum.PvpFireElementReduction},
            {EffectsEnum.Effect_AddPvpWaterElementReduction, StatsEnum.PvpWaterElementReduction},
        };

        public static bool TryGetRelatedStat(EffectsEnum effectId, out StatsEnum stat)
        {
            return EffectsRelations.TryGetValue(effectId, out stat);
        }

        public static bool IsNegativeEffectForStats(EffectsEnum effectId)
        {
            return NegativeEffectsForStats.Contains(effectId);
        }

        public static bool ShouldIgnoreEffectForStats(EffectsEnum effectId)
        {
            return IgnoredEffectsForStats.Contains(effectId);
        }
    }
}
