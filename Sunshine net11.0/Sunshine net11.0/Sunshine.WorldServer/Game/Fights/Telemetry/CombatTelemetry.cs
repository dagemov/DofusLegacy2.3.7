using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Threading;
using Sunshine.BaseServer.Configuration;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Monsters;

namespace Sunshine.WorldServer.Game.Fights.Telemetry
{
    internal static class CombatTelemetry
    {
        public const string SchemaVersion = "combat-telemetry-phase2-jsonl-1";

        private static readonly object SyncRoot = new object();
        private static StreamWriter _turnFlowWriter;
        private static StreamWriter _spellCastWriter;
        private static string _turnFlowPath;
        private static string _spellCastPath;
        private static bool _initialized;
        private static int _initFailedLogged;

        public static bool Enabled => ResolveEnabled();
        public static bool WriteTurnFlow => Enabled && ResolveBool("CombatTelemetryWriteTurnFlow", true);
        public static bool WriteSpellCasts => Enabled && ResolveBool("CombatTelemetryWriteSpellCasts", true);

        public static void LogTurnEvent(
            string eventName,
            Fight fight,
            FightActor actor = null,
            long? durationMs = null,
            string detail = null,
            IDictionary<string, object> extra = null)
        {
            if (!WriteTurnFlow || fight == null)
                return;

            EnsureInitialized();
            if (_turnFlowWriter == null)
                return;

            var payload = BuildBasePayload(eventName, fight, actor, durationMs, detail, extra);
            WriteJsonLine(_turnFlowWriter, payload);
        }

        public static void LogReadyCheckerEvent(
            string eventName,
            Fight fight,
            FightActor turnOwner,
            FightActor actorOverride = null,
            FightActor nextActor = null,
            long? elapsedMs = null,
            string reason = null,
            IEnumerable<CharacterFighter> waiters = null)
        {
            if (!WriteTurnFlow || fight == null)
                return;

            var extra = new Dictionary<string, object>(StringComparer.Ordinal);
            if (nextActor != null)
            {
                extra["nextActorId"] = nextActor.Id;
                extra["nextActorName"] = ResolveActorName(nextActor);
            }

            if (!string.IsNullOrWhiteSpace(reason))
                extra["reason"] = reason;

            if (waiters != null)
            {
                var waiterIds = waiters.Where(x => x != null).Select(x => x.Id).ToArray();
                if (waiterIds.Length > 0)
                    extra["waiterIds"] = string.Join(",", waiterIds);
            }

            LogTurnEvent(eventName, fight, actorOverride ?? turnOwner, elapsedMs, reason, extra);
        }

        public static void LogSpellEvent(
            string eventName,
            Fight fight,
            FightActor caster,
            int? spellId = null,
            short? spellLevel = null,
            object targetIds = null,
            object effectIds = null,
            string result = null,
            string error = null,
            long? durationMs = null)
        {
            if (!WriteSpellCasts || fight == null)
                return;

            EnsureInitialized();
            if (_spellCastWriter == null)
                return;

            var payload = BuildBasePayload(eventName, fight, caster, durationMs, null, null);
            if (spellId.HasValue)
                payload["spellId"] = spellId.Value;
            if (spellLevel.HasValue)
                payload["spellLevel"] = spellLevel.Value;
            if (!string.IsNullOrWhiteSpace(result))
                payload["result"] = result;
            if (!string.IsNullOrWhiteSpace(error))
                payload["error"] = error;
            if (targetIds != null)
                payload["targetIds"] = FormatIdList(targetIds);
            if (effectIds != null)
                payload["effectIds"] = FormatIdList(effectIds);

            WriteJsonLine(_spellCastWriter, payload);
        }

        public static string ResolveTurnId(Fight fight, FightActor actor)
        {
            if (fight == null)
                return string.Empty;

            var round = fight.TimeLine?.RoundNumber ?? 0;
            var actorId = actor?.Id ?? fight.FighterPlaying?.Id ?? 0;
            return string.Format(CultureInfo.InvariantCulture, "{0}-{1}", round, actorId);
        }

        public static string ResolveActorName(FightActor actor)
        {
            if (actor == null)
                return string.Empty;

            if (actor is CharacterFighter characterFighter)
                return characterFighter.Name ?? string.Empty;

            if (actor is MonsterFighter monsterFighter && monsterFighter.Monster != null)
            {
                var record = monsterFighter.Monster.Record;
                if (record != null && !string.IsNullOrWhiteSpace(record.Name))
                    return record.Name;
                return monsterFighter.Monster.Id.ToString(CultureInfo.InvariantCulture);
            }

            return actor.GetType().Name;
        }

