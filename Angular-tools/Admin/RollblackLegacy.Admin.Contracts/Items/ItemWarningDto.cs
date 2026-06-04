namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemWarningDto(
    string Code,
    string Severity,
    string Message,
    string? Field);
