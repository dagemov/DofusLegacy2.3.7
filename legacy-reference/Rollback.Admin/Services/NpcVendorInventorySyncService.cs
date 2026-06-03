using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class NpcVendorInventorySyncService
{
    private sealed record RuntimeItemSnapshot(
        short ItemId,
        short ItemSetId,
        ItemType TypeId,
        short Level,
        int Price,
        string DisplayName,
        string SetName);

    private sealed record VendorLink(int Id, int ShopActionId, int Price);

    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly NpcVendorCatalogService _catalogService;
    private readonly CustomItemClassificationService _customItemClassificationService;

    public NpcVendorInventorySyncService(
        AdminDbConnectionFactory connectionFactory,
        NpcVendorCatalogService catalogService,
        CustomItemClassificationService customItemClassificationService)
    {
        _connectionFactory = connectionFactory;
        _catalogService = catalogService;
        _customItemClassificationService = customItemClassificationService;
    }

    public async Task<string?> SyncItemAsync(short itemId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var item = await LoadItemAsync(connection, transaction, itemId, cancellationToken);
            if (item is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return null;
            }

            var vendorLinks = await LoadVendorLinksAsync(connection, transaction, itemId, cancellationToken);
            var wasAssignedToStandardVendor = vendorLinks.Any(link => !_catalogService.IsRollBackVendor(link.ShopActionId));
            var isRollBackItem = _customItemClassificationService.IsCustomItem(item.ItemId, item.ItemSetId, item.DisplayName, item.SetName);
            var invalidLinks = vendorLinks
                .Where(link => !_catalogService.IsCompatible(link.ShopActionId, item.TypeId, item.Level, isRollBackItem))
                .ToArray();

            foreach (var invalidLink in invalidLinks)
                await DeleteVendorLinkAsync(connection, transaction, invalidLink.Id, cancellationToken);

            var preferredStandardShopActionId = _catalogService.ResolvePreferredShopActionId(item.TypeId, item.Level);
            var validStandardLinks = vendorLinks
                .Except(invalidLinks)
                .Where(link => !_catalogService.IsRollBackVendor(link.ShopActionId))
                .ToArray();

            var messages = new List<string>();

            if (wasAssignedToStandardVendor && preferredStandardShopActionId.HasValue)
            {
                foreach (var extraLink in validStandardLinks.Where(link => link.ShopActionId != preferredStandardShopActionId.Value))
                    await DeleteVendorLinkAsync(connection, transaction, extraLink.Id, cancellationToken);

                if (!validStandardLinks.Any(link => link.ShopActionId == preferredStandardShopActionId.Value))
                {
                    var sourcePrice = vendorLinks.Select(link => link.Price).FirstOrDefault(price => price > 0);
                    await UpsertVendorLinkAsync(
                        connection,
                        transaction,
                        preferredStandardShopActionId.Value,
                        item.ItemId,
                        sourcePrice > 0 ? sourcePrice : item.Price,
                        cancellationToken);

                    if (invalidLinks.Length > 0)
                    {
                        var destination = _catalogService.GetDefinition(preferredStandardShopActionId.Value);
                        if (destination is not null)
                            messages.Add($"Categoria resynchronizada: item movido a {destination.DisplayName}.");
                    }
                }
            }

            if (isRollBackItem && _catalogService.SupportsRollBackType(item.TypeId))
            {
                var rollBackPrice = vendorLinks.Select(link => link.Price).FirstOrDefault(price => price > 0);
                await UpsertVendorLinkAsync(
                    connection,
                    transaction,
                    NpcVendorCatalogService.RollBackShopActionId,
                    item.ItemId,
                    rollBackPrice > 0 ? rollBackPrice : item.Price,
                    cancellationToken);
            }
            else
            {
                foreach (var specialLink in vendorLinks.Where(link => _catalogService.IsRollBackVendor(link.ShopActionId)))
                    await DeleteVendorLinkAsync(connection, transaction, specialLink.Id, cancellationToken);
            }

            if (vendorLinks.Count > 0)
                messages.Add($"Vendor refresh forced after template update for item {item.ItemId}; las compras nuevas usaran el template actual.");

            await transaction.CommitAsync(cancellationToken);
            return messages.Count == 0 ? null : string.Join(" ", messages);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<int> SyncRollBackVendorAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var rollBackItems = new List<RuntimeItemSnapshot>();
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
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
                    ORDER BY it.Level, it.Id;
                    """;

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var snapshot = new RuntimeItemSnapshot(
                        reader.GetSafeInt16("Id"),
                        reader.GetSafeInt16("ItemSetId"),
                        (ItemType)reader.GetSafeInt16("TypeId"),
                        reader.GetSafeInt16("Level"),
                        reader.GetSafeInt32("Price"),
                        reader.GetSafeString("DisplayName"),
                        reader.GetSafeString("SetName"));

                    if (_customItemClassificationService.IsRollBackVendorEligible(snapshot.ItemId, snapshot.ItemSetId, snapshot.TypeId, snapshot.DisplayName, snapshot.SetName))
                        rollBackItems.Add(snapshot);
                }
            }

            var expectedItemIds = rollBackItems.Select(item => item.ItemId).ToHashSet();

            await using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = """
                    DELETE ni
                    FROM npcs_items ni
                    WHERE ni.ShopActionId = @shopActionId
                      AND ni.ItemId NOT IN (
                          SELECT ItemId
                          FROM (
                              SELECT CAST(@expectedCsvItemId AS SIGNED) AS ItemId
                          ) expected_ids
                      );
                    """;

                if (expectedItemIds.Count == 0)
                {
                    deleteCommand.CommandText = "DELETE FROM npcs_items WHERE ShopActionId = @shopActionId;";
                }
                else
                {
                    var expectedParameters = new List<string>();
                    short index = 0;
                    deleteCommand.CommandText = "DELETE FROM npcs_items WHERE ShopActionId = @shopActionId AND ItemId NOT IN (" +
                                                string.Join(",", expectedItemIds.Select(itemId =>
                                                {
                                                    var parameterName = $"@itemId{index++}";
                                                    deleteCommand.Parameters.AddWithValue(parameterName, itemId);
                                                    return parameterName;
                                                })) +
                                                ");";
                }

                deleteCommand.Parameters.AddWithValue("@shopActionId", NpcVendorCatalogService.RollBackShopActionId);
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var item in rollBackItems)
            {
                await UpsertVendorLinkAsync(
                    connection,
                    transaction,
                    NpcVendorCatalogService.RollBackShopActionId,
                    item.ItemId,
                    item.Price,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return rollBackItems.Count;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task DeleteVendorLinkAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int vendorItemId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM npcs_items WHERE Id = @vendorItemId;";
        command.Parameters.AddWithValue("@vendorItemId", vendorItemId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertVendorLinkAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int shopActionId,
        short itemId,
        int price,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO npcs_items (ShopActionId, ItemId, Price, EffectGenerationType)
            VALUES (@shopActionId, @itemId, @price, 0)
            ON DUPLICATE KEY UPDATE
                Price = VALUES(Price),
                EffectGenerationType = 0;
            """;
        command.Parameters.AddWithValue("@shopActionId", shopActionId);
        command.Parameters.AddWithValue("@itemId", itemId);
        command.Parameters.AddWithValue("@price", Math.Max(0, price));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<RuntimeItemSnapshot?> LoadItemAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        short itemId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
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

    private static async Task<List<VendorLink>> LoadVendorLinksAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        short itemId,
        CancellationToken cancellationToken)
    {
        var result = new List<VendorLink>();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT Id, ShopActionId, Price
            FROM npcs_items
            WHERE ItemId = @itemId;
            """;
        command.Parameters.AddWithValue("@itemId", itemId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new VendorLink(
                reader.GetSafeInt32("Id"),
                reader.GetSafeInt32("ShopActionId"),
                reader.GetSafeInt32("Price")));

        return result;
    }
}
