using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.Items;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class ItemIdentityDiagnosticService
{
    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly ReferenceItemCatalogService _referenceCatalogService;
    private readonly GameAssetPreviewService _assetPreviewService;
    private readonly ClientItemLocalizationService _localizationService = new();

    public ItemIdentityDiagnosticService(
        AdminDbConnectionFactory connectionFactory,
        ReferenceItemCatalogService referenceCatalogService,
        GameAssetPreviewService assetPreviewService)
    {
        _connectionFactory = connectionFactory;
        _referenceCatalogService = referenceCatalogService;
        _assetPreviewService = assetPreviewService;
    }

    public async Task<ItemDiagnosticReport> DiagnoseAsync(short itemId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        RuntimeItemIdentitySnapshot? runtime = null;
        var runtimeSetName = string.Empty;
        var overrideName = string.Empty;
        var overrideDescription = string.Empty;
        var manualAssetRelativePath = string.Empty;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT
                    i.Id,
                    i.TypeId,
                    i.Level,
                    i.Price,
                    i.ItemSetId,
                    COALESCE(s.Name, '') AS RuntimeSetName,
                    i.AppearanceId,
                    i.BinaryEffects,
                    ao.DisplayName AS OverrideName,
                    ao.Description AS OverrideDescription,
                    aa.RelativePath AS ManualAssetPath
                FROM items_templates i
                LEFT JOIN items_sets s
                    ON s.Id = i.ItemSetId
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
            if (await reader.ReadAsync(cancellationToken))
            {
                var binaryEffects = reader.GetSafeBytes("BinaryEffects");
                runtime = new RuntimeItemIdentitySnapshot
                {
                    ItemId = itemId,
                    TypeId = (ItemType)reader.GetSafeInt16("TypeId"),
                    Level = reader.GetSafeInt16("Level"),
                    Price = reader.GetSafeInt32("Price"),
                    ItemSetId = reader.GetSafeInt16("ItemSetId", -1),
                    AppearanceId = reader.GetSafeInt16("AppearanceId", -1),
                    HasEffects = binaryEffects.Length > 0,
                };
                runtimeSetName = reader.GetSafeString("RuntimeSetName");
                overrideName = reader.GetSafeString("OverrideName");
                overrideDescription = reader.GetSafeString("OverrideDescription");
                manualAssetRelativePath = reader.GetSafeString("ManualAssetPath");
            }
        }

        var reference = _referenceCatalogService.GetItem(itemId);
        var referenceSet = reference is { ItemSetId: > 0 }
            ? _referenceCatalogService.GetSet(reference.ItemSetId)
            : null;

        return BuildReport(
            itemId,
            runtime,
            runtimeSetName,
            reference,
            referenceSet,
            _localizationService.Get(itemId),
            overrideName,
            overrideDescription,
            manualAssetRelativePath);
    }

    public ItemDiagnosticReport BuildReport(
        short itemId,
        RuntimeItemIdentitySnapshot? runtime,
        string? runtimeSetName,
        ReferenceItemIdentity? reference,
        ReferenceItemSetIdentity? referenceSet,
        AdminClientItemText client,
        string? overrideName,
        string? overrideDescription,
        string? manualAssetRelativePath)
    {
        var report = new ItemDiagnosticReport
        {
            ItemId = itemId,
            Runtime = runtime,
            RuntimeSetName = (runtimeSetName ?? string.Empty).Trim(),
            Reference = reference,
            ReferenceSet = referenceSet,
            Client = client,
            OverrideName = overrideName ?? string.Empty,
            OverrideDescription = overrideDescription ?? string.Empty,
            ManualAssetRelativePath = manualAssetRelativePath ?? string.Empty,
        };

        report.DisplayName = ItemAuditEvaluator.ResolveDisplayName(itemId, report.OverrideName, client.Name, reference?.Name);
        report.DisplayDescription = ItemAuditEvaluator.ResolveDescription(report.OverrideDescription, client.Description, reference?.Description);
        report.NameSourceLabel = ItemAuditEvaluator.ResolveIdentitySourceLabel(report.OverrideName, report.OverrideDescription, client, reference);
        report.ClientBitmapFileName = client.IconId is > 0 ? $"{client.IconId}.png" : string.Empty;
        report.Audit = ItemAuditEvaluator.Build(report);
        report.ClientVisibility = ItemAuditEvaluator.BuildClientVisibility(
            report,
            client.IconId is > 0 && HasBitmapAsset(client.IconId.Value));
        return report;
    }

    private bool HasBitmapAsset(int iconId)
    {
        var preview = _assetPreviewService.ResolveItemBitmapPreview(iconId);
        return preview.ResolvedAssetId == iconId && preview.HasPreview;
    }
}
