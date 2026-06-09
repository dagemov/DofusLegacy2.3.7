using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Effects.Spells.Damages;
using Sunshine.WorldServer.Game.Fights.Diagnostics;
using Sunshine.WorldServer.Game.Spells;
using System;

namespace Sunshine.WorldServer.Game.Fights.Buffs.Spells
{
    public class DamageOverTimeBuff : Buff
    {
        private EffectSchoolEnum _effectSchool;
        private uint _diceNum;
        private uint _diceFace;

        public EffectSchoolEnum EffectSchool => _effectSchool;
        public uint DiceNum => _diceNum;
        public uint DiceFace => _diceFace;

        public DamageOverTimeBuff(FightActor caster, FightActor target, Spell spell, Effect effect,
            short duration, EffectSchoolEnum effectSchool, uint diceNum, uint diceFace)
        {
            Id = caster.PopNextBuffId();
            Caster = caster;
            Target = target;
            Spell = spell;
            Effect = effect;
            Duration = duration;
            Dispellable = true;
            _effectSchool = effectSchool;
            _diceNum = diceNum;
            _diceFace = diceFace;
        }

        public override void Apply()
        {
        }

        public override void Dispell()
        {
        }

        public void Tick()
        {
            if (Target == null || Target.IsDead())
                return;

            Damage damage = new Damage(_effectSchool, _diceNum, _diceFace, Spell, Caster);
            Target.InflictDamage(damage, true);
            FightCombatLogger.LogBuffTick(Target.Fight, Target, this, "DOT", damage.Amount, Duration);
        }

        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTemporaryBoostEffect(Id, Target?.Id ?? 0,
                Duration > 500 ? (short)-1 : Duration,
                Convert.ToSByte(Dispellable ? 0 : 1),
                (short)(Spell?.Id ?? 0), 1);
        }
    }
}
