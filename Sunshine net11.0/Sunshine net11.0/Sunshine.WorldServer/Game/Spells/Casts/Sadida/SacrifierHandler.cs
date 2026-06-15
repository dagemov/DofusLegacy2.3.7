using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using System.Linq;

namespace Sunshine.WorldServer.Game.Spells.Casts.Sadida
{
    // Spell 189 (La Sacrifiée) uses the default CastSpell → EffectDispatcher path (single Effect_Summon).
    // Doll suicide/explosion is handled on the doll's own spell (233) via effect handlers.

    /// <summary>
    /// Handler for Sacrifice Doll spell (2006).
    /// </summary>
    [SpellCastHandler((int)SpellIdEnum.SacrificeDoll)]
    public class SacrificeHandler : SpellCastHandler
    {
        public SacrificeHandler(FightActor caster, Spell spell, short targetedCell, bool critical)
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            if (!m_initialized)
                Initialize();

            var handlersList = Handlers.ToList();
            if (handlersList.Count > 1)
            {
                handlersList.RemoveAt(1);
                Handlers = handlersList.ToArray();
            }

            foreach (var handler in Handlers)
                handler.Apply();

            Caster.InflictDamage(Caster.Stats.Health.Total, Caster);
        }
    }
}
