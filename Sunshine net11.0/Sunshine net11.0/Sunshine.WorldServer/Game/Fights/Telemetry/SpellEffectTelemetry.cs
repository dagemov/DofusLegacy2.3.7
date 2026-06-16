using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Effects.Spells.Damages;
using Sunshine.WorldServer.Game.Fights.Buffs;
using Sunshine.WorldServer.Game.Fights.Buffs.Customs;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using Sunshine.WorldServer.Game.Spells;

namespace Sunshine.WorldServer.Game.Fights.Telemetry
{
    /// <summary>
    /// Specialized facade for spell/effect layer diagnostics.
    /// All events are emitted through <see cref="CombatTelemetry"/> (spell-casts JSONL channel).
    /// </summary>
    public static class SpellEffectTelemetry
    {
        public const string SchemaVersion = "spell-effect-telemetry-v1";

        private static readonly ConcurrentDictionary<int, int> FightSequence = new ConcurrentDictionary<int, int>();

        public static bool Enabled => CombatTelemetry.WriteSpellEffects;

        public static string AllocateCorrelationId(Fight fight, FightActor caster)
        {
            if (fight == null)
                return string.Empty;

            var seq = FightSequence.AddOrUpdate(fight.Id, 1, (_, value) => value + 1);
            var turnId = CombatTelemetry.ResolveTurnId(fight, caster);
            return string.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", fight.Id, turnId, seq);
        }

        public static void SpellCastAttempt(
            Fight fight,
            FightActor caster,
            Spell spell,
            short targetCell,
            int apBefore,
            string source = "Player")
        {
            if (!Enabled || fight == null || caster == null)
                return;

            var correlationId = SpellCastTelemetryScope.Current?.CorrelationId
                ?? AllocateCorrelationId(fight, caster);

            Emit("SpellCastAttempt", fight, caster, spell, SpellEffectLayer.Validation, "Pending",
                correlationId: correlationId,
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["targetCell"] = targetCell,
                    ["apBefore"] = apBefore,
                    ["source"] = source,
                    ["casterTeam"] = ResolveTeamLabel(caster)
                });
        }

