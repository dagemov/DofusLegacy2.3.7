namespace RollblackLegacy.Admin.Contracts.Items;

/// <summary>
/// Estado del paquete de publicación en staging (Phase 3C — no es producción).
/// </summary>
public static class StagingPublicationPackageStatuses
{
    public const string NoPackageGenerated = "NO_PACKAGE_GENERATED";
    public const string PackageAvailableInStaging = "PACKAGE_AVAILABLE_IN_STAGING";
    public const string NeedsValidation = "NEEDS_VALIDATION";
    public const string ReadyForControlledPublish = "READY_FOR_CONTROLLED_PUBLISH";
}
