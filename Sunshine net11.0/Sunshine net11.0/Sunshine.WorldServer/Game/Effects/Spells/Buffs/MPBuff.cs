using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Buffs
{
    [EffectHandler(EffectsEnum.Effect_AddMP), EffectHandler(EffectsEnum.Effect_AddMP_128)]
    public class MPBuff : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach(var actor in GetAffectedActors())
            {
                Effect.GenerateEffect();

                if (Effect.Duration > 0)
                    actor.AddBuff(new StatsBuff(Caster, actor, Spell, Effect, (short)Duration, true, StatsEnum.MP, (short)Effect.Value));
                else
                    actor.RegainMP((short)Effect.Value);
            }
        }
    }
}
