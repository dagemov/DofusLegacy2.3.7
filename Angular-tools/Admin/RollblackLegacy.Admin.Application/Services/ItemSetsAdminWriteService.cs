using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Exceptions;
using RollblackLegacy.Admin.Application.Items;
using RollblackLegacy.Admin.Application.Models.Items;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Services;

public sealed class ItemSetsAdminWriteService : IItemSetsAdminWriteService
{
    private const int UnprocessableEntityStatusCode = 422;

    private readonly IItemSetsAdminReadRepository _readRepository;
    private readonly IItemSetsAdminWriteRepository _writeRepository;
    private readonly IItemEffectsCatalog _effectsCatalog;

    public ItemSetsAdminWriteService(
        IItemSetsAdminReadRepository readRepository,
        IItemSetsAdminWriteRepository writeRepository,
        IItemEffectsCatalog effectsCatalog)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _effectsCatalog = effectsCatalog;
    }

    public async Task<ItemSetWriteResultDto> CreateAsync(
        ItemSetCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var draft = await ValidateAndBuildDraftAsync(request.Name, request.ItemIds, request.BonusTiers, cancellationToken);
        var setId = await _writeRepository.CreateAsync(draft, cancellationToken);
        return new ItemSetWriteResultDto(setId, $"Set #{setId} creado correctamente.");
    }

    public async Task<ItemSetWriteResultDto> UpdateAsync(
        int setId,
        ItemSetUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePositiveSetId(setId);

        if (!await _readRepository.ExistsAsync(setId, cancellationToken))
        {
            throw new AdminEntityNotFoundException("item-set", setId.ToString());
        }

        var draft = await ValidateAndBuildDraftAsync(request.Name, request.ItemIds, request.BonusTiers, cancellationToken);
        await _writeRepository.UpdateAsync(setId, draft, cancellationToken);
        return new ItemSetWriteResultDto(setId, $"Set #{setId} actualizado correctamente.");
    }

    public async Task DeleteAsync(int setId, CancellationToken cancellationToken = default)
    {
        EnsurePositiveSetId(setId);
        await _writeRepository.DeleteAsync(setId, cancellationToken);
    }

    private async Task<AdminItemSetWriteDraft> ValidateAndBuildDraftAsync(
        string? name,
        IReadOnlyList<int> itemIds,
        IReadOnlyList<ItemSetBonusTierWriteDto> bonusTiers,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        var normalizedName = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            errors["name"] = ["El nombre del set es obligatorio."];
        }

        var normalizedItemIds = itemIds
            .Where(id => id > 0)
            .ToList();

        if (normalizedItemIds.Count != normalizedItemIds.Distinct().Count())
        {
            errors["itemIds"] = ["No se permiten ItemIds duplicados."];
        }

        var existingIds = await _readRepository.ResolveExistingItemIdsAsync(normalizedItemIds, cancellationToken);
        var missingIds = normalizedItemIds.Except(existingIds).ToArray();
        if (missingIds.Length > 0)
        {
            errors["itemIds"] =
            [
                $"Los siguientes ItemIds no existen: {string.Join(", ", missingIds)}.",
            ];
        }

        ValidateBonusTiers(bonusTiers, normalizedItemIds.Count, errors);

        if (errors.Count > 0)
        {
            throw new AdminValidationException(
                "La solicitud de set contiene errores de validación.",
                errors,
                UnprocessableEntityStatusCode);
        }

        var tierInputs = MapTierInputs(bonusTiers);
        var effectsHex = ItemSetEffectsCodec.EncodeTiers(tierInputs);

        return new AdminItemSetWriteDraft(
            normalizedName,
            normalizedItemIds.Distinct().ToList(),
            effectsHex);
    }

    private void ValidateBonusTiers(
        IReadOnlyList<ItemSetBonusTierWriteDto> bonusTiers,
        int itemCount,
        IDictionary<string, string[]> errors)
    {
        if (bonusTiers.Count == 0)
        {
            return;
        }

        var optionsById = _effectsCatalog.GetOptions().ToDictionary(x => x.EffectId);
        var duplicatePieceCounts = bonusTiers
            .GroupBy(tier => tier.PieceCount)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicatePieceCounts.Length > 0)
        {
            errors["bonusTiers"] =
            [
                $"Hay tiers duplicados para: {string.Join(", ", duplicatePieceCounts)} piezas.",
            ];
        }

        foreach (var tier in bonusTiers)
        {
            if (tier.PieceCount < 2)
            {
                errors["bonusTiers"] = ["pieceCount debe ser >= 2."];
                break;
            }

            if (itemCount > 0 && tier.PieceCount > itemCount)
            {
                errors["bonusTiers"] =
                [
                    $"El tier de {tier.PieceCount} piezas excede los {itemCount} items del set.",
                ];
                break;
            }

            foreach (var effect in tier.Effects)
            {
                if (effect.EffectId <= 0)
                {
                    continue;
                }

                if (!optionsById.ContainsKey(effect.EffectId))
                {
                    errors["bonusTiers"] =
                    [
                        $"EffectId {effect.EffectId} no existe en el catálogo de efectos.",
                    ];
                    return;
                }
            }
        }
    }

    private static IReadOnlyList<ItemSetBonusTierWriteInput> MapTierInputs(
        IReadOnlyList<ItemSetBonusTierWriteDto> bonusTiers) =>
        bonusTiers
            .OrderBy(tier => tier.PieceCount)
            .Select(tier => new ItemSetBonusTierWriteInput(
                tier.PieceCount,
                tier.Effects
                    .Where(effect => effect.EffectId > 0)
                    .Select(effect => new ItemSetBonusEffectWriteInput(
                        effect.EffectId,
                        effect.Value,
                        effect.DiceNum,
                        effect.DiceSide,
                        string.IsNullOrWhiteSpace(effect.Format) ? "Integer" : effect.Format))
                    .ToList()))
            .ToList();

    private static void EnsurePositiveSetId(int setId)
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
    }
}
