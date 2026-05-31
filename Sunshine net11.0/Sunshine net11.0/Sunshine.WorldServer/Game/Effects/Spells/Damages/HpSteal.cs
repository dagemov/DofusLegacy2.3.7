using Sunshine.Protocol.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Damages
{
    [EffectHandler(EffectsEnum.Effect_StealHPAir), EffectHandler(EffectsEnum.Effect_StealHPNeutral), 
     EffectHandler(EffectsEnum.Effect_StealHPWater), EffectHandler(EffectsEnum.Effect_StealHPFire), 
     EffectHandler(EffectsEnum.Effect_StealHPEarth)]
    public class HpSteal : SpellEffectHandler
    {
        public HpSteal()
        {
        }

        public override void Apply()
        {
            var effectSchool = GetEffectSchool(Id);
            foreach (var actor in GetAffectedActors())
            {
                Damage damage = new Damage(effectSchool, DiceNum, DiceFace, Spell, Caster);
                actor.InflictDamage(damage);
                Caster.Heal(damage.Amount / 2, actor);
            }
        }

        private EffectSchoolEnum GetEffectSchool(EffectsEnum effect)
        {
            EffectSchoolEnum result;
            switch (effect)
            {
                case EffectsEnum.Effect_StealHPWater:
                    result = EffectSchoolEnum.Water;
                    break;
                case EffectsEnum.Effect_StealHPEarth:
                    result = EffectSchoolEnum.Earth;
                    break;
                case EffectsEnum.Effect_StealHPAir:
                    result = EffectSchoolEnum.Air;
                    break;
                case EffectsEnum.Effect_StealHPFire:
                    result = EffectSchoolEnum.Fire;
                    break;
                case EffectsEnum.Effect_StealHPNeutral:
                    result = EffectSchoolEnum.Neutral;
                    break;
                default:
                    throw new System.Exception(string.Format("Effect {0} has not associated School Type", effect));
            }
            return result;
        }
    }
}
