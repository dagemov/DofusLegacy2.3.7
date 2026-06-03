using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.Vendors;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class NpcVendorAdminService
{
    private sealed record RuntimeItemSnapshot(
        short ItemId,
        short ItemSetId,
        ItemType TypeId,
        short Level,
        int Price,
        string DisplayName,
        string SetName);

    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly NpcVendorCatalogService _catalogService;
    private readonly NpcVendorInventorySyncService _syncService;
    private readonly CustomItemClassificationService _customItemClassificationService;
    private readonly ClientItemLocalizationService _localizationService = new();

    public NpcVendorAdminService(
        AdminDbConnectionFactory connectionFactory,
        NpcVendorCatalogService catalogService,
        NpcVendorInventorySyncService syncService,
        CustomItemClassificationService customItemClassificationService)
    {
        _connectionFactory = connectionFactory;
        _catalogService = catalogService;
        _syncService = syncService;
        _customItemClassificationService = customItemClassificationService;
    }

    public async Task<AdminPagedResult<NpcVendorListItem>> GetPagedAsync(AdminPagedQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var normalized = Normalize(query);
        var search = normalized.Search.Trim();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                na.Id AS ShopActionId,
                na.NpcId,
                nt.Name AS TemplateName,
                COALESCE(MIN(ns.MapId), 0) AS MapId,
                COALESCE(MIN(ns.CellId), 0) AS CellId,
                COUNT(DISTINCT ni.ItemId) AS ItemCount,
                GROUP_CONCAT(DISTINCT CAST(it.TypeId AS CHAR) ORDER BY it.TypeId SEPARATOR ',') AS TypeIdsCsv
            FROM npcs_actions na
            LEFT JOIN npcs_templates nt ON nt.Id = na.NpcId
            INNER JOIN npcs_spawns ns ON ns.NpcId = na.NpcId
            LEFT JOIN npcs_items ni ON ni.ShopActionId = na.Id
            LEFT JOIN items_templates it ON it.Id = ni.ItemId
            WHERE na.Action = 'Shop'
            GROUP BY na.Id, na.NpcId
            ORDER BY na.NpcId, na.Id;
            """;

        var allItems = new List<NpcVendorListItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var npcId = reader.GetSafeInt16("NpcId");
            var categoryLabel = BuildCategoryLabel(reader.GetSafeString("TypeIdsCsv"));
            allItems.Add(new NpcVendorListItem
            {
                ShopActionId = reader.GetSafeInt32("ShopActionId"),
                NpcId = npcId,
                Name = _catalogService.ResolveVendorName(npcId, reader.GetSafeString("TemplateName"), categoryLabel),
                MapId = reader.GetSafeInt32("MapId"),
                CellId = reader.GetSafeInt16("CellId"),
                ItemCount = reader.GetSafeInt32("ItemCount"),
                CategoryLabel = categoryLabel,
            });
        }

        var filteredItems = string.IsNullOrWhiteSpace(search)
            ? allItems
            : allItems.Where(item =>
                    item.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.CategoryLabel.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.NpcId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.ShopActionId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.MapId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var totalCount = filteredItems.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)normalized.PageSize));
        var currentPage = Math.Min(normalized.Page, totalPages);
        var items = filteredItems
            .Skip((currentPage - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToArray();

        return new AdminPagedResult<NpcVendorListItem>(items, totalCount, currentPage, normalized.PageSize);
    }

    public async Task<NpcVendorEditModel?> GetByShopActionIdAsync(
        int shopActionId,
        NpcVendorItemsQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var normalizedQuery = Normalize(query ?? new NpcVendorItemsQuery());
        NpcVendorEditModel? model = null;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT
                    na.Id AS ShopActionId,
                    na.NpcId,
                    nt.Name AS TemplateName,
                    COALESCE(MIN(ns.MapId), 0) AS MapId,
                    COALESCE(MIN(ns.CellId), 0) AS CellId,
                    GROUP_CONCAT(DISTINCT CAST(it.TypeId AS CHAR) ORDER BY it.TypeId SEPARATOR ',') AS TypeIdsCsv
                FROM npcs_actions na
                LEFT JOIN npcs_templates nt ON nt.Id = na.NpcId
                LEFT JOIN npcs_spawns ns ON ns.NpcId = na.NpcId
                LEFT JOIN npcs_items ni ON ni.ShopActionId = na.Id
                LEFT JOIN items_templates it ON it.Id = ni.ItemId
                WHERE na.Action = 'Shop' AND na.Id = @shopActionId
                GROUP BY na.Id, na.NpcId, nt.Name;
                """;
            command.Parameters.AddWithValue("@shopActionId", shopActionId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            var npcId = reader.GetSafeInt16("NpcId");
            var categoryLabel = BuildCategoryLabel(reader.GetSafeString("TypeIdsCsv"));
            model = new NpcVendorEditModel
            {
                ShopActionId = reader.GetSafeInt32("ShopActionId"),
                NpcId = npcId,
                Name = _catalogService.ResolveVendorName(npcId, reader.GetSafeString("TemplateName"), categoryLabel),
                MapId = reader.GetSafeInt32("MapId"),
                CellId = reader.GetSafeInt16("CellId"),
                CategoryLabel = categoryLabel,
            };
        }

        var vendorItems = new List<NpcVendorItemEntry>();
        await using (var itemsCommand = connection.CreateCommand())
        {
            var clauses = new List<string> { "ni.ShopActionId = @shopActionId" };
            itemsCommand.Parameters.AddWithValue("@shopActionId", shopActionId);

            if (normalizedQuery.ItemId.HasValue)
            {
                clauses.Add("ni.ItemId = @itemId");
                itemsCommand.Parameters.AddWithValue("@itemId", normalizedQuery.ItemId.Value);
            }

            if (normalizedQuery.MinLevel.HasValue)
            {
                clauses.Add("it.Level >= @minLevel");
                itemsCommand.Parameters.AddWithValue("@minLevel", normalizedQuery.MinLevel.Value);
            }

            if (normalizedQuery.MaxLevel.HasValue)
            {
                clauses.Add("it.Level <= @maxLevel");
                itemsCommand.Parameters.AddWithValue("@maxLevel", normalizedQuery.MaxLevel.Value);
            }

            if (normalizedQuery.TypeId.HasValue)
            {
                clauses.Add("it.TypeId = @typeId");
                itemsCommand.Parameters.AddWithValue("@typeId", (short)normalizedQuery.TypeId.Value);
            }

            itemsCommand.CommandText = $"""
                SELECT
                    ni.Id,
                    ni.ShopActionId,
                    ni.ItemId,
                    COALESCE(ni.Price, it.Price, 0) AS Price,
                    it.TypeId,
                    it.Level
                FROM npcs_items ni
                INNER JOIN items_templates it ON it.Id = ni.ItemId
                WHERE {string.Join(" AND ", clauses)}
                ORDER BY it.Level, ni.ItemId;
                """;

            await using var reader = await itemsCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var itemId = reader.GetSafeInt16("ItemId");
                var type = (ItemType)reader.GetSafeInt16("TypeId");
                var localization = _localizationService.Get(itemId);
                vendorItems.Add(new NpcVendorItemEntry
                {
                    Id = reader.GetSafeInt32("Id"),
                    ShopActionId = reader.GetSafeInt32("ShopActionId"),
                    ItemId = itemId,
                    Name = string.IsNullOrWhiteSpace(localization.Name) ? $"Item #{itemId}" : localization.Name,
                    TypeId = type,
                    TypeLabel = ItemTypeLabelService.GetDisplayName(type),
                    Level = reader.GetSafeInt16("Level"),
                    Price = reader.GetSafeInt32("Price"),
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedQuery.Search))
        {
            var search = normalizedQuery.Search.Trim();
            vendorItems = vendorItems
                .Where(item =>
                    item.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.ItemId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.TypeLabel.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var totalCount = vendorItems.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)normalizedQuery.PageSize));
        var currentPage = Math.Min(normalizedQuery.Page, totalPages);
        model!.ItemPage = new AdminPagedResult<NpcVendorItemEntry>(
            vendorItems
                .Skip((currentPage - 1) * normalizedQuery.PageSize)
                .Take(normalizedQuery.PageSize)
                .ToArray(),
            totalCount,
            currentPage,
            normalizedQuery.PageSize);

        return model;
    }

    public async Task<NpcVendorAssignmentResult> AddOrUpdateItemAsync(
        int requestedShopActionId,
        short itemId,
        int price,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var item = await LoadRuntimeItemAsync(connection, itemId, cancellationToken)
            ?? throw new InvalidOperationException($"El item #{itemId} no existe en runtime.");

        var isRollBackItem = _customItemClassificationService.IsCustomItem(item.ItemId, item.ItemSetId, item.DisplayName, item.SetName);
        var requestedDefinition = _catalogService.GetDefinition(requestedShopActionId);
        if (requestedDefinition is not null &&
            !_catalogService.IsCompatible(requestedShopActionId, item.TypeId, item.Level, isRollBackItem))
        {
            if (_catalogService.IsRollBackVendor(requestedShopActionId))
                throw new InvalidOperationException("El vendor Sets RollBack solo acepta items equipables custom del servidor o pertenecientes a sets/custom content no presentes en la referencia sana.");

            var preferredShopActionId = _catalogService.ResolvePreferredShopActionId(item.TypeId, item.Level);
            if (!preferredShopActionId.HasValue)
                throw new InvalidOperationException("No existe un vendor compatible para la categoria/nivel actual del item.");

            requestedShopActionId = preferredShopActionId.Value;
        }

        int? existingId = null;
        await using (var exists = connection.CreateCommand())
        {
            exists.CommandText = "SELECT Id FROM npcs_items WHERE ShopActionId = @shopActionId AND ItemId = @itemId LIMIT 1;";
            exists.Parameters.AddWithValue("@shopActionId", requestedShopActionId);
            exists.Parameters.AddWithValue("@itemId", itemId);
            var scalar = await exists.ExecuteScalarAsync(cancellationToken);
            existingId = scalar is null ? null : Convert.ToInt32(scalar);
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = existingId.HasValue
                ? """
                  UPDATE npcs_items
                  SET Price = @price,
                      EffectGenerationType = 0
                  WHERE Id = @id;
                  """
                : """
                  INSERT INTO npcs_items (ShopActionId, ItemId, Price, EffectGenerationType)
                  VALUES (@shopActionId, @itemId, @price, 0);
                  """;
            if (existingId.HasValue)
                command.Parameters.AddWithValue("@id", existingId.Value);
            command.Parameters.AddWithValue("@shopActionId", requestedShopActionId);
            command.Parameters.AddWithValue("@itemId", itemId);
            command.Parameters.AddWithValue("@price", Math.Max(0, price));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        var effectiveDefinition = _catalogService.GetDefinition(requestedShopActionId);
        var redirected = requestedDefinition is not null && requestedDefinition.ShopActionId != requestedShopActionId;
        return new NpcVendorAssignmentResult
        {
            EffectiveShopActionId = requestedShopActionId,
            EffectiveNpcId = effectiveDefinition?.NpcId ?? 0,
            EffectiveVendorName = effectiveDefinition?.DisplayName ?? $"ShopAction {requestedShopActionId}",
            Redirected = redirected,
            Message = redirected
                ? $"Item #{itemId} redirigido automaticamente a {effectiveDefinition?.DisplayName}."
                : $"Item #{itemId} publicado en {effectiveDefinition?.DisplayName ?? $"ShopAction {requestedShopActionId}"}."
        };
    }

    public async Task RemoveItemAsync(int vendorItemId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM npcs_items WHERE Id = @vendorItemId;";
        command.Parameters.AddWithValue("@vendorItemId", vendorItemId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> SyncSuggestedCategoryAsync(int shopActionId, CancellationToken cancellationToken = default)
    {
        if (_catalogService.IsRollBackVendor(shopActionId))
            return await _syncService.SyncRollBackVendorAsync(cancellationToken);

        var rule = _catalogService.GetDefinition(shopActionId);
        if (rule is null)
            return 0;

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var typeIdsCsv = string.Join(",", rule.Types.Select(x => (short)x));
            var minLevelClause = rule.MinLevel.HasValue ? " OR it.Level < @minLevel" : string.Empty;
            var maxLevelClause = rule.MaxLevel.HasValue ? " OR it.Level > @maxLevel" : string.Empty;
            var levelWhereClauses = new List<string>();
            if (rule.MinLevel.HasValue)
                levelWhereClauses.Add("Level >= @minLevel");
            if (rule.MaxLevel.HasValue)
                levelWhereClauses.Add("Level <= @maxLevel");
            var levelWhere = levelWhereClauses.Count == 0
                ? string.Empty
                : $" AND {string.Join(" AND ", levelWhereClauses)}";

            await using (var deleteWrong = connection.CreateCommand())
            {
                deleteWrong.Transaction = transaction;
                deleteWrong.CommandText = $"""
                    DELETE ni
                    FROM npcs_items ni
                    INNER JOIN items_templates it ON it.Id = ni.ItemId
                    WHERE ni.ShopActionId = @shopActionId
                      AND (it.TypeId NOT IN ({typeIdsCsv}){minLevelClause}{maxLevelClause});
                    """;
                deleteWrong.Parameters.AddWithValue("@shopActionId", shopActionId);
                if (rule.MinLevel.HasValue)
                    deleteWrong.Parameters.AddWithValue("@minLevel", rule.MinLevel.Value);
                if (rule.MaxLevel.HasValue)
                    deleteWrong.Parameters.AddWithValue("@maxLevel", rule.MaxLevel.Value);
                await deleteWrong.ExecuteNonQueryAsync(cancellationToken);
            }

            var processed = 0;
            await using var itemsCommand = connection.CreateCommand();
            itemsCommand.Transaction = transaction;
            itemsCommand.CommandText = $"""
                SELECT Id, Price
                FROM items_templates
                WHERE TypeId IN ({typeIdsCsv})
                {levelWhere}
                ORDER BY Level, Id;
                """;
            if (rule.MinLevel.HasValue)
                itemsCommand.Parameters.AddWithValue("@minLevel", rule.MinLevel.Value);
            if (rule.MaxLevel.HasValue)
                itemsCommand.Parameters.AddWithValue("@maxLevel", rule.MaxLevel.Value);

            await using var reader = await itemsCommand.ExecuteReaderAsync(cancellationToken);
            var toInsert = new List<(short ItemId, int Price)>();
            while (await reader.ReadAsync(cancellationToken))
                toInsert.Add((reader.GetSafeInt16("Id"), Math.Max(0, reader.GetSafeInt32("Price"))));
            await reader.CloseAsync();

            foreach (var item in toInsert)
            {
                processed++;
                await using var upsert = connection.CreateCommand();
                upsert.Transaction = transaction;
                upsert.CommandText = """
                    INSERT INTO npcs_items (ShopActionId, ItemId, Price, EffectGenerationType)
                    VALUES (@shopActionId, @itemId, @price, 0)
                    ON DUPLICATE KEY UPDATE
                        Price = VALUES(Price),
                        EffectGenerationType = 0;
                    """;
                upsert.Parameters.AddWithValue("@shopActionId", shopActionId);
                upsert.Parameters.AddWithValue("@itemId", item.ItemId);
                upsert.Parameters.AddWithValue("@price", item.Price);
                await upsert.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return processed;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public Task<string?> SyncItemPlacementsAsync(short itemId, CancellationToken cancellationToken = default) =>
        _syncService.SyncItemAsync(itemId, cancellationToken);

    public IReadOnlyDictionary<int, string> GetSuggestedCatalogLabels() =>
        _catalogService.GetSuggestedCatalogLabels();

    public IReadOnlyList<ItemType> GetSuggestedTypes(int shopActionId) =>
        _catalogService.GetSuggestedTypes(shopActionId);

    public IReadOnlyList<ItemType> GetSupportedFilterTypes(int shopActionId) =>
        _catalogService.GetSupportedFilterTypes(shopActionId);

    private static string BuildCategoryLabel(string typeIdsCsv)
    {
        var labels = typeIdsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => short.TryParse(x, out _))
            .Select(short.Parse)
            .Distinct()
            .Take(3)
            .Select(x => ItemTypeLabelService.GetDisplayName((ItemType)x).ToLowerInvariant())
            .ToArray();

        if (labels.Length == 0)
            return "sin inventario";

        return string.Join(", ", labels);
    }

    private static AdminPagedQuery Normalize(AdminPagedQuery query)
    {
        query.Page = query.Page <= 0 ? 1 : query.Page;
        query.PageSize = query.PageSize switch
        {
            <= 0 => 25,
            > 100 => 100,
            _ => query.PageSize,
        };
        return query;
    }

    private static NpcVendorItemsQuery Normalize(NpcVendorItemsQuery query)
    {
        query.Page = query.Page <= 0 ? 1 : query.Page;
        query.PageSize = query.PageSize switch
        {
            <= 0 => 10,
            > 100 => 100,
            _ => query.PageSize,
        };
        return query;
    }

    private static async Task<RuntimeItemSnapshot?> LoadRuntimeItemAsync(
        MySqlConnection connection,
        short itemId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                it.Id,
                COALESCE(it.ItemSetId, 0) AS ItemSetId,
                it.TypeId,
                it.Level,
                COALESCE(it.Price, 0) AS Price,
                COALESCE(ao.DisplayName, '') AS DisplayName,
                COALESCE(s.Name, '') AS SetName
            FROM items_templates it
            LEFT JOIN admin_entity_text_overrides ao
                ON ao.EntityType = 'Item'
               AND ao.EntityId = it.Id
            LEFT JOIN items_sets s ON s.Id = it.ItemSetId
            WHERE it.Id = @itemId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@itemId", itemId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new RuntimeItemSnapshot(
            reader.GetSafeInt16("Id"),
            reader.GetSafeInt16("ItemSetId"),
            (ItemType)reader.GetSafeInt16("TypeId"),
            reader.GetSafeInt16("Level"),
            reader.GetSafeInt32("Price"),
            reader.GetSafeString("DisplayName"),
            reader.GetSafeString("SetName"));
    }
}
