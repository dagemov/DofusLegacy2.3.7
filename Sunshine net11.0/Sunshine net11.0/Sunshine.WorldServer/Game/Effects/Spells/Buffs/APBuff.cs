using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Buffs
{
    [EffectHandler(EffectsEnum.Effect_AddAP_111), EffectHandler(EffectsEnum.Effect_RegainAP)]
    public class APBuff : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var actor in GetAffectedActors())
            {
                Effect.GenerateEffect();

                if (Effect.Duration > 0)
                    actor.AddBuff(new StatsBuff(Caster, actor, Spell, Effect, (short)Duration, true, StatsEnum.AP, (short)Effect.Value));
                else
                    actor.RegainAP((short)Effect.Value);
            }
        }
    }
}
