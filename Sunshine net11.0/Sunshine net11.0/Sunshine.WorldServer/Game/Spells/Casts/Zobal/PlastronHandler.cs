using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using System.Linq;

namespace Sunshine.WorldServer.Game.Spells.Casts.Zobal
{
    /// <summary>
    /// Handler for Plastron spell
    /// Modifies targets to include self and all allies
    /// </summary>
    [SpellCastHandler(2843)] // Plastron spell ID
    public class PlastronHandler : SpellCastHandler
    {
        public PlastronHandler(FightActor caster, Spell spell, short targetedCell, bool critical) 
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            if (!m_initialized)
            {
                Initialize();
            }

            if (Handlers.Length > 0)
            {
                // Modify first handler to target self and all allies
                Handlers[0].TargetType = SpellTargetType.SELF | SpellTargetType.ALLY_ALL;
            }

            // Execute all handlers
            foreach (var handler in Handlers)
            {
                handler.Apply();
            }
        }
    }
}
