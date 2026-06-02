using System.Buffers.Binary;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using RollblackLegacy.Admin.Application.Models.Items;
using RollblackLegacy.Admin.Contracts.Common;

namespace RollblackLegacy.Admin.Infrastructure.Items;

public sealed class AdminProtocolCatalog
{
    private static readonly Regex EnumEntryRegex = new(
        @"^\s*(?<name>[A-Za-z0-9_]+)\s*=\s*(?<value>-?\d+),?\s*$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private readonly Lazy<Dictionary<int, string>> _itemTypes;
    private readonly Lazy<Dictionary<int, string>> _effectNames;

    public AdminProtocolCatalog(IHostEnvironment hostEnvironment)
    {
        var repositoryRoot = AdminRepositoryPathResolver.ResolveRepositoryRoot(hostEnvironment.ContentRootPath);

        _itemTypes = new Lazy<Dictionary<int, string>>(() =>
            LoadEnumEntries(
                Path.Combine(repositoryRoot, "Sunshine net11.0", "Sunshine net11.0", "Sunshine.Protocol", "Enums", "ItemTypeEnum.cs"),
                HumanizeEnumName));

        _effectNames = new Lazy<Dictionary<int, string>>(() =>
            LoadEnumEntries(
                Path.Combine(repositoryRoot, "Sunshine net11.0", "Sunshine net11.0", "Sunshine.Protocol", "Enums", "EffectsEnum.cs"),
                name => name));
    }

    public string? GetItemTypeLabel(int typeId)
    {
        return _itemTypes.Value.TryGetValue(typeId, out var label) ? label : null;
    }

    public IReadOnlyList<AdminOptionDto> GetItemTypeOptions()
    {
        return _itemTypes.Value
            .OrderBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
            .Select(x => new AdminOptionDto(x.Key, x.Value))
            .ToList();
    }

    public IReadOnlyList<AdminItemEffectReadModel> DecodeItemEffects(string? hex)
    {
        var codec = new SunshineItemEffectsCodec();
        var decoded = codec.Decode(hex);

        return decoded.Entries
            .Where(x => x.IsSupported)
            .Select(entry =>
            {
                var description = GetEffectName(entry.EffectId);
                return entry.SerializationTypeId switch
                {
                    SunshineItemEffectsCodec.TypeDice => new AdminItemEffectReadModel(
                        entry.EffectId,
                        entry.DiceNum,
                        entry.DiceSide,
                        entry.Value,
                        description),
                    SunshineItemEffectsCodec.TypeMinMax => new AdminItemEffectReadModel(
                        entry.EffectId,
                        entry.MinValue,
                        entry.MaxValue,
                        0,
                        description),
                    _ => new AdminItemEffectReadModel(
                        entry.EffectId,
                        entry.DiceNum,
                        entry.DiceSide,
                        entry.Value,
                        description),
                };
            })
            .ToList();
    }

    public string GetEffectName(int effectId) =>
        _effectNames.Value.TryGetValue(effectId, out var name)
            ? name
            : $"Effect_{effectId}";

    private static Dictionary<int, string> LoadEnumEntries(string filePath, Func<string, string> labelFactory)
    {
        var entries = new Dictionary<int, string>();
        if (!File.Exists(filePath))
            return entries;

        var text = File.ReadAllText(filePath);
        foreach (Match match in EnumEntryRegex.Matches(text))
        {
            if (!int.TryParse(match.Groups["value"].Value, out var value))
                continue;

            var name = match.Groups["name"].Value;
            entries[value] = labelFactory(name);
        }

        return entries;
    }

    private static string HumanizeEnumName(string value)
    {
        return string.Join(
            ' ',
            value.Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => char.ToUpperInvariant(x[0]) + x[1..].ToLowerInvariant()));
    }
}
