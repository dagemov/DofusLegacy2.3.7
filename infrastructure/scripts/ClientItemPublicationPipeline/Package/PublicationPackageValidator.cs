using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using ClientItemPublicationPipeline.D2i;
using D2oItem = Sunshine.Protocol.Tools.D2o.Classes.Item;

namespace ClientItemPublicationPipeline.Package;

internal sealed class PublicationPackageValidator
{
    public PublicationPackageValidationResult Validate(PublicationPackageValidationRequest request)
    {
        var blocking = new List<string>();
        var warnings = new List<string>();
        var checks = new List<string>();
        var packageDirectory = request.PackageDirectory;
        var targetItemId = request.ExpectedTargetItemId;

        var itemsPath = PublicationPackagePaths.ResolveItemsPath(packageDirectory);
        var esPath = PublicationPackagePaths.ResolveI18nEsPath(packageDirectory);
        var enPath = PublicationPackagePaths.ResolveI18nEnPath(packageDirectory);

        RequireFile(blocking, checks, itemsPath, "Items.d2o");
        RequireFile(blocking, checks, esPath, "i18n_es.d2i");
        RequireFile(blocking, checks, enPath, "i18n_en.d2i");

        D2oItem? item = null;
        if (blocking.Count == 0 && File.Exists(itemsPath))
        {
            try
            {
                var indexIds = ReadD2oIndexIds(itemsPath);
                if (!indexIds.Contains(targetItemId))
                {
                    blocking.Add($"ItemId {targetItemId} no existe en Items.d2o del paquete.");
                    checks.Add($"ItemId {targetItemId}: MISSING");
                }
                else
                {
                    var reader = new Sunshine.Protocol.Tools.D2o.D2OReader(itemsPath);
                    item = reader.ReadObject<D2oItem>(targetItemId, true);
                    reader.Close();
                    checks.Add($"ItemId {targetItemId}: FOUND");
                }
            }
            catch (Exception exception)
            {
                blocking.Add($"No se pudo leer Items.d2o: {exception.Message}");
            }
        }

        if (item is not null)
        {
            if (request.ExpectedTypeId.HasValue && item.typeId != request.ExpectedTypeId.Value)
            {
                blocking.Add($"typeId en paquete ({item.typeId}) no coincide con esperado ({request.ExpectedTypeId}).");
            }

            if (!IsTypeKnown(request.ClientItemTypesD2oPath, item.typeId))
            {
                blocking.Add($"TypeId {item.typeId} no resuelve en ItemTypes.d2o del cliente.");
            }
            else
            {
                checks.Add($"TypeId {item.typeId}: VALID");
            }

            if (item.appearanceId == 0)
            {
                checks.Add("AppearanceId 0: NOT_APPLICABLE");
            }
            else if (request.ExpectedAppearanceId == 0)
            {
                warnings.Add($"AppearanceId en paquete es {item.appearanceId}; caso control esperaba 0.");
            }

            ValidateI18n(blocking, warnings, checks, esPath, enPath, item.nameId, "nameId");
            ValidateI18n(blocking, warnings, checks, esPath, enPath, item.descriptionId, "descriptionId");

            if (request.ExpectedNameId.HasValue && item.nameId != request.ExpectedNameId.Value)
            {
                warnings.Add($"nameId paquete ({item.nameId}) difiere del esperado ({request.ExpectedNameId}).");
            }

            if (request.ExpectedDescriptionId.HasValue && item.descriptionId != request.ExpectedDescriptionId.Value)
            {
                warnings.Add($"descriptionId paquete ({item.descriptionId}) difiere del esperado ({request.ExpectedDescriptionId}).");
            }

            ValidateIcon(blocking, warnings, checks, request, item.iconId);
        }

        var generatedFiles = new[]
        {
            PublicationPackagePaths.ItemsRelative,
            PublicationPackagePaths.I18nEsRelative,
            PublicationPackagePaths.I18nEnRelative
        };

        var checksums = PublicationPackageChecksumWriter.ComputeChecksums(packageDirectory, generatedFiles);
        foreach (var relative in generatedFiles)
        {
            if (!checksums.ContainsKey(relative))
            {
                blocking.Add($"Checksum no generado para {relative}.");
            }
        }

        ValidateManifestChecksums(blocking, warnings, checks, packageDirectory, checksums);

        var isValid = blocking.Count == 0;
        var validationStatus = isValid
            ? StagingPublicationPackageValidationStatuses.ReadyForControlledPublish
            : StagingPublicationPackageValidationStatuses.InvalidStagingPackage;

        if (isValid && warnings.Count > 0)
        {
            validationStatus = StagingPublicationPackageValidationStatuses.ValidStagingPackage;
        }

        IReadOnlyList<string> nextSteps = isValid
            ? new[]
            {
                "Paquete autoconsistente en staging; no copiar a Client2.3.7 original.",
                "Phase 4: aplicar patch controlado solo en copia backup del cliente.",
                "Regenerar launcher lane (data.meta, VerInfo.rec) cuando se apruebe QA."
            }
            : new[]
            {
                "Corregir blocking reasons y volver a ejecutar --mode validate-publication-package.",
                "Si el paquete está corrupto, regenerar con --mode stage-item-publication."
            };

        return new PublicationPackageValidationResult(
            isValid,
            validationStatus,
            blocking,
            warnings,
            checks,
            checksums,
            nextSteps,
            item?.typeId,
            item?.iconId,
            item?.nameId,
            item?.descriptionId);
    }

