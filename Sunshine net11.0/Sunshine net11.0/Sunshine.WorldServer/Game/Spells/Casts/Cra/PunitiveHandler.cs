using Sunshine.WorldServer.Game.Actors.Fighters;
using System.Linq;

namespace Sunshine.WorldServer.Game.Spells.Casts.Cra
{
    /// <summary>
    /// Handler for Punitive Arrow spell (ID 171)
    /// Doubles the buff value if the spell is already active on caster
    /// </summary>
    [SpellCastHandler(171)]
    public class PunitiveHandler : SpellCastHandler
    {
        public PunitiveHandler(FightActor caster, Spell spell, short targetedCell, bool critical) 
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            if (Handlers.Length > 1)
            {
                // Store original value
                var originalValue = Handlers[1].Value;

                // Check if caster already has this buff
                var existingBuff = Caster.GetBuffs().FirstOrDefault(x => x.Spell.Id == Spell.Id);
                
                if (existingBuff != null)
                {
                    // Double the buff value
                    Handlers[1].Value *= 2;
                }

                // Apply to caster
                Handlers[1].TargetedCell = Caster.Position.Cell;
            }

            // Execute all handlers
            foreach (var handler in Handlers)
            {
                handler.Apply();
            }
        }
    }
}
