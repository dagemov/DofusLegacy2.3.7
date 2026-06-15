using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.Others
{
    [EffectHandler(EffectsEnum.Effect_Kill)]
    public class Kill : SpellEffectHandler
    {
        /// <summary>
        /// Sacrificial doll explosion (spell 233): Effect_Kill removes only the doll.
        /// Enemies are damaged by the spell's damage effect, never by instant kill.
        /// </summary>
        private static bool IsSacrificialDollSuicide(int? spellId) => spellId == 233;

        public override void Apply()
        {
            if (IsSacrificialDollSuicide(Spell?.Id))
            {
                if (Caster is SummonedMonster summoned && summoned.IsAlive && !Caster.DeathHandled)
                    summoned.Die(Caster);
                else if (Caster != null && Caster.IsAlive && !Caster.DeathHandled)
                    Caster.Kill(Caster);
                return;
            }

            var actors = GetAffectedActors().ToList();

            // Self-target fallback: spells that kill the caster sometimes resolve with no
            // affected actors (e.g. sacrificial casts). Target the caster in that case.
            if (actors.Count == 0 && Caster != null && !Caster.DeathHandled)
                actors.Add(Caster);

            foreach (var actor in actors)
            {
                if (actor != null && actor.IsAlive)
                    actor.Kill(Caster);
            }
        }
    }
}
