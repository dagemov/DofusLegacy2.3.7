using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Exceptions;
using RollblackLegacy.Admin.Contracts.Items;
using RollblackLegacy.Admin.Application.Items;

namespace RollblackLegacy.Admin.Application.Services;

public sealed class ItemSetsAdminReadService : IItemSetsAdminReadService
{
    private readonly IItemSetsAdminReadRepository _repository;
    private readonly IItemPreviewStateResolver _previewStateResolver;
    private readonly IItemEffectsCatalog _effectsCatalog;

    public ItemSetsAdminReadService(
        IItemSetsAdminReadRepository repository,
        IItemPreviewStateResolver previewStateResolver,
        IItemEffectsCatalog effectsCatalog)
    {
        _repository = repository;
        _previewStateResolver = previewStateResolver;
        _effectsCatalog = effectsCatalog;
    }

    public async Task<IReadOnlyList<ItemSetListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _repository.ListAsync(cancellationToken);
        return rows
            .Select(row => new ItemSetListItemDto(
                row.SetId,
                row.Name,
                row.ItemCount,
                ItemSetEffectsCodec.DecodeTiers(row.EffectsHex).Count))
            .ToList();
    }

    public async Task<ItemSetDetailDto> GetByIdAsync(int setId, CancellationToken cancellationToken = default)
    {
        if (setId <= 0)
        {
            throw new AdminValidationException(
                "SetId must be a positive integer.",
                new Dictionary<string, string[]>
                {
                    ["setId"] = ["SetId must be a positive integer."]
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

        var members = row.Items
            .Select(member =>
            {
                var preview = _previewStateResolver.Resolve(member.ItemId, member.IconId, member.TypeId);
                return new ItemSetMemberDto(
                    member.ItemId,
                    member.Name,
                    member.TypeId,
                    member.TypeName,
                    member.IconId,
                    preview,
                    preview.State == "FOUND" ? "Preview disponible" : "Preview pendiente");
            })
            .ToList();

        return new ItemSetDetailDto(
            row.SetId,
            row.Name,
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
                effect.Format);
        }

        return new ItemSetBonusEffectDto(
            effect.EffectId,
            $"Effect {effect.EffectId}",
            $"Effect_{effect.EffectId}",
            effect.Value,
            effect.Format);
    }
}
