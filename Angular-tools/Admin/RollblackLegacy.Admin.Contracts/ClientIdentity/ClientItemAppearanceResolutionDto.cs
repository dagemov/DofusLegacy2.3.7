namespace RollblackLegacy.Admin.Contracts.ClientIdentity;

public sealed record ClientItemAppearanceResolutionDto(
    int? AppearanceId,
    bool? Exists,
    string? SourcePath);
