using Sunshine.WorldServer.Game.Actors.Fighters;

namespace Sunshine.WorldServer.Game.Spells.Casts.Rogue
{
    [SpellCastHandler(2803)]
    public class EntourloupeHandler : SpellCastHandler
    {
        public EntourloupeHandler(FightActor caster, Spell spell, short targetedCell, bool critical)
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            var target = Fight?.GetOneFighter(TargetedCell);
            if (target == null || target is not BombFighter || !Caster.IsFriendlyWith(target) || Handlers == null)
                return;

            foreach (var handler in Handlers)
            {
                handler.TargetedCell = TargetedCell;
                handler.Apply();
            }
        }
    }
}