namespace RollblackLegacy.Admin.Contracts.Health;

public sealed record AdminDatabaseHealthResponse(
    string Status,
    string Service,
    string Database,
    string Message,
    DateTimeOffset CheckedAtUtc);
