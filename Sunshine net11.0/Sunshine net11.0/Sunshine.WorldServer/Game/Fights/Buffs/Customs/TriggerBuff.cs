using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Spells;
using System;

namespace Sunshine.WorldServer.Game.Fights.Buffs.Customs
{
    [Flags]
    public enum BuffTriggerType
    {
        NONE = 0,
        BUFF_ADDED = 1,
        TURN_BEGIN = 2,
        TURN_END = 4,
        MOVE = 8,
        BEFORE_ATTACKED = 16,
        AFTER_ATTACKED = 32,
        BEFORE_ATTACK = 64,
        AFTER_ATTACK = 128,
        BUFF_ENDED = 256,
        BUFF_ENDED_TURNEND = 512,
        AFTER_DEATH = 1024
    }

    public delegate void TriggerBuffCallback(TriggerBuff buff, BuffTriggerType trigger, object token);

    public sealed class TriggerBuff : Buff
    {
        private bool _isTriggering;

        public TriggerBuff(int id, FightActor target, FightActor caster, Effect effect, Spell spell, BuffTriggerType triggerType, TriggerBuffCallback callback)
        {
            Id = id;
            Target = target;
            Caster = caster;
            Effect = effect;
            Spell = spell;
            TriggerType = triggerType;
            Callback = callback;
            Dispellable = true;

            var rawType = (int)triggerType;
            Type = Enum.IsDefined(typeof(BuffTypeEnum), rawType) ? (BuffTypeEnum)rawType : BuffTypeEnum.UNKNOWN;
        }

        public BuffTriggerType TriggerType { get; }

        public TriggerBuffCallback Callback { get; }

        public bool TriggersOn(BuffTriggerType trigger)
        {
            return trigger != BuffTriggerType.NONE && (TriggerType & trigger) == trigger;
        }

        public void TryTrigger(BuffTriggerType trigger, object token = null)
        {
            if (_isTriggering || !TriggersOn(trigger) || Callback == null)
                return;

            try
            {
                _isTriggering = true;
                Callback(this, trigger, token);
            }
            finally
            {
                _isTriggering = false;
            }
        }

        public override void Apply()
        {
        }

        public override void Dispell()
        {
        }

        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTemporaryBoostEffect(
                Id,
                Target != null ? Target.Id : 0,
                Duration > 500 ? (short)-1 : Duration,
                Convert.ToSByte(Dispellable ? 0 : 1),
                (short)(Spell != null ? Spell.Id : 0),
                0);
        }
    }
}
