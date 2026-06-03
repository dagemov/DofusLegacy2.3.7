using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;

namespace Rollback.Admin.Services;

public sealed class NpcClientPublishService
{
    private static readonly Regex DataFieldVisibilityRegex = new(
        @"protected\s+var\s+_datas:Array\s*=\s*new Array\(\);",
        RegexOptions.Compiled);

    private static readonly IReadOnlyDictionary<short, IReadOnlyDictionary<int, string>> AdditionalNpcTextEntries =
        new Dictionary<short, IReadOnlyDictionary<int, string>>
        {
            [1248] = new Dictionary<int, string>
            {
                [49630] = "Mazmorras 1-50 (1/2). Elige una ruta sana o ya sembrada para progresar sin llave.",
                [49631] = "Entrar a Incarnam",
                [49632] = "Entrar a Jalatos",
                [49633] = "Entrar a Campos",
                [49634] = "Entrar a Larvas",
                [49635] = "Entrar a Escarahojas",
                [49636] = "Entrar a Mob la Esponja",
                [49638] = "Pagina siguiente",
                [49652] = "Pagina anterior",
                [49653] = "Volver",
                [49654] = "Mazmorras 1-50 (2/2). Aqui quedan las rutas que siguen activas en esta fase.",
            },
            [1249] = new Dictionary<int, string>
            {
                [49640] = "Mazmorras 50-100. Solo se muestran rutas con base auditada o ya sembradas en esta build.",
                [49641] = "Entrar a Gelatinas",
                [49642] = "Entrar a Wey Wabbit",
                [49643] = "Entrar a Mazmorra del Mega Minilobu",
                [49644] = "Entrar a Maestro Cuervo",
                [49647] = "Entrar a Rasgabola",
                [49655] = "Volver",
            },
            [1980] = new Dictionary<int, string>
            {
                [49650] = "Quieres volver al hub de shops?",
                [49651] = "Salir",
            },
            [1981] = new Dictionary<int, string>
            {
                [49650] = "Quieres volver al hub de shops?",
                [49651] = "Salir",
            },
            [1982] = new Dictionary<int, string>
            {
                [49650] = "Quieres volver al hub de shops?",
                [49651] = "Salir",
            },
        };

    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly AdminEntityClientMetadataService _clientMetadataService;
    private readonly ClientDataPathResolver _pathResolver;

    public NpcClientPublishService(
        AdminDbConnectionFactory connectionFactory,
        AdminEntityClientMetadataService clientMetadataService,
        ClientDataPathResolver pathResolver)
    {
        _connectionFactory = connectionFactory;
        _clientMetadataService = clientMetadataService;
        _pathResolver = pathResolver;
    }

    public async Task<NpcClientPublishResult> PublishAsync(short npcId, CancellationToken cancellationToken = default)
    {
        var runtime = await LoadRuntimeNpcAsync(npcId, cancellationToken)
            ?? throw new InvalidOperationException($"El NPC #{npcId} no existe en runtime.");

        var displayName = string.IsNullOrWhiteSpace(runtime.Name)
            ? $"NPC #{npcId}"
            : runtime.Name.Trim();

        var persistedMetadata = await _clientMetadataService.GetAsync(AdminEntityType.Npc, npcId, cancellationToken: cancellationToken);
        var nameId = persistedMetadata?.NameId is > 0
            ? persistedMetadata.NameId
            : AllocateNewTextId();

        var backupDirectory = CreateBackupDirectory(npcId);
        await PublishTextAsync(nameId, displayName, backupDirectory, cancellationToken);
        await PublishAdditionalTextsAsync(runtime.Id, backupDirectory, cancellationToken);
        await PublishNpcDefinitionAsync(runtime, nameId, backupDirectory, cancellationToken);

        await _clientMetadataService.SaveAsync(
            AdminEntityType.Npc,
            npcId,
            nameId,
            0,
            0,
            cancellationToken: cancellationToken);

        return new NpcClientPublishResult(
            $"NPC cliente publicado para #{npcId} con NameId {nameId}.",
            displayName);
    }

