using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Spells;
using System;

namespace Sunshine.WorldServer.Game.Effects.Spells.Damages
{
    internal static class EffectDamageResolver
    {
        public static (uint diceNum, uint diceFace, int fixedBonus) ResolveDice(Effect effect)
        {
            if (effect == null)
                return (0, 0, 0);

            uint diceNum = effect.DiceNum;
            uint diceFace = effect.DiceFace;
            int fixedBonus = effect.Value;

            if (diceNum == 0 && diceFace == 0 && fixedBonus > 0)
            {
                diceNum = (uint)fixedBonus;
                diceFace = (uint)fixedBonus;
                fixedBonus = 0;
            }

            return (diceNum, diceFace, fixedBonus);
        }

        public static void ApplyFixedBonus(Damage damage, int fixedBonus)
        {
            if (damage == null || fixedBonus == 0)
                return;

            damage.Amount += fixedBonus;
        }

        public static int RollAndCombine(Effect effect)
        {
            var (diceNum, diceFace, fixedBonus) = ResolveDice(effect);
            int min = Math.Min((int)diceNum, (int)diceFace);
            int max = Math.Max((int)diceNum, (int)diceFace);

            if (max <= 0 && fixedBonus <= 0)
                return 0;

            int rolled = min == max ? min : new AsyncRandom().Next(min, max + 1);
            return rolled + fixedBonus;
        }
    }
}
