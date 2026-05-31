using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Fights.Buffs;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Debuffs
{
    [EffectHandler(EffectsEnum.Effect_ReduceEffectsDuration)]
    public class ReduceEffectsDuration : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var actor in GetAffectedActors())
            {
                foreach(var buff in actor.GetBuffs().Where(x => x.Dispellable))
                {
                    Buff currentBuff = buff;
                    currentBuff.Duration -= 1;
                    actor.UpdateBuff(buff);

                    if (currentBuff.Duration <= 0)
                        actor.RemoveBuff(currentBuff);
                }         
            }
        }
    }
}

