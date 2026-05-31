using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using Sunshine.WorldServer.Game.Fights.Mechanics;
using Sunshine.WorldServer.Handlers.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Buffs
{
    [EffectHandler(EffectsEnum.Effect_LostMP), EffectHandler(EffectsEnum.Effect_LosingMP), EffectHandler(EffectsEnum.Effect_SubMP_1080)]
    public class MPDebuff : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var target in GetAffectedActors())
            {
                if (target == null || !target.IsAlive)
                    continue;

                if (FrigostBossMechanics.TryApplyHamrackMpLossBoost(target, Caster, Spell, Effect))
                    continue;

                if (target.Stats.MP.TotalMax <= 0)
                    continue;

                short subMP = target.RollMPLose(Caster, (int)Effect.DiceNum);

                if (Effect.Duration > 1)
                    target.AddBuff(new StatsBuff(Caster, target, Spell, Effect, (short)Duration, true, StatsEnum.MP, (short)-subMP, true));
                else
                    target.LostMP(subMP);

                if (Effect.DiceNum - subMP > 0)
                    ActionsHandler.SendGameActionFightDodgePointLossMessage(Fight.Clients, ActionsEnum.ACTION_FIGHT_SPELL_DODGED_PM, Caster, target, (short)(Effect.DiceNum - subMP));
            }
        }
    }

    [EffectHandler(EffectsEnum.Effect_StealMP_77)]
    public class MPStealNonFix : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var target in GetAffectedActors())
            {
                if (target == null || !target.IsAlive)
                    continue;

                if (FrigostBossMechanics.TryApplyHamrackMpLossBoost(target, Caster, Spell, Effect))
                    continue;

                if (target.Stats.MP.TotalMax <= 0)
                    continue;

                Effect.GenerateEffect();
                int requested = System.Math.Max(0, Effect.Value);
                if (requested <= 0)
                    continue;

                short stolen = target.RollMPLose(Caster, requested);
                short dodged = (short)(requested - stolen);

                if (dodged > 0)
                {
                    ActionsHandler.SendGameActionFightDodgePointLossMessage(
                        Fight.Clients,
                        ActionsEnum.ACTION_FIGHT_SPELL_DODGED_PM,
                        Caster,
                        target,
                        dodged);
                }

                if (stolen <= 0)
                    continue;

                target.LostMP(stolen);

                if (Duration > 0)
                {
                    Caster.AddBuff(new StatsBuff(
                        Caster,
                        Caster,
                        Spell,
                        Effect,
                        (short)Duration,
                        true,
                        StatsEnum.MP,
                        stolen,
                        (short)EffectsEnum.Effect_AddMP_128));
                }
                else
                {
                    Caster.RegainMP(stolen);
                }
            }
        }
    }

}
