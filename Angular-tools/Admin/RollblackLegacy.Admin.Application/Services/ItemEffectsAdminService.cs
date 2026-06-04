using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Exceptions;
using RollblackLegacy.Admin.Application.Models.Items;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Services;

public sealed class ItemEffectsAdminService : IItemEffectsAdminService
{
    private readonly IItemEffectsAdminRepository _repository;
    private readonly IItemsAdminWriteRepository _itemsWriteRepository;
    private readonly IItemEffectsCodec _codec;
    private readonly IItemEffectsCharacteristicCatalog _catalog;
    private readonly IItemEffectsCatalog _effectsCatalog;
    private readonly IItemEffectNameResolver _effectNameResolver;

    public ItemEffectsAdminService(
        IItemEffectsAdminRepository repository,
        IItemsAdminWriteRepository itemsWriteRepository,
        IItemEffectsCodec codec,
        IItemEffectsCharacteristicCatalog catalog,
        IItemEffectsCatalog effectsCatalog,
        IItemEffectNameResolver effectNameResolver)
    {
        _repository = repository;
        _itemsWriteRepository = itemsWriteRepository;
        _codec = codec;
        _catalog = catalog;
        _effectsCatalog = effectsCatalog;
        _effectNameResolver = effectNameResolver;
    }

    public async Task<ItemEffectsEditDto> GetEditAsync(int itemId, CancellationToken cancellationToken = default)
    {
        EnsurePositiveItemId(itemId);

        var row = await _repository.GetEffectsRowAsync(itemId, cancellationToken);
        if (row is null)
        {
            throw new AdminEntityNotFoundException("item", itemId.ToString());
        }

        await EnsureNotWeaponAsync((int)row.TypeId, cancellationToken);

        var decoded = _codec.Decode(row.Effects);
        var effects = decoded.Entries
            .Select((entry, index) => MapEditDto(entry, index))
            .ToList();

        return new ItemEffectsEditDto(
            row.ItemId,
            row.Effects ?? _codec.EmptyEffectsHex,
            effects,
            decoded.PreservedSuffixHex,
            decoded.Warnings,
            effects.Any(x => !x.IsSupported) || !string.IsNullOrWhiteSpace(decoded.PreservedSuffixHex));
    }

    public async Task<ItemEffectsUpdateResultDto> UpdateAsync(
        int itemId,
        ItemEffectsUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePositiveItemId(itemId);

        var row = await _repository.GetEffectsRowAsync(itemId, cancellationToken);
        if (row is null)
        {
            throw new AdminEntityNotFoundException("item", itemId.ToString());
        }

        await EnsureNotWeaponAsync((int)row.TypeId, cancellationToken);

        var validationErrors = ValidateRequest(request);
        if (validationErrors.Count > 0)
        {
            throw new AdminValidationException(
                "Los efectos enviados no son válidos.",
                validationErrors,
                422);
        }

        var decodedCurrent = _codec.Decode(row.Effects);
        var preservedSuffix = request.PreservedSuffixHex ?? decodedCurrent.PreservedSuffixHex;

        var removedUnsupportedRowIds = request.RemovedUnsupportedRowIds ?? [];

        if (!string.IsNullOrWhiteSpace(preservedSuffix)
            && removedUnsupportedRowIds.Count > 0)
        {
            throw new AdminValidationException(
                "No se pueden eliminar filas no soportadas sin limpiar el sufijo preservado.",
                new Dictionary<string, string[]>
                {
                    ["preservedSuffixHex"] =
                    [
                        "Elimina preservedSuffixHex o conserva los efectos no soportados."
                    ]
                },
                422);
        }

        if (removedUnsupportedRowIds.Count > 0)
        {
            preservedSuffix = null;
        }

        var entries = request.Effects
            .Where(row => string.IsNullOrWhiteSpace(row.RowId)
                || !removedUnsupportedRowIds.Contains(row.RowId))
            .Select(MapToEntry)
            .ToList();

        var encoded = _codec.Encode(entries, preservedSuffix);
        var updated = await _repository.UpdateEffectsHexAsync(itemId, encoded, cancellationToken);
        if (!updated)
        {
            throw new AdminEntityNotFoundException("item", itemId.ToString());
        }

        var decodedSaved = _codec.Decode(encoded);
        var warnings = decodedSaved.Warnings.ToList();
        if (!string.IsNullOrWhiteSpace(preservedSuffix))
        {
            warnings.Add("Se conservaron bytes no soportados al final del payload.");
        }

        return new ItemEffectsUpdateResultDto(
            itemId,
            encoded,
            decodedSaved.Entries.Select((entry, index) => MapEditDto(entry, index)).ToList(),
            warnings);
    }

