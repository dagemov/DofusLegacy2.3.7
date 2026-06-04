using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;

namespace Rollback.Admin.Services;

public sealed class AdminEntityClientMetadataService
{
    private readonly AdminDbConnectionFactory _connectionFactory;

    public AdminEntityClientMetadataService(AdminDbConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    public async Task<AdminEntityClientMetadata?> GetAsync(
        AdminEntityType entityType,
        int entityId,
        string languageCode = "es",
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EntityType, EntityId, LanguageCode, NameId, DescriptionId, IconId, AppearanceId
            FROM admin_entity_client_metadata
            WHERE EntityType = @entityType
              AND EntityId = @entityId
              AND LanguageCode = @languageCode
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@entityType", entityType.ToString());
        command.Parameters.AddWithValue("@entityId", entityId);
        command.Parameters.AddWithValue("@languageCode", languageCode);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new AdminEntityClientMetadata
        {
            EntityType = entityType,
            EntityId = entityId,
            LanguageCode = string.IsNullOrWhiteSpace(reader.GetSafeString("LanguageCode")) ? languageCode : reader.GetSafeString("LanguageCode"),
            NameId = reader.GetSafeInt32("NameId"),
            DescriptionId = reader.GetSafeInt32("DescriptionId"),
            IconId = reader.GetSafeInt32("IconId"),
            AppearanceId = reader.GetSafeInt32("AppearanceId"),
        };
    }

    public async Task SaveAsync(
        AdminEntityType entityType,
        int entityId,
        int nameId,
        int descriptionId,
        int iconId,
        string languageCode = "es",
        CancellationToken cancellationToken = default) =>
        await SaveAsync(entityType, entityId, nameId, descriptionId, iconId, 0, languageCode, cancellationToken);

    public async Task SaveAsync(
        AdminEntityType entityType,
        int entityId,
        int nameId,
        int descriptionId,
        int iconId,
        int appearanceId,
        string languageCode = "es",
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await SaveAsync(connection, null, entityType, entityId, nameId, descriptionId, iconId, appearanceId, languageCode, cancellationToken);
    }

    public static async Task SaveAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        AdminEntityType entityType,
        int entityId,
        int nameId,
        int descriptionId,
        int iconId,
        string languageCode = "es",
        CancellationToken cancellationToken = default) =>
        await SaveAsync(connection, transaction, entityType, entityId, nameId, descriptionId, iconId, 0, languageCode, cancellationToken);

    public static async Task SaveAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        AdminEntityType entityType,
        int entityId,
        int nameId,
        int descriptionId,
        int iconId,
        int appearanceId,
        string languageCode = "es",
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO admin_entity_client_metadata
            (
                EntityType,
                EntityId,
                LanguageCode,
                NameId,
                DescriptionId,
                IconId,
                AppearanceId
            )
            VALUES
            (
                @entityType,
                @entityId,
                @languageCode,
                @nameId,
                @descriptionId,
                @iconId,
                @appearanceId
            )
            ON DUPLICATE KEY UPDATE
                NameId = VALUES(NameId),
                DescriptionId = VALUES(DescriptionId),
                IconId = VALUES(IconId),
                AppearanceId = VALUES(AppearanceId);
            """;
        command.Parameters.AddWithValue("@entityType", entityType.ToString());
        command.Parameters.AddWithValue("@entityId", entityId);
        command.Parameters.AddWithValue("@languageCode", languageCode);
        command.Parameters.AddWithValue("@nameId", nameId);
        command.Parameters.AddWithValue("@descriptionId", descriptionId);
        command.Parameters.AddWithValue("@iconId", iconId);
        command.Parameters.AddWithValue("@appearanceId", appearanceId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
