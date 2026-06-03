using System.Text;
using System.Text.Json;
using Rollback.Admin.Models.Spells;

namespace Rollback.Admin.Services;

internal sealed class ClientSpellTypeCatalogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private static readonly Lazy<ClientSpellTypeState> State = new(
        LoadState,
        LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly ClientI18nTextService _i18nTextService = new();

    public IReadOnlyList<SpellTypeOption> GetOptions() =>
        State.Value.EntriesByTypeId.Values
            .OrderBy(entry => ResolveDisplayName(entry))
            .ThenBy(entry => entry.TypeId)
            .Select(entry => new SpellTypeOption(entry.TypeId, ResolveDisplayName(entry)))
            .ToArray();

    public string GetDisplayName(short typeId)
    {
        if (!State.Value.EntriesByTypeId.TryGetValue((sbyte)typeId, out var entry))
            return $"Tipo {typeId}";

        return ResolveDisplayName(entry);
    }

    private string ResolveDisplayName(ClientMapEntry entry)
    {
        if (entry.LongNameId.HasValue && _i18nTextService.TryGetText(entry.LongNameId.Value, out var longName))
            return Normalize(longName);

        if (entry.ShortNameId.HasValue && _i18nTextService.TryGetText(entry.ShortNameId.Value, out var shortName))
            return Normalize(shortName);

        return $"Tipo {entry.TypeId}";
    }

    private static ClientSpellTypeState LoadState()
    {
        foreach (var candidate in FindJsonCandidates())
        {
            var entries = TryLoadFromJson(candidate);
            if (entries.Count > 0)
            {
                return new ClientSpellTypeState(
                    entries,
                    $"Mapa local encontrado en {Path.GetFileName(candidate)}");
            }
        }

        var extractedEntries = ExtractFromSwf(out var extractedSource);
        if (extractedEntries.Count > 0)
        {
            TryPersistGeneratedMap(extractedEntries);
            return new ClientSpellTypeState(extractedEntries, extractedSource);
        }

        return new ClientSpellTypeState(new Dictionary<sbyte, ClientMapEntry>(), "Sin tipos cliente extraidos");
    }

    private static Dictionary<sbyte, ClientMapEntry> TryLoadFromJson(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return new Dictionary<sbyte, ClientMapEntry>();

            var json = File.ReadAllText(filePath, Encoding.UTF8);
            var items = JsonSerializer.Deserialize<List<ClientMapEntry>>(json) ?? new List<ClientMapEntry>();

            return items
                .GroupBy(x => x.TypeId)
                .ToDictionary(group => group.Key, group => group.Last());
        }
        catch
        {
            return new Dictionary<sbyte, ClientMapEntry>();
        }
    }

    private static Dictionary<sbyte, ClientMapEntry> ExtractFromSwf(out string sourceDescription)
    {
        var commonDirectory = ClientSwfAbcSupport.FindCommonDirectory("SpellTypes0.swf");
        if (commonDirectory is null)
        {
            sourceDescription = "No se encontro client/app/data/common para extraer SpellTypes*.swf";
            return new Dictionary<sbyte, ClientMapEntry>();
        }

        var results = new Dictionary<sbyte, ClientMapEntry>();
        foreach (var swfPath in Directory.EnumerateFiles(commonDirectory, "SpellTypes*.swf", SearchOption.TopDirectoryOnly))
        {
            try
            {
                foreach (var pair in ExtractFromSwfFile(swfPath))
                    results[pair.Key] = pair.Value;
            }
            catch
            {
                // Optional client metadata.
            }
        }

        sourceDescription = results.Count > 0
            ? "Extraido automaticamente desde common/SpellTypes*.swf"
            : "No se pudo reconstruir la metadata de tipos desde SpellTypes*.swf";

        return results;
    }

    private static Dictionary<sbyte, ClientMapEntry> ExtractFromSwfFile(string swfPath)
    {
        var body = ClientSwfAbcSupport.ReadSwfBody(File.ReadAllBytes(swfPath));
        var mappings = new Dictionary<sbyte, ClientMapEntry>();

        foreach (var abcPayload in ClientSwfAbcSupport.EnumerateDoAbcPayloads(body))
        {
            var abc = new ClientSwfAbcSupport.AbcFile(abcPayload);
            abc.Parse();

            var dataIndex = abc.FindMultinameIndex("_datas");
            var createPatterns = abc.FindMultinameIndices("create")
                .Select(index => (MultinameIndex: index, ArgCount: 3))
                .ToArray();
            if (dataIndex <= 0 || createPatterns.Length == 0)
                continue;

            foreach (var methodBody in abc.MethodBodies.OrderByDescending(x => x.Length))
            {
                foreach (var pair in ExtractMappingsFromBody(methodBody, abc, dataIndex, createPatterns))
                    mappings[pair.Key] = pair.Value;

                if (mappings.Count > 0)
                    break;
            }
        }

        return mappings;
    }

    private static IEnumerable<KeyValuePair<sbyte, ClientMapEntry>> ExtractMappingsFromBody(
        byte[] code,
        ClientSwfAbcSupport.AbcFile abc,
        int dataIndex,
        IReadOnlyCollection<(int MultinameIndex, int ArgCount)> createPatterns)
    {
        var offset = 0;
        while (offset < code.Length)
        {
            if (!TryParseTypeBlock(code, offset, abc, dataIndex, createPatterns, out var consumedBytes, out var entry))
            {
                offset++;
                continue;
            }

            yield return new KeyValuePair<sbyte, ClientMapEntry>(entry.TypeId, entry);
            offset = consumedBytes;
        }
    }

    private static bool TryParseTypeBlock(
        byte[] code,
        int startOffset,
        ClientSwfAbcSupport.AbcFile abc,
        int dataIndex,
        IReadOnlyCollection<(int MultinameIndex, int ArgCount)> createPatterns,
        out int consumedBytes,
        out ClientMapEntry entry)
    {
        consumedBytes = startOffset;
        entry = new ClientMapEntry();

        var offset = startOffset;
        if (!ClientSwfAbcSupport.TryReadInstructionByte(code, ref offset, out var opcode) || opcode != 0x66)
            return false;

        if (!ClientSwfAbcSupport.TryReadU30(code, ref offset, out var propertyIndex) || propertyIndex != dataIndex)
            return false;

        if (!ClientSwfAbcSupport.TryReadLiteral(code, ref offset, abc, out var mapKey))
            return false;

        if (!ClientSwfAbcSupport.TryReadInstructionByte(code, ref offset, out opcode) || opcode != 0x60)
            return false;

        if (!ClientSwfAbcSupport.TryReadU30(code, ref offset, out var lexIndex) || lexIndex <= 0)
            return false;

        var arguments = new List<object?>(3);
        while (arguments.Count < 3)
        {
            if (!ClientSwfAbcSupport.TryReadLiteral(code, ref offset, abc, out var argument))
                return false;

            arguments.Add(argument);
        }

        var matchedPatternLength = 0;
        var callOffset = -1;
        foreach (var createPattern in createPatterns)
        {
            var callPattern = ClientSwfAbcSupport.BuildCallPropertyPattern(createPattern.MultinameIndex, createPattern.ArgCount);
            callOffset = ClientSwfAbcSupport.IndexOf(code, callPattern, offset, Math.Min(code.Length, startOffset + 512));
            if (callOffset < 0)
                continue;

            matchedPatternLength = callPattern.Length;
            break;
        }

        if (callOffset < 0 || matchedPatternLength <= 0)
            return false;

        if (!ClientSwfAbcSupport.TryToShort(mapKey, out var mapTypeId))
            return false;

        if (!ClientSwfAbcSupport.TryToShort(arguments[0], out var typeIdFromArgs))
            return false;

        if (typeIdFromArgs != mapTypeId || typeIdFromArgs < sbyte.MinValue || typeIdFromArgs > sbyte.MaxValue)
            return false;

        entry.TypeId = (sbyte)typeIdFromArgs;
        entry.ShortNameId = ClientSwfAbcSupport.TryToInt(arguments.ElementAtOrDefault(1), out var shortNameId) ? shortNameId : null;
        entry.LongNameId = ClientSwfAbcSupport.TryToInt(arguments.ElementAtOrDefault(2), out var longNameId) ? longNameId : null;

        consumedBytes = callOffset + matchedPatternLength;
        return true;
    }

    private static string Normalize(string text) =>
        (text ?? string.Empty).Trim();

    private static IEnumerable<string> FindJsonCandidates()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            yield return Path.Combine(current.FullName, "client", "app", "data", "common", "spell-type-client-map.generated.json");
            yield return Path.Combine(current.FullName, "client", "app", "data", "common", "spell-type-client-map.json");
            yield return Path.Combine(current.FullName, "spell-type-client-map.generated.json");
            current = current.Parent;
        }
    }

    private static IEnumerable<string> FindWritableJsonCandidates()
    {
        var commonDirectory = ClientSwfAbcSupport.FindCommonDirectory("SpellTypes0.swf");
        if (!string.IsNullOrWhiteSpace(commonDirectory))
            yield return Path.Combine(commonDirectory, "spell-type-client-map.generated.json");

        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            yield return Path.Combine(current.FullName, "spell-type-client-map.generated.json");
            current = current.Parent;
        }
    }

    private static void TryPersistGeneratedMap(Dictionary<sbyte, ClientMapEntry> mappings)
    {
        var orderedEntries = mappings.Values
            .OrderBy(x => x.TypeId)
            .ToList();

        foreach (var candidate in FindWritableJsonCandidates())
        {
            try
            {
                var directory = Path.GetDirectoryName(candidate);
                if (string.IsNullOrWhiteSpace(directory))
                    continue;

                Directory.CreateDirectory(directory);
                var json = JsonSerializer.Serialize(orderedEntries, JsonOptions);
                File.WriteAllText(candidate, json, Encoding.UTF8);
                return;
            }
            catch
            {
                // Try the next writable location.
            }
        }
    }

    private sealed class ClientSpellTypeState
    {
        public ClientSpellTypeState(
            Dictionary<sbyte, ClientMapEntry> entriesByTypeId,
            string sourceDescription)
        {
            EntriesByTypeId = entriesByTypeId;
            SourceDescription = sourceDescription;
        }

        public Dictionary<sbyte, ClientMapEntry> EntriesByTypeId { get; }

        public string SourceDescription { get; }
    }

    private sealed class ClientMapEntry
    {
        public sbyte TypeId { get; set; }

        public int? ShortNameId { get; set; }

        public int? LongNameId { get; set; }
    }
}
