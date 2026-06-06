using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Exceptions;
using RollblackLegacy.Admin.Application.Models.Items;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Services;

public sealed class ItemsAdminWriteService : IItemsAdminWriteService
{
    private const int UnprocessableEntityStatusCode = 422;

    private static readonly HashSet<int> UnsupportedWeaponTypeIds =
    [
        2, 3, 4, 5, 6, 7, 8, 19, 20, 21, 22, 83, 99, 114
    ];

    private readonly IItemsAdminWriteRepository _writeRepository;
    private readonly IItemsAdminReadRepository _readRepository;
    private readonly IItemEffectsCodec _effectsCodec;
    private readonly IItemPreviewStateResolver _previewStateResolver;
    private readonly IItemAppearancePreviewStateResolver _appearancePreviewStateResolver;

    public ItemsAdminWriteService(
        IItemsAdminWriteRepository writeRepository,
        IItemsAdminReadRepository readRepository,
        IItemEffectsCodec effectsCodec,
        IItemPreviewStateResolver previewStateResolver,
        IItemAppearancePreviewStateResolver appearancePreviewStateResolver)
    {
        _writeRepository = writeRepository;
        _readRepository = readRepository;
        _effectsCodec = effectsCodec;
        _previewStateResolver = previewStateResolver;
        _appearancePreviewStateResolver = appearancePreviewStateResolver;
    }

    public async Task<ItemWriteResultDto> CreateAsync(ItemCreateRequest request, CancellationToken cancellationToken = default)
    {
        var draft = await ValidateAndBuildDraftAsync(request, cancellationToken);
        var effectsHex = EncodeCreateEffects(request.Effects);
        draft = draft with { EffectsHex = effectsHex };
        var row = await _writeRepository.CreateAsync(draft, cancellationToken);
        return MapResult(row, "create", draft);
    }

    public async Task<ItemWriteResultDto> UpdateAsync(int itemId, ItemUpdateRequest request, CancellationToken cancellationToken = default)
    {
        EnsurePositiveItemId(itemId);

        var existing = await _writeRepository.GetByIdAsync(itemId, cancellationToken);
        if (existing is null)
        {
            throw new AdminEntityNotFoundException("item", itemId.ToString());
        }

        var draft = await ValidateAndBuildDraftAsync(request, cancellationToken);
        var row = await _writeRepository.UpdateAsync(itemId, draft, cancellationToken);
        if (row is null)
        {
            throw new AdminEntityNotFoundException("item", itemId.ToString());
        }

        return MapResult(row, "update", draft);
    }

    public async Task<ItemWriteResultDto> DuplicateAsync(int sourceItemId, ItemDuplicateRequest request, CancellationToken cancellationToken = default)
    {
        EnsurePositiveItemId(sourceItemId);

        var existing = await _writeRepository.GetByIdAsync(sourceItemId, cancellationToken);
        if (existing is null)
        {
            throw new AdminEntityNotFoundException("item", sourceItemId.ToString());
        }

        var draft = await ValidateAndBuildDraftAsync(request, cancellationToken);
        var row = await _writeRepository.DuplicateAsync(sourceItemId, draft, cancellationToken);
        if (row is null)
        {
            throw new AdminEntityNotFoundException("item", sourceItemId.ToString());
        }

        return MapResult(row, "duplicate", draft);
    }

    public Task<ItemPreviewStateDto> ResolvePreviewStateAsync(
        int? itemId,
        int? iconId,
        int? typeId = null,
        CancellationToken cancellationToken = default)
    {
        if ((!itemId.HasValue || itemId.Value <= 0) && (!iconId.HasValue || iconId.Value <= 0))
        {
            throw new AdminValidationException(
                "A preview lookup needs at least one positive identity value.",
                new Dictionary<string, string[]>
                {
                    ["previewLookup"] = ["Either itemId or iconId must be greater than zero."]
                });
        }

        return Task.FromResult(_previewStateResolver.Resolve(itemId, iconId, typeId));
    }

    public Task<ItemAppearancePreviewStateDto> ResolveAppearancePreviewStateAsync(
        int appearanceId,
        bool? appearanceKnown,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (appearanceId < 0)
        {
            throw new AdminValidationException(
                "The requested appearance id is invalid.",
                new Dictionary<string, string[]>
                {
                    ["appearanceId"] = ["appearanceId must be greater than or equal to zero."]
                });
        }

        return Task.FromResult(_appearancePreviewStateResolver.Resolve(appearanceId, appearanceKnown));
    }

