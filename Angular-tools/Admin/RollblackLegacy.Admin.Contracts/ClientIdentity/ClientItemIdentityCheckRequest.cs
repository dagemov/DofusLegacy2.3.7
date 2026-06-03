namespace RollblackLegacy.Admin.Contracts.ClientIdentity;

public sealed record ClientItemIdentityCheckRequest(
    IReadOnlyList<int> ItemIds);
