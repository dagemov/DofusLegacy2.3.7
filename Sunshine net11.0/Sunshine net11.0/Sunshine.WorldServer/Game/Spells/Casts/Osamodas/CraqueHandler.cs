using Sunshine.WorldServer.Game.Actors.Fighters;

namespace Sunshine.WorldServer.Game.Spells.Casts.Osamodas
{
    /// <summary>
    /// Handler for Craque spell (ID 1999)
    /// Applies two effects to the same target
    /// </summary>
    [SpellCastHandler(1999)]
    public class CraqueHandler : SpellCastHandler
    {
        public CraqueHandler(FightActor caster, Spell spell, short targetedCell, bool critical) 
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            var target = Fight.GetOneFighter(TargetedCell);
            if (target != null && Handlers.Length >= 2)
            {
                // Apply both effects to the target
                Handlers[0].TargetedCell = TargetedCell;
                Handlers[0].Apply();
                
                Handlers[1].TargetedCell = TargetedCell;
                Handlers[1].Apply();
            }
        }
    }
}