    private async Task<AdminItemWriteDraft> ValidateAndBuildDraftAsync(
        ItemCreateRequest request,
        CancellationToken cancellationToken)
    {
        return await ValidateAndBuildDraftCoreAsync(
            request.ResolvedName,
            request.Description,
            request.TypeId,
            request.Level,
            request.Weight,
            request.Price,
            request.IconId,
            request.AppearanceId,
            request.SetId,
            request.Conditions,
            request.IsVisible,
            request.Usable,
            request.Targetable,
            request.TwoHanded,
            request.Etheral,
            cancellationToken);
    }

    private async Task<AdminItemWriteDraft> ValidateAndBuildDraftAsync(
        ItemUpdateRequest request,
        CancellationToken cancellationToken)
    {
        return await ValidateAndBuildDraftCoreAsync(
            request.ResolvedName,
            request.Description,
            request.TypeId,
            request.Level,
            request.Weight,
            request.Price,
            request.IconId,
            request.AppearanceId,
            request.SetId,
            request.Conditions,
            request.IsVisible,
            request.Usable,
            request.Targetable,
            request.TwoHanded,
            request.Etheral,
            cancellationToken);
    }

    private async Task<AdminItemWriteDraft> ValidateAndBuildDraftAsync(
        ItemDuplicateRequest request,
        CancellationToken cancellationToken)
    {
        return await ValidateAndBuildDraftCoreAsync(
            request.ResolvedName,
            request.Description,
            request.TypeId,
            request.Level,
            request.Weight,
            request.Price,
            request.IconId,
            request.AppearanceId,
            request.SetId,
            request.Conditions,
            request.IsVisible,
            request.Usable,
            request.Targetable,
            request.TwoHanded,
            request.Etheral,
            cancellationToken);
    }

    private async Task<AdminItemWriteDraft> ValidateAndBuildDraftCoreAsync(
        string? resolvedName,
        string? description,
        int typeId,
        int level,
        int weight,
        double price,
        int iconId,
        int appearanceId,
        int? setId,
        string? conditions,
        bool? isVisible,
        bool usable,
        bool targetable,
        bool twoHanded,
        bool etheral,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        var normalizedName = (resolvedName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            errors["resolvedName"] = ["ResolvedName is required."];
        }

        if (level < 1)
        {
            errors["level"] = ["Level must be greater than or equal to 1."];
        }

        if (weight < 0)
        {
            errors["weight"] = ["Weight must be greater than or equal to 0."];
        }

        if (price < 0)
        {
            errors["price"] = ["Price must be greater than or equal to 0."];
        }

        if (iconId < 0)
        {
            errors["iconId"] = ["IconId must be greater than or equal to 0."];
        }

        if (appearanceId < 0)
        {
            errors["appearanceId"] = ["AppearanceId must be greater than or equal to 0."];
        }

        if (setId.HasValue && setId.Value <= 0)
        {
            errors["setId"] = ["SetId must be greater than zero when supplied."];
        }

        var knownTypeIds = (await _readRepository.GetTypeOptionsAsync(cancellationToken))
            .Select(x => x.Value)
            .ToHashSet();

        if (!knownTypeIds.Contains(typeId))
        {
            errors["typeId"] = ["TypeId does not map to a known item type option."];
        }
        else
        {
            var liveWeaponTypeIds = await _writeRepository.GetWeaponTypeIdsAsync(cancellationToken);
            if (UnsupportedWeaponTypeIds.Contains(typeId) || liveWeaponTypeIds.Contains(typeId))
            {
                errors["typeId"] =
                [
                    "Weapon item types are not supported by Phase 7 because Sunshine stores them in items_weapons, not items."
                ];
            }
        }

        if (setId.HasValue && !await _writeRepository.ItemSetExistsAsync(setId.Value, cancellationToken))
        {
            errors["setId"] = ["SetId does not resolve to an existing items_sets row."];
        }

        if (errors.Count > 0)
        {
            throw new AdminValidationException(
                "The item write payload is invalid.",
                errors,
                statusCode: UnprocessableEntityStatusCode);
        }

        return new AdminItemWriteDraft(
            normalizedName,
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            typeId,
            level,
            weight,
            price,
            iconId,
            appearanceId,
            setId,
            NormalizeConditions(conditions),
            isVisible,
            usable,
            targetable,
            twoHanded,
            etheral);
    }

    private ItemWriteResultDto MapResult(AdminItemWriteRow row, string operation, AdminItemWriteDraft draft)
    {
        var previewState = _previewStateResolver.Resolve(row.ItemId, row.IconId, row.TypeId);
        var warnings = BuildWarnings(row, draft, previewState);

        return new ItemWriteResultDto(
            row.ItemId,
            operation,
            row.ResolvedName,
            row.DescriptionId,
            DescriptionPersisted: false,
            IsVisiblePersisted: false,
            DetailPath: $"/admin/items/{row.ItemId}",
            previewState,
            warnings);
    }