        public static void SpellValidationResult(
            Fight fight,
            FightActor caster,
            Spell spell,
            short targetCell,
            SpellCastResult result,
            string correlationId = null)
        {
            if (!Enabled || fight == null || caster == null)
                return;

            correlationId ??= SpellCastTelemetryScope.Current?.CorrelationId
                ?? AllocateCorrelationId(fight, caster);

            var allowed = result == SpellCastResult.OK;
            Emit("SpellValidationResult", fight, caster, spell, SpellEffectLayer.Validation,
                allowed ? "Allowed" : "Rejected",
                reasonCode: result.ToString(),
                correlationId: correlationId,
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["allowed"] = allowed,
                    ["targetCell"] = targetCell
                });
        }

        public static void SpellCastResolved(
            Fight fight,
            FightActor caster,
            Spell spell,
            short targetCell,
            int apBefore,
            int apAfter,
            FightSpellCastCriticalEnum critical,
            int effectCount,
            string handlerPath,
            string customHandlerType,
            long durationMs,
            string correlationId = null)
        {
            if (!Enabled || fight == null || caster == null)
                return;

            correlationId ??= SpellCastTelemetryScope.Current?.CorrelationId;

            Emit("SpellCastResolved", fight, caster, spell, SpellEffectLayer.Handler, "OK",
                correlationId: correlationId,
                durationMs: durationMs,
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["targetCell"] = targetCell,
                    ["apBefore"] = apBefore,
                    ["apAfter"] = apAfter,
                    ["critical"] = critical.ToString(),
                    ["effectCount"] = effectCount,
                    ["handlerPath"] = handlerPath ?? string.Empty,
                    ["customHandlerType"] = customHandlerType ?? string.Empty
                });
        }

        public static void SpellEffectPlanned(
            Fight fight,
            FightActor caster,
            Spell spell,
            Effect effect,
            short targetCell,
            bool isCriticalEffect,
            string correlationId = null)
        {
            if (!Enabled || fight == null || effect == null)
                return;

            correlationId ??= SpellCastTelemetryScope.Current?.CorrelationId;

            Emit("SpellEffectPlanned", fight, caster, spell, SpellEffectLayer.SpellData, "Planned", effect: effect,
                correlationId: correlationId,
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["targetCell"] = targetCell,
                    ["diceNum"] = effect.DiceNum,
                    ["diceSide"] = effect.DiceFace,
                    ["value"] = effect.Value,
                    ["duration"] = effect.Duration,
                    ["delay"] = effect.Delay,
                    ["targetMask"] = effect.Target,
                    ["targetMaskLabel"] = EffectTargetTelemetryHelper.ResolveTargetMaskLabel((int)effect.Target),
                    ["zoneShape"] = effect.ZoneShape.ToString(),
                    ["zoneSize"] = effect.ZoneSize,
                    ["zoneMinSize"] = effect.ZoneMinSize,
                    ["isCriticalEffect"] = isCriticalEffect
                });
        }

        public static void EffectTargetsResolved(
            Fight fight,
            FightActor caster,
            Spell spell,
            Effect effect,
            short targetCell,
            IEnumerable<EffectTargetEntry> included,
            IEnumerable<EffectTargetEntry> filtered,
            string correlationId = null)
        {
            if (!Enabled || fight == null || effect == null)
                return;

            correlationId ??= SpellCastTelemetryScope.Current?.CorrelationId;

            Emit("EffectTargetsResolved", fight, caster, spell, SpellEffectLayer.TargetMask, "Resolved", effect: effect,
                correlationId: correlationId,
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["targetCell"] = targetCell,
                    ["includedTargets"] = SerializeTargets(included),
                    ["filteredTargets"] = SerializeTargets(filtered, includeReason: true)
                });
        }

        public static void EffectHandlerResult(
            Fight fight,
            FightActor caster,
            Spell spell,
            Effect effect,
            string handlerType,
            string outcome,
            string error = null,
            long? durationMs = null,
            string correlationId = null)
        {
            if (!Enabled || fight == null)
                return;

            correlationId ??= SpellCastTelemetryScope.Current?.CorrelationId;

            Emit("EffectHandlerResult", fight, caster, spell, SpellEffectLayer.Handler, outcome, effect: effect,
                reasonCode: error,
                correlationId: correlationId,
                durationMs: durationMs,
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["handlerType"] = handlerType ?? string.Empty
                });
        }

        public static void DamageComputed(
            Fight fight,
            FightActor source,
            FightActor target,
            Spell spell,
            Effect effect,
            Damage damage,
            int rolledAmount,
            int afterSpellBoost,
            string formulaNotes = null,
            string correlationId = null)
        {
            if (!Enabled || fight == null || damage == null)
                return;

            correlationId ??= SpellCastTelemetryScope.Current?.CorrelationId;

            Emit("DamageComputed", fight, source, spell, SpellEffectLayer.Formula, "Computed", effect: effect,
                correlationId: correlationId,
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["targetId"] = target?.Id ?? 0,
                    ["targetName"] = CombatTelemetry.ResolveActorName(target),
                    ["school"] = damage.EffectSchool.ToString(),
                    ["baseMin"] = damage.BaseMinDamages,
                    ["baseMax"] = damage.BaseMaxDamages,
                    ["rolledAmount"] = rolledAmount,
                    ["fixedBonus"] = damage.FixedBonus,
                    ["afterSpellBoost"] = afterSpellBoost,
                    ["formulaNotes"] = formulaNotes ?? string.Empty
                });
        }

        public static void DamageApplied(
            Fight fight,
            FightActor source,
            FightActor target,
            Spell spell,
            Effect effect,
            int hpBefore,
            int hpAfter,
            int finalDamage,
            int afterResist,
            bool isPoison,
            string correlationId = null)
        {
            if (!Enabled || fight == null || target == null)
                return;

            correlationId ??= SpellCastTelemetryScope.Current?.CorrelationId;

            Emit("DamageApplied", fight, source, spell, SpellEffectLayer.Formula, "Applied", effect: effect,
                correlationId: correlationId,
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["targetId"] = target.Id,
                    ["targetName"] = CombatTelemetry.ResolveActorName(target),
                    ["hpBefore"] = hpBefore,
                    ["hpAfter"] = hpAfter,
                    ["finalDamage"] = finalDamage,
                    ["afterResist"] = afterResist,
                    ["isPoison"] = isPoison
                });
        }

        public static void HealApplied(
            Fight fight,
            FightActor source,
            FightActor target,
            Spell spell,
            Effect effect,
            int hpBefore,
            int hpAfter,
            int amount,
            string correlationId = null)
        {
            if (!Enabled || fight == null || target == null)
                return;

            correlationId ??= SpellCastTelemetryScope.Current?.CorrelationId;

            Emit("HealApplied", fight, source, spell, SpellEffectLayer.Handler, "Applied", effect: effect,
                correlationId: correlationId,
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["targetId"] = target.Id,
                    ["hpBefore"] = hpBefore,
                    ["hpAfter"] = hpAfter,
                    ["amount"] = amount
                });
        }

        public static void BuffApplied(
            Fight fight,
            FightActor target,
            Buff buff,
            string buffKind,
            string correlationId = null)
        {
            if (!Enabled || fight == null || buff == null)
                return;

            correlationId ??= SpellCastTelemetryScope.Current?.CorrelationId;

            Emit("BuffApplied", fight, buff.Caster, buff.Spell, SpellEffectLayer.BuffLifecycle, "Applied", effect: buff.Effect,
                correlationId: correlationId,
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["targetId"] = target?.Id ?? 0,
                    ["buffId"] = buff.Id,
                    ["buffKind"] = buffKind ?? buff.GetType().Name,
                    ["duration"] = buff.Duration,
                    ["triggerCondition"] = buff is TriggerBuff tb ? tb.TriggerType.ToString() : string.Empty
                });
        }

        public static void DelayedEffectScheduled(
            Fight fight,
            FightActor creator,
            FightActor carrier,
            Spell spell,
            Effect effect,
            string kind,
            string expectedTrigger,
            short durationRemaining,
            int expectedDamageMin = 0,
            int expectedDamageMax = 0,
            string correlationId = null)
        {
            if (!Enabled || fight == null)
                return;

            correlationId ??= SpellCastTelemetryScope.Current?.CorrelationId;

            Emit("DelayedEffectScheduled", fight, creator, spell, SpellEffectLayer.BuffLifecycle, "Scheduled", effect: effect,
                correlationId: correlationId,
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["kind"] = kind,
                    ["carrierId"] = carrier?.Id ?? 0,
                    ["expectedTrigger"] = expectedTrigger,
                    ["durationRemaining"] = durationRemaining,
                    ["expectedDamageMin"] = expectedDamageMin,
                    ["expectedDamageMax"] = expectedDamageMax
                });
        }

        public static void DelayedEffectTick(
            Fight fight,
            FightActor carrier,
            Spell spell,
            Effect effect,
            string kind,
            int amount,
            short remaining,
            bool executed,
            string skipReason = null,
            string correlationId = null)
        {
            if (!Enabled || fight == null)
                return;

            Emit("DelayedEffectTick", fight, carrier, spell, SpellEffectLayer.BuffLifecycle, executed ? "Ticked" : "Skipped", effect: effect,
                reasonCode: skipReason,
                correlationId: correlationId,
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["kind"] = kind,
                    ["carrierId"] = carrier?.Id ?? 0,
                    ["amount"] = amount,
                    ["remaining"] = remaining
                });
        }

        public static void DelayedEffectExpired(
            Fight fight,
            FightActor carrier,
            Spell spell,
            Effect effect,
            string kind,
            bool executed,
            string skipReason = null,
            string correlationId = null)
        {
            if (!Enabled || fight == null)
                return;

            Emit("DelayedEffectExpired", fight, carrier, spell, SpellEffectLayer.BuffLifecycle,
                executed ? "Executed" : "ExpiredWithoutTick", effect: effect,
                reasonCode: skipReason,
                correlationId: correlationId,
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["kind"] = kind,
                    ["carrierId"] = carrier?.Id ?? 0
                });
        }

        public static void SummonAttempt(
            Fight fight,
            FightActor owner,
            Spell spell,
            Effect effect,
            int monsterTemplateId,
            short chosenCell,
            int summonCount,
            int summonLimit,
            bool usesSlot,
            string correlationId = null)
        {
            if (!Enabled || fight == null)
                return;

            correlationId ??= SpellCastTelemetryScope.Current?.CorrelationId;

            Emit("SummonAttempt", fight, owner, spell, SpellEffectLayer.Handler, "Attempt", effect: effect,
                correlationId: correlationId,
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["monsterTemplateId"] = monsterTemplateId,
                    ["ownerTeam"] = ResolveTeamLabel(owner),
                    ["chosenCell"] = chosenCell,
                    ["summonCount"] = summonCount,
                    ["summonLimit"] = summonLimit,
                    ["usesSlot"] = usesSlot
                });
        }

        public static void SummonResult(
            Fight fight,
            FightActor owner,
            Spell spell,
            Effect effect,
            bool success,
            int? summonFighterId,
            int monsterTemplateId,
            short cell,
            string failReason = null,
            string aiType = null,
            string correlationId = null)
        {
            if (!Enabled || fight == null)
                return;

            correlationId ??= SpellCastTelemetryScope.Current?.CorrelationId;

            Emit(success ? "SummonResult" : "SummonFailedReason", fight, owner, spell, SpellEffectLayer.Handler,
                success ? "Success" : "Failed", effect: effect,
                reasonCode: failReason,
                correlationId: correlationId,
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["success"] = success,
                    ["summonFighterId"] = summonFighterId ?? 0,
                    ["monsterTemplateId"] = monsterTemplateId,
                    ["cell"] = cell,
                    ["aiType"] = aiType ?? string.Empty
                });
        }

        public static void AiSpellCandidate(
            Fight fight,
            FightActor fighter,
            Spell spell,
            short targetCell,
            int? targetFighterId = null)
        {
            if (!Enabled || fight == null || fighter == null || spell == null)
                return;

            Emit("AiSpellCandidate", fight, fighter, spell, SpellEffectLayer.Ai, "Candidate",
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["targetCell"] = targetCell,
                    ["targetId"] = targetFighterId ?? 0,
                    ["monsterTemplateId"] = ResolveMonsterTemplateId(fighter)
                });
        }

        public static void AiSpellRejected(
            Fight fight,
            FightActor fighter,
            Spell spell,
            short targetCell,
            string reasonCode,
            string rejectLayer = SpellEffectLayer.Ai,
            int? targetFighterId = null)
        {
            if (!Enabled || fight == null || fighter == null || spell == null)
                return;

            Emit("AiSpellRejected", fight, fighter, spell, rejectLayer, "Rejected",
                reasonCode: reasonCode,
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["targetCell"] = targetCell,
                    ["targetId"] = targetFighterId ?? 0,
                    ["monsterTemplateId"] = ResolveMonsterTemplateId(fighter)
                });
        }

        public static void AiSpellSelected(
            Fight fight,
            FightActor fighter,
            Spell spell,
            short targetCell,
            int? targetFighterId = null)
        {
            if (!Enabled || fight == null || fighter == null || spell == null)
                return;

            Emit("AiSpellSelected", fight, fighter, spell, SpellEffectLayer.Ai, "Selected",
                extra: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["targetCell"] = targetCell,
                    ["targetId"] = targetFighterId ?? 0,
                    ["monsterTemplateId"] = ResolveMonsterTemplateId(fighter)
                });
        }

        public static void BuffTriggered(
            Fight fight,
            FightActor target,
            Buff buff,
            string triggerType,
            bool fired,
            bool bonusAppliedToStats,
            IDictionary<string, object> statsSnapshot = null,
            string correlationId = null)
        {
            if (!Enabled || fight == null || buff == null)
                return;

            var extra = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["targetId"] = target?.Id ?? 0,
                ["triggerType"] = triggerType ?? string.Empty,
                ["fired"] = fired,
                ["bonusAppliedToStats"] = bonusAppliedToStats
            };

            if (statsSnapshot != null)
                extra["statsSnapshot"] = statsSnapshot;

            Emit("BuffTriggered", fight, buff.Caster, buff.Spell, SpellEffectLayer.BuffLifecycle,
                fired ? "Triggered" : "Skipped", effect: buff.Effect,
                correlationId: correlationId,
                extra: extra);
        }

        public static void EmitBuffTriggered(
            Fight fight,
            FightActor target,
            PunishmentBuff buff,
            string statName,
            int bonusApplied,
            int statTotalAfter,
            bool bonusAppliedToStats)
        {
            if (!Enabled || fight == null || buff == null)
                return;

            var snapshot = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["stat"] = statName,
                ["bonusApplied"] = bonusApplied,
                ["statTotalAfter"] = statTotalAfter,
                ["currentBoost"] = buff.CurrentBoost,
                ["maxBoost"] = buff.MaxBoost
            };

            BuffTriggered(fight, target, buff, "AfterDamaged", true, bonusAppliedToStats, snapshot);
        }

        private static void Emit(
            string eventName,
            Fight fight,
            FightActor caster,
            Spell spell,
            string layer,
            string result,
            string reasonCode = null,
            string correlationId = null,
            long? durationMs = null,
            Effect effect = null,
            IDictionary<string, object> extra = null)
        {
            var fields = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["layer"] = layer,
                ["result"] = result ?? string.Empty
            };

            if (!string.IsNullOrWhiteSpace(reasonCode))
                fields["reasonCode"] = reasonCode;

            if (!string.IsNullOrWhiteSpace(correlationId))
                fields["correlationId"] = correlationId;

            if (effect != null)
            {
                fields["effectId"] = effect.Id.ToString();
                fields["actionId"] = effect.Id;
            }

            if (extra != null)
            {
                foreach (var pair in extra)
                    fields[pair.Key] = pair.Value;
            }

            CombatTelemetry.LogSpellEffectEvent(eventName, fight, caster, spell, durationMs, fields);
        }

        private static List<Dictionary<string, object>> SerializeTargets(
            IEnumerable<EffectTargetEntry> entries,
            bool includeReason = false)
        {
            var list = new List<Dictionary<string, object>>();
            if (entries == null)
                return list;

            foreach (var entry in entries)
            {
                if (entry?.Actor == null)
                    continue;

                var actor = entry.Actor;
                var item = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["fighterId"] = actor.Id,
                    ["name"] = CombatTelemetry.ResolveActorName(actor),
                    ["type"] = CombatTelemetry.ResolveActorType(actor),
                    ["team"] = ResolveTeamLabel(actor),
                    ["cell"] = actor.Position?.Cell ?? (short)0,
                    ["isSummon"] = actor is ISummoned,
                    ["ownerId"] = actor is ISummoned summoned ? summoned.Summoner?.Id ?? 0 : 0,
                    ["hpBefore"] = Math.Max(0, actor.Stats?.Health?.Total ?? 0)
                };

                if (includeReason && !string.IsNullOrWhiteSpace(entry.FilterReason))
                    item["filterReason"] = entry.FilterReason;

                list.Add(item);
            }

            return list;
        }

        private static int ResolveMonsterTemplateId(FightActor fighter)
        {
            if (fighter is MonsterFighter monsterFighter && monsterFighter.Monster?.Record != null)
                return monsterFighter.Monster.Record.Id;
            return 0;
        }

        private static string ResolveTeamLabel(FightActor actor)
        {
            if (actor?.Team == null)
                return string.Empty;

            if (actor.Team.Attackers != null && actor.Team.Attackers.Contains(actor))
                return "Challenger";

            if (actor.Team.Defenders != null && actor.Team.Defenders.Contains(actor))
                return "Defender";

            return "Unknown";
        }
    }
}
