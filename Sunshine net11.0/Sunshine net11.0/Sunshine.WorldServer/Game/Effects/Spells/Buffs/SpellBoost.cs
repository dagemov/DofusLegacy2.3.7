using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Buffs
{
    [EffectHandler(EffectsEnum.Effect_SpellBoost)]
    public class SpellBoost : SpellEffectHandler
    {
        public override void Apply()
        {
            short spell = (short)Effect.DiceNum;
          
            if (Caster.Spells.GetSpell(spell) == null)
                return;

            Duration += 1;
            Caster.AddBuff(new SpellBuff(Caster, Caster, Spell, Effect, (short)Duration, false, Spell, (short)Value));
        }
    }
}
