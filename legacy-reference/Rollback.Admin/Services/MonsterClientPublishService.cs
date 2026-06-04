using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;

namespace Rollback.Admin.Services;

public sealed class MonsterClientPublishService
{
    private static readonly Regex DataFieldVisibilityRegex = new(
        @"protected\s+var\s+_datas:Array\s*=\s*new Array\(\);",
        RegexOptions.Compiled);

    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly AdminEntityClientMetadataService _clientMetadataService;
    private readonly ClientDataPathResolver _pathResolver;

    public MonsterClientPublishService(
        AdminDbConnectionFactory connectionFactory,
        AdminEntityClientMetadataService clientMetadataService,
        ClientDataPathResolver pathResolver)
    {
        _connectionFactory = connectionFactory;
        _clientMetadataService = clientMetadataService;
        _pathResolver = pathResolver;
    }

    public async Task<MonsterClientPublishResult> PublishAsync(
        short monsterId,
        string displayName,
        short? sourceMonsterId = null,
        int? gfxIdOverride = null,
        byte? raceOverride = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new InvalidOperationException("El nombre cliente del monstruo no puede estar vacio.");

        var runtime = await LoadRuntimeMonsterAsync(monsterId, cancellationToken)
            ?? throw new InvalidOperationException($"El monstruo runtime #{monsterId} no existe.");

        var persistedMetadata = await _clientMetadataService.GetAsync(
            AdminEntityType.Monster,
            monsterId,
            cancellationToken: cancellationToken);

        var sourceMetadata = sourceMonsterId.HasValue
            ? LoadCatalogEntry(sourceMonsterId.Value)
            : null;

        var nameId = persistedMetadata?.NameId is > 0
            ? persistedMetadata.NameId
            : AllocateNewTextId();

        int? persistedGfxId = persistedMetadata?.AppearanceId is > 0
            ? persistedMetadata.AppearanceId
            : null;

        var gfxId = gfxIdOverride
            ?? persistedGfxId
            ?? sourceMetadata?.GfxId;

        if (gfxId is null or <= 0)
            throw new InvalidOperationException($"No se pudo resolver un GfxId cliente valido para el monstruo #{monsterId}.");

        var race = raceOverride ?? runtime.Race;
        var backupDirectory = CreateBackupDirectory(monsterId);

        await PublishTextAsync(nameId, displayName.Trim(), backupDirectory, cancellationToken);
        await PublishMonsterDefinitionAsync(runtime, nameId, gfxId.Value, race, backupDirectory, cancellationToken);
        await UpsertGeneratedCatalogAsync(monsterId, nameId, gfxId.Value, displayName.Trim(), cancellationToken);

        await _clientMetadataService.SaveAsync(
            AdminEntityType.Monster,
            monsterId,
            nameId,
            0,
            0,
            gfxId.Value,
            cancellationToken: cancellationToken);

        return new MonsterClientPublishResult(
            $"Monstruo cliente publicado para #{monsterId} con NameId {nameId} y GfxId {gfxId.Value}.",
            displayName.Trim(),
            nameId,
            gfxId.Value);
    }

    private async Task<RuntimeMonsterData?> LoadRuntimeMonsterAsync(short monsterId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var templateCommand = connection.CreateCommand();
        templateCommand.CommandText = """
            SELECT Id, Race, EntityLookString
            FROM monsters_templates
            WHERE Id = @monsterId
            LIMIT 1;
            """;
        templateCommand.Parameters.AddWithValue("@monsterId", monsterId);

        short id;
        byte race;
        string entityLookString;

        await using (var reader = await templateCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            id = reader.GetSafeInt16("Id");
            race = reader.GetSafeByte("Race");
            entityLookString = reader.GetSafeString("EntityLookString");
        }

        await using var gradesCommand = connection.CreateCommand();
        gradesCommand.CommandText = """
            SELECT Grade, Level, APDodge, MPDodge, EarthResistance, AirResistance, FireResistance, WaterResistance, NeutralResistance
            FROM monsters_grades
            WHERE MonsterId = @monsterId
            ORDER BY Grade;
            """;
        gradesCommand.Parameters.AddWithValue("@monsterId", monsterId);

        var grades = new List<RuntimeMonsterGradeData>();
        await using (var reader = await gradesCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                grades.Add(new RuntimeMonsterGradeData(
                    reader.GetSafeSByte("Grade"),
                    reader.GetSafeInt16("Level"),
                    reader.GetSafeInt16("APDodge"),
                    reader.GetSafeInt16("MPDodge"),
                    reader.GetSafeInt16("EarthResistance"),
                    reader.GetSafeInt16("AirResistance"),
                    reader.GetSafeInt16("FireResistance"),
                    reader.GetSafeInt16("WaterResistance"),
                    reader.GetSafeInt16("NeutralResistance")));
            }
        }

