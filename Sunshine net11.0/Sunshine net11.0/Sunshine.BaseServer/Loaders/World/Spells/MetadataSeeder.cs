using System.Collections.Generic;
using System.Linq;
using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Spells;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Effects.Metadata;
using Sunshine.WorldServer.Game.Spells;

namespace Sunshine.BaseServer.Loaders.World.Spells
{
    /// <summary>
    /// Phase 1 (Fase C): seeds the `effect_metadata` rows for spells 159/192/233, mirroring the
    /// existing hardcode so the shadow parity logs report Matched=true. The exact EffectIds are
    /// read from the already-loaded spells (SpellManager) instead of being guessed, so the rows
    /// always line up with what the handlers actually receive.
    ///
    /// Seeding is idempotent (INSERT ... ON DUPLICATE KEY UPDATE) and never aborts boot. The
    /// hardcode is NOT removed: both coexist for validation, per REQUISITO 5.
    /// </summary>
    public static class MetadataSeeder
    {
        public static void Initialize()
        {
            try
            {
                // Spell 159 (Colere de Iop): charge bonus gated by State_51, on each damage effect.
                foreach (var effectId in EffectIdsOf(159, name => name.Contains("Damage")))
                {
                    Upsert(159, effectId,
                        requiresState: 51, bonusIfState: 1, bonusMultiplier: 2.00m, grantsStateOnCast: 51);
                }

                // Spell 192 (Ronce Apaisante): heal allowed on enemy team.
                foreach (var effectId in EffectIdsOf(192, name => name.Contains("Heal")))
                {
                    Upsert(192, effectId, allowEnemyTarget: 1);
                }

                // Spell 233 (Sacrificial doll): Effect_Kill targets the caster (summon suicide).
                foreach (var effectId in EffectIdsOf(233, name => name == "Effect_Kill"))
                {
                    Upsert(233, effectId, killTarget: (int)KillTargetType.Caster);
                }

                // Refresh the cache so the freshly seeded rows are visible to the handlers.
                MetadataCache.Instance.Load();
            }
            catch (System.Exception ex)
            {
                global::Sunshine.Logs.Logger.WriteError($"[METADATA] Seeder failed (server continues, rows may be missing): {ex}");
            }
        }

        private static IEnumerable<int> EffectIdsOf(int spellId, System.Func<string, bool> namePredicate)
        {
            if (!SpellManager.Instance.Spells.TryGetValue(spellId, out var levels) || levels == null)
                return Enumerable.Empty<int>();

            var ids = new HashSet<int>();
            foreach (var spell in levels)
            {
                if (spell?.Effects == null)
                    continue;

                foreach (var effect in spell.Effects)
                {
                    if (namePredicate(effect.Id.ToString()))
                        ids.Add((int)effect.Id);
                }
            }
            return ids;
        }

        private static void Upsert(int spellId, int effectId,
            int killTarget = 0, int requiresState = 0, int bonusIfState = 0,
            decimal bonusMultiplier = 1.00m, int grantsStateOnCast = 0,
            int allowEnemyTarget = 0, int triggerTiming = 0)
        {
            const string sql = @"
INSERT INTO `effect_metadata`
    (`SpellId`,`EffectId`,`KillTarget`,`RequiresState`,`BonusIfState`,`BonusMultiplier`,`GrantsStateOnCast`,`AllowEnemyTarget`,`TriggerTiming`)
VALUES
    (@SpellId,@EffectId,@KillTarget,@RequiresState,@BonusIfState,@BonusMultiplier,@GrantsStateOnCast,@AllowEnemyTarget,@TriggerTiming)
ON DUPLICATE KEY UPDATE
    `KillTarget`=VALUES(`KillTarget`),
    `RequiresState`=VALUES(`RequiresState`),
    `BonusIfState`=VALUES(`BonusIfState`),
    `BonusMultiplier`=VALUES(`BonusMultiplier`),
    `GrantsStateOnCast`=VALUES(`GrantsStateOnCast`),
    `AllowEnemyTarget`=VALUES(`AllowEnemyTarget`),
    `TriggerTiming`=VALUES(`TriggerTiming`);";

            DatabaseManager.Connection.Execute(sql, new
            {
                SpellId = spellId,
                EffectId = effectId,
                KillTarget = killTarget,
                RequiresState = requiresState,
                BonusIfState = bonusIfState,
                BonusMultiplier = bonusMultiplier,
                GrantsStateOnCast = grantsStateOnCast,
                AllowEnemyTarget = allowEnemyTarget,
                TriggerTiming = triggerTiming
            });

            MetadataLog.Write($"Seeded Spell={spellId} Effect={effectId}");
        }
    }
}
