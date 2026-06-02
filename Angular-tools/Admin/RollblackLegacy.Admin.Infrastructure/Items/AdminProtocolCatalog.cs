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
        if (string.IsNullOrWhiteSpace(hex))
            return Array.Empty<AdminItemEffectReadModel>();

        try
        {
            var data = Convert.FromHexString(hex);
            if (data.Length < 2)
                return Array.Empty<AdminItemEffectReadModel>();

            var offset = 0;
            var count = ReadInt16(data, ref offset);
            var effects = new List<AdminItemEffectReadModel>(count);

            for (var index = 0; index < count && offset + 4 <= data.Length; index++)
            {
                var typeId = ReadInt16(data, ref offset);
                var actionId = ReadInt16(data, ref offset);
                var description = GetEffectName(actionId);

                switch (typeId)
                {
                    case 70:
                    {
                        var value = offset + 2 <= data.Length ? ReadInt16(data, ref offset) : 0;
                        effects.Add(new AdminItemEffectReadModel(actionId, 0, 0, value, description));
                        break;
                    }
                    case 73:
                    {
                        var diceNum = offset + 2 <= data.Length ? ReadInt16(data, ref offset) : 0;
                        var diceSide = offset + 2 <= data.Length ? ReadInt16(data, ref offset) : 0;
                        var diceConst = offset + 2 <= data.Length ? ReadInt16(data, ref offset) : 0;
                        effects.Add(new AdminItemEffectReadModel(actionId, diceNum, diceSide, diceConst, description));
                        break;
                    }
                    case 82:
                    {
                        var min = offset + 2 <= data.Length ? ReadInt16(data, ref offset) : 0;
                        var max = offset + 2 <= data.Length ? ReadInt16(data, ref offset) : 0;
                        effects.Add(new AdminItemEffectReadModel(actionId, min, max, 0, description));
                        break;
                    }
                    case 71:
                    {
                        var familyId = offset + 2 <= data.Length ? ReadInt16(data, ref offset) : 0;
                        effects.Add(new AdminItemEffectReadModel(actionId, 0, 0, familyId, description));
                        break;
                    }
                    case 74:
                    {
                        var days = offset + 2 <= data.Length ? ReadInt16(data, ref offset) : 0;
                        var hours = offset + 2 <= data.Length ? ReadInt16(data, ref offset) : 0;
                        var minutes = offset + 2 <= data.Length ? ReadInt16(data, ref offset) : 0;
                        effects.Add(new AdminItemEffectReadModel(actionId, days, hours, minutes, description));
                        break;
                    }
                    case 76:
                    {
                        effects.Add(new AdminItemEffectReadModel(actionId, 0, 0, 0, description));
                        break;
                    }
                    default:
                    {
                        effects.Add(new AdminItemEffectReadModel(actionId, 0, 0, 0, description));
                        return effects;
                    }
                }
            }

            return effects;
        }
        catch
        {
            return Array.Empty<AdminItemEffectReadModel>();
        }
    }

    private string GetEffectName(int effectId)
    {
        return _effectNames.Value.TryGetValue(effectId, out var name)
            ? name
            : $"Effect_{effectId}";
    }

    private static short ReadInt16(byte[] data, ref int offset)
    {
        var value = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(offset, 2));
        offset += 2;
        return value;
    }

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
