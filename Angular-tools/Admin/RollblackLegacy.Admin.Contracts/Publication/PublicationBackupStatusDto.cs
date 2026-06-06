namespace RollblackLegacy.Admin.Contracts.Publication;

public sealed record PublicationBackupStatusDto(
    DateTimeOffset? LastClientBackupUtc,
    string? LastClientBackupPath,
    DateTimeOffset? LastDbBackupUtc,
    string? LastDbBackupPath,
    DateTimeOffset? LastVpsInventoryUtc,
    string? LastVpsInventoryPath,
    DateTimeOffset? LastValidationUtc,
    string? LastValidationStatus,
    string PublishLaneStatus,
    int TargetItemId,
    string? StagingPackagePath,
    bool ProductionPublishBlocked,
    IReadOnlyList<string> PublishLaneBlockingReasons,
    IReadOnlyList<string> RecoveryReadinessNotes,
    IReadOnlyList<string> NextManualSteps,
    DateTimeOffset GeneratedAtUtc);
