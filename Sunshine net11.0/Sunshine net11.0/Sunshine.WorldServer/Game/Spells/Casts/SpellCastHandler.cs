using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Effects.Spells;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Maps;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Spells.Casts
{
    /// <summary>
    /// Base class for custom spell cast handlers
    /// Adapted from ñoña emulator architecture
    /// </summary>
    public abstract class SpellCastHandler
    {
        protected bool m_initialized;

        public SpellEffectHandler[] Handlers { get; set; }

        public FightActor Caster { get; protected set; }
        public Spell Spell { get; protected set; }
        public short TargetedCell { get; protected set; }
        public bool Critical { get; protected set; }
        public Fight Fight => Caster?.Fight;
        public Map Map => Fight?.Map;

        public virtual bool SilentCast
        {
            get
            {
                if (!m_initialized)
                    return false;
                return Handlers != null && Handlers.Any(entry => entry.RequireSilentCast());
            }
        }

        protected SpellCastHandler(FightActor caster, Spell spell, short targetedCell, bool critical)
        {
            Caster = caster;
            Spell = spell;
            TargetedCell = targetedCell;
            Critical = critical;
        }

        public virtual void Initialize()
        {
            m_initialized = true;
        }

        public abstract void Execute();
    }
}
