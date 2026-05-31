using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using System.Linq;

namespace Sunshine.WorldServer.Game.Spells.Casts.Sadida
{
    /// <summary>
    /// Handler for Sacrifice Doll spell
    /// Removes second effect and kills caster after execution
    /// </summary>
    [SpellCastHandler((int)SpellIdEnum.Sacrifier)] // Sacrifiée attack spell ID
    public class SacrifieeHandler : SpellCastHandler
    {
        public SacrifieeHandler(FightActor caster, Spell spell, short targetedCell, bool critical) 
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            if (!m_initialized)
            {
                Initialize();
            }

            // Remove second handler (index 1)
            var handlersList = Handlers.ToList();
            if (handlersList.Count > 1)
            {
                handlersList.RemoveAt(1);
                Handlers = handlersList.ToArray();
            }

            // Execute remaining handlers
            foreach (var handler in Handlers)
            {
                handler.Apply();
            }

            // Kill the caster
            Caster.InflictDamage(Caster.Stats.Health.Total, Caster);
        }
    }

    /// <summary>
    /// Handler for Sacrifice spell
    /// Similar to Sacrifice Doll
    /// </summary>
    [SpellCastHandler((int)SpellIdEnum.SacrificeDoll)] // Sacrifice Doll spell ID
    public class SacrificeHandler : SpellCastHandler
    {
        public SacrificeHandler(FightActor caster, Spell spell, short targetedCell, bool critical) 
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            if (!m_initialized)
            {
                Initialize();
            }

            // Remove second handler (index 1)
            var handlersList = Handlers.ToList();
            if (handlersList.Count > 1)
            {
                handlersList.RemoveAt(1);
                Handlers = handlersList.ToArray();
            }

            // Execute remaining handlers
            foreach (var handler in Handlers)
            {
                handler.Apply();
            }

            // Kill the caster
            Caster.InflictDamage(Caster.Stats.Health.Total, Caster);
        }
    }
}
