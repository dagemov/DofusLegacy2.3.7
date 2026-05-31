using System;

namespace Sunshine.WorldServer.Game.Spells.Casts
{
    /// <summary>
    /// Attribute to mark spell cast handlers with their spell ID
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SpellCastHandlerAttribute : Attribute
    {
        public int SpellId { get; }

        public SpellCastHandlerAttribute(int spellId)
        {
            SpellId = spellId;
        }
    }
}
