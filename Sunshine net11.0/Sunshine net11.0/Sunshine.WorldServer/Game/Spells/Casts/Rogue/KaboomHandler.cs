using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;

namespace Sunshine.WorldServer.Game.Spells.Casts.Rogue
{
    /// <summary>
    /// Handler for Kaboom spell (ID 2815)
    /// Modifies targets to include self and all allies
    /// </summary>
    [SpellCastHandler(2815)]
    public class KaboomHandler : SpellCastHandler
    {
        public KaboomHandler(FightActor caster, Spell spell, short targetedCell, bool critical) 
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            // Modify first handler to target self and all allies
            if (Handlers != null && Handlers.Length > 0 && Handlers[0] != null)
            {
                Handlers[0].TargetType = SpellTargetType.SELF | SpellTargetType.ALLY_ALL;
                Handlers[0].Apply();
            }
        }
    }
}
