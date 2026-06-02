using Microsoft.Extensions.Options;
using MySqlConnector;
using RollblackLegacy.Admin.Application.Abstractions;
using RollblackLegacy.Admin.Application.Models;
using RollblackLegacy.Admin.Infrastructure.Configuration;

namespace RollblackLegacy.Admin.Infrastructure.Services;

public sealed class MySqlAdminDatabaseHealthService : IAdminDatabaseHealthService
{
    private readonly AdminDatabaseOptions _options;

    public MySqlAdminDatabaseHealthService(IOptions<AdminDatabaseOptions> options)
    {
        _options = options.Value;
    }

    public async Task<AdminDatabaseHealthProbeResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.HasUsableConnectionString())
        {
            return new AdminDatabaseHealthProbeResult(
                "not_configured",
                "sunshine",
                "SunshineAdmin connection string is missing or still using the placeholder password.",
                DateTimeOffset.UtcNow);
        }

        try
        {
            await using var connection = new MySqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new MySqlCommand("SELECT 1;", connection);
            _ = await command.ExecuteScalarAsync(cancellationToken);

            return new AdminDatabaseHealthProbeResult(
                "ok",
                "sunshine",
                "Connection probe completed successfully.",
                DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            return new AdminDatabaseHealthProbeResult(
                "error",
                "sunshine",
                ex.Message,
                DateTimeOffset.UtcNow);
        }
    }
}
