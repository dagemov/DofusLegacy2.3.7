using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Armor
{
    [EffectHandler(EffectsEnum.Effect_ReflectSpell)]
    public class DamageReflect : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (FightActor actor in GetAffectedActors())
                actor.AddBuff(new ReflectBuff(Caster, actor, Spell, Effect, (short)Duration, true));
        }
    }
}
