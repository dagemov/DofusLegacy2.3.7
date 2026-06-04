namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IStagingPublicationPackageProbe
{
    StagingPublicationPackageProbeResult Probe(int itemId);
}

public sealed record StagingPublicationPackageProbeResult(
    string StagingPackageStatus,
    string? StagingPackagePath,
    string? StagingPackageId,
    string? StagingValidationStatus,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> NextManualSteps);
