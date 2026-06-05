using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;

namespace Rollback.Admin.Services;

public sealed class AdminEntityTextOverrideService
{
    private readonly AdminDbConnectionFactory _connectionFactory;

    public AdminEntityTextOverrideService(AdminDbConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    public async Task<AdminEntityTextOverride?> GetAsync(
        AdminEntityType entityType,
        int entityId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        return await GetAsync(connection, entityType, entityId, cancellationToken);
    }

    public async Task<Dictionary<int, AdminEntityTextOverride>> GetManyAsync(
        AdminEntityType entityType,
        IEnumerable<int> entityIds,
        CancellationToken cancellationToken = default)
    {
        var ids = entityIds.Distinct().Where(x => x > 0).ToArray();
        if (ids.Length == 0)
            return new Dictionary<int, AdminEntityTextOverride>();

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        return await GetManyAsync(connection, entityType, ids, cancellationToken);
    }

    public async Task SaveAsync(
        AdminEntityType entityType,
        int entityId,
        string? displayName,
        string? description,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await SaveAsync(connection, entityType, entityId, displayName, description, transaction: null, cancellationToken);
    }

    internal static async Task<AdminEntityTextOverride?> GetAsync(
        MySqlConnection connection,
        AdminEntityType entityType,
        int entityId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EntityType, EntityId, DisplayName, Description
            FROM admin_entity_text_overrides
            WHERE EntityType = @entityType AND EntityId = @entityId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@entityType", entityType.ToString());
        command.Parameters.AddWithValue("@entityId", entityId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var result = new AdminEntityTextOverride
        {
            EntityType = entityType,
            EntityId = reader.GetSafeInt32("EntityId"),
            DisplayName = reader.GetSafeString("DisplayName"),
            Description = reader.GetSafeString("Description"),
        };

        return result;
    }

    internal static async Task<Dictionary<int, AdminEntityTextOverride>> GetManyAsync(
        MySqlConnection connection,
        AdminEntityType entityType,
        IReadOnlyCollection<int> entityIds,
        CancellationToken cancellationToken)
    {
        var idsCsv = string.Join(",", entityIds);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT EntityId, DisplayName, Description
            FROM admin_entity_text_overrides
            WHERE EntityType = @entityType
              AND EntityId IN ({idsCsv});
            """;
        command.Parameters.AddWithValue("@entityType", entityType.ToString());

        var result = new Dictionary<int, AdminEntityTextOverride>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var entityId = reader.GetSafeInt32("EntityId");
                result[entityId] = new AdminEntityTextOverride
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    DisplayName = reader.GetSafeString("DisplayName"),
                    Description = reader.GetSafeString("Description"),
                };
            }
        }

        return result;
    }

    internal static async Task SaveAsync(
        MySqlConnection connection,
        AdminEntityType entityType,
        int entityId,
        string? displayName,
        string? description,
        MySqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var normalizedName = (displayName ?? string.Empty).Trim();
        var normalizedDescription = (description ?? string.Empty).Trim();

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        if (string.IsNullOrWhiteSpace(normalizedName) && string.IsNullOrWhiteSpace(normalizedDescription))
        {
            command.CommandText = """
                DELETE FROM admin_entity_text_overrides
                WHERE EntityType = @entityType AND EntityId = @entityId;
                """;
        }
        else
        {
            command.CommandText = """
                INSERT INTO admin_entity_text_overrides (EntityType, EntityId, DisplayName, Description)
                VALUES (@entityType, @entityId, @displayName, @description)
                ON DUPLICATE KEY UPDATE
                    DisplayName = VALUES(DisplayName),
                    Description = VALUES(Description),
                    UpdatedAt = CURRENT_TIMESTAMP;
                """;
            command.Parameters.AddWithValue("@displayName", normalizedName);
            command.Parameters.AddWithValue("@description", normalizedDescription);
        }

        command.Parameters.AddWithValue("@entityType", entityType.ToString());
        command.Parameters.AddWithValue("@entityId", entityId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
