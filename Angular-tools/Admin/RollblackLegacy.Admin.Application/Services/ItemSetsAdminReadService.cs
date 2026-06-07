using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Exceptions;
using RollblackLegacy.Admin.Application.Items;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Services;

public sealed class ItemSetsAdminReadService : IItemSetsAdminReadService
{
    private readonly IItemSetsAdminReadRepository _repository;
    private readonly IItemPreviewStateResolver _previewStateResolver;
    private readonly IItemEffectsCatalog _effectsCatalog;
    private readonly IItemPublicationManifestService _publicationManifestService;

    public ItemSetsAdminReadService(
        IItemSetsAdminReadRepository repository,
        IItemPreviewStateResolver previewStateResolver,
        IItemEffectsCatalog effectsCatalog,
        IItemPublicationManifestService publicationManifestService)
    {
        _repository = repository;
        _previewStateResolver = previewStateResolver;
        _effectsCatalog = effectsCatalog;
        _publicationManifestService = publicationManifestService;
    }

    public async Task<ItemPagedResultDto<ItemSetListItemDto>> SearchAsync(
        ItemSetSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        request.Page = request.Page <= 0 ? 1 : request.Page;
        request.PageSize = request.PageSize switch
        {
            <= 0 => 20,
            > 100 => 100,
            _ => request.PageSize,
        };

        var page = await _repository.SearchAsync(request, cancellationToken);
        var items = page.Items
            .Select(row => new ItemSetListItemDto(
                row.SetId,
                row.Name,
                row.Level,
                row.ItemCount,
                ItemSetEffectsCodec.DecodeTiers(row.EffectsHex).Count,
                row.PreviewIconIds
                    .Select(iconId =>
                    {
                        var preview = _previewStateResolver.Resolve(null, iconId, null);
                        return preview.ResolvedPath ?? preview.ByCategoryPath ?? preview.ByIconPath;
                    })
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .ToList()))
            .ToList();

        return new ItemPagedResultDto<ItemSetListItemDto>(
            request.Page,
            request.PageSize,
            page.TotalCount,
            items);
    }

    public async Task<ItemSetDetailDto> GetByIdAsync(int setId, CancellationToken cancellationToken = default)
    {
        if (setId <= 0)
        {
            throw new AdminValidationException(
                "SetId must be a positive integer.",
                new Dictionary<string, string[]>
                {
                    ["setId"] = ["SetId must be a positive integer."],
                });
        }

        var row = await _repository.GetByIdAsync(setId, cancellationToken);
        if (row is null)
        {
            throw new AdminEntityNotFoundException("item-set", setId.ToString());
        }

        var optionsById = _effectsCatalog.GetOptions().ToDictionary(x => x.EffectId);
        var tiers = ItemSetEffectsCodec.DecodeTiers(row.EffectsHex)
            .Select(tier => new ItemSetBonusTierDto(
                tier.PieceCount,
                tier.TierLabel,
                tier.Effects
                    .Select(effect => MapBonusEffect(effect, optionsById))
                    .ToList()))
            .ToList();

        var members = new List<ItemSetMemberDto>();
        foreach (var member in row.Items)
        {
            var preview = _previewStateResolver.Resolve(member.ItemId, member.IconId, member.TypeId);
            var publicationSummary = await TryResolvePublicationSummaryAsync(member.ItemId, cancellationToken);
            members.Add(new ItemSetMemberDto(
                member.ItemId,
                member.Name,
                member.TypeId,
                member.TypeName,
                member.IconId,
                preview,
                preview.ResolvedPath,
                publicationSummary));
        }

        return new ItemSetDetailDto(
            row.SetId,
            row.Name,
            row.Level,
            row.BonusIsSecret,
            members,
            tiers);
    }

    private static ItemSetBonusEffectDto MapBonusEffect(
        ItemSetBonusEffectLine effect,
        IReadOnlyDictionary<int, AdminEffectOptionDto> optionsById)
    {
        if (optionsById.TryGetValue(effect.EffectId, out var option))
        {
            return new ItemSetBonusEffectDto(
                effect.EffectId,
                option.Label,
                option.ProtocolName,
                effect.Value,
                effect.DiceNum,
                effect.DiceSide,
                effect.Format);
        }

        return new ItemSetBonusEffectDto(
            effect.EffectId,
            $"Effect {effect.EffectId}",
            $"Effect_{effect.EffectId}",
            effect.Value,
            effect.DiceNum,
            effect.DiceSide,
            effect.Format);
    }

    private async Task<string?> TryResolvePublicationSummaryAsync(int itemId, CancellationToken cancellationToken)
    {
        try
        {
            var manifest = await _publicationManifestService.GetManifestAsync(itemId, cancellationToken);
            return manifest.PrimaryState;
        }
        catch
        {
            return null;
        }
    }

}
