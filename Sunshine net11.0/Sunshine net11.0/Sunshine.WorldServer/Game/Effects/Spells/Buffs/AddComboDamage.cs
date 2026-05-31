using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;

namespace Sunshine.WorldServer.Game.Effects.Spells.Buffs
{
    [EffectHandler(EffectsEnum.Effect_AddComboDamage)]
    public class AddComboDamage : SpellEffectHandler
    {
        public override void Apply()
        {
            int comboBonus = DiceNum > 0 ? (int)DiceNum : System.Math.Max(0, Value);

            foreach (var actor in GetAffectedActors())
            {
                if (actor is BombFighter bomb)
                {
                    if (comboBonus > 0)
                        bomb.AddComboBonus(comboBonus, true);
                    else
                        bomb.IncreaseCombo();
                }
            }
        }
    }
}
