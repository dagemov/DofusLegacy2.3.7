using System.Text;
using System.Text.Json;
using Rollback.Admin.Models.Items;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class ClientItemMetadataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private static readonly object SyncRoot = new();
    private static readonly ClientDataPathResolver PathResolver = new();
    private static readonly FfdecItemScriptExtractor FfdecExtractor = new(PathResolver);
    private static Lazy<ClientItemMetadataState> _state = CreateState();

    public string MappingSourceDescription =>
        _state.Value.SourceDescription;

    public bool HasMetadata =>
        _state.Value.EntriesByItemId.Count > 0;

    public AdminClientItemMetadata Get(short itemId)
    {
        if (itemId <= 0)
            return new AdminClientItemMetadata { ItemId = itemId };

        EnsureChunkLoaded((short)(itemId / 1000));

        if (!_state.Value.EntriesByItemId.TryGetValue(itemId, out var entry))
            return new AdminClientItemMetadata { ItemId = itemId };

        return ToModel(entry);
    }

    public IReadOnlyList<AdminClientItemMetadata> GetAll()
    {
        EnsureAllChunksLoaded();

        lock (SyncRoot)
        {
            return _state.Value.EntriesByItemId.Values
                .OrderBy(x => x.ItemId)
                .Select(ToModel)
                .ToArray();
        }
    }

    public static void RegisterOrUpdate(AdminClientItemMetadata metadata)
    {
        if (metadata.ItemId <= 0)
            return;

        lock (SyncRoot)
        {
            var state = _state.Value;
            state.EntriesByItemId[metadata.ItemId] = ToEntry(metadata);
            state.LoadedChunks.Add((short)(metadata.ItemId / 1000));
            state.SourceDescription = "Mapa cliente actualizado con metadata publicada desde admin";
            TryPersistGeneratedMap(state.EntriesByItemId);
        }
    }

    public static void InvalidateCache()
    {
        lock (SyncRoot)
            _state = CreateState();
    }

    private void EnsureChunkLoaded(short chunkId)
    {
        lock (SyncRoot)
        {
            var state = _state.Value;
            EnsureChunkLoadedUnlocked(state, chunkId);
        }
    }

    private void EnsureAllChunksLoaded()
    {
        var commonDirectory = FindCommonDirectory();
        if (string.IsNullOrWhiteSpace(commonDirectory) || !Directory.Exists(commonDirectory))
            return;

        var chunkIds = Directory
            .EnumerateFiles(commonDirectory, "Items*.swf", SearchOption.TopDirectoryOnly)
            .Select(ParseChunkId)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        if (chunkIds.Length == 0)
            return;

        lock (SyncRoot)
        {
            var state = _state.Value;
            foreach (var chunkId in chunkIds)
                EnsureChunkLoadedUnlocked(state, chunkId);
        }
    }

    private static short? ParseChunkId(string swfPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(swfPath);
        if (!fileName.StartsWith("Items", StringComparison.OrdinalIgnoreCase))
            return null;

        return short.TryParse(fileName["Items".Length..], out var chunkId)
            ? chunkId
            : null;
    }

    private static void EnsureChunkLoadedUnlocked(ClientItemMetadataState state, short chunkId)
    {
        if (state.LoadedChunks.Contains(chunkId))
            return;

        state.LoadedChunks.Add(chunkId);
        if (!FfdecExtractor.IsAvailable)
            return;

        if (!FfdecExtractor.TryExtractChunk(chunkId, out var extractedEntries, out var sourceDescription))
            return;

        var changed = false;
        foreach (var pair in extractedEntries)
        {
            if (state.EntriesByItemId.TryGetValue(pair.Key, out var existing) &&
                MetadataEquivalent(existing, pair.Value))
            {
                continue;
            }

            state.EntriesByItemId[pair.Key] = ToEntry(pair.Value);
            changed = true;
        }

        if (!changed)
            return;

        state.SourceDescription = sourceDescription;
        TryPersistGeneratedMap(state.EntriesByItemId);
    }

    private static Lazy<ClientItemMetadataState> CreateState() =>
        new(LoadState, LazyThreadSafetyMode.ExecutionAndPublication);

    private static ClientItemMetadataState LoadState()
    {
        var mergedEntries = new Dictionary<short, ClientMapEntry>();
        var fallbackSource = string.Empty;

        foreach (var candidate in FindJsonCandidates())
        {
            var entries = TryLoadFromJson(candidate);
            if (entries.Count <= 0)
                continue;

            foreach (var pair in entries)
                mergedEntries[pair.Key] = pair.Value;

            if (string.IsNullOrWhiteSpace(fallbackSource))
                fallbackSource = $"Mapa local encontrado en {Path.GetFileName(candidate)}";
        }

        var extractedEntries = ExtractFromSwf(out var extractedSource);
        if (extractedEntries.Count > 0)
        {
            foreach (var pair in extractedEntries)
                mergedEntries[pair.Key] = pair.Value;

            TryPersistGeneratedMap(mergedEntries);

            var sourceDescription = mergedEntries.Count > extractedEntries.Count &&
                                    !string.IsNullOrWhiteSpace(fallbackSource)
                ? $"{extractedSource} (fusionado con mapa local previo)"
                : extractedSource;

            return new ClientItemMetadataState(mergedEntries, sourceDescription);
        }

        if (mergedEntries.Values.Any(HasVisualMetadata))
        {
            return new ClientItemMetadataState(
                mergedEntries,
                string.IsNullOrWhiteSpace(fallbackSource)
                    ? "Mapa local cliente reutilizado"
                    : fallbackSource);
        }

        foreach (var candidate in FindJsonCandidates())
        {
            var entries = TryLoadFromJson(candidate);
            if (entries.Count <= 0)
                continue;

            if (entries.Values.Any())
            {
                return new ClientItemMetadataState(
                    entries,
                    $"Mapa local encontrado en {Path.GetFileName(candidate)}");
            }
        }

        return new ClientItemMetadataState(new Dictionary<short, ClientMapEntry>(), "Sin metadata cliente extraida");
    }

    private static bool HasVisualMetadata(ClientMapEntry entry) =>
        entry.IconId is > 0 || entry.AppearanceId is > 0;

    private static Dictionary<short, ClientMapEntry> TryLoadFromJson(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return new Dictionary<short, ClientMapEntry>();

            var json = File.ReadAllText(filePath, Encoding.UTF8);
            var items = JsonSerializer.Deserialize<List<ClientMapEntry>>(json) ?? new List<ClientMapEntry>();

            return items
                .Where(x => x.ItemId > 0)
                .GroupBy(x => x.ItemId)
                .ToDictionary(group => group.Key, group => group.Last());
        }
        catch
        {
            return new Dictionary<short, ClientMapEntry>();
        }
    }

    private static Dictionary<short, ClientMapEntry> ExtractFromSwf(out string sourceDescription)
    {
        var commonDirectory = FindCommonDirectory();
        if (commonDirectory is null)
        {
            sourceDescription = "No se encontro client/app/data/common para extraer Items*.swf";
            return new Dictionary<short, ClientMapEntry>();
        }

        var results = new Dictionary<short, ClientMapEntry>();
        foreach (var swfPath in Directory.EnumerateFiles(commonDirectory, "Items*.swf", SearchOption.TopDirectoryOnly))
        {
            try
            {
                foreach (var pair in ExtractFromSwfFile(swfPath))
                    results[pair.Key] = pair.Value;
            }
            catch
            {
                // Ignore malformed SWF files and keep the admin usable.
            }
        }

        sourceDescription = results.Count > 0
            ? "Extraido automaticamente desde common/Items*.swf"
            : "No se pudo reconstruir la metadata de items desde Items*.swf";

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
            var createPatterns = new List<(int MultinameIndex, int ArgCount)>();
            foreach (var createIndex in abc.FindMultinameIndices("create"))
                createPatterns.Add((createIndex, 18));

            foreach (var createWeaponIndex in abc.FindMultinameIndices("createWeapon"))
                createPatterns.Add((createWeaponIndex, 26));

            if (dataIndex <= 0 || createPatterns.Count <= 0)
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
            if (!TryParseItemBlock(code, offset, abc, dataIndex, createPatterns, out var consumedBytes, out var entry))
            {
                offset++;
                continue;
            }

            if (entry.ItemId > 0)
                yield return new KeyValuePair<short, ClientMapEntry>(entry.ItemId, entry);

            offset = consumedBytes;
        }
    }

    private static bool TryParseItemBlock(
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

        var arguments = new List<object?>(17);
        while (arguments.Count < 17)
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

        if (!ClientSwfAbcSupport.TryToShort(mapKey, out var mapItemId))
            return false;

        if (!ClientSwfAbcSupport.TryToShort(arguments[0], out var itemIdFromArgs))
            return false;

        if (itemIdFromArgs != mapItemId)
            return false;

        entry.ItemId = itemIdFromArgs;
        entry.NameId = ClientSwfAbcSupport.TryToInt(arguments.ElementAtOrDefault(1), out var nameId) ? nameId : null;
        entry.TypeId = ClientSwfAbcSupport.TryToShort(arguments.ElementAtOrDefault(2), out var typeId) ? typeId : null;
        entry.DescriptionId = ClientSwfAbcSupport.TryToInt(arguments.ElementAtOrDefault(3), out var descriptionId) ? descriptionId : null;
        entry.IconId = ClientSwfAbcSupport.TryToInt(arguments.ElementAtOrDefault(4), out var iconId) ? iconId : null;
        entry.AppearanceId = ClientSwfAbcSupport.TryToShort(arguments.ElementAtOrDefault(16), out var appearanceId) ? appearanceId : null;

        consumedBytes = callOffset + matchedPatternLength;
        return entry.ItemId > 0 &&
               (entry.NameId.HasValue || entry.DescriptionId.HasValue || entry.IconId.HasValue || entry.AppearanceId.HasValue);
    }

    private static IEnumerable<string> FindJsonCandidates()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            yield return Path.Combine(current.FullName, "client", "app", "data", "common", "item-client-map.generated.json");
            yield return Path.Combine(current.FullName, "client", "app", "data", "common", "item-client-map.json");
            yield return Path.Combine(current.FullName, "item-client-map.generated.json");
            current = current.Parent;
        }
    }

    private static IEnumerable<string> FindWritableJsonCandidates()
    {
        var commonDirectory = FindCommonDirectory();
        if (!string.IsNullOrWhiteSpace(commonDirectory))
            yield return Path.Combine(commonDirectory, "item-client-map.generated.json");

        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            yield return Path.Combine(current.FullName, "item-client-map.generated.json");
            current = current.Parent;
        }
    }

    private static string? FindCommonDirectory() =>
        ClientSwfAbcSupport.FindCommonDirectory("Items0.swf");

    private static void TryPersistGeneratedMap(Dictionary<short, ClientMapEntry> mappings)
    {
        var orderedEntries = mappings.Values
            .OrderBy(x => x.ItemId)
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

    private static AdminClientItemMetadata ToModel(ClientMapEntry entry) =>
        new()
        {
            ItemId = entry.ItemId,
            TypeId = entry.TypeId.HasValue ? (ItemType?)entry.TypeId.Value : null,
            NameId = entry.NameId,
            DescriptionId = entry.DescriptionId,
            IconId = entry.IconId,
            AppearanceId = entry.AppearanceId,
        };

    private static ClientMapEntry ToEntry(AdminClientItemMetadata metadata) =>
        new()
        {
            ItemId = metadata.ItemId,
            TypeId = metadata.TypeId.HasValue ? (short?)metadata.TypeId.Value : null,
            NameId = metadata.NameId,
            DescriptionId = metadata.DescriptionId,
            IconId = metadata.IconId,
            AppearanceId = metadata.AppearanceId,
        };

    private static bool MetadataEquivalent(ClientMapEntry existing, AdminClientItemMetadata incoming) =>
        existing.ItemId == incoming.ItemId &&
        existing.TypeId == (incoming.TypeId.HasValue ? (short?)incoming.TypeId.Value : null) &&
        existing.NameId == incoming.NameId &&
        existing.DescriptionId == incoming.DescriptionId &&
        existing.IconId == incoming.IconId &&
        existing.AppearanceId == incoming.AppearanceId;

    private sealed class ClientItemMetadataState
    {
        public ClientItemMetadataState(
            Dictionary<short, ClientMapEntry> entriesByItemId,
            string sourceDescription)
        {
            EntriesByItemId = entriesByItemId;
            SourceDescription = sourceDescription;
        }

        public Dictionary<short, ClientMapEntry> EntriesByItemId { get; }

        public HashSet<short> LoadedChunks { get; } = new();

        public string SourceDescription { get; set; }
    }

    private sealed class ClientMapEntry
    {
        public short ItemId { get; set; }

        public short? TypeId { get; set; }

        public int? NameId { get; set; }

        public int? DescriptionId { get; set; }

        public int? IconId { get; set; }

        public short? AppearanceId { get; set; }
    }
}
