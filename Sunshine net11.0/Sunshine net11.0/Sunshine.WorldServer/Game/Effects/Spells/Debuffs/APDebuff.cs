using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using Sunshine.WorldServer.Handlers.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Buffs
{
    [EffectHandler(EffectsEnum.Effect_RemoveAP), EffectHandler(EffectsEnum.Effect_LosingAP)]
    public class APDebuff : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var target in GetAffectedActors())
            {
                if (target.Stats.AP.TotalMax <= 0)
                    return;

                short subAP = target.RollAPLose(Caster, (int)Effect.DiceNum);

                if (Effect.Duration > 1)
                    target.AddBuff(new StatsBuff(Caster, target, Spell, Effect, (short)Duration, true, StatsEnum.AP, (short)-subAP, true));
                else
                    target.LostAP(subAP);

                if (Effect.DiceNum - subAP > 0)
                    ActionsHandler.SendGameActionFightDodgePointLossMessage(Fight.Clients, ActionsEnum.ACTION_FIGHT_SPELL_DODGED_PA, Caster, target, (short)(Effect.DiceNum - subAP));
            }
        }
    }
}
