namespace Rollback.Admin.Models.Common;

public sealed record AdminLookupOption(
    string Value,
    string Label,
    string? Hint = null);
