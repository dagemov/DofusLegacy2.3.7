using MySqlConnector;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.Monsters;

namespace Rollback.Admin.Services;

public sealed class MonsterCatalogService
{
    private readonly ClientReferenceResolverService _clientReferenceResolver;

    public MonsterCatalogService(ClientReferenceResolverService clientReferenceResolver) =>
        _clientReferenceResolver = clientReferenceResolver;

    public IReadOnlyList<short> SearchClientMonsterIds(string? search, int maxResults = 200) =>
        _clientReferenceResolver.SearchMonsterIds(search, maxResults);

    public async Task<Dictionary<short, MonsterCatalogPresentation>> GetPresentationsAsync(
        MySqlConnection connection,
        IEnumerable<short> monsterIds,
        CancellationToken cancellationToken = default)
    {
        var ids = monsterIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
            return new Dictionary<short, MonsterCatalogPresentation>();

        var overrides = await AdminEntityTextOverrideService.GetManyAsync(
            connection,
            AdminEntityType.Monster,
            ids.Select(id => (int)id).ToArray(),
            cancellationToken);

        var result = new Dictionary<short, MonsterCatalogPresentation>();
        foreach (var id in ids)
        {
            _clientReferenceResolver.TryResolveMonster(id, out var clientReference);
            overrides.TryGetValue(id, out var textOverride);

            var clientName = clientReference?.DisplayName?.Trim() ?? string.Empty;
            var overrideName = textOverride?.DisplayName?.Trim() ?? string.Empty;
            var displayName = !string.IsNullOrWhiteSpace(overrideName)
                ? overrideName
                : !string.IsNullOrWhiteSpace(clientName)
                    ? clientName
                    : $"Monstruo #{id}";

            result[id] = new MonsterCatalogPresentation
            {
                MonsterId = id,
                DisplayName = displayName,
                NameSource = !string.IsNullOrWhiteSpace(overrideName)
                    ? "admin"
                    : !string.IsNullOrWhiteSpace(clientName)
                        ? "client"
                        : "fallback",
                ClientDisplayName = clientName,
                ClientNameId = clientReference?.NameId,
                ClientGfxId = clientReference?.GfxId,
                HasClientReference = clientReference is not null,
                HasNameMismatch = !string.IsNullOrWhiteSpace(overrideName) &&
                                  !string.IsNullOrWhiteSpace(clientName) &&
                                  !string.Equals(overrideName, clientName, StringComparison.OrdinalIgnoreCase),
            };
        }

        return result;
    }

    public static MonsterCatalogPresentation Fallback(short monsterId) =>
        new()
        {
            MonsterId = monsterId,
            DisplayName = $"Monstruo #{monsterId}",
            NameSource = "fallback",
        };
}
