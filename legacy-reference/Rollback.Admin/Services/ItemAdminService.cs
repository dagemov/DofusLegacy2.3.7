using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.Items;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class ItemAdminService
{
    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly ItemIdentityDiagnosticService _itemDiagnosticService;
    private readonly ItemIdentityCorrectionService _itemIdentityCorrectionService;
    private readonly AdminEntityAssetOverrideService _assetOverrideService;
    private readonly AdminItemCatalogService _itemCatalogService;
    private readonly ItemAppearanceCatalogService _appearanceCatalogService;
    private readonly ItemAppearanceResolverService _appearanceResolverService;
    private readonly GameEffectEditorService _effectEditorService;
    private readonly ReferenceItemCatalogService _referenceCatalogService;
    private readonly ItemClientPublishService _itemClientPublishService;
    private readonly NpcVendorInventorySyncService _npcVendorInventorySyncService;
    private readonly ClientItemLocalizationService _localizationService = new();

    public ItemAdminService(
        AdminDbConnectionFactory connectionFactory,
        ItemIdentityDiagnosticService itemDiagnosticService,
        ItemIdentityCorrectionService itemIdentityCorrectionService,
        AdminEntityAssetOverrideService assetOverrideService,
        AdminItemCatalogService itemCatalogService,
        ItemAppearanceCatalogService appearanceCatalogService,
        ItemAppearanceResolverService appearanceResolverService,
        GameEffectEditorService effectEditorService,
        ReferenceItemCatalogService referenceCatalogService,
        ItemClientPublishService itemClientPublishService,
        NpcVendorInventorySyncService npcVendorInventorySyncService)
    {
        _connectionFactory = connectionFactory;
        _itemDiagnosticService = itemDiagnosticService;
        _itemIdentityCorrectionService = itemIdentityCorrectionService;
        _assetOverrideService = assetOverrideService;
        _itemCatalogService = itemCatalogService;
        _appearanceCatalogService = appearanceCatalogService;
        _appearanceResolverService = appearanceResolverService;
        _effectEditorService = effectEditorService;
        _referenceCatalogService = referenceCatalogService;
        _itemClientPublishService = itemClientPublishService;
        _npcVendorInventorySyncService = npcVendorInventorySyncService;
    }

    public async Task<AdminPagedResult<ItemListItem>> GetPagedAsync(
        AdminPagedQuery query,
        ItemType? typeId = null,
        short? minLevel = null,
        short? maxLevel = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(query);
        var filteredItems = await _itemCatalogService.SearchAsync(
            new ItemCatalogQuery
            {
                Search = normalized.Search,
                Types = typeId.HasValue ? new[] { typeId.Value } : Array.Empty<ItemType>(),
                MinLevel = minLevel,
                MaxLevel = maxLevel,
            },
            cancellationToken);

        var totalCount = filteredItems.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)normalized.PageSize));
        var currentPage = Math.Min(normalized.Page, totalPages);
        var pagedItems = filteredItems
            .Skip((currentPage - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToArray();

        return new AdminPagedResult<ItemListItem>(pagedItems, totalCount, currentPage, normalized.PageSize);
    }

    public async Task<ItemEditModel?> GetByIdAsync(short itemId, CancellationToken cancellationToken = default)
    {
        var report = await _itemDiagnosticService.DiagnoseAsync(itemId, cancellationToken);
        if (report.Runtime is null)
            return null;

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                i.Id,
                i.TypeId,
                i.Level,
                i.Weight,
                i.Usable,
                i.Targetable,
                i.Etheral,
                i.Price,
                i.ItemSetId,
                i.StringCriterion,
                i.AppearanceId,
                i.BinaryEffects,
                i.RecipesCSV,
                i.TwoHanded,
                i.APCost,
                i.MinRange,
                i.MaxRange,
                i.CastInLine,
                i.CastTestLOS,
                i.CriticalHitProbability,
                i.CriticalHitBonus,
                i.CriticalFailureProbability,
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
            WHERE i.Id = @itemId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@entityType", AdminEntityType.Item.ToString());
        command.Parameters.AddWithValue("@assetKind", AdminEntityAssetOverrideService.PreviewPngKind);
        command.Parameters.AddWithValue("@itemId", itemId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        var binaryEffects = reader.GetSafeBytes("BinaryEffects");

        return new ItemEditModel
        {
            Id = reader.GetSafeInt16("Id"),
            TypeId = (ItemType)reader.GetSafeInt16("TypeId"),
            Level = reader.GetSafeInt16("Level"),
            Weight = reader.GetSafeInt32("Weight"),
            Usable = reader.GetSafeBoolean("Usable"),
            Targetable = reader.GetSafeBoolean("Targetable"),
            Etheral = reader.GetSafeBoolean("Etheral"),
            Price = reader.GetSafeInt32("Price"),
            ItemSetId = reader.GetSafeInt16("ItemSetId", -1),
            StringCriterion = reader.GetSafeString("StringCriterion"),
            AppearanceId = reader.GetSafeInt16("AppearanceId", -1),
            RecipesCsv = reader.GetSafeString("RecipesCSV"),
            TwoHanded = reader.GetSafeBoolean("TwoHanded"),
            APCost = reader.GetSafeInt16("APCost"),
            MinRange = reader.GetSafeSByte("MinRange"),
            MaxRange = reader.GetSafeSByte("MaxRange"),
            CastInLine = reader.GetSafeBoolean("CastInLine"),
            CastTestLOS = reader.GetSafeBoolean("CastTestLOS"),
            CriticalHitProbability = reader.GetSafeSByte("CriticalHitProbability"),
            CriticalHitBonus = reader.GetSafeSByte("CriticalHitBonus"),
            CriticalFailureProbability = reader.GetSafeSByte("CriticalFailureProbability"),
            Name = report.DisplayName,
            Description = report.DisplayDescription,
            NameSourceLabel = report.NameSourceLabel,
            OverrideName = report.OverrideName,
            OverrideDescription = report.OverrideDescription,
            ClientName = report.Client.Name,
            ClientDescription = report.Client.Description,
            ClientIconId = report.Client.IconId,
            ClientAppearanceId = report.Client.ClientAppearanceId,
            ManualAssetRelativePath = report.ManualAssetRelativePath,
            ManualImageUrl = AdminItemCatalogService.BuildManualAssetUrl(report.ManualAssetRelativePath),
            ReferenceNameId = report.Reference?.NameId,
            ReferenceDescriptionId = report.Reference?.DescriptionId,
            ReferenceIconId = report.Reference?.IconId,
            ReferenceTypeId = report.Reference?.TypeId,
            ReferenceTypeLabel = report.Reference?.TypeLabel ?? string.Empty,
            ReferenceSetId = report.Reference?.ItemSetId,
            ReferenceSetName = report.ReferenceSet?.Name ?? string.Empty,
            Audit = report.Audit,
            ClientVisibility = report.ClientVisibility,
            IdentityCorrectionPlan = await _itemIdentityCorrectionService.PreviewAsync(itemId, cancellationToken),
            Effects = _effectEditorService.Deserialize(binaryEffects),
        };
    }

    public async Task<short> GetNextAvailableIdAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id FROM items_templates ORDER BY Id ASC;";

        short nextId = 1;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var currentId = reader.GetSafeInt16("Id");
            if (currentId < nextId)
                continue;

            if (currentId == nextId)
            {
                if (nextId == short.MaxValue)
                    throw new InvalidOperationException("No quedan IDs libres en el rango soportado por items_templates.Id.");

                nextId++;
                continue;
            }

            break;
        }

        return nextId;
    }

    public async Task<bool> ExistsAsync(short itemId, CancellationToken cancellationToken = default)
    {
        if (itemId <= 0)
            return false;

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM items_templates WHERE Id = @itemId LIMIT 1;";
        command.Parameters.AddWithValue("@itemId", itemId);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null and not DBNull;
    }

    public async Task<AdminSaveResult> SaveAsync(ItemEditModel model, CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var infos = new List<string>();
        var errors = new List<string>();
        var existsBeforeSave = await ExistsAsync(model.Id, cancellationToken);
        var appearancePipelineInfo = await TryNormalizeVisualAppearanceAsync(model, existsBeforeSave, cancellationToken);
        if (!string.IsNullOrWhiteSpace(appearancePipelineInfo))
            infos.Add(appearancePipelineInfo);

        var appearanceWarning = await _appearanceCatalogService.GetValidationWarningAsync(model.TypeId, model.AppearanceId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(appearanceWarning))
            warnings.Add(appearanceWarning);

        var shouldAutoPublishClient = ShouldAutoPublishClient(model, existsBeforeSave);
        var creationWarning = BuildCreationWarning(model, existsBeforeSave);
        if (!string.IsNullOrWhiteSpace(creationWarning))
            warnings.Add(creationWarning);

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var binaryEffects = _effectEditorService.Serialize(model.Effects);

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO items_templates
                (
                    Id, TypeId, Level, Weight, Usable, Targetable, Etheral, Price, ItemSetId,
                    StringCriterion, AppearanceId, BinaryEffects, RecipesCSV, TwoHanded, APCost,
                    MinRange, MaxRange, CastInLine, CastTestLOS, CriticalHitProbability,
                    CriticalHitBonus, CriticalFailureProbability
                )
                VALUES
                (
                    @id, @typeId, @level, @weight, @usable, @targetable, @etheral, @price, @itemSetId,
                    @stringCriterion, @appearanceId, @binaryEffects, @recipesCsv, @twoHanded, @apCost,
                    @minRange, @maxRange, @castInLine, @castTestLOS, @criticalHitProbability,
                    @criticalHitBonus, @criticalFailureProbability
                )
                ON DUPLICATE KEY UPDATE
                    TypeId = VALUES(TypeId),
                    Level = VALUES(Level),
                    Weight = VALUES(Weight),
                    Usable = VALUES(Usable),
                    Targetable = VALUES(Targetable),
                    Etheral = VALUES(Etheral),
                    Price = VALUES(Price),
                    ItemSetId = VALUES(ItemSetId),
                    StringCriterion = VALUES(StringCriterion),
                    AppearanceId = VALUES(AppearanceId),
                    BinaryEffects = VALUES(BinaryEffects),
                    RecipesCSV = VALUES(RecipesCSV),
                    TwoHanded = VALUES(TwoHanded),
                    APCost = VALUES(APCost),
                    MinRange = VALUES(MinRange),
                    MaxRange = VALUES(MaxRange),
                    CastInLine = VALUES(CastInLine),
                    CastTestLOS = VALUES(CastTestLOS),
                    CriticalHitProbability = VALUES(CriticalHitProbability),
                    CriticalHitBonus = VALUES(CriticalHitBonus),
                    CriticalFailureProbability = VALUES(CriticalFailureProbability);
                """;
            command.Parameters.AddWithValue("@id", model.Id);
            command.Parameters.AddWithValue("@typeId", (short)model.TypeId);
            command.Parameters.AddWithValue("@level", model.Level);
            command.Parameters.AddWithValue("@weight", model.Weight);
            command.Parameters.AddWithValue("@usable", model.Usable);
            command.Parameters.AddWithValue("@targetable", model.Targetable);
            command.Parameters.AddWithValue("@etheral", model.Etheral);
            command.Parameters.AddWithValue("@price", model.Price);
            command.Parameters.AddWithValue("@itemSetId", model.ItemSetId);
            command.Parameters.AddWithValue("@stringCriterion", model.StringCriterion ?? string.Empty);
            command.Parameters.AddWithValue("@appearanceId", model.AppearanceId);
            command.Parameters.Add("@binaryEffects", MySqlDbType.Blob).Value = binaryEffects;
            command.Parameters.AddWithValue("@recipesCsv", model.RecipesCsv ?? string.Empty);
            command.Parameters.AddWithValue("@twoHanded", model.TwoHanded);
            command.Parameters.AddWithValue("@apCost", model.APCost);
            command.Parameters.AddWithValue("@minRange", model.MinRange);
            command.Parameters.AddWithValue("@maxRange", model.MaxRange);
            command.Parameters.AddWithValue("@castInLine", model.CastInLine);
            command.Parameters.AddWithValue("@castTestLOS", model.CastTestLOS);
            command.Parameters.AddWithValue("@criticalHitProbability", model.CriticalHitProbability);
            command.Parameters.AddWithValue("@criticalHitBonus", model.CriticalHitBonus);
            command.Parameters.AddWithValue("@criticalFailureProbability", model.CriticalFailureProbability);
            await command.ExecuteNonQueryAsync(cancellationToken);

            await AdminEntityTextOverrideService.SaveAsync(
                connection,
                AdminEntityType.Item,
                model.Id,
                model.OverrideName,
                model.OverrideDescription,
                transaction,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            if (shouldAutoPublishClient)
            {
                try
                {
                    var publishResult = await _itemClientPublishService.PublishAsync(model.Id, cancellationToken);
                    infos.Add(publishResult.Summary);
                    if (publishResult.HasWarnings)
                        warnings.AddRange(publishResult.Warnings);
                }
                catch (Exception ex)
                {
                    errors.Add($"El item #{model.Id} se guardo en runtime, pero no se pudo publicar su definicion cliente: {ex.Message}");
                }
            }

            try
            {
                var vendorSyncMessage = await _npcVendorInventorySyncService.SyncItemAsync(model.Id, cancellationToken);
                if (!string.IsNullOrWhiteSpace(vendorSyncMessage))
                    infos.Add(vendorSyncMessage);
            }
            catch (Exception ex)
            {
                errors.Add($"El item #{model.Id} se guardo, pero no se pudo resincronizar su asignacion de vendors: {ex.Message}");
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

    public async Task<AdminSaveResult> SaveManualAssetAsync(short itemId, string? relativePath, CancellationToken cancellationToken = default)
    {
        await _assetOverrideService.SaveAsync(
            AdminEntityType.Item,
            itemId,
            relativePath,
            AdminEntityAssetOverrideService.PreviewPngKind,
            cancellationToken);

        var infos = new List<string>();
        var warnings = new List<string>();
        var errors = new List<string>();

        if (itemId <= 0 || string.IsNullOrWhiteSpace(relativePath))
            return AdminSaveResult.Empty;

        var model = await GetByIdAsync(itemId, cancellationToken);
        if (model is null)
            return AdminSaveResult.Empty;

        model.ManualAssetRelativePath = relativePath;
        var appearancePipelineInfo = await TryNormalizeVisualAppearanceAsync(model, existsBeforeSave: true, cancellationToken);
        if (string.IsNullOrWhiteSpace(appearancePipelineInfo) || model.AppearanceId <= 0)
            return AdminSaveResult.Empty;

        var saveResult = await SaveAsync(model, cancellationToken);
        infos.Add(appearancePipelineInfo);
        if (saveResult.HasInfos)
            infos.AddRange(saveResult.Infos);
        if (saveResult.HasWarnings)
            warnings.AddRange(saveResult.Warnings);
        if (saveResult.HasErrors)
            errors.AddRange(saveResult.Errors);

        return new AdminSaveResult
        {
            Infos = infos,
            Warnings = warnings,
            Errors = errors,
        };
    }

    public async Task ClearManualAssetAsync(short itemId, CancellationToken cancellationToken = default)
    {
        await _assetOverrideService.DeleteAsync(
            AdminEntityType.Item,
            itemId,
            AdminEntityAssetOverrideService.PreviewPngKind,
            cancellationToken);
    }

    public async Task DeleteAsync(short itemId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var vendorDelete = connection.CreateCommand())
            {
                vendorDelete.Transaction = transaction;
                vendorDelete.CommandText = "DELETE FROM npcs_items WHERE ItemId = @itemId;";
                vendorDelete.Parameters.AddWithValue("@itemId", itemId);
                await vendorDelete.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var overrideDelete = connection.CreateCommand())
            {
                overrideDelete.Transaction = transaction;
                overrideDelete.CommandText = """
                    DELETE FROM admin_entity_text_overrides
                    WHERE EntityType = @entityType AND EntityId = @entityId;
                    """;
                overrideDelete.Parameters.AddWithValue("@entityType", AdminEntityType.Item.ToString());
                overrideDelete.Parameters.AddWithValue("@entityId", itemId);
                await overrideDelete.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var assetDelete = connection.CreateCommand())
            {
                assetDelete.Transaction = transaction;
                assetDelete.CommandText = """
                    DELETE FROM admin_entity_asset_overrides
                    WHERE EntityType = @entityType AND EntityId = @entityId;
                    """;
                assetDelete.Parameters.AddWithValue("@entityType", AdminEntityType.Item.ToString());
                assetDelete.Parameters.AddWithValue("@entityId", itemId);
                await assetDelete.ExecuteNonQueryAsync(cancellationToken);
            }

            var impactedSets = new List<(short Id, string ItemsCsv)>();
            await using (var setQuery = connection.CreateCommand())
            {
                setQuery.Transaction = transaction;
                setQuery.CommandText = "SELECT Id, ItemsCSV FROM items_sets WHERE FIND_IN_SET(@itemId, ItemsCSV) > 0;";
                setQuery.Parameters.AddWithValue("@itemId", itemId);
                await using var reader = await setQuery.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    impactedSets.Add((reader.GetSafeInt16("Id"), reader.GetSafeString("ItemsCSV")));
            }

            foreach (var impactedSet in impactedSets)
            {
                var newCsv = string.Join(",",
                    impactedSet.ItemsCsv
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Where(x => x != itemId.ToString()));

                await using var setUpdate = connection.CreateCommand();
                setUpdate.Transaction = transaction;
                setUpdate.CommandText = "UPDATE items_sets SET ItemsCSV = @itemsCsv WHERE Id = @setId;";
                setUpdate.Parameters.AddWithValue("@setId", impactedSet.Id);
                setUpdate.Parameters.AddWithValue("@itemsCsv", newCsv);
                await setUpdate.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var deleteItem = connection.CreateCommand())
            {
                deleteItem.Transaction = transaction;
                deleteItem.CommandText = "DELETE FROM items_templates WHERE Id = @itemId;";
                deleteItem.Parameters.AddWithValue("@itemId", itemId);
                await deleteItem.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<AdminLookupOption>> GetLookupAsync(
        string search,
        IReadOnlyCollection<ItemType>? types = null,
        CancellationToken cancellationToken = default)
    {
        var items = await _itemCatalogService.SearchAsync(
            new ItemCatalogQuery
            {
                Search = search,
                Types = types ?? Array.Empty<ItemType>(),
                MaxResults = 40,
            },
            cancellationToken);

        return items
            .Select(item => new AdminLookupOption(
                item.Id.ToString(),
                $"{item.Name} Â· {item.TypeLabel} Â· lvl {item.Level} Â· {item.Price:N0}k"))
            .ToArray();
    }

    public async Task<string> DescribeTypedItemIdAsync(short itemId, CancellationToken cancellationToken = default)
    {
        var report = await _itemDiagnosticService.DiagnoseAsync(itemId, cancellationToken);
        return ItemAuditEvaluator.BuildIdGuidance(report);
    }

    public Task<ItemDiagnosticReport> GetDiagnosticAsync(short itemId, CancellationToken cancellationToken = default) =>
        _itemDiagnosticService.DiagnoseAsync(itemId, cancellationToken);

    public Task<AdminSaveResult> ApplyIdentityCorrectionAsync(short itemId, CancellationToken cancellationToken = default) =>
        _itemIdentityCorrectionService.ApplyAsync(itemId, cancellationToken);

    public Task<ItemClientPublishResult> PublishClientSupportAsync(short itemId, CancellationToken cancellationToken = default) =>
        _itemClientPublishService.PublishAsync(itemId, cancellationToken);

    public static string FormatDisplayNameWithId(string? displayName, short itemId)
    {
        var normalized = (displayName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return $"Item #{itemId}";

        if (normalized.Contains($"#{itemId}", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains($"[{itemId}]", StringComparison.OrdinalIgnoreCase))
            return normalized;

        return $"{normalized} [#{itemId}]";
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

    private string? BuildCreationWarning(ItemEditModel model, bool existsBeforeSave)
    {
        if (existsBeforeSave)
            return null;

        var reference = _referenceCatalogService.GetItem(model.Id);
        var report = _itemDiagnosticService.BuildReport(
            model.Id,
            runtime: null,
            runtimeSetName: string.Empty,
            reference,
            reference is { ItemSetId: > 0 } ? _referenceCatalogService.GetSet(reference.ItemSetId) : null,
            _localizationService.Get(model.Id),
            model.OverrideName,
            model.OverrideDescription,
            model.ManualAssetRelativePath);

        return ItemAuditEvaluator.BuildIdGuidance(report);
    }

    private async Task<string?> TryNormalizeVisualAppearanceAsync(
        ItemEditModel model,
        bool existsBeforeSave,
        CancellationToken cancellationToken)
    {
        _ = existsBeforeSave;

        if (!IsVisualWearable(model.TypeId))
            return null;

        var resolution = await _appearanceResolverService.AnalyzeAsync(model, cancellationToken);
        if (resolution?.AppearanceId is not > 0)
            return null;

        if (!resolution.NeedsCorrection)
            return null;

        model.AppearanceId = resolution.AppearanceId;

        return resolution.IsMismatch
            ? $"Appearance corregida automaticamente: {resolution.CurrentAppearanceId} -> {resolution.AppearanceId}. {resolution.Message}"
            : resolution.Message;
    }

    private static bool IsVisualWearable(ItemType itemType) =>
        itemType is ItemType.Chapeau or ItemType.Cape or ItemType.Familier;

    private static bool ShouldAutoPublishClient(ItemEditModel model, bool existsBeforeSave)
    {
        _ = model;
        _ = existsBeforeSave;
        return true;
    }
}
