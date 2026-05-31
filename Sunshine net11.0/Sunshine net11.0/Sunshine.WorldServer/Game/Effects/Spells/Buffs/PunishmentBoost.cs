using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using System;

namespace Sunshine.WorldServer.Game.Effects.Spells.Buffs
{
    [EffectHandler(EffectsEnum.Effect_Punishment)]
    public class PunishmentBoost : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (FightActor current in GetAffectedActors())
            {
                current.AddBuff(new PunishmentBuff(current.PopNextBuffId(), current, Caster, Effect, Spell, (short)Effect.Duration, ResolveBoostedStat((ushort)Spell.Id, Effect), ResolveMaxBoost(Effect)));
            }
        }

        private static int ResolveMaxBoost(Game.Spells.Effect effect)
        {
            if (effect == null)
                return 0;

            if (effect.DiceFace > 0)
                return (int)effect.DiceFace;

            if (effect.DiceNum > 0)
                return (int)effect.DiceNum;

            return Math.Max(0, effect.Value);
        }

        private static StatsEnum ResolveBoostedStat(ushort spellId, Game.Spells.Effect effect)
        {
            switch ((SpellIdEnum)spellId)
            {
                case SpellIdEnum.ForcedPunishment:
                    return StatsEnum.Strength;
                case SpellIdEnum.BoldPunishment:
                    return StatsEnum.Chance;
                case SpellIdEnum.NimblePunishment:
                    return StatsEnum.Agility;
                case SpellIdEnum.SpiritualPunishment:
                    return StatsEnum.Intelligence;
                case SpellIdEnum.VitalPunishment:
                    return StatsEnum.Vitality;
                default:
                    return ResolveBoostedStatFromEffect(effect);
            }
        }

        private static StatsEnum ResolveBoostedStatFromEffect(Game.Spells.Effect effect)
        {
            if (effect == null)
                return StatsEnum.Strength;

            switch ((ActionsEnum)effect.DiceNum)
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
                    return StatsEnum.Vitality;
                case ActionsEnum.ACTION_CHARACTER_BOOST_DAMAGES_PERCENT:
                    return StatsEnum.DamageBonusPercent;
                default:
                    return StatsEnum.Strength;
            }
        }
    }
}
