using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Armor
{
    [EffectHandler(EffectsEnum.Effect_AddGlobalDamageReduction_105), EffectHandler(EffectsEnum.Effect_AddArmorDamageReduction)]
    public class DamageReduction : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var actor in GetAffectedActors())
            {
                StatsEnum[] armors = GetAssociatedCaracteristics(Spell.Id);

                if (armors.Length > 1)
                    actor.AddBuff(new StatsBuff(Caster, actor, Spell, Effect, (short)Duration, true, armors[0], armors[1], (short)Effect.DiceNum));
                else
                    actor.AddBuff(new StatsBuff(Caster, actor, Spell, Effect, (short)Duration, true, armors[0], (short)Effect.DiceNum));
            }
           
        }

        public StatsEnum[] GetAssociatedCaracteristics(int spellId)
        {
            switch(spellId)
            {
                case 18:
                    return new StatsEnum[] { StatsEnum.WaterDamageArmor };
                case 1:
                    return new StatsEnum[] { StatsEnum.FireDamageArmor };
                case 6:
                    return new StatsEnum[] { StatsEnum.EarthDamageArmor, StatsEnum.NeutralDamageArmor };
                case 14:
                    return new StatsEnum[] { StatsEnum.AirDamageArmor };
                default:
                    return new StatsEnum[] { StatsEnum.GlobalDamageReduction };
            }
        }
    }
}
