using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Buffs;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using Sunshine.WorldServer.Game.Fights.Mechanics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Heals
{
    [EffectHandler(EffectsEnum.Effect_HealHP_81), EffectHandler(EffectsEnum.Effect_HealHP_143), EffectHandler(EffectsEnum.Effect_HealHP_108)]
    public class Heal : SpellEffectHandler
    {
        /// <summary>
        /// Spells that intentionally heal combatants on the enemy team (e.g. Ronce Apaisante on slowed foes).
        /// All other heals skip targets that are not friendly with the caster.
        /// </summary>
        private static bool AllowsEnemyHealing(Spell spell)
        {
            if (spell == null)
                return false;

            switch (spell.Id)
            {
                case 192: // Ronce Apaisante (La Lora)
                    return true;
                default:
                    return false;
            }
        }

        private IEnumerable<FightActor> GetHealTargets()
        {
            var actors = GetAffectedActors();
            if (AllowsEnemyHealing(Spell) || Caster == null)
                return actors;

            return actors.Where(actor => Caster.IsFriendlyWith(actor));
        }

        public override void Apply()
        {
            // Shadow metadata read (Phase 1): logged for parity only, does NOT drive behavior.
            Metadata.MetadataObserver.LogEnemyHealing(Spell, Id, AllowsEnemyHealing(Spell));

            foreach (var actor in GetHealTargets())
            {
                Effect.GenerateEffect();
                if (Duration > 0)
                {
                    var hotBuff = new HealOverTimeBuff(Caster, actor, Spell, Effect, (short)Duration, (short)Effect.Value);
                    actor.AddBuff(hotBuff);
                }
                else
                {
                    actor.Heal(Effect.Value, base.Caster, true);
                    FrigostBossMechanics.OnActorHealed(Caster, actor);
                }
            }
        }
    }
}

