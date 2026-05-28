using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Actions;
using System;

namespace Sunshine.WorldServer.Game.Fights.Buffs.Spells
{
    public class SkinBuff : Buff
    {
        public ActorLook Look { get; set; }

        public ActorLook OriginalLook { get; set; }

        public SkinBuff(FightActor caster, FightActor target, Spell spell, Effect effect,
            short duration, bool dispellable, ActorLook look, short actionId)
        {
            Id = caster.PopNextBuffId();
            Caster = caster;
            Target = target;
            Look = look.Clone();
            OriginalLook = target.Look.Clone();
            Spell = spell;
            Effect = effect;
            Duration = duration;
            Dispellable = dispellable;
            ActionId = actionId;
        }

        public SkinBuff(FightActor caster, FightActor target, Spell spell, Effect effect,
            short duration, bool dispellable, ActorLook look)
        {
            Id = caster.PopNextBuffId();
            Caster = caster;
            Target = target;
            Look = look.Clone();
            OriginalLook = target.Look.Clone();
            Spell = spell;
            Effect = effect;
            Duration = duration;
            Dispellable = dispellable;
        }

        private ActorLook GetNetworkLook()
        {
            var characterFighter = Target as CharacterFighter;
            if (characterFighter?.Character != null)
                return characterFighter.Character.GetDisplayedLook(Target.Look).Clone();

            return Target.Look.Clone();
        }

        public override void Apply()
        {
            Target.Look = Look.Clone();

            var networkLook = GetNetworkLook();

            ActionsHandler.SendGameActionFightChangeLookMessage(
                Target.Fight.Clients,
                Caster,
                Target,
                networkLook);
        }

        public override void Dispell()
        {
            Target.Look = OriginalLook.Clone();

            var networkLook = GetNetworkLook();

            ActionsHandler.SendGameActionFightChangeLookMessage(
                Target.Fight.Clients,
                Caster,
                Target,
                networkLook);
        }

        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTemporaryBoostEffect(
                Id,
                Target.Id,
                Duration > 500 ? (short)-1 : Duration,
                Convert.ToSByte(Dispellable ? 0 : 1),
                (short)Spell.Id,
                0);
        }
    }
}