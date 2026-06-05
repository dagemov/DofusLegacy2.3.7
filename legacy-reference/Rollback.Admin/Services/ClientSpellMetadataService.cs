using System.Text;
using System.Text.Json;
using Rollback.Admin.Models.Spells;

namespace Rollback.Admin.Services;

public sealed class ClientSpellMetadataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private static readonly object SyncRoot = new();
    private static Lazy<ClientSpellMetadataState> _state = CreateState();

    public AdminClientSpellMetadata Get(short spellId)
    {
        if (!_state.Value.EntriesBySpellId.TryGetValue(spellId, out var entry))
            return new AdminClientSpellMetadata { SpellId = spellId };

        return new AdminClientSpellMetadata
        {
            SpellId = entry.SpellId,
            TypeId = entry.TypeId,
            NameId = entry.NameId,
            DescriptionId = entry.DescriptionId,
            IconId = entry.IconId,
            ScriptParams = entry.ScriptParams ?? string.Empty,
            ScriptId = entry.ScriptId,
        };
    }

    public static void RegisterOrUpdate(AdminClientSpellMetadata metadata)
    {
        if (metadata.SpellId <= 0)
            return;

        lock (SyncRoot)
        {
            _state.Value.EntriesBySpellId[metadata.SpellId] = new ClientMapEntry
            {
                SpellId = metadata.SpellId,
                TypeId = metadata.TypeId,
                NameId = metadata.NameId,
                DescriptionId = metadata.DescriptionId,
                IconId = metadata.IconId,
                ScriptParams = metadata.ScriptParams,
                ScriptId = metadata.ScriptId,
            };

            TryPersistGeneratedMap(_state.Value.EntriesBySpellId);
        }
    }

    public static void InvalidateCache()
    {
        lock (SyncRoot)
            _state = CreateState();
    }

    private static Lazy<ClientSpellMetadataState> CreateState() =>
        new(LoadState, LazyThreadSafetyMode.ExecutionAndPublication);

    private static ClientSpellMetadataState LoadState()
    {
        foreach (var candidate in FindJsonCandidates())
        {
            var entries = TryLoadFromJson(candidate);
            if (entries.Count > 0)
            {
                return new ClientSpellMetadataState(
                    entries,
                    $"Mapa local encontrado en {Path.GetFileName(candidate)}");
            }
        }

        var extractedEntries = ExtractFromSwf(out var extractedSource);
        if (extractedEntries.Count > 0)
        {
            TryPersistGeneratedMap(extractedEntries);
            return new ClientSpellMetadataState(extractedEntries, extractedSource);
        }

        return new ClientSpellMetadataState(new Dictionary<short, ClientMapEntry>(), "Sin metadata cliente extraida");
    }

    private static Dictionary<short, ClientMapEntry> TryLoadFromJson(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return new Dictionary<short, ClientMapEntry>();

            var json = File.ReadAllText(filePath, Encoding.UTF8);
            var items = JsonSerializer.Deserialize<List<ClientMapEntry>>(json) ?? new List<ClientMapEntry>();

            return items
                .Where(x => x.SpellId > 0)
                .GroupBy(x => x.SpellId)
                .ToDictionary(group => group.Key, group => group.Last());
        }
        catch
        {
            return new Dictionary<short, ClientMapEntry>();
        }
    }

    private static Dictionary<short, ClientMapEntry> ExtractFromSwf(out string sourceDescription)
    {
        var commonDirectory = ClientSwfAbcSupport.FindCommonDirectory("Spells0.swf");
        if (commonDirectory is null)
        {
            sourceDescription = "No se encontro client/app/data/common para extraer Spells*.swf";
            return new Dictionary<short, ClientMapEntry>();
        }

        var results = new Dictionary<short, ClientMapEntry>();
        foreach (var swfPath in Directory.EnumerateFiles(commonDirectory, "Spells*.swf", SearchOption.TopDirectoryOnly))
        {
            try
            {
                foreach (var pair in ExtractFromSwfFile(swfPath))
                    results[pair.Key] = pair.Value;
            }
            catch
            {
                // Keep the admin usable even if one SWF is malformed.
            }
        }

        sourceDescription = results.Count > 0
            ? "Extraido automaticamente desde common/Spells*.swf"
            : "No se pudo reconstruir la metadata de spells desde Spells*.swf";

        return results;
    }

    private static Dictionary<short, ClientMapEntry> ExtractFromSwfFile(string swfPath)
    {
        var body = ClientSwfAbcSupport.ReadSwfBody(File.ReadAllBytes(swfPath));
        var mappings = new Dictionary<short, ClientMapEntry>();

        foreach (var abcPayload in ClientSwfAbcSupport.EnumerateDoAbcPayloads(body))
        {
            var abc = new ClientSwfAbcSupport.AbcFile(abcPayload);
            abc.Parse();

            var dataIndex = abc.FindMultinameIndex("_datas");
            var createPatterns = abc.FindMultinameIndices("create")
                .Select(index => (MultinameIndex: index, ArgCount: 8))
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

    private static IEnumerable<KeyValuePair<short, ClientMapEntry>> ExtractMappingsFromBody(
        byte[] code,
        ClientSwfAbcSupport.AbcFile abc,
        int dataIndex,
        IReadOnlyCollection<(int MultinameIndex, int ArgCount)> createPatterns)
    {
        var offset = 0;
        while (offset < code.Length)
        {
            if (!TryParseSpellBlock(code, offset, abc, dataIndex, createPatterns, out var consumedBytes, out var entry))
            {
                offset++;
                continue;
            }

            if (entry.SpellId > 0)
                yield return new KeyValuePair<short, ClientMapEntry>(entry.SpellId, entry);

            offset = consumedBytes;
        }
    }

    private static bool TryParseSpellBlock(
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

        var arguments = new List<object?>(7);
        while (arguments.Count < 7)
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
            callOffset = ClientSwfAbcSupport.IndexOf(code, callPattern, offset, Math.Min(code.Length, startOffset + 1024));
            if (callOffset < 0)
                continue;

            matchedPatternLength = callPattern.Length;
            break;
        }

        if (callOffset < 0 || matchedPatternLength <= 0)
            return false;

        if (!ClientSwfAbcSupport.TryToShort(mapKey, out var mapSpellId))
            return false;

        if (!ClientSwfAbcSupport.TryToShort(arguments[0], out var spellIdFromArgs))
            return false;

        if (spellIdFromArgs != mapSpellId)
            return false;

        entry.SpellId = spellIdFromArgs;
        entry.NameId = ClientSwfAbcSupport.TryToInt(arguments.ElementAtOrDefault(1), out var nameId) ? nameId : null;
        entry.DescriptionId = ClientSwfAbcSupport.TryToInt(arguments.ElementAtOrDefault(2), out var descriptionId) ? descriptionId : null;
        entry.ScriptParams = arguments.ElementAtOrDefault(3)?.ToString() ?? string.Empty;
        entry.ScriptId = ClientSwfAbcSupport.TryToInt(arguments.ElementAtOrDefault(4), out var scriptId) ? scriptId : null;
        entry.IconId = ClientSwfAbcSupport.TryToInt(arguments.ElementAtOrDefault(5), out var iconId) ? iconId : null;
        entry.TypeId = ClientSwfAbcSupport.TryToShort(arguments.ElementAtOrDefault(6), out var typeId) ? typeId : null;

        consumedBytes = callOffset + matchedPatternLength;
        return entry.SpellId > 0 &&
               (entry.NameId.HasValue || entry.DescriptionId.HasValue || entry.TypeId.HasValue);
    }

    private static IEnumerable<string> FindJsonCandidates()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            yield return Path.Combine(current.FullName, "client", "app", "data", "common", "spell-client-map.generated.json");
            yield return Path.Combine(current.FullName, "client", "app", "data", "common", "spell-client-map.json");
            yield return Path.Combine(current.FullName, "spell-client-map.generated.json");
            current = current.Parent;
        }
    }

    private static IEnumerable<string> FindWritableJsonCandidates()
    {
        var commonDirectory = ClientSwfAbcSupport.FindCommonDirectory("Spells0.swf");
        if (!string.IsNullOrWhiteSpace(commonDirectory))
            yield return Path.Combine(commonDirectory, "spell-client-map.generated.json");

        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            yield return Path.Combine(current.FullName, "spell-client-map.generated.json");
            current = current.Parent;
        }
    }

    private static void TryPersistGeneratedMap(Dictionary<short, ClientMapEntry> mappings)
    {
        var orderedEntries = mappings.Values
            .OrderBy(x => x.SpellId)
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

    private sealed class ClientSpellMetadataState
    {
        public ClientSpellMetadataState(
            Dictionary<short, ClientMapEntry> entriesBySpellId,
            string sourceDescription)
        {
            EntriesBySpellId = entriesBySpellId;
            SourceDescription = sourceDescription;
        }

        public Dictionary<short, ClientMapEntry> EntriesBySpellId { get; }

        public string SourceDescription { get; }
    }

    private sealed class ClientMapEntry
    {
        public short SpellId { get; set; }

        public short? TypeId { get; set; }

        public int? NameId { get; set; }

        public int? DescriptionId { get; set; }

        public int? IconId { get; set; }

        public string ScriptParams { get; set; } = string.Empty;

        public int? ScriptId { get; set; }
    }
}
