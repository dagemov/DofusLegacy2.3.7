using System.Text;
using System.Text.Json;

namespace RollblackLegacy.Admin.Infrastructure.Spells;

public sealed class ReferenceSpellCatalogReader
{
    private readonly Lazy<ReferenceSpellCatalogSnapshot> _snapshot;

    public ReferenceSpellCatalogReader()
    {
        _snapshot = new Lazy<ReferenceSpellCatalogSnapshot>(
            LoadSnapshot,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    internal ReferenceSpellCatalogSnapshot GetSnapshot() => _snapshot.Value;

    private static ReferenceSpellCatalogSnapshot LoadSnapshot()
    {
        var referenceDirectory = FindReferenceDirectory();
        if (string.IsNullOrWhiteSpace(referenceDirectory))
        {
            return ReferenceSpellCatalogSnapshot.Empty("No se encontro spellsReferences en Documents.");
        }

        var templatesPath = Path.Combine(referenceDirectory, "spells_templates.sql");
        var levelsPath = Path.Combine(referenceDirectory, "spells_levels.sql");
        var typesPath = Path.Combine(referenceDirectory, "spells_types.sql");
        var i18nPath = Path.Combine(referenceDirectory, "i18n_es.json");
        if (!File.Exists(templatesPath) ||
            !File.Exists(levelsPath) ||
            !File.Exists(typesPath) ||
            !File.Exists(i18nPath))
        {
            return ReferenceSpellCatalogSnapshot.Empty($"spellsReferences incompleto en {referenceDirectory}.");
        }

        var texts = LoadI18nTexts(i18nPath);
        var typeLabels = LoadTypeLabels(typesPath, texts);
        var levelInfoBySpellId = LoadLevelInfo(levelsPath);
        var spellsById = LoadSpellTemplates(templatesPath, texts, typeLabels, levelInfoBySpellId);
        var classicSpellIds = spellsById.Values
            .Where(spell => spell.HasClassicBreedLevels)
            .Select(spell => spell.SpellId)
            .OrderBy(id => id)
            .ToArray();

        return new ReferenceSpellCatalogSnapshot(
            spellsById,
            typeLabels,
            classicSpellIds,
            $"Referencia spell cargada desde {referenceDirectory}.");
    }

    private static IReadOnlyDictionary<short, ReferenceSpellCatalogEntry> LoadSpellTemplates(
        string templatesPath,
        IReadOnlyDictionary<int, string> texts,
        IReadOnlyDictionary<int, string> typeLabels,
        IReadOnlyDictionary<short, ReferenceSpellLevelInfo> levelInfoBySpellId)
    {
        var result = new Dictionary<short, ReferenceSpellCatalogEntry>();
        foreach (var values in EnumerateInsertValues(templatesPath, "spells_templates"))
        {
            if (values.Count < 10)
            {
                continue;
            }

            var spellId = ToInt16(values[0]);
            if (spellId <= 0)
            {
                continue;
            }

            var nameId = ToInt32(values[1]);
            var descriptionId = ToInt32(values[2]);
            var typeId = ToInt32(values[3]);
            var iconId = ToInt32(values[8]);
            var levelsCsv = values[9];
            levelInfoBySpellId.TryGetValue(spellId, out var levelInfo);

            result[spellId] = new ReferenceSpellCatalogEntry(
                spellId,
                texts.TryGetValue(nameId, out var name) ? name : null,
                texts.TryGetValue(descriptionId, out var description) ? description : null,
                typeId,
                typeLabels.TryGetValue(typeId, out var typeLabel) ? typeLabel : null,
                iconId > 0 ? iconId : null,
                levelInfo?.BreedIds ?? Array.Empty<int>(),
                CountCsv(levelsCsv),
                levelInfo?.HasClassicBreedLevels ?? false);
        }

        return result;
    }

    private static IReadOnlyDictionary<short, ReferenceSpellLevelInfo> LoadLevelInfo(string levelsPath)
    {
        var breedsBySpellId = new Dictionary<short, HashSet<int>>();
        foreach (var values in EnumerateInsertValues(levelsPath, "spells_levels"))
        {
            if (values.Count < 3)
            {
                continue;
            }

            var spellId = ToInt16(values[1]);
            var spellBreed = ToInt32(values[2]);
            if (spellId <= 0)
            {
                continue;
            }

            if (!breedsBySpellId.TryGetValue(spellId, out var breeds))
            {
                breeds = new HashSet<int>();
                breedsBySpellId[spellId] = breeds;
            }

            if (spellBreed > 0)
            {
                breeds.Add(spellBreed);
            }
        }

        return breedsBySpellId.ToDictionary(
            pair => pair.Key,
            pair => new ReferenceSpellLevelInfo(
                pair.Value.OrderBy(value => value).ToArray(),
                pair.Value.Any(value => value is >= 1 and <= 12)));
    }

    private static IReadOnlyDictionary<int, string> LoadTypeLabels(
        string typesPath,
        IReadOnlyDictionary<int, string> texts)
    {
        var result = new Dictionary<int, string>();
        foreach (var values in EnumerateInsertValues(typesPath, "spells_types"))
        {
            if (values.Count < 3)
            {
                continue;
            }

            var typeId = ToInt32(values[0]);
            var longNameId = ToInt32(values[1]);
            var shortNameId = ToInt32(values[2]);
            var label = texts.TryGetValue(longNameId, out var longName) && !string.IsNullOrWhiteSpace(longName)
                ? longName
                : texts.TryGetValue(shortNameId, out var shortName) && !string.IsNullOrWhiteSpace(shortName)
                    ? shortName
                    : null;
            if (!string.IsNullOrWhiteSpace(label))
            {
                result[typeId] = label.Trim();
            }
        }

        return result;
    }

    private static IReadOnlyDictionary<int, string> LoadI18nTexts(string i18nPath)
    {
        var result = new Dictionary<int, string>();
        var bytes = File.ReadAllBytes(i18nPath);
        var reader = new Utf8JsonReader(bytes, new JsonReaderOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });

        var insideIndexes = false;
        while (reader.Read())
        {
            if (!insideIndexes)
            {
                if (reader.TokenType == JsonTokenType.PropertyName &&
                    string.Equals(reader.GetString(), "Indexes", StringComparison.Ordinal))
                {
                    if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                    {
                        break;
                    }

                    insideIndexes = true;
                }

                continue;
            }

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var key = reader.GetString();
            if (!reader.Read() || !int.TryParse(key, out var id))
            {
                continue;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                result[id] = (reader.GetString() ?? string.Empty).Trim();
            }
        }

        return result;
    }

    private static IEnumerable<IReadOnlyList<string>> EnumerateInsertValues(string filePath, string tableName)
    {
        var prefix = $"INSERT INTO `{tableName}` VALUES ";
        foreach (var line in File.ReadLines(filePath, Encoding.UTF8))
        {
            if (!line.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var payload = line[prefix.Length..].Trim();
            if (payload.Length < 4 || !payload.StartsWith('(') || !payload.EndsWith(");", StringComparison.Ordinal))
            {
                continue;
            }

            yield return SplitSqlTuple(payload[1..^2]);
        }
    }

    private static List<string> SplitSqlTuple(string tuple)
    {
        var values = new List<string>();
        var buffer = new StringBuilder();
        var inString = false;

        for (var index = 0; index < tuple.Length; index++)
        {
            var current = tuple[index];
            if (inString)
            {
                if (current == '\\' && index + 1 < tuple.Length)
                {
                    index++;
                    buffer.Append(tuple[index] switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        _ => tuple[index],
                    });
                    continue;
                }

                if (current == '\'')
                {
                    if (index + 1 < tuple.Length && tuple[index + 1] == '\'')
                    {
                        buffer.Append('\'');
                        index++;
                        continue;
                    }

                    inString = false;
                    continue;
                }

                buffer.Append(current);
                continue;
            }

            if (current == '\'')
            {
                inString = true;
                continue;
            }

            if (current == ',')
            {
                values.Add(buffer.ToString().Trim());
                buffer.Clear();
                continue;
            }

            buffer.Append(current);
        }

        values.Add(buffer.ToString().Trim());
        return values;
    }

    private static string? FindReferenceDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "spellsReferences"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "OneDrive",
                "Documents",
                "spellsReferences"),
        };

        return candidates.FirstOrDefault(Directory.Exists);
    }

    private static int CountCsv(string? csv)
    {
        return (csv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Count(value => !string.IsNullOrWhiteSpace(value));
    }

    private static short ToInt16(string value)
    {
        if (short.TryParse(value, out var parsed))
        {
            return parsed;
        }

        if (int.TryParse(value, out var intValue) &&
            intValue is >= short.MinValue and <= short.MaxValue)
        {
            return (short)intValue;
        }

        return 0;
    }

    private static int ToInt32(string value)
    {
        return int.TryParse(value, out var parsed)
            ? parsed
            : 0;
    }

    internal sealed record ReferenceSpellCatalogSnapshot(
        IReadOnlyDictionary<short, ReferenceSpellCatalogEntry> SpellsById,
        IReadOnlyDictionary<int, string> TypeLabels,
        IReadOnlyCollection<short> ClassicSpellIds,
        string SourceDescription)
    {
        public bool IsAvailable => SpellsById.Count > 0;

        public static ReferenceSpellCatalogSnapshot Empty(string sourceDescription) =>
            new(
                new Dictionary<short, ReferenceSpellCatalogEntry>(),
                new Dictionary<int, string>(),
                Array.Empty<short>(),
                sourceDescription);
    }

    internal sealed record ReferenceSpellCatalogEntry(
        short SpellId,
        string? Name,
        string? Description,
        int TypeId,
        string? TypeLabel,
        int? IconId,
        IReadOnlyList<int> BreedIds,
        int LevelCount,
        bool HasClassicBreedLevels);

    private sealed record ReferenceSpellLevelInfo(
        IReadOnlyList<int> BreedIds,
        bool HasClassicBreedLevels);
}
