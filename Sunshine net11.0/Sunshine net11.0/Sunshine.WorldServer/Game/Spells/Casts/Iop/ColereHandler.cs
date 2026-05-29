using Sunshine.WorldServer.Game.Actors.Fighters;
using System.Linq;

namespace Sunshine.WorldServer.Game.Spells.Casts.Iop
{
    /// <summary>
    /// Handler for Colere spell (Iop's Wrath)
    /// If no target, applies buff to caster with duration 5
    /// </summary>
    [SpellCastHandler(143)] // Iop's Wrath spell ID
    public class ColereHandler : SpellCastHandler
    {
        public ColereHandler(FightActor caster, Spell spell, short targetedCell, bool critical) 
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            if (!m_initialized)
            {
                Initialize();
            }

            var target = Fight.GetOneFighter(TargetedCell);
            
            if (target == null && Handlers.Length > 1)
            {
                // No target found, apply buff to caster
                Handlers[1].Duration = 5;
                Handlers[1].TargetedCell = Caster.Position.Cell;
                Handlers[1].Apply();
            }
            else
            {
                // Normal execution with target
                foreach (var handler in Handlers)
                {
                    handler.Apply();
                }
            }
        }
    }
}
