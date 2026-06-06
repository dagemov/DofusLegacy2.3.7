using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using D2oItem = Sunshine.Protocol.Tools.D2o.Classes.Item;

namespace ClientItemPublicationPipeline.D2i;

internal sealed class D2iStagingPublisher
{
    public D2iInspectBundleResult Inspect(string sourceEsPath, string sourceEnPath, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var es = D2iFile.Inspect(sourceEsPath);
        var en = D2iFile.Inspect(sourceEnPath);
        var mdPath = Path.Combine(outputDirectory, "d2i-inspect-report.md");
        File.WriteAllText(mdPath, BuildInspectMarkdown(es, en), Encoding.UTF8);
        return new D2iInspectBundleResult(es, en, mdPath);
    }

    public D2iRoundTripResult RoundTrip(string sourceEsPath, string sourceEnPath, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var stagingEs = Path.Combine(outputDirectory, "i18n_es.d2i");
        var stagingEn = Path.Combine(outputDirectory, "i18n_en.d2i");
        var roundTripEs = Path.Combine(outputDirectory, "i18n_es.roundtrip.d2i");
        var roundTripEn = Path.Combine(outputDirectory, "i18n_en.roundtrip.d2i");

        D2iFile.CopyToStaging(sourceEsPath, stagingEs);
        D2iFile.CopyToStaging(sourceEnPath, stagingEn);

        var beforeEs = D2iFile.Load(sourceEsPath).Count;
        var beforeEn = D2iFile.Load(sourceEnPath).Count;
        var originalEsHash = HashFile(sourceEsPath);
        var originalEnHash = HashFile(sourceEnPath);

        var loadedEs = D2iFile.Load(stagingEs);
        loadedEs.Save(roundTripEs);
        var loadedEn = D2iFile.Load(stagingEn);
        loadedEn.Save(roundTripEn);

        var originalEs = D2iFile.Load(sourceEsPath);
        var afterEs = D2iFile.Load(roundTripEs);
        var afterEn = D2iFile.Load(roundTripEn);

        const int controlTextId = 40904;
        originalEs.TryGetText(controlTextId, out var controlOriginal);
        afterEs.TryGetText(controlTextId, out var controlRoundTrip);
        var textsMatch = string.Equals(controlOriginal, controlRoundTrip, StringComparison.Ordinal);

        var reportPath = Path.Combine(outputDirectory, "client-d2i-roundtrip-report.md");
        var ok = beforeEs == afterEs.Count
            && beforeEn == afterEn.Count
            && textsMatch
            && string.Equals(originalEsHash, HashFile(sourceEsPath), StringComparison.OrdinalIgnoreCase)
            && string.Equals(originalEnHash, HashFile(sourceEnPath), StringComparison.OrdinalIgnoreCase);

        File.WriteAllText(
            reportPath,
            BuildRoundTripMarkdown(beforeEs, beforeEn, afterEs.Count, afterEn.Count, ok, controlTextId, controlOriginal, controlRoundTrip, originalEsHash, originalEnHash),
            Encoding.UTF8);

        return new D2iRoundTripResult(beforeEs, beforeEn, afterEs.Count, afterEn.Count, ok, stagingEs, stagingEn, roundTripEs, roundTripEn, reportPath);
    }

    public D2iAppendTextResult AppendText(
        string sourceEsPath,
        string sourceEnPath,
        string outputDirectory,
        string esName,
        string esDescription,
        string enName,
        string enDescription)
    {
        Directory.CreateDirectory(outputDirectory);
        var stagingEs = Path.Combine(outputDirectory, "i18n_es.d2i");
        var stagingEn = Path.Combine(outputDirectory, "i18n_en.d2i");

        if (!File.Exists(stagingEs))
        {
            D2iFile.CopyToStaging(sourceEsPath, stagingEs);
        }

        if (!File.Exists(stagingEn))
        {
            D2iFile.CopyToStaging(sourceEnPath, stagingEn);
        }

        var esFile = D2iFile.Load(stagingEs);
        var enFile = D2iFile.Load(stagingEn);
        var esCountBefore = esFile.Count;
        var enCountBefore = enFile.Count;

        var nameId = Math.Max(esFile.AllocateNextId(), enFile.AllocateNextId());
        var descriptionId = nameId + 1;

        esFile.AppendText(nameId, esName);
        esFile.AppendText(descriptionId, esDescription);
        enFile.AppendText(nameId, enName);
        enFile.AppendText(descriptionId, enDescription);

        esFile.Save(stagingEs);
        enFile.Save(stagingEn);

        var verifyEs = D2iFile.Load(stagingEs);
        var verifyEn = D2iFile.Load(stagingEn);
        verifyEs.TryGetText(nameId, out var resolvedEsName);
        verifyEs.TryGetText(descriptionId, out var resolvedEsDesc);
        verifyEn.TryGetText(nameId, out var resolvedEnName);
        verifyEn.TryGetText(descriptionId, out var resolvedEnDesc);

        var result = new D2iAppendTextResult(
            nameId,
            descriptionId,
            esName,
            esDescription,
            enName,
            enDescription,
            resolvedEsName,
            resolvedEsDesc,
            resolvedEnName,
            resolvedEnDesc,
            esCountBefore,
            enCountBefore,
            verifyEs.Count,
            verifyEn.Count,
            stagingEs,
            stagingEn,
            string.Equals(esName, resolvedEsName, StringComparison.Ordinal)
                && string.Equals(esDescription, resolvedEsDesc, StringComparison.Ordinal)
                && string.Equals(enName, resolvedEnName, StringComparison.Ordinal)
                && string.Equals(enDescription, resolvedEnDesc, StringComparison.Ordinal));

        var jsonPath = Path.Combine(outputDirectory, "d2i-append-report.json");
        var mdPath = Path.Combine(outputDirectory, "d2i-append-report.md");
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(jsonPath, json, Encoding.UTF8);
        File.WriteAllText(mdPath, BuildAppendMarkdown(result), Encoding.UTF8);

        return result with { JsonPath = jsonPath, MarkdownPath = mdPath };
    }

