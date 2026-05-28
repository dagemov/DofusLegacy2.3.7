using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using System;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.States
{
    [EffectHandler(EffectsEnum.Effect_RemoveState)]
    [EffectHandler(EffectsEnum.Effect_952)]
    public class RemoveState : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (FightActor current in base.GetAffectedActors())
            {
                if (current == null)
                    continue;

                SpellStatesEnum state = (SpellStatesEnum)ResolveStateId();

                if (state == SpellStatesEnum.Drunk || state == SpellStatesEnum.State_44 || state == SpellStatesEnum.Barmy)
                {
                    current.ApplyBambooMilkPandawaReset();
                    continue;
                }

                var stateBuffs = current.GetBuffs(x => x is StateBuff stateBuff && stateBuff.State == state).ToArray();
                foreach (var buff in stateBuffs)
                    current.RemoveBuff(buff);

                while (current.HasState(state))
                    current.RemoveState(state);
            }
        }

        private int ResolveStateId()
        {
            // Lait de Bambou SaveKrosmoz retire Drunk = 1. Selon le format SQL/D2O,
            // l'état peut arriver dans Value, DiceNum ou DiceFace.
            var candidates = new[] { Value, (int)DiceNum, (int)DiceFace };
            var nonZero = candidates.FirstOrDefault(x => x > 0 && Enum.IsDefined(typeof(SpellStatesEnum), x));
            return nonZero != 0 ? nonZero : candidates.FirstOrDefault(x => Enum.IsDefined(typeof(SpellStatesEnum), x));
        }
    }
}
