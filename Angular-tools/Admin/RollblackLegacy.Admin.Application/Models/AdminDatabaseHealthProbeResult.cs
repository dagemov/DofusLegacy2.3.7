namespace RollblackLegacy.Admin.Application.Models;

public sealed record AdminDatabaseHealthProbeResult(
    string Status,
    string Database,
    string Message,
    DateTimeOffset CheckedAtUtc);
