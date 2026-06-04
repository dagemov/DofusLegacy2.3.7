using System.Text.RegularExpressions;

namespace RollblackLegacy.Admin.Infrastructure.Items;

internal static class ItemEffectDisplayMetadata
{
    private static readonly Regex SplitPascalCaseRegex = new(
        "([a-z])([A-Z0-9])",
        RegexOptions.Compiled);

    private static readonly HashSet<int> DiceEffectIds = new(
    [
        77, 81, 82, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100,
        108, 145, 440, 441
    ]);

    public static string GetDisplayLabel(int effectId, string protocolName)
    {
        if (LegacyGameEffectDisplayLabels.ByEffectId.TryGetValue(effectId, out var entry))
        {
            return entry.Label;
        }

        return HumanizeProtocolName(protocolName);
    }

    public static int GetSortPriority(int effectId, string displayLabel)
    {
        if (LegacyGameEffectDisplayLabels.ByEffectId.TryGetValue(effectId, out var entry))
        {
            return entry.Priority;
        }

        return 1000 + effectId;
    }

    public static string ResolveGroup(int effectId, string protocolName, string displayLabel) =>
        LegacyBlazorEffectLabelRegistry.ResolveGroup(effectId, protocolName, displayLabel);

    public static string SuggestFormat(int effectId, string protocolName)
    {
        if (DiceEffectIds.Contains(effectId))
        {
            return "Dice";
        }

        if (protocolName.Contains("Damage", StringComparison.OrdinalIgnoreCase)
            || protocolName.Contains("Steal", StringComparison.OrdinalIgnoreCase)
            || protocolName.Contains("HealHP", StringComparison.OrdinalIgnoreCase))
        {
            return "Dice";
        }

        return "Integer";
    }

    public static short MapFormatToSerializationTypeId(string format) =>
        format switch
        {
            "Dice" => SunshineItemEffectsCodec.TypeDice,
            "MinMax" => SunshineItemEffectsCodec.TypeMinMax,
            "Duration" => SunshineItemEffectsCodec.TypeDuration,
            _ => SunshineItemEffectsCodec.TypeInteger,
        };

    private static string HumanizeProtocolName(string protocolName)
    {
        var fallback = protocolName;
        if (fallback.StartsWith("Effect_", StringComparison.Ordinal))
        {
            fallback = fallback["Effect_".Length..];
        }

        fallback = fallback.Replace("Add", "Agregar ", StringComparison.Ordinal)
            .Replace("Sub", "Quitar ", StringComparison.Ordinal)
            .Replace('_', ' ');

        return SplitPascalCaseRegex.Replace(fallback, "$1 $2").Trim();
    }
}
