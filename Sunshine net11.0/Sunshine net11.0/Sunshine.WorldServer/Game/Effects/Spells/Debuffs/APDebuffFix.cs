using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Buffs
{
    [EffectHandler(EffectsEnum.Effect_SubAP), EffectHandler(EffectsEnum.Effect_SubAP_1079)]
    public class APDebuffFix : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var actor in GetAffectedActors())
            {
                Effect.GenerateEffect();

                if (Effect.Duration > 1)
                    actor.AddBuff(new StatsBuff(Caster, actor, Spell, Effect, (short)Duration, true, StatsEnum.AP, (short)-Effect.Value, true));
                else
                    actor.LostAP((short)Effect.Value);
            }
        }
    }
}
