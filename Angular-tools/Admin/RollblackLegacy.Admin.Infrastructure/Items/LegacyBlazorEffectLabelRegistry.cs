namespace RollblackLegacy.Admin.Infrastructure.Items;

/// <summary>
/// Spanish labels and UI groups ported from Rollback.Admin GameEffectDisplayService
/// for item characteristics editing (Phase 7B functional port).
/// </summary>
public static class LegacyBlazorEffectLabelRegistry
{
    public sealed record EffectUiMetadata(
        string Label,
        string Group,
        string OperatorMode,
        short DefaultSerializationTypeId,
        int SortPriority);

    private static readonly (int EffectId, EffectUiMetadata Metadata)[] Characteristics =
    [
        (111, new("+ PA", "Principales", "Integer", SunshineItemEffectsCodec.TypeInteger, 0)),
        (168, new("- PA", "Principales", "Integer", SunshineItemEffectsCodec.TypeInteger, 1)),
        (128, new("+ PM", "Principales", "Integer", SunshineItemEffectsCodec.TypeInteger, 2)),
        (19, new("+ PM", "Principales", "Integer", SunshineItemEffectsCodec.TypeInteger, 2)),
        (61, new("+ Vitalidad", "Stats", "Integer", SunshineItemEffectsCodec.TypeInteger, 4)),
        (153, new("- Vitalidad", "Stats", "Integer", SunshineItemEffectsCodec.TypeInteger, 5)),
        (54, new("+ Fuerza", "Stats", "Integer", SunshineItemEffectsCodec.TypeInteger, 9)),
        (157, new("- Fuerza", "Stats", "Integer", SunshineItemEffectsCodec.TypeInteger, 10)),
        (62, new("+ Inteligencia", "Stats", "Integer", SunshineItemEffectsCodec.TypeInteger, 11)),
        (155, new("- Inteligencia", "Stats", "Integer", SunshineItemEffectsCodec.TypeInteger, 12)),
        (59, new("+ Suerte", "Stats", "Integer", SunshineItemEffectsCodec.TypeInteger, 13)),
        (152, new("- Suerte", "Stats", "Integer", SunshineItemEffectsCodec.TypeInteger, 14)),
        (55, new("+ Agilidad", "Stats", "Integer", SunshineItemEffectsCodec.TypeInteger, 15)),
        (154, new("- Agilidad", "Stats", "Integer", SunshineItemEffectsCodec.TypeInteger, 16)),
        (60, new("+ Sabiduria", "Stats", "Integer", SunshineItemEffectsCodec.TypeInteger, 17)),
        (156, new("- Sabiduria", "Stats", "Integer", SunshineItemEffectsCodec.TypeInteger, 18)),
        (53, new("+ Alcance", "Principales", "Integer", SunshineItemEffectsCodec.TypeInteger, 19)),
        (51, new("+ Golpes criticos", "Combate", "Integer", SunshineItemEffectsCodec.TypeInteger, 21)),
        (118, new("+ Danos", "Combate", "Integer", SunshineItemEffectsCodec.TypeInteger, 23)),
        (114, new("+ Invocaciones", "Especiales", "Integer", SunshineItemEffectsCodec.TypeInteger, 33)),
        (93, new("- Pods", "Especiales", "Integer", SunshineItemEffectsCodec.TypeInteger, 35)),
        (210, new("+ % Resistencia Tierra", "Resistencias", "Integer", SunshineItemEffectsCodec.TypeInteger, 36)),
        (211, new("+ % Resistencia Agua", "Resistencias", "Integer", SunshineItemEffectsCodec.TypeInteger, 37)),
        (212, new("+ % Resistencia Aire", "Resistencias", "Integer", SunshineItemEffectsCodec.TypeInteger, 38)),
        (213, new("+ % Resistencia Fuego", "Resistencias", "Integer", SunshineItemEffectsCodec.TypeInteger, 39)),
        (214, new("+ % Resistencia Neutro", "Resistencias", "Integer", SunshineItemEffectsCodec.TypeInteger, 40)),
    ];

    private static readonly Dictionary<int, EffectUiMetadata> ByEffectId =
        Characteristics.ToDictionary(x => x.EffectId, x => x.Metadata);

    public static bool TryGetCharacteristic(int effectId, out EffectUiMetadata metadata) =>
        ByEffectId.TryGetValue(effectId, out metadata!);

    public static IReadOnlyList<(int EffectId, EffectUiMetadata Metadata)> GetCharacteristicOptions() =>
        Characteristics;

    public static string ResolveGroup(int effectId, string protocolName, string? characteristicLabel)
    {
        if (TryGetCharacteristic(effectId, out var metadata))
        {
            return metadata.Group;
        }

        var label = characteristicLabel ?? protocolName;

        if (label.Contains("PA", StringComparison.OrdinalIgnoreCase)
            || label.Contains("PM", StringComparison.OrdinalIgnoreCase)
            || label.Contains("Alcance", StringComparison.OrdinalIgnoreCase))
        {
            return "Principales";
        }

        if (label.Contains("Vitalidad", StringComparison.OrdinalIgnoreCase)
            || label.Contains("Vida", StringComparison.OrdinalIgnoreCase)
            || label.Contains("Fuerza", StringComparison.OrdinalIgnoreCase)
            || label.Contains("Inteligencia", StringComparison.OrdinalIgnoreCase)
            || label.Contains("Suerte", StringComparison.OrdinalIgnoreCase)
            || label.Contains("Agilidad", StringComparison.OrdinalIgnoreCase)
            || label.Contains("Sabiduria", StringComparison.OrdinalIgnoreCase))
        {
            return "Stats";
        }

        if (label.Contains("Resistencia", StringComparison.OrdinalIgnoreCase))
        {
            return "Resistencias";
        }

        if (label.Contains("Danos", StringComparison.OrdinalIgnoreCase)
            || label.Contains("criticos", StringComparison.OrdinalIgnoreCase))
        {
            return "Combate";
        }

        return "Especiales";
    }
}
