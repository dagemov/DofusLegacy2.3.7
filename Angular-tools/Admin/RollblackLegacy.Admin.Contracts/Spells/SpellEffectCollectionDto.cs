namespace RollblackLegacy.Admin.Contracts.Spells;

public sealed record SpellEffectCollectionDto(
    bool RuntimeAvailable,
    bool ReferenceAvailable,
    string? RuntimeSource,
    string? ReferenceSource,
    IReadOnlyList<SpellEffectRowDto> RuntimeRows,
    IReadOnlyList<SpellEffectRowDto> ReferenceRows,
    IReadOnlyList<string> RuntimeWarnings,
    IReadOnlyList<string> ReferenceWarnings);
