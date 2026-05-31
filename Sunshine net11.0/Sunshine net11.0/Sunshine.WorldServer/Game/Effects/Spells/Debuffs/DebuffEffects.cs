using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Fights.Buffs;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Debuffs
{
    [EffectHandler(EffectsEnum.Effect_Debuff)]
    public class DebuffEffects : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var actor in GetAffectedActors())
            {
                if (actor == null)
                    continue;

                Buff[] buffs = Value > 0
                    ? actor.GetBuffs().Where(x => x != null && x.Spell != null && x.Spell.Id == Value && x.Dispellable).ToArray()
                    : actor.GetBuffs().Where(x => x != null && x.Dispellable).ToArray();

                foreach (var buff in buffs)
                    actor.RemoveBuff(buff);

                if (Value > 0 && buffs.Length > 0)
                    ContextHandler.SendGameActionFightDispellSpellMessage(actor.Fight.Clients, (short)Effect.Id, Caster, actor, (ushort)Value);
            }
        }
    }
}