        if (grades.Count == 0)
            throw new InvalidOperationException($"El monstruo #{monsterId} no tiene grades runtime para publicar en cliente.");

        return new RuntimeMonsterData(id, race, entityLookString, grades);
    }

    private async Task PublishMonsterDefinitionAsync(
        RuntimeMonsterData runtime,
        int nameId,
        int gfxId,
        byte race,
        string backupDirectory,
        CancellationToken cancellationToken)
    {
        var commonDirectory = _pathResolver.EnsureCommonDataDirectory();
        var chunkId = runtime.Id / 1000;
        var swfPath = Path.Combine(commonDirectory, $"Monsters{chunkId}.swf");
        if (!File.Exists(swfPath))
            throw new InvalidOperationException($"No existe Monsters{chunkId}.swf para publicar el monstruo #{runtime.Id}.");

        await PatchSwfClassAsync(
            swfPath,
            $"Monsters{chunkId}",
            script => UpsertMonsterEntry(script, BuildMonsterScriptLine(runtime, nameId, gfxId, race)),
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
            throw new InvalidOperationException("No se pudo resolver un NameId valido para publicar el monstruo en i18n.");

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
            RunFfdec("-selectclass", className, "-export", "script", exportDirectory, swfPath);

            var scriptsDirectory = Path.Combine(exportDirectory, "scripts");
            var scriptPath = Path.Combine(scriptsDirectory, $"{className}.as");
            if (!File.Exists(scriptPath))
                throw new InvalidOperationException($"FFDec no exporto {className}.as desde {Path.GetFileName(swfPath)}.");

            var script = await File.ReadAllTextAsync(scriptPath, Encoding.UTF8, cancellationToken);
            var transformed = EnsureImportableScript(transform(script));
            await File.WriteAllTextAsync(scriptPath, transformed, Encoding.UTF8, cancellationToken);

            var patchedPath = Path.Combine(workspace, Path.GetFileName(swfPath));
            RunFfdec("-importScript", swfPath, patchedPath, scriptsDirectory);

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

    private void RunFfdec(params string[] arguments)
    {
        var ffdecPath = _pathResolver.EnsureFfdecCliPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = ffdecPath,
            Arguments = BuildArguments(arguments),
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

        var candidate = fileInfos.FirstOrDefault(info => info.MaxId <= info.ChunkId * 1000 + 999);
        if (candidate is null)
            throw new InvalidOperationException("No se encontro un archivo i18n con espacio para asignar NameId nuevo.");

        return candidate.MaxId + 1;
    }

    private I18nFileInfo? LoadI18nFileInfo(string path)
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

    private async Task UpsertGeneratedCatalogAsync(
        short monsterId,
        int nameId,
        int gfxId,
        string displayName,
        CancellationToken cancellationToken)
    {
        var commonDirectory = _pathResolver.EnsureCommonDataDirectory();
        var catalogPath = Path.Combine(commonDirectory, "monster-client-map.generated.json");
        List<MonsterCatalogEntry> entries;

        if (File.Exists(catalogPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(catalogPath, cancellationToken);
                entries = JsonSerializer.Deserialize<List<MonsterCatalogEntry>>(json) ?? new();
            }
            catch
            {
                entries = new List<MonsterCatalogEntry>();
            }
        }
        else
        {
            entries = new List<MonsterCatalogEntry>();
        }

        var existing = entries.FirstOrDefault(entry => entry.MonsterId == monsterId);
        if (existing is not null)
        {
            existing.NameId = nameId;
            existing.GfxId = gfxId;
            existing.DisplayName = displayName;
        }
        else
        {
            entries.Add(new MonsterCatalogEntry
            {
                MonsterId = monsterId,
                NameId = nameId,
                GfxId = gfxId,
                DisplayName = displayName,
            });
        }

        entries = entries
            .Where(entry => entry.MonsterId > 0)
            .OrderBy(entry => entry.MonsterId)
            .ToList();

        var output = JsonSerializer.Serialize(entries, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        await File.WriteAllTextAsync(catalogPath, output, Encoding.UTF8, cancellationToken);
    }

    private MonsterCatalogEntry? LoadCatalogEntry(short monsterId)
    {
        var commonDirectory = _pathResolver.EnsureCommonDataDirectory();
        var catalogPath = Path.Combine(commonDirectory, "monster-client-map.generated.json");
        if (!File.Exists(catalogPath))
            return null;

        try
        {
            var json = File.ReadAllText(catalogPath, Encoding.UTF8);
            var entries = JsonSerializer.Deserialize<List<MonsterCatalogEntry>>(json) ?? new();
            return entries.FirstOrDefault(entry => entry.MonsterId == monsterId);
        }
        catch
        {
            return null;
        }
    }

    private string CreateBackupDirectory(short monsterId)
    {
        var repoRoot = _pathResolver.EnsureRepoRoot();
        var backupDirectory = Path.Combine(
            repoRoot,
            "runtime",
            "client-state-backups",
            "monster-client-publish",
            $"{monsterId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}");
        Directory.CreateDirectory(backupDirectory);
        return backupDirectory;
    }

    private static string BuildMonsterScriptLine(RuntimeMonsterData runtime, int nameId, int gfxId, byte race)
    {
        var gradeVector = string.Join(
            ",",
            runtime.Grades
                .OrderBy(grade => grade.Grade)
                .Select(grade =>
                    $"MonsterGrade.create({grade.Grade.ToString(CultureInfo.InvariantCulture)},{runtime.Id.ToString(CultureInfo.InvariantCulture)},{grade.Level.ToString(CultureInfo.InvariantCulture)},{grade.APDodge.ToString(CultureInfo.InvariantCulture)},{grade.MPDodge.ToString(CultureInfo.InvariantCulture)},{grade.EarthResistance.ToString(CultureInfo.InvariantCulture)},{grade.AirResistance.ToString(CultureInfo.InvariantCulture)},{grade.FireResistance.ToString(CultureInfo.InvariantCulture)},{grade.WaterResistance.ToString(CultureInfo.InvariantCulture)},{grade.NeutralResistance.ToString(CultureInfo.InvariantCulture)})"));

        return $"         this._datas[{runtime.Id}] = Monster.create({runtime.Id},{nameId.ToString(CultureInfo.InvariantCulture)},{gfxId.ToString(CultureInfo.InvariantCulture)},{race.ToString(CultureInfo.InvariantCulture)},Vector.<MonsterGrade>([{gradeVector}]));";
    }

    private static string UpsertMonsterEntry(string script, string monsterLine)
    {
        var entryRegex = BuildMonsterEntryRegex(GetDataId(monsterLine));
        if (entryRegex.IsMatch(script))
            return entryRegex.Replace(script, monsterLine, 1);

        return InsertBeforeCreateClosingBrace(script, monsterLine);
    }

    private static string UpsertTextEntry(string script, int textId, string text)
    {
        var line = $"         _datas[{textId}] = \"{EscapeStringLiteral(text)}\";";
        var entryRegex = new Regex($@"^\s*(?:this\.)?_datas\[{textId}\]\s*=.*(?:\r?\n)?", RegexOptions.Multiline);
        if (entryRegex.IsMatch(script))
            return entryRegex.Replace(script, line, 1);

        return InsertBeforeCreateClosingBrace(script, line);
    }

    private static Regex BuildMonsterEntryRegex(int id) =>
        new(
            $@"^\s*(?:this\.)?_datas\[{id}\]\s*=\s*Monster\.create\(.*?\)\);\s*(?:\r?\n)?",
            RegexOptions.Multiline | RegexOptions.Singleline);

    private static int GetDataId(string line)
    {
        var match = Regex.Match(line, @"_datas\[(?<id>\d+)\]");
        if (!match.Success || !int.TryParse(match.Groups["id"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            throw new InvalidOperationException("No se pudo extraer el id _datas del script generado.");

        return id;
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

    private static string EnsureImportableScript(string script)
    {
        if (DataFieldVisibilityRegex.IsMatch(script))
            return DataFieldVisibilityRegex.Replace(script, "public var _datas:Array = new Array();", 1);

        return script;
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

    private static string BuildArguments(IEnumerable<string> arguments) =>
        string.Join(" ", arguments.Select(QuoteArgument));

    private static string QuoteArgument(string value) =>
        value.Contains(' ') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\\\"")}\""
            : value;

    private static string EscapeStringLiteral(string value) =>
        value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");

    private sealed record RuntimeMonsterData(short Id, byte Race, string EntityLookString, IReadOnlyList<RuntimeMonsterGradeData> Grades);
    private sealed record RuntimeMonsterGradeData(
        sbyte Grade,
        short Level,
        short APDodge,
        short MPDodge,
        short EarthResistance,
        short AirResistance,
        short FireResistance,
        short WaterResistance,
        short NeutralResistance);

    private sealed record I18nFileInfo(int ChunkId, int MaxId);

    private sealed class MonsterCatalogEntry
    {
        public short MonsterId { get; set; }
        public int NameId { get; set; }
        public int GfxId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }
}

public sealed record MonsterClientPublishResult(string Summary, string DisplayName, int NameId, int GfxId);
