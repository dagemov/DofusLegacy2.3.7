using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Spells;

namespace Rollback.Admin.Services;

public sealed class ClassicSpellDomainService
{
    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly ReferenceSpellCatalogService _referenceCatalogService;

    public ClassicSpellDomainService(
        AdminDbConnectionFactory connectionFactory,
        ReferenceSpellCatalogService referenceCatalogService)
    {
        _connectionFactory = connectionFactory;
        _referenceCatalogService = referenceCatalogService;
    }

    public async Task<ClassicSpellDomainSnapshot> BuildAsync(
        IEnumerable<short> runtimeSpellIds,
        CancellationToken cancellationToken = default)
    {
        var runtimeIds = runtimeSpellIds
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        var classicBreedSpellIds = await LoadClassicBreedSpellIdsAsync(cancellationToken);
        var referenceClassicIds = _referenceCatalogService.GetClassicSpellIds()
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();
        var referenceModernIds = _referenceCatalogService.GetModernSpellIds()
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();
        var referenceSupportIds = _referenceCatalogService.GetSupportSpellIds()
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        var adminSpellIds = runtimeIds
            .Concat(referenceClassicIds)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        return new ClassicSpellDomainSnapshot
        {
            RuntimeSpellIds = runtimeIds,
            ClassicBreedSpellIds = classicBreedSpellIds,
            ReferenceClassicSpellIds = referenceClassicIds,
            ReferenceModernSpellIds = referenceModernIds,
            ReferenceSupportSpellIds = referenceSupportIds,
            AdminSpellIds = adminSpellIds,
            ExcludedModernReferenceCount = referenceModernIds.Except(runtimeIds).Count(),
        };
    }

    private async Task<short[]> LoadClassicBreedSpellIdsAsync(CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DISTINCT SpellId
            FROM breeds_spells
            WHERE BreedId BETWEEN 1 AND 12
            ORDER BY SpellId ASC;
            """;

        var result = new List<short>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var spellId = reader.GetSafeInt16("SpellId");
                if (spellId > 0)
                    result.Add(spellId);
            }
        }

        return result.ToArray();
    }
}
