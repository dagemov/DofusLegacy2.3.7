using System.Text;
using System.Text.Json;
using Rollback.Admin.Models.Monsters;

namespace Rollback.Admin.Services;

public sealed class ClientReferenceResolverService
{
    private readonly ClientDataPathResolver _pathResolver;
    private readonly Lazy<ClientReferenceState> _state;

    public ClientReferenceResolverService(ClientDataPathResolver pathResolver)
    {
        _pathResolver = pathResolver;
        _state = new Lazy<ClientReferenceState>(LoadState, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string MonsterSourceDescription => _state.Value.MonsterSourceDescription;

    public bool TryResolveMonster(short monsterId, out ClientMonsterReference reference) =>
        _state.Value.MonstersById.TryGetValue(monsterId, out reference!);

    public IReadOnlyList<short> SearchMonsterIds(string? search, int maxResults = 200)
    {
        var normalized = (search ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return Array.Empty<short>();

        var matches = _state.Value.MonstersById.Values
            .Where(reference =>
                reference.MonsterId.ToString().Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                (reference.NameId?.ToString().Contains(normalized, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (reference.GfxId?.ToString().Contains(normalized, StringComparison.OrdinalIgnoreCase) ?? false) ||
                reference.DisplayName.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .OrderBy(reference => reference.MonsterId)
            .Take(Math.Max(1, maxResults))
            .Select(reference => reference.MonsterId)
            .ToArray();

        return matches;
    }

    private ClientReferenceState LoadState()
    {
        var monsters = new Dictionary<short, ClientMonsterReference>();
        var sourceDescription = "Sin referencias cliente de monstruos";

        foreach (var candidate in EnumerateMonsterCatalogCandidates())
        {
            var entries = TryLoadMonsterCatalog(candidate);
            if (entries.Count <= 0)
                continue;

            foreach (var entry in entries)
                monsters[entry.MonsterId] = entry;

            sourceDescription = $"monster-client-map generado: {candidate}";
            break;
        }

        return new ClientReferenceState(monsters, sourceDescription);
    }

    private IEnumerable<string> EnumerateMonsterCatalogCandidates()
    {
        if (_pathResolver.CommonDataDirectory is { Length: > 0 } commonDirectory)
            yield return Path.Combine(commonDirectory, "monster-client-map.generated.json");

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            yield return Path.Combine(current.FullName, "client", "app", "data", "common", "monster-client-map.generated.json");
            yield return Path.Combine(current.FullName, "monster-client-map.generated.json");
            current = current.Parent;
        }
    }

    private static List<ClientMonsterReference> TryLoadMonsterCatalog(string path)
    {
        try
        {
            if (!File.Exists(path))
                return new List<ClientMonsterReference>();

            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<List<ClientMonsterReference>>(json) ?? new List<ClientMonsterReference>();
        }
        catch
        {
            return new List<ClientMonsterReference>();
        }
    }

    private sealed class ClientReferenceState
    {
        public ClientReferenceState(
            Dictionary<short, ClientMonsterReference> monstersById,
            string monsterSourceDescription)
        {
            MonstersById = monstersById;
            MonsterSourceDescription = monsterSourceDescription;
        }

        public Dictionary<short, ClientMonsterReference> MonstersById { get; }

        public string MonsterSourceDescription { get; }
    }
}
