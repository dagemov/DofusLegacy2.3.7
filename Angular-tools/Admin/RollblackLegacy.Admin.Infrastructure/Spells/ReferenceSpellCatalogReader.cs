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
        var levelsById = LoadReferenceLevels(levelsPath, out var levelsBySpellId);
        var spellsById = LoadSpellTemplates(templatesPath, texts, typeLabels, levelsById, levelsBySpellId);
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
        IReadOnlyDictionary<int, ReferenceSpellLevelEntry> levelsById,
        IReadOnlyDictionary<short, IReadOnlyList<ReferenceSpellLevelEntry>> levelsBySpellId)
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
            var orderedLevelIds = ParseCsvInts(levelsCsv);
            var spellLevels = orderedLevelIds
                .Where(levelId => levelsById.ContainsKey(levelId))
                .Select(levelId => levelsById[levelId])
                .ToArray();

            if (spellLevels.Length == 0 &&
                levelsBySpellId.TryGetValue(spellId, out var groupedLevels))
            {
                spellLevels = groupedLevels.ToArray();
            }

            var breedIds = spellLevels
                .Select(level => level.SpellBreed)
                .Where(value => value > 0)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            var levelsLookup = spellLevels.ToDictionary(level => level.LevelId);

            result[spellId] = new ReferenceSpellCatalogEntry(
                spellId,
                texts.TryGetValue(nameId, out var name) ? name : null,
                texts.TryGetValue(descriptionId, out var description) ? description : null,
                nameId > 0 ? nameId : null,
                descriptionId > 0 ? descriptionId : null,
                typeId,
                typeLabels.TryGetValue(typeId, out var typeLabel) ? typeLabel : null,
                iconId > 0 ? iconId : null,
                breedIds,
                CountCsv(levelsCsv),
                breedIds.Any(value => value is >= 1 and <= 12),
                NormalizeCsv(levelsCsv),
                orderedLevelIds,
                levelsLookup);
        }

        return result;
    }

    private static IReadOnlyDictionary<int, ReferenceSpellLevelEntry> LoadReferenceLevels(
        string levelsPath,
        out IReadOnlyDictionary<short, IReadOnlyList<ReferenceSpellLevelEntry>> levelsBySpellId)
    {
        var result = new Dictionary<int, ReferenceSpellLevelEntry>();
        var grouped = new Dictionary<short, List<ReferenceSpellLevelEntry>>();
        foreach (var values in EnumerateInsertValues(levelsPath, "spells_levels"))
        {
            if (values.Count < 29)
            {
                continue;
            }

            var levelId = ToInt32(values[0]);
            var spellId = ToInt16(values[1]);
            if (levelId <= 0 || spellId <= 0)
            {
                continue;
            }

            var level = new ReferenceSpellLevelEntry(
                levelId,
                spellId,
                ToInt32(values[2]),
                ToInt32(values[3]),
                ToInt32(values[25]),
                ToInt32(values[4]),
                ToBoolean(values[5]),
                ToBoolean(values[6]),
                ToBoolean(values[7]),
                ToBoolean(values[11]),
                ToBoolean(values[13]),
                ToBoolean(values[14]),
                ToBoolean(values[22]),
                ToInt32(values[8]),
                ToInt32(values[10]),
                ToInt32(values[16]),
                ToInt32(values[17]),
                ToInt32(values[18]),
                ToInt32(values[19]),
                ToInt32(values[21]),
                NormalizeCsv(values[9]),
                NormalizeCsv(values[26]),
                HasSerializedPayload(values[27]),
                HasSerializedPayload(values[28]));

            result[levelId] = level;
            if (!grouped.TryGetValue(spellId, out var spellLevels))
            {
                spellLevels = new List<ReferenceSpellLevelEntry>();
                grouped[spellId] = spellLevels;
            }

            spellLevels.Add(level);
        }

        levelsBySpellId = grouped.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<ReferenceSpellLevelEntry>)pair.Value.ToArray());
        return result;
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

    private static string NormalizeCsv(string? csv) =>
        string.Join(
            ",",
            (csv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value)));

    private static int[] ParseCsvInts(string? csv) =>
        NormalizeCsv(csv)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var parsed) ? parsed : 0)
            .Where(value => value > 0)
            .ToArray();

    private static bool HasSerializedPayload(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        if (string.Equals(normalized, "null", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[2..];
        }

        return normalized.Any(character => character != '0');
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

    private static bool ToBoolean(string value) =>
        string.Equals(value, "1", StringComparison.Ordinal) ||
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

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
        int? NameId,
        int? DescriptionId,
        int TypeId,
        string? TypeLabel,
        int? IconId,
        IReadOnlyList<int> BreedIds,
        int LevelCount,
        bool HasClassicBreedLevels,
        string SpellLevelsIdsCsv,
        IReadOnlyList<int> OrderedLevelIds,
        IReadOnlyDictionary<int, ReferenceSpellLevelEntry> LevelsById);

    internal sealed record ReferenceSpellLevelEntry(
        int LevelId,
        short SpellId,
        int SpellBreed,
        int ApCost,
        int MinRange,
        int MaxRange,
        bool CastInLine,
        bool CastInDiagonal,
        bool CastTestLos,
        bool NeedFreeCell,
        bool NeedTakenCell,
        bool RangeCanBeBoosted,
        bool CriticalFailureEndsTurn,
        int CriticalHitProbability,
        int CriticalFailureProbability,
        int MaxCastPerTurn,
        int MaxCastPerTarget,
        int MinCastInterval,
        int InitialCooldown,
        int MinPlayerLevel,
        string StatesRequiredCsv,
        string StatesForbiddenCsv,
        bool HasEffects,
        bool HasCriticalEffects);
}
