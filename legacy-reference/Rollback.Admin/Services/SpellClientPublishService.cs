using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.GameEffects;
using Rollback.Admin.Models.Spells;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class SpellClientPublishService
{
    private static readonly Regex SpellEntryRegex = new(
        @"^(?<indent>\s*)(?:this\.)?_datas\[(?<id>\d+)\]\s*=\s*Spell\.create\((?<args>.+)\);\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex SpellLevelEntryRegex = new(
        @"^(?<indent>\s*)(?:this\.)?_datas\[(?<id>\d+)\]\s*=\s*SpellLevel\.create\((?<args>.+)\);\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex DataFieldVisibilityRegex = new(
        @"protected\s+var\s+_datas:Array\s*=\s*new Array\(\);",
        RegexOptions.Compiled);

    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly AdminEntityClientMetadataService _clientMetadataService;
    private readonly ClientDataPathResolver _pathResolver;
    private readonly FfdecSpellLevelScriptExtractor _scriptExtractor;
    private readonly GameEffectEditorService _effectEditorService;
    private readonly ReferenceSpellCatalogService _referenceCatalogService;
    private readonly ClientEffectDefinitionService _clientEffectDefinitionService;
    private readonly SpellClientPresentationCompatibilityService _compatibilityService;
    private readonly SpellTooltipFallbackService _tooltipFallbackService;
    private readonly ClientSpellMetadataService _clientMetadataServiceReader = new();

    public SpellClientPublishService(
        AdminDbConnectionFactory connectionFactory,
        AdminEntityClientMetadataService clientMetadataService,
        ClientDataPathResolver pathResolver,
        FfdecSpellLevelScriptExtractor scriptExtractor,
        GameEffectEditorService effectEditorService,
        ReferenceSpellCatalogService referenceCatalogService,
        ClientEffectDefinitionService clientEffectDefinitionService,
        SpellClientPresentationCompatibilityService compatibilityService,
        SpellTooltipFallbackService tooltipFallbackService)
    {
        _connectionFactory = connectionFactory;
        _clientMetadataService = clientMetadataService;
        _pathResolver = pathResolver;
        _scriptExtractor = scriptExtractor;
        _effectEditorService = effectEditorService;
        _referenceCatalogService = referenceCatalogService;
        _clientEffectDefinitionService = clientEffectDefinitionService;
        _compatibilityService = compatibilityService;
        _tooltipFallbackService = tooltipFallbackService;
    }

    public async Task<SpellClientPublishResult> PublishAsync(SpellEditModel model, CancellationToken cancellationToken = default)
    {
        var levelResult = await PublishRuntimeLevelsAsync(model.Id, cancellationToken);
        var warnings = levelResult.Warnings.ToList();

        var presentationSummary = await PublishPresentationAsync(model, warnings, cancellationToken);

        return new SpellClientPublishResult
        {
            Summary = string.Join(
                " ",
                new[] { levelResult.Summary, presentationSummary }
                    .Where(text => !string.IsNullOrWhiteSpace(text))),
            Warnings = warnings,
        };
    }

    public async Task<SpellClientPublishResult> PublishRuntimeLevelsAsync(short spellId, CancellationToken cancellationToken = default)
    {
        var runtimeLevels = await LoadRuntimeLevelsAsync(spellId, cancellationToken);
        if (runtimeLevels.Count == 0)
        {
            return new SpellClientPublishResult
            {
                Summary = $"El hechizo #{spellId} no tiene niveles runtime para publicar en SpellLevels*.swf.",
                Warnings = Array.Empty<string>(),
            };
        }

        if (!_scriptExtractor.IsAvailable)
        {
            return new SpellClientPublishResult
            {
                Summary = $"El hechizo #{spellId} se guardo en runtime, pero no se pudo sincronizar SpellLevels*.swf porque FFDec no esta disponible.",
                Warnings = new[] { "Instala FFDec para reflejar AP/rango/flags/estados del hechizo dentro del cliente." },
            };
        }

        var backupDirectory = CreateBackupDirectory(spellId);
        var warnings = new List<string>();
        var updatedChunks = new List<short>();

        foreach (var chunkGroup in runtimeLevels.Values.GroupBy(level => (short)(level.LevelId / 1000)).OrderBy(group => group.Key))
        {
            if (!_scriptExtractor.TryExtractChunk(chunkGroup.Key, out var chunkEntries, out var sourceDescription))
            {
                warnings.Add($"No se pudo leer SpellLevels{chunkGroup.Key}.swf: {sourceDescription}");
                continue;
            }

            var missingLevels = chunkGroup
                .Where(level => !chunkEntries.ContainsKey(level.LevelId))
                .Select(level => level.LevelId)
                .ToArray();

            if (missingLevels.Length > 0)
            {
                warnings.Add(
                    $"SpellLevels{chunkGroup.Key}.swf no contenia los levelId {string.Join(", ", missingLevels)} del hechizo #{spellId}. Se insertaran durante este publish.");
            }

            if (await PatchChunkAsync(
                    chunkGroup.Key,
                    chunkGroup.ToDictionary(level => level.LevelId),
                    backupDirectory,
                    cancellationToken))
                updatedChunks.Add(chunkGroup.Key);
        }

        var summary = updatedChunks.Count == 0
            ? $"No se actualizaron chunks cliente para el hechizo #{spellId}."
            : $"SpellLevels cliente sincronizado para el hechizo #{spellId} en {string.Join(", ", updatedChunks.Select(chunk => $"SpellLevels{chunk}.swf"))}.";

        warnings.Add("La sincronizacion cliente ahora alinea param1/param2/param3 de EffectInstance con las plantillas reales de Effects0/i18n del cliente para que PatternDecoder resuelva mejor danos, robos de vida y buffs simples.");

        return new SpellClientPublishResult
        {
            Summary = summary,
            Warnings = warnings,
        };
    }

    private async Task<string> PublishPresentationAsync(
        SpellEditModel model,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        if (!_scriptExtractor.IsAvailable)
        {
            warnings.Add("FFDec no esta disponible para publicar nombre/descripcion de hechizos en el cliente.");
            return $"No se pudo publicar la presentacion cliente del hechizo #{model.Id}.";
        }

        var desiredName = ResolvePublishedName(model);
        var generatedTooltipDescription = _tooltipFallbackService.BuildDescription(model);
        var desiredDescription = ResolvePublishedDescription(model, generatedTooltipDescription);
        var persistedMetadata = await _clientMetadataService.GetAsync(
            AdminEntityType.Spell,
            model.Id,
            cancellationToken: cancellationToken);
        var clientMetadata = _clientMetadataServiceReader.Get(model.Id);
        var reference = _referenceCatalogService.Get(model.Id);
        var (nameId, descriptionId) = ResolveTextIds(persistedMetadata, clientMetadata, model);
        var iconId = ResolveIconId(persistedMetadata, clientMetadata, model);
        _compatibilityService.TryGet(model.Id, out var compatibilityOverride);
        var preferredScriptParams = ResolveScriptParams(reference);
        var preferredScriptId = ResolveScriptId(reference);
        var backupDirectory = CreateBackupDirectory(model.Id);

        await PublishTextAsync(nameId, desiredName, backupDirectory, cancellationToken);
        await PublishTextAsync(descriptionId, desiredDescription, backupDirectory, cancellationToken);
        await PublishSpellDefinitionAsync(
            model.Id,
            nameId,
            descriptionId,
            iconId,
            model.TypeId,
            model.Levels.OrderBy(level => level.LevelNumber).Select(level => level.Id).Where(levelId => levelId > 0).ToArray(),
            compatibilityOverride,
            preferredScriptParams,
            preferredScriptId,
            backupDirectory,
            warnings,
            cancellationToken);

        await _clientMetadataService.SaveAsync(
            AdminEntityType.Spell,
            model.Id,
            nameId,
            descriptionId,
            iconId,
            cancellationToken: cancellationToken);

        ClientI18nTextService.InvalidateCache();
        ClientSpellMetadataService.RegisterOrUpdate(new AdminClientSpellMetadata
        {
            SpellId = model.Id,
            TypeId = model.TypeId,
            NameId = nameId,
            DescriptionId = descriptionId,
            IconId = iconId,
            ScriptParams = compatibilityOverride?.ScriptParams ?? clientMetadata.ScriptParams,
            ScriptId = compatibilityOverride?.ScriptId ?? clientMetadata.ScriptId,
        });

        return $"Textos cliente sincronizados para el hechizo #{model.Id} con NameId {nameId} y DescriptionId {descriptionId}.";
    }

    private async Task PublishSpellDefinitionAsync(
        short spellId,
        int nameId,
        int descriptionId,
        int iconId,
        sbyte typeId,
        IReadOnlyCollection<int> levelIds,
        SpellClientPresentationCompatibility? compatibilityOverride,
        string? preferredScriptParams,
        int? preferredScriptId,
        string backupDirectory,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var commonDirectory = _pathResolver.EnsureCommonDataDirectory();
        var chunkId = spellId / 1000;
        var swfPath = Path.Combine(commonDirectory, $"Spells{chunkId}.swf");
        if (!File.Exists(swfPath))
        {
            warnings.Add($"No existe Spells{chunkId}.swf para sincronizar la definicion cliente del hechizo #{spellId}.");
            return;
        }

        await PatchSwfClassAsync(
            swfPath,
            $"Spells{chunkId}",
            script => RewriteSpellEntry(script, spellId, nameId, descriptionId, iconId, typeId, levelIds, compatibilityOverride, preferredScriptParams, preferredScriptId),
            backupDirectory,
            cancellationToken);
    }

    private async Task PublishTextAsync(
        int textId,
        string text,
        string backupDirectory,
        CancellationToken cancellationToken)
    {
        if (textId <= 0)
            throw new InvalidOperationException("No se pudo resolver un TextId valido para publicar el hechizo en i18n.");

        var i18nDirectory = _pathResolver.EnsureSpanishI18nDirectory();
        var i18nTmpDirectory = _pathResolver.EnsureSpanishI18nTmpDirectory();
        var chunkId = textId / 1000;
        var swfPath = Path.Combine(i18nDirectory, $"i18n{chunkId}.swf");
        if (!File.Exists(swfPath))
            throw new InvalidOperationException($"No existe i18n{chunkId}.swf para publicar el TextId {textId}.");

        var updatedScript = await PatchSwfClassAsync(
            swfPath,
            $"i18n{chunkId}",
            script => UpsertTextEntry(script, textId, text),
            backupDirectory,
            cancellationToken);

        var tmpPath = Path.Combine(i18nTmpDirectory, $"i18n{chunkId}.as");
        BackupFileIfNeeded(tmpPath, backupDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(tmpPath)!);
        await File.WriteAllTextAsync(tmpPath, updatedScript, Encoding.UTF8, cancellationToken);
    }

    private async Task<Dictionary<int, RuntimeSpellClientLevelRow>> LoadRuntimeLevelsAsync(short spellId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var headerCommand = connection.CreateCommand();
        headerCommand.CommandText = """
            SELECT TypeId, SpellLevelsCSV
            FROM spells_templates
            WHERE Id = @spellId
            LIMIT 1;
            """;
        headerCommand.Parameters.AddWithValue("@spellId", spellId);

        string levelsCsv;
        sbyte typeId;
        await using (var headerReader = await headerCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (!await headerReader.ReadAsync(cancellationToken))
                return new Dictionary<int, RuntimeSpellClientLevelRow>();

            levelsCsv = headerReader.GetSafeString("SpellLevelsCSV");
            typeId = headerReader.GetSafeSByte("TypeId");
        }

        var levelIds = levelsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var levelId) ? levelId : 0)
            .Where(levelId => levelId > 0)
            .Distinct()
            .ToArray();

        if (levelIds.Length == 0)
            return new Dictionary<int, RuntimeSpellClientLevelRow>();

        await using var levelCommand = connection.CreateCommand();
        levelCommand.CommandText = $"""
            SELECT
                Id,
                APCost,
                MinRange,
                MaxRange,
                CastInLine,
                CastTestLOS,
                NeedFreeCell,
                RangeCanBeBoosted,
                CriticalHitProbability,
                CriticalFailureProbability,
                MaxCastPerTurn,
                MaxCastPerTarget,
                MinCastInterval,
                MinPlayerLevel,
                CriticalFailureEndsTurn,
                BinaryEffects,
                BinaryCriticalEffects,
                StatesRequiredCSV,
                StatesForbiddenCSV
            FROM spells_levels
            WHERE Id IN ({string.Join(",", levelIds)})
            ORDER BY Id ASC;
            """;

        var rows = new Dictionary<int, RuntimeSpellClientLevelRow>();
        await using var reader = await levelCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var levelId = reader.GetSafeInt32("Id");
            rows[levelId] = new RuntimeSpellClientLevelRow(
                spellId,
                levelId,
                typeId,
                reader.GetSafeByte("APCost"),
                reader.GetSafeSByte("MinRange"),
                reader.GetSafeSByte("MaxRange"),
                reader.GetSafeBoolean("CastInLine"),
                reader.GetSafeBoolean("CastTestLOS"),
                reader.GetSafeBoolean("NeedFreeCell"),
                reader.GetSafeBoolean("RangeCanBeBoosted"),
                reader.GetSafeSByte("CriticalHitProbability"),
                reader.GetSafeSByte("CriticalFailureProbability"),
                reader.GetSafeByte("MaxCastPerTurn"),
                reader.GetSafeByte("MaxCastPerTarget"),
                reader.GetSafeByte("MinCastInterval"),
                reader.GetSafeByte("MinPlayerLevel"),
                reader.GetSafeBoolean("CriticalFailureEndsTurn"),
                _effectEditorService.Deserialize(reader.GetSafeBytes("BinaryEffects")),
                _effectEditorService.Deserialize(reader.GetSafeBytes("BinaryCriticalEffects")),
                NormalizeStates(reader.GetSafeString("StatesRequiredCSV")),
                NormalizeStates(reader.GetSafeString("StatesForbiddenCSV")));
        }

        return rows;
    }

    private async Task<bool> PatchChunkAsync(
        short chunkId,
        IReadOnlyDictionary<int, RuntimeSpellClientLevelRow> runtimeLevels,
        string backupDirectory,
        CancellationToken cancellationToken)
    {
        var commonDirectory = _pathResolver.EnsureCommonDataDirectory();
        var swfPath = Path.Combine(commonDirectory, $"SpellLevels{chunkId}.swf");
        if (!File.Exists(swfPath))
            throw new InvalidOperationException($"No existe SpellLevels{chunkId}.swf.");

        var workspace = _pathResolver.CreateTempWorkspace($"publish-spelllevels{chunkId}");
        var keepWorkspace = false;
        try
        {
            var exportDirectory = Path.Combine(workspace, "src");
            RunFfdec(
                "-selectclass",
                $"SpellLevels{chunkId}",
                "-export",
                "script",
                exportDirectory,
                swfPath);

            var scriptsDirectory = Path.Combine(exportDirectory, "scripts");
            var scriptPath = Path.Combine(scriptsDirectory, $"SpellLevels{chunkId}.as");
            if (!File.Exists(scriptPath))
                throw new InvalidOperationException($"FFDec no exporto SpellLevels{chunkId}.as.");

            var script = await File.ReadAllTextAsync(scriptPath, Encoding.UTF8, cancellationToken);
            var updatedScript = EnsureImportableScript(RewriteSpellLevelEntries(script, runtimeLevels));

            if (string.Equals(script, updatedScript, StringComparison.Ordinal))
                return false;

            await File.WriteAllTextAsync(scriptPath, updatedScript, Encoding.UTF8, cancellationToken);

            var patchedPath = Path.Combine(workspace, $"SpellLevels{chunkId}.patched.swf");
            File.Copy(swfPath, patchedPath, overwrite: true);

            RunFfdec(
                "-importScript",
                swfPath,
                patchedPath,
                scriptsDirectory);

            BackupFileIfNeeded(swfPath, backupDirectory);
            File.Copy(patchedPath, swfPath, overwrite: true);
            return true;
        }
        catch (Exception exception)
        {
            keepWorkspace = true;
            throw new InvalidOperationException(
                $"{exception.Message}{Environment.NewLine}Workspace preservado para inspeccion: {workspace}",
                exception);
        }
        finally
        {
            if (!keepWorkspace)
            {
                try
                {
                    Directory.Delete(workspace, recursive: true);
                }
                catch
                {
                    // Keep leftovers only if cleanup fails.
                }
            }
        }
    }

    private string RewriteSpellLevelEntries(
        string script,
        IReadOnlyDictionary<int, RuntimeSpellClientLevelRow> runtimeLevels)
    {
        var builder = new StringBuilder(script.Length + (runtimeLevels.Count * 32));
        var lastIndex = 0;
        var updatedLevelIds = new HashSet<int>();

        foreach (Match match in SpellLevelEntryRegex.Matches(script))
        {
            builder.Append(script, lastIndex, match.Index - lastIndex);
            lastIndex = match.Index + match.Length;

            if (!int.TryParse(match.Groups["id"].Value, out var levelId) ||
                !runtimeLevels.TryGetValue(levelId, out var runtimeLevel))
            {
                builder.Append(match.Value);
                continue;
            }

            updatedLevelIds.Add(levelId);
            var arguments = SplitArguments(match.Groups["args"].Value);
            if (arguments.Count < 21)
            {
                builder.Append(match.Value);
                continue;
            }

            arguments[3] = runtimeLevel.APCost.ToString();
            arguments[4] = runtimeLevel.MinRange.ToString();
            arguments[5] = runtimeLevel.MaxRange.ToString();
            arguments[6] = ToAsBoolean(runtimeLevel.CastInLine);
            arguments[7] = ToAsBoolean(runtimeLevel.CastTestLos);
            arguments[8] = runtimeLevel.CriticalHitProbability.ToString();
            arguments[9] = runtimeLevel.CriticalFailureProbability.ToString();
            arguments[10] = ToAsBoolean(runtimeLevel.NeedFreeCell);
            arguments[11] = ToAsBoolean(runtimeLevel.RangeCanBeBoosted);
            arguments[12] = runtimeLevel.MaxCastPerTurn.ToString();
            arguments[13] = runtimeLevel.MaxCastPerTarget.ToString();
            arguments[14] = runtimeLevel.MinCastInterval.ToString();
            arguments[15] = runtimeLevel.MinPlayerLevel.ToString();
            arguments[16] = ToAsBoolean(runtimeLevel.CriticalFailureEndsTurn);
            arguments[17] = BuildStatesVector(runtimeLevel.StatesRequired);
            arguments[18] = BuildStatesVector(runtimeLevel.StatesForbidden);
            arguments[19] = BuildEffectVectorLiteral(
                runtimeLevel.Effects,
                ExtractEffectInstanceExpressions(arguments[19]));
            arguments[20] = BuildEffectVectorLiteral(
                runtimeLevel.CriticalEffects,
                ExtractEffectInstanceExpressions(arguments[20]));

            builder.Append(match.Value.Replace(match.Groups["args"].Value, string.Join(",", arguments), StringComparison.Ordinal));
        }

        builder.Append(script, lastIndex, script.Length - lastIndex);
        var updatedScript = builder.ToString();
        foreach (var runtimeLevel in runtimeLevels.Values
                     .Where(level => !updatedLevelIds.Contains(level.LevelId))
                     .OrderBy(level => level.LevelId))
        {
            updatedScript = InsertBeforeCreateClosingBrace(updatedScript, BuildSpellLevelEntryLine(runtimeLevel));
        }

        return updatedScript;
    }

    private static string RewriteSpellEntry(
        string script,
        short spellId,
        int nameId,
        int descriptionId,
        int iconId,
        sbyte typeId,
        IReadOnlyCollection<int> levelIds,
        SpellClientPresentationCompatibility? compatibilityOverride,
        string? preferredScriptParams,
        int? preferredScriptId)
    {
        var builder = new StringBuilder(script.Length + 128);
        var lastIndex = 0;
        var replaced = false;

        foreach (Match match in SpellEntryRegex.Matches(script))
        {
            builder.Append(script, lastIndex, match.Index - lastIndex);
            lastIndex = match.Index + match.Length;

            if (!short.TryParse(match.Groups["id"].Value, out var entrySpellId) || entrySpellId != spellId)
            {
                builder.Append(match.Value);
                continue;
            }

            if (replaced)
                continue;

            var arguments = SplitArguments(match.Groups["args"].Value);
            if (arguments.Count < 8)
            {
                builder.Append(match.Value);
                replaced = true;
                continue;
            }

            arguments[0] = spellId.ToString();
            arguments[1] = nameId.ToString();
            arguments[2] = descriptionId.ToString();
            arguments[3] = compatibilityOverride is not null
                ? $"\"{EscapeStringLiteral(compatibilityOverride.ScriptParams)}\""
                : arguments[3];
            arguments[4] = compatibilityOverride is not null
                ? compatibilityOverride.ScriptId.ToString()
                : arguments[4];
            arguments[5] = compatibilityOverride?.IconId?.ToString()
                ?? arguments[5];
            arguments[6] = typeId.ToString();
            arguments[7] = BuildSpellLevelsVector(levelIds);

            builder.Append($"{match.Groups["indent"].Value}this._datas[{spellId}] = Spell.create({string.Join(",", arguments)});");
            replaced = true;
        }

        builder.Append(script, lastIndex, script.Length - lastIndex);
        if (replaced)
            return RemoveDuplicateSpellEntryLines(builder.ToString(), spellId);

        var scriptParamsLiteral = compatibilityOverride is not null
            ? $"\"{EscapeStringLiteral(compatibilityOverride.ScriptParams)}\""
            : preferredScriptParams is not null
            ? $"\"{EscapeStringLiteral(preferredScriptParams)}\""
            : "\"\"";
        var scriptIdLiteral = compatibilityOverride?.ScriptId.ToString()
            ?? preferredScriptId?.ToString()
            ?? "0";
        var iconLiteral = compatibilityOverride?.IconId?.ToString() ?? iconId.ToString();

        return RemoveDuplicateSpellEntryLines(
            InsertBeforeCreateClosingBrace(
                builder.ToString(),
                $"         this._datas[{spellId}] = Spell.create({spellId},{nameId},{descriptionId},{scriptParamsLiteral},{scriptIdLiteral},{iconLiteral},{typeId},{BuildSpellLevelsVector(levelIds)});"),
            spellId);
    }

    private static string EnsureImportableScript(string script)
    {
        if (DataFieldVisibilityRegex.IsMatch(script))
            return DataFieldVisibilityRegex.Replace(script, "public var _datas:Array = new Array();", 1);

        return script;
    }

    private async Task<string> PatchSwfClassAsync(
        string swfPath,
        string className,
        Func<string, string> transform,
        string backupDirectory,
        CancellationToken cancellationToken)
    {
        var workspace = _pathResolver.CreateTempWorkspace($"publish-{className.ToLowerInvariant()}");
        var keepWorkspace = false;
        try
        {
            var exportDirectory = Path.Combine(workspace, "src");
            RunFfdec("-selectclass", className, "-export", "script", exportDirectory, swfPath);

            var scriptsDirectory = Path.Combine(exportDirectory, "scripts");
            var scriptPath = Path.Combine(scriptsDirectory, $"{className}.as");
            if (!File.Exists(scriptPath))
                throw new InvalidOperationException($"FFDec no exporto {className}.as.");

            var script = await File.ReadAllTextAsync(scriptPath, Encoding.UTF8, cancellationToken);
            var transformed = EnsureImportableScript(transform(script));
            await File.WriteAllTextAsync(scriptPath, transformed, Encoding.UTF8, cancellationToken);

            var patchedPath = Path.Combine(workspace, Path.GetFileName(swfPath));
            RunFfdec("-importScript", swfPath, patchedPath, scriptsDirectory);

            BackupFileIfNeeded(swfPath, backupDirectory);
            File.Copy(patchedPath, swfPath, overwrite: true);
            return transformed;
        }
        catch (Exception exception)
        {
            keepWorkspace = true;
            throw new InvalidOperationException(
                $"{exception.Message}{Environment.NewLine}Workspace preservado para inspeccion: {workspace}",
                exception);
        }
        finally
        {
            if (!keepWorkspace)
            {
                try
                {
                    Directory.Delete(workspace, recursive: true);
                }
                catch
                {
                    // Keep leftovers only if cleanup fails.
                }
            }
        }
    }

    private void RunFfdec(params string[] arguments)
    {
        var ffdecPath = _pathResolver.EnsureFfdecCliPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = ffdecPath,
            Arguments = string.Join(" ", arguments.Select(QuoteArgument)),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("No se pudo iniciar FFDec.");
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0)
            return;

        var details = string.Join(
            Environment.NewLine,
            new[] { standardOutput, standardError }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim()));

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(details)
                ? $"FFDec fallo con codigo {process.ExitCode}."
                : $"FFDec fallo con codigo {process.ExitCode}:{Environment.NewLine}{details}");
    }

    private string CreateBackupDirectory(short spellId)
    {
        var repoRoot = _pathResolver.EnsureRepoRoot();
        var directory = Path.Combine(
            repoRoot,
            "runtime",
            "client-state-backups",
            "spell-level-publish",
            $"spell-{spellId}",
            DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void BackupFileIfNeeded(string path, string backupDirectory)
    {
        if (!File.Exists(path))
            return;

        Directory.CreateDirectory(backupDirectory);
        var backupPath = Path.Combine(backupDirectory, Path.GetFileName(path));
        if (!File.Exists(backupPath))
            File.Copy(path, backupPath, overwrite: false);
    }

    private static string QuoteArgument(string value) =>
        value.Contains(' ') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\\\"")}\""
            : value;

    private static string ResolvePublishedName(SpellEditModel model)
    {
        var candidates = new[]
        {
            model.OverrideName,
            model.ReferenceName,
            model.ClientName,
            model.Name,
        };

        foreach (var candidate in candidates)
        {
            var normalized = NormalizePublishedText(candidate);
            if (!string.IsNullOrWhiteSpace(normalized))
                return normalized;
        }

        return $"Hechizo #{model.Id}";
    }

    private static string ResolvePublishedDescription(SpellEditModel model, string generatedTooltipDescription)
    {
        var normalizedManualDescription = NormalizePublishedText(model.OverrideDescription);
        if (!string.IsNullOrWhiteSpace(generatedTooltipDescription))
        {
            return string.IsNullOrWhiteSpace(normalizedManualDescription)
                ? generatedTooltipDescription
                : $"{normalizedManualDescription}{Environment.NewLine}{Environment.NewLine}{generatedTooltipDescription}";
        }

        var candidates = new[]
        {
            normalizedManualDescription,
            model.ReferenceDescription,
            model.Description,
            model.ClientDescription,
        };

        foreach (var candidate in candidates)
        {
            var normalized = NormalizePublishedText(candidate);
            if (!string.IsNullOrWhiteSpace(normalized))
                return normalized;
        }

        return $"Hechizo #{model.Id}.";
    }

    private static string? ResolveScriptParams(ReferenceSpellIdentity? reference)
    {
        if (reference is null)
            return null;

        var normalized = (reference.ScriptParams ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized) ||
            string.Equals(normalized, "null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return normalized;
    }

    private static int? ResolveScriptId(ReferenceSpellIdentity? reference)
    {
        if (reference is null || reference.ScriptId <= 0)
            return null;

        return reference.ScriptId;
    }

    private static string NormalizePublishedText(string? text)
    {
        var normalized = (text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        normalized = Regex.Replace(normalized, @"\s*\[#\d+\]\s*$", string.Empty, RegexOptions.IgnoreCase);
        return normalized.Trim();
    }

    private (int NameId, int DescriptionId) ResolveTextIds(
        AdminEntityClientMetadata? persistedMetadata,
        AdminClientSpellMetadata clientMetadata,
        SpellEditModel model)
    {
        var nameId = persistedMetadata?.NameId > 0
            ? persistedMetadata.NameId
            : clientMetadata.NameId.GetValueOrDefault();
        if (nameId <= 0 && model.ReferenceNameId is > 0)
            nameId = model.ReferenceNameId.Value;

        var descriptionId = persistedMetadata?.DescriptionId > 0
            ? persistedMetadata.DescriptionId
            : clientMetadata.DescriptionId.GetValueOrDefault();
        if (descriptionId <= 0 && model.ReferenceDescriptionId is > 0)
            descriptionId = model.ReferenceDescriptionId.Value;

        if (nameId > 0 && descriptionId > 0)
            return (nameId, descriptionId);

        var allocated = AllocateNewTextIds();
        return (
            nameId > 0 ? nameId : allocated.NameId,
            descriptionId > 0 ? descriptionId : allocated.DescriptionId);
    }

    private static int ResolveIconId(
        AdminEntityClientMetadata? persistedMetadata,
        AdminClientSpellMetadata clientMetadata,
        SpellEditModel model)
    {
        if (persistedMetadata?.IconId > 0)
            return persistedMetadata.IconId;

        if (clientMetadata.IconId is > 0)
            return clientMetadata.IconId.Value;

        if (model.ClientIconId is > 0)
            return model.ClientIconId.Value;

        if (model.ReferenceIconId is > 0)
            return model.ReferenceIconId.Value;

        return 0;
    }

    private (int NameId, int DescriptionId) AllocateNewTextIds()
    {
        var tmpDirectory = _pathResolver.EnsureSpanishI18nTmpDirectory();
        var fileInfos = Directory
            .EnumerateFiles(tmpDirectory, "i18n*.as", SearchOption.TopDirectoryOnly)
            .Select(LoadI18nFileInfo)
            .Where(info => info is not null)
            .Cast<I18nFileInfo>()
            .OrderByDescending(info => info.ChunkId)
            .ToArray();

        var candidate = fileInfos.FirstOrDefault(info => info.MaxId <= info.ChunkId * 1000 + 997);
        if (candidate is null)
            throw new InvalidOperationException("No se encontro un archivo i18n con espacio para asignar NameId y DescriptionId nuevos.");

        return (candidate.MaxId + 1, candidate.MaxId + 2);
    }

    private static I18nFileInfo? LoadI18nFileInfo(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        if (!fileName.StartsWith("i18n", StringComparison.OrdinalIgnoreCase))
            return null;

        if (!int.TryParse(fileName["i18n".Length..], out var chunkId))
            return null;

        var maxId = chunkId * 1000;
        var content = File.ReadAllText(path, Encoding.UTF8);
        foreach (Match match in Regex.Matches(content, "(?:this\\.)?_datas\\[(?<id>\\d+)\\]\\s*="))
        {
            if (int.TryParse(match.Groups["id"].Value, out var currentId))
                maxId = Math.Max(maxId, currentId);
        }

        return new I18nFileInfo(chunkId, maxId);
    }

    private static string UpsertTextEntry(string script, int textId, string text)
    {
        var line = $"         _datas[{textId}] = \"{EscapeStringLiteral(text)}\";";
        var entryRegex = new Regex($@"^\s*(?:this\.)?_datas\[{textId}\]\s*=.*(?:\r?\n)?", RegexOptions.Multiline);
        if (entryRegex.IsMatch(script))
            return entryRegex.Replace(script, line + Environment.NewLine, 1);

        return InsertBeforeCreateClosingBrace(script, line);
    }

    private static string InsertBeforeCreateClosingBrace(string script, string line)
    {
        var marker = "      }\r\n   }\r\n}";
        var index = script.LastIndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
        {
            marker = "      }\n   }\n}";
            index = script.LastIndexOf(marker, StringComparison.Ordinal);
        }

        if (index < 0)
            throw new InvalidOperationException("No se encontro el cierre de create() para insertar la entrada _datas.");

        var newline = script.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        return script.Insert(index, $"{line}{newline}");
    }

    private static string RemoveDuplicateSpellEntryLines(string script, short spellId)
    {
        var newline = script.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = script.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var needle = $"_datas[{spellId}] = Spell.create(";
        var seen = false;
        var filtered = new List<string>(lines.Length);

        foreach (var line in lines)
        {
            if (!line.Contains(needle, StringComparison.Ordinal))
            {
                filtered.Add(line);
                continue;
            }

            if (seen)
                continue;

            seen = true;
            filtered.Add(line);
        }

        return string.Join(newline, filtered);
    }

    private static string EscapeStringLiteral(string value) =>
        (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");

    private static string BuildSpellLevelsVector(IReadOnlyCollection<int> levelIds) =>
        $"Vector.<uint>([{string.Join(",", levelIds.OrderBy(value => value))}])";

    private string BuildSpellLevelEntryLine(RuntimeSpellClientLevelRow runtimeLevel) =>
        $"         this._datas[{runtimeLevel.LevelId}] = SpellLevel.create({runtimeLevel.LevelId},{runtimeLevel.SpellId},{runtimeLevel.TypeId},{runtimeLevel.APCost},{runtimeLevel.MinRange},{runtimeLevel.MaxRange},{ToAsBoolean(runtimeLevel.CastInLine)},{ToAsBoolean(runtimeLevel.CastTestLos)},{runtimeLevel.CriticalHitProbability},{runtimeLevel.CriticalFailureProbability},{ToAsBoolean(runtimeLevel.NeedFreeCell)},{ToAsBoolean(runtimeLevel.RangeCanBeBoosted)},{runtimeLevel.MaxCastPerTurn},{runtimeLevel.MaxCastPerTarget},{runtimeLevel.MinCastInterval},{runtimeLevel.MinPlayerLevel},{ToAsBoolean(runtimeLevel.CriticalFailureEndsTurn)},{BuildStatesVector(runtimeLevel.StatesRequired)},{BuildStatesVector(runtimeLevel.StatesForbidden)},{BuildEffectVectorLiteral(runtimeLevel.Effects, Array.Empty<string>())},{BuildEffectVectorLiteral(runtimeLevel.CriticalEffects, Array.Empty<string>())});";

    private static List<string> SplitArguments(string rawArguments)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var depth = 0;
        var inString = false;
        var escaped = false;

        foreach (var character in rawArguments)
        {
            if (escaped)
            {
                current.Append(character);
                escaped = false;
                continue;
            }

            if (character == '\\')
            {
                current.Append(character);
                escaped = true;
                continue;
            }

            if (character == '"')
            {
                current.Append(character);
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                switch (character)
                {
                    case '(':
                    case '[':
                    case '<':
                    case '{':
                        depth++;
                        current.Append(character);
                        continue;
                    case ')':
                    case ']':
                    case '>':
                    case '}':
                        depth--;
                        current.Append(character);
                        continue;
                    case ',' when depth == 0:
                        result.Add(current.ToString().Trim());
                        current.Clear();
                        continue;
                }
            }

            current.Append(character);
        }

        if (current.Length > 0)
            result.Add(current.ToString().Trim());

        return result;
    }

    private static IReadOnlyList<string> ExtractEffectInstanceExpressions(string vectorLiteral) =>
        Regex.Matches(vectorLiteral ?? string.Empty, @"EffectInstance\.create\((?<args>[^()]*)\)")
            .Select(match => match.Value)
            .ToArray();

    private string BuildEffectVectorLiteral(
        IReadOnlyList<GameEffectEditRow> effects,
        IReadOnlyList<string> existingExpressions)
    {
        if (effects.Count == 0)
            return "new Vector.<EffectInstance>(0,true)";

        var expressions = new List<string>(effects.Count);
        for (var index = 0; index < effects.Count; index++)
        {
            var effect = effects[index];
            if (TryBuildEffectInstanceExpression(effect, out var expression))
            {
                expressions.Add(expression);
                continue;
            }

            if (index < existingExpressions.Count)
                expressions.Add(existingExpressions[index]);
        }

        return expressions.Count == 0
            ? "new Vector.<EffectInstance>(0,true)"
            : $"Vector.<EffectInstance>([{string.Join(",", expressions)}])";
    }

    private bool TryBuildEffectInstanceExpression(GameEffectEditRow effect, out string expression)
    {
        expression = string.Empty;
        if (!SupportsClientTooltipPublish(effect))
            return false;

        if (!TryBuildEffectInstanceParameters(effect, out var param1, out var param2, out var param3))
            return false;

        expression =
            $"EffectInstance.create({(int)effect.EffectId},{effect.Duration},{param1},{param2},{param3},{effect.Random},{effect.ZoneSize},{(int)effect.Shape},{(int)effect.TargetType})";
        return true;
    }

    private bool SupportsClientTooltipPublish(GameEffectEditRow effect)
    {
        if (effect.Kind is not (EffectEditorKind.Integer or EffectEditorKind.Dice))
            return false;

        if (!_clientEffectDefinitionService.TryGet(effect.EffectId, out _))
            return false;

        return effect.EffectId switch
        {
            EffectId.EffectDamageNeutral or
            EffectId.EffectDamageFire or
            EffectId.EffectDamageEarth or
            EffectId.EffectDamageWater or
            EffectId.EffectDamageAir or
            EffectId.EffectStealHPNeutral or
            EffectId.EffectStealHPFire or
            EffectId.EffectStealHPEarth or
            EffectId.EffectStealHPWater or
            EffectId.EffectStealHPAir or
            EffectId.EffectHealHP81 or
            EffectId.EffectHealHP108 or
            EffectId.EffectHealHP143 or
            EffectId.EffectAddAP111 or
            EffectId.EffectAddMP or
            EffectId.EffectAddMP128 or
            EffectId.EffectRegainAP or
            EffectId.EffectAddDamageBonus or
            EffectId.EffectAddDamageBonus121 or
            EffectId.EffectIncreaseDamage138 or
            EffectId.Effect701 or
            EffectId.EffectAddRange or
            EffectId.EffectAddRange136 or
            EffectId.EffectAddStrength or
            EffectId.EffectAddAgility or
            EffectId.EffectAddChance or
            EffectId.EffectAddWisdom or
            EffectId.EffectAddVitality or
            EffectId.EffectAddIntelligence or
            EffectId.EffectAddInitiative or
            EffectId.EffectAddProspecting or
            EffectId.EffectAddHealBonus or
            EffectId.EffectAddDodgeAPProbability or
            EffectId.EffectAddDodgeMPProbability or
            EffectId.EffectAddCriticalHit or
            EffectId.EffectAddDamageBonusPercent or
            EffectId.EffectAddSummonLimit or
            EffectId.EffectAddTrapBonus or
            EffectId.EffectAddTrapBonusPercent or
            EffectId.EffectAddEarthResistPercent or
            EffectId.EffectAddWaterResistPercent or
            EffectId.EffectAddAirResistPercent or
            EffectId.EffectAddFireResistPercent or
            EffectId.EffectAddNeutralResistPercent or
            EffectId.EffectAddEarthElementReduction or
            EffectId.EffectAddWaterElementReduction or
            EffectId.EffectAddAirElementReduction or
            EffectId.EffectAddFireElementReduction or
            EffectId.EffectAddNeutralElementReduction or
            EffectId.EffectSubDamageBonus or
            EffectId.EffectSubChance or
            EffectId.EffectSubVitality or
            EffectId.EffectSubAgility or
            EffectId.EffectSubIntelligence or
            EffectId.EffectSubWisdom or
            EffectId.EffectSubStrength or
            EffectId.EffectSubRange or
            EffectId.EffectLostAP or
            EffectId.EffectLostMP or
            EffectId.EffectSubDodgeAPProbability or
            EffectId.EffectSubDodgeMPProbability or
            EffectId.EffectSubCriticalHit or
            EffectId.EffectSubHealBonus or
            EffectId.EffectSubDamageBonusPercent or
            EffectId.EffectSubEarthResistPercent or
            EffectId.EffectSubWaterResistPercent or
            EffectId.EffectSubAirResistPercent or
            EffectId.EffectSubFireResistPercent or
            EffectId.EffectSubNeutralResistPercent or
            EffectId.EffectSubEarthElementReduction or
            EffectId.EffectSubWaterElementReduction or
            EffectId.EffectSubAirElementReduction or
            EffectId.EffectSubFireElementReduction or
            EffectId.EffectSubNeutralElementReduction or
            EffectId.EffectAddEarthDamageBonus or
            EffectId.EffectAddWaterDamageBonus or
            EffectId.EffectAddAirDamageBonus or
            EffectId.EffectAddFireDamageBonus or
            EffectId.EffectAddNeutralDamageBonus or
            EffectId.EffectSubEarthDamageBonus or
            EffectId.EffectSubWaterDamageBonus or
            EffectId.EffectSubAirDamageBonus or
            EffectId.EffectSubFireDamageBonus or
            EffectId.EffectSubNeutralDamageBonus => true,
            _ => false,
        };
    }

    private bool TryBuildEffectInstanceParameters(
        GameEffectEditRow effect,
        out string param1,
        out string param2,
        out string param3)
    {
        param1 = "null";
        param2 = "null";
        param3 = "null";

        var descriptionTemplate = _clientEffectDefinitionService.TryGetDescription(effect.EffectId, out var description)
            ? description
            : string.Empty;

        switch (effect.Kind)
        {
            case EffectEditorKind.Integer:
                var integerValue = effect.Value != 0
                    ? effect.Value
                    : effect.MinValue != 0
                        ? effect.MinValue
                        : effect.MaxValue;

                if (integerValue == 0)
                    return false;

                param1 = ToAsValueLiteral(integerValue);
                return true;

            case EffectEditorKind.Dice:
                return TryBuildDiceEffectParameters(effect, descriptionTemplate, out param1, out param2, out param3);

            default:
                return false;
        }
    }

    private static bool TryBuildDiceEffectParameters(
        GameEffectEditRow effect,
        string descriptionTemplate,
        out string param1,
        out string param2,
        out string param3)
    {
        param1 = "null";
        param2 = "null";
        param3 = "null";

        var hasMin = effect.MinValue != 0;
        var hasMax = effect.MaxValue != 0;
        var hasValue = effect.Value != 0;

        if (!hasMin && !hasMax && !hasValue)
            return false;

        if (hasValue && !hasMin && !hasMax && UsesOptionalDicePattern(descriptionTemplate))
        {
            param1 = "0";
            param2 = "0";
            param3 = ToAsValueLiteral(effect.Value);
            return true;
        }

        if (hasMax && effect.MaxValue != effect.MinValue)
        {
            param1 = ToAsValueLiteral(hasMin ? effect.MinValue : 0);

            if (UsesThirdSlotRangePattern(descriptionTemplate))
            {
                // Some client templates render the upper bound from #3, using #2 only as an optional-range sentinel.
                // Example: "Potencia: #1{~1~2 à }#3"
                param2 = "0";
                param3 = ToAsValueLiteral(effect.MaxValue);
                return true;
            }

            param2 = ToAsValueLiteral(effect.MaxValue);
            param3 = hasValue ? ToAsValueLiteral(effect.Value) : "null";
            return true;
        }

        var singleValue = hasMin
            ? effect.MinValue
            : hasMax
                ? effect.MaxValue
                : effect.Value;

        if (singleValue == 0)
            return false;

        param1 = ToAsValueLiteral(singleValue);
        param2 = "null";
        param3 = hasValue && effect.Value != singleValue
            ? ToAsValueLiteral(effect.Value)
            : "null";
        return true;
    }

    private static bool UsesOptionalDicePattern(string descriptionTemplate) =>
        !string.IsNullOrWhiteSpace(descriptionTemplate) &&
        descriptionTemplate.Contains("{~1~2", StringComparison.Ordinal) &&
        descriptionTemplate.Contains("#1", StringComparison.Ordinal) &&
        descriptionTemplate.Contains("#2", StringComparison.Ordinal);

    private static bool UsesThirdSlotRangePattern(string descriptionTemplate) =>
        !string.IsNullOrWhiteSpace(descriptionTemplate) &&
        descriptionTemplate.Contains("{~1~2", StringComparison.Ordinal) &&
        descriptionTemplate.Contains("#1", StringComparison.Ordinal) &&
        descriptionTemplate.Contains("#3", StringComparison.Ordinal) &&
        !descriptionTemplate.Contains("#2", StringComparison.Ordinal);

    private static string BuildStatesVector(IReadOnlyCollection<short> states) =>
        states.Count == 0
            ? "null"
            : $"Vector.<int>([{string.Join(",", states.OrderBy(value => value))}])";

    private static IReadOnlyCollection<short> NormalizeStates(string statesCsv) =>
        (statesCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => short.TryParse(value, out var parsed) ? parsed : (short)0)
            .Where(value => value > 0)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();

    private static string ToAsBoolean(bool value) =>
        value ? "true" : "false";

    private static string ToAsValueLiteral(int value) =>
        value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private sealed record RuntimeSpellClientLevelRow(
        short SpellId,
        int LevelId,
        sbyte TypeId,
        byte APCost,
        sbyte MinRange,
        sbyte MaxRange,
        bool CastInLine,
        bool CastTestLos,
        bool NeedFreeCell,
        bool RangeCanBeBoosted,
        sbyte CriticalHitProbability,
        sbyte CriticalFailureProbability,
        byte MaxCastPerTurn,
        byte MaxCastPerTarget,
        byte MinCastInterval,
        byte MinPlayerLevel,
        bool CriticalFailureEndsTurn,
        IReadOnlyList<GameEffectEditRow> Effects,
        IReadOnlyList<GameEffectEditRow> CriticalEffects,
        IReadOnlyCollection<short> StatesRequired,
        IReadOnlyCollection<short> StatesForbidden);

    private sealed record I18nFileInfo(int ChunkId, int MaxId);
}
