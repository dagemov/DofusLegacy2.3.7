using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Fights.Buffs;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using Sunshine.WorldServer.Game.Fights.Mechanics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Heals
{
    [EffectHandler(EffectsEnum.Effect_HealHP_81), EffectHandler(EffectsEnum.Effect_HealHP_143), EffectHandler(EffectsEnum.Effect_HealHP_108)]
    public class Heal : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var actor in GetAffectedActors())
            {
                Effect.GenerateEffect();
                if (Duration > 0)
                {
                    var hotBuff = new HealOverTimeBuff(Caster, actor, Spell, Effect, (short)Duration, (short)Effect.Value);
                    actor.AddBuff(hotBuff);
                }
                else
                {
                    actor.Heal(Effect.Value, base.Caster, true);
                    FrigostBossMechanics.OnActorHealed(Caster, actor);
                }
            }
        }
    }
}

