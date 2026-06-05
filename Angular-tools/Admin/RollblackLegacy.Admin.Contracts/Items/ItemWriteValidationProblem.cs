namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemWriteValidationProblem(
    string Code,
    string Severity,
    string Message,
    string? Field);