        public static string ResolveActorType(FightActor actor)
        {
            return actor == null ? string.Empty : actor.GetType().Name;
        }

        private static Dictionary<string, object> BuildBasePayload(
            string eventName,
            Fight fight,
            FightActor actor,
            long? durationMs,
            string detail,
            IDictionary<string, object> extra)
        {
            var resolvedActor = actor ?? fight.FighterPlaying;
            var payload = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["schemaVersion"] = SchemaVersion,
                ["timestampUtc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                ["event"] = eventName,
                ["fightId"] = fight.Id,
                ["turnId"] = ResolveTurnId(fight, resolvedActor),
                ["threadId"] = Environment.CurrentManagedThreadId
            };

            if (resolvedActor != null)
            {
                payload["actorId"] = resolvedActor.Id;
                payload["actorName"] = ResolveActorName(resolvedActor);
                payload["actorType"] = ResolveActorType(resolvedActor);
            }

            if (durationMs.HasValue)
                payload["durationMs"] = durationMs.Value;

            if (!string.IsNullOrWhiteSpace(detail))
                payload["detail"] = detail;

            if (extra != null)
            {
                foreach (var pair in extra)
                    payload[pair.Key] = pair.Value;
            }

            return payload;
        }

        private static void WriteJsonLine(StreamWriter writer, Dictionary<string, object> payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                lock (SyncRoot)
                {
                    writer.WriteLine(json);
                    writer.Flush();
                }
            }
            catch
            {
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
                return;

            lock (SyncRoot)
            {
                if (_initialized)
                    return;

                try
                {
                    var root = ResolveLogDirectory();
                    Directory.CreateDirectory(root);

                    var spellCastDir = Path.Combine(root, "spell-casts");
                    Directory.CreateDirectory(spellCastDir);

                    var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                    _turnFlowPath = Path.Combine(root, $"combat-turn-flow-{stamp}.jsonl");
                    _spellCastPath = Path.Combine(spellCastDir, $"spell-casts-{stamp}.jsonl");

                    _turnFlowWriter = new StreamWriter(new FileStream(_turnFlowPath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    {
                        AutoFlush = false
                    };

                    _spellCastWriter = new StreamWriter(new FileStream(_spellCastPath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    {
                        AutoFlush = false
                    };
                }
                catch (Exception ex)
                {
                    if (Interlocked.CompareExchange(ref _initFailedLogged, 1, 0) == 0)
                        Logs.Logger.WriteError($"CombatTelemetry: failed to initialize log writers. {ex.Message}");
                }
                finally
                {
                    _initialized = true;
                }
            }
        }

        private static string ResolveLogDirectory()
        {
            var fromEnv = Environment.GetEnvironmentVariable("FIGHT_TELEMETRY_LOG_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(fromEnv))
                return Path.GetFullPath(fromEnv);

            var fromConfig = GameConfig.GetString("CombatTelemetryLogDirectory", string.Empty);
            if (!string.IsNullOrWhiteSpace(fromConfig))
                return Path.GetFullPath(fromConfig);

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "combat");
        }

        private static bool ResolveEnabled()
        {
            var env = Environment.GetEnvironmentVariable("FIGHT_TELEMETRY_ENABLED");
            if (!string.IsNullOrWhiteSpace(env) && TryParseBool(env, out var envEnabled))
                return envEnabled;

            if (string.Equals(Environment.GetEnvironmentVariable("COMBAT_HEALTH_LAB"), "1", StringComparison.Ordinal))
                return ResolveBool("CombatTelemetryEnabled", true);

            return ResolveBool("CombatTelemetryEnabled", false);
        }

        private static bool ResolveBool(string key, bool defaultValue)
        {
            var raw = GameConfig.GetString(key, defaultValue ? "true" : "false");
            return TryParseBool(raw, out var value) ? value : defaultValue;
        }

        private static string FormatIdList(object ids)
        {
            if (ids == null)
                return string.Empty;

            if (ids is string text)
                return text;

            if (ids is System.Collections.IEnumerable enumerable && ids is not string)
            {
                var builder = new StringBuilder();
                foreach (var item in enumerable)
                {
                    if (item == null)
                        continue;

                    if (builder.Length > 0)
                        builder.Append(',');

                    builder.Append(Convert.ToInt32(item, CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }

            return Convert.ToInt32(ids, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        }

        private static bool TryParseBool(string raw, out bool value)
        {
            value = false;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            if (bool.TryParse(raw, out value))
                return true;

            if (raw == "1")
            {
                value = true;
                return true;
            }

            if (raw == "0")
                return true;

            return false;
        }
    }
}
