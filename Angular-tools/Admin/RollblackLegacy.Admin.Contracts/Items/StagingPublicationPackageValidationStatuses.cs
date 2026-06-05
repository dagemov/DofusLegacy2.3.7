namespace RollblackLegacy.Admin.Contracts.Items;

/// <summary>
/// Resultado de validación del paquete staging (CLI o reporte persistido).
/// </summary>
public static class StagingPublicationPackageValidationStatuses
{
    public const string ValidStagingPackage = "VALID_STAGING_PACKAGE";
    public const string InvalidStagingPackage = "INVALID_STAGING_PACKAGE";
    public const string ReadyForControlledPublish = "READY_FOR_CONTROLLED_PUBLISH";
    public const string BlockedValidation = "BLOCKED_VALIDATION";
}
