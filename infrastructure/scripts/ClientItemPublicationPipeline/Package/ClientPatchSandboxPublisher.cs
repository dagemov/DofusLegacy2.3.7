using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClientItemPublicationPipeline.D2i;
using D2oItem = Sunshine.Protocol.Tools.D2o.Classes.Item;

namespace ClientItemPublicationPipeline.Package;

internal sealed class ClientPatchSandboxPublisher
{
    public SandboxApplyResult ApplyPackageToSandbox(
        string repoRoot,
        string packageDirectory,
        string sandboxDirectory,
        int expectedItemId = 12617)
    {
        Directory.CreateDirectory(sandboxDirectory);
        SeedSandboxFromClient(repoRoot, sandboxDirectory);

        var patchRelativeFiles = PublicationPackagePatchFiles.ResolveRelativeFiles(packageDirectory);

        foreach (var relative in patchRelativeFiles)
        {
            var source = PublicationPackagePatchFiles.ResolvePackageSourcePath(packageDirectory, relative);
            var destination = Path.Combine(sandboxDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
            var parent = Path.GetDirectoryName(destination)!;
            Directory.CreateDirectory(parent);
            File.WriteAllBytes(destination, File.ReadAllBytes(source));
        }

        var appliedAt = DateTimeOffset.UtcNow;
        var manifest = new
        {
            AppliedAtUtc = appliedAt,
            PackageDirectory = ToRepoRelative(repoRoot, packageDirectory),
            SandboxDirectory = ToRepoRelative(repoRoot, sandboxDirectory),
            ExpectedItemId = expectedItemId,
            AppliedFiles = patchRelativeFiles,
            ClientRealUntouched = true,
            RealClientRoot = Path.Combine(repoRoot, "Client2.3.7")
        };

        var manifestPath = Path.Combine(sandboxDirectory, "sandbox-apply-manifest.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonWriteOptions), Encoding.UTF8);

        return new SandboxApplyResult(sandboxDirectory, manifestPath, appliedAt);
    }

    public SandboxValidationResult ValidateSandbox(
        string repoRoot,
        string sandboxDirectory,
        int expectedItemId = 12617,
        int expectedIconId = 23012)
    {
        var blocking = new List<string>();
        var warnings = new List<string>();
        var checks = new List<string>();

        var realClientRoot = Path.Combine(repoRoot, "Client2.3.7");
        var baselinePath = Path.Combine(sandboxDirectory, "original-client-baseline.json");
        VerifyRealClientUnchanged(realClientRoot, baselinePath, blocking, checks);

        var sandboxItems = Path.Combine(sandboxDirectory, "data", "common", "Items.d2o");
        var sandboxEs = Path.Combine(sandboxDirectory, "data", "i18n", "i18n_es.d2i");
        var sandboxEn = Path.Combine(sandboxDirectory, "data", "i18n", "i18n_en.d2i");

        RequireFile(blocking, checks, sandboxItems, "sandbox Items.d2o");
        RequireFile(blocking, checks, sandboxEs, "sandbox i18n_es.d2i");
        RequireFile(blocking, checks, sandboxEn, "sandbox i18n_en.d2i");

        D2oItem? item = null;
        if (File.Exists(sandboxItems))
        {
            try
            {
                var indexIds = ReadD2oIndexIds(sandboxItems);
                if (!indexIds.Contains(expectedItemId))
                {
                    blocking.Add($"ItemId {expectedItemId} no existe en sandbox Items.d2o.");
                }
                else
                {
                    checks.Add($"ItemId {expectedItemId}: FOUND");
                    var reader = new Sunshine.Protocol.Tools.D2o.D2OReader(sandboxItems);
                    item = reader.ReadObject<D2oItem>(expectedItemId, true);
                    reader.Close();
                }
            }
            catch (Exception exception)
            {
                blocking.Add($"No se pudo leer sandbox Items.d2o: {exception.Message}");
            }
        }

        if (item is not null)
        {
            ValidateI18n(blocking, warnings, checks, sandboxEs, sandboxEn, item.nameId, "nameId");
            ValidateI18n(blocking, warnings, checks, sandboxEs, sandboxEn, item.descriptionId, "descriptionId");

            if (item.iconId != expectedIconId)
            {
                warnings.Add($"IconId en sandbox es {item.iconId}; esperado {expectedIconId}.");
            }
            else
            {
                checks.Add($"IconId {expectedIconId}: OK");
            }

            if (item.appearanceId == 0)
            {
                checks.Add("AppearanceId 0: NOT_APPLICABLE");
            }
        }

        var patchedFiles = PublicationPackagePatchFiles.ResolveClientRelativeFiles(sandboxDirectory);
        var checksums = PublicationPackageChecksumWriter.ComputeChecksums(sandboxDirectory, patchedFiles);
        foreach (var relative in patchedFiles)
        {
            if (checksums.ContainsKey(relative))
            {
                checks.Add($"Checksum {relative}: GENERATED");
            }
        }

        var isValid = blocking.Count == 0;
        var result = new SandboxValidationResult(
            isValid,
            isValid ? "VALID_SANDBOX_CLIENT" : "INVALID_SANDBOX_CLIENT",
            blocking,
            warnings,
            checks,
            checksums,
            item?.nameId,
            item?.descriptionId,
            item?.typeId,
            item?.iconId);

        WriteValidationReports(sandboxDirectory, result, expectedItemId);
        return result;
    }

    private static void SeedSandboxFromClient(string repoRoot, string sandboxDirectory)
    {
        var clientRoot = Path.Combine(repoRoot, "Client2.3.7");
        var baseline = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var relative in PublicationPackagePatchFiles.CoreRelativeFiles)
        {
            var source = Path.Combine(clientRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(source))
            {
                throw new FileNotFoundException($"Archivo cliente requerido no encontrado: {source}");
            }

            baseline[relative] = HashFile(source);

            var destination = Path.Combine(sandboxDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
            var parent = Path.GetDirectoryName(destination)!;
            Directory.CreateDirectory(parent);

            if (!File.Exists(destination))
            {
                File.WriteAllBytes(destination, File.ReadAllBytes(source));
            }
        }

        var itemSetsSource = Path.Combine(
            clientRoot,
            PublicationPackagePaths.ItemSetsRelative.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(itemSetsSource))
        {
            baseline[PublicationPackagePaths.ItemSetsRelative] = HashFile(itemSetsSource);
            var itemSetsDestination = Path.Combine(
                sandboxDirectory,
                PublicationPackagePaths.ItemSetsRelative.Replace('/', Path.DirectorySeparatorChar));
            var parent = Path.GetDirectoryName(itemSetsDestination)!;
            Directory.CreateDirectory(parent);
            if (!File.Exists(itemSetsDestination))
            {
                File.WriteAllBytes(itemSetsDestination, File.ReadAllBytes(itemSetsSource));
            }
        }

        var baselinePath = Path.Combine(sandboxDirectory, "original-client-baseline.json");
        if (!File.Exists(baselinePath))
        {
            var payload = new
            {
                CapturedAtUtc = DateTimeOffset.UtcNow,
                ClientRootPath = clientRoot,
                Checksums = baseline
            };
            File.WriteAllText(baselinePath, JsonSerializer.Serialize(payload, JsonWriteOptions), Encoding.UTF8);
        }
    }

    private static void VerifyRealClientUnchanged(
        string realClientRoot,
        string baselinePath,
        List<string> blocking,
        List<string> checks)
    {
        if (!File.Exists(baselinePath))
        {
            blocking.Add("original-client-baseline.json ausente; ejecutar apply-package-to-sandbox primero.");
            return;
        }

        try
        {
            using var stream = File.OpenRead(baselinePath);
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("Checksums", out var checksumsElement))
            {
                blocking.Add("Baseline sin checksums.");
                return;
            }

            foreach (var property in checksumsElement.EnumerateObject())
            {
                var relative = property.Name;
                var expected = property.Value.GetString();
                var currentPath = Path.Combine(realClientRoot, relative.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(currentPath))
                {
                    blocking.Add($"Archivo real eliminado: {relative}");
                    continue;
                }

                var current = HashFile(currentPath);
                if (!string.Equals(expected, current, StringComparison.OrdinalIgnoreCase))
                {
                    blocking.Add($"Client2.3.7 real MODIFICADO: {relative}");
                }
                else
                {
                    checks.Add($"Client2.3.7 intacto: {relative}");
                }
            }
        }
        catch (Exception exception)
        {
            blocking.Add($"No se pudo verificar baseline del cliente real: {exception.Message}");
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
            blocking.Add($"{label} {textId} no resuelve en sandbox i18n_es.d2i.");
        }

        if (!en.TryGetText(textId, out _))
        {
            blocking.Add($"{label} {textId} no resuelve en sandbox i18n_en.d2i.");
        }

        if (es.TryGetText(textId, out var esText) && en.TryGetText(textId, out var enText))
        {
            checks.Add($"{label} {textId}: ES+EN");
            if (string.Equals(esText, enText, StringComparison.Ordinal))
            {
                warnings.Add($"{label} {textId} tiene el mismo texto en ES y EN.");
            }
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

    private static void WriteValidationReports(string sandboxDirectory, SandboxValidationResult result, int itemId)
    {
        var jsonPath = Path.Combine(sandboxDirectory, "sandbox-validation-report.json");
        var mdPath = Path.Combine(sandboxDirectory, "sandbox-validation-report.md");

        var payload = new
        {
            result.IsValid,
            result.ValidationStatus,
            ItemId = itemId,
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
            # Sandbox validation report

            - ItemId: {itemId}
            - Valid: **{result.IsValid}**
            - Status: `{result.ValidationStatus}`

            ## Checks

            {string.Join(Environment.NewLine, result.Checks.Select(static c => $"- {c}"))}

            ## Blocking

            {string.Join(Environment.NewLine, result.BlockingReasons.Select(static b => $"- {b}").DefaultIfEmpty("- (ninguno)"))}
            """,
            Encoding.UTF8);
    }

    private static HashSet<int> ReadD2oIndexIds(string d2oPath)
    {
        using var stream = File.OpenRead(d2oPath);
        var header = new byte[3];
        stream.ReadExactly(header);
        var buffer = new byte[4];
        stream.ReadExactly(buffer);
        stream.Position = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer);
        stream.ReadExactly(buffer);
        var indexLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer);
        var count = indexLength / 8;
        var ids = new HashSet<int>(count);
        for (var index = 0; index < count; index++)
        {
            stream.ReadExactly(buffer);
            var id = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer);
            stream.ReadExactly(buffer);
            ids.Add(id);
        }

        return ids;
    }

    private static string HashFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static string ToRepoRelative(string repoRoot, string absolutePath) =>
        Path.GetRelativePath(repoRoot, absolutePath).Replace('\\', '/');

    private static readonly JsonSerializerOptions JsonWriteOptions = new() { WriteIndented = true };
}

internal sealed record SandboxApplyResult(string SandboxDirectory, string ManifestPath, DateTimeOffset AppliedAtUtc);

internal sealed record SandboxValidationResult(
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
