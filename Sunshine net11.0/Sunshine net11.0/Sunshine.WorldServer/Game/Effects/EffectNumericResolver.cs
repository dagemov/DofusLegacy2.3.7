using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Spells;

namespace Sunshine.WorldServer.Game.Effects
{
    public static class EffectNumericResolver
    {
        public static int GetNumericValue(Effect effect)
        {
            if (effect == null)
                return 0;

            if (effect.Value > 0)
                return effect.Value;

            if (effect.DiceNum > 0)
                return (int)effect.DiceNum;

            if (effect.DiceFace > 0)
                return (int)effect.DiceFace;

            return 0;
        }

        public static int GetNumericValue(BasePlayerItem item, EffectsEnum effectId)
        {
            if (item == null)
                return 0;

            int value = 0;

            if (item.RawObjectEffects != null)
            {
                foreach (var rawEffect in item.RawObjectEffects)
                {
                    if (rawEffect == null || rawEffect.actionId != (short)effectId)
                        continue;

                    switch (rawEffect)
                    {
                        case ObjectEffectInteger integerEffect when integerEffect.value > 0:
                            value += integerEffect.value;
                            break;
                        case ObjectEffectMinMax minMaxEffect when minMaxEffect.max > 0:
                            value += minMaxEffect.max;
                            break;
                        case ObjectEffectDice diceEffect when diceEffect.diceNum > 0:
                            value += diceEffect.diceNum;
                            break;
                    }
                }
            }

            if (value <= 0 && item.Effects != null)
            {
                foreach (var effect in item.Effects)
                {
                    if (effect == null || effect.Id != effectId)
                        continue;

                    value += GetNumericValue(effect);
                }
            }

            return value > 0 ? value : 0;
        }
    }
}
