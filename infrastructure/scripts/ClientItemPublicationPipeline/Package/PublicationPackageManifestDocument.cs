namespace ClientItemPublicationPipeline.Package;

internal sealed record PublicationPackageManifestDocument
{
    public string PackageId { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public int SourceTemplateItemId { get; init; }
    public int TargetItemId { get; init; }
    public int NameId { get; init; }
    public int DescriptionId { get; init; }
    public string IdModel { get; init; } = string.Empty;
    public IReadOnlyList<string> GeneratedFiles { get; init; } = [];
    public IReadOnlyDictionary<string, string> Checksums { get; init; } = new Dictionary<string, string>();
    public string ValidationStatus { get; init; } = string.Empty;
    public IReadOnlyList<string> BlockingReasons { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyList<string> NextManualSteps { get; init; } = [];
    public bool IsProductionPackage { get; init; }
}
