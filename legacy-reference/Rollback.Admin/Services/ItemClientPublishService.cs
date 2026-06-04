using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.Items;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class ItemClientPublishService
{
    private static readonly Regex DataEntryRegexTemplate = new(
        @"^\s*_datas\[(?<id>\d+)\]\s*=.*;$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex DataFieldVisibilityRegex = new(
        @"protected\s+var\s+_datas:Array\s*=\s*new Array\(\);",
        RegexOptions.Compiled);

    private static readonly HashSet<ItemType> WeaponTypes = new()
    {
        ItemType.Arc,
        ItemType.Baguette,
        ItemType.Baton,
        ItemType.Dague,
        ItemType.Epee,
        ItemType.Marteau,
        ItemType.Pelle,
        ItemType.Hache,
        ItemType.Outil,
        ItemType.Pioche,
        ItemType.Faux,
        ItemType.Arbalete,
        ItemType.ArmeMagique,
    };

    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly ItemIdentityDiagnosticService _diagnosticService;
    private readonly AdminEntityClientMetadataService _clientMetadataService;
    private readonly ClientDataPathResolver _pathResolver;

    public ItemClientPublishService(
        AdminDbConnectionFactory connectionFactory,
        ItemIdentityDiagnosticService diagnosticService,
        AdminEntityClientMetadataService clientMetadataService,
        ClientDataPathResolver pathResolver)
    {
        _connectionFactory = connectionFactory;
        _diagnosticService = diagnosticService;
        _clientMetadataService = clientMetadataService;
        _pathResolver = pathResolver;
    }

    public async Task<ItemClientPublishResult> PublishAsync(short itemId, CancellationToken cancellationToken = default)
    {
        var runtime = await LoadRuntimeTemplateAsync(itemId, cancellationToken)
            ?? throw new InvalidOperationException($"El item #{itemId} no existe en runtime.");

        var report = await _diagnosticService.DiagnoseAsync(itemId, cancellationToken);
        var warnings = new List<string>();
        var backupDirectory = CreateBackupDirectory(itemId);

        var commonDirectory = _pathResolver.EnsureCommonDataDirectory();
        var itemChunk = (short)(itemId / 1000);
        var itemSwfPath = Path.Combine(commonDirectory, $"Items{itemChunk}.swf");
        if (!File.Exists(itemSwfPath))
        {
            throw new InvalidOperationException(
                $"El cliente actual no trae Items{itemChunk}.swf. El Id #{itemId} cae fuera del dominio de chunks soportado por esta build y no puede publicarse todavia.");
        }

        var persistedMetadata = await _clientMetadataService.GetAsync(AdminEntityType.Item, itemId, cancellationToken: cancellationToken);
        var publishedMetadata = ResolvePublishedMetadata(runtime, report, persistedMetadata, warnings);

        foreach (var staleTextId in publishedMetadata.StaleDuplicateTextIds.Where(id => id > 0).Distinct())
            await RemoveTextEntryAsync(staleTextId, backupDirectory, cancellationToken);

        await PublishTextAsync(publishedMetadata.NameId, publishedMetadata.NameText, backupDirectory, cancellationToken);
        await PublishTextAsync(publishedMetadata.DescriptionId, publishedMetadata.DescriptionText, backupDirectory, cancellationToken);
        await EnsureBitmapAsync(report, publishedMetadata.IconId, backupDirectory, warnings, cancellationToken);
        await PublishItemDefinitionAsync(runtime, publishedMetadata, backupDirectory, cancellationToken);

        await _clientMetadataService.SaveAsync(
            AdminEntityType.Item,
            itemId,
            publishedMetadata.NameId,
            publishedMetadata.DescriptionId,
            publishedMetadata.IconId,
            NormalizeAppearanceId(runtime.AppearanceId),
            cancellationToken: cancellationToken);

        ClientI18nTextService.InvalidateCache();
        ClientItemMetadataService.RegisterOrUpdate(new AdminClientItemMetadata
        {
            ItemId = itemId,
            TypeId = runtime.TypeId,
            NameId = publishedMetadata.NameId,
            DescriptionId = publishedMetadata.DescriptionId,
            IconId = publishedMetadata.IconId,
            AppearanceId = NormalizeAppearanceId(runtime.AppearanceId),
        });

        return new ItemClientPublishResult
        {
            Summary = $"Definicion cliente publicada para el item #{itemId} en Items{itemChunk}.swf con NameId {publishedMetadata.NameId}, DescriptionId {publishedMetadata.DescriptionId} e IconId {publishedMetadata.IconId}.",
            Warnings = warnings,
        };
    }

    private async Task<RuntimeItemTemplateData?> LoadRuntimeTemplateAsync(short itemId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                Id,
                TypeId,
                Level,
                Weight,
                Usable,
                Targetable,
                Etheral,
                Price,
                ItemSetId,
                StringCriterion,
                AppearanceId,
                RecipesCSV,
                TwoHanded,
                APCost,
                MinRange,
                MaxRange,
                CastInLine,
                CastTestLOS,
                CriticalHitProbability,
                CriticalHitBonus,
                CriticalFailureProbability
            FROM items_templates
            WHERE Id = @itemId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@itemId", itemId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new RuntimeItemTemplateData
        {
            Id = reader.GetSafeInt16("Id"),
            TypeId = (ItemType)reader.GetSafeInt16("TypeId"),
            Level = reader.GetSafeInt16("Level"),
            Weight = reader.GetSafeInt32("Weight"),
            Usable = reader.GetSafeBoolean("Usable"),
            Targetable = reader.GetSafeBoolean("Targetable"),
            Etheral = reader.GetSafeBoolean("Etheral"),
            Price = reader.GetSafeInt32("Price"),
            ItemSetId = reader.GetSafeInt16("ItemSetId", -1),
            StringCriterion = reader.GetSafeString("StringCriterion"),
            AppearanceId = reader.GetSafeInt16("AppearanceId", -1),
            RecipesCsv = reader.GetSafeString("RecipesCSV"),
            TwoHanded = reader.GetSafeBoolean("TwoHanded"),
            APCost = reader.GetSafeInt16("APCost"),
            MinRange = reader.GetSafeSByte("MinRange"),
            MaxRange = reader.GetSafeSByte("MaxRange"),
            CastInLine = reader.GetSafeBoolean("CastInLine"),
            CastTestLOS = reader.GetSafeBoolean("CastTestLOS"),
            CriticalHitProbability = reader.GetSafeSByte("CriticalHitProbability"),
            CriticalHitBonus = reader.GetSafeSByte("CriticalHitBonus"),
            CriticalFailureProbability = reader.GetSafeSByte("CriticalFailureProbability"),
        };
    }

    private PublishedClientMetadata ResolvePublishedMetadata(
        RuntimeItemTemplateData runtime,
        ItemDiagnosticReport report,
        AdminEntityClientMetadata? persistedMetadata,
        List<string> warnings)
    {
        var nameText = ResolveVisibleName(report);
        var descriptionText = ResolveVisibleDescription(report, runtime);

        var textResolution = ResolveTextIds(persistedMetadata, report, nameText, descriptionText, warnings);
        var iconId = ResolveIconId(persistedMetadata, report, warnings);

        if (iconId <= 0)
        {
            throw new InvalidOperationException(
                $"El item #{runtime.Id} no tiene un IconId reutilizable ni un PNG manual exportable al cliente. Sube un PNG manual o reutiliza un icono cliente valido antes de publicar.");
        }

        return new PublishedClientMetadata
        {
            NameId = textResolution.NameId,
            DescriptionId = textResolution.DescriptionId,
            IconId = iconId,
            NameText = nameText,
            DescriptionText = descriptionText,
            StaleDuplicateTextIds = textResolution.StaleDuplicateTextIds,
        };
    }

    private TextIdResolution ResolveTextIds(
        AdminEntityClientMetadata? persistedMetadata,
        ItemDiagnosticReport report,
        string desiredName,
        string desiredDescription,
        List<string> warnings)
    {
        if (persistedMetadata is { NameId: > 0, DescriptionId: > 0 })
        {
            var nameUsage = AnalyzeTextIdUsage(persistedMetadata.NameId, desiredName);
            var descriptionUsage = AnalyzeTextIdUsage(persistedMetadata.DescriptionId, desiredDescription);
            if (nameUsage.IsReusable && descriptionUsage.IsReusable)
            {
                return new TextIdResolution(
                    persistedMetadata.NameId,
                    persistedMetadata.DescriptionId,
                    Array.Empty<int>());
            }

            var staleDuplicates = new List<int>();
            if (nameUsage.ShouldCleanupDuplicate)
                staleDuplicates.Add(persistedMetadata.NameId);
            if (descriptionUsage.ShouldCleanupDuplicate)
                staleDuplicates.Add(persistedMetadata.DescriptionId);

            warnings.Add("Los TextId previamente asignados por admin no eran seguros para reutilizar. Se reasignaran ids limpios.");
            var reallocated = AllocateNewTextIds();
            return new TextIdResolution(reallocated.NameId, reallocated.DescriptionId, staleDuplicates);
        }

        if (report.Client.NameId is > 0 && report.Client.DescriptionId is > 0)
        {
            var nameUsage = AnalyzeTextIdUsage(report.Client.NameId.Value, desiredName);
            var descriptionUsage = AnalyzeTextIdUsage(report.Client.DescriptionId.Value, desiredDescription);
            if (nameUsage.IsReusable && descriptionUsage.IsReusable)
            {
                return new TextIdResolution(
                    report.Client.NameId.Value,
                    report.Client.DescriptionId.Value,
                    Array.Empty<int>());
            }
        }

        var allocated = AllocateNewTextIds();
        return new TextIdResolution(allocated.NameId, allocated.DescriptionId, Array.Empty<int>());
    }

    private int ResolveIconId(
        AdminEntityClientMetadata? persistedMetadata,
        ItemDiagnosticReport report,
        List<string> warnings)
    {
        if (persistedMetadata?.IconId > 0)
            return persistedMetadata.IconId;

        if (report.Client.IconId is > 0)
            return report.Client.IconId ?? 0;

        if (report.Reference?.IconId is > 0 && BitmapExists(report.Reference.IconId))
            return report.Reference.IconId;

        if (!string.IsNullOrWhiteSpace(report.ManualAssetRelativePath))
        {
            warnings.Add("Se asignara un IconId nuevo a partir del PNG manual del panel para publicar el item en el cliente.");
            return AllocateNextBitmapId();
        }

        return 0;
    }

    private async Task PublishTextAsync(
        int textId,
        string text,
        string backupDirectory,
        CancellationToken cancellationToken)
    {
        if (textId <= 0)
            throw new InvalidOperationException("No se pudo resolver un TextId valido para publicar en i18n.");

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

    private async Task RemoveTextEntryAsync(int textId, string backupDirectory, CancellationToken cancellationToken)
    {
        if (textId <= 0)
            return;

        var i18nDirectory = _pathResolver.EnsureSpanishI18nDirectory();
        var i18nTmpDirectory = _pathResolver.EnsureSpanishI18nTmpDirectory();
        var chunkId = textId / 1000;
        var swfPath = Path.Combine(i18nDirectory, $"i18n{chunkId}.swf");
        if (!File.Exists(swfPath))
            return;

        var updatedScript = await PatchSwfClassAsync(
            swfPath,
            $"i18n{chunkId}",
            script => RemoveTextEntry(script, textId),
            backupDirectory,
            cancellationToken);

        var tmpPath = Path.Combine(i18nTmpDirectory, $"i18n{chunkId}.as");
        BackupFileIfNeeded(tmpPath, backupDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(tmpPath)!);
        await File.WriteAllTextAsync(tmpPath, updatedScript, Encoding.UTF8, cancellationToken);
    }

    private async Task EnsureBitmapAsync(
        ItemDiagnosticReport report,
        int iconId,
        string backupDirectory,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        if (iconId <= 0)
            return;

        var bitmapDirectory = _pathResolver.EnsureItemBitmapDirectory();
        var targetPath = Path.Combine(bitmapDirectory, $"{iconId}.png");
        if (File.Exists(targetPath))
            return;

        if (string.IsNullOrWhiteSpace(report.ManualAssetRelativePath))
            throw new InvalidOperationException($"No existe {iconId}.png en el cliente y el item no tiene PNG manual para publicarlo.");

        var sourcePath = ResolveManualAssetAbsolutePath(report.ManualAssetRelativePath);
        if (!File.Exists(sourcePath))
            throw new InvalidOperationException($"No se encontro el PNG manual {report.ManualAssetRelativePath} para publicarlo en el cliente.");

        if (!string.Equals(Path.GetExtension(sourcePath), ".png", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El cliente solo acepta PNG para el bitmap real del item. Sube el asset manual como .png antes de publicar.");

        BackupFileIfNeeded(targetPath, backupDirectory);
        await using var input = File.OpenRead(sourcePath);
        await using var output = File.Create(targetPath);
        await input.CopyToAsync(output, cancellationToken);
        warnings.Add($"Se copio {Path.GetFileName(sourcePath)} como {iconId}.png al pack local del cliente.");
    }

    private async Task PublishItemDefinitionAsync(
        RuntimeItemTemplateData runtime,
        PublishedClientMetadata metadata,
        string backupDirectory,
        CancellationToken cancellationToken)
    {
        var commonDirectory = _pathResolver.EnsureCommonDataDirectory();
        var chunkId = runtime.Id / 1000;
        var swfPath = Path.Combine(commonDirectory, $"Items{chunkId}.swf");
        if (!File.Exists(swfPath))
            throw new InvalidOperationException($"No existe Items{chunkId}.swf para publicar el item #{runtime.Id}.");

        await PatchSwfClassAsync(
            swfPath,
            $"Items{chunkId}",
            script => UpsertItemEntry(script, BuildItemScriptLine(runtime, metadata)),
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
                // Keep leftovers only if cleanup fails.
            }
        }
    }

    private static string UpsertItemEntry(string script, string itemLine)
    {
        var entryRegex = BuildDataEntryRegex(GetDataId(itemLine));
        if (entryRegex.IsMatch(script))
            return entryRegex.Replace(script, itemLine, 1);

        return InsertBeforeCreateClosingBrace(script, itemLine);
    }

    private static string UpsertTextEntry(string script, int textId, string text)
    {
        var line = $"         _datas[{textId}] = \"{EscapeStringLiteral(text)}\";";
        var entryRegex = BuildDataEntryRegex(textId);
        if (entryRegex.IsMatch(script))
            return entryRegex.Replace(script, line, 1);

        return InsertBeforeCreateClosingBrace(script, line);
    }

    private static string RemoveTextEntry(string script, int textId)
    {
        var entryRegex = BuildDataEntryRegex(textId);
        return entryRegex.Replace(script, string.Empty);
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

    private string CreateBackupDirectory(short itemId)
    {
        var repoRoot = _pathResolver.EnsureRepoRoot();
        var backupDirectory = Path.Combine(
            repoRoot,
            "runtime",
            "client-state-backups",
            "item-client-publish",
            $"{itemId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}");
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

    private string ResolveManualAssetAbsolutePath(string relativePath)
    {
        var assetsRoot = _pathResolver.EnsureWebAdminAssetsRootDirectory();
        return Path.GetFullPath(Path.Combine(assetsRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private (int NameId, int DescriptionId) AllocateNewTextIds()
    {
        var tmpDirectory = _pathResolver.EnsureSpanishI18nTmpDirectory();
        var fileInfos = Directory
            .EnumerateFiles(tmpDirectory, "i18n*.as", SearchOption.TopDirectoryOnly)
            .Select(path => LoadI18nFileInfo(path))
            .Where(info => info is not null)
            .Cast<I18nFileInfo>()
            .OrderByDescending(info => info.ChunkId)
            .ToArray();

        var candidate = fileInfos.FirstOrDefault(info => info.MaxId <= info.ChunkId * 1000 + 997);
        if (candidate is null)
            throw new InvalidOperationException("No se encontro un archivo i18n con espacio para asignar NameId y DescriptionId nuevos.");

        return (candidate.MaxId + 1, candidate.MaxId + 2);
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

    private int AllocateNextBitmapId()
    {
        var bitmapDirectory = _pathResolver.EnsureItemBitmapDirectory();
        var maxId = 0;
        foreach (var file in Directory.EnumerateFiles(bitmapDirectory, "*.png", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (int.TryParse(fileName, out var currentId))
                maxId = Math.Max(maxId, currentId);
        }

        return maxId + 1;
    }

    private bool BitmapExists(int iconId)
    {
        var bitmapDirectory = _pathResolver.EnsureItemBitmapDirectory();
        return File.Exists(Path.Combine(bitmapDirectory, $"{iconId}.png"));
    }

    private static string ResolveVisibleName(ItemDiagnosticReport report)
    {
        var name = (report.OverrideName ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        name = (report.Client.Name ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        name = (report.Reference?.Name ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        return $"Item #{report.ItemId}";
    }

    private static string ResolveVisibleDescription(ItemDiagnosticReport report, RuntimeItemTemplateData runtime)
    {
        var description = (report.OverrideDescription ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(description))
            return description;

        description = (report.Client.Description ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(description))
            return description;

        description = (report.Reference?.Description ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(description))
            return description;

        return $"Template runtime del item #{runtime.Id} ({ItemTypeLabelService.GetDisplayName(runtime.TypeId)}).";
    }

    private static string BuildItemScriptLine(RuntimeItemTemplateData runtime, PublishedClientMetadata metadata)
    {
        var appearanceId = NormalizeAppearanceId(runtime.AppearanceId);
        var itemSetId = runtime.ItemSetId > 0 ? runtime.ItemSetId : (short)-1;
        var criteria = EscapeStringLiteral(runtime.StringCriterion ?? string.Empty);
        var recipesLiteral = BuildRecipesLiteral(runtime.RecipesCsv);
        var common = string.Join(",",
            runtime.Id.ToString(),
            metadata.NameId.ToString(),
            ((short)runtime.TypeId).ToString(),
            metadata.DescriptionId.ToString(),
            metadata.IconId.ToString(),
            runtime.Level.ToString(),
            runtime.Weight.ToString(),
            "false",
            "-1",
            runtime.Usable ? "true" : "false",
            runtime.Targetable ? "true" : "false",
            runtime.Price.ToString(),
            runtime.TwoHanded ? "true" : "false",
            runtime.Etheral ? "true" : "false",
            itemSetId.ToString(),
            $"\"{criteria}\"",
            appearanceId.ToString(),
            recipesLiteral);

        return WeaponTypes.Contains(runtime.TypeId)
            ? $"         _datas[{runtime.Id}] = Weapon.createWeapon({common},{runtime.APCost},{runtime.MinRange},{runtime.MaxRange},{(runtime.CastInLine ? "true" : "false")},{(runtime.CastTestLOS ? "true" : "false")},{runtime.CriticalHitProbability},{runtime.CriticalHitBonus},{runtime.CriticalFailureProbability});"
            : $"         _datas[{runtime.Id}] = Item.create({common});";
    }

    private static string BuildRecipesLiteral(string? recipesCsv)
    {
        var recipeIds = (recipesCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => int.TryParse(value, out var parsed) && parsed > 0)
            .ToArray();

        return recipeIds.Length == 0
            ? "new Vector.<uint>(0,true)"
            : $"Vector.<uint>([{string.Join(",", recipeIds)}])";
    }

    private static short NormalizeAppearanceId(short appearanceId) =>
        appearanceId > 0 ? appearanceId : (short)0;

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

    private sealed record RuntimeItemTemplateData
    {
        public short Id { get; init; }

        public ItemType TypeId { get; init; }

        public short Level { get; init; }

        public int Weight { get; init; }

        public bool Usable { get; init; }

        public bool Targetable { get; init; }

        public bool Etheral { get; init; }

        public int Price { get; init; }

        public short ItemSetId { get; init; }

        public string StringCriterion { get; init; } = string.Empty;

        public short AppearanceId { get; init; }

        public string RecipesCsv { get; init; } = string.Empty;

        public bool TwoHanded { get; init; }

        public short APCost { get; init; }

        public sbyte MinRange { get; init; }

        public sbyte MaxRange { get; init; }

        public bool CastInLine { get; init; }

        public bool CastTestLOS { get; init; }

        public sbyte CriticalHitProbability { get; init; }

        public sbyte CriticalHitBonus { get; init; }

        public sbyte CriticalFailureProbability { get; init; }
    }

    private sealed record PublishedClientMetadata
    {
        public int NameId { get; init; }

        public int DescriptionId { get; init; }

        public int IconId { get; init; }

        public string NameText { get; init; } = string.Empty;

        public string DescriptionText { get; init; } = string.Empty;

        public IReadOnlyList<int> StaleDuplicateTextIds { get; init; } = Array.Empty<int>();
    }

    private sealed record I18nFileInfo(int ChunkId, int MaxId);

    private sealed record TextIdResolution(int NameId, int DescriptionId, IReadOnlyList<int> StaleDuplicateTextIds);

    private sealed record TextIdUsage(bool IsReusable, bool ShouldCleanupDuplicate);
}
