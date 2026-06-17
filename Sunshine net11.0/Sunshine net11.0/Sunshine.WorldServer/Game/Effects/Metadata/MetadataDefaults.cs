using Sunshine.MySql.Database.World.Spells;

namespace Sunshine.WorldServer.Game.Effects.Metadata
{
    /// <summary>
    /// Fallback semantics applied whenever a spell/effect has no metadata row (REQUISITO 3).
    /// These values reproduce the engine's current default behavior. Reading metadata must NEVER
    /// throw or abort a fight; absence of a row always degrades to these constants.
    /// </summary>
    public static class MetadataDefaults
    {
        public const bool AllowEnemyTarget = false;
        public const KillTargetType KillTarget = KillTargetType.Affected;
        public const int RequiresState = 0; // 0 = no required state (null)
        public const bool BonusIfState = false;
        public const decimal BonusMultiplier = 1.0m;
        public const int GrantsStateOnCast = 0;
        public const TriggerTimingType TriggerTiming = TriggerTimingType.TurnBegin;
    }

    /// <summary>
    /// Centralized "[METADATA]" observability prefix (REQUISITO 4). Writes through the existing
    /// console/disk logger so behavior can be verified from logs without attaching a debugger.
    /// </summary>
    public static class MetadataLog
    {
        public static void Write(string line)
        {
            global::Sunshine.Logs.Logger.WriteInfo("[METADATA] " + line);
        }
    }
}
