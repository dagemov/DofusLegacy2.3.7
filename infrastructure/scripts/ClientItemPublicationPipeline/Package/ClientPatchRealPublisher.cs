using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClientItemPublicationPipeline.D2i;
using D2oItem = Sunshine.Protocol.Tools.D2o.Classes.Item;

namespace ClientItemPublicationPipeline.Package;

internal sealed class ClientPatchRealPublisher
{
    private static readonly string[] PatchRelativeFiles =
    [
        PublicationPackagePaths.ItemsRelative,
        PublicationPackagePaths.I18nEsRelative,
        PublicationPackagePaths.I18nEnRelative
    ];

    public RealClientApplyResult ApplyPackageToRealClient(
        string repoRoot,
        string packageDirectory,
        string clientRoot,
        int expectedItemId = 12617)
    {
        RequireConfirmPublish();
        var errors = new List<string>();
        var checks = new List<string>();

        if (!ClientPublicationBackupGuard.TryResolveLatestBackup(repoRoot, out var backupDir, out var backupErrors))
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, backupErrors));
        }

        if (!ClientPublicationBackupGuard.BackupMatchesClient(clientRoot, backupDir, errors, checks))
        {
            throw new InvalidOperationException(
                "Backup obligatorio invalido o desactualizado. DETENERSE — NO PUBLICAR." + Environment.NewLine +
                string.Join(Environment.NewLine, errors));
        }

        var packageItems = PublicationPackagePaths.ResolveItemsPath(packageDirectory);
        var packageEs = PublicationPackagePaths.ResolveI18nEsPath(packageDirectory);
        var packageEn = PublicationPackagePaths.ResolveI18nEnPath(packageDirectory);

        var prePatchChecksums = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var relative in PatchRelativeFiles)
        {
            var destination = Path.Combine(clientRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(destination))
            {
                throw new FileNotFoundException($"Archivo cliente requerido no encontrado: {destination}");
            }

            prePatchChecksums[relative] = HashFile(destination);
        }

        foreach (var relative in PatchRelativeFiles)
        {
            var source = relative switch
            {
                _ when relative == PublicationPackagePaths.ItemsRelative => packageItems,
                _ when relative == PublicationPackagePaths.I18nEsRelative => packageEs,
                _ when relative == PublicationPackagePaths.I18nEnRelative => packageEn,
                _ => throw new InvalidOperationException($"Archivo no mapeado: {relative}")
            };

            var destination = Path.Combine(clientRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            var parent = Path.GetDirectoryName(destination)!;
            Directory.CreateDirectory(parent);
            File.WriteAllBytes(destination, File.ReadAllBytes(source));
        }

        var appliedAt = DateTimeOffset.UtcNow;
        var reportDir = Path.Combine(
            repoRoot,
            "Infrastructure",
            "temporal-artifacts",
            "client-real-publish",
            expectedItemId.ToString());
        Directory.CreateDirectory(reportDir);

        var manifest = new
        {
            AppliedAtUtc = appliedAt,
            PackageDirectory = ToRepoRelative(repoRoot, packageDirectory),
            ClientRoot = ToRepoRelative(repoRoot, clientRoot),
            BackupDirectory = ToRepoRelative(repoRoot, backupDir),
            ExpectedItemId = expectedItemId,
            AppliedFiles = PatchRelativeFiles,
            PrePatchChecksums = prePatchChecksums,
            PostPatchChecksums = PublicationPackageChecksumWriter.ComputeChecksums(clientRoot, PatchRelativeFiles),
            ConfirmPublish = true
        };

        var manifestPath = Path.Combine(reportDir, "real-client-apply-manifest.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonWriteOptions), Encoding.UTF8);

        return new RealClientApplyResult(clientRoot, manifestPath, backupDir, appliedAt);
    }

    public RealClientValidationResult ValidateRealClient(
        string repoRoot,
        string clientRoot,
        int expectedItemId = 12617,
        int expectedIconId = 23012,
        int? expectedNameId = 63079,
        int? expectedDescriptionId = 63080)
    {
        var blocking = new List<string>();
        var warnings = new List<string>();
        var checks = new List<string>();

        var itemsPath = Path.Combine(clientRoot, "data", "common", "Items.d2o");
        var esPath = Path.Combine(clientRoot, "data", "i18n", "i18n_es.d2i");
        var enPath = Path.Combine(clientRoot, "data", "i18n", "i18n_en.d2i");

        RequireFile(blocking, checks, itemsPath, "Items.d2o");
        RequireFile(blocking, checks, esPath, "i18n_es.d2i");
        RequireFile(blocking, checks, enPath, "i18n_en.d2i");

        D2oItem? item = null;
        if (File.Exists(itemsPath))
        {
            try
            {
                var indexIds = ClientPatchD2oIndex.ReadIds(itemsPath);
                if (!indexIds.Contains(expectedItemId))
                {
                    blocking.Add($"ItemId {expectedItemId} no existe en Items.d2o real.");
                }
                else
                {
                    checks.Add($"ItemId {expectedItemId}: FOUND");
                    var reader = new Sunshine.Protocol.Tools.D2o.D2OReader(itemsPath);
                    item = reader.ReadObject<D2oItem>(expectedItemId, true);
                    reader.Close();
                }
            }
            catch (Exception exception)
            {
                blocking.Add($"No se pudo leer Items.d2o real: {exception.Message}");
            }
        }

        if (item is not null)
        {
            ValidateI18n(blocking, warnings, checks, esPath, enPath, item.nameId, "nameId");
            ValidateI18n(blocking, warnings, checks, esPath, enPath, item.descriptionId, "descriptionId");

            if (expectedNameId.HasValue && item.nameId != expectedNameId.Value)
            {
                warnings.Add($"nameId real ({item.nameId}) difiere del esperado paquete 12617 ({expectedNameId}).");
            }

            if (expectedDescriptionId.HasValue && item.descriptionId != expectedDescriptionId.Value)
            {
                warnings.Add(
                    $"descriptionId real ({item.descriptionId}) difiere del esperado paquete 12617 ({expectedDescriptionId}).");
            }

            if (item.iconId != expectedIconId)
            {
                warnings.Add($"IconId real es {item.iconId}; esperado {expectedIconId}.");
            }
            else
            {
                checks.Add($"IconId {expectedIconId}: OK");
            }
        }

        var checksums = PublicationPackageChecksumWriter.ComputeChecksums(clientRoot, PatchRelativeFiles);
        foreach (var relative in PatchRelativeFiles)
        {
            if (checksums.ContainsKey(relative))
            {
                checks.Add($"Checksum {relative}: REGISTERED");
            }
        }

        var adminByIcon = Path.Combine(
            repoRoot,
            "Angular-tools",
            "Admin",
            "RollblackLegacy.Admin.Angular",
            "src",
            "assets",
            "item-previews",
            "by-icon",
            $"{expectedIconId}.png");

        if (File.Exists(adminByIcon))
        {
            checks.Add($"Admin by-icon preview {expectedIconId}.png: EXISTS");
        }
        else
        {
            warnings.Add($"Admin by-icon preview no encontrado: {adminByIcon}");
        }

        var isValid = blocking.Count == 0;
        var result = new RealClientValidationResult(
            isValid,
            isValid ? "VALID_REAL_CLIENT" : "INVALID_REAL_CLIENT",
            blocking,
            warnings,
            checks,
            checksums,
            item?.nameId,
            item?.descriptionId,
            item?.typeId,
            item?.iconId);

        var reportDir = Path.Combine(
            repoRoot,
            "Infrastructure",
            "temporal-artifacts",
            "client-real-publish",
            expectedItemId.ToString());
        Directory.CreateDirectory(reportDir);
        WriteValidationReports(reportDir, result, expectedItemId, clientRoot);
        return result;
    }

    private static void RequireConfirmPublish()
    {
        var confirm = Environment.GetEnvironmentVariable("CONFIRM_PUBLISH");
        if (!string.Equals(confirm, "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Publicacion al cliente real bloqueada. Establece CONFIRM_PUBLISH=1 y ejecuta backup-client antes.");
        }
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
        if (!es.TryGetText(textId, out _))
        {
            blocking.Add($"{label} {textId} no resuelve en i18n_es.d2i real.");
        }

        if (!en.TryGetText(textId, out _))
        {
            blocking.Add($"{label} {textId} no resuelve en i18n_en.d2i real.");
        }

        if (es.TryGetText(textId, out _) && en.TryGetText(textId, out _))
        {
            checks.Add($"{label} {textId}: ES+EN");
        }
    }

    private static void RequireFile(List<string> blocking, List<string> checks, string path, string label)
    {
        if (!File.Exists(path))
        {
            blocking.Add($"{label} no encontrado.");
        }
        else
        {
            checks.Add($"{label}: EXISTS");
        }
    }

    private static void WriteValidationReports(
        string reportDirectory,
        RealClientValidationResult result,
        int itemId,
        string clientRoot)
    {
        var jsonPath = Path.Combine(reportDirectory, "real-client-validation-report.json");
        var mdPath = Path.Combine(reportDirectory, "real-client-validation-report.md");

        var payload = new
        {
            result.IsValid,
            result.ValidationStatus,
            ItemId = itemId,
            ClientRoot = clientRoot,
            CheckedAt = DateTimeOffset.UtcNow,
            result.Checks,
            result.BlockingReasons,
            result.Warnings,
            result.Checksums,
            result.NameId,
            result.DescriptionId,
            result.TypeId,
            result.IconId
        };

        File.WriteAllText(jsonPath, JsonSerializer.Serialize(payload, JsonWriteOptions), Encoding.UTF8);
        File.WriteAllText(
            mdPath,
            $"""
            # Real client validation report

            - ItemId: {itemId}
            - Client: `{clientRoot}`
            - Valid: **{result.IsValid}**
            - Status: `{result.ValidationStatus}`

            ## Checks

            {string.Join(Environment.NewLine, result.Checks.Select(static c => $"- {c}"))}

            ## Blocking

            {string.Join(Environment.NewLine, result.BlockingReasons.Select(static b => $"- {b}").DefaultIfEmpty("- (ninguno)"))}
            """,
            Encoding.UTF8);
    }

    private static string HashFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static string ToRepoRelative(string repoRoot, string absolutePath) =>
        Path.GetRelativePath(repoRoot, absolutePath).Replace('\\', '/');

    private static readonly JsonSerializerOptions JsonWriteOptions = new() { WriteIndented = true };
}

internal sealed record RealClientApplyResult(
    string ClientRoot,
    string ManifestPath,
    string BackupDirectory,
    DateTimeOffset AppliedAtUtc);

internal sealed record RealClientValidationResult(
    bool IsValid,
    string ValidationStatus,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Checks,
    IReadOnlyDictionary<string, string> Checksums,
    int? NameId,
    int? DescriptionId,
    int? TypeId,
    int? IconId);
