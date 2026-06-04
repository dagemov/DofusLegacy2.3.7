using System.Text;
using System.Text.RegularExpressions;
using Rollback.Admin.Models.Common;

namespace Rollback.Admin.Services;

public sealed class I18nCatalogService
{
    private static readonly Regex TextEntryRegex = new(
        "_datas\\[(?<id>\\d+)\\]\\s*=\\s*\"(?<text>(?:\\\\.|[^\"])*)\";",
        RegexOptions.Compiled);

    private readonly ClientDataPathResolver _pathResolver;
    private readonly Lazy<IReadOnlyList<I18nCatalogEntry>> _entries;

    public I18nCatalogService(ClientDataPathResolver pathResolver)
    {
        _pathResolver = pathResolver;
        _entries = new Lazy<IReadOnlyList<I18nCatalogEntry>>(LoadEntries, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IReadOnlyList<I18nCatalogEntry> Search(string? search, int maxResults = 100)
    {
        var normalized = (search ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return Array.Empty<I18nCatalogEntry>();

        return _entries.Value
            .Where(entry =>
                entry.TextId.ToString().Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                entry.Text.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.TextId)
            .Take(Math.Max(1, maxResults))
            .ToArray();
    }

    private IReadOnlyList<I18nCatalogEntry> LoadEntries()
    {
        var result = new List<I18nCatalogEntry>();
        foreach (var directory in EnumerateI18nTmpDirectories())
        {
            if (!Directory.Exists(directory.Path))
                continue;

            foreach (var file in Directory.EnumerateFiles(directory.Path, "i18n*.as", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var content = File.ReadAllText(file, Encoding.UTF8);
                    foreach (Match match in TextEntryRegex.Matches(content))
                    {
                        if (!int.TryParse(match.Groups["id"].Value, out var textId))
                            continue;

                        result.Add(new I18nCatalogEntry
                        {
                            TextId = textId,
                            Text = DecodeText(match.Groups["text"].Value),
                            SourceFile = Path.GetFileName(file),
                            LanguageCode = directory.LanguageCode,
                            EntityType = InferEntityType(textId),
                        });
                    }
                }
                catch
                {
                    // Keep the admin responsive even if a local exported i18n file is malformed.
                }
            }
        }

        return result
            .GroupBy(entry => (entry.LanguageCode, entry.TextId))
            .Select(group => group.First())
            .OrderBy(entry => entry.LanguageCode)
            .ThenBy(entry => entry.TextId)
            .ToArray();
    }

    private IEnumerable<(string Path, string LanguageCode)> EnumerateI18nTmpDirectories()
    {
        if (_pathResolver.SpanishI18nTmpDirectory is { Length: > 0 } spanishTmp)
            yield return (spanishTmp, "es");

        if (_pathResolver.ClientApplicationDirectory is { Length: > 0 } clientApp)
        {
            var dataDirectory = Path.Combine(clientApp, "data");
            yield return (Path.Combine(dataDirectory, "i18n_en", "tmp"), "en");
        }
    }

    private static string DecodeText(string input) =>
        input.Replace("\\\"", "\"")
             .Replace("\\n", "\n")
             .Replace("\\r", "\r")
             .Replace("\\t", "\t")
             .Replace("\\\\", "\\")
             .Trim();

    private static string InferEntityType(int textId) =>
        textId switch
        {
            >= 3000 and < 5000 => "MonsterOrNpc",
            >= 10000 and < 20000 => "ItemOrSpell",
            _ => "Unknown",
        };
}
