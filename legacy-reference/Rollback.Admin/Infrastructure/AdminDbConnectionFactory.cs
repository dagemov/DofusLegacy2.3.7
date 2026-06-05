using Microsoft.Extensions.Options;
using MySqlConnector;
using Rollback.Admin.Configuration;

namespace Rollback.Admin.Infrastructure;

public sealed class AdminDbConnectionFactory
{
    private readonly RollbackAdminDatabasesOptions _options;

    public AdminDbConnectionFactory(IOptions<RollbackAdminDatabasesOptions> options) =>
        _options = options.Value;

    public string WorldDatabaseName =>
        _options.World.DatabaseName;

    public string AuthDatabaseName =>
        _options.Auth.DatabaseName;

    public MySqlConnection CreateWorldConnection() =>
        CreateConnection(_options.World);

    public MySqlConnection CreateAuthConnection() =>
        CreateConnection(_options.Auth);

    private static MySqlConnection CreateConnection(Rollback.Common.ORM.Config.DatabaseConfiguration configuration) =>
        new($"{configuration.ConnectionString}Connection Timeout=15;TreatTinyAsBoolean=true;");
}
