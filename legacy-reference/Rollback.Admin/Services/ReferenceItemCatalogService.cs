using System.Text;
using Rollback.Admin.Models.Items;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class ReferenceItemCatalogService
{
    private readonly Lazy<ReferenceItemCatalogState> _state;
    private readonly ClientI18nTextService _i18nTextService = new();

    public ReferenceItemCatalogService() =>
        _state = new Lazy<ReferenceItemCatalogState>(LoadState, LazyThreadSafetyMode.ExecutionAndPublication);

    public bool IsAvailable => _state.Value.ItemsById.Count > 0;

    public string SourceDescription => _state.Value.SourceDescription;

    public IReadOnlyDictionary<short, ReferenceItemIdentity> GetAllItems() =>
        _state.Value.ItemsById;

    public ReferenceItemIdentity? GetItem(short itemId) =>
        _state.Value.ItemsById.TryGetValue(itemId, out var item)
            ? item
            : null;

    public ReferenceItemSetIdentity? GetSet(short setId) =>
        _state.Value.SetsById.TryGetValue(setId, out var set)
            ? set
            : null;

    public string GetTypeLabel(short typeId)
    {
        if (_state.Value.TypeLabels.TryGetValue(typeId, out var label) &&
            !string.IsNullOrWhiteSpace(label))
        {
            return label;
        }

        if (Enum.IsDefined(typeof(ItemType), (int)typeId))
            return ItemTypeLabelService.GetDisplayName((ItemType)typeId);

        return $"Tipo {typeId}";
    }

    private ReferenceItemCatalogState LoadState()
    {
        var referenceDirectory = FindReferenceDirectory();
        if (string.IsNullOrWhiteSpace(referenceDirectory))
        {
            return new ReferenceItemCatalogState(
                new Dictionary<short, ReferenceItemIdentity>(),
                new Dictionary<short, ReferenceItemSetIdentity>(),
                new Dictionary<short, string>(),
                "No se encontraron exports de referencia de items en Documents");
        }

        var templatesPath = Path.Combine(referenceDirectory, "items_templates_reference.sql");
        var setsPath = Path.Combine(referenceDirectory, "items_sets_reference.sql");
        var typesPath = Path.Combine(referenceDirectory, "items_types_reference.sql");
        if (!File.Exists(templatesPath) || !File.Exists(setsPath) || !File.Exists(typesPath))
        {
            return new ReferenceItemCatalogState(
                new Dictionary<short, ReferenceItemIdentity>(),
                new Dictionary<short, ReferenceItemSetIdentity>(),
                new Dictionary<short, string>(),
                $"La referencia de items esta incompleta en {referenceDirectory}");
        }

        var typeLabels = LoadTypeLabels(typesPath);
        var sets = LoadSets(setsPath);
        var items = LoadItems(templatesPath, typeLabels);

        return new ReferenceItemCatalogState(
            items,
            sets,
            typeLabels,
            $"Referencia sana de items cargada desde {referenceDirectory}");
    }

    private Dictionary<short, ReferenceItemIdentity> LoadItems(
        string templatesPath,
        IReadOnlyDictionary<short, string> typeLabels)
    {
        var result = new Dictionary<short, ReferenceItemIdentity>();
        foreach (var values in EnumerateInsertValues(templatesPath, "items_templates"))
        {
            if (values.Count < 26)
                continue;

            var itemId = ToInt16(values[0]);
            if (itemId <= 0)
                continue;

            var nameId = ToInt32(values[3]);
            var typeId = ToInt16(values[4]);
            var descriptionId = ToInt32(values[5]);
            var iconId = ToInt32(values[6]);
            var level = ToInt16(values[7]);
            var itemSetId = ToInt16(values[15]);
            var appearanceId = ToInt32(values[18]);

            var resolvedName = TryResolveReferenceName(nameId);
            var resolvedDescription = !string.IsNullOrWhiteSpace(resolvedName)
                ? TryResolveReferenceDescription(descriptionId)
                : string.Empty;

            result[itemId] = new ReferenceItemIdentity
            {
                ItemId = itemId,
                NameId = nameId,
                DescriptionId = descriptionId,
                TypeId = typeId,
                TypeLabel = typeLabels.TryGetValue(typeId, out var typeLabel)
                    ? typeLabel
                    : GetTypeLabelFallback(typeId),
                IconId = iconId,
                Level = level,
                ItemSetId = itemSetId,
                AppearanceId = appearanceId,
                Name = resolvedName,
                Description = resolvedDescription,
            };
        }

        return result;
    }

    private Dictionary<short, ReferenceItemSetIdentity> LoadSets(string setsPath)
    {
        var result = new Dictionary<short, ReferenceItemSetIdentity>();
        foreach (var values in EnumerateInsertValues(setsPath, "items_sets"))
        {
            if (values.Count < 5)
                continue;

            var setId = ToInt16(values[0]);
            if (setId <= 0)
                continue;

            var itemsCsv = NormalizeCsv(values[1]);
            var nameId = ToInt32(values[2]);
            result[setId] = new ReferenceItemSetIdentity
            {
                SetId = setId,
                NameId = nameId,
                Name = TryResolveReferenceName(nameId),
                ItemsCsv = itemsCsv,
                ItemIds = ParseCsvShorts(itemsCsv),
            };
        }

        return result;
    }

    private Dictionary<short, string> LoadTypeLabels(string typesPath)
    {
        var result = new Dictionary<short, string>();
        foreach (var values in EnumerateInsertValues(typesPath, "items_types"))
        {
            if (values.Count < 2)
                continue;

            var typeId = ToInt16(values[0]);
            if (typeId <= 0)
                continue;

            var nameId = ToInt32(values[1]);
            var label = TryResolveReferenceName(nameId);
            if (string.IsNullOrWhiteSpace(label))
                label = GetTypeLabelFallback(typeId);

            result[typeId] = label;
        }

        return result;
    }

    private string TryResolveReferenceName(int nameId)
    {
        if (nameId <= 0 || !_i18nTextService.TryGetText(nameId, out var text))
            return string.Empty;

        var normalized = text.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        if (normalized.StartsWith('#') || LooksLikeDescription(normalized))
            return string.Empty;

        return normalized;
    }

    private string TryResolveReferenceDescription(int descriptionId)
    {
        if (descriptionId <= 0 || !_i18nTextService.TryGetText(descriptionId, out var text))
            return string.Empty;

        var normalized = text.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        if (normalized.StartsWith('#') || LooksLikeTitle(normalized))
            return string.Empty;

        return normalized;
    }

    private static bool LooksLikeTitle(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (text.Contains('\n') || text.Contains('\r'))
            return false;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length <= 8 && text.Length <= 80 && !text.Contains('.') && !text.Contains(';') && !text.Contains(':');
    }

    private static bool LooksLikeDescription(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (text.Contains('\n') || text.Contains('\r'))
            return true;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length >= 10 || text.Length >= 90 || text.Contains('.') || text.Contains(';') || text.Contains('!') || text.Contains('?');
    }

    private static string GetTypeLabelFallback(short typeId)
    {
        if (Enum.IsDefined(typeof(ItemType), (int)typeId))
            return ItemTypeLabelService.GetDisplayName((ItemType)typeId);

        return $"Tipo {typeId}";
    }

    private static string? FindReferenceDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var directDocuments = Path.Combine(current.FullName, "Documents");
            if (HasReferenceFiles(directDocuments))
                return directDocuments;

            current = current.Parent;
        }

        var myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (HasReferenceFiles(myDocuments))
            return myDocuments;

        return null;
    }

    private static bool HasReferenceFiles(string? directory) =>
        !string.IsNullOrWhiteSpace(directory) &&
        File.Exists(Path.Combine(directory, "items_templates_reference.sql")) &&
        File.Exists(Path.Combine(directory, "items_sets_reference.sql")) &&
        File.Exists(Path.Combine(directory, "items_types_reference.sql"));

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
                    inString = false;
                    continue;
                }

                buffer.Append(current);
                continue;
            }

            switch (current)
            {
                case '\'':
                    inString = true;
                    break;

                case ',':
                    values.Add(buffer.ToString().Trim());
                    buffer.Clear();
                    break;

                default:
                    buffer.Append(current);
                    break;
            }
        }

        values.Add(buffer.ToString().Trim());
        return values;
    }

    private static short ToInt16(string value)
    {
        if (short.TryParse(value, out var parsed))
            return parsed;

        if (int.TryParse(value, out var intValue) && intValue is >= short.MinValue and <= short.MaxValue)
            return (short)intValue;

        return 0;
    }

    private static int ToInt32(string value) =>
        int.TryParse(value, out var parsed)
            ? parsed
            : 0;

    private static string NormalizeCsv(string? csv) =>
        string.Join(
            ",",
            (csv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value)));

    private static IReadOnlyList<short> ParseCsvShorts(string csv) =>
        NormalizeCsv(csv)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => ToInt16(value))
            .Where(value => value > 0)
            .ToArray();

    private sealed class ReferenceItemCatalogState
    {
        public ReferenceItemCatalogState(
            IReadOnlyDictionary<short, ReferenceItemIdentity> itemsById,
            IReadOnlyDictionary<short, ReferenceItemSetIdentity> setsById,
            IReadOnlyDictionary<short, string> typeLabels,
            string sourceDescription)
        {
            ItemsById = itemsById;
            SetsById = setsById;
            TypeLabels = typeLabels;
            SourceDescription = sourceDescription;
        }

        public IReadOnlyDictionary<short, ReferenceItemIdentity> ItemsById { get; }

        public IReadOnlyDictionary<short, ReferenceItemSetIdentity> SetsById { get; }

        public IReadOnlyDictionary<short, string> TypeLabels { get; }

        public string SourceDescription { get; }
    }
}
