using System.Collections.Generic;
using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Spells;
using Sunshine.Protocol.Utils;

namespace Sunshine.WorldServer.Game.Effects.Metadata
{
    /// <summary>
    /// In-memory lookup of effect metadata, keyed by (SpellId, EffectId). Loaded once at startup
    /// (and again after seeding). Handlers consult <see cref="Resolve"/>; a null result means
    /// "no metadata -> use current behavior" (see MetadataDefaults).
    ///
    /// Phase 1 is shadow-only: the cache is read for logging/parity, it does not drive behavior.
    /// </summary>
    public class MetadataCache : Singleton<MetadataCache>
    {
        private readonly Dictionary<(int SpellId, int EffectId), EffectMetadataRecord> _effects =
            new Dictionary<(int, int), EffectMetadataRecord>();

        public int Count
        {
            get { return _effects.Count; }
        }

        /// <summary>
        /// Reloads the cache from the database. Never throws: on failure the cache is left empty,
        /// which makes every handler fall back to its current behavior.
        /// </summary>
        public void Load()
        {
            try
            {
                var records = MetadataRepository.Instance.GetAllEffectMetadata();
                _effects.Clear();
                foreach (var record in records)
                    _effects[(record.SpellId, record.EffectId)] = record;

                MetadataLog.Write($"Cache loaded: {_effects.Count} effect_metadata row(s).");
            }
            catch (System.Exception ex)
            {
                _effects.Clear();
                global::Sunshine.Logs.Logger.WriteError($"[METADATA] Cache load failed, using fallback defaults: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns the metadata row for the given spell/effect, or null if none exists.
        /// </summary>
        public EffectMetadataRecord Resolve(int spellId, int effectId)
        {
            return _effects.TryGetValue((spellId, effectId), out var record) ? record : null;
        }
    }
}