    public void WriteReports(
        string packageDirectory,
        PublicationPackageValidationResult result,
        PublicationPackageManifestDocument? manifest = null)
    {
        var jsonPath = Path.Combine(packageDirectory, PublicationPackagePaths.ValidationJson);
        var mdPath = Path.Combine(packageDirectory, PublicationPackagePaths.ValidationMarkdown);

        var payload = new
        {
            result.IsValid,
            result.ValidationStatus,
            CheckedAt = DateTimeOffset.UtcNow,
            result.Checks,
            result.BlockingReasons,
            result.Warnings,
            result.NextManualSteps,
            result.Checksums
        };

        File.WriteAllText(jsonPath, JsonSerializer.Serialize(payload, JsonWriteOptions), Encoding.UTF8);
        File.WriteAllText(mdPath, BuildValidationMarkdown(result), Encoding.UTF8);

        if (manifest is not null)
        {
            var updated = manifest with
            {
                ValidationStatus = result.ValidationStatus,
                BlockingReasons = result.BlockingReasons,
                Warnings = result.Warnings,
                NextManualSteps = result.NextManualSteps,
                Checksums = result.Checksums
            };
            WriteManifest(packageDirectory, updated);
        }
    }

    public static void WriteManifest(string packageDirectory, PublicationPackageManifestDocument manifest)
    {
        var jsonPath = Path.Combine(packageDirectory, PublicationPackagePaths.ManifestJson);
        var mdPath = Path.Combine(packageDirectory, PublicationPackagePaths.ManifestMarkdown);
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(manifest, JsonWriteOptions), Encoding.UTF8);
        File.WriteAllText(mdPath, BuildManifestMarkdown(manifest), Encoding.UTF8);
    }

    private static void ValidateI18n(
        List<string> blocking,
        List<string> warnings,
        List<string> checks,
        string esPath,
        string enPath,
        int textId,
        string label)
    {
        if (!File.Exists(esPath) || !File.Exists(enPath))
        {
            return;
        }

        var es = D2iFile.Load(esPath);
        var en = D2iFile.Load(enPath);
        var esFound = es.TryGetText(textId, out var esText);
        var enFound = en.TryGetText(textId, out var enText);

        if (!esFound)
        {
            blocking.Add($"{label} {textId} no existe en i18n_es.d2i.");
        }

        if (!enFound)
        {
            blocking.Add($"{label} {textId} no existe en i18n_en.d2i.");
        }

        if (esFound && enFound)
        {
            checks.Add($"{label} {textId}: ES+EN");
            if (string.Equals(esText, enText, StringComparison.Ordinal))
            {
                warnings.Add($"{label} {textId} tiene el mismo texto en ES y EN (esperado distinto por locale).");
            }
        }
    }

    private static void ValidateIcon(
        List<string> blocking,
        List<string> warnings,
        List<string> checks,
        PublicationPackageValidationRequest request,
        int iconId)
    {
        if (iconId <= 0)
        {
            blocking.Add("IconId en paquete es cero o inválido.");
            return;
        }

        var curatedPath = Path.Combine(request.AdminByIconPreviewDirectory, $"{iconId}.png");
        if (File.Exists(curatedPath))
        {
            checks.Add($"IconId {iconId}: CURATED_BY_ICON");
            return;
        }

        if (TryFindIconInD2p(request.ClientRootPath, iconId))
        {
            checks.Add($"IconId {iconId}: D2P_GFX");
            return;
        }

        blocking.Add($"IconId {iconId} no encontrado en catálogo by-icon ni en bitmap*.d2p del cliente.");
    }

    private static bool TryFindIconInD2p(string clientRoot, int iconId)
    {
        var gfxDir = Path.Combine(clientRoot, "content", "gfx", "items");
        if (!Directory.Exists(gfxDir))
        {
            return false;
        }

        foreach (var d2pPath in Directory.EnumerateFiles(gfxDir, "bitmap*.d2p"))
        {
            if (D2pContainsIconId(d2pPath, iconId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool D2pContainsIconId(string d2pPath, int iconId)
    {
        try
        {
            var bytes = File.ReadAllBytes(d2pPath);
            var needle = Encoding.ASCII.GetBytes(iconId.ToString());
            return IndexOf(bytes, needle) >= 0;
        }
        catch
        {
            return false;
        }
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        if (needle.Length == 0 || haystack.Length < needle.Length)
        {
            return -1;
        }

        for (var index = 0; index <= haystack.Length - needle.Length; index++)
        {
            var match = true;
            for (var offset = 0; offset < needle.Length; offset++)
            {
                if (haystack[index + offset] != needle[offset])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsTypeKnown(string? itemTypesPath, int typeId)
    {
        if (string.IsNullOrWhiteSpace(itemTypesPath) || !File.Exists(itemTypesPath))
        {
            return false;
        }

        try
        {
            return ReadD2oIndexIds(itemTypesPath).Contains(typeId);
        }
        catch
        {
            return false;
        }
    }

    private static HashSet<int> ReadD2oIndexIds(string d2oPath)
    {
        using var stream = File.OpenRead(d2oPath);
        var header = new byte[3];
        stream.ReadExactly(header);
        var buffer = new byte[4];
        stream.ReadExactly(buffer);
        stream.Position = BinaryPrimitives.ReadInt32BigEndian(buffer);
        stream.ReadExactly(buffer);
        var indexLength = BinaryPrimitives.ReadInt32BigEndian(buffer);
        var count = indexLength / 8;
        var ids = new HashSet<int>(count);
        for (var index = 0; index < count; index++)
        {
            stream.ReadExactly(buffer);
            var id = BinaryPrimitives.ReadInt32BigEndian(buffer);
            stream.ReadExactly(buffer);
            ids.Add(id);
        }

        return ids;
    }

    private static void ValidateManifestChecksums(
        List<string> blocking,
        List<string> warnings,
        List<string> checks,
        string packageDirectory,
        IReadOnlyDictionary<string, string> computed)
    {
        var manifestPath = Path.Combine(packageDirectory, PublicationPackagePaths.ManifestJson);
        if (!File.Exists(manifestPath))
        {
            warnings.Add("publication-package-manifest.json ausente; se generará al validar.");
            return;
        }

        try
        {
            using var stream = File.OpenRead(manifestPath);
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("Checksums", out var checksumsElement)
                || checksumsElement.ValueKind != JsonValueKind.Object)
            {
                warnings.Add("Manifest sin sección Checksums; se actualizará tras validación.");
                return;
            }

            foreach (var property in checksumsElement.EnumerateObject())
            {
                if (!computed.TryGetValue(property.Name, out var actual))
                {
                    continue;
                }

                var expected = property.Value.GetString();
                if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                {
                    blocking.Add($"Checksum manifest no coincide para {property.Name}.");
                }
                else
                {
                    checks.Add($"Checksum {property.Name}: OK");
                }
            }
        }
        catch (JsonException exception)
        {
            warnings.Add($"No se pudo comparar manifest: {exception.Message}");
        }
    }

    private static void RequireFile(List<string> blocking, List<string> checks, string path, string label)
    {
        if (!File.Exists(path))
        {
            blocking.Add($"{label} no encontrado: {path}");
        }
        else
        {
            checks.Add($"{label}: EXISTS");
        }
    }

    private static string BuildValidationMarkdown(PublicationPackageValidationResult result) =>
        $"""
        # Validation report (staging)

        - Valid: **{result.IsValid}**
        - Status: `{result.ValidationStatus}`
        - Checks: {result.Checks.Count}
        - Blocking: {result.BlockingReasons.Count}
        - Warnings: {result.Warnings.Count}

        ## BlockingReasons

        {string.Join(Environment.NewLine, result.BlockingReasons.Select(static reason => $"- {reason}").DefaultIfEmpty("- (ninguno)"))}

        ## Warnings

        {string.Join(Environment.NewLine, result.Warnings.Select(static warning => $"- {warning}").DefaultIfEmpty("- (ninguno)"))}
        """;

    private static string BuildManifestMarkdown(PublicationPackageManifestDocument manifest) =>
        $"""
        # Publication package manifest (staging)

        - PackageId: `{manifest.PackageId}`
        - TargetItemId: {manifest.TargetItemId}
        - SourceTemplateItemId: {manifest.SourceTemplateItemId}
        - ValidationStatus: `{manifest.ValidationStatus}`
        - Production: **{manifest.IsProductionPackage}** (debe ser false)
        - nameId / descriptionId: {manifest.NameId} / {manifest.DescriptionId}
        """;

    private static readonly JsonSerializerOptions JsonWriteOptions = new() { WriteIndented = true };
}

internal static class StagingPublicationPackageValidationStatuses
{
    public const string ValidStagingPackage = "VALID_STAGING_PACKAGE";
    public const string InvalidStagingPackage = "INVALID_STAGING_PACKAGE";
    public const string ReadyForControlledPublish = "READY_FOR_CONTROLLED_PUBLISH";
    public const string BlockedValidation = "BLOCKED_VALIDATION";
}

internal sealed record PublicationPackageValidationRequest(
    string PackageDirectory,
    string RepoRoot,
    string ClientRootPath,
    string AdminByIconPreviewDirectory,
    string? ClientItemTypesD2oPath,
    int ExpectedTargetItemId,
    int? ExpectedTypeId,
    int? ExpectedNameId,
    int? ExpectedDescriptionId,
    int? ExpectedAppearanceId);

internal sealed record PublicationPackageValidationResult(
    bool IsValid,
    string ValidationStatus,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Checks,
    IReadOnlyDictionary<string, string> Checksums,
    IReadOnlyList<string> NextManualSteps,
    int? ResolvedTypeId,
    int? ResolvedIconId,
    int? ResolvedNameId,
    int? ResolvedDescriptionId);
