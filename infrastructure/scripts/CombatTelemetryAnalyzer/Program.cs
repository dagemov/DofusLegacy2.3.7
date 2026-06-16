using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

try
{
    var options = AnalyzerOptions.Parse(args);
    var analyzer = new CombatTelemetryAnalyzer(options);
    return await analyzer.RunAsync();
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    return 1;
}

internal sealed record AnalyzerOptions(string InputDirectory, string OutputFile, string JsonOutputFile)
{
    public static AnalyzerOptions Parse(string[] args)
    {
        string? input = default;
        string? output = default;
        string? jsonOutput = default;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--input":
                    input = index + 1 < args.Length ? args[++index] : throw new ArgumentException("Missing value for --input.");
                    break;
                case "--output":
                    output = index + 1 < args.Length ? args[++index] : throw new ArgumentException("Missing value for --output.");
                    break;
                case "--json-output":
                    jsonOutput = index + 1 < args.Length ? args[++index] : throw new ArgumentException("Missing value for --json-output.");
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
            throw new ArgumentException("Usage: --input <combat-log-directory> --output <markdown-report-path> [--json-output <report.json>]");

        input = Path.GetFullPath(input);
        output = Path.GetFullPath(output);
        jsonOutput = string.IsNullOrWhiteSpace(jsonOutput)
            ? Path.Combine(Path.GetDirectoryName(output) ?? input, "report.json")
            : Path.GetFullPath(jsonOutput);

        return new AnalyzerOptions(input, output, jsonOutput);
    }
}

