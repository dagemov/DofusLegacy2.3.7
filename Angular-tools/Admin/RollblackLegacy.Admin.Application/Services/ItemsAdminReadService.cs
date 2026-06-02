using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Exceptions;
using RollblackLegacy.Admin.Application.Models.Items;
using RollblackLegacy.Admin.Contracts.Common;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Services;

public sealed class ItemsAdminReadService : IItemsAdminReadService
{
    private readonly IItemsAdminReadRepository _repository;
    private readonly IItemPreviewStateResolver _previewStateResolver;

    public ItemsAdminReadService(
        IItemsAdminReadRepository repository,
        IItemPreviewStateResolver previewStateResolver)
    {
        _repository = repository;
        _previewStateResolver = previewStateResolver;
    }

    public async Task<ItemPagedResultDto<ItemListItemDto>> SearchAsync(
        ItemSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var result = await _repository.SearchAsync(request, cancellationToken);
        var items = result.Items
            .Select(MapListItem)
            .ToList();

        return new ItemPagedResultDto<ItemListItemDto>(
            request.Page,
            request.PageSize,
            result.TotalCount,
            items);
    }

    public Task<ItemPagedResultDto<ItemIconOptionDto>> SearchIconsAsync(
        ItemIconSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        return _repository.SearchIconsAsync(request, cancellationToken);
    }

    public async Task<ItemDetailDto> GetItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        EnsurePositiveItemId(itemId);

        var item = await _repository.GetByIdAsync(itemId, cancellationToken);
        if (item is null)
            throw new AdminEntityNotFoundException("item", itemId.ToString());

