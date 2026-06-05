using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.GameEffects;
using Rollback.Admin.Models.Items;

namespace Rollback.Admin.Services;

public sealed class SetAdminService
{
    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly AdminItemCatalogService _itemCatalogService;
    private readonly GameEffectEditorService _effectEditorService;
    private readonly ReferenceItemCatalogService _referenceCatalogService;
    private readonly SetClientPublishService _setClientPublishService;

    public SetAdminService(
        AdminDbConnectionFactory connectionFactory,
        AdminItemCatalogService itemCatalogService,
        GameEffectEditorService effectEditorService,
        ReferenceItemCatalogService referenceCatalogService,
        SetClientPublishService setClientPublishService)
    {
        _connectionFactory = connectionFactory;
        _itemCatalogService = itemCatalogService;
        _effectEditorService = effectEditorService;
        _referenceCatalogService = referenceCatalogService;
        _setClientPublishService = setClientPublishService;
    }

    public async Task<AdminPagedResult<ItemSetListItem>> GetPagedAsync(
        AdminPagedQuery query,
        int? itemCountFilter = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var normalized = Normalize(query);
        var search = normalized.Search.Trim();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, ItemsCSV, BinaryEffects
            FROM items_sets
            ORDER BY Id ASC;
            """;

        var sets = new List<ItemSetListItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var setId = reader.GetSafeInt16("Id");
            var runtimeName = reader.GetSafeString("Name");
            var itemsCsv = reader.GetSafeString("ItemsCSV");
            var referenceName = _referenceCatalogService.GetSet(setId)?.Name ?? string.Empty;
            var displayName = ResolveSetName(setId, runtimeName, referenceName);
            var itemCount = ParseItemsCsv(itemsCsv).Length;
            if (!MatchesSearch(search, setId, itemsCsv, displayName, runtimeName, referenceName))
                continue;

            if (itemCountFilter.HasValue && itemCount != itemCountFilter.Value)
                continue;

            sets.Add(new ItemSetListItem
            {
                Id = setId,
                Name = displayName,
                ReferenceName = referenceName,
                NameSourceLabel = !string.IsNullOrWhiteSpace(runtimeName)
                    ? "Runtime"
                    : !string.IsNullOrWhiteSpace(referenceName)
                        ? "Referencia"
                        : "Fallback",
                ItemCount = itemCount,
                BonusTierCount = _effectEditorService.DeserializeSet(reader.GetSafeBytes("BinaryEffects")).Count,
            });
        }

        var totalCount = sets.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)normalized.PageSize));
        var currentPage = Math.Min(normalized.Page, totalPages);
        var pagedItems = sets
            .Skip((currentPage - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToArray();

        return new AdminPagedResult<ItemSetListItem>(pagedItems, totalCount, currentPage, normalized.PageSize);
    }

    public async Task<ItemSetEditModel?> GetByIdAsync(short setId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, ItemsCSV, BinaryEffects FROM items_sets WHERE Id = @setId LIMIT 1;";
        command.Parameters.AddWithValue("@setId", setId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var binaryEffects = reader.GetSafeBytes("BinaryEffects");
        var runtimeName = reader.GetSafeString("Name");
        var referenceName = _referenceCatalogService.GetSet(setId)?.Name ?? string.Empty;
        var itemIds = ParseItemsCsv(reader.GetSafeString("ItemsCSV"));
        var catalog = await _itemCatalogService.GetByIdsAsync(itemIds, cancellationToken);
        var items = itemIds
            .Select(id => catalog.TryGetValue(id, out var item)
                ? item
                : new ItemListItem
                {
                    Id = id,
                    Name = $"Item #{id}",
                    TypeLabel = "Sin catalogo",
                    PreviewFallbackLabel = "IT",
                })
            .ToList();

        return new ItemSetEditModel
        {
            Id = reader.GetSafeInt16("Id"),
            Name = ResolveSetName(setId, runtimeName, referenceName),
            ReferenceName = referenceName,
            UsesRuntimeName = !string.IsNullOrWhiteSpace(runtimeName),
            Items = items,
            BonusTiers = _effectEditorService.DeserializeSet(binaryEffects),
            RawBinaryEffectsBase64 = Convert.ToBase64String(binaryEffects),
        };
    }

    public async Task<AdminSaveResult> SaveAsync(ItemSetEditModel model, CancellationToken cancellationToken = default)
    {
        var selectedIds = model.Items
            .Select(x => x.Id)
            .Where(x => x > 0)
            .Distinct()
            .ToArray();

        ValidateBonusTiers(model.BonusTiers, selectedIds.Length);

        var warnings = new List<string>();
        if (selectedIds.Length == 0)
            warnings.Add("El set se guardo sin items asociados. Existira en base, pero no otorgara bonuses reales hasta que agregues miembros.");

        var binaryEffects = _effectEditorService.SerializeSet(model.BonusTiers);
        var itemsCsv = string.Join(",", selectedIds);

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = """
                    INSERT INTO items_sets (Id, Name, ItemsCSV, BinaryEffects)
                    VALUES (@id, @name, @itemsCsv, @binaryEffects)
                    ON DUPLICATE KEY UPDATE
                        Name = VALUES(Name),
                        ItemsCSV = VALUES(ItemsCSV),
                        BinaryEffects = VALUES(BinaryEffects);
                    """;
                command.Parameters.AddWithValue("@id", model.Id);
                command.Parameters.AddWithValue("@name", (model.Name ?? string.Empty).Trim());
                command.Parameters.AddWithValue("@itemsCsv", itemsCsv);
                command.Parameters.Add("@binaryEffects", MySqlDbType.Blob).Value = binaryEffects;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            var otherSets = new List<(short Id, string ItemsCsv)>();
            await using (var queryOtherSets = connection.CreateCommand())
            {
                queryOtherSets.Transaction = transaction;
                queryOtherSets.CommandText = "SELECT Id, ItemsCSV FROM items_sets WHERE Id <> @setId;";
                queryOtherSets.Parameters.AddWithValue("@setId", model.Id);
                await using var reader = await queryOtherSets.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    otherSets.Add((reader.GetSafeInt16("Id"), reader.GetSafeString("ItemsCSV")));
            }

            foreach (var otherSet in otherSets)
            {
                var normalizedItems = ParseItemsCsv(otherSet.ItemsCsv)
                    .Except(selectedIds)
                    .ToArray();

                var updatedCsv = string.Join(",", normalizedItems);
                if (string.Equals(updatedCsv, otherSet.ItemsCsv, StringComparison.Ordinal))
                    continue;

                await using var updateOtherSet = connection.CreateCommand();
                updateOtherSet.Transaction = transaction;
                updateOtherSet.CommandText = "UPDATE items_sets SET ItemsCSV = @itemsCsv WHERE Id = @setId;";
                updateOtherSet.Parameters.AddWithValue("@setId", otherSet.Id);
                updateOtherSet.Parameters.AddWithValue("@itemsCsv", updatedCsv);
                await updateOtherSet.ExecuteNonQueryAsync(cancellationToken);
            }

            await SyncTemplateMembershipAsync(connection, transaction, model.Id, selectedIds, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var infos = new List<string>();
            var errors = new List<string>();

            try
            {
                var publishResult = await _setClientPublishService.PublishAsync(model.Id, cancellationToken);
                infos.Add(publishResult.Summary);
                if (publishResult.HasWarnings)
                    warnings.AddRange(publishResult.Warnings);
            }
            catch (Exception ex)
            {
                errors.Add($"El set #{model.Id} se guardo en runtime, pero no se pudo publicar su definicion cliente: {ex.Message}");
            }

            return warnings.Count == 0 && infos.Count == 0 && errors.Count == 0
                ? AdminSaveResult.Empty
                : new AdminSaveResult
                {
                    Warnings = warnings,
                    Infos = infos,
                    Errors = errors,
                };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteAsync(short setId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var clearItems = connection.CreateCommand())
            {
                clearItems.Transaction = transaction;
                clearItems.CommandText = "UPDATE items_templates SET ItemSetId = -1 WHERE ItemSetId = @setId;";
                clearItems.Parameters.AddWithValue("@setId", setId);
                await clearItems.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var deleteSet = connection.CreateCommand())
            {
                deleteSet.Transaction = transaction;
                deleteSet.CommandText = "DELETE FROM items_sets WHERE Id = @setId;";
                deleteSet.Parameters.AddWithValue("@setId", setId);
                await deleteSet.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static short[] ParseItemsCsv(string itemsCsv) =>
        itemsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => short.TryParse(value, out var itemId) ? itemId : (short)0)
            .Where(itemId => itemId > 0)
            .ToArray();

    private static bool MatchesSearch(string search, short setId, string itemsCsv, string displayName, string runtimeName, string referenceName)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        return setId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
               itemsCsv.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               displayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               runtimeName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               referenceName.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveSetName(short setId, string runtimeName, string referenceName)
    {
        if (!string.IsNullOrWhiteSpace(runtimeName))
            return runtimeName.Trim();

        if (!string.IsNullOrWhiteSpace(referenceName))
            return referenceName.Trim();

        return $"Set #{setId}";
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

    private static void ValidateBonusTiers(IReadOnlyCollection<GameEffectTierEditModel> tiers, int itemCount)
    {
        if (tiers.Count == 0)
            return;

        if (itemCount <= 0)
            throw new InvalidOperationException("No puedes guardar bonus de set sin items asociados.");

        var duplicateRequiredPieces = tiers
            .GroupBy(tier => tier.RequiredItemCount)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(x => x)
            .ToArray();
        if (duplicateRequiredPieces.Length > 0)
            throw new InvalidOperationException($"Hay tiers duplicados para {string.Join(", ", duplicateRequiredPieces)} item(s).");

        var invalidRequiredPieces = tiers
            .Where(tier => tier.RequiredItemCount < 2 || tier.RequiredItemCount > itemCount)
            .Select(tier => tier.RequiredItemCount)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
        if (invalidRequiredPieces.Length > 0)
            throw new InvalidOperationException($"Los tiers deben pedir entre 2 y {itemCount} item(s). Valores invalidos: {string.Join(", ", invalidRequiredPieces)}.");

        var emptyTiers = tiers
            .Where(tier => tier.Effects.All(effect => effect.EffectId == 0))
            .Select(tier => tier.RequiredItemCount)
            .OrderBy(x => x)
            .ToArray();
        if (emptyTiers.Length > 0)
            throw new InvalidOperationException($"Los tiers {string.Join(", ", emptyTiers)} no tienen efectos configurados.");
    }

    private static async Task SyncTemplateMembershipAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        short setId,
        short[] selectedIds,
        CancellationToken cancellationToken)
    {
        await using (var clearCurrentMembers = connection.CreateCommand())
        {
            clearCurrentMembers.Transaction = transaction;
            if (selectedIds.Length == 0)
            {
                clearCurrentMembers.CommandText = "UPDATE items_templates SET ItemSetId = -1 WHERE ItemSetId = @setId;";
                clearCurrentMembers.Parameters.AddWithValue("@setId", setId);
            }
            else
            {
                var idsCsv = string.Join(",", selectedIds);
                clearCurrentMembers.CommandText = $"""
                    UPDATE items_templates
                    SET ItemSetId = -1
                    WHERE ItemSetId = @setId
                      AND Id NOT IN ({idsCsv});
                    """;
                clearCurrentMembers.Parameters.AddWithValue("@setId", setId);
            }

            await clearCurrentMembers.ExecuteNonQueryAsync(cancellationToken);
        }

        if (selectedIds.Length == 0)
            return;

        await using var assignMembers = connection.CreateCommand();
        assignMembers.Transaction = transaction;
        assignMembers.CommandText = $"""
            UPDATE items_templates
            SET ItemSetId = @setId
            WHERE Id IN ({string.Join(",", selectedIds)});
            """;
        assignMembers.Parameters.AddWithValue("@setId", setId);
        await assignMembers.ExecuteNonQueryAsync(cancellationToken);
    }
}
