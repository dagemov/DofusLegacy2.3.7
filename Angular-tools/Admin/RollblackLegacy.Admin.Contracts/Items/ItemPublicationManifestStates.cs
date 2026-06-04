namespace RollblackLegacy.Admin.Contracts.Items;

/// <summary>
/// Estados del manifiesto de publicación cliente (Phase 1 — dry-run).
/// </summary>
public static class ItemPublicationManifestStates
{
    public const string ReadyToStage = "READY_TO_STAGE";
    public const string BlockedClientWriterMissing = "BLOCKED_CLIENT_WRITER_MISSING";
    public const string BlockedI18nWriterMissing = "BLOCKED_I18N_WRITER_MISSING";
    public const string BlockedUnknownType = "BLOCKED_UNKNOWN_TYPE";
    public const string BlockedInvalidIcon = "BLOCKED_INVALID_ICON";
    public const string BlockedManualReview = "BLOCKED_MANUAL_REVIEW";
}
