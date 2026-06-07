namespace RollblackLegacy.Admin.Application.Models.Spells;

public sealed record AdminSpellEffectCollectionReadModel(
    bool RuntimeAvailable,
    bool ReferenceAvailable,
    string? RuntimeSource,
    string? ReferenceSource,
    IReadOnlyList<AdminSpellEffectRowReadModel> RuntimeRows,
    IReadOnlyList<AdminSpellEffectRowReadModel> ReferenceRows,
    IReadOnlyList<string> RuntimeWarnings,
    IReadOnlyList<string> ReferenceWarnings);
