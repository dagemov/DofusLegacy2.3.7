using System.Text;
using System.Text.Json;
using Rollback.Admin.Models.Spells;

namespace Rollback.Admin.Services;

public sealed class ReferenceSpellCatalogService
{
    private readonly Lazy<ReferenceSpellCatalogState> _state;
    private readonly ClientSpellTypeCatalogService _clientTypeCatalogService = new();

    public ReferenceSpellCatalogService() =>
        _state = new Lazy<ReferenceSpellCatalogState>(LoadState, LazyThreadSafetyMode.ExecutionAndPublication);

    public bool IsAvailable => _state.Value.SpellsById.Count > 0;

    public string SourceDescription => _state.Value.SourceDescription;

    public IReadOnlyDictionary<short, ReferenceSpellIdentity> GetAll() =>
        _state.Value.SpellsById;

    public ReferenceSpellIdentity? Get(short spellId) =>
        _state.Value.SpellsById.TryGetValue(spellId, out var entry)
            ? entry
            : null;

    public IReadOnlyCollection<short> GetClassicSpellIds() =>
        _state.Value.ClassicSpellIds;

    public IReadOnlyCollection<short> GetModernSpellIds() =>
        _state.Value.ModernSpellIds;

    public IReadOnlyCollection<short> GetSupportSpellIds() =>
        _state.Value.SupportSpellIds;

    public IReadOnlyList<SpellTypeOption> GetTypeOptions() =>
        _state.Value.TypeOptions;

    public string GetTypeLabel(sbyte typeId)
    {
        if (_state.Value.TypeLabels.TryGetValue(typeId, out var label) &&
            !string.IsNullOrWhiteSpace(label))
        {
            return label;
        }

        var fallback = _clientTypeCatalogService.GetDisplayName(typeId);
        return string.IsNullOrWhiteSpace(fallback)
            ? $"Tipo {typeId}"
            : fallback;
    }

