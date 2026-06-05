namespace RollblackLegacy.Admin.Contracts.ClientIdentity;

public sealed record ClientItemI18nResolutionDto(
    string LanguageCode,
    int? TextId,
    bool Exists,
    string? Text,
    string? SourcePath);
