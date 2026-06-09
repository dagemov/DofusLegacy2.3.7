using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Diagnostics;
using Sunshine.WorldServer.Game.Spells;
using System;

namespace Sunshine.WorldServer.Game.Fights.Buffs.Spells
{
    public class HealOverTimeBuff : Buff
    {
        public short Value { get; private set; }

        public HealOverTimeBuff(FightActor caster, FightActor target, Spell spell, Effect effect, short duration, short value)
        {
            Id = caster.PopNextBuffId();
            Caster = caster;
            Target = target;
            Spell = spell;
            Effect = effect;
            Duration = duration;
            Value = value;
            Dispellable = true;
        }

        public override void Apply()
        {
            Tick();
        }

        public override void Dispell()
        {
        }

        public void Tick()
        {
            if (Target == null || Target.IsDead())
                return;

            Target.Heal(Value, Caster, true);
            FightCombatLogger.LogBuffTick(Target.Fight, Target, this, "HOT", Value, Duration);
        }

        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTemporaryBoostEffect(
                Id,
                Target != null ? Target.Id : 0,
                Duration > 500 ? (short)-1 : Duration,
                Convert.ToSByte(Dispellable ? 0 : 1),
                (short)(Spell != null ? Spell.Id : 0),
                Value);
        }
    }
}
