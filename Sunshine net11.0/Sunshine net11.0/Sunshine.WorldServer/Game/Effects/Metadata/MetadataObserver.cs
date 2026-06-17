using Sunshine.MySql.Database.World.Spells;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Spells;

namespace Sunshine.WorldServer.Game.Effects.Metadata
{
    /// <summary>
    /// Phase 1 shadow observability (REQUISITO 4 + 6). Each method reads the metadata row for a
    /// spell/effect and emits a "[METADATA]" parity line comparing the legacy hardcode decision
    /// (which still governs behavior) against the metadata-derived decision. It NEVER changes
    /// behavior and NEVER throws.
    /// </summary>
    public static class MetadataObserver
    {
        private static string EffectName(EffectsEnum effect)
        {
            var name = effect.ToString();
            return name.StartsWith("Effect_") ? name.Substring("Effect_".Length) : name;
        }

        /// <summary>S1 / Spell 159: charge bonus gated by a required state.</summary>
        public static void LogChargeBonus(Spell spell, EffectsEnum effect, bool hardcodeCharged)
        {
            try
            {
                if (spell == null)
                    return;

                var record = MetadataCache.Instance.Resolve(spell.Id, (int)effect);
                if (record == null)
                {
                    MetadataLog.Write($"Spell={spell.Id} Effect={EffectName(effect)} Fallback=DefaultBehavior (hardcodeCharged={hardcodeCharged})");
                    return;
                }

                bool metadataBonus = record.BonusIfState != 0 && record.RequiresState != 0 && hardcodeCharged;
                bool matched = metadataBonus == hardcodeCharged;
                MetadataLog.Write($"Spell={spell.Id} Effect={EffectName(effect)} RequiresState={record.RequiresState} Matched={matched} Multiplier={record.BonusMultiplier}");
            }
            catch { /* observability must never break combat */ }
        }

        /// <summary>S2 / Spell 192: heal allowed on enemy team.</summary>
        public static void LogEnemyHealing(Spell spell, EffectsEnum effect, bool hardcodeAllows)
        {
            try
            {
                if (spell == null)
                    return;

                var record = MetadataCache.Instance.Resolve(spell.Id, (int)effect);

                // Skip the trivial default-vs-default case to keep logs focused.
                if (record == null && !hardcodeAllows)
                    return;

                if (record == null)
                {
                    MetadataLog.Write($"Spell={spell.Id} Effect={EffectName(effect)} Fallback=DefaultBehavior (hardcodeAllowEnemyTarget={hardcodeAllows})");
                    return;
                }

                bool metadataAllows = record.AllowEnemyTarget != 0;
                bool matched = metadataAllows == hardcodeAllows;
                MetadataLog.Write($"Spell={spell.Id} Effect={EffectName(effect)} AllowEnemyTarget={metadataAllows} Matched={matched}");
            }
            catch { /* observability must never break combat */ }
        }

        /// <summary>S3 / Spell 233: kill targets the caster (summon suicide) instead of affected actors.</summary>
        public static void LogKillTarget(Spell spell, EffectsEnum effect, bool hardcodeCasterSuicide)
        {
            try
            {
                if (spell == null)
                    return;

                var record = MetadataCache.Instance.Resolve(spell.Id, (int)effect);

                // Skip the trivial default-vs-default case to keep logs focused.
                if (record == null && !hardcodeCasterSuicide)
                    return;

                if (record == null)
                {
                    MetadataLog.Write($"Spell={spell.Id} Effect={EffectName(effect)} Fallback=DefaultBehavior (hardcodeKillTarget={(hardcodeCasterSuicide ? KillTargetType.Caster : KillTargetType.Affected)})");
                    return;
                }

                var metadataKill = (KillTargetType)record.KillTarget;
                bool metadataCasterSuicide = metadataKill == KillTargetType.Caster || metadataKill == KillTargetType.Summon;
                bool matched = metadataCasterSuicide == hardcodeCasterSuicide;
                MetadataLog.Write($"Spell={spell.Id} Effect={EffectName(effect)} KillTarget={metadataKill} Matched={matched}");
            }
            catch { /* observability must never break combat */ }
        }
    }
}