    private async Task<RuntimeNpcData?> LoadRuntimeNpcAsync(short npcId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, MessagesCSV, RepliesCSV, ActionsCSV
            FROM npcs_templates
            WHERE Id = @npcId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@npcId", npcId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new RuntimeNpcData(
            reader.GetSafeInt16("Id"),
            reader.GetSafeString("Name"),
            reader.GetSafeString("MessagesCSV"),
            reader.GetSafeString("RepliesCSV"),
            reader.GetSafeString("ActionsCSV"));
    }

    private async Task PublishAdditionalTextsAsync(
        short npcId,
        string backupDirectory,
        CancellationToken cancellationToken)
    {
        if (!AdditionalNpcTextEntries.TryGetValue(npcId, out var entries))
            return;

        foreach (var entry in entries.OrderBy(x => x.Key))
            await PublishTextAsync(entry.Key, entry.Value, backupDirectory, cancellationToken);
    }

    private async Task PublishTextAsync(
        int textId,
        string text,
        string backupDirectory,
        CancellationToken cancellationToken)
    {
        if (textId <= 0)
            throw new InvalidOperationException("No se pudo resolver un NameId valido para publicar el NPC en i18n.");

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

    private async Task PublishNpcDefinitionAsync(
        RuntimeNpcData runtime,
        int nameId,
        string backupDirectory,
        CancellationToken cancellationToken)
    {
        var commonDirectory = _pathResolver.EnsureCommonDataDirectory();
        var chunkId = runtime.Id / 1000;
        var swfPath = Path.Combine(commonDirectory, $"Npcs{chunkId}.swf");
        if (!File.Exists(swfPath))
            throw new InvalidOperationException($"No existe Npcs{chunkId}.swf para publicar el NPC #{runtime.Id}.");

        await PatchSwfClassAsync(
            swfPath,
            $"Npcs{chunkId}",
            script => UpsertNpcEntry(script, BuildNpcScriptLine(runtime, nameId)),
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

    private string CreateBackupDirectory(short npcId)
    {
        var repoRoot = _pathResolver.EnsureRepoRoot();
        var backupDirectory = Path.Combine(
            repoRoot,
            "runtime",
            "client-state-backups",
            "npc-client-publish",
            $"{npcId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}");
        Directory.CreateDirectory(backupDirectory);
        return backupDirectory;
    }

    private static string BuildArguments(IEnumerable<string> arguments) =>
        string.Join(" ", arguments.Select(QuoteArgument));

    private static string QuoteArgument(string value) =>
        value.Contains(' ') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\\\"")}\""
            : value;

    private static string UpsertTextEntry(string script, int textId, string text)
    {
        var line = $"         _datas[{textId}] = \"{EscapeStringLiteral(text)}\";";
        var entryRegex = BuildDataEntryRegex(textId);
        if (entryRegex.IsMatch(script))
            return entryRegex.Replace(script, line, 1);

        return InsertBeforeCreateClosingBrace(script, line);
    }

    private static string UpsertNpcEntry(string script, string npcLine)
    {
        var entryRegex = BuildNpcEntryRegex(GetDataId(npcLine));
        if (entryRegex.IsMatch(script))
            return entryRegex.Replace(script, npcLine, 1);

        return InsertBeforeCreateClosingBrace(script, npcLine);
    }

    private static Regex BuildDataEntryRegex(int id) =>
        new($@"^\s*(?:this\.)?_datas\[{id}\]\s*=.*(?:\r?\n)?", RegexOptions.Multiline);

    private static Regex BuildNpcEntryRegex(int id) =>
        new(
            $@"^\s*(?:this\.)?_datas\[{id}\]\s*=\s*Npc\.create\(.*?\)\);\s*(?:\r?\n)?",
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

    private static string BuildNpcScriptLine(RuntimeNpcData runtime, int nameId)
    {
        var actionTokens = runtime.ActionsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => byte.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            .Select(token => byte.Parse(token, CultureInfo.InvariantCulture))
            .Distinct()
            .OrderBy(value => value)
            .ToArray();

        if (actionTokens.Length == 0)
            actionTokens = new byte[] { 1 };

        var actionsCsv = string.Join(",", actionTokens.Select(value => value.ToString(CultureInfo.InvariantCulture)));
        var messagesVector = BuildDialogVector(runtime.MessagesCsv);
        var repliesVector = BuildDialogVector(runtime.RepliesCsv);
        return $"         this._datas[{runtime.Id}] = Npc.create({runtime.Id},{nameId},{messagesVector},{repliesVector},Vector.<uint>([{actionsCsv}]));";
    }

    private static string BuildDialogVector(string csv)
    {
        var entries = ParseDialogEntries(csv)
            .Select(entry => $"{{\"id\":{entry.Id.ToString(CultureInfo.InvariantCulture)},\"textId\":{entry.TextId.ToString(CultureInfo.InvariantCulture)}}}")
            .ToArray();

        return entries.Length == 0
            ? "new Vector.<Object>(0,true)"
            : $"Vector.<Object>([{string.Join(",", entries)}])";
    }

    private static IReadOnlyList<DialogEntry> ParseDialogEntries(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return Array.Empty<DialogEntry>();

        var entries = new List<DialogEntry>();
        foreach (var rawEntry in csv.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tokens = rawEntry.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length != 2)
                continue;

            if (!short.TryParse(tokens[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) || id <= 0)
                continue;

            if (!int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var textId) || textId <= 0)
                continue;

            entries.Add(new DialogEntry(id, textId));
        }

        return entries;
    }

    private static string EscapeStringLiteral(string value) =>
        value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");

    private sealed record RuntimeNpcData(short Id, string Name, string MessagesCsv, string RepliesCsv, string ActionsCsv);
    private sealed record DialogEntry(short Id, int TextId);
    private sealed record I18nFileInfo(int ChunkId, int MaxId);
}

public sealed record NpcClientPublishResult(string Summary, string DisplayName);