    private ReferenceSpellCatalogState LoadState()
    {
        var referenceDirectory = FindReferenceDirectory();
        if (string.IsNullOrWhiteSpace(referenceDirectory))
        {
            return new ReferenceSpellCatalogState(
                new Dictionary<short, ReferenceSpellIdentity>(),
                new Dictionary<sbyte, string>(),
                Array.Empty<SpellTypeOption>(),
                Array.Empty<short>(),
                Array.Empty<short>(),
                Array.Empty<short>(),
                "No se encontro spellsReferences en Documents");
        }

        var templatesPath = Path.Combine(referenceDirectory, "spells_templates.sql");
        var levelsPath = Path.Combine(referenceDirectory, "spells_levels.sql");
        var typesPath = Path.Combine(referenceDirectory, "spells_types.sql");
        var i18nPath = Path.Combine(referenceDirectory, "i18n_es.json");
        if (!File.Exists(templatesPath) || !File.Exists(levelsPath) || !File.Exists(typesPath) || !File.Exists(i18nPath))
        {
            return new ReferenceSpellCatalogState(
                new Dictionary<short, ReferenceSpellIdentity>(),
                new Dictionary<sbyte, string>(),
                Array.Empty<SpellTypeOption>(),
                Array.Empty<short>(),
                Array.Empty<short>(),
                Array.Empty<short>(),
                $"spellsReferences incompleto en {referenceDirectory}");
        }

        var texts = LoadI18nTexts(i18nPath);
        var typeLabels = LoadTypeLabels(typesPath, texts);
        var levelsById = LoadReferenceLevels(levelsPath, out var levelsBySpellId);
        var spellsById = LoadReferenceTemplates(templatesPath, texts, typeLabels, levelsById, levelsBySpellId);

        var classicIds = spellsById.Values
            .Where(spell => spell.HasClassicBreedLevels)
            .Select(spell => spell.SpellId)
            .OrderBy(id => id)
            .ToArray();

        var modernIds = spellsById.Values
            .Where(spell => !spell.HasClassicBreedLevels && spell.HasModernBreedLevels)
            .Select(spell => spell.SpellId)
            .OrderBy(id => id)
            .ToArray();

        var supportIds = spellsById.Values
            .Where(spell => !spell.HasClassicBreedLevels && !spell.HasModernBreedLevels && spell.HasSupportLevels)
            .Select(spell => spell.SpellId)
            .OrderBy(id => id)
            .ToArray();

        var typeOptions = typeLabels
            .OrderBy(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Select(pair => new SpellTypeOption(pair.Key, pair.Value))
            .ToArray();

        return new ReferenceSpellCatalogState(
            spellsById,
            typeLabels,
            typeOptions,
            classicIds,
            modernIds,
            supportIds,
            $"Referencia sana cargada desde {referenceDirectory}");
    }

    private Dictionary<short, ReferenceSpellIdentity> LoadReferenceTemplates(
        string templatesPath,
        IReadOnlyDictionary<int, string> texts,
        IReadOnlyDictionary<sbyte, string> typeLabels,
        IReadOnlyDictionary<int, ReferenceSpellLevelSummary> levelsById,
        IReadOnlyDictionary<short, List<ReferenceSpellLevelSummary>> levelsBySpellId)
    {
        var result = new Dictionary<short, ReferenceSpellIdentity>();

        foreach (var values in EnumerateInsertValues(templatesPath, "spells_templates"))
        {
            if (values.Count < 11)
                continue;

            var spellId = ToInt16(values[0]);
            if (spellId <= 0)
                continue;

            var nameId = ToInt32(values[1]);
            var descriptionId = ToInt32(values[2]);
            var typeId = ToSByte(values[3]);
            var scriptParams = values[4];
            var scriptId = ToInt32(values[6]);
            var iconId = ToInt32(values[8]);
            var levelsCsv = values[9];
            var orderedLevelIds = ParseCsvInts(levelsCsv);

            var spellLevels = orderedLevelIds
                .Where(levelId => levelsById.ContainsKey(levelId))
                .Select(levelId => levelsById[levelId])
                .ToArray();

            var spellBreeds = spellLevels
                .Select(level => level.SpellBreed)
                .Distinct()
                .OrderBy(breed => breed)
                .ToArray();

            if (spellBreeds.Length == 0 &&
                levelsBySpellId.TryGetValue(spellId, out var groupedLevels))
            {
                spellBreeds = groupedLevels
                    .Select(level => level.SpellBreed)
                    .Distinct()
                    .OrderBy(breed => breed)
                    .ToArray();
            }

            var levelsLookup = spellLevels.ToDictionary(level => level.Id);
            result[spellId] = new ReferenceSpellIdentity
            {
                SpellId = spellId,
                NameId = nameId,
                DescriptionId = descriptionId,
                TypeId = typeId,
                TypeLabel = typeLabels.TryGetValue(typeId, out var typeLabel)
                    ? typeLabel
                    : _clientTypeCatalogService.GetDisplayName(typeId),
                Name = texts.TryGetValue(nameId, out var spellName) ? spellName : string.Empty,
                Description = texts.TryGetValue(descriptionId, out var spellDescription) ? spellDescription : string.Empty,
                ScriptParams = scriptParams,
                ScriptId = scriptId,
                IconId = iconId,
                SpellLevelsIdsCsv = levelsCsv,
                OrderedLevelIds = orderedLevelIds,
                LevelsById = levelsLookup,
                SpellBreeds = spellBreeds,
            };
        }

        return result;
    }

    private static Dictionary<int, ReferenceSpellLevelSummary> LoadReferenceLevels(
        string levelsPath,
        out Dictionary<short, List<ReferenceSpellLevelSummary>> levelsBySpellId)
    {
        var result = new Dictionary<int, ReferenceSpellLevelSummary>();
        levelsBySpellId = new Dictionary<short, List<ReferenceSpellLevelSummary>>();

        foreach (var values in EnumerateInsertValues(levelsPath, "spells_levels"))
        {
            if (values.Count < 29)
                continue;

            var summary = new ReferenceSpellLevelSummary
            {
                Id = ToInt32(values[0]),
                SpellId = ToInt16(values[1]),
                SpellBreed = ToInt32(values[2]),
                APCost = ToInt32(values[3]),
                MaxRange = ToInt32(values[4]),
                CastInLine = ToBoolean(values[5]),
                CastInDiagonal = ToBoolean(values[6]),
                CastTestLos = ToBoolean(values[7]),
                CriticalHitProbability = ToInt32(values[8]),
                StatesRequiredCsv = NormalizeCsv(values[9]),
                CriticalFailureProbability = ToInt32(values[10]),
                NeedFreeCell = ToBoolean(values[11]),
                NeedFreeTrapCell = ToBoolean(values[12]),
                NeedTakenCell = ToBoolean(values[13]),
                RangeCanBeBoosted = ToBoolean(values[14]),
                MaxStack = ToInt32(values[15]),
                MaxCastPerTurn = ToInt32(values[16]),
                MaxCastPerTarget = ToInt32(values[17]),
                MinCastInterval = ToInt32(values[18]),
                InitialCooldown = ToInt32(values[19]),
                GlobalCooldown = ToInt32(values[20]),
                MinPlayerLevel = ToInt32(values[21]),
                CriticalFailureEndsTurn = ToBoolean(values[22]),
                HideEffects = ToBoolean(values[23]),
                Hidden = ToBoolean(values[24]),
                MinRange = ToInt32(values[25]),
                StatesForbiddenCsv = NormalizeCsv(values[26]),
                HasEffects = HasHexPayload(values[27]),
                HasCriticalEffects = HasHexPayload(values[28]),
            };

            if (summary.Id <= 0 || summary.SpellId <= 0)
                continue;

            result[summary.Id] = summary;

            if (!levelsBySpellId.TryGetValue(summary.SpellId, out var spellLevels))
            {
                spellLevels = new List<ReferenceSpellLevelSummary>();
                levelsBySpellId[summary.SpellId] = spellLevels;
            }

            spellLevels.Add(summary);
        }

        return result;
    }

    private static Dictionary<sbyte, string> LoadTypeLabels(
        string typesPath,
        IReadOnlyDictionary<int, string> texts)
    {
        var result = new Dictionary<sbyte, string>();
        foreach (var values in EnumerateInsertValues(typesPath, "spells_types"))
        {
            if (values.Count < 3)
                continue;

            var typeId = ToSByte(values[0]);
            var longNameId = ToInt32(values[1]);
            var shortNameId = ToInt32(values[2]);

            var label = texts.TryGetValue(longNameId, out var longName) && !string.IsNullOrWhiteSpace(longName)
                ? longName
                : texts.TryGetValue(shortNameId, out var shortName) && !string.IsNullOrWhiteSpace(shortName)
                    ? shortName
                    : $"Tipo {typeId}";

            result[typeId] = label.Trim();
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
                        break;

                    insideIndexes = true;
                }

                continue;
            }

            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var key = reader.GetString();
            if (!reader.Read())
                break;

            if (!int.TryParse(key, out var id))
                continue;

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
                continue;

            var payload = line[prefix.Length..].Trim();
            if (payload.Length < 4 || !payload.StartsWith('(') || !payload.EndsWith(");", StringComparison.Ordinal))
                continue;

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

    private static short ToInt16(string value)
    {
        if (short.TryParse(value, out var parsed))
            return parsed;

        if (int.TryParse(value, out var asInt) &&
            asInt is >= short.MinValue and <= short.MaxValue)
        {
            return (short)asInt;
        }

        return 0;
    }

    private static int ToInt32(string value) =>
        int.TryParse(value, out var parsed)
            ? parsed
            : 0;

    private static sbyte ToSByte(string value)
    {
        var intValue = ToInt32(value);
        return intValue switch
        {
            > sbyte.MaxValue => sbyte.MaxValue,
            < sbyte.MinValue => sbyte.MinValue,
            _ => (sbyte)intValue,
        };
    }

    private static bool ToBoolean(string value) =>
        string.Equals(value, "1", StringComparison.Ordinal) ||
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeCsv(string? csv) =>
        string.Join(
            ",",
            (csv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value)));

