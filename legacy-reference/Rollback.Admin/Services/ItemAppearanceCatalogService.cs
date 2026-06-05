using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Assets;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.Items;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class ItemAppearanceCatalogService
{
    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly GameAssetPreviewService _assetPreviewService;
    private readonly ClientItemLocalizationService _clientLocalizationService;

    public ItemAppearanceCatalogService(
        AdminDbConnectionFactory connectionFactory,
        GameAssetPreviewService assetPreviewService)
    {
        _connectionFactory = connectionFactory;
        _assetPreviewService = assetPreviewService;
        _clientLocalizationService = new ClientItemLocalizationService();
    }

    public async Task<IReadOnlyList<AppearanceOption>> GetOptionsAsync(
        ItemType itemType,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var sampleRows = new List<(short AppearanceId, short SampleItemId, short SampleLevel, int UsageCount)>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT
                    i.AppearanceId,
                    MIN(i.Id) AS SampleItemId,
                    MIN(i.Level) AS SampleLevel,
                    COUNT(*) AS UsageCount
                FROM items_templates i
                WHERE i.TypeId = @typeId
                  AND i.AppearanceId > 0
                GROUP BY i.AppearanceId
                ORDER BY i.AppearanceId;
                """;
            command.Parameters.AddWithValue("@typeId", (short)itemType);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                sampleRows.Add((
                    reader.GetSafeInt16("AppearanceId"),
                    reader.GetSafeInt16("SampleItemId"),
                    reader.GetSafeInt16("SampleLevel"),
                    reader.GetSafeInt32("UsageCount")));
            }
        }

        if (sampleRows.Count == 0)
            return Array.Empty<AppearanceOption>();

        var sampleIds = sampleRows.Select(x => (int)x.SampleItemId).ToArray();
        var overrides = await AdminEntityTextOverrideService.GetManyAsync(connection, AdminEntityType.Item, sampleIds, cancellationToken);

        return sampleRows
            .Select(row =>
            {
                var clientText = _clientLocalizationService.Get(row.SampleItemId);
                overrides.TryGetValue(row.SampleItemId, out var overrideText);
                var label = ResolveLabel(row.SampleItemId, row.SampleLevel, row.UsageCount, overrideText, clientText);

                return new AppearanceOption
                {
                    AppearanceId = row.AppearanceId,
                    Label = label,
                    PreviewUrl = _assetPreviewService.ResolveItemPreviewUrl(row.SampleItemId, row.AppearanceId),
                };
            })
            .ToArray();
    }

    public async Task<bool> IsValidForTypeAsync(
        ItemType itemType,
        short appearanceId,
        CancellationToken cancellationToken = default)
    {
        if (appearanceId <= 0)
            return true;

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT 1
            FROM items_templates
            WHERE TypeId = @typeId AND AppearanceId = @appearanceId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@typeId", (short)itemType);
        command.Parameters.AddWithValue("@appearanceId", appearanceId);

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null and not DBNull;
    }

    public async Task<short?> GetSuggestedAppearanceAsync(
        ItemType itemType,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT i.AppearanceId
            FROM items_templates i
            WHERE i.TypeId = @typeId
              AND i.AppearanceId > 0
            GROUP BY i.AppearanceId
            ORDER BY COUNT(*) DESC, MIN(i.Level) ASC, MIN(i.Id) ASC, i.AppearanceId ASC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@typeId", (short)itemType);

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        if (scalar is null or DBNull)
            return null;

        return Convert.ToInt16(scalar);
    }

    public async Task<string?> GetValidationWarningAsync(
        ItemType itemType,
        short appearanceId,
        CancellationToken cancellationToken = default)
    {
        if (appearanceId <= 0)
            return null;

        if (await IsValidForTypeAsync(itemType, appearanceId, cancellationToken))
            return null;

        var options = await GetOptionsAsync(itemType, cancellationToken);
        if (options.Count == 0)
        {
            return $"AppearanceId {appearanceId} se guardo sin bloqueo porque este tipo no tiene un catalogo local de apariencias conocidas todavia.";
        }

        return $"AppearanceId {appearanceId} no aparece en el catalogo local para {ItemTypeLabelService.GetDisplayName(itemType)}. El panel lo guardo igualmente para no bloquear items custom.";
    }

    private static string ResolveLabel(
        short sampleItemId,
        short sampleLevel,
        int usageCount,
        AdminEntityTextOverride? overrideText,
        AdminClientItemText clientText)
    {
        var name = !string.IsNullOrWhiteSpace(overrideText?.DisplayName)
            ? overrideText.DisplayName
            : !string.IsNullOrWhiteSpace(clientText.Name)
                ? clientText.Name
                : $"Item #{sampleItemId}";

        return $"{name} · sample #{sampleItemId} · lvl {sampleLevel} · {usageCount} uso(s)";
    }
}
