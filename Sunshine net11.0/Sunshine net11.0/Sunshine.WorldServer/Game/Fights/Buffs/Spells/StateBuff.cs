using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Fights.Buffs.Spells
{
    public class StateBuff : Buff
    {
        public SpellStatesEnum State { get; set; }

        public StateBuff(FightActor caster, FightActor target, Spell spell, Effect effect,
            short duration, bool dispellable, SpellStatesEnum state, short actionId)
        {
            Id = caster.PopNextBuffId();
            Caster = caster;
            Target = target;
            State = state;
            Spell = spell;
            Effect = effect;
            Duration = duration;
            Dispellable = dispellable;
            ActionId = actionId;
        }

        public StateBuff(FightActor caster, FightActor target, Spell spell, Effect effect,
            short duration, bool dispellable, SpellStatesEnum state)
        {
            Id = caster.PopNextBuffId();
            Caster = caster;
            Target = target;
            State = state;
            Spell = spell;
            Effect = effect;
            Duration = duration;
            Dispellable = dispellable;
        }

        public override void Apply()
        {
            Target.AddState(State);
        }

        public override void Dispell()
        {
            Target.RemoveState(State);
        }

        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTemporaryBoostStateEffect(Id, Target.Id, Duration > 500 ? (short)-1 : Duration, Convert.ToSByte(Dispellable ? 0 : 1), (short)Spell.Id, 1, (short)State);
        }
    }
}
