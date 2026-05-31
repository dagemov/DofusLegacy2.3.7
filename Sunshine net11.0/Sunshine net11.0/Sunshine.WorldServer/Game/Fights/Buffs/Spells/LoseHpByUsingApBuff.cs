using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Spells;
using System;

namespace Sunshine.WorldServer.Game.Fights.Buffs.Spells
{
    public class LoseHpByUsingApBuff : Buff
    {
        public int DamagePerAp { get; private set; }

        public LoseHpByUsingApBuff(FightActor caster, FightActor target, Spell spell, Effect effect, short duration, int damagePerAp)
        {
            Id = caster.PopNextBuffId();
            Caster = caster;
            Target = target;
            Spell = spell;
            Effect = effect;
            Duration = duration;
            Dispellable = true;
            DamagePerAp = Math.Max(0, damagePerAp);
        }

        public override void Apply()
        {
        }

        public override void Dispell()
        {
        }

        public void OnApUsed(short usedAp)
        {
            if (usedAp <= 0 || DamagePerAp <= 0 || Target == null || Target.IsDead())
                return;

            int amount = DamagePerAp * usedAp;
            Target.InflictDamage(amount, Caster ?? Target, true);
        }

        public override short GetActionId()
        {
            return (short)ActionsEnum.ACTION_CHARACTER_MANA_USE_KILL_LIFE;
        }

        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTriggeredEffect(
                Id,
                Target != null ? Target.Id : 0,
                Duration > 500 ? (short)-1 : Duration,
                Convert.ToSByte(Dispellable ? 0 : 1),
                (short)(Spell != null ? Spell.Id : 0),
                DamagePerAp,
                0,
                0,
                0);
        }
    }
}
