using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;
using Rollback.World.Game.Effects;

namespace Rollback.Admin.Services;

public sealed class SetClientPublishService
{
    private static readonly Regex DataEntryRegexTemplate = new(
        @"^\s*_datas\[(?<id>\d+)\]\s*=.*;$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex DataFieldVisibilityRegex = new(
        @"protected\s+var\s+_datas:Array\s*=\s*new Array\(\);",
        RegexOptions.Compiled);

    private readonly Infrastructure.AdminDbConnectionFactory _connectionFactory;
    private readonly ReferenceItemCatalogService _referenceCatalogService;
    private readonly AdminEntityClientMetadataService _clientMetadataService;
    private readonly ClientDataPathResolver _pathResolver;

    public SetClientPublishService(
        Infrastructure.AdminDbConnectionFactory connectionFactory,
        ReferenceItemCatalogService referenceCatalogService,
        AdminEntityClientMetadataService clientMetadataService,
        ClientDataPathResolver pathResolver)
    {
        _connectionFactory = connectionFactory;
        _referenceCatalogService = referenceCatalogService;
        _clientMetadataService = clientMetadataService;
        _pathResolver = pathResolver;
    }

    public async Task<SetClientPublishResult> PublishAsync(short setId, CancellationToken cancellationToken = default)
    {
        var runtime = await LoadRuntimeSetAsync(setId, cancellationToken)
            ?? throw new InvalidOperationException($"El set #{setId} no existe en runtime.");

        var warnings = new List<string>();
        var backupDirectory = CreateBackupDirectory(setId);
        var commonDirectory = _pathResolver.EnsureCommonDataDirectory();
        var itemSetsSwfPath = Path.Combine(commonDirectory, "ItemSets0.swf");
        if (!File.Exists(itemSetsSwfPath))
            throw new InvalidOperationException("No existe ItemSets0.swf para publicar el set en el cliente.");

        var nameText = ResolveVisibleName(runtime);
        var persistedMetadata = await _clientMetadataService.GetAsync(AdminEntityType.Set, setId, cancellationToken: cancellationToken);
        var nameId = ResolveNameId(persistedMetadata, nameText, warnings);

        warnings.Add($"En este cliente ItemSets0 publica nombre e items del set; los bonus activos ({runtime.TierCount} tier(s) runtime) siguen resolviendose desde el servidor.");

        await PublishTextAsync(nameId, nameText, backupDirectory, cancellationToken);
        await PublishSetDefinitionAsync(runtime, nameId, backupDirectory, cancellationToken);

        await _clientMetadataService.SaveAsync(
            AdminEntityType.Set,
            setId,
            nameId,
            0,
            0,
            cancellationToken: cancellationToken);

        ClientI18nTextService.InvalidateCache();

        return new SetClientPublishResult(
            $"Definicion cliente publicada para el set #{setId} en ItemSets0.swf con NameId {nameId} y {runtime.TierCount} tier(s) runtime.",
            warnings);
    }

    private async Task<RuntimeSetData?> LoadRuntimeSetAsync(short setId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, ItemsCSV, BinaryEffects
            FROM items_sets
            WHERE Id = @setId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@setId", setId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new RuntimeSetData
        {
            Id = reader.GetSafeInt16("Id"),
            Name = reader.GetSafeString("Name"),
            ItemIds = ParseItemsCsv(reader.GetSafeString("ItemsCSV")),
            TierCount = EffectManager.DeserializeSetBonusTiers(reader.GetSafeBytes("BinaryEffects")).Count,
        };
    }

    private int ResolveNameId(AdminEntityClientMetadata? persistedMetadata, string desiredName, List<string> warnings)
    {
        if (persistedMetadata?.NameId > 0)
        {
            var usage = AnalyzeTextIdUsage(persistedMetadata.NameId, desiredName);
            if (usage.IsReusable)
                return persistedMetadata.NameId;

            if (usage.ShouldCleanupDuplicate)
            {
                warnings.Add("El NameId cliente previo del set tenia entradas duplicadas en i18n. Se reasignara un id limpio.");
                return AllocateNewTextId();
            }
        }

        return AllocateNewTextId();
    }

    private string ResolveVisibleName(RuntimeSetData runtime)
    {
        var runtimeName = (runtime.Name ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(runtimeName))
            return runtimeName;

        var referenceName = _referenceCatalogService.GetSet(runtime.Id)?.Name?.Trim();
        if (!string.IsNullOrWhiteSpace(referenceName))
            return referenceName;

        return $"Set #{runtime.Id}";
    }

