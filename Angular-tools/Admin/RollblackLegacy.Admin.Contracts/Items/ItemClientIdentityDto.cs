namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemClientIdentityDto(
    int ItemId,
    int? ClientNameId,
    string? ClientName,
    int? IconId,
    int? AppearanceId,
    string Source,
    double Confidence);
