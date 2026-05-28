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
    public class InvisibilityBuff : Buff
    {
        public InvisibilityBuff(FightActor caster, FightActor target, Spell spell, Effect effect,
            short duration, bool dispellable, short actionId)
        {
            Id = caster.PopNextBuffId();
            Caster = caster;
            Target = target;
            Spell = spell;
            Effect = effect;
            Duration = duration;
            Dispellable = dispellable;
            ActionId = actionId;
        }

        public InvisibilityBuff(FightActor caster, FightActor target, Spell spell, Effect effect,
            short duration, bool dispellable)
        {
            Id = caster.PopNextBuffId();
            Caster = caster;
            Target = target;
            Spell = spell;
            Effect = effect;
            Duration = duration;
            Dispellable = dispellable;
        }

        public override void Apply()
        {
            Target.SetInvisibilityState(GameActionFightInvisibilityStateEnum.INVISIBLE, Caster);
        }

        public override void Dispell()
        {
            Target.SetInvisibilityState(GameActionFightInvisibilityStateEnum.VISIBLE, Caster);
        }

        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTemporaryBoostEffect(Id, Target.Id, Duration > 500 ? (short)-1 : Duration, Convert.ToSByte(Dispellable ? 0 : 1), (short)Spell.Id, 1);
        }
    }
}
