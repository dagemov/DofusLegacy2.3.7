using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using System;

namespace Sunshine.WorldServer.Game.Effects.Spells.Debuffs
{
    [EffectHandler(EffectsEnum.Effect_StealKamas)]
    public class StealKamas : SpellEffectHandler
    {
        public override void Apply()
        {
            int min = System.Math.Min((int)DiceNum, (int)DiceFace);
            int max = System.Math.Max((int)DiceNum, (int)DiceFace);

            foreach (var target in GetAffectedActors())
            {
                int rolled = new Random().Next(min, max + 1);

                if (target is CharacterFighter targetChar && targetChar.Character != null)
                {
                    int taken = Math.Min(rolled, targetChar.Character.Inventory.Kamas);
                    if (taken > 0)
                    {
                        targetChar.Character.Inventory.SetKamas(-taken);
                        targetChar.Character.SendServerMessage(string.Format("Has perdido {0} kamas.", taken));

                        if (Caster is CharacterFighter casterChar && casterChar.Character != null)
                        {
                            casterChar.Character.Inventory.SetKamas(taken);
                            casterChar.Character.SendServerMessage(string.Format("Has robado {0} kamas.", taken));
                        }
                    }
                }
                else if (Caster is CharacterFighter casterChar && casterChar.Character != null && rolled > 0)
                {
                    casterChar.Character.Inventory.SetKamas(rolled);
                    casterChar.Character.SendServerMessage(string.Format("Has robado {0} kamas.", rolled));
                }
            }
        }
    }
}