    private static IReadOnlyList<ItemWriteValidationProblem> BuildWarnings(
        AdminItemWriteRow row,
        AdminItemWriteDraft draft,
        ItemPreviewStateDto previewState)
    {
        var warnings = new List<ItemWriteValidationProblem>
        {
            new(
                "IDENTITY_RULE_REMINDER",
                "info",
                "ItemId, IconId, and AppearanceId are different identities and remain stored separately.",
                null),
            new(
                "DESCRIPTION_NOT_PERSISTED",
                "warning",
                "Phase 7 does not persist free-text Description because sunshine.items only stores DescriptionId. A runtime DescriptionId is retained or allocated, but no client text publish happens yet.",
                "description"),
            new(
                "IS_VISIBLE_NOT_PERSISTED",
                "info",
                "IsVisible has no direct column in sunshine.items, so this field remains deferred in Phase 7.",
                "isVisible")
        };

        if (previewState.State == "MISSING" || previewState.State == "UNKNOWN")
        {
            warnings.Add(new ItemWriteValidationProblem(
                "PREVIEW_NOT_RESOLVED",
                "warning",
                "No preview PNG is currently resolved for the selected IconId. Save is still allowed.",
                "iconId"));
        }

        if (row.IconId <= 0)
        {
            warnings.Add(new ItemWriteValidationProblem(
                "ICON_ID_ZERO",
                "warning",
                "The saved row has IconId <= 0, so preview and client identity will remain weak.",
                "iconId"));
        }

        if (row.AppearanceId <= 0)
        {
            warnings.Add(new ItemWriteValidationProblem(
                "APPEARANCE_ID_ZERO",
                "info",
                "The saved row has AppearanceId <= 0. Inventory preview can still work, but equipped look identity remains unresolved.",
                "appearanceId"));
        }

        if (draft.SetId is null)
        {
            warnings.Add(new ItemWriteValidationProblem(
                "NO_ITEM_SET",
                "info",
                "The saved row is not linked to an item set.",
                "setId"));
        }

        return warnings;
    }

    private static string NormalizeConditions(string? conditions)
    {
        return string.IsNullOrWhiteSpace(conditions) ? "null" : conditions.Trim();
    }

    private string EncodeCreateEffects(IReadOnlyList<ItemEffectEditRowRequest>? effects)
    {
        if (effects is null || effects.Count == 0)
        {
            return _effectsCodec.EmptyEffectsHex;
        }

        var validationErrors = ValidateCreateEffects(effects);
        if (validationErrors.Count > 0)
        {
            throw new AdminValidationException(
                "Los efectos enviados no son válidos.",
                validationErrors,
                UnprocessableEntityStatusCode);
        }

        var entries = effects
            .Select(MapCreateEffectToEntry)
            .ToList();

        return _effectsCodec.Encode(entries, preservedSuffixHex: null);
    }

    private static Dictionary<string, string[]> ValidateCreateEffects(IReadOnlyList<ItemEffectEditRowRequest> effects)
    {
        var errors = new Dictionary<string, string[]>();

        for (var index = 0; index < effects.Count; index++)
        {
            var row = effects[index];
            var prefix = $"effects[{index}]";

            if (!string.IsNullOrWhiteSpace(row.PreservedEffectHex))
            {
                errors[$"{prefix}.preservedEffectHex"] =
                [
                    "Las filas preservadas no están soportadas al crear un item."
                ];
            }

            if (row.EffectId <= 0)
            {
                errors[$"{prefix}.effectId"] = ["EffectId debe ser mayor que cero."];
            }

            if (row.SerializationTypeId <= 0)
            {
                errors[$"{prefix}.serializationTypeId"] = ["SerializationTypeId es obligatorio en filas editables."];
            }

            if (row.DiceNum < 0 || row.DiceSide < 0 || row.Value < 0 || row.MinValue < 0 || row.MaxValue < 0)
            {
                errors[$"{prefix}.value"] = ["Los valores no pueden ser negativos."];
            }
        }

        return errors;
    }

    private static ItemEffectEntryModel MapCreateEffectToEntry(ItemEffectEditRowRequest row)
    {
        return new ItemEffectEntryModel(
            row.SerializationTypeId,
            row.EffectId,
            row.DiceNum,
            row.DiceSide,
            row.Value,
            row.MinValue,
            row.MaxValue,
            IsSupported: true,
            null);
    }

    private static void EnsurePositiveItemId(int itemId)
    {
        if (itemId <= 0)
        {
            throw new AdminValidationException(
                "The requested item id is invalid.",
                new Dictionary<string, string[]>
                {
                    ["itemId"] = ["itemId must be greater than zero."]
                });
        }
    }
}
