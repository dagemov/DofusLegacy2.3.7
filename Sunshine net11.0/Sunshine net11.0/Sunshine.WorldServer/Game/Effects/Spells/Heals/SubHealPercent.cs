using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Fights.Buffs;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Debuffs
{
    [EffectHandler(EffectsEnum.Effect_SubHealPercent), EffectHandler(EffectsEnum.Effect_SubHealPercent_1033)]
    public class SubHealPercent : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var actor in GetAffectedActors())
            {
                Effect.GenerateEffect();
                short value = (short)(actor.Stats.Health.Total * (Effect.Value / 100.0));
                actor.AddBuff(new StatsBuff(Caster, actor, Spell, Effect, (short)Duration, true, StatsEnum.Health, (short)-value, true));
            }
        }
    }
}

