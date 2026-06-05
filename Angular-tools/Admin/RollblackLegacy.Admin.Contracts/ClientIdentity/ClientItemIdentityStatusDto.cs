namespace RollblackLegacy.Admin.Contracts.ClientIdentity;

public sealed record ClientItemIdentityStatusDto(
    string PrimaryStatus,
    bool ClientKnown,
    bool NeedsClientPatch,
    IReadOnlyList<string> Statuses,
    IReadOnlyList<string> Warnings,
    string RecommendedAction);
