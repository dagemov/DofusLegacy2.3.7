using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Monsters;

namespace Rollback.Admin.Services;

public sealed class MonsterFamilyCatalogService
{
    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly MonsterCatalogService _monsterCatalogService;

    public MonsterFamilyCatalogService(
        AdminDbConnectionFactory connectionFactory,
        MonsterCatalogService monsterCatalogService)
    {
        _connectionFactory = connectionFactory;
        _monsterCatalogService = monsterCatalogService;
    }

    public async Task<IReadOnlyList<MonsterFamilyCatalogItem>> GetFamiliesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                mt.Race,
                COUNT(DISTINCT mt.Id) AS MonsterCount,
                COALESCE(MIN(mg.Level), 0) AS MinLevel,
                COALESCE(MAX(mg.Level), 0) AS MaxLevel,
                GROUP_CONCAT(DISTINCT mt.Id ORDER BY mt.Id SEPARATOR ',') AS SampleIds
            FROM monsters_templates mt
            LEFT JOIN monsters_grades mg ON mg.MonsterId = mt.Id
            GROUP BY mt.Race
            ORDER BY MinLevel ASC, mt.Race ASC;
            """;

        var rawItems = new List<(byte Race, int Count, short Min, short Max, short[] SampleIds)>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                rawItems.Add((
                    reader.GetSafeByte("Race"),
                    reader.GetSafeInt32("MonsterCount"),
                    reader.GetSafeInt16("MinLevel"),
                    reader.GetSafeInt16("MaxLevel"),
                    ParseSampleIds(reader.GetSafeString("SampleIds"))));
            }
        }

        var allSampleIds = rawItems
            .SelectMany(item => item.SampleIds.Take(4))
            .Distinct()
            .ToArray();
        var presentations = await _monsterCatalogService.GetPresentationsAsync(connection, allSampleIds, cancellationToken);

        return rawItems
            .Select(item =>
            {
                var samples = item.SampleIds
                    .Take(4)
                    .Select(id => presentations.TryGetValue(id, out var presentation)
                        ? $"{presentation.DisplayName} #{id}"
                        : $"Monstruo #{id}")
                    .ToArray();

                return new MonsterFamilyCatalogItem
                {
                    Race = item.Race,
                    Label = $"Familia {item.Race}",
                    MonsterCount = item.Count,
                    MinLevel = item.Min,
                    MaxLevel = item.Max,
                    SampleMonsters = string.Join(", ", samples),
                };
            })
            .ToArray();
    }

    private static short[] ParseSampleIds(string value) =>
        value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(piece => short.TryParse(piece, out var id) ? id : (short)0)
            .Where(id => id > 0)
            .ToArray();
}