    public Task<IReadOnlyList<AdminEffectOptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_effectsCatalog.GetOptions());

    private ItemEffectEditDto MapEditDto(ItemEffectEntryModel entry, int index)
    {
        var protocolName = _effectNameResolver.GetEffectName(entry.EffectId);
        var isCharacteristic = _catalog.IsCharacteristic(entry.EffectId);

        var operatorMode = entry.SerializationTypeId switch
        {
            73 => "Dice",
            82 => "MinMax",
            74 or 75 => "Duration",
            76 => "Base",
            _ => "Integer",
        };

        var label = _catalog.GetCharacteristicLabel(entry.EffectId) ?? protocolName;
        var group = entry.IsSupported
            ? _catalog.GetGroup(entry.EffectId)
            : "Other / unsupported";

        return new ItemEffectEditDto(
            RowId: entry.PreservedEffectHex is not null
                ? $"opaque-{entry.SerializationTypeId}-{entry.EffectId}-{index}"
                : $"{entry.SerializationTypeId}-{entry.EffectId}-{index}",
            entry.SerializationTypeId,
            entry.EffectId,
            label,
            entry.DiceNum,
            entry.DiceSide,
            entry.Value,
            entry.MinValue,
            entry.MaxValue,
            operatorMode,
            group,
            isCharacteristic,
            entry.IsSupported,
            entry.IsSupported ? null : "Tipo de efecto preservado pero no editable en Phase 7B.",
            entry.PreservedEffectHex,
            BuildPreviewText(entry, protocolName));
    }

    private static string BuildPreviewText(ItemEffectEntryModel entry, string protocolName)
    {
        if (!entry.IsSupported)
        {
            return $"{protocolName} (preservado, tipo {entry.SerializationTypeId})";
        }

        return entry.SerializationTypeId switch
        {
            73 => $"{protocolName}: {entry.DiceNum}d{entry.DiceSide}+{entry.Value}",
            82 => $"{protocolName}: {entry.MinValue}..{entry.MaxValue}",
            74 or 75 => $"{protocolName}: {entry.DiceNum}d {entry.DiceSide}h {entry.Value}m",
            76 => protocolName,
            _ => $"{protocolName}: {entry.Value}",
        };
    }

    private static ItemEffectEntryModel MapToEntry(ItemEffectEditRowRequest row)
    {
        if (!string.IsNullOrWhiteSpace(row.PreservedEffectHex))
        {
            return new ItemEffectEntryModel(
                row.SerializationTypeId,
                row.EffectId,
                row.DiceNum,
                row.DiceSide,
                row.Value,
                row.MinValue,
                row.MaxValue,
                IsSupported: false,
                row.PreservedEffectHex);
        }

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

    private static Dictionary<string, string[]> ValidateRequest(ItemEffectsUpdateRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Effects is null)
        {
            errors["effects"] = ["La colección de efectos es obligatoria."];
            return errors;
        }

        for (var index = 0; index < request.Effects.Count; index++)
        {
            var row = request.Effects[index];
            var prefix = $"effects[{index}]";

            if (row.EffectId <= 0)
            {
                errors[$"{prefix}.effectId"] = ["EffectId debe ser mayor que cero."];
            }

            if (row.SerializationTypeId <= 0 && string.IsNullOrWhiteSpace(row.PreservedEffectHex))
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

    private async Task EnsureNotWeaponAsync(int typeId, CancellationToken cancellationToken)
    {
        var weaponTypeIds = await _itemsWriteRepository.GetWeaponTypeIdsAsync(cancellationToken);
        if (weaponTypeIds.Contains(typeId))
        {
            throw new AdminValidationException(
                "Los items de tipo arma no pueden editar efectos en Phase 7B.",
                new Dictionary<string, string[]>
                {
                    ["typeId"] = ["Las armas viven en items_weapons y quedan fuera de alcance."]
                },
                422);
        }
    }

    private static void EnsurePositiveItemId(int itemId)
    {
        if (itemId <= 0)
        {
            throw new AdminValidationException(
                "El ItemId solicitado no es válido.",
                new Dictionary<string, string[]>
                {
                    ["itemId"] = ["itemId debe ser mayor que cero."]
                });
        }
    }
}