    public StagePublicationPackageResult? TryStagePublicationPackage(
        string repoRoot,
        string packageDirectory,
        int sourceItemId,
        int targetItemId,
        D2iAppendTextResult i18nResult,
        int typeId = 23,
        int iconId = 23012,
        int appearanceId = 0)
    {
        var sourceItems = Path.Combine(repoRoot, "Client2.3.7", "data", "common", "Items.d2o");
        var clientRoot = Path.Combine(repoRoot, "Client2.3.7");
        var adminByIcon = Path.Combine(
            repoRoot,
            "Angular-tools",
            "Admin",
            "RollblackLegacy.Admin.Angular",
            "src",
            "assets",
            "item-previews",
            "by-icon");
        var itemTypes = Path.Combine(clientRoot, "data", "common", "ItemTypes.d2o");

        var builder = new Package.PublicationPackageBuilder();
        var build = builder.Build(
            new Package.PublicationPackageBuildRequest(
                repoRoot,
                packageDirectory,
                sourceItems,
                clientRoot,
                adminByIcon,
                itemTypes,
                sourceItemId,
                targetItemId,
                typeId,
                iconId,
                appearanceId,
                i18nResult));

        var jsonPath = Path.Combine(packageDirectory, Package.PublicationPackagePaths.ManifestJson);
        var mdPath = Path.Combine(packageDirectory, Package.PublicationPackagePaths.ManifestMarkdown);
        return new StagePublicationPackageResult(
            build.PackageDirectory,
            jsonPath,
            mdPath,
            build.NameId,
            build.DescriptionId,
            build.ValidationPassed,
            build.ValidationStatus);
    }

    private static string HashFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static string BuildInspectMarkdown(D2iInspectResult es, D2iInspectResult en) =>
        $"""
        # D2I inspect

        ## i18n_es.d2i
        - Path: `{es.Path}`
        - Size: {es.FileSizeBytes}
        - dataSize: {es.DataSize}
        - indexSize: {es.IndexSize}
        - entries: {es.IndexCount}
        - textId range: {es.MinTextId}..{es.MaxTextId}
        - Magic D2I header: no (primer int32 = offset al índice)

        ## i18n_en.d2i
        - Path: `{en.Path}`
        - Size: {en.FileSizeBytes}
        - entries: {en.IndexCount}
        - textId range: {en.MinTextId}..{en.MaxTextId}
        """;

    private static string BuildRoundTripMarkdown(
        int beforeEs,
        int beforeEn,
        int afterEs,
        int afterEn,
        bool ok,
        int controlTextId,
        string? controlOriginal,
        string? controlRoundTrip,
        string originalEsHash,
        string originalEnHash) =>
        $"""
        # D2I round-trip

        - ES before/after: {beforeEs} / {afterEs}
        - EN before/after: {beforeEn} / {afterEn}
        - Control textId {controlTextId} preserved: {string.Equals(controlOriginal, controlRoundTrip, StringComparison.Ordinal)}
        - Round-trip OK: {ok}
        - Original ES sha256 unchanged: {originalEsHash}
        - Original EN sha256 unchanged: {originalEnHash}
        """;

    private static string BuildAppendMarkdown(D2iAppendTextResult r) =>
        $"""
        # D2I append

        - NameId (ES+EN shared): {r.NameId}
        - DescriptionId (ES+EN shared): {r.DescriptionId}
        - Verified: {r.Verified}
        - ES name: {r.ResolvedEsName}
        - EN name: {r.ResolvedEnName}
        """;

}

internal sealed record D2iInspectBundleResult(D2iInspectResult Es, D2iInspectResult En, string MarkdownPath);

internal sealed record D2iRoundTripResult(
    int BeforeEsCount,
    int BeforeEnCount,
    int AfterEsCount,
    int AfterEnCount,
    bool Success,
    string StagingEsPath,
    string StagingEnPath,
    string RoundTripEsPath,
    string RoundTripEnPath,
    string ReportPath);

internal sealed record D2iAppendTextResult(
    int NameId,
    int DescriptionId,
    string EsName,
    string EsDescription,
    string EnName,
    string EnDescription,
    string? ResolvedEsName,
    string? ResolvedEsDesc,
    string? ResolvedEnName,
    string? ResolvedEnDesc,
    int EsCountBeforeSave,
    int EnCountBeforeSave,
    int EsCountAfter,
    int EnCountAfter,
    string StagingEsPath,
    string StagingEnPath,
    bool Verified)
{
    public string? JsonPath { get; init; }
    public string? MarkdownPath { get; init; }
}

internal sealed record StagePublicationPackageResult(
    string PackageDirectory,
    string JsonPath,
    string MarkdownPath,
    int NameId,
    int DescriptionId,
    bool ValidationPassed,
    string ValidationStatus);
