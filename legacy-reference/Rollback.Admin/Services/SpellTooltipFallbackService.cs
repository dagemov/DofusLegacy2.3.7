using Rollback.Admin.Models.GameEffects;
using Rollback.Admin.Models.Spells;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class SpellTooltipFallbackService
{
    public string BuildDescription(SpellEditModel model)
    {
        var level = model.Levels
            .OrderBy(level => level.LevelNumber)
            .LastOrDefault(level => level.Effects.Count > 0 || level.CriticalEffects.Count > 0);

        if (level is null)
            return string.Empty;

        var normal = BuildLines(level.Effects);
        var critical = BuildLines(level.CriticalEffects);
        if (normal.Count == 0 && critical.Count == 0)
            return string.Empty;

        var sections = new List<string>();

        if (normal.Count > 0)
            sections.Add($"Nivel {level.LevelNumber}: {string.Join("; ", normal)}");

        if (critical.Count > 0)
            sections.Add($"Critico: {string.Join("; ", critical)}");

        return string.Join(Environment.NewLine, sections.Where(section => !string.IsNullOrWhiteSpace(section)));
    }

    private static IReadOnlyList<string> BuildLines(IReadOnlyList<GameEffectEditRow> effects)
    {
        var fragments = new List<EffectTextFragment>();
        foreach (var effect in effects)
        {
            if (TryBuildElementalFragment(effect, out var elemental))
            {
                fragments.Add(elemental);
                continue;
            }

            if (TryBuildGenericFragment(effect, out var generic))
                fragments.Add(generic);
        }

        if (fragments.Count == 0)
            return Array.Empty<string>();

        var merged = MergeElementalFragments(fragments);
        return merged
            .Select(fragment => fragment.ToDisplayText())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();
    }

    private static IReadOnlyList<EffectTextFragment> MergeElementalFragments(IReadOnlyList<EffectTextFragment> fragments)
    {
        var merged = new List<EffectTextFragment>();

        foreach (var group in fragments
                     .Where(fragment => fragment.Kind is EffectTextKind.Damage or EffectTextKind.StealLife)
                     .GroupBy(fragment => new { fragment.Kind, fragment.RangeText, fragment.DurationSuffix }))
        {
            var single = group.First();
            merged.Add(single with
            {
                ElementNames = group
                    .SelectMany(fragment => fragment.ElementNames)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
            });
        }

        merged.AddRange(fragments.Where(fragment => fragment.Kind is not (EffectTextKind.Damage or EffectTextKind.StealLife)));
        return merged;
    }

    private static bool TryBuildElementalFragment(GameEffectEditRow effect, out EffectTextFragment fragment)
    {
        fragment = default!;

        if (!TryResolveRange(effect, out var rangeText))
            return false;

        var element = effect.EffectId switch
        {
            EffectId.EffectDamageAir or EffectId.EffectStealHPAir => "aire",
            EffectId.EffectDamageFire or EffectId.EffectStealHPFire => "fuego",
            EffectId.EffectDamageWater or EffectId.EffectStealHPWater => "agua",
            EffectId.EffectDamageEarth or EffectId.EffectStealHPEarth => "tierra",
            EffectId.EffectDamageNeutral or EffectId.EffectStealHPNeutral => "neutral",
            _ => string.Empty,
        };

        if (string.IsNullOrWhiteSpace(element))
            return false;

        var kind = effect.EffectId switch
        {
            EffectId.EffectDamageAir or
            EffectId.EffectDamageFire or
            EffectId.EffectDamageWater or
            EffectId.EffectDamageEarth or
            EffectId.EffectDamageNeutral => EffectTextKind.Damage,
            _ => EffectTextKind.StealLife,
        };

        fragment = new EffectTextFragment(
            kind,
            rangeText,
            Array.Empty<string>(),
            new[] { element },
            BuildDurationSuffix(effect.Duration));
        return true;
    }

    private static bool TryBuildGenericFragment(GameEffectEditRow effect, out EffectTextFragment fragment)
    {
        fragment = default!;

        if (!TryResolveRange(effect, out var magnitude))
            return false;

        string? text = effect.EffectId switch
        {
            EffectId.EffectAddAP111 or EffectId.EffectRegainAP => $"+{magnitude} PA",
            EffectId.EffectLostAP => $"-{magnitude} PA",
            EffectId.EffectAddMP or EffectId.EffectAddMP128 => $"+{magnitude} PM",
            EffectId.EffectLostMP => $"-{magnitude} PM",
            EffectId.EffectAddRange or EffectId.EffectAddRange136 => $"+{magnitude} alcance",
            EffectId.EffectSubRange => $"-{magnitude} alcance",
            EffectId.Effect701 => $"+{magnitude} potencia",
            EffectId.EffectAddDamageBonus or EffectId.EffectAddDamageBonus121 or EffectId.EffectIncreaseDamage138 => $"+{magnitude} danos",
            EffectId.EffectSubDamageBonus => $"-{magnitude} danos",
            EffectId.EffectAddStrength => $"+{magnitude} fuerza",
            EffectId.EffectSubStrength => $"-{magnitude} fuerza",
            EffectId.EffectAddIntelligence => $"+{magnitude} inteligencia",
            EffectId.EffectSubIntelligence => $"-{magnitude} inteligencia",
            EffectId.EffectAddChance => $"+{magnitude} suerte",
            EffectId.EffectSubChance => $"-{magnitude} suerte",
            EffectId.EffectAddAgility => $"+{magnitude} agilidad",
            EffectId.EffectSubAgility => $"-{magnitude} agilidad",
            EffectId.EffectAddWisdom => $"+{magnitude} sabiduria",
            EffectId.EffectSubWisdom => $"-{magnitude} sabiduria",
            EffectId.EffectAddVitality => $"+{magnitude} vitalidad",
            EffectId.EffectSubVitality => $"-{magnitude} vitalidad",
            EffectId.EffectAddHealBonus => $"+{magnitude} curas",
            EffectId.EffectSubHealBonus => $"-{magnitude} curas",
            EffectId.EffectAddDodgeAPProbability => $"+{magnitude}% esquiva PA",
            EffectId.EffectSubDodgeAPProbability => $"-{magnitude}% esquiva PA",
            EffectId.EffectAddDodgeMPProbability => $"+{magnitude}% esquiva PM",
            EffectId.EffectSubDodgeMPProbability => $"-{magnitude}% esquiva PM",
            EffectId.EffectAddCriticalHit => $"+{magnitude} golpes criticos",
            EffectId.EffectSubCriticalHit => $"-{magnitude} golpes criticos",
            EffectId.EffectHealHP81 or EffectId.EffectHealHP108 or EffectId.EffectHealHP143 => $"Cura {magnitude}",
            _ => null,
        };

        if (text is null)
            return false;

        fragment = new EffectTextFragment(
            EffectTextKind.Generic,
            string.Empty,
            new[] { $"{text}{BuildDurationSuffix(effect.Duration)}" },
            Array.Empty<string>(),
            string.Empty);
        return true;
    }

    private static bool TryResolveRange(GameEffectEditRow effect, out string text)
    {
        text = string.Empty;

        var min = effect.MinValue;
        var max = effect.MaxValue;
        var value = effect.Value;

        if (min != 0 || max != 0)
        {
            if (max != 0 && max != min)
            {
                text = $"{Math.Abs(min)} a {Math.Abs(max)}";
                return true;
            }

            var single = min != 0 ? min : max;
            if (single != 0)
            {
                text = Math.Abs(single).ToString();
                return true;
            }
        }

        if (value == 0)
            return false;

        text = Math.Abs(value).ToString();
        return true;
    }

    private static string BuildDurationSuffix(short duration) =>
        duration > 0
            ? duration == 1
                ? " (1 turno)"
                : $" ({duration} turnos)"
            : string.Empty;

    private enum EffectTextKind
    {
        Damage,
        StealLife,
        Generic,
    }

    private sealed record EffectTextFragment(
        EffectTextKind Kind,
        string RangeText,
        IReadOnlyList<string> LiteralTexts,
        IReadOnlyList<string> ElementNames,
        string DurationSuffix)
    {
        public string ToDisplayText() =>
            Kind switch
            {
                EffectTextKind.Damage => ElementNames.Count switch
                {
                    0 => string.Empty,
                    1 => $"{RangeText} {ElementNames[0]}{DurationSuffix}",
                    _ => $"{RangeText} ({string.Join(", ", ElementNames)}){DurationSuffix}",
                },
                EffectTextKind.StealLife => ElementNames.Count switch
                {
                    0 => string.Empty,
                    1 => $"Roba {RangeText} vida ({ElementNames[0]}){DurationSuffix}",
                    _ => $"Roba {RangeText} vida ({string.Join(", ", ElementNames)}){DurationSuffix}",
                },
                _ => LiteralTexts.FirstOrDefault() ?? string.Empty,
            };
    }
}
