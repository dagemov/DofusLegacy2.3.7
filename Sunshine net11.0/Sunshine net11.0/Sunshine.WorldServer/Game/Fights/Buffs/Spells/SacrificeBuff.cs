using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Effects.Spells;
using Sunshine.WorldServer.Game.Spells;

namespace Sunshine.WorldServer.Game.Fights.Buffs.Spells
{
    public class SacrificeBuff : Buff
    {
        public SacrificeBuff(FightActor caster, FightActor target, Spell spell, Effect effect, short duration, bool dispellable)
        {
            Id = caster.PopNextBuffId();
            Caster = caster;
            Target = target;
            Spell = spell;
            Effect = effect;
            Duration = duration;
            Dispellable = dispellable;
            Type = BuffTypeEnum.BEFORE_ATTACKED;
        }

        public override void Apply()
        {
            // Logic handled in FightActor.InflictDamage
        }

        public override void Dispell()
        {
        }

        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTriggeredEffect(
                Id,
                Target.Id,
                Duration > 500 ? (short)-1 : Duration,
                System.Convert.ToSByte(Dispellable ? 0 : 1),
                (short)Spell.Id,
                (int)Effect.Id,
                0,
                0,
                0);
        }

        public override short GetActionId()
        {
            return (short)ActionsEnum.ACTION_CHARACTER_LIFE_POINTS_LOST;
        }
    }
}