using MySqlConnector;
using Rollback.Admin.Infrastructure;

namespace Rollback.Admin.Services;

public sealed class AdminRuntimeRevisionService
{
    private readonly AdminDbConnectionFactory _connectionFactory;

    public AdminRuntimeRevisionService(AdminDbConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    public async Task TouchAsync(string domain, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await TouchOnConnectionAsync(connection, domain, cancellationToken);
    }

    public Task TouchAsync(
        MySqlConnection connection,
        string domain,
        CancellationToken cancellationToken = default) =>
        TouchOnConnectionAsync(connection, domain, cancellationToken);

    public static async Task TouchOnConnectionAsync(
        MySqlConnection connection,
        string domain,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("El dominio de runtime no puede ser vacio.", nameof(domain));

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO admin_runtime_revisions (Domain, Revision, UpdatedAt)
            VALUES (@domain, 1, UTC_TIMESTAMP())
            ON DUPLICATE KEY UPDATE
                Revision = Revision + 1,
                UpdatedAt = UTC_TIMESTAMP();
            """;
        command.Parameters.AddWithValue("@domain", domain.Trim());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
