using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using System.Linq;

namespace Sunshine.WorldServer.Game.Spells.Casts.Feca
{
    [SpellCastHandler(16)]
    public class FractionHandler : SpellCastHandler
    {
        public FractionHandler(FightActor caster, Spell spell, short targetedCell, bool critical)
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            if (!m_initialized)
                Initialize();

            if (Handlers == null || Handlers.Length == 0)
                return;

            Handlers[0].TargetType = SpellTargetType.ALLY_ALL | SpellTargetType.SELF;

            var adjacentAllies = Fight.GetAllFighters()
                .Where(x => x != Caster &&
                            x.Team == Caster.Team &&
                            !(x is ISummoned) &&
                            x.Position.Point.IsAdjacentTo(Caster.Position.Point))
                .ToList();

            if (!adjacentAllies.Any())
                return;

            foreach (var handler in Handlers)
                handler.Apply();
        }
    }
}
