namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemEffectsUpdateRequest(
    IReadOnlyList<ItemEffectEditRowRequest> Effects,
    string? PreservedSuffixHex = null,
    IReadOnlyList<string>? RemovedUnsupportedRowIds = null);
