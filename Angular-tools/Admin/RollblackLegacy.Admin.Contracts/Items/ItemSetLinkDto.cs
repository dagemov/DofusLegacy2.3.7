namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemSetLinkDto(
    int SetId,
    string? SetName,
    string State);
