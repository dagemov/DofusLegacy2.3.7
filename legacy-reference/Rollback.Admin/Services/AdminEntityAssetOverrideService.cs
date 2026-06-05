using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;

namespace Rollback.Admin.Services;

public sealed class AdminEntityAssetOverrideService
{
    public const string PreviewPngKind = "PreviewPng";

    private readonly AdminDbConnectionFactory _connectionFactory;

    public AdminEntityAssetOverrideService(AdminDbConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    public async Task<AdminEntityAssetOverride?> GetAsync(
        AdminEntityType entityType,
        int entityId,
        string assetKind = PreviewPngKind,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        return await GetAsync(connection, entityType, entityId, assetKind, cancellationToken);
    }

    public async Task<Dictionary<int, AdminEntityAssetOverride>> GetManyAsync(
        AdminEntityType entityType,
        IEnumerable<int> entityIds,
        string assetKind = PreviewPngKind,
        CancellationToken cancellationToken = default)
    {
        var ids = entityIds.Distinct().Where(x => x > 0).ToArray();
        if (ids.Length == 0)
            return new Dictionary<int, AdminEntityAssetOverride>();

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        return await GetManyAsync(connection, entityType, ids, assetKind, cancellationToken);
    }

    public async Task SaveAsync(
        AdminEntityType entityType,
        int entityId,
        string? relativePath,
        string assetKind = PreviewPngKind,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await SaveAsync(connection, entityType, entityId, assetKind, relativePath, transaction: null, cancellationToken);
    }

    public async Task DeleteAsync(
        AdminEntityType entityType,
        int entityId,
        string assetKind = PreviewPngKind,
        CancellationToken cancellationToken = default)
    {
        await SaveAsync(entityType, entityId, relativePath: null, assetKind, cancellationToken);
    }

    internal static async Task<AdminEntityAssetOverride?> GetAsync(
        MySqlConnection connection,
        AdminEntityType entityType,
        int entityId,
        string assetKind,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EntityType, EntityId, AssetKind, RelativePath
            FROM admin_entity_asset_overrides
            WHERE EntityType = @entityType
              AND EntityId = @entityId
              AND AssetKind = @assetKind
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@entityType", entityType.ToString());
        command.Parameters.AddWithValue("@entityId", entityId);
        command.Parameters.AddWithValue("@assetKind", assetKind);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new AdminEntityAssetOverride
        {
            EntityType = entityType,
            EntityId = reader.GetSafeInt32("EntityId"),
            AssetKind = reader.GetSafeString("AssetKind"),
            RelativePath = reader.GetSafeString("RelativePath"),
        };
    }

    internal static async Task<Dictionary<int, AdminEntityAssetOverride>> GetManyAsync(
        MySqlConnection connection,
        AdminEntityType entityType,
        IReadOnlyCollection<int> entityIds,
        string assetKind,
        CancellationToken cancellationToken)
    {
        var idsCsv = string.Join(",", entityIds);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT EntityId, AssetKind, RelativePath
            FROM admin_entity_asset_overrides
            WHERE EntityType = @entityType
              AND AssetKind = @assetKind
              AND EntityId IN ({idsCsv});
            """;
        command.Parameters.AddWithValue("@entityType", entityType.ToString());
        command.Parameters.AddWithValue("@assetKind", assetKind);

        var result = new Dictionary<int, AdminEntityAssetOverride>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var entityId = reader.GetSafeInt32("EntityId");
            result[entityId] = new AdminEntityAssetOverride
            {
                EntityType = entityType,
                EntityId = entityId,
                AssetKind = reader.GetSafeString("AssetKind"),
                RelativePath = reader.GetSafeString("RelativePath"),
            };
        }

        return result;
    }

    internal static async Task SaveAsync(
        MySqlConnection connection,
        AdminEntityType entityType,
        int entityId,
        string assetKind,
        string? relativePath,
        MySqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var normalizedPath = (relativePath ?? string.Empty).Trim().Replace('\\', '/');

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            command.CommandText = """
                DELETE FROM admin_entity_asset_overrides
                WHERE EntityType = @entityType
                  AND EntityId = @entityId
                  AND AssetKind = @assetKind;
                """;
        }
        else
        {
            command.CommandText = """
                INSERT INTO admin_entity_asset_overrides (EntityType, EntityId, AssetKind, RelativePath)
                VALUES (@entityType, @entityId, @assetKind, @relativePath)
                ON DUPLICATE KEY UPDATE
                    RelativePath = VALUES(RelativePath),
                    UpdatedAt = CURRENT_TIMESTAMP;
                """;
            command.Parameters.AddWithValue("@relativePath", normalizedPath);
        }

        command.Parameters.AddWithValue("@entityType", entityType.ToString());
        command.Parameters.AddWithValue("@entityId", entityId);
        command.Parameters.AddWithValue("@assetKind", assetKind);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
