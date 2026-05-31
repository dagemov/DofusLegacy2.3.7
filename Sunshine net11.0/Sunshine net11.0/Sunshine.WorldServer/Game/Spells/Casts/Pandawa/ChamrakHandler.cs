using Sunshine.Protocol.Enums;
using System.Linq;

namespace Sunshine.WorldServer.Game.Spells.Casts.Pandawa
{
    [SpellCastHandler((int)SpellIdEnum.Chamrak)]
    public class ChamrakHandler : SpellCastHandler
    {
        public ChamrakHandler(Game.Actors.Fighters.FightActor caster, Spell spell, short targetedCell, bool critical)
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            if (!m_initialized)
                Initialize();

            if (!Caster.IsCarrying)
                return;

            foreach (var handler in Handlers ?? Enumerable.Empty<Effects.Spells.SpellEffectHandler>())
            {
                handler.TargetedCell = TargetedCell;
                handler.Apply();
            }
        }
    }
}