    private async Task PublishTextAsync(
        int textId,
        string text,
        string backupDirectory,
        CancellationToken cancellationToken)
    {
        if (textId <= 0)
            throw new InvalidOperationException("No se pudo resolver un NameId valido para publicar el set en i18n.");

        var i18nDirectory = _pathResolver.EnsureSpanishI18nDirectory();
        var i18nTmpDirectory = _pathResolver.EnsureSpanishI18nTmpDirectory();
        var chunkId = textId / 1000;
        var swfPath = Path.Combine(i18nDirectory, $"i18n{chunkId}.swf");
        if (!File.Exists(swfPath))
            throw new InvalidOperationException($"No existe i18n{chunkId}.swf para publicar el NameId {textId}.");

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

    private async Task PublishSetDefinitionAsync(
        RuntimeSetData runtime,
        int nameId,
        string backupDirectory,
        CancellationToken cancellationToken)
    {
        var commonDirectory = _pathResolver.EnsureCommonDataDirectory();
        var swfPath = Path.Combine(commonDirectory, "ItemSets0.swf");

        await PatchSwfClassAsync(
            swfPath,
            "ItemSets0",
            script => UpsertSetEntry(script, BuildSetScriptLine(runtime, nameId)),
            backupDirectory,
            cancellationToken);
    }

    private async Task<string> PatchSwfClassAsync(
        string swfPath,
        string className,
        Func<string, string> transform,
        string backupDirectory,
        CancellationToken cancellationToken)
    {
        var workspace = _pathResolver.CreateTempWorkspace($"publish-{className.ToLowerInvariant()}");
        try
        {
            var exportDirectory = Path.Combine(workspace, "src");
            RunFfdec(
                "-selectclass",
                className,
                "-export",
                "script",
                exportDirectory,
                swfPath);

            var scriptsDirectory = Path.Combine(exportDirectory, "scripts");
            var scriptPath = Path.Combine(scriptsDirectory, $"{className}.as");
            if (!File.Exists(scriptPath))
                throw new InvalidOperationException($"FFDec no exporto {className}.as desde {Path.GetFileName(swfPath)}.");

            var script = await File.ReadAllTextAsync(scriptPath, Encoding.UTF8, cancellationToken);
            var transformed = EnsureImportableScript(transform(script));
            await File.WriteAllTextAsync(scriptPath, transformed, Encoding.UTF8, cancellationToken);

            var patchedPath = Path.Combine(workspace, Path.GetFileName(swfPath));
            RunFfdec(
                "-importScript",
                swfPath,
                patchedPath,
                scriptsDirectory);

            BackupFileIfNeeded(swfPath, backupDirectory);
            File.Copy(patchedPath, swfPath, overwrite: true);
            return transformed;
        }
        finally
        {
            try
            {
                if (Directory.Exists(workspace))
                    Directory.Delete(workspace, recursive: true);
            }
            catch
            {
            }
        }
    }

    private static string UpsertSetEntry(string script, string setLine)
    {
        var entryRegex = BuildDataEntryRegex(GetDataId(setLine));
        if (entryRegex.IsMatch(script))
            return entryRegex.Replace(script, setLine, 1);

        return InsertBeforeCreateClosingBrace(script, setLine);
    }

    private static string UpsertTextEntry(string script, int textId, string text)
    {
        var line = $"         _datas[{textId}] = \"{EscapeStringLiteral(text)}\";";
        var entryRegex = BuildDataEntryRegex(textId);
        if (entryRegex.IsMatch(script))
            return entryRegex.Replace(script, line, 1);

        return InsertBeforeCreateClosingBrace(script, line);
    }

    private static Regex BuildDataEntryRegex(int id) =>
        new($@"^\s*(?:this\.)?_datas\[{id}\]\s*=.*(?:\r?\n)?", RegexOptions.Multiline);

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

    private static string EnsureImportableScript(string script)
    {
        if (DataFieldVisibilityRegex.IsMatch(script))
            return DataFieldVisibilityRegex.Replace(script, "public var _datas:Array = new Array();", 1);

        return script;
    }

    private string CreateBackupDirectory(short setId)
    {
        var repoRoot = _pathResolver.EnsureRepoRoot();
        var backupDirectory = Path.Combine(
            repoRoot,
            "runtime",
            "client-state-backups",
            "set-client-publish",
            $"{setId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}");
        Directory.CreateDirectory(backupDirectory);
        return backupDirectory;
    }

    private static void BackupFileIfNeeded(string filePath, string backupDirectory)
    {
        if (!File.Exists(filePath))
            return;

        var backupPath = Path.Combine(backupDirectory, Path.GetFileName(filePath));
        if (File.Exists(backupPath))
            return;

        File.Copy(filePath, backupPath, overwrite: false);
    }

    private int AllocateNewTextId()
    {
        var tmpDirectory = _pathResolver.EnsureSpanishI18nTmpDirectory();
        var fileInfos = Directory
            .EnumerateFiles(tmpDirectory, "i18n*.as", SearchOption.TopDirectoryOnly)
            .Select(LoadI18nFileInfo)
            .Where(info => info is not null)
            .Cast<I18nFileInfo>()
            .OrderByDescending(info => info.ChunkId)
            .ToArray();

        var candidate = fileInfos.FirstOrDefault(info => info.MaxId <= info.ChunkId * 1000 + 998);
        if (candidate is null)
            throw new InvalidOperationException("No se encontro un archivo i18n con espacio para asignar un NameId nuevo para el set.");

        return candidate.MaxId + 1;
    }

    private I18nFileInfo? LoadI18nFileInfo(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        if (!fileName.StartsWith("i18n", StringComparison.OrdinalIgnoreCase))
            return null;

        if (!int.TryParse(fileName["i18n".Length..], out var chunkId))
            return null;

        var regex = new Regex("(?:this\\.)?_datas\\[(?<id>\\d+)\\]\\s*=", RegexOptions.Compiled);
        var maxId = chunkId * 1000;
        var content = File.ReadAllText(path, Encoding.UTF8);
        foreach (Match match in regex.Matches(content))
        {
            if (int.TryParse(match.Groups["id"].Value, out var currentId))
                maxId = Math.Max(maxId, currentId);
        }

        return new I18nFileInfo(chunkId, maxId);
    }

    private TextIdUsage AnalyzeTextIdUsage(int textId, string desiredText)
    {
        if (textId <= 0)
            return new TextIdUsage(false, false);

        var i18nTmpDirectory = _pathResolver.EnsureSpanishI18nTmpDirectory();
        var chunkId = textId / 1000;
        var tmpPath = Path.Combine(i18nTmpDirectory, $"i18n{chunkId}.as");
        if (!File.Exists(tmpPath))
            return new TextIdUsage(false, false);

        var content = File.ReadAllText(tmpPath, Encoding.UTF8);
        var regex = new Regex($@"(?:this\.)?_datas\[{textId}\]\s*=\s*""(?<text>(?:\\.|[^""])*)"";", RegexOptions.Compiled);
        var matches = regex.Matches(content);
        if (matches.Count == 0)
            return new TextIdUsage(false, false);

        var lastText = matches[^1].Groups["text"].Value
            .Replace("\\\"", "\"")
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t")
            .Replace("\\\\", "\\");

        var matchesDesired = string.Equals(lastText.Trim(), desiredText.Trim(), StringComparison.Ordinal);
        var hasDuplicates = matches.Count > 1;
        return new TextIdUsage(matchesDesired && !hasDuplicates, hasDuplicates);
    }

    private static string BuildSetScriptLine(RuntimeSetData runtime, int nameId)
    {
        var itemIds = runtime.ItemIds.Length == 0
            ? string.Empty
            : string.Join(",", runtime.ItemIds.Select(itemId => itemId.ToString()));
        return $"         _datas[{runtime.Id}] = ItemSet.create({runtime.Id},{nameId},Vector.<uint>([{itemIds}]));";
    }

    private static int[] ParseItemsCsv(string itemsCsv) =>
        itemsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var itemId) ? itemId : 0)
            .Where(itemId => itemId > 0)
            .ToArray();

    private static string EscapeStringLiteral(string value) =>
        (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");

    private static int GetDataId(string line)
    {
        var match = DataEntryRegexTemplate.Match(line);
        if (!match.Success || !int.TryParse(match.Groups["id"].Value, out var id))
            throw new InvalidOperationException("No se pudo extraer el id de la linea _datas generada.");

        return id;
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

    private static string QuoteArgument(string value) =>
        value.Contains(' ') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\\\"")}\""
            : value;

    private sealed record RuntimeSetData
    {
        public short Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public int[] ItemIds { get; init; } = Array.Empty<int>();

        public int TierCount { get; init; }
    }

    private sealed record I18nFileInfo(int ChunkId, int MaxId);

    private sealed record TextIdUsage(bool IsReusable, bool ShouldCleanupDuplicate);
}

public sealed record SetClientPublishResult(string Summary, IReadOnlyList<string> Warnings)
{
    public bool HasWarnings => Warnings.Count > 0;
}
