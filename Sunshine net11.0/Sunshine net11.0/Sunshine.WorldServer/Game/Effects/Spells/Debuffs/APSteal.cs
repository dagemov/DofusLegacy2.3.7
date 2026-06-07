using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using Sunshine.WorldServer.Handlers.Actions;
using System;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.Buffs
{
    [EffectHandler(EffectsEnum.Effect_StealAP_84), EffectHandler(EffectsEnum.Effect_StealAP_440)]
    public class APSteal : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var target in GetAffectedActors())
            {
                if (target == null || !target.IsAlive)
                    continue;

                if (target.Stats.AP.TotalMax <= 0)
                    continue;

                Effect.GenerateEffect();
                int requested = System.Math.Max(0, Effect.Value);
                if (requested <= 0)
                    continue;

                short stolen = target.RollAPLose(Caster, requested);
                short dodged = (short)(requested - stolen);

                if (dodged > 0)
                {
                    ActionsHandler.SendGameActionFightDodgePointLossMessage(
                        Fight.Clients,
                        ActionsEnum.ACTION_FIGHT_SPELL_DODGED_PA,
                        Caster,
                        target,
                        dodged);
                }

                if (stolen <= 0)
                    continue;

                target.LostAP(stolen);

                if (Duration > 0)
                {
                    Caster.AddBuff(new StatsBuff(
                        Caster,
                        Caster,
                        Spell,
                        Effect,
                        (short)Duration,
                        true,
                        StatsEnum.AP,
                        stolen,
                        (short)EffectsEnum.Effect_AddAP_111));
                }
                else
                {
                    Caster.RegainAP(stolen);
                }
            }
        }
    }
}
