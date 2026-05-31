using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using System;

namespace Sunshine.WorldServer.Game.Fights.Buffs.Spells
{
    public class PunishmentBuff : Buff
    {
        public StatsEnum BoostedStat { get; private set; }
        public int MaxBoost { get; private set; }
        public int CurrentBoost { get; private set; }

        public PunishmentBuff(int id, FightActor target, FightActor caster, Effect effect, Spell spell, short duration, StatsEnum boostedStat, int maxBoost)
        {
            Id = id;
            Target = target;
            Caster = caster;
            Effect = effect;
            Spell = spell;
            Duration = duration;
            // Keep the authoritative stat resolved from the spell metadata, but
            // still honor historical client-side action ids stored in Effect.Value
            // when they match a real punishment boost action.
            BoostedStat = ResolveBoostedStat(effect, boostedStat);
            MaxBoost = maxBoost;
            CurrentBoost = 0;
            Type = BuffTypeEnum.AFTER_ATTACKED;
            Dispellable = true;
        }

        public override void Apply()
        {
        }

        public override void Dispell()
        {
            if (CurrentBoost <= 0)
                return;

            bool lifeMaximumChanged = AffectsLifeMaximum();
            int lifeBefore = lifeMaximumChanged ? Target.GetCurrentLife() : 0;

            Target.Stats[BoostedStat].Context -= (short)CurrentBoost;
            CurrentBoost = 0;

            if (lifeMaximumChanged)
                Target.PreserveCurrentLifeAfterMaxLifeChange(lifeBefore, Caster, true);
        }

        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTemporaryBoostEffect(
                Id,
                Target.Id,
                Duration > 500 ? (short)-1 : Duration,
                Convert.ToSByte(Dispellable ? 0 : 1),
                (short)Spell.Id,
                (short)CurrentBoost);
        }

        public override short GetActionId()
        {
            short effectActionId;
            if (TryResolveDisplayedActionId(out effectActionId))
                return effectActionId;

            return BoostedStat switch
            {
                StatsEnum.Strength => (short)ActionsEnum.ACTION_CHARACTER_BOOST_STRENGTH,
                StatsEnum.Agility => (short)ActionsEnum.ACTION_CHARACTER_BOOST_AGILITY,
                StatsEnum.Chance => (short)ActionsEnum.ACTION_CHARACTER_BOOST_CHANCE,
                StatsEnum.Intelligence => (short)ActionsEnum.ACTION_CHARACTER_BOOST_INTELLIGENCE,
                StatsEnum.Wisdom => (short)ActionsEnum.ACTION_CHARACTER_BOOST_WISDOM,
                StatsEnum.Vitality => (short)ActionsEnum.ACTION_CHARACTER_BOOST_VITALITY,
                _ => (short)ActionsEnum.ACTION_CHARACTER_BOOST_DAMAGES
            };
        }

        public override short GetUpdateActionId()
        {
            // The 2.3.7 client keeps the punishment line coherent when the
            // refresh reuses the displayed boost action instead of the generic
            // 515 update.
            return GetActionId();
        }

        public void OnDamaged(FightActor source, int damage)
        {
            if (damage <= 0 || CurrentBoost >= MaxBoost)
                return;

            int bonus = damage;
            int remaining = MaxBoost - CurrentBoost;

            if (bonus > remaining)
                bonus = remaining;

            if (bonus <= 0)
                return;

            bool lifeMaximumChanged = AffectsLifeMaximum();
            int lifeBefore = lifeMaximumChanged ? Target.GetCurrentLife() : 0;

            CurrentBoost += bonus;
            Target.Stats[BoostedStat].Context += (short)bonus;

            if (lifeMaximumChanged)
                Target.PreserveCurrentLifeAfterMaxLifeChange(lifeBefore, source ?? Caster, true);

            ActionsHandler.SendGameActionFightPointsVariationMessage(
                Target.Fight.Clients,
                (ActionsEnum)GetActionId(),
                source ?? Target,
                Target,
                (short)bonus);

            Target.UpdateBuff(this);

            if (Target is CharacterFighter fighter)
                fighter.Character?.RefreshStats();

            if (Target.Fight != null && !Target.Fight.IsVisualSequenceActive)
                ContextHandler.SendGameFightSynchronizeMessage(Target.Fight.Clients, Target);
        }

        private bool AffectsLifeMaximum()
        {
            return BoostedStat == StatsEnum.Health || BoostedStat == StatsEnum.Vitality;
        }

        private bool TryResolveDisplayedActionId(out short actionId)
        {
            actionId = 0;

            if (Effect == null || Effect.Value <= 0)
                return false;

            switch ((ActionsEnum)Effect.Value)
            {
                case ActionsEnum.ACTION_CHARACTER_BOOST_STRENGTH:
                case ActionsEnum.ACTION_CHARACTER_BOOST_AGILITY:
                case ActionsEnum.ACTION_CHARACTER_BOOST_CHANCE:
                case ActionsEnum.ACTION_CHARACTER_BOOST_INTELLIGENCE:
                case ActionsEnum.ACTION_CHARACTER_BOOST_WISDOM:
                case ActionsEnum.ACTION_CHARACTER_BOOST_VITALITY:
                case (ActionsEnum)407:
                    actionId = (short)Effect.Value;
                    return true;
                default:
                    return false;
            }
        }

        private static StatsEnum ResolveBoostedStat(Effect effect, StatsEnum fallback)
        {
            if (effect != null)
            {
                switch ((ActionsEnum)effect.Value)
                {
                    case ActionsEnum.ACTION_CHARACTER_BOOST_STRENGTH:
                        return StatsEnum.Strength;
                    case ActionsEnum.ACTION_CHARACTER_BOOST_AGILITY:
                        return StatsEnum.Agility;
                    case ActionsEnum.ACTION_CHARACTER_BOOST_CHANCE:
                        return StatsEnum.Chance;
                    case ActionsEnum.ACTION_CHARACTER_BOOST_INTELLIGENCE:
                        return StatsEnum.Intelligence;
                    case ActionsEnum.ACTION_CHARACTER_BOOST_WISDOM:
                        return StatsEnum.Wisdom;
                    case ActionsEnum.ACTION_CHARACTER_BOOST_VITALITY:
                    case (ActionsEnum)407:
                        return StatsEnum.Vitality;
                }
            }

            return fallback;
        }
    }
}