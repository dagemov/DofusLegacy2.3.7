using Sunshine.WorldServer.Game.Actors.Fighters;

namespace Sunshine.WorldServer.Game.Spells.Casts.Rogue
{
    [SpellCastHandler(2820)]
    public class RoublabotDetonationHandler : SpellCastHandler
    {
        public RoublabotDetonationHandler(FightActor caster, Spell spell, short targetedCell, bool critical)
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            RoublabotSpellHelper.Detonate(Caster, Spell, Critical, TargetedCell);
        }
    }
}
