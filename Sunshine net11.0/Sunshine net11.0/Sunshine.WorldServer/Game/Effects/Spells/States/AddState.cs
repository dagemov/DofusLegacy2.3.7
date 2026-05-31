using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using System;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.States
{
    [EffectHandler(EffectsEnum.Effect_AddState)]
    public class AddState : SpellEffectHandler
    {
        public override void Apply()
        {
            if (Duration == -1)
                Duration = 1000;

            var state = (SpellStatesEnum)ResolveStateId();
            bool dispellable = state != SpellStatesEnum.Carrying && state != SpellStatesEnum.Carried;

            foreach (var actor in GetAffectedActors())
            {
                if (actor == null)
                    continue;

                actor.AddBuff(new StateBuff(Caster, actor, Spell, Effect, (short)Duration, dispellable, state));
            }
        }

        private int ResolveStateId()
        {
            // SaveKrosmoz confirme Drunk = 1. Selon le format SQL/D2O, l'état peut
            // arriver dans Value, DiceNum ou DiceFace. On prend le premier ID valide.
            var candidates = new[] { Value, (int)DiceNum, (int)DiceFace };
            var nonZero = candidates.FirstOrDefault(x => x > 0 && Enum.IsDefined(typeof(SpellStatesEnum), x));
            return nonZero != 0 ? nonZero : candidates.FirstOrDefault(x => Enum.IsDefined(typeof(SpellStatesEnum), x));
        }
    }
}
