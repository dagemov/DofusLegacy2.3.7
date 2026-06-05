using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;

namespace RollblackLegacy.Admin.Infrastructure.Items;

/// <summary>
/// Loads every <c>EffectsEnum</c> action id (including implicit enum increments) from Sunshine.Protocol source.
/// </summary>
public sealed class EffectsEnumCatalogReader
{
    private static readonly Regex MemberRegex = new(
        @"^\s*Effect_(?<name>\w+)(?:\s*=\s*(?<value>-?\d+))?\s*,?\s*$",
        RegexOptions.Compiled);

    private readonly Lazy<IReadOnlyList<int>> _effectIds;

    public EffectsEnumCatalogReader(IHostEnvironment hostEnvironment)
    {
        var repositoryRoot = AdminRepositoryPathResolver.ResolveRepositoryRoot(hostEnvironment.ContentRootPath);
        var enumPath = Path.Combine(
            repositoryRoot,
            "Sunshine net11.0",
            "Sunshine net11.0",
            "Sunshine.Protocol",
            "Enums",
            "EffectsEnum.cs");

        _effectIds = new Lazy<IReadOnlyList<int>>(() => LoadEffectIds(enumPath));
    }

    public IReadOnlyList<int> GetEffectIds() => _effectIds.Value;

    private static IReadOnlyList<int> LoadEffectIds(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Array.Empty<int>();
        }

        var ids = new List<int>();
        var current = 0;
        var started = false;

        foreach (var line in File.ReadLines(filePath))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("End", StringComparison.Ordinal))
            {
                break;
            }

            var match = MemberRegex.Match(trimmed);
            if (!match.Success)
            {
                continue;
            }

            if (match.Groups["value"].Success)
            {
                current = int.Parse(match.Groups["value"].Value);
                started = true;
            }
            else if (started)
            {
                current++;
            }
            else
            {
                continue;
            }

            ids.Add(current);
        }

        return ids;
    }
}
