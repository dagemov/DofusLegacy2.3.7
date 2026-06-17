using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.Effects.Metadata;

namespace Sunshine.BaseServer.Loaders.World.Spells
{
    /// <summary>
    /// Startup hook for spell metadata (Phase 1). Ensures the `effect_metadata` table exists and
    /// loads it into MetadataCache. Wrapped so a failure here never aborts server boot: on error
    /// the cache stays empty and handlers fall back to their current behavior.
    /// </summary>
    public static class MetadataLoader
    {
        public static void Initialize()
        {
            try
            {
                MetadataRepository.Instance.EnsureTable();
                MetadataCache.Instance.Load();
            }
            catch (System.Exception ex)
            {
                global::Sunshine.Logs.Logger.WriteError($"[METADATA] Loader failed (server continues with fallback defaults): {ex}");
            }
        }
    }
}
