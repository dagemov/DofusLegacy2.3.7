using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Fights.Buffs;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Debuffs
{
    [EffectHandler(EffectsEnum.Effect_ReducCooldown)]
    public class ReducCooldown : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var actor in GetAffectedActors())
            {
                ushort spell = (ushort)Effect.DiceNum;
                if (actor.CustomCoolDown.ContainsKey(spell))
                    actor.CustomCoolDown.Remove(spell);

                actor.CustomCoolDown.Add(spell, (byte)Value);

                ActionsHandler.SendGameActionFightSpellCooldownVariationMessage(actor.Fight.Clients, Caster, actor, spell, (short)Value);
            }
        }
    }
}