internal sealed class CombatTelemetryAnalyzer
{
    private static readonly Regex FieldRegex = new(@"(?<key>[A-Za-z][A-Za-z0-9]*)=(?:""(?<quoted>[^""]*)""|(?<bare>\S+))",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SpellIdRegex = new(@"\bspellId=(?<spellId>-?\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex EffectIdRegex = new(@"\beffectId=(?<effectId>-?\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex HandlerTypeRegex = new(@"\bhandlerType=(?<handlerType>[A-Za-z0-9_]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex SuccessRegex = new(@"\bsuccess=(?<success>true|false)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ResultRegex = new(@"\bresult=(?<result>[A-Za-z0-9_]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] PreferredPhaseOrder =
    {
        "FightManager.CreatePvM",
        "StartPlacement",
        "StartFight",
        "NewTurn",
        "FightActor.StartTurn",
        "Brain.Play",
        "Brain.SelectTarget",
        "Brain.MoveNear",
        "Brain.TrySpell",
        "PathFinder.Resolve",
        "FightActor.CastSpell",
        "CastSpell.ValidateBasic",
        "CastSpell.ValidateLos",
        "CastSpell.ValidateHistory",
        "CastSpell.CreateSpellCast",
        "CastSpell.StartSequence",
        "CastSpell.SendMessage",
        "CastSpell.RevealInvisible",
        "CastSpell.UseAP",
        "CastSpell.ApplyHandlers",
        "SpellCast.ApplyHandlers",
        "ApplyHandlers.Handler",
        "FightPvM.GenerateResults",
        "EndFight",
        "CharacterFighter.OnQuitFight",
    };

    private readonly AnalyzerOptions _options;

    public CombatTelemetryAnalyzer(AnalyzerOptions options) =>
        _options = options;

    public Task<int> RunAsync()
    {
        if (!Directory.Exists(_options.InputDirectory))
            throw new DirectoryNotFoundException($"Can not find combat log directory: {_options.InputDirectory}");

        var logFiles = Directory.GetFiles(_options.InputDirectory, "*.log", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(_options.InputDirectory, "*.jsonl", SearchOption.AllDirectories))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (logFiles.Length is 0)
            throw new InvalidOperationException($"No .log or .jsonl files found in {_options.InputDirectory}");

        var events = new List<TelemetryEvent>(capacity: 4096);
        var turnEvents = new List<TurnEvent>(capacity: 2048);
        var spellEvents = new List<SpellCastTelemetryEvent>(capacity: 2048);
        var filePerfCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var fileTurnCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var fileSpellCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in logFiles)
        {
            var fileLabel = NormalizePath(file, _options.InputDirectory);
            var perfCount = 0;
            var turnCount = 0;
            var spellCount = 0;
            var lineNumber = 0;
            var isJsonl = file.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase);

            foreach (var line in File.ReadLines(file))
            {
                lineNumber++;

                if (isJsonl)
                {
                    if (TryParseJsonlSpellEvent(file, fileLabel, lineNumber, line, out var spellEvent))
                    {
                        spellEvents.Add(spellEvent);
                        spellCount++;
                        continue;
                    }

                    if (TryParseJsonlTurnEvent(file, fileLabel, lineNumber, line, out var jsonTurnEvent))
                    {
                        turnEvents.Add(jsonTurnEvent);
                        turnCount++;
                    }

                    continue;
                }

                if (TryParseEvent(file, fileLabel, lineNumber, line, out var telemetryEvent))
                {
                    events.Add(telemetryEvent);
                    perfCount++;
                    continue;
                }

                if (TryParseTurnEvent(file, fileLabel, lineNumber, line, out var turnEvent))
                {
                    turnEvents.Add(turnEvent);
                    turnCount++;
                }
            }

            filePerfCounts[fileLabel] = perfCount;
            fileTurnCounts[fileLabel] = turnCount;
            fileSpellCounts[fileLabel] = spellCount;
        }

        if (events.Count is 0 && turnEvents.Count is 0 && spellEvents.Count is 0)
            throw new InvalidOperationException($"No combat telemetry lines found in {_options.InputDirectory}");

        var turnAnalysis = BuildTurnAnalysis(turnEvents);
        var report = BuildReport(_options.InputDirectory, logFiles.Select(path => NormalizePath(path, _options.InputDirectory)).ToArray(), filePerfCounts, fileTurnCounts, events, turnEvents, turnAnalysis);
        var turnLatencyReport = BuildTurnLatencyReport(_options.InputDirectory, turnAnalysis);
        var outputDirectory = Path.GetDirectoryName(_options.OutputFile);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        File.WriteAllText(_options.OutputFile, report, new UTF8Encoding(false));

        var jsonReport = BuildJsonReport(
            _options.InputDirectory,
            logFiles.Select(path => NormalizePath(path, _options.InputDirectory)).ToArray(),
            filePerfCounts,
            fileTurnCounts,
            fileSpellCounts,
            events,
            turnEvents,
            spellEvents,
            turnAnalysis);
        File.WriteAllText(_options.JsonOutputFile, JsonSerializer.Serialize(jsonReport, JsonOutputOptions), new UTF8Encoding(false));

        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            var turnLatencyOutputFile = Path.Combine(outputDirectory, "combat-turn-latency-analysis-report.md");
            var turnTransitionOutputFile = Path.Combine(outputDirectory, "combat-turn-transition-phase2-report.md");
            File.WriteAllText(turnLatencyOutputFile, turnLatencyReport, new UTF8Encoding(false));
            File.WriteAllText(turnTransitionOutputFile, BuildTurnTransitionPhase2Report(_options.InputDirectory, turnAnalysis), new UTF8Encoding(false));
            var spellEffectLayerReport = BuildSpellEffectLayerReport(spellEvents);
            var spellEffectLayerOutputFile = Path.Combine(outputDirectory, "spell-effect-layer-report.md");
            File.WriteAllText(spellEffectLayerOutputFile, spellEffectLayerReport, new UTF8Encoding(false));
            Console.WriteLine($"Turn latency report written to {turnLatencyOutputFile}");
            Console.WriteLine($"Turn transition report written to {turnTransitionOutputFile}");
            Console.WriteLine($"Spell effect layer report written to {spellEffectLayerOutputFile}");
        }

        Console.WriteLine($"Analyzed {events.Count} FIGHT-PERF, {turnEvents.Count} turn event(s), {spellEvents.Count} spell event(s) from {logFiles.Length} file(s).");
        Console.WriteLine($"Report written to {_options.OutputFile}");
        Console.WriteLine($"JSON report written to {_options.JsonOutputFile}");
        return Task.FromResult(0);
    }

    private static readonly JsonSerializerOptions JsonOutputOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static bool TryParseJsonlTurnEvent(string filePath, string fileLabel, int lineNumber, string line, out TurnEvent turnEvent)
    {
        turnEvent = default!;

        if (string.IsNullOrWhiteSpace(line) || !line.TrimStart().StartsWith('{'))
            return false;

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (!root.TryGetProperty("event", out var eventProperty))
                return false;

            var rawEvent = eventProperty.GetString();
            if (string.IsNullOrWhiteSpace(rawEvent) || !IsJsonlTurnFlowEvent(rawEvent))
                return false;

            var mappedEvent = MapJsonlTurnEventName(rawEvent);
            var fightId = TryReadShort(root, "fightId") ?? 0;
            var round = TryReadRound(root);
            var atUnixMs = TryReadTimestampUtcMs(root);
            var actorId = TryReadInt(root, "actorId");
            var actorType = TryReadString(root, "actorType");
            var detail = TryReadString(root, "detail");
            var source = TryParseDetailValue(detail, "source");
            var timerType = TryParseDetailValue(detail, "timer");

            turnEvent = new TurnEvent(
                FileName: fileLabel,
                FilePath: filePath,
                LineNumber: lineNumber,
                FightId: fightId,
                Round: round,
                EventName: mappedEvent,
                AtUnixMs: atUnixMs,
                FighterId: actorId,
                FighterType: actorType,
                MonsterId: string.Equals(actorType, "MonsterFighter", StringComparison.Ordinal) ? TryReadShort(root, "actorId") : null,
                ElapsedMs: TryReadLong(root, "durationMs"),
                ElapsedSinceTurnStartMs: null,
                Source: source,
                Waiters: null,
                Missing: null,
                ActiveSequences: null,
                PendingSequences: null,
                TimerType: string.IsNullOrWhiteSpace(timerType) ? null : timerType,
                TimeoutMs: TryParseDetailInt(detail, "intervalMs"),
                TurnTimeMs: null,
                Status: null,
                Detail: detail,
                RawLine: line);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseJsonlSpellEvent(string filePath, string fileLabel, int lineNumber, string line, out SpellCastTelemetryEvent spellEvent)
    {
        spellEvent = default!;

        if (string.IsNullOrWhiteSpace(line) || !line.TrimStart().StartsWith('{'))
            return false;

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (!root.TryGetProperty("event", out var eventProperty))
                return false;

            var rawEvent = eventProperty.GetString();
            if (string.IsNullOrWhiteSpace(rawEvent) || !IsJsonlSpellEvent(rawEvent))
                return false;

            spellEvent = new SpellCastTelemetryEvent(
                FileName: fileLabel,
                FilePath: filePath,
                LineNumber: lineNumber,
                EventName: rawEvent,
                TimestampUtc: TryReadString(root, "timestampUtc"),
                FightId: TryReadShort(root, "fightId"),
                TurnId: TryReadString(root, "turnId"),
                CasterId: TryReadInt(root, "actorId"),
                CasterName: TryReadString(root, "actorName"),
                SpellId: TryReadInt(root, "spellId"),
                SpellLevel: TryReadShort(root, "spellLevel"),
                TargetIds: TryReadString(root, "targetIds"),
                EffectIds: TryReadString(root, "effectIds"),
                Result: TryReadString(root, "result"),
                Error: TryReadString(root, "error"),
                DurationMs: TryReadLong(root, "durationMs"),
                Layer: TryReadString(root, "layer"),
                ReasonCode: TryReadString(root, "reasonCode"),
                CorrelationId: TryReadString(root, "correlationId"),
                RawLine: line);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsJsonlTurnFlowEvent(string eventName) =>
        eventName is "FightStarted"
            or "TurnStarted"
            or "TurnOwner"
            or "TurnTimerStarted"
            or "AiStarted"
            or "AiActionSelected"
            or "AiFinished"
            or "EndTurnRequested"
            or "EndTurnCompleted"
            or "NextTurnRequested"
            or "NextTurnStarted"
            or "TimerElapsed"
            or "FightEnded"
            or "GameFightTurnReadyMessageReceived";

    private static bool IsJsonlSpellEvent(string eventName) =>
        eventName is "SpellCastStarted"
            or "SpellCastResolved"
            or "SpellCastFailed"
            or "EffectResolved"
            or "EffectFailed"
            or "SpellCastAttempt"
            or "SpellValidationResult"
            or "SpellEffectPlanned"
            or "EffectTargetsResolved"
            or "EffectHandlerResult"
            or "DamageComputed"
            or "DamageApplied"
            or "HealApplied"
            or "BuffApplied"
            or "DelayedEffectScheduled"
            or "DelayedEffectTick"
            or "DelayedEffectExpired"
            or "SummonAttempt"
            or "SummonResult"
            or "SummonFailedReason"
            or "AiSpellCandidate"
            or "AiSpellRejected"
            or "AiSpellSelected"
            or "BuffTriggered";

    private static string MapJsonlTurnEventName(string eventName) =>
        eventName switch
        {
            "TurnStarted" => "TurnStart",
            "AiStarted" => "AIStart",
            "AiFinished" => "AIEnd",
            _ => eventName
        };

    private static short TryReadRound(JsonElement root)
    {
        if (TryReadShort(root, "round") is { } explicitRound)
            return explicitRound;

        var turnId = TryReadString(root, "turnId");
        if (string.IsNullOrWhiteSpace(turnId))
            return 0;

        var dashIndex = turnId.IndexOf('-', StringComparison.Ordinal);
        if (dashIndex <= 0)
            return 0;

        return short.TryParse(turnId.AsSpan(0, dashIndex), NumberStyles.Integer, CultureInfo.InvariantCulture, out var round)
            ? round
            : (short)0;
    }

    private static long TryReadTimestampUtcMs(JsonElement root)
    {
        var timestamp = TryReadString(root, "timestampUtc");
        if (string.IsNullOrWhiteSpace(timestamp))
            return 0;

        return DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? new DateTimeOffset(parsed.ToUniversalTime()).ToUnixTimeMilliseconds()
            : 0;
    }

    private static string? TryReadString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static short? TryReadShort(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
            ? (short)value
            : null;

    private static int? TryReadInt(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
            ? value
            : null;

    private static long? TryReadLong(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) && property.TryGetInt64(out var value)
            ? value
            : null;

    private static string? TryParseDetailValue(string? detail, string key)
    {
        if (string.IsNullOrWhiteSpace(detail))
            return null;

        foreach (var segment in detail.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = segment.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && string.Equals(parts[0], key, StringComparison.OrdinalIgnoreCase))
                return parts[1];
        }

        return null;
    }

    private static int? TryParseDetailInt(string? detail, string key)
    {
        var value = TryParseDetailValue(detail, key);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static object BuildJsonReport(
        string inputDirectory,
        string[] logFiles,
        IReadOnlyDictionary<string, int> filePerfCounts,
        IReadOnlyDictionary<string, int> fileTurnCounts,
        IReadOnlyDictionary<string, int> fileSpellCounts,
        IReadOnlyList<TelemetryEvent> events,
        IReadOnlyList<TurnEvent> turnEvents,
        IReadOnlyList<SpellCastTelemetryEvent> spellEvents,
        TurnAnalysis turnAnalysis)
    {
        return new
        {
            schemaVersion = "combat-telemetry-analyzer-report-1",
            generatedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            inputDirectory,
            files = logFiles,
            counts = new
            {
                perfEvents = events.Count,
                turnEvents = turnEvents.Count,
                spellEvents = spellEvents.Count,
                filePerfCounts,
                fileTurnCounts,
                fileSpellCounts
            },
            turnAnalysis = new
            {
                turnAnalysis.TotalTurnEvents,
                turnAnalysis.FilesAnalyzed,
                turnAnalysis.MonsterAverageTurnMs,
                turnAnalysis.MonsterMaxTurnMs,
                turnAnalysis.PlayerAverageTurnMs,
                turnAnalysis.PlayerMaxTurnMs,
                turnAnalysis.TurnsOver5s,
                turnAnalysis.TurnsOver30s,
                turnAnalysis.ReadyCheckerStartCount,
                turnAnalysis.ReadyCheckerAckCount,
                turnAnalysis.ReadyCheckerTimeoutCount,
                turnAnalysis.TimerElapsedCount,
                turnAnalysis.TurnsEndedByTimerCount,
                turnAnalysis.PendingSequenceTurnCount,
                turnAnalysis.CauseCounts,
                turnAnalysis.EndTurnMissingCount,
                readyMessageReceivedCount = turnEvents.Count(entry => string.Equals(entry.EventName, "GameFightTurnReadyMessageReceived", StringComparison.Ordinal))
            },
            spellCastSummary = new
            {
                started = spellEvents.Count(entry => entry.EventName == "SpellCastStarted"),
                resolved = spellEvents.Count(entry => entry.EventName == "SpellCastResolved"),
                failed = spellEvents.Count(entry => entry.EventName == "SpellCastFailed"),
                effectsResolved = spellEvents.Count(entry => entry.EventName == "EffectResolved"),
                effectsFailed = spellEvents.Count(entry => entry.EventName == "EffectFailed"),
                averageResolvedDurationMs = spellEvents
                    .Where(entry => entry.DurationMs.HasValue && entry.EventName is "SpellCastResolved" or "EffectResolved")
                    .Select(entry => entry.DurationMs!.Value)
                    .DefaultIfEmpty(0)
                    .Average()
            }
        };
    }

    private static bool TryParseEvent(string filePath, string fileLabel, int lineNumber, string line, out TelemetryEvent telemetryEvent)
    {
        telemetryEvent = default!;

        if (!line.StartsWith("[FIGHT-PERF]", StringComparison.Ordinal))
            return false;

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in FieldRegex.Matches(line))
        {
            var key = match.Groups["key"].Value;
            var value = match.Groups["quoted"].Success ? match.Groups["quoted"].Value : match.Groups["bare"].Value;
            fields[key] = value;
        }

        if (!fields.TryGetValue("phase", out var phase) || string.IsNullOrWhiteSpace(phase))
            return false;

        fields.TryGetValue("detail", out var detail);
        fields.TryGetValue("status", out var status);
        fields.TryGetValue("exceptionType", out var exceptionType);
        fields.TryGetValue("exceptionMessage", out var exceptionMessage);

        telemetryEvent = new TelemetryEvent(
            FileName: fileLabel,
            FilePath: filePath,
            LineNumber: lineNumber,
            Phase: phase,
            FightId: TryParseShort(fields, "fightId"),
            FighterId: TryParseInt(fields, "fighterId"),
            MonsterId: TryParseShort(fields, "monsterId"),
            SpellId: TryParseSpellId(fields, detail),
            EffectId: TryParseEffectId(fields, detail),
            HandlerType: TryParseHandlerType(fields, detail),
            ElapsedMs: TryParseLong(fields, "elapsedMs") ?? 0,
            Slow: TryParseBool(fields, "slow"),
            ThresholdMs: TryParseInt(fields, "thresholdMs") ?? 0,
            ObservedMessageFanOut: TryParseLong(fields, "observedMessageFanOut"),
            Status: string.IsNullOrWhiteSpace(status) ? "unknown" : status,
            Detail: detail,
            Success: TryParseSuccess(fields, detail),
            Result: TryParseResult(fields, detail),
            ExceptionType: exceptionType,
            ExceptionMessage: exceptionMessage,
            RawLine: line);

        return true;
    }

    private static bool TryParseTurnEvent(string filePath, string fileLabel, int lineNumber, string line, out TurnEvent turnEvent)
    {
        turnEvent = default!;

        if (!line.StartsWith("[FIGHT-TURN]", StringComparison.Ordinal))
            return false;

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in FieldRegex.Matches(line))
        {
            var key = match.Groups["key"].Value;
            var value = match.Groups["quoted"].Success ? match.Groups["quoted"].Value : match.Groups["bare"].Value;
            fields[key] = value;
        }

        if (!TryParseShort(fields, "fightId").HasValue || !TryParseShort(fields, "round").HasValue || !fields.TryGetValue("event", out var eventName))
            return false;

        fields.TryGetValue("fighterType", out var fighterType);
        fields.TryGetValue("source", out var source);
        fields.TryGetValue("missing", out var missing);
        fields.TryGetValue("timerType", out var timerType);
        fields.TryGetValue("status", out var status);
        fields.TryGetValue("detail", out var detail);

        turnEvent = new TurnEvent(
            FileName: fileLabel,
            FilePath: filePath,
            LineNumber: lineNumber,
            FightId: TryParseShort(fields, "fightId")!.Value,
            Round: TryParseShort(fields, "round")!.Value,
            EventName: eventName,
            AtUnixMs: TryParseLong(fields, "atUnixMs") ?? 0,
            FighterId: TryParseInt(fields, "fighterId"),
            FighterType: fighterType,
            MonsterId: TryParseShort(fields, "monsterId"),
            ElapsedMs: TryParseLong(fields, "elapsedMs"),
            ElapsedSinceTurnStartMs: TryParseLong(fields, "elapsedSinceTurnStartMs"),
            Source: source,
            Waiters: TryParseInt(fields, "waiters"),
            Missing: missing,
            ActiveSequences: TryParseInt(fields, "activeSequences") ?? TryParseInt(fields, "sequences"),
            PendingSequences: TryParseInt(fields, "pending"),
            TimerType: timerType,
            TimeoutMs: TryParseInt(fields, "timeoutMs"),
            TurnTimeMs: TryParseInt(fields, "turnTime"),
            Status: status,
            Detail: detail,
            RawLine: line);

        return true;
    }

    private static short? TryParseShort(IReadOnlyDictionary<string, string> fields, string key) =>
        fields.TryGetValue(key, out var raw) && short.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static int? TryParseInt(IReadOnlyDictionary<string, string> fields, string key) =>
        fields.TryGetValue(key, out var raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static long? TryParseLong(IReadOnlyDictionary<string, string> fields, string key) =>
        fields.TryGetValue(key, out var raw) && long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static bool TryParseBool(IReadOnlyDictionary<string, string> fields, string key) =>
        fields.TryGetValue(key, out var raw) && bool.TryParse(raw, out var value) && value;

    private static int? TryParseSpellId(IReadOnlyDictionary<string, string> fields, string? detail)
    {
        if (TryParseInt(fields, "spellId") is { } explicitSpellId)
            return explicitSpellId;

        if (string.IsNullOrWhiteSpace(detail))
            return null;

        var match = SpellIdRegex.Match(detail);
        return match.Success && int.TryParse(match.Groups["spellId"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var spellId)
            ? spellId
            : null;
    }

    private static int? TryParseEffectId(IReadOnlyDictionary<string, string> fields, string? detail)
    {
        if (TryParseInt(fields, "effectId") is { } explicitEffectId)
            return explicitEffectId;

        if (string.IsNullOrWhiteSpace(detail))
            return null;

        var match = EffectIdRegex.Match(detail);
        return match.Success && int.TryParse(match.Groups["effectId"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var effectId)
            ? effectId
            : null;
    }

    private static string? TryParseHandlerType(IReadOnlyDictionary<string, string> fields, string? detail)
    {
        if (fields.TryGetValue("handlerType", out var explicitHandlerType) && !string.IsNullOrWhiteSpace(explicitHandlerType))
            return explicitHandlerType;

        if (string.IsNullOrWhiteSpace(detail))
            return null;

        var match = HandlerTypeRegex.Match(detail);
        return match.Success ? match.Groups["handlerType"].Value : null;
    }

    private static bool? TryParseSuccess(IReadOnlyDictionary<string, string> fields, string? detail)
    {
        if (fields.TryGetValue("success", out var explicitSuccess) && bool.TryParse(explicitSuccess, out var parsedSuccess))
            return parsedSuccess;

        if (string.IsNullOrWhiteSpace(detail))
            return null;

        var match = SuccessRegex.Match(detail);
        return match.Success && bool.TryParse(match.Groups["success"].Value, out parsedSuccess)
            ? parsedSuccess
            : null;
    }

    private static string? TryParseResult(IReadOnlyDictionary<string, string> fields, string? detail)
    {
        if (fields.TryGetValue("result", out var explicitResult) && !string.IsNullOrWhiteSpace(explicitResult))
            return explicitResult;

        if (string.IsNullOrWhiteSpace(detail))
            return null;

        var match = ResultRegex.Match(detail);
        return match.Success ? match.Groups["result"].Value : null;
    }

    private static string BuildReport(
        string inputDirectory,
        string[] logFiles,
        IReadOnlyDictionary<string, int> filePerfCounts,
        IReadOnlyDictionary<string, int> fileTurnCounts,
        IReadOnlyList<TelemetryEvent> events,
        IReadOnlyList<TurnEvent> turnEvents,
        TurnAnalysis turnAnalysis)
    {
        var builder = new StringBuilder();
        var generatedAt = DateTimeOffset.Now;
        var phaseStats = BuildPhaseStats(events);
        var topSlowEvents = events.OrderByDescending(entry => entry.ElapsedMs).ThenBy(entry => entry.FileName, StringComparer.OrdinalIgnoreCase).ThenBy(entry => entry.LineNumber).Take(10).ToArray();
        var monsterStats = BuildMonsterStats(events);
        var spellStats = BuildSpellStats(events);
        var fightStats = BuildFightStats(events);
        var fanOutStats = BuildFanOutStats(events);
        var errorGroups = BuildErrorGroups(events);
        var fineGrainedPhaseStats = BuildFineGrainedPhaseStats(events);
        var handlerStats = BuildHandlerStats(events);

        builder.AppendLine("# Combat Telemetry Analysis Report");
        builder.AppendLine();
        builder.AppendLine($"Generated at: `{generatedAt:yyyy-MM-dd HH:mm:ss zzz}`");
        builder.AppendLine($"Input directory: `{inputDirectory}`");
        builder.AppendLine($"Total log files: `{logFiles.Length}`");
        builder.AppendLine($"Total FIGHT-PERF events: `{events.Count}`");
        builder.AppendLine($"Total FIGHT-TURN events: `{turnEvents.Count}`");
        builder.AppendLine($"Total distinct session fights: `{fightStats.Length}`");
        builder.AppendLine();

        builder.AppendLine("## Visible Turn-Latency Follow-up");
        builder.AppendLine();
        builder.AppendLine("The earlier telemetry pass measured internal combat methods such as AI, spell casting, handlers, and cleanup. This updated report keeps that data and adds `FIGHT-TURN` reconstruction so we can separate inner-method cost from player-visible turn waits between `AIEnd`, `EndTurn`, `ReadyChecker`, and the next visible turn.");
        builder.AppendLine("Phase 2 extends that again with transition-specific checkpoints such as `EndTurnRequested`, `EndTurnBegin`, `EndTurnCompleted`, `EndTurnTimerDispose`, `NextTurnRequested`, and `SequencesClearedBeforeNewTurn`, which lets us distinguish a slow AI from a turn that actually stalls after `EndTurn`.");
        builder.AppendLine();

        builder.AppendLine("## Files Analyzed");
        builder.AppendLine();
        foreach (var file in logFiles)
            builder.AppendLine($"- `{file}`: `perf={filePerfCounts[file]} turn={fileTurnCounts[file]}`");
        builder.AppendLine();

        builder.AppendLine("## Summary By Phase");
        builder.AppendLine();
        builder.AppendLine("| Phase | Count | AvgMs | MaxMs | P50 | P95 | P99 | Slow | Errors | AvgFanOut | MaxFanOut |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
        foreach (var stat in phaseStats)
        {
            builder.AppendLine(
                $"| `{stat.Phase}` | {stat.Count} | {Format(stat.AverageMs)} | {stat.MaxMs} | {stat.P50} | {stat.P95} | {stat.P99} | {stat.SlowCount} | {stat.ErrorCount} | {Format(stat.AverageFanOut)} | {FormatNullable(stat.MaxFanOut)} |");
        }
        builder.AppendLine();

        builder.AppendLine("## Fine-Grained Profiling Summary");
        builder.AppendLine();
        if (fineGrainedPhaseStats.Length is 0)
        {
            builder.AppendLine("No fine-grained `Brain.*`, `CastSpell.*`, or `ApplyHandlers.*` events were found in this capture set.");
        }
        else
        {
            builder.AppendLine("| Phase | Count | AvgMs | MaxMs | P95 | Slow | Errors |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: |");
            foreach (var stat in fineGrainedPhaseStats)
            {
                builder.AppendLine(
                    $"| `{stat.Phase}` | {stat.Count} | {Format(stat.AverageMs)} | {stat.MaxMs} | {stat.P95} | {stat.SlowCount} | {stat.ErrorCount} |");
            }
        }
        builder.AppendLine();

        builder.AppendLine("## Top 10 Slowest Events");
        builder.AppendLine();
        builder.AppendLine("| Rank | ElapsedMs | Phase | FightId | MonsterId | SpellId | FanOut | Status | Source | Detail |");
        builder.AppendLine("| --- | ---: | --- | ---: | ---: | ---: | ---: | --- | --- | --- |");
        for (var index = 0; index < topSlowEvents.Length; index++)
        {
            var entry = topSlowEvents[index];
            builder.AppendLine(
                $"| {index + 1} | {entry.ElapsedMs} | `{entry.Phase}` | {FormatNullable(entry.FightId)} | {FormatNullable(entry.MonsterId)} | {FormatNullable(entry.SpellId)} | {FormatNullable(entry.ObservedMessageFanOut)} | `{entry.Status}` | `{entry.FileName}:{entry.LineNumber}` | {EscapeTable(entry.Detail, 96)} |");
        }
        builder.AppendLine();

        builder.AppendLine("## Worst Monsters By AI");
        builder.AppendLine();
        if (monsterStats.Length is 0)
        {
            builder.AppendLine("No monster AI events with `monsterId > 0` were found.");
        }
        else
        {
            builder.AppendLine("| MonsterId | AI Events | Avg AI Ms | Max AI Ms | Slow AI | Slow Phases |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | --- |");
            foreach (var stat in monsterStats.Take(10))
            {
                builder.AppendLine(
                    $"| {stat.MonsterId} | {stat.AiEventCount} | {Format(stat.AverageAiMs)} | {stat.MaxAiMs} | {stat.SlowAiCount} | {EscapeTable(string.Join(", ", stat.SlowPhases), 80)} |");
            }
        }
        builder.AppendLine();

        builder.AppendLine("## Worst Spells");
        builder.AppendLine();
        if (spellStats.Length is 0)
        {
            builder.AppendLine("No events exposing `spellId` were found.");
        }
        else
        {
            builder.AppendLine("| SpellId | Events | Avg Ms | Max Ms | Slow | Errors | Phases |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | --- |");
            foreach (var stat in spellStats.Take(10))
            {
                builder.AppendLine(
                    $"| {stat.SpellId} | {stat.EventCount} | {Format(stat.AverageMs)} | {stat.MaxMs} | {stat.SlowCount} | {stat.ErrorCount} | {EscapeTable(string.Join(", ", stat.Phases), 80)} |");
            }
        }
        builder.AppendLine();

        builder.AppendLine("## Worst Handler Types");
        builder.AppendLine();
        if (handlerStats.Length is 0)
        {
            builder.AppendLine("No handler-level `ApplyHandlers.Handler` events were found.");
        }
        else
        {
            builder.AppendLine("| HandlerType | Events | Avg Ms | Max Ms | Slow | Errors | EffectIds | SpellIds |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | --- | --- |");
            foreach (var stat in handlerStats.Take(10))
            {
                builder.AppendLine(
                    $"| `{stat.HandlerType}` | {stat.EventCount} | {Format(stat.AverageMs)} | {stat.MaxMs} | {stat.SlowCount} | {stat.ErrorCount} | {EscapeTable(string.Join(", ", stat.EffectIds), 64)} | {EscapeTable(string.Join(", ", stat.SpellIds), 64)} |");
            }
        }
        builder.AppendLine();

        builder.AppendLine("## Worst Session Fights");
        builder.AppendLine();
        builder.AppendLine("| Session Fight | FightId | Events | Total Observed Ms | Max Event Ms | Worst Phase | Slow | Errors | Max FanOut |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | --- | ---: | ---: | ---: |");
        foreach (var stat in fightStats.Take(10))
        {
            builder.AppendLine(
                $"| `{stat.SessionFight}` | {stat.FightId} | {stat.EventCount} | {stat.TotalObservedMs} | {stat.MaxEventMs} | `{stat.WorstPhase}` | {stat.SlowCount} | {stat.ErrorCount} | {stat.MaxFanOut} |");
        }
        builder.AppendLine();

        builder.AppendLine("## Fan-out Correlation");
        builder.AppendLine();
        builder.AppendLine("| Cohort | Events With FanOut | Avg FanOut | P95 FanOut | Max FanOut | Avg ElapsedMs |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: |");
        builder.AppendLine($"| Slow events | {fanOutStats.SlowCount} | {Format(fanOutStats.SlowAverageFanOut)} | {FormatNullable(fanOutStats.SlowP95FanOut)} | {FormatNullable(fanOutStats.SlowMaxFanOut)} | {Format(fanOutStats.SlowAverageElapsedMs)} |");
        builder.AppendLine($"| Non-slow events | {fanOutStats.NonSlowCount} | {Format(fanOutStats.NonSlowAverageFanOut)} | {FormatNullable(fanOutStats.NonSlowP95FanOut)} | {FormatNullable(fanOutStats.NonSlowMaxFanOut)} | {Format(fanOutStats.NonSlowAverageElapsedMs)} |");
        builder.AppendLine();
        builder.AppendLine(GetFanOutConclusion(phaseStats, fanOutStats));
        builder.AppendLine();

        builder.AppendLine("## Turn Latency Analysis");
        builder.AppendLine();
        if (turnAnalysis.CompletedTurns.Length is 0)
        {
            builder.AppendLine("No `FIGHT-TURN` completion events were found in this sample. This log set predates turn-latency telemetry, so it cannot explain the visible 30-second waits yet.");
        }
        else
        {
            builder.AppendLine($"Completed turns: `{turnAnalysis.CompletedTurns.Length}`");
            builder.AppendLine($"Monster turns avg/max: `{Format(turnAnalysis.MonsterAverageTurnMs)}` / `{turnAnalysis.MonsterMaxTurnMs}` ms");
            builder.AppendLine($"Player turns avg/max: `{Format(turnAnalysis.PlayerAverageTurnMs)}` / `{turnAnalysis.PlayerMaxTurnMs}` ms");
            builder.AppendLine($"Turns > 5000 ms: `{turnAnalysis.TurnsOver5s}`");
            builder.AppendLine($"Turns > 30000 ms: `{turnAnalysis.TurnsOver30s}`");
            builder.AppendLine($"ReadyChecker timeouts: `{turnAnalysis.ReadyCheckerTimeoutCount}`");
            builder.AppendLine($"Turns without explicit EndTurn: `{turnAnalysis.EndTurnMissingCount}`");
            builder.AppendLine();
            builder.AppendLine("Detailed turn-gap breakdown lives in `combat-turn-latency-analysis-report.md`.");
            builder.AppendLine();
            builder.AppendLine("| Session Fight | Round | FighterId | FighterType | MonsterId | DurationMs | AIEnd->EndTurnMs | EndTurn->NextTurnMs | ProbableCause |");
            builder.AppendLine("| --- | ---: | ---: | --- | ---: | ---: | ---: | ---: | --- |");
            foreach (var turn in turnAnalysis.CompletedTurns.OrderByDescending(entry => entry.DurationMs).ThenBy(entry => entry.SessionFight, StringComparer.OrdinalIgnoreCase).Take(10))
            {
            builder.AppendLine($"| `{turn.SessionFight}` | {turn.Round} | {FormatNullable(turn.FighterId)} | `{turn.FighterType}` | {FormatNullable(turn.MonsterId)} | {turn.DurationMs} | {FormatNullable(turn.AiEndToEndTurnMs)} | {FormatNullable(turn.EndTurnToNextTurnMs)} | `{turn.ProbableCauseCode}` |");
            }
        }
        builder.AppendLine();

        builder.AppendLine("## Errors Detected");
        builder.AppendLine();
        if (errorGroups.Length is 0)
        {
            builder.AppendLine("No `status=error` or `exceptionType=` entries were found.");
        }
        else
        {
            builder.AppendLine("| Phase | FightId | MonsterId | SpellId | ExceptionType | Count | Example |");
            builder.AppendLine("| --- | ---: | ---: | ---: | --- | ---: | --- |");
            foreach (var group in errorGroups)
            {
                builder.AppendLine(
                    $"| `{group.Phase}` | {FormatNullable(group.FightId)} | {FormatNullable(group.MonsterId)} | {FormatNullable(group.SpellId)} | `{group.ExceptionType}` | {group.Count} | {EscapeTable(group.Example, 96)} |");
            }
        }
        builder.AppendLine();

        builder.AppendLine("## Conclusions");
        builder.AppendLine();
        foreach (var line in BuildConclusions(events, phaseStats, monsterStats, spellStats, fightStats, fanOutStats, errorGroups, turnAnalysis))
            builder.AppendLine($"- {line}");
        builder.AppendLine();

        builder.AppendLine("## Recommended Next Phase");
        builder.AppendLine();
        foreach (var line in BuildRecommendations(events, phaseStats, monsterStats, spellStats, fightStats, fanOutStats, turnAnalysis))
            builder.AppendLine($"- {line}");

        return builder.ToString();
    }

    private static PhaseStat[] BuildPhaseStats(IReadOnlyList<TelemetryEvent> events)
    {
        var groups = events.GroupBy(entry => entry.Phase, StringComparer.Ordinal).ToDictionary(group => group.Key, group => CreatePhaseStat(group.Key, group), StringComparer.Ordinal);
        var ordered = new List<PhaseStat>();

        foreach (var phase in PreferredPhaseOrder)
        {
            if (groups.Remove(phase, out var stat))
                ordered.Add(stat);
        }

        ordered.AddRange(groups.Values.OrderByDescending(stat => stat.P95).ThenByDescending(stat => stat.MaxMs).ThenBy(stat => stat.Phase, StringComparer.Ordinal));
        return ordered.ToArray();
    }

    private static PhaseStat[] BuildFineGrainedPhaseStats(IReadOnlyList<TelemetryEvent> events) =>
        events
            .Where(entry => entry.Phase.StartsWith("Brain.", StringComparison.Ordinal) ||
                            entry.Phase.StartsWith("CastSpell.", StringComparison.Ordinal) ||
                            entry.Phase.StartsWith("ApplyHandlers.", StringComparison.Ordinal))
            .GroupBy(entry => entry.Phase, StringComparer.Ordinal)
            .Select(group => CreatePhaseStat(group.Key, group))
            .OrderByDescending(stat => stat.MaxMs)
            .ThenByDescending(stat => stat.P95)
            .ThenBy(stat => stat.Phase, StringComparer.Ordinal)
            .ToArray();

    private static PhaseStat CreatePhaseStat(string phase, IEnumerable<TelemetryEvent> events)
    {
        var buffer = events.ToArray();
        var elapsed = buffer.Select(entry => entry.ElapsedMs).OrderBy(value => value).ToArray();
        var fanOuts = buffer.Where(entry => entry.ObservedMessageFanOut.HasValue).Select(entry => entry.ObservedMessageFanOut!.Value).OrderBy(value => value).ToArray();

        return new PhaseStat(
            Phase: phase,
            Count: buffer.Length,
            AverageMs: buffer.Average(entry => entry.ElapsedMs),
            MaxMs: buffer.Max(entry => entry.ElapsedMs),
            P50: Percentile(elapsed, 0.50),
            P95: Percentile(elapsed, 0.95),
            P99: Percentile(elapsed, 0.99),
            SlowCount: buffer.Count(entry => entry.Slow),
            ErrorCount: buffer.Count(entry => !string.Equals(entry.Status, "ok", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(entry.ExceptionType)),
            AverageFanOut: fanOuts.Length is 0 ? null : fanOuts.Average(),
            MaxFanOut: fanOuts.Length is 0 ? null : fanOuts.Max());
    }

    private static MonsterStat[] BuildMonsterStats(IReadOnlyList<TelemetryEvent> events) =>
        events
            .Where(entry => entry.MonsterId is > 0)
            .GroupBy(entry => entry.MonsterId!.Value)
            .Select(group =>
            {
                var aiEvents = group.Where(entry => entry.Phase.StartsWith("Brain.", StringComparison.Ordinal)).ToArray();
                if (aiEvents.Length is 0)
                    return null;

                return new MonsterStat(
                    MonsterId: group.Key,
                    AiEventCount: aiEvents.Length,
                    AverageAiMs: aiEvents.Average(entry => entry.ElapsedMs),
                    MaxAiMs: aiEvents.Max(entry => entry.ElapsedMs),
                    SlowAiCount: aiEvents.Count(entry => entry.Slow),
                    SlowPhases: group.Where(entry => entry.Slow).Select(entry => entry.Phase).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            })
            .Where(stat => stat is not null)
            .Cast<MonsterStat>()
            .OrderByDescending(stat => stat.MaxAiMs)
            .ThenByDescending(stat => stat.AverageAiMs)
            .ThenByDescending(stat => stat.AiEventCount)
            .ToArray();

    private static HandlerStat[] BuildHandlerStats(IReadOnlyList<TelemetryEvent> events) =>
        events
            .Where(entry => string.Equals(entry.Phase, "ApplyHandlers.Handler", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(entry.HandlerType))
            .GroupBy(entry => entry.HandlerType!, StringComparer.Ordinal)
            .Select(group => new HandlerStat(
                HandlerType: group.Key,
                EventCount: group.Count(),
                AverageMs: group.Average(entry => entry.ElapsedMs),
                MaxMs: group.Max(entry => entry.ElapsedMs),
                SlowCount: group.Count(entry => entry.Slow),
                ErrorCount: group.Count(entry => !string.Equals(entry.Status, "ok", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(entry.ExceptionType)),
                EffectIds: group.Where(entry => entry.EffectId.HasValue).Select(entry => entry.EffectId!.Value).Distinct().OrderBy(value => value).ToArray(),
                SpellIds: group.Where(entry => entry.SpellId.HasValue).Select(entry => entry.SpellId!.Value).Distinct().OrderBy(value => value).ToArray()))
            .OrderByDescending(stat => stat.MaxMs)
            .ThenByDescending(stat => stat.AverageMs)
            .ThenByDescending(stat => stat.EventCount)
            .ToArray();

    private static TurnAnalysis BuildTurnAnalysis(IReadOnlyList<TurnEvent> turnEvents)
    {
        if (turnEvents.Count is 0)
            return TurnAnalysis.Empty;

        var completedTurns = new List<CompletedTurn>();
        var readyCheckerStartCount = 0;
        var readyCheckerAckCount = 0;
        var timerElapsedCount = 0;
        var turnsEndedByTimerCount = 0;

        foreach (var sessionFight in turnEvents
                     .GroupBy(entry => $"{entry.FileName}#{entry.FightId}", StringComparer.OrdinalIgnoreCase)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            ActiveTurnState? activeTurn = default;

            foreach (var turnEvent in sessionFight.OrderBy(entry => entry.AtUnixMs).ThenBy(entry => entry.LineNumber))
            {
                if (activeTurn is not null)
                {
                    activeTurn.MaxActiveSequences = Math.Max(activeTurn.MaxActiveSequences, turnEvent.ActiveSequences ?? 0);
                    activeTurn.MaxPendingSequences = Math.Max(activeTurn.MaxPendingSequences, turnEvent.PendingSequences ?? 0);

                    switch (turnEvent.EventName)
                    {
                        case "AIStart":
                            activeTurn.AiStartAtUnixMs ??= turnEvent.AtUnixMs;
                            break;
                        case "AIEnd":
                            activeTurn.MaxAiElapsedMs = Math.Max(activeTurn.MaxAiElapsedMs, turnEvent.ElapsedMs ?? 0);
                            activeTurn.AiEndAtUnixMs ??= turnEvent.AtUnixMs;
                            break;
                        case "EndTurnRequested":
                            activeTurn.EndTurnRequestedCount++;
                            activeTurn.FirstEndTurnRequestedAtUnixMs ??= turnEvent.AtUnixMs;
                            activeTurn.FirstEndTurnSource ??= turnEvent.Source;
                            activeTurn.LastEndTurnSource = turnEvent.Source;
                            break;
                        case "EndTurnBegin":
                            activeTurn.EndTurnBegunAtUnixMs ??= turnEvent.AtUnixMs;
                            activeTurn.FirstEndTurnSource ??= turnEvent.Source;
                            activeTurn.LastEndTurnSource = turnEvent.Source;
                            break;
                        case "EndTurnCalled":
                            activeTurn.EndTurnCalled = true;
                            activeTurn.EndTurnCallCount++;
                            activeTurn.FirstEndTurnAtUnixMs ??= turnEvent.AtUnixMs;
                            activeTurn.LastEndTurnAtUnixMs = turnEvent.AtUnixMs;
                            activeTurn.FirstEndTurnSource ??= turnEvent.Source;
                            activeTurn.LastEndTurnSource = turnEvent.Source;
                            break;
                        case "EndTurnCompleted":
                            activeTurn.EndTurnCompletedAtUnixMs = turnEvent.AtUnixMs;
                            activeTurn.FirstEndTurnSource ??= turnEvent.Source;
                            activeTurn.LastEndTurnSource = turnEvent.Source;
                            break;
                        case "EndTurnTimerDispose":
                            activeTurn.EndTurnTimerDisposed = true;
                            activeTurn.EndTurnTimerDisposedAtUnixMs = turnEvent.AtUnixMs;
                            activeTurn.TimerDisposedSource = turnEvent.Source;
                            break;
                        case "ReadyCheckerStart":
                            readyCheckerStartCount++;
                            activeTurn.ReadyCheckerStarted = true;
                            activeTurn.ReadyCheckerStartCount++;
                            activeTurn.ReadyCheckerStartedAtUnixMs ??= turnEvent.AtUnixMs;
                            activeTurn.ReadyCheckerWaiters ??= turnEvent.Waiters;
                            activeTurn.ReadyCheckerMissing ??= turnEvent.Missing;
                            break;
                        case "ReadyCheckerAck":
                            activeTurn.ReadyCheckerAckCount++;
                            activeTurn.ReadyCheckerWaiters = turnEvent.Waiters;
                            activeTurn.ReadyCheckerMissing = turnEvent.Missing;
                            break;
                        case "ReadyCheckerCompleted":
                            readyCheckerAckCount++;
                            activeTurn.ReadyCheckerOutcome = turnEvent.Source ?? turnEvent.EventName;
                            activeTurn.ReadyCheckerCompletedAtUnixMs = turnEvent.AtUnixMs;
                            activeTurn.ReadyCheckerDurationMs = turnEvent.ElapsedMs;
                            activeTurn.ReadyCheckerWaiters ??= turnEvent.Waiters;
                            activeTurn.ReadyCheckerMissing ??= turnEvent.Missing;
                            break;
                        case "ReadyCheckerTimeout":
                            activeTurn.HadReadyCheckerTimeout = true;
                            activeTurn.ReadyCheckerTimeoutAtUnixMs = turnEvent.AtUnixMs;
                            activeTurn.ReadyCheckerOutcome = activeTurn.ReadyCheckerOutcome is null or "ACK"
                                ? turnEvent.Source ?? turnEvent.EventName
                                : activeTurn.ReadyCheckerOutcome;
                            activeTurn.ReadyCheckerCompletedAtUnixMs = turnEvent.AtUnixMs;
                            activeTurn.ReadyCheckerDurationMs = turnEvent.ElapsedMs;
                            activeTurn.ReadyCheckerWaiters ??= turnEvent.Waiters;
                            activeTurn.ReadyCheckerMissing ??= turnEvent.Missing;
                            break;
                        case "TimerElapsed":
                            timerElapsedCount++;
                            if (string.Equals(turnEvent.TimerType, "EndTurn", StringComparison.OrdinalIgnoreCase))
                            {
                                activeTurn.EndTurnTimerElapsed = true;
                                activeTurn.TimerElapsedAtUnixMs = turnEvent.AtUnixMs;
                                activeTurn.TurnTimerMs = turnEvent.TimeoutMs ?? turnEvent.TurnTimeMs;
                            }
                            break;
                        case "NextTurnRequested":
                            activeTurn.NextTurnRequestedAtUnixMs ??= turnEvent.AtUnixMs;
                            activeTurn.NextTurnRequestSource ??= turnEvent.Source;
                            break;
                        case "PendingSequencesBeforeNewTurn":
                            activeTurn.PendingSequencesObserved = true;
                            break;
                        case "SequencesBeforeEndTurn":
                        case "SequencesAfterEndTurn":
                        case "SequencesClearedBeforeNewTurn":
                            activeTurn.PendingSequencesObserved = activeTurn.PendingSequencesObserved
                                || (turnEvent.PendingSequences ?? 0) > 0
                                || (turnEvent.ActiveSequences ?? 0) > 1;
                            break;
                        case "SequenceStart":
                            activeTurn.SequenceStartCount++;
                            break;
                        case "SequenceAcknowledge":
                            activeTurn.SequenceAcknowledgeCount++;
                            break;
                    }
                }

                switch (turnEvent.EventName)
                {
                    case "TurnStart":
                        activeTurn = new ActiveTurnState
                        {
                            SessionFight = sessionFight.Key,
                            FightId = turnEvent.FightId,
                            Round = turnEvent.Round,
                            FighterId = turnEvent.FighterId,
                            FighterType = string.IsNullOrWhiteSpace(turnEvent.FighterType) ? "Unknown" : turnEvent.FighterType!,
                            MonsterId = turnEvent.MonsterId,
                            StartedAtUnixMs = turnEvent.AtUnixMs,
                            LogFile = turnEvent.FileName,
                            MaxActiveSequences = turnEvent.ActiveSequences ?? 0,
                            MaxPendingSequences = turnEvent.PendingSequences ?? 0,
                        };
                        break;

                    case "NextTurnStarted":
                        if (activeTurn is not null)
                        {
                            completedTurns.Add(CompleteTurn(activeTurn, turnEvent.AtUnixMs, "NextTurn"));
                            if (string.Equals(activeTurn.LastEndTurnSource, "Timer", StringComparison.OrdinalIgnoreCase) || activeTurn.EndTurnTimerElapsed)
                                turnsEndedByTimerCount++;
                            activeTurn = default;
                        }
                        break;

                    case "TurnClosed":
                        if (activeTurn is not null)
                        {
                            completedTurns.Add(CompleteTurn(activeTurn, turnEvent.AtUnixMs, turnEvent.Source ?? "TurnClosed"));
                            if (string.Equals(activeTurn.LastEndTurnSource, "Timer", StringComparison.OrdinalIgnoreCase) || activeTurn.EndTurnTimerElapsed)
                                turnsEndedByTimerCount++;
                            activeTurn = default;
                        }
                        break;
                }
            }
        }

        var monsterTurns = completedTurns.Where(turn => turn.IsMonsterTurn).ToArray();
        var playerTurns = completedTurns.Where(turn => !turn.IsMonsterTurn).ToArray();
        var causeCounts = completedTurns
            .GroupBy(turn => turn.ProbableCauseCode, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        return new TurnAnalysis(
            CompletedTurns: completedTurns.ToArray(),
            TotalTurnEvents: turnEvents.Count,
            FilesAnalyzed: turnEvents.Select(entry => entry.FileName).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            MonsterAverageTurnMs: monsterTurns.Length is 0 ? 0 : monsterTurns.Average(turn => turn.DurationMs),
            MonsterMaxTurnMs: monsterTurns.Length is 0 ? 0 : monsterTurns.Max(turn => turn.DurationMs),
            PlayerAverageTurnMs: playerTurns.Length is 0 ? 0 : playerTurns.Average(turn => turn.DurationMs),
            PlayerMaxTurnMs: playerTurns.Length is 0 ? 0 : playerTurns.Max(turn => turn.DurationMs),
            TurnsOver5s: completedTurns.Count(turn => turn.DurationMs > 5_000),
            TurnsOver30s: completedTurns.Count(turn => turn.DurationMs > 30_000),
            ReadyCheckerStartCount: readyCheckerStartCount,
            ReadyCheckerAckCount: readyCheckerAckCount,
            ReadyCheckerTimeoutCount: completedTurns.Count(turn => turn.HadReadyCheckerTimeout),
            TimerElapsedCount: timerElapsedCount,
            TurnsEndedByTimerCount: turnsEndedByTimerCount,
            PendingSequenceTurnCount: completedTurns.Count(turn => turn.PendingSequencesObserved || turn.MaxActiveSequences > 1 || turn.MaxPendingSequences > 0),
            CauseCounts: causeCounts,
            EndTurnMissingCount: completedTurns.Count(turn => !turn.EndTurnCalled));
    }

    private static CompletedTurn CompleteTurn(ActiveTurnState activeTurn, long completedAtUnixMs, string completionEvent)
    {
        var durationMs = Math.Max(0, completedAtUnixMs - activeTurn.StartedAtUnixMs);
        long? aiDurationMs = activeTurn.AiStartAtUnixMs.HasValue && activeTurn.AiEndAtUnixMs.HasValue
            ? Math.Max(0, activeTurn.AiEndAtUnixMs.Value - activeTurn.AiStartAtUnixMs.Value)
            : activeTurn.MaxAiElapsedMs > 0
                ? activeTurn.MaxAiElapsedMs
                : null;
        long? endTurnRequestedToBeginMs = activeTurn.FirstEndTurnRequestedAtUnixMs.HasValue && activeTurn.EndTurnBegunAtUnixMs.HasValue
            ? Math.Max(0, activeTurn.EndTurnBegunAtUnixMs.Value - activeTurn.FirstEndTurnRequestedAtUnixMs.Value)
            : null;
        long? endTurnBeginToCompletedMs = activeTurn.EndTurnBegunAtUnixMs.HasValue && activeTurn.EndTurnCompletedAtUnixMs.HasValue
            ? Math.Max(0, activeTurn.EndTurnCompletedAtUnixMs.Value - activeTurn.EndTurnBegunAtUnixMs.Value)
            : null;
        long? aiEndToEndTurnMs = activeTurn.AiEndAtUnixMs.HasValue && activeTurn.FirstEndTurnAtUnixMs.HasValue
            ? Math.Max(0, activeTurn.FirstEndTurnAtUnixMs.Value - activeTurn.AiEndAtUnixMs.Value)
            : null;
        var endTurnAnchor = activeTurn.EndTurnCompletedAtUnixMs ?? activeTurn.FirstEndTurnAtUnixMs;
        long? endTurnToNextTurnMs = endTurnAnchor.HasValue
            ? Math.Max(0, completedAtUnixMs - endTurnAnchor.Value)
            : null;
        long? endTurnCompletedToNextTurnRequestedMs = activeTurn.EndTurnCompletedAtUnixMs.HasValue && activeTurn.NextTurnRequestedAtUnixMs.HasValue
            ? Math.Max(0, activeTurn.NextTurnRequestedAtUnixMs.Value - activeTurn.EndTurnCompletedAtUnixMs.Value)
            : null;
        long? nextTurnRequestedToStartedMs = activeTurn.NextTurnRequestedAtUnixMs.HasValue
            ? Math.Max(0, completedAtUnixMs - activeTurn.NextTurnRequestedAtUnixMs.Value)
            : null;
        long? endTurnTimerDisposeMs = activeTurn.EndTurnTimerDisposedAtUnixMs.HasValue && activeTurn.FirstEndTurnRequestedAtUnixMs.HasValue
            ? Math.Max(0, activeTurn.EndTurnTimerDisposedAtUnixMs.Value - activeTurn.FirstEndTurnRequestedAtUnixMs.Value)
            : null;
        long? turnStartToTimerElapsedMs = activeTurn.TimerElapsedAtUnixMs.HasValue
            ? Math.Max(0, activeTurn.TimerElapsedAtUnixMs.Value - activeTurn.StartedAtUnixMs)
            : null;

        var probableCauseCode = ClassifyProbableCause(activeTurn, aiDurationMs, aiEndToEndTurnMs, endTurnToNextTurnMs, endTurnCompletedToNextTurnRequestedMs, nextTurnRequestedToStartedMs);

        return new CompletedTurn(
            LogFile: activeTurn.LogFile,
            SessionFight: activeTurn.SessionFight,
            FightId: activeTurn.FightId,
            Round: activeTurn.Round,
            FighterId: activeTurn.FighterId,
            FighterType: activeTurn.FighterType,
            MonsterId: activeTurn.MonsterId,
            DurationMs: durationMs,
            IsMonsterTurn: activeTurn.MonsterId.HasValue || activeTurn.FighterType.Contains("AI", StringComparison.OrdinalIgnoreCase) || activeTurn.FighterType.Contains("Monster", StringComparison.OrdinalIgnoreCase),
            EndTurnCalled: activeTurn.EndTurnCalled,
            EndTurnRequestedCount: activeTurn.EndTurnRequestedCount,
            FirstEndTurnSource: activeTurn.FirstEndTurnSource,
            LastEndTurnSource: activeTurn.LastEndTurnSource,
            EndTurnCallCount: activeTurn.EndTurnCallCount,
            EndTurnRequestedToBeginMs: endTurnRequestedToBeginMs,
            EndTurnBeginToCompletedMs: endTurnBeginToCompletedMs,
            EndTurnTimerDisposed: activeTurn.EndTurnTimerDisposed,
            EndTurnTimerDisposeMs: endTurnTimerDisposeMs,
            TimerDisposedSource: activeTurn.TimerDisposedSource,
            ReadyCheckerOutcome: activeTurn.ReadyCheckerOutcome,
            ReadyCheckerAckCount: activeTurn.ReadyCheckerAckCount,
            HadReadyCheckerTimeout: activeTurn.HadReadyCheckerTimeout,
            ReadyCheckerWaiters: activeTurn.ReadyCheckerWaiters,
            ReadyCheckerMissing: activeTurn.ReadyCheckerMissing,
            MaxActiveSequences: activeTurn.MaxActiveSequences,
            MaxPendingSequences: activeTurn.MaxPendingSequences,
            PendingSequencesObserved: activeTurn.PendingSequencesObserved,
            SequenceStartCount: activeTurn.SequenceStartCount,
            SequenceAcknowledgeCount: activeTurn.SequenceAcknowledgeCount,
            AiDurationMs: aiDurationMs,
            MaxAiElapsedMs: activeTurn.MaxAiElapsedMs,
            AiEndToEndTurnMs: aiEndToEndTurnMs,
            EndTurnToNextTurnMs: endTurnToNextTurnMs,
            EndTurnCompletedToNextTurnRequestedMs: endTurnCompletedToNextTurnRequestedMs,
            NextTurnRequestedToStartedMs: nextTurnRequestedToStartedMs,
            NextTurnRequestSource: activeTurn.NextTurnRequestSource,
            ReadyCheckerDurationMs: activeTurn.ReadyCheckerDurationMs,
            TurnStartToTimerElapsedMs: turnStartToTimerElapsedMs,
            CompletionEvent: completionEvent,
            ProbableCauseCode: probableCauseCode,
            ProbableCause: DescribeCause(probableCauseCode));
    }

    private static string ClassifyProbableCause(ActiveTurnState activeTurn, long? aiDurationMs, long? aiEndToEndTurnMs, long? endTurnToNextTurnMs, long? endTurnCompletedToNextTurnRequestedMs, long? nextTurnRequestedToStartedMs)
    {
        if ((activeTurn.EndTurnTimerElapsed || string.Equals(activeTurn.LastEndTurnSource, "Timer", StringComparison.OrdinalIgnoreCase)) &&
            (activeTurn.TurnTimerMs.HasValue && activeTurn.TurnTimerMs.Value >= 30_000 || (activeTurn.TimerElapsedAtUnixMs.HasValue && activeTurn.TimerElapsedAtUnixMs.Value - activeTurn.StartedAtUnixMs >= 30_000)))
        {
            return "TIMER_FALLBACK";
        }

        if (!activeTurn.EndTurnCalled)
            return "ENDTURN_NOT_CALLED";

        if (activeTurn.HadReadyCheckerTimeout)
            return "READYCHECKER_WAIT";

        if (activeTurn.PendingSequencesObserved || activeTurn.MaxActiveSequences > 1 || activeTurn.MaxPendingSequences > 0)
            return "PENDING_SEQUENCE";

        if ((aiDurationMs ?? activeTurn.MaxAiElapsedMs) >= 1_000)
            return "AI_SLOW";

        if (endTurnCompletedToNextTurnRequestedMs >= 1_000 && activeTurn.ReadyCheckerStarted)
            return "READYCHECKER_WAIT";

        if (nextTurnRequestedToStartedMs >= 1_000 && activeTurn.ReadyCheckerStarted)
            return "NETWORK_ACK_WAIT";

        if (aiEndToEndTurnMs >= 1_000)
            return "NETWORK_ACK_WAIT";

        if (endTurnToNextTurnMs >= 1_000)
            return "NETWORK_ACK_WAIT";

        return "UNKNOWN";
    }

    private static string DescribeCause(string causeCode) =>
        causeCode switch
        {
            "AI_SLOW" => "AI work itself is slow.",
            "ENDTURN_NOT_CALLED" => "The turn never called EndTurn before it closed.",
            "READYCHECKER_WAIT" => "The turn waited in ReadyChecker and hit timeout.",
            "PENDING_SEQUENCE" => "Pending or stacked sequences delayed the next turn.",
            "TIMER_FALLBACK" => "The fight only progressed when the end-turn timer elapsed.",
            "NETWORK_ACK_WAIT" => "The wait happened after EndTurn, while waiting for ACK/turn transition.",
            _ => "The available telemetry does not isolate a single cause."
        };

    private static SpellStat[] BuildSpellStats(IReadOnlyList<TelemetryEvent> events) =>
        events
            .Where(entry => entry.SpellId.HasValue)
            .GroupBy(entry => entry.SpellId!.Value)
            .Select(group => new SpellStat(
                SpellId: group.Key,
                EventCount: group.Count(),
                AverageMs: group.Average(entry => entry.ElapsedMs),
                MaxMs: group.Max(entry => entry.ElapsedMs),
                SlowCount: group.Count(entry => entry.Slow),
                ErrorCount: group.Count(entry => !string.Equals(entry.Status, "ok", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(entry.ExceptionType)),
                Phases: group.Select(entry => entry.Phase).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray()))
            .OrderByDescending(stat => stat.MaxMs)
            .ThenByDescending(stat => stat.AverageMs)
            .ThenByDescending(stat => stat.EventCount)
            .ToArray();

    private static FightStat[] BuildFightStats(IReadOnlyList<TelemetryEvent> events) =>
        events
            .Where(entry => entry.FightId.HasValue)
            .GroupBy(entry => new { entry.FileName, FightId = entry.FightId!.Value })
            .Select(group =>
            {
                var worst = group.OrderByDescending(entry => entry.ElapsedMs).First();
                return new FightStat(
                    SessionFight: $"{group.Key.FileName}#{group.Key.FightId}",
                    FightId: group.Key.FightId,
                    EventCount: group.Count(),
                    TotalObservedMs: group.Sum(entry => entry.ElapsedMs),
                    MaxEventMs: worst.ElapsedMs,
                    WorstPhase: worst.Phase,
                    SlowCount: group.Count(entry => entry.Slow),
                    ErrorCount: group.Count(entry => !string.Equals(entry.Status, "ok", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(entry.ExceptionType)),
                    MaxFanOut: group.Where(entry => entry.ObservedMessageFanOut.HasValue).Select(entry => entry.ObservedMessageFanOut!.Value).DefaultIfEmpty().Max());
            })
            .OrderByDescending(stat => stat.TotalObservedMs)
            .ThenByDescending(stat => stat.MaxEventMs)
            .ToArray();

    private static FanOutStat BuildFanOutStats(IReadOnlyList<TelemetryEvent> events)
    {
        var withFanOut = events.Where(entry => entry.ObservedMessageFanOut.HasValue).ToArray();
        var slow = withFanOut.Where(entry => entry.Slow).ToArray();
        var nonSlow = withFanOut.Where(entry => !entry.Slow).ToArray();

        return new FanOutStat(
            SlowCount: slow.Length,
            SlowAverageFanOut: slow.Length is 0 ? null : slow.Average(entry => entry.ObservedMessageFanOut!.Value),
            SlowP95FanOut: slow.Length is 0 ? null : Percentile(slow.Select(entry => entry.ObservedMessageFanOut!.Value).OrderBy(value => value).ToArray(), 0.95),
            SlowMaxFanOut: slow.Length is 0 ? null : slow.Max(entry => entry.ObservedMessageFanOut!.Value),
            SlowAverageElapsedMs: slow.Length is 0 ? null : slow.Average(entry => entry.ElapsedMs),
            NonSlowCount: nonSlow.Length,
            NonSlowAverageFanOut: nonSlow.Length is 0 ? null : nonSlow.Average(entry => entry.ObservedMessageFanOut!.Value),
            NonSlowP95FanOut: nonSlow.Length is 0 ? null : Percentile(nonSlow.Select(entry => entry.ObservedMessageFanOut!.Value).OrderBy(value => value).ToArray(), 0.95),
            NonSlowMaxFanOut: nonSlow.Length is 0 ? null : nonSlow.Max(entry => entry.ObservedMessageFanOut!.Value),
            NonSlowAverageElapsedMs: nonSlow.Length is 0 ? null : nonSlow.Average(entry => entry.ElapsedMs));
    }

    private static ErrorGroup[] BuildErrorGroups(IReadOnlyList<TelemetryEvent> events) =>
        events
            .Where(entry => !string.Equals(entry.Status, "ok", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(entry.ExceptionType))
            .GroupBy(entry => new { entry.Phase, entry.FightId, entry.MonsterId, entry.SpellId, ExceptionType = entry.ExceptionType ?? "status=error" })
            .Select(group => new ErrorGroup(
                Phase: group.Key.Phase,
                FightId: group.Key.FightId,
                MonsterId: group.Key.MonsterId,
                SpellId: group.Key.SpellId,
                ExceptionType: group.Key.ExceptionType,
                Count: group.Count(),
                Example: group.First().RawLine))
            .OrderByDescending(group => group.Count)
            .ThenBy(group => group.Phase, StringComparer.Ordinal)
            .ToArray();

    private static IEnumerable<string> BuildConclusions(
        IReadOnlyList<TelemetryEvent> events,
        IReadOnlyList<PhaseStat> phaseStats,
        IReadOnlyList<MonsterStat> monsterStats,
        IReadOnlyList<SpellStat> spellStats,
        IReadOnlyList<FightStat> fightStats,
        FanOutStat fanOutStats,
        IReadOnlyList<ErrorGroup> errorGroups,
        TurnAnalysis turnAnalysis)
    {
        var topEvent = events.OrderByDescending(entry => entry.ElapsedMs).ThenBy(entry => entry.FileName, StringComparer.OrdinalIgnoreCase).ThenBy(entry => entry.LineNumber).FirstOrDefault();
        var dominantThreshold = events
            .GroupBy(entry => entry.ThresholdMs)
            .OrderByDescending(group => group.Count())
            .ThenByDescending(group => group.Key)
            .Select(group => group.Key)
            .FirstOrDefault();

        if (topEvent is not null)
        {
            yield return $"No sampled event crossed the configured `slow` threshold of `{dominantThreshold} ms`; the highest single event was `{topEvent.Phase}` at `{topEvent.ElapsedMs} ms` in `{topEvent.FileName}:{topEvent.LineNumber}`.";
        }

        if (phaseStats.Count > 0)
        {
            var hottestByMax = phaseStats.OrderByDescending(stat => stat.MaxMs).ThenByDescending(stat => stat.P95).First();
            yield return $"The phase with the highest absolute spike is `{hottestByMax.Phase}` with `max={hottestByMax.MaxMs} ms`, while its `P95={hottestByMax.P95} ms` stays below the current slow threshold.";
        }

        if (monsterStats.Count > 0)
        {
            var worstMonster = monsterStats[0];
            yield return $"The heaviest monster AI observed is `monsterId={worstMonster.MonsterId}` with `avg AI={Format(worstMonster.AverageAiMs)} ms` and `max AI={worstMonster.MaxAiMs} ms`.";
        }

        if (spellStats.Count > 0)
        {
            var worstSpell = spellStats[0];
            yield return $"The heaviest spell path observed is `spellId={worstSpell.SpellId}` with `avg={Format(worstSpell.AverageMs)} ms` and `max={worstSpell.MaxMs} ms`.";
        }

        if (fightStats.Count > 0)
        {
            var worstFight = fightStats[0];
            yield return $"The hottest session fight is `{worstFight.SessionFight}` with `total={worstFight.TotalObservedMs} ms`, `slow events={worstFight.SlowCount}`, and worst phase `{worstFight.WorstPhase}`.";
        }

        if (fanOutStats.SlowCount > 0 && fanOutStats.NonSlowCount > 0)
        {
            var slowFanOut = fanOutStats.SlowAverageFanOut ?? 0;
            var nonSlowFanOut = fanOutStats.NonSlowAverageFanOut ?? 0;
            var ratio = nonSlowFanOut <= 0 ? 0 : slowFanOut / nonSlowFanOut;

            yield return ratio >= 1.5
                ? $"Slow events carry noticeably more observed fan-out (`avg {Format(slowFanOut)}` vs `avg {Format(nonSlowFanOut)}`), so message bursts are a meaningful secondary cost."
                : $"Slow events do not show a strong fan-out spike (`avg {Format(slowFanOut)}` vs `avg {Format(nonSlowFanOut)}`), which points more toward AI, pathfinding, spell handling, or cleanup than pure broadcast cost.";
        }
        else
        {
            var maxFanOutPhase = phaseStats.Where(stat => stat.MaxFanOut.HasValue).OrderByDescending(stat => stat.MaxFanOut).FirstOrDefault();
            if (maxFanOutPhase is not null)
                yield return $"Fan-out gets high in `{maxFanOutPhase.Phase}` (`max={maxFanOutPhase.MaxFanOut}`), but none of those events crossed the current slow threshold, so broadcast volume does not look like the first optimization target in this sample.";
        }

        yield return errorGroups.Count is 0
            ? "No `status=error` combat telemetry entries were found in this sample."
            : $"There are `{errorGroups.Sum(group => group.Count)}` error event(s) across `{errorGroups.Count}` unique failure signature(s), so the next phase should keep error clustering visible while profiling.";

        if (turnAnalysis.CompletedTurns.Length is 0)
        {
            yield return "This sample contains no `FIGHT-TURN` lifecycle data yet, so it still cannot explain the user-visible 30-second waits between AI completion and the next visible turn transition.";
        }
        else
        {
            var dominantCause = turnAnalysis.CompletedTurns
                .Where(turn => turn.IsMonsterTurn && turn.DurationMs > 5_000)
                .GroupBy(turn => turn.ProbableCauseCode, StringComparer.Ordinal)
                .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
                .OrderByDescending(entry => entry.Value)
                .ThenBy(entry => entry.Key, StringComparer.Ordinal)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(dominantCause.Key))
                dominantCause = turnAnalysis.CompletedTurns
                .Where(turn => turn.DurationMs > 5_000)
                .GroupBy(turn => turn.ProbableCauseCode, StringComparer.Ordinal)
                .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
                .OrderByDescending(entry => entry.Value)
                .ThenBy(entry => entry.Key, StringComparer.Ordinal)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(dominantCause.Key))
                dominantCause = turnAnalysis.CauseCounts
                    .OrderByDescending(entry => entry.Value)
                    .ThenBy(entry => entry.Key, StringComparer.Ordinal)
                    .FirstOrDefault();

            yield return $"Turn-latency telemetry captured `{turnAnalysis.CompletedTurns.Length}` completed turns, with `{turnAnalysis.TurnsOver30s}` turn(s) exceeding 30 seconds and `{turnAnalysis.ReadyCheckerTimeoutCount}` turn(s) ending in ReadyChecker timeout.";
            yield return $"The dominant slow monster-turn cause is `{dominantCause.Key}` (`{dominantCause.Value}` turn(s)); AI durations stay low while waits cluster after `EndTurn` and before the next visible turn.";
        }
    }

    private static IEnumerable<string> BuildRecommendations(
        IReadOnlyList<TelemetryEvent> events,
        IReadOnlyList<PhaseStat> phaseStats,
        IReadOnlyList<MonsterStat> monsterStats,
        IReadOnlyList<SpellStat> spellStats,
        IReadOnlyList<FightStat> fightStats,
        FanOutStat fanOutStats,
        TurnAnalysis turnAnalysis)
    {
        yield return "Keep `feature/combat-telemetry-phase1` non-invasive and use this parser as the baseline before any combat optimization.";

        if (events.All(entry => !entry.Slow))
            yield return "Lower `FightTelemetrySlowThresholdMs` for the next capture window or add per-turn aggregate timings, because user-visible latency can still exist even when each single event stays under `50 ms`.";

        if (phaseStats.Any(stat => string.Equals(stat.Phase, "Brain.Play", StringComparison.Ordinal) && stat.MaxMs >= 15))
            yield return "Move next to phase 2 profiling focused on `Brain.Play`, with per-monster AI traces and finer timing around target selection versus movement.";

        if (phaseStats.Any(stat => string.Equals(stat.Phase, "PathFinder.Resolve", StringComparison.Ordinal) && stat.MaxMs >= 10))
            yield return "Add deeper pathfinding telemetry next, separating route expansion cost from fight-action orchestration.";
        else
            yield return "Do not start with pathfinding optimization yet; `PathFinder.Resolve` stays inexpensive in this sample.";

        if (spellStats.Count > 0)
            yield return "Split spell profiling between `FightActor.CastSpell` and `SpellCast.ApplyHandlers`, then drill into the slowest `spellId` values before changing spell logic.";

        if (fightStats.Any(stat => stat.WorstPhase.Contains("GenerateResults", StringComparison.Ordinal) || stat.WorstPhase.Contains("EndFight", StringComparison.Ordinal)))
            yield return "Prioritize end-fight cleanup and reward profiling after AI/spell profiling, because at least one hot fight is dominated by result or cleanup work.";

        if ((fanOutStats.SlowAverageFanOut ?? 0) > (fanOutStats.NonSlowAverageFanOut ?? 0) * 1.5)
            yield return "Treat message fan-out as a secondary bottleneck candidate and add phase-level network counters before attempting broadcaster refactors.";
        else
            yield return "Do not start with broadcast refactors; the current data points more strongly to AI, pathfinding, spell handling, or cleanup.";

        if (turnAnalysis.CompletedTurns.Length is 0)
        {
            yield return "Capture a fresh combat sample with `FIGHT-TURN` enabled against Dark Vlad, Edad, Nomekop, Pandora, Minotoror, and a control mob, because the visible 30-second waits likely live in turn transition rather than the already-measured inner methods.";
        }
        else
        {
            if (turnAnalysis.TurnsEndedByTimerCount > 0 || turnAnalysis.ReadyCheckerTimeoutCount > 0)
                yield return "Prioritize the hand-off between `EndTurn`, `ReadyChecker`, and the end-turn timer before changing monster AI; the visible wait is happening after AI returns.";

            if (turnAnalysis.PendingSequenceTurnCount > 0)
                yield return "Review whether sequence disposal/acknowledgement can keep the fight in a half-closed turn state before the next fighter starts.";

            yield return "Use `combat-turn-latency-analysis-report.md` as the baseline for the next phase, then add targeted instrumentation inside `ReadyChecker` and turn-transition scheduling rather than inside spell math.";
        }
    }

    private static string BuildTurnLatencyReport(string inputDirectory, TurnAnalysis turnAnalysis)
    {
        var builder = new StringBuilder();
        var generatedAt = DateTimeOffset.Now;
        var topTurns = turnAnalysis.CompletedTurns
            .OrderByDescending(turn => turn.DurationMs)
            .ThenBy(turn => turn.LogFile, StringComparer.OrdinalIgnoreCase)
            .ThenBy(turn => turn.FightId)
            .ThenBy(turn => turn.Round)
            .ThenBy(turn => turn.FighterId)
            .ToArray();
        var monsterWaits = turnAnalysis.CompletedTurns
            .Where(turn => turn.MonsterId is > 0)
            .GroupBy(turn => turn.MonsterId!.Value)
            .Select(group =>
            {
                var dominantCause = group
                    .GroupBy(turn => turn.ProbableCauseCode, StringComparer.Ordinal)
                    .OrderByDescending(inner => inner.Count())
                    .ThenBy(inner => inner.Key, StringComparer.Ordinal)
                    .First();

                return new MonsterWaitSummary(
                    MonsterId: group.Key,
                    TurnCount: group.Count(),
                    AverageTurnMs: group.Average(turn => turn.DurationMs),
                    MaxTurnMs: group.Max(turn => turn.DurationMs),
                    DominantCauseCode: dominantCause.Key,
                    DominantCauseCount: dominantCause.Count());
            })
            .OrderByDescending(entry => entry.MaxTurnMs)
            .ThenByDescending(entry => entry.AverageTurnMs)
            .ThenByDescending(entry => entry.TurnCount)
            .ToArray();
        var readyCheckerWaits = turnAnalysis.CompletedTurns
            .Where(turn => turn.ReadyCheckerOutcome is not null)
            .OrderByDescending(turn => turn.ReadyCheckerDurationMs ?? 0)
            .ThenBy(turn => turn.LogFile, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();
        var timerTurns = turnAnalysis.CompletedTurns
            .Where(turn => turn.TurnStartToTimerElapsedMs.HasValue || string.Equals(turn.LastEndTurnSource, "Timer", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(turn => turn.TurnStartToTimerElapsedMs ?? 0)
            .ThenBy(turn => turn.LogFile, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var sequenceTurns = turnAnalysis.CompletedTurns
            .Where(turn => turn.PendingSequencesObserved || turn.MaxActiveSequences > 1 || turn.MaxPendingSequences > 0)
            .OrderByDescending(turn => turn.DurationMs)
            .ThenBy(turn => turn.LogFile, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        builder.AppendLine("# Combat Turn Latency Analysis Report");
        builder.AppendLine();
        builder.AppendLine($"Generated at: `{generatedAt:yyyy-MM-dd HH:mm:ss zzz}`");
        builder.AppendLine($"Input directory: `{inputDirectory}`");
        builder.AppendLine($"FIGHT-TURN events analyzed: `{turnAnalysis.TotalTurnEvents}`");
        builder.AppendLine($"Turn files analyzed: `{turnAnalysis.FilesAnalyzed}`");
        builder.AppendLine($"Turns reconstructed: `{turnAnalysis.CompletedTurns.Length}`");
        builder.AppendLine();

        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Monster turns avg/max: `{Format(turnAnalysis.MonsterAverageTurnMs)}` / `{turnAnalysis.MonsterMaxTurnMs} ms`");
        builder.AppendLine($"- Player turns avg/max: `{Format(turnAnalysis.PlayerAverageTurnMs)}` / `{turnAnalysis.PlayerMaxTurnMs} ms`");
        builder.AppendLine($"- Turns > 5000 ms: `{turnAnalysis.TurnsOver5s}`");
        builder.AppendLine($"- Turns > 30000 ms: `{turnAnalysis.TurnsOver30s}`");
        builder.AppendLine($"- ReadyChecker starts/ACKs/timeouts: `{turnAnalysis.ReadyCheckerStartCount}` / `{turnAnalysis.ReadyCheckerAckCount}` / `{turnAnalysis.ReadyCheckerTimeoutCount}`");
        builder.AppendLine($"- TimerElapsed count: `{turnAnalysis.TimerElapsedCount}`");
        builder.AppendLine($"- Turns ended by timer: `{turnAnalysis.TurnsEndedByTimerCount}`");
        builder.AppendLine($"- Turns with pending sequences: `{turnAnalysis.PendingSequenceTurnCount}`");
        builder.AppendLine();

        builder.AppendLine("## Cause Breakdown");
        builder.AppendLine();
        builder.AppendLine("| Cause | Turns |");
        builder.AppendLine("| --- | ---: |");
        foreach (var cause in turnAnalysis.CauseCounts.OrderByDescending(entry => entry.Value).ThenBy(entry => entry.Key, StringComparer.Ordinal))
            builder.AppendLine($"| `{cause.Key}` | {cause.Value} |");
        builder.AppendLine();

        var slowCauseCounts = turnAnalysis.CompletedTurns
            .Where(turn => turn.DurationMs > 5_000)
            .GroupBy(turn => turn.ProbableCauseCode, StringComparer.Ordinal)
            .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .ToArray();

        builder.AppendLine("### Slow Turns Only (> 5000 ms)");
        builder.AppendLine();
        if (slowCauseCounts.Length is 0)
        {
            builder.AppendLine("No turns above `5000 ms` were found.");
        }
        else
        {
            builder.AppendLine("| Cause | Slow Turns |");
            builder.AppendLine("| --- | ---: |");
            foreach (var cause in slowCauseCounts)
                builder.AppendLine($"| `{cause.Key}` | {cause.Value} |");
        }
        builder.AppendLine();

        builder.AppendLine("## Top Turn Gaps");
        builder.AppendLine();
        builder.AppendLine("| Rank | File | FightId | Round | FighterId | MonsterId | TotalTurnMs | AIDurationMs | AIEndToEndTurnMs | EndTurnToNextTurnMs | Cause |");
        builder.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |");
        for (var index = 0; index < topTurns.Take(15).Count(); index++)
        {
            var turn = topTurns[index];
            builder.AppendLine($"| {index + 1} | `{turn.LogFile}` | {turn.FightId} | {turn.Round} | {FormatNullable(turn.FighterId)} | {FormatNullable(turn.MonsterId)} | {turn.DurationMs} | {FormatNullable(turn.AiDurationMs)} | {FormatNullable(turn.AiEndToEndTurnMs)} | {FormatNullable(turn.EndTurnToNextTurnMs)} | `{turn.ProbableCauseCode}` |");
        }
        if (topTurns.Length is 0)
            builder.AppendLine("No completed turns were reconstructed in this sample.");
        builder.AppendLine();

        builder.AppendLine("## Monsters With The Most Wait");
        builder.AppendLine();
        if (monsterWaits.Length is 0)
        {
            builder.AppendLine("No monster turns were reconstructed.");
        }
        else
        {
            builder.AppendLine("| MonsterId | Turns | AvgTurnMs | MaxTurnMs | DominantCause | CauseCount |");
            builder.AppendLine("| --- | ---: | ---: | ---: | --- | ---: |");
            foreach (var entry in monsterWaits.Take(15))
                builder.AppendLine($"| {entry.MonsterId} | {entry.TurnCount} | {Format(entry.AverageTurnMs)} | {entry.MaxTurnMs} | `{entry.DominantCauseCode}` | {entry.DominantCauseCount} |");
        }
        builder.AppendLine();

        builder.AppendLine("## ReadyChecker");
        builder.AppendLine();
        builder.AppendLine($"- starts: `{turnAnalysis.ReadyCheckerStartCount}`");
        builder.AppendLine($"- ACK completions: `{turnAnalysis.ReadyCheckerAckCount}`");
        builder.AppendLine($"- timeouts: `{turnAnalysis.ReadyCheckerTimeoutCount}`");
        builder.AppendLine();
        if (readyCheckerWaits.Length > 0)
        {
            builder.AppendLine("| File | FightId | Round | FighterId | MonsterId | Outcome | WaitMs | Missing |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | --- | ---: | --- |");
            foreach (var turn in readyCheckerWaits)
                builder.AppendLine($"| `{turn.LogFile}` | {turn.FightId} | {turn.Round} | {FormatNullable(turn.FighterId)} | {FormatNullable(turn.MonsterId)} | `{FormatReadyCheckerOutcome(turn)}` | {FormatNullable(turn.ReadyCheckerDurationMs)} | {EscapeTable(turn.ReadyCheckerMissing, 64)} |");
        }
        else
        {
            builder.AppendLine("No ReadyChecker events were reconstructed.");
        }
        builder.AppendLine();

        builder.AppendLine("## Timers");
        builder.AppendLine();
        builder.AppendLine($"- `TimerElapsed` events: `{turnAnalysis.TimerElapsedCount}`");
        builder.AppendLine($"- turns that only progressed after timer fallback: `{turnAnalysis.TurnsEndedByTimerCount}`");
        builder.AppendLine();
        if (timerTurns.Length > 0)
        {
            builder.AppendLine("| File | FightId | Round | FighterId | MonsterId | TurnStartToTimerElapsedMs | EndTurnSource | Cause |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | --- | --- |");
            foreach (var turn in timerTurns.Take(15))
                builder.AppendLine($"| `{turn.LogFile}` | {turn.FightId} | {turn.Round} | {FormatNullable(turn.FighterId)} | {FormatNullable(turn.MonsterId)} | {FormatNullable(turn.TurnStartToTimerElapsedMs)} | `{turn.LastEndTurnSource ?? "-"}` | `{turn.ProbableCauseCode}` |");
        }
        else
        {
            builder.AppendLine("No end-turn timer fallbacks were found.");
        }
        builder.AppendLine();

        builder.AppendLine("## Sequences");
        builder.AppendLine();
        builder.AppendLine($"- turns with pending or stacked sequences: `{turnAnalysis.PendingSequenceTurnCount}`");
        builder.AppendLine();
        if (sequenceTurns.Length > 0)
        {
            builder.AppendLine("| File | FightId | Round | FighterId | MonsterId | MaxActiveSequences | MaxPendingSequences | TotalTurnMs | Cause |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |");
            foreach (var turn in sequenceTurns.Take(15))
                builder.AppendLine($"| `{turn.LogFile}` | {turn.FightId} | {turn.Round} | {FormatNullable(turn.FighterId)} | {FormatNullable(turn.MonsterId)} | {turn.MaxActiveSequences} | {turn.MaxPendingSequences} | {turn.DurationMs} | `{turn.ProbableCauseCode}` |");
        }
        else
        {
            builder.AppendLine("No turns showed pending sequences or stacked sequence pressure.");
        }
        builder.AppendLine();

        builder.AppendLine("## Conclusion");
        builder.AppendLine();
        foreach (var line in BuildTurnConclusions(turnAnalysis, monsterWaits))
            builder.AppendLine($"- {line}");

        return builder.ToString();
    }

    private static string BuildTurnTransitionPhase2Report(string inputDirectory, TurnAnalysis turnAnalysis)
    {
        var builder = new StringBuilder();
        var generatedAt = DateTimeOffset.Now;
        var phase2MarkerTurns = turnAnalysis.CompletedTurns.Count(turn =>
            turn.EndTurnRequestedCount > 0
            || turn.EndTurnRequestedToBeginMs.HasValue
            || turn.EndTurnBeginToCompletedMs.HasValue
            || turn.EndTurnTimerDisposed
            || turn.NextTurnRequestSource is not null
            || turn.NextTurnRequestedToStartedMs.HasValue
            || turn.EndTurnCompletedToNextTurnRequestedMs.HasValue);
        var topTransitionGaps = turnAnalysis.CompletedTurns
            .OrderByDescending(turn => turn.EndTurnToNextTurnMs ?? turn.DurationMs)
            .ThenByDescending(turn => turn.NextTurnRequestedToStartedMs ?? 0)
            .ThenBy(turn => turn.LogFile, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();
        var turnsWithoutEndTurn = turnAnalysis.CompletedTurns
            .Where(turn => !turn.EndTurnCalled)
            .OrderByDescending(turn => turn.DurationMs)
            .ThenBy(turn => turn.LogFile, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var timerTurns = turnAnalysis.CompletedTurns
            .Where(turn => turn.TurnStartToTimerElapsedMs.HasValue || string.Equals(turn.LastEndTurnSource, "Timer", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(turn => turn.TurnStartToTimerElapsedMs ?? 0)
            .ThenBy(turn => turn.LogFile, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var readyCheckerTimeouts = turnAnalysis.CompletedTurns
            .Where(turn => turn.HadReadyCheckerTimeout)
            .OrderByDescending(turn => turn.ReadyCheckerDurationMs ?? 0)
            .ThenBy(turn => turn.LogFile, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var pendingSequenceTurns = turnAnalysis.CompletedTurns
            .Where(turn => turn.PendingSequencesObserved || turn.MaxActiveSequences > 1 || turn.MaxPendingSequences > 0)
            .OrderByDescending(turn => turn.DurationMs)
            .ThenBy(turn => turn.LogFile, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var turnsWithEndTurnButNoNextTurn = turnAnalysis.CompletedTurns
            .Where(turn => turn.EndTurnCalled && !string.Equals(turn.CompletionEvent, "NextTurnStarted", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(turn => turn.DurationMs)
            .ThenBy(turn => turn.LogFile, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var turnsWithNextTurnRequestedButNoStart = turnAnalysis.CompletedTurns
            .Where(turn => !string.IsNullOrWhiteSpace(turn.NextTurnRequestSource) && !string.Equals(turn.CompletionEvent, "NextTurnStarted", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(turn => turn.DurationMs)
            .ThenBy(turn => turn.LogFile, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var timerFallbackTurnsWithImmediateNextTurn = timerTurns
            .Where(turn => (turn.NextTurnRequestedToStartedMs ?? long.MaxValue) <= 5)
            .ToArray();

        builder.AppendLine("# Combat Turn Transition Phase 2 Report");
        builder.AppendLine();
        builder.AppendLine($"Generated at: `{generatedAt:yyyy-MM-dd HH:mm:ss zzz}`");
        builder.AppendLine($"Input directory: `{inputDirectory}`");
        builder.AppendLine($"Turn files analyzed: `{turnAnalysis.FilesAnalyzed}`");
        builder.AppendLine($"FIGHT-TURN events analyzed: `{turnAnalysis.TotalTurnEvents}`");
        builder.AppendLine($"Turns reconstructed: `{turnAnalysis.CompletedTurns.Length}`");
        builder.AppendLine();

        builder.AppendLine("## What This Phase Adds");
        builder.AppendLine();
        builder.AppendLine("This phase extends the earlier turn-latency analysis with transition-specific checkpoints so we can separate `EndTurn` itself from `ReadyChecker`, timer disposal, pending sequences, and the actual hand-off to the next turn. The goal is to explain the visible 30-second waits without changing combat behavior yet.");
        builder.AppendLine();

        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Turns with `EndTurn -> NextTurn` gap > 1000 ms: `{turnAnalysis.CompletedTurns.Count(turn => (turn.EndTurnToNextTurnMs ?? 0) > 1_000)}`");
        builder.AppendLine($"- Turns without `EndTurn`: `{turnsWithoutEndTurn.Length}`");
        builder.AppendLine($"- Turns ended by timer fallback: `{turnAnalysis.TurnsEndedByTimerCount}`");
        builder.AppendLine($"- ReadyChecker timeouts: `{turnAnalysis.ReadyCheckerTimeoutCount}`");
        builder.AppendLine($"- Pending sequence turns: `{pendingSequenceTurns.Length}`");
        builder.AppendLine($"- Turns with `EndTurn` but no `NextTurnStarted`: `{turnsWithEndTurnButNoNextTurn.Length}`");
        builder.AppendLine($"- Turns with `NextTurnRequested` but no `NextTurnStarted`: `{turnsWithNextTurnRequestedButNoStart.Length}`");
        builder.AppendLine($"- Turns with explicit end-turn timer disposal observed: `{turnAnalysis.CompletedTurns.Count(turn => turn.EndTurnTimerDisposed)}`");
        builder.AppendLine($"- Turns with phase-2 transition markers populated: `{phase2MarkerTurns}`");
        builder.AppendLine($"- Timer fallback turns where `NextTurnRequested -> NextTurnStarted <= 5 ms`: `{timerFallbackTurnsWithImmediateNextTurn.Length}`");
        builder.AppendLine();

        builder.AppendLine("## Top 20 Turn Transition Gaps");
        builder.AppendLine();
        builder.AppendLine("| Rank | File | FightId | Round | FighterId | MonsterId | TotalTurnMs | EndTurnRequestedToBeginMs | EndTurnBeginToCompletedMs | EndTurnToNextTurnMs | EndTurnCompletedToNextTurnRequestedMs | NextTurnRequestedToStartedMs | ReadyChecker | TimerDisposed | Cause |");
        builder.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | --- |");
        for (var index = 0; index < topTransitionGaps.Length; index++)
        {
            var turn = topTransitionGaps[index];
            builder.AppendLine($"| {index + 1} | `{turn.LogFile}` | {turn.FightId} | {turn.Round} | {FormatNullable(turn.FighterId)} | {FormatNullable(turn.MonsterId)} | {turn.DurationMs} | {FormatNullable(turn.EndTurnRequestedToBeginMs)} | {FormatNullable(turn.EndTurnBeginToCompletedMs)} | {FormatNullable(turn.EndTurnToNextTurnMs)} | {FormatNullable(turn.EndTurnCompletedToNextTurnRequestedMs)} | {FormatNullable(turn.NextTurnRequestedToStartedMs)} | `{FormatReadyCheckerOutcome(turn)}` | `{(turn.EndTurnTimerDisposed ? $"{turn.TimerDisposedSource ?? "yes"}:{FormatNullable(turn.EndTurnTimerDisposeMs)}" : "no")}` | `{turn.ProbableCauseCode}` |");
        }
        if (topTransitionGaps.Length is 0)
            builder.AppendLine("No turns were reconstructed.");
        builder.AppendLine();

        builder.AppendLine("## Turns Without EndTurn");
        builder.AppendLine();
        if (turnsWithoutEndTurn.Length is 0)
        {
            builder.AppendLine("No reconstructed turn closed without an `EndTurn` call.");
        }
        else
        {
            builder.AppendLine("| File | FightId | Round | FighterId | MonsterId | TotalTurnMs | Completion | Cause |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | --- | --- |");
            foreach (var turn in turnsWithoutEndTurn.Take(20))
                builder.AppendLine($"| `{turn.LogFile}` | {turn.FightId} | {turn.Round} | {FormatNullable(turn.FighterId)} | {FormatNullable(turn.MonsterId)} | {turn.DurationMs} | `{turn.CompletionEvent}` | `{turn.ProbableCauseCode}` |");
        }
        builder.AppendLine();

        builder.AppendLine("## Turns Closed By Timer");
        builder.AppendLine();
        if (timerTurns.Length is 0)
        {
            builder.AppendLine("No reconstructed turn depended on timer fallback in this sample.");
        }
        else
        {
            builder.AppendLine("| File | FightId | Round | FighterId | MonsterId | TurnStartToTimerElapsedMs | EndTurnSource | NextTurnRequestedToStartedMs | Cause |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | --- | ---: | --- |");
            foreach (var turn in timerTurns.Take(20))
                builder.AppendLine($"| `{turn.LogFile}` | {turn.FightId} | {turn.Round} | {FormatNullable(turn.FighterId)} | {FormatNullable(turn.MonsterId)} | {FormatNullable(turn.TurnStartToTimerElapsedMs)} | `{turn.LastEndTurnSource ?? "-"}` | {FormatNullable(turn.NextTurnRequestedToStartedMs)} | `{turn.ProbableCauseCode}` |");
        }
        builder.AppendLine();

        builder.AppendLine("## ReadyChecker Timeouts");
        builder.AppendLine();
        if (readyCheckerTimeouts.Length is 0)
        {
            builder.AppendLine("No ReadyChecker timeout was reconstructed in this sample.");
        }
        else
        {
            builder.AppendLine("| File | FightId | Round | FighterId | MonsterId | Waiters | Missing | ReadyCheckerMs | EndTurnToNextTurnMs | Cause |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | --- | ---: | ---: | --- |");
            foreach (var turn in readyCheckerTimeouts.Take(20))
                builder.AppendLine($"| `{turn.LogFile}` | {turn.FightId} | {turn.Round} | {FormatNullable(turn.FighterId)} | {FormatNullable(turn.MonsterId)} | {FormatNullable(turn.ReadyCheckerWaiters)} | {EscapeTable(turn.ReadyCheckerMissing, 72)} | {FormatNullable(turn.ReadyCheckerDurationMs)} | {FormatNullable(turn.EndTurnToNextTurnMs)} | `{turn.ProbableCauseCode}` |");
        }
        builder.AppendLine();

        builder.AppendLine("## Pending Sequence Cases");
        builder.AppendLine();
        if (pendingSequenceTurns.Length is 0)
        {
            builder.AppendLine("No pending-sequence pressure was reconstructed in this sample.");
        }
        else
        {
            builder.AppendLine("| File | FightId | Round | FighterId | MonsterId | MaxActiveSequences | MaxPendingSequences | NextTurnRequestedToStartedMs | Cause |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |");
            foreach (var turn in pendingSequenceTurns.Take(20))
                builder.AppendLine($"| `{turn.LogFile}` | {turn.FightId} | {turn.Round} | {FormatNullable(turn.FighterId)} | {FormatNullable(turn.MonsterId)} | {turn.MaxActiveSequences} | {turn.MaxPendingSequences} | {FormatNullable(turn.NextTurnRequestedToStartedMs)} | `{turn.ProbableCauseCode}` |");
        }
        builder.AppendLine();

        builder.AppendLine("## Conclusion");
        builder.AppendLine();
        var longestTurn = topTransitionGaps.FirstOrDefault();
        if (longestTurn is null)
        {
            builder.AppendLine("- No reconstructed transition data is available yet. Generate a fresh capture with the new phase 2 telemetry enabled.");
        }
        else
        {
            if (phase2MarkerTurns is 0)
                builder.AppendLine("- This capture set predates the new phase-2 transition markers. The existing diagnosis remains valid, but `EndTurnRequested -> NextTurnRequested` sub-gaps will stay blank until a fresh capture is recorded with this branch.");
            builder.AppendLine($"- Longest observed transition gap in this sample: fight `{longestTurn.FightId}` round `{longestTurn.Round}` on `{longestTurn.LogFile}`, with `EndTurnToNextTurnMs={longestTurn.EndTurnToNextTurnMs}` and cause `{longestTurn.ProbableCauseCode}`.");
            builder.AppendLine($"- `EndTurn` missing count: `{turnsWithoutEndTurn.Length}`.");
            builder.AppendLine($"- ReadyChecker timeouts reconstructed: `{readyCheckerTimeouts.Length}`.");
            builder.AppendLine($"- Timer fallback turns reconstructed: `{timerTurns.Length}`.");
            if (timerFallbackTurnsWithImmediateNextTurn.Length > 0)
                builder.AppendLine($"- In `{timerFallbackTurnsWithImmediateNextTurn.Length}` timer-fallback turn(s), `NextTurnRequested -> NextTurnStarted` completed in `<= 5 ms`. That means the visible 30-second wait is happening before `NextTurnRequested`, not after it.");
            if (readyCheckerTimeouts.Length > 0)
                builder.AppendLine("- `ReadyChecker` does participate, but the timeout itself is not the 30-second stall. In the new phase-2 captures, the turn hits `ReadyCheckerTimeout` first and still does not advance until the end-turn timer later fires.");
            if (turnsWithEndTurnButNoNextTurn.Length > 0)
                builder.AppendLine("- There are reconstructed turns where `EndTurn` is called but no next turn starts before the turn closes. In the slow monster cases, that points to the hand-off between `EndTurnCompleted`, `ReadyChecker`, and the turn timer rather than to AI or spell execution.");
            builder.AppendLine("- Next fix should target the transition path after `EndTurnCompleted`: verify why `ReadyCheckerTimeout`/success does not immediately request the next turn, and why the end-turn timer still reaches `TimerElapsed` in those monster turns.");
        }

        return builder.ToString();
    }

    private static IEnumerable<string> BuildTurnConclusions(TurnAnalysis turnAnalysis, IReadOnlyList<MonsterWaitSummary> monsterWaits)
    {
        if (turnAnalysis.CompletedTurns.Length is 0)
        {
            yield return "No `FIGHT-TURN` events were reconstructed, so this capture still cannot explain visible turn latency.";
            yield break;
        }

        var topMonster = monsterWaits.FirstOrDefault();
        var cause = turnAnalysis.CompletedTurns
            .Where(turn => turn.IsMonsterTurn && turn.DurationMs > 5_000)
            .GroupBy(turn => turn.ProbableCauseCode, StringComparer.Ordinal)
            .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(cause.Key))
            cause = turnAnalysis.CompletedTurns
            .Where(turn => turn.DurationMs > 5_000)
            .GroupBy(turn => turn.ProbableCauseCode, StringComparer.Ordinal)
            .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(cause.Key))
            cause = turnAnalysis.CauseCounts.OrderByDescending(entry => entry.Value).ThenBy(entry => entry.Key, StringComparer.Ordinal).FirstOrDefault();

        yield return turnAnalysis.CompletedTurns.Any(turn => (turn.AiDurationMs ?? 0) >= 1_000)
            ? "Some monster turns do spend meaningful time inside AI itself."
            : "The AI itself stays fast in this sample; the visible wait is not explained by `Brain.Play` duration.";

        yield return turnAnalysis.EndTurnMissingCount > 0
            ? $"`EndTurn` is missing in `{turnAnalysis.EndTurnMissingCount}` reconstructed turn(s)."
            : "`EndTurn` is being called; the visible wait happens after the call rather than before it.";

        yield return turnAnalysis.ReadyCheckerTimeoutCount > 0
            ? $"`ReadyChecker` is a real factor: `{turnAnalysis.ReadyCheckerTimeoutCount}` turn(s) ended in timeout."
            : "`ReadyChecker` did not timeout in this sample.";

        yield return turnAnalysis.TurnsEndedByTimerCount > 0
            ? $"The 30-second class of stalls lines up with timer fallback in `{turnAnalysis.TurnsEndedByTimerCount}` turn(s)."
            : "No turn had to wait for the end-turn timer fallback.";

        if (topMonster is not null)
            yield return $"The most reproducible monster-side wait in this sample is `monsterId={topMonster.MonsterId}` with `max turn={topMonster.MaxTurnMs} ms` and dominant cause `{topMonster.DominantCauseCode}`.";

        yield return $"Dominant slow monster-turn cause: `{cause.Key}` ({cause.Value} turn(s)). The next engineering phase should target turn transition, `ReadyChecker`, and timer interplay before touching AI or spell math.";
    }

    private static string FormatReadyCheckerOutcome(CompletedTurn turn)
    {
        if (turn.HadReadyCheckerTimeout && string.Equals(turn.ReadyCheckerOutcome, "ACK", StringComparison.OrdinalIgnoreCase))
            return "TIMEOUT_THEN_ACK";

        if (turn.HadReadyCheckerTimeout && string.IsNullOrWhiteSpace(turn.ReadyCheckerOutcome))
            return "TIMEOUT";

        return turn.ReadyCheckerOutcome ?? "-";
    }

    private static string GetFanOutConclusion(IReadOnlyList<PhaseStat> phaseStats, FanOutStat fanOutStats)
    {
        var highFanOutPhase = phaseStats
            .Where(stat => stat.MaxFanOut.HasValue)
            .OrderByDescending(stat => stat.MaxFanOut)
            .ThenByDescending(stat => stat.P95)
            .FirstOrDefault();

        if (highFanOutPhase is null)
            return "No events with `observedMessageFanOut` were captured in this sample.";

        var slowAvg = fanOutStats.SlowAverageFanOut ?? 0;
        var nonSlowAvg = fanOutStats.NonSlowAverageFanOut ?? 0;

        return slowAvg > nonSlowAvg * 1.5
            ? $"Highest observed fan-out clusters around `{highFanOutPhase.Phase}` (max `{highFanOutPhase.MaxFanOut}`), and slow events carry materially higher fan-out than non-slow events. Fan-out looks like a real contributing factor, even if it is not the only one."
            : $"Highest observed fan-out clusters around `{highFanOutPhase.Phase}` (max `{highFanOutPhase.MaxFanOut}`), but slow events do not show a proportionally higher fan-out footprint. The main hotspot is more likely AI, pathfinding, spell handling, or cleanup than raw message broadcast volume.";
    }

    private static long Percentile(IReadOnlyList<long> sortedValues, double percentile)
    {
        if (sortedValues.Count is 0)
            return 0;

        var rank = (int)Math.Ceiling(percentile * sortedValues.Count);
        rank = Math.Clamp(rank, 1, sortedValues.Count);
        return sortedValues[rank - 1];
    }

    private static string Format(double value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Format(double? value) =>
        value.HasValue ? Format(value.Value) : "-";

    private static string FormatNullable<T>(T? value) where T : struct =>
        value.HasValue ? Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? "-" : "-";

    private static string EscapeTable(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "-";

        var normalized = value.Replace("\r", " ").Replace("\n", " ").Replace("|", "\\|").Trim();
        return normalized.Length <= maxLength ? normalized : $"{normalized[..Math.Max(0, maxLength - 3)]}...";
    }

    private static string NormalizePath(string path, string baseDirectory) =>
        Path.GetRelativePath(baseDirectory, path).Replace('\\', '/');

    private static string BuildSpellEffectLayerReport(IReadOnlyList<SpellCastTelemetryEvent> spellEvents) =>
        SpellEffectLayerReportBuilder.Build(spellEvents);
}

internal sealed record TelemetryEvent(
    string FileName,
    string FilePath,
    int LineNumber,
    string Phase,
    short? FightId,
    int? FighterId,
    short? MonsterId,
    int? SpellId,
    int? EffectId,
    string? HandlerType,
    long ElapsedMs,
    bool Slow,
    int ThresholdMs,
    long? ObservedMessageFanOut,
    string Status,
    string? Detail,
    bool? Success,
    string? Result,
    string? ExceptionType,
    string? ExceptionMessage,
    string RawLine);

internal sealed record TurnEvent(
    string FileName,
    string FilePath,
    int LineNumber,
    short FightId,
    short Round,
    string EventName,
    long AtUnixMs,
    int? FighterId,
    string? FighterType,
    short? MonsterId,
    long? ElapsedMs,
    long? ElapsedSinceTurnStartMs,
    string? Source,
    int? Waiters,
    string? Missing,
    int? ActiveSequences,
    int? PendingSequences,
    string? TimerType,
    int? TimeoutMs,
    int? TurnTimeMs,
    string? Status,
    string? Detail,
    string RawLine);

internal sealed record PhaseStat(
    string Phase,
    int Count,
    double AverageMs,
    long MaxMs,
    long P50,
    long P95,
    long P99,
    int SlowCount,
    int ErrorCount,
    double? AverageFanOut,
    long? MaxFanOut);

internal sealed record MonsterStat(
    short MonsterId,
    int AiEventCount,
    double AverageAiMs,
    long MaxAiMs,
    int SlowAiCount,
    string[] SlowPhases);

internal sealed record SpellStat(
    int SpellId,
    int EventCount,
    double AverageMs,
    long MaxMs,
    int SlowCount,
    int ErrorCount,
    string[] Phases);

internal sealed record HandlerStat(
    string HandlerType,
    int EventCount,
    double AverageMs,
    long MaxMs,
    int SlowCount,
    int ErrorCount,
    int[] EffectIds,
    int[] SpellIds);

internal sealed record CompletedTurn(
    string LogFile,
    string SessionFight,
    short FightId,
    short Round,
    int? FighterId,
    string FighterType,
    short? MonsterId,
    long DurationMs,
    bool IsMonsterTurn,
    bool EndTurnCalled,
    int EndTurnRequestedCount,
    string? FirstEndTurnSource,
    string? LastEndTurnSource,
    int EndTurnCallCount,
    long? EndTurnRequestedToBeginMs,
    long? EndTurnBeginToCompletedMs,
    bool EndTurnTimerDisposed,
    long? EndTurnTimerDisposeMs,
    string? TimerDisposedSource,
    string? ReadyCheckerOutcome,
    int ReadyCheckerAckCount,
    bool HadReadyCheckerTimeout,
    int? ReadyCheckerWaiters,
    string? ReadyCheckerMissing,
    int MaxActiveSequences,
    int MaxPendingSequences,
    bool PendingSequencesObserved,
    int SequenceStartCount,
    int SequenceAcknowledgeCount,
    long? AiDurationMs,
    long MaxAiElapsedMs,
    long? AiEndToEndTurnMs,
    long? EndTurnToNextTurnMs,
    long? EndTurnCompletedToNextTurnRequestedMs,
    long? NextTurnRequestedToStartedMs,
    string? NextTurnRequestSource,
    long? ReadyCheckerDurationMs,
    long? TurnStartToTimerElapsedMs,
    string CompletionEvent,
    string ProbableCauseCode,
    string ProbableCause);

internal sealed record TurnAnalysis(
    CompletedTurn[] CompletedTurns,
    int TotalTurnEvents,
    int FilesAnalyzed,
    double MonsterAverageTurnMs,
    long MonsterMaxTurnMs,
    double PlayerAverageTurnMs,
    long PlayerMaxTurnMs,
    int TurnsOver5s,
    int TurnsOver30s,
    int ReadyCheckerStartCount,
    int ReadyCheckerAckCount,
    int ReadyCheckerTimeoutCount,
    int TimerElapsedCount,
    int TurnsEndedByTimerCount,
    int PendingSequenceTurnCount,
    IReadOnlyDictionary<string, int> CauseCounts,
    int EndTurnMissingCount)
{
    public static TurnAnalysis Empty { get; } = new(
        CompletedTurns: Array.Empty<CompletedTurn>(),
        TotalTurnEvents: 0,
        FilesAnalyzed: 0,
        MonsterAverageTurnMs: 0,
        MonsterMaxTurnMs: 0,
        PlayerAverageTurnMs: 0,
        PlayerMaxTurnMs: 0,
        TurnsOver5s: 0,
        TurnsOver30s: 0,
        ReadyCheckerStartCount: 0,
        ReadyCheckerAckCount: 0,
        ReadyCheckerTimeoutCount: 0,
        TimerElapsedCount: 0,
        TurnsEndedByTimerCount: 0,
        PendingSequenceTurnCount: 0,
        CauseCounts: new Dictionary<string, int>(StringComparer.Ordinal),
        EndTurnMissingCount: 0);
}

internal sealed record MonsterWaitSummary(
    short MonsterId,
    int TurnCount,
    double AverageTurnMs,
    long MaxTurnMs,
    string DominantCauseCode,
    int DominantCauseCount);

internal sealed record FightStat(
    string SessionFight,
    short FightId,
    int EventCount,
    long TotalObservedMs,
    long MaxEventMs,
    string WorstPhase,
    int SlowCount,
    int ErrorCount,
    long MaxFanOut);

internal sealed record FanOutStat(
    int SlowCount,
    double? SlowAverageFanOut,
    long? SlowP95FanOut,
    long? SlowMaxFanOut,
    double? SlowAverageElapsedMs,
    int NonSlowCount,
    double? NonSlowAverageFanOut,
    long? NonSlowP95FanOut,
    long? NonSlowMaxFanOut,
    double? NonSlowAverageElapsedMs);

internal sealed record ErrorGroup(
    string Phase,
    short? FightId,
    short? MonsterId,
    int? SpellId,
    string ExceptionType,
    int Count,
    string Example);

internal sealed class ActiveTurnState
{
    public required string LogFile { get; init; }

    public required string SessionFight { get; init; }

    public required short FightId { get; init; }

    public required short Round { get; init; }

    public required int? FighterId { get; init; }

    public required string FighterType { get; init; }

    public required short? MonsterId { get; init; }

    public required long StartedAtUnixMs { get; init; }

    public required int MaxActiveSequences { get; set; }

    public required int MaxPendingSequences { get; set; }

    public long? AiStartAtUnixMs { get; set; }

    public long? AiEndAtUnixMs { get; set; }

    public bool EndTurnCalled { get; set; }

    public int EndTurnRequestedCount { get; set; }

    public long? FirstEndTurnRequestedAtUnixMs { get; set; }

    public long? EndTurnBegunAtUnixMs { get; set; }

    public int EndTurnCallCount { get; set; }

    public long? FirstEndTurnAtUnixMs { get; set; }

    public long? LastEndTurnAtUnixMs { get; set; }

    public string? FirstEndTurnSource { get; set; }

    public string? LastEndTurnSource { get; set; }

    public long? EndTurnCompletedAtUnixMs { get; set; }

    public bool EndTurnTimerDisposed { get; set; }

    public long? EndTurnTimerDisposedAtUnixMs { get; set; }

    public string? TimerDisposedSource { get; set; }

    public bool ReadyCheckerStarted { get; set; }

    public int ReadyCheckerStartCount { get; set; }

    public int ReadyCheckerAckCount { get; set; }

    public long? ReadyCheckerStartedAtUnixMs { get; set; }

    public string? ReadyCheckerOutcome { get; set; }

    public bool HadReadyCheckerTimeout { get; set; }

    public long? ReadyCheckerTimeoutAtUnixMs { get; set; }

    public long? ReadyCheckerCompletedAtUnixMs { get; set; }

    public long? ReadyCheckerDurationMs { get; set; }

    public int? ReadyCheckerWaiters { get; set; }

    public string? ReadyCheckerMissing { get; set; }

    public bool EndTurnTimerElapsed { get; set; }

    public long? TimerElapsedAtUnixMs { get; set; }

    public int? TurnTimerMs { get; set; }

    public long? NextTurnRequestedAtUnixMs { get; set; }

    public string? NextTurnRequestSource { get; set; }

    public bool PendingSequencesObserved { get; set; }

    public int SequenceStartCount { get; set; }

    public int SequenceAcknowledgeCount { get; set; }

    public long MaxAiElapsedMs { get; set; }
}

internal sealed record SpellCastTelemetryEvent(
    string FileName,
    string FilePath,
    int LineNumber,
    string EventName,
    string? TimestampUtc,
    short? FightId,
    string? TurnId,
    int? CasterId,
    string? CasterName,
    int? SpellId,
    short? SpellLevel,
    string? TargetIds,
    string? EffectIds,
    string? Result,
    string? Error,
    long? DurationMs,
    string? Layer,
    string? ReasonCode,
    string? CorrelationId,
    string RawLine);

internal static class SpellEffectLayerReportBuilder
{
    public static string Build(IReadOnlyList<SpellCastTelemetryEvent> spellEvents)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Spell effect layer report");
        builder.AppendLine();

        var layered = spellEvents.Where(x => !string.IsNullOrWhiteSpace(x.Layer)).ToList();
        if (layered.Count == 0)
        {
            builder.AppendLine("No spell-effect telemetry events with `layer` found.");
            builder.AppendLine("Enable `SpellEffectTelemetryEnabled=true` and rerun combat tests.");
            return builder.ToString();
        }

        builder.AppendLine($"Total layered events: **{layered.Count}**");
        builder.AppendLine();

        builder.AppendLine("## Events by layer");
        foreach (var group in layered.GroupBy(x => x.Layer!).OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            builder.AppendLine($"### Layer `{group.Key}` ({group.Count()} events)");
            foreach (var eventGroup in group.GroupBy(x => x.EventName).OrderByDescending(x => x.Count()))
                builder.AppendLine($"- `{eventGroup.Key}`: {eventGroup.Count()}");
            builder.AppendLine();
        }

        builder.AppendLine("## Validation rejections (layer V)");
        foreach (var rejection in layered
                     .Where(x => x.Layer == "V" && x.EventName is "SpellValidationResult" or "AiSpellRejected")
                     .GroupBy(x => x.ReasonCode ?? "unknown")
                     .OrderByDescending(x => x.Count())
                     .Take(20))
        {
            builder.AppendLine($"- `{rejection.Key}`: {rejection.Count()}");
        }
        builder.AppendLine();

        builder.AppendLine("## AI spell rejections (layer A/V)");
        foreach (var rejection in layered
                     .Where(x => x.EventName == "AiSpellRejected")
                     .GroupBy(x => x.ReasonCode ?? "unknown")
                     .OrderByDescending(x => x.Count())
                     .Take(20))
        {
            builder.AppendLine($"- `{rejection.Key}`: {rejection.Count()}");
        }
        builder.AppendLine();

        builder.AppendLine("## Summon failures");
        foreach (var failure in layered
                     .Where(x => x.EventName is "SummonFailedReason" or "SummonResult" && x.Result == "Failed")
                     .GroupBy(x => x.ReasonCode ?? x.Result ?? "unknown")
                     .OrderByDescending(x => x.Count())
                     .Take(20))
        {
            builder.AppendLine($"- `{failure.Key}`: {failure.Count()}");
        }

        return builder.ToString();
    }
}