        return MapDetail(item);
    }

    public async Task<ItemClientIdentityDto> GetIdentityAsync(int itemId, CancellationToken cancellationToken = default)
    {
        EnsurePositiveItemId(itemId);

        var item = await _repository.GetByIdAsync(itemId, cancellationToken);
        if (item is null)
            throw new AdminEntityNotFoundException("item", itemId.ToString());

        return BuildClientIdentity(item);
    }

    public async Task<ItemQaSummaryDto> GetQaSummaryAsync(int itemId, CancellationToken cancellationToken = default)
    {
        var detail = await GetItemAsync(itemId, cancellationToken);

        var qaBlockingReasons = BuildQaBlockingReasons(detail);
        var publishBlockingReasons = BuildPublishBlockingReasons(detail);
        var blockingReasons = qaBlockingReasons
            .Concat(publishBlockingReasons)
            .ToList();

        var canQa = qaBlockingReasons.Count == 0;

        return new ItemQaSummaryDto(
            detail.ItemId,
            detail.ResolvedName,
            detail.TypeName,
            detail.Level,
            detail.IconId,
            detail.AppearanceId,
            detail.PreviewState,
            detail.Warnings,
            canQa ? "READY_FOR_QA" : "BLOCKED",
            canQa,
            CanPublish: false,
            blockingReasons,
            BuildRecommendedChecks(detail));
    }

    public Task<IReadOnlyList<AdminOptionDto>> GetTypeOptionsAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetTypeOptionsAsync(cancellationToken);
    }

    public Task<IReadOnlyList<AdminOptionDto>> GetItemSetOptionsAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetItemSetOptionsAsync(cancellationToken);
    }

    private ItemListItemDto MapListItem(AdminItemListReadModel item)
    {
        var previewState = _previewStateResolver.Resolve(item.ItemId, item.IconId);
        var warnings = BuildWarnings(
            item.ResolvedName,
            item.TypeName,
            item.IconId,
            item.AppearanceId,
            item.SetId,
            item.SetName,
            BuildClientIdentity(item.ItemId, item.ResolvedName, item.IconId, item.AppearanceId),
            previewState);

        return new ItemListItemDto(
            item.ItemId,
            item.ResolvedName,
            item.TypeId,
            item.TypeName,
            item.Level,
            item.SetId,
            item.SetName,
            item.IconId,
            item.AppearanceId,
            previewState,
            warnings.Count);
    }

    private ItemDetailDto MapDetail(AdminItemDetailReadModel item)
    {
        var previewState = _previewStateResolver.Resolve(item.ItemId, item.IconId);
        var clientIdentity = BuildClientIdentity(item);
        var warnings = BuildWarnings(
            item.ResolvedName,
            item.TypeName,
            item.IconId,
            item.AppearanceId,
            item.SetId,
            item.SetName,
            clientIdentity,
            previewState);

        var setLink = item.SetId.HasValue
            ? new ItemSetLinkDto(item.SetId.Value, item.SetName, string.IsNullOrWhiteSpace(item.SetName) ? "MISSING" : "LINKED")
            : null;

        return new ItemDetailDto(
            item.ItemId,
            item.ResolvedName,
            null,
            item.DescriptionId,
            item.TypeId,
            item.TypeName,
            item.Level,
            item.Weight,
            item.Price,
            item.Usable,
            item.Targetable,
            item.TwoHanded,
            item.Etheral,
            item.Criteria,
            item.IconId,
            item.AppearanceId,
            setLink,
            clientIdentity,
            previewState,
            warnings,
            item.Effects.Select(x => new ItemEffectDto(
                x.EffectId,
                x.DiceNum,
                x.DiceSide,
                x.Value,
                x.Description)).ToList());
    }

    private static ItemClientIdentityDto BuildClientIdentity(AdminItemDetailReadModel item)
    {
        return BuildClientIdentity(item.ItemId, item.ResolvedName, item.IconId, item.AppearanceId);
    }

    private static ItemClientIdentityDto BuildClientIdentity(
        int itemId,
        string? resolvedName,
        int iconId,
        int appearanceId)
    {
        return new ItemClientIdentityDto(
            itemId,
            ClientNameId: null,
            ClientName: resolvedName,
            IconId: iconId > 0 ? iconId : null,
            AppearanceId: appearanceId > 0 ? appearanceId : null,
            Source: "sunshine.items",
            Confidence: 0.50d);
    }

    private static IReadOnlyList<ItemWarningDto> BuildWarnings(
        string? resolvedName,
        string? typeName,
        int iconId,
        int appearanceId,
        int? setId,
        string? setName,
        ItemClientIdentityDto clientIdentity,
        ItemPreviewStateDto previewState)
    {
        var warnings = new List<ItemWarningDto>();

        if (string.IsNullOrWhiteSpace(resolvedName))
        {
            warnings.Add(new ItemWarningDto(
                "MISSING_CLIENT_NAME",
                "warning",
                "No client-facing name could be resolved from the current Sunshine item row.",
                "resolvedName"));
        }

        if (iconId <= 0)
        {
            warnings.Add(new ItemWarningDto(
                "MISSING_ICON",
                "warning",
                "The item does not expose a valid IconId in the current Sunshine row.",
                "iconId"));
        }

        if (!string.IsNullOrWhiteSpace(clientIdentity.ClientName) && clientIdentity.IconId.HasValue && clientIdentity.IconId.Value != iconId)
        {
            warnings.Add(new ItemWarningDto(
                "ICON_ID_MISMATCH",
                "warning",
                "The resolved client identity icon does not match the runtime IconId.",
                "iconId"));
        }

        if (clientIdentity.AppearanceId.HasValue && clientIdentity.AppearanceId.Value != appearanceId)
        {
            warnings.Add(new ItemWarningDto(
                "APPEARANCE_ID_MISMATCH",
                "warning",
                "The resolved client identity appearance does not match the runtime AppearanceId.",
                "appearanceId"));
        }

        if (previewState.State == "MISSING")
        {
            warnings.Add(new ItemWarningDto(
                "MANUAL_ASSET_MISSING",
                "warning",
                "No preview asset was resolved from manual, by-item, or by-icon paths.",
                "previewState"));
        }

        if (setId.HasValue && string.IsNullOrWhiteSpace(setName))
        {
            warnings.Add(new ItemWarningDto(
                "SET_LINK_MISSING",
                "warning",
                "The item references an ItemSetId that did not resolve to an existing item set.",
                "setId"));
        }

        if (string.IsNullOrWhiteSpace(typeName))
        {
            warnings.Add(new ItemWarningDto(
                "UNKNOWN_TYPE",
                "warning",
                "The current TypeId does not map to a known item type option.",
                "typeId"));
        }

        return warnings;
    }

    private static IReadOnlyList<string> BuildQaBlockingReasons(ItemDetailDto detail)
    {
        var reasons = new List<string>();

        if (string.IsNullOrWhiteSpace(detail.ResolvedName))
        {
            reasons.Add("ResolvedName is missing. Assign a stable operator-facing name before QA.");
        }

        if (string.IsNullOrWhiteSpace(detail.TypeName))
        {
            reasons.Add("TypeId did not resolve to a known item type. Fix the item type before QA.");
        }

        if (detail.IconId <= 0)
        {
            reasons.Add("IconId is missing or invalid. QA should not start until the inventory preview identity is set.");
        }

        if (!IsPreviewReady(detail.PreviewState))
        {
            reasons.Add("Preview is not ready. Resolve a curated or manual preview before QA.");
        }

        return reasons;
    }

    private static IReadOnlyList<string> BuildPublishBlockingReasons(ItemDetailDto detail)
    {
        var reasons = new List<string>
        {
            "Real client publish is intentionally disabled in Phase 8. Use this panel for readiness only.",
            "Description publish is still deferred because sunshine.items stores DescriptionId but not free-text client i18n payload.",
            "IsVisible publish is still deferred because sunshine.items has no direct persistence field for it."
        };

        if (detail.AppearanceId <= 0)
        {
            reasons.Add("AppearanceId is not set. Equipped/runtime appearance QA will stay incomplete until it is confirmed.");
        }

        return reasons;
    }

    private static IReadOnlyList<string> BuildRecommendedChecks(ItemDetailDto detail)
    {
        return new List<string>
        {
            $"Confirm item identity in Admin: ItemId={detail.ItemId}, IconId={detail.IconId}, AppearanceId={detail.AppearanceId}.",
            $"Confirm preview readiness: state={detail.PreviewState.State}, path={detail.PreviewState.ResolvedPath ?? detail.PreviewState.ByIconPath}.",
            $"Confirm runtime fields: Type={detail.TypeName ?? detail.TypeId.ToString()}, Level={detail.Level}, Weight={detail.Weight}, Price={detail.Price}.",
            "Confirm the row exists in sunshine.items with the expected Name, TypeId, Level, IconId, and AppearanceId values.",
            $"Give the item in game using the Sunshine command `.item add {detail.ItemId} 1 <CharacterName>` if your operator role allows moderator commands.",
            "Confirm the inventory icon, item name, and tooltip in game.",
            "If the item is equipable, confirm equipped appearance, slot behavior, and effects in game.",
            "Confirm the client does not error after receiving or equipping the item.",
            "Capture the QA result and only hand off to future publish workflow once blockers are cleared."
        };
    }

    private static bool IsPreviewReady(ItemPreviewStateDto previewState)
    {
        return string.Equals(previewState.State, "FOUND", StringComparison.OrdinalIgnoreCase)
            || string.Equals(previewState.State, "MANUAL", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateRequest(ItemSearchRequest request)
    {
        if (request.LevelMin.HasValue && request.LevelMax.HasValue && request.LevelMin.Value > request.LevelMax.Value)
        {
            throw new AdminValidationException(
                "The requested item search range is invalid.",
                new Dictionary<string, string[]>
                {
                    ["levelRange"] = new[]
                    {
                        "levelMin must be less than or equal to levelMax."
                    }
                });
        }
    }

    private static void EnsurePositiveItemId(int itemId)
    {
        if (itemId <= 0)
        {
            throw new AdminValidationException(
                "The requested item id is invalid.",
                new Dictionary<string, string[]>
                {
                    ["itemId"] = new[]
                    {
                        "itemId must be greater than zero."
                    }
                });
        }
    }
}
