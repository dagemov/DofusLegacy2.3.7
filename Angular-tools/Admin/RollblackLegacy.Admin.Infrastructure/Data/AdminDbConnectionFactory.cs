using Microsoft.Extensions.Options;
using MySqlConnector;
using RollblackLegacy.Admin.Application.Exceptions;
using RollblackLegacy.Admin.Infrastructure.Configuration;

namespace RollblackLegacy.Admin.Infrastructure.Data;

public sealed class AdminDbConnectionFactory
{
    private readonly AdminDatabaseOptions _options;

    public AdminDbConnectionFactory(IOptions<AdminDatabaseOptions> options)
    {
        _options = options.Value;
    }

    public async Task<MySqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.HasUsableConnectionString())
        {
            throw new AdminNotConfiguredException(
                "SunshineAdmin no está configurado o sigue usando el password placeholder.");
        }

        var connection = new MySqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
