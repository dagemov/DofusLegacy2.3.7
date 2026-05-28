using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.States
{
    [EffectHandler(EffectsEnum.Effect_Invisibility)]
    public class Invisibility : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (FightActor current in base.GetAffectedActors())
            {
                current.AddBuff(new InvisibilityBuff(Caster, current, Spell, Effect, (short)Duration, true));
            }
        }
    }
}
