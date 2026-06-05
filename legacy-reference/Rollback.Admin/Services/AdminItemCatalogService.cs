using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.Items;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class AdminItemCatalogService
{
    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly GameAssetPreviewService _assetPreviewService;
    private readonly ClientItemLocalizationService _localizationService = new();

    public AdminItemCatalogService(
        AdminDbConnectionFactory connectionFactory,
        GameAssetPreviewService assetPreviewService)
    {
        _connectionFactory = connectionFactory;
        _assetPreviewService = assetPreviewService;
    }

    public async Task<IReadOnlyList<ItemListItem>> SearchAsync(
        ItemCatalogQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var normalized = Normalize(query);
        var items = await QueryItemsAsync(connection, normalized, cancellationToken);

        if (normalized.MaxResults > 0)
            return items.Take(normalized.MaxResults).ToArray();

        return items;
    }

    public async Task<IReadOnlyDictionary<short, ItemListItem>> GetByIdsAsync(
        IEnumerable<short> itemIds,
        CancellationToken cancellationToken = default)
    {
        var normalizedIds = itemIds
            .Where(x => x > 0)
            .Distinct()
            .ToArray();

        if (normalizedIds.Length == 0)
            return new Dictionary<short, ItemListItem>();

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var idFilter = string.Join(",", normalizedIds);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                i.Id,
                i.TypeId,
                i.Level,
                i.Price,
                i.ItemSetId,
                i.AppearanceId,
                ao.DisplayName AS OverrideName,
                ao.Description AS OverrideDescription,
                aa.RelativePath AS ManualAssetPath
            FROM items_templates i
            LEFT JOIN admin_entity_text_overrides ao
                ON ao.EntityType = @entityType
               AND ao.EntityId = i.Id
            LEFT JOIN admin_entity_asset_overrides aa
                ON aa.EntityType = @entityType
               AND aa.EntityId = i.Id
               AND aa.AssetKind = @assetKind
            WHERE i.Id IN ({idFilter})
            ORDER BY i.Level DESC, i.Id DESC;
            """;
        command.Parameters.AddWithValue("@entityType", AdminEntityType.Item.ToString());
        command.Parameters.AddWithValue("@assetKind", AdminEntityAssetOverrideService.PreviewPngKind);

        var rows = new Dictionary<short, ItemListItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var item = MapItem(reader);
            rows[item.Id] = item;
        }

        return rows;
    }

    public static bool MatchesSearch(
        string search,
        short itemId,
        ItemType type,
        short? dbAppearanceId,
        AdminResolvedText resolved,
        AdminClientItemText clientText,
        string? manualAssetRelativePath)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        var normalized = search.Trim();
        var iconFileName = clientText.IconId is > 0
            ? $"{clientText.IconId}.png"
            : string.Empty;

        return Contains(itemId.ToString(), normalized) ||
               Contains(((short)type).ToString(), normalized) ||
               Contains(ItemTypeLabelService.GetDisplayName(type), normalized) ||
               Contains(resolved.DisplayName, normalized) ||
               Contains(resolved.Description, normalized) ||
               Contains(clientText.Name, normalized) ||
               Contains(clientText.Description, normalized) ||
               Contains(clientText.IconId?.ToString(), normalized) ||
               Contains(clientText.ClientAppearanceId?.ToString(), normalized) ||
               Contains(dbAppearanceId?.ToString(), normalized) ||
               Contains(iconFileName, normalized) ||
               Contains(manualAssetRelativePath, normalized);
    }

    public static AdminResolvedText ResolveText(
        AdminClientItemText clientText,
        string overrideName,
        string overrideDescription,
        short itemId)
    {
        var hasOverride = !string.IsNullOrWhiteSpace(overrideName) || !string.IsNullOrWhiteSpace(overrideDescription);
        var resolvedName = !string.IsNullOrWhiteSpace(overrideName)
            ? overrideName.Trim()
            : !string.IsNullOrWhiteSpace(clientText.Name)
                ? clientText.Name
                : $"Item #{itemId}";

        var resolvedDescription = !string.IsNullOrWhiteSpace(overrideDescription)
            ? overrideDescription.Trim()
            : clientText.Description;

        return new AdminResolvedText
        {
            DisplayName = resolvedName,
            Description = resolvedDescription,
            SourceLabel = hasOverride
                ? "Manual"
                : !string.IsNullOrWhiteSpace(clientText.Name) || !string.IsNullOrWhiteSpace(clientText.Description)
                    ? "Cliente ES"
                    : "Fallback",
            ClientDisplayName = clientText.Name,
            ClientDescription = clientText.Description,
            OverrideDisplayName = overrideName,
            OverrideDescription = overrideDescription,
        };
    }

    public static string BuildManualAssetUrl(string relativePath) =>
        string.IsNullOrWhiteSpace(relativePath)
            ? string.Empty
            : $"/admin-assets/{relativePath.Trim().Replace('\\', '/')}";

    private async Task<List<ItemListItem>> QueryItemsAsync(
        MySqlConnection connection,
        ItemCatalogQuery query,
        CancellationToken cancellationToken)
    {
        var search = query.Search.Trim();
        var typeFilter = query.Types.Count > 0;
        var excludedFilter = query.ExcludedItemIds.Count > 0;
        var typeIds = typeFilter ? string.Join(",", query.Types.Select(x => (short)x)) : string.Empty;
        var excludedIds = excludedFilter ? string.Join(",", query.ExcludedItemIds) : string.Empty;

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                i.Id,
                i.TypeId,
                i.Level,
                i.Price,
                i.ItemSetId,
                i.AppearanceId,
                ao.DisplayName AS OverrideName,
                ao.Description AS OverrideDescription,
                aa.RelativePath AS ManualAssetPath
            FROM items_templates i
            LEFT JOIN admin_entity_text_overrides ao
                ON ao.EntityType = @entityType
               AND ao.EntityId = i.Id
            LEFT JOIN admin_entity_asset_overrides aa
                ON aa.EntityType = @entityType
               AND aa.EntityId = i.Id
               AND aa.AssetKind = @assetKind
            WHERE (@minLevel IS NULL OR i.Level >= @minLevel)
              AND (@maxLevel IS NULL OR i.Level <= @maxLevel)
              {(typeFilter ? $"AND i.TypeId IN ({typeIds})" : string.Empty)}
              {(excludedFilter ? $"AND i.Id NOT IN ({excludedIds})" : string.Empty)}
            ORDER BY i.Level DESC, i.Id DESC;
            """;
        command.Parameters.AddWithValue("@entityType", AdminEntityType.Item.ToString());
        command.Parameters.AddWithValue("@assetKind", AdminEntityAssetOverrideService.PreviewPngKind);
        command.Parameters.Add("@minLevel", MySqlDbType.Int16).Value = query.MinLevel.HasValue ? query.MinLevel.Value : DBNull.Value;
        command.Parameters.Add("@maxLevel", MySqlDbType.Int16).Value = query.MaxLevel.HasValue ? query.MaxLevel.Value : DBNull.Value;

        var items = new List<ItemListItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var item = MapItem(reader);
            var localization = _localizationService.Get(item.Id);
            var overrideName = reader.GetSafeString("OverrideName");
            var overrideDescription = reader.GetSafeString("OverrideDescription");
            var resolved = ResolveText(localization, overrideName, overrideDescription, item.Id);
            var manualAssetRelativePath = reader.GetSafeString("ManualAssetPath");
            if (!MatchesSearch(search, item.Id, item.TypeId, item.AppearanceId, resolved, localization, manualAssetRelativePath))
                continue;

            items.Add(item);
        }

        return items;
    }

    private ItemListItem MapItem(MySqlDataReader reader)
    {
        var id = reader.GetSafeInt16("Id");
        var type = (ItemType)reader.GetSafeInt16("TypeId");
        var localization = _localizationService.Get(id);
        var overrideName = reader.GetSafeString("OverrideName");
        var overrideDescription = reader.GetSafeString("OverrideDescription");
        var manualAssetRelativePath = reader.GetSafeString("ManualAssetPath");
        var dbAppearanceId = reader.GetSafeInt16("AppearanceId", -1);
        var resolved = ResolveText(localization, overrideName, overrideDescription, id);

        return new ItemListItem
        {
            Id = id,
            TypeId = type,
            TypeLabel = ItemTypeLabelService.GetDisplayName(type),
            Name = resolved.DisplayName,
            Description = resolved.Description,
            NameSourceLabel = resolved.SourceLabel,
            Level = reader.GetSafeInt16("Level"),
            Price = reader.GetSafeInt32("Price"),
            ItemSetId = reader.GetSafeInt16("ItemSetId", -1),
            AppearanceId = dbAppearanceId,
            ClientIconId = localization.IconId,
            ClientAppearanceId = localization.ClientAppearanceId,
            ManualPreviewImageUrl = BuildManualAssetUrl(manualAssetRelativePath),
            PreviewImageUrl = _assetPreviewService.ResolveItemPreviewUrl(id, dbAppearanceId),
            PreviewFallbackLabel = ItemTypeLabelService.GetShortCode(type),
        };
    }

    private static bool Contains(string? value, string search) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(search, StringComparison.OrdinalIgnoreCase);

    private static ItemCatalogQuery Normalize(ItemCatalogQuery query)
    {
        query.MaxResults = query.MaxResults switch
        {
            < 0 => 0,
            > 100 => 100,
            _ => query.MaxResults,
        };
        return query;
    }
}
