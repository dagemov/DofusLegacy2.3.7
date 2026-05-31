using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Fights.Buffs.Spells
{
    public class ReflectBuff : Buff
    {
        public ReflectBuff(FightActor caster, FightActor target, Spell spell, Effect effect,
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

        public ReflectBuff(FightActor caster, FightActor target, Spell spell, Effect effect,
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
            Target.HasReflectBuff = true;
        }

        public override void Dispell()
        {
            Target.HasReflectBuff = false;
        }

        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTriggeredEffect(Id, Target.Id, Duration > 500 ? (short)-1 : Duration, Convert.ToSByte(Dispellable ? 0 : 1), (short)Spell.Id, (int)Effect.Value, (int)Effect.DiceFace, (int)Effect.DiceNum, (short)Effect.Delay);
        }
    }
}
