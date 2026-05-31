using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using Sunshine.WorldServer.Game.Fights.Mechanics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Buffs
{
    [EffectHandler(EffectsEnum.Effect_SubMP)]
    public class MPDebuffFix : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var actor in GetAffectedActors())
            {
                if (actor == null || !actor.IsAlive)
                    continue;

                if (FrigostBossMechanics.TryApplyHamrackMpLossBoost(actor, Caster, Spell, Effect))
                    continue;

                Effect.GenerateEffect();

                if (Effect.Duration > 1)
                    actor.AddBuff(new StatsBuff(Caster, actor, Spell, Effect, (short)Duration, true, StatsEnum.MP, (short)-Effect.Value, true));
                else
                    actor.LostMP((short)Effect.Value);
            }
        }
    }
}