    private static int[] ParseCsvInts(string csv) =>
        NormalizeCsv(csv)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var parsed) ? parsed : 0)
            .Where(value => value > 0)
            .ToArray();

    private static bool HasHexPayload(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
        value.Length > 2;

    private sealed class ReferenceSpellCatalogState
    {
        public ReferenceSpellCatalogState(
            IReadOnlyDictionary<short, ReferenceSpellIdentity> spellsById,
            IReadOnlyDictionary<sbyte, string> typeLabels,
            IReadOnlyList<SpellTypeOption> typeOptions,
            IReadOnlyCollection<short> classicSpellIds,
            IReadOnlyCollection<short> modernSpellIds,
            IReadOnlyCollection<short> supportSpellIds,
            string sourceDescription)
        {
            SpellsById = spellsById;
            TypeLabels = typeLabels;
            TypeOptions = typeOptions;
            ClassicSpellIds = classicSpellIds;
            ModernSpellIds = modernSpellIds;
            SupportSpellIds = supportSpellIds;
            SourceDescription = sourceDescription;
        }

        public IReadOnlyDictionary<short, ReferenceSpellIdentity> SpellsById { get; }

        public IReadOnlyDictionary<sbyte, string> TypeLabels { get; }

        public IReadOnlyList<SpellTypeOption> TypeOptions { get; }

        public IReadOnlyCollection<short> ClassicSpellIds { get; }

        public IReadOnlyCollection<short> ModernSpellIds { get; }

        public IReadOnlyCollection<short> SupportSpellIds { get; }

        public string SourceDescription { get; }
    }
}
