using Dapper;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Models.Items;
using RollblackLegacy.Admin.Contracts.Items;
using RollblackLegacy.Admin.Infrastructure.Data;
using RollblackLegacy.Admin.Infrastructure.Items;

namespace RollblackLegacy.Admin.Infrastructure.Services.Items;

public sealed class ItemSetsAdminReadRepository : IItemSetsAdminReadRepository
{
    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly AdminProtocolCatalog _protocolCatalog;

    public ItemSetsAdminReadRepository(
        AdminDbConnectionFactory connectionFactory,
        AdminProtocolCatalog protocolCatalog)
    {
        _connectionFactory = connectionFactory;
        _protocolCatalog = protocolCatalog;
    }

    public async Task<AdminPagedItemSetsReadModel> SearchAsync(
        ItemSetSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        const string countSql = """
            SELECT COUNT(*)
            FROM (
                SELECT
                    s.Id AS SetId,
                    COALESCE(MIN(i.Level), 0) AS Level,
                    COUNT(i.Id) AS ItemCount
                FROM items_sets AS s
                LEFT JOIN items AS i ON i.ItemSetId = s.Id
                WHERE (@Search IS NULL OR s.Name LIKE CONCAT('%', @Search, '%') OR CAST(s.Id AS CHAR) = @Search)
                GROUP BY s.Id, s.Name, s.Effects
                HAVING (@MinLevel IS NULL OR COALESCE(MIN(i.Level), 0) >= @MinLevel)
                   AND (@MaxLevel IS NULL OR COALESCE(MIN(i.Level), 0) <= @MaxLevel)
                   AND (@MinParts IS NULL OR COUNT(i.Id) >= @MinParts)
                   AND (@MaxParts IS NULL OR COUNT(i.Id) <= @MaxParts)
            ) AS filtered;
            """;

        const string pageSql = """
            SELECT
                s.Id AS SetId,
                s.Name,
                s.Effects AS EffectsHex,
                COALESCE(MIN(i.Level), 0) AS Level,
                COUNT(i.Id) AS ItemCount,
                GROUP_CONCAT(i.IconId ORDER BY i.Level, i.Id SEPARATOR ',') AS IconIdsCsv
            FROM items_sets AS s
            LEFT JOIN items AS i ON i.ItemSetId = s.Id
            WHERE (@Search IS NULL OR s.Name LIKE CONCAT('%', @Search, '%') OR CAST(s.Id AS CHAR) = @Search)
            GROUP BY s.Id, s.Name, s.Effects
            HAVING (@MinLevel IS NULL OR COALESCE(MIN(i.Level), 0) >= @MinLevel)
               AND (@MaxLevel IS NULL OR COALESCE(MIN(i.Level), 0) <= @MaxLevel)
               AND (@MinParts IS NULL OR COUNT(i.Id) >= @MinParts)
               AND (@MaxParts IS NULL OR COUNT(i.Id) <= @MaxParts)
            ORDER BY s.Name, s.Id
            LIMIT @Offset, @PageSize;
            """;

        var parameters = new
        {
            Search = NormalizeSearch(request.Search),
            request.MinLevel,
            request.MaxLevel,
            request.MinParts,
            request.MaxParts,
            Offset = (request.Page - 1) * request.PageSize,
            request.PageSize,
        };

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            countSql,
            parameters,
            cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<ItemSetListRow>(new CommandDefinition(
            pageSql,
            parameters,
            cancellationToken: cancellationToken));

        var items = rows
            .Select(row => new AdminItemSetListReadModel(
                row.SetId,
                row.Name,
                row.Level,
                row.ItemCount,
                row.EffectsHex ?? string.Empty,
                ParseIconIds(row.IconIdsCsv)))
            .ToList();

        return new AdminPagedItemSetsReadModel(totalCount, items);
    }

    public async Task<AdminItemSetDetailReadModel?> GetByIdAsync(int setId, CancellationToken cancellationToken = default)
    {
        const string setSql = """
            SELECT
                s.Id AS SetId,
                s.Name,
                s.BonusIsSecret,
                s.Effects AS EffectsHex,
                COALESCE(MIN(i.Level), 0) AS Level
            FROM items_sets AS s
            LEFT JOIN items AS i ON i.ItemSetId = s.Id
            WHERE s.Id = @SetId
            GROUP BY s.Id, s.Name, s.BonusIsSecret, s.Effects
            LIMIT 1;
            """;

        const string itemsSql = """
            SELECT
                i.Id AS ItemId,
                i.Name,
                i.TypeId,
                i.IconId,
                i.AppearanceId,
                i.Level
            FROM items AS i
            WHERE i.ItemSetId = @SetId
            ORDER BY i.Level, i.Id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var setRow = await connection.QuerySingleOrDefaultAsync<ItemSetDetailRow>(new CommandDefinition(
            setSql,
            new { SetId = setId },
            cancellationToken: cancellationToken));

        if (setRow is null)
        {
            return null;
        }

        var itemRows = await connection.QueryAsync<ItemSetMemberRow>(new CommandDefinition(
            itemsSql,
            new { SetId = setId },
            cancellationToken: cancellationToken));

        var members = itemRows
            .Select(row => new AdminItemSetMemberReadModel(
                row.ItemId,
                row.Name,
                row.TypeId,
                _protocolCatalog.GetItemTypeLabel(row.TypeId) ?? $"Type{row.TypeId}",
                row.IconId,
                row.AppearanceId,
                row.Level))
            .ToList();

        return new AdminItemSetDetailReadModel(
            setRow.SetId,
            setRow.Name,
            setRow.Level,
            setRow.BonusIsSecret,
            setRow.EffectsHex ?? string.Empty,
            members);
    }

    public async Task<bool> ExistsAsync(int setId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM items_sets
            WHERE Id = @SetId;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            new { SetId = setId },
            cancellationToken: cancellationToken));

        return count > 0;
    }

    public async Task<IReadOnlyList<int>> ResolveExistingItemIdsAsync(
        IReadOnlyList<int> itemIds,
        CancellationToken cancellationToken = default)
    {
        if (itemIds.Count == 0)
        {
            return [];
        }

        const string sql = """
            SELECT Id
            FROM items
            WHERE Id IN @ItemIds;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<int>(new CommandDefinition(
            sql,
            new { ItemIds = itemIds.Distinct().ToArray() },
            cancellationToken: cancellationToken));

        return rows.ToList();
    }

    private static string? NormalizeSearch(string? search)
    {
        return string.IsNullOrWhiteSpace(search) ? null : search.Trim();
    }

    private static IReadOnlyList<int> ParseIconIds(string? iconIdsCsv)
    {
        if (string.IsNullOrWhiteSpace(iconIdsCsv))
        {
            return [];
        }

        return iconIdsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var iconId) ? iconId : 0)
            .Where(iconId => iconId > 0)
            .Distinct()
            .Take(4)
            .ToList();
    }

    private sealed class ItemSetListRow
    {
        public int SetId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Level { get; init; }
        public int ItemCount { get; init; }
        public string? EffectsHex { get; init; }
        public string? IconIdsCsv { get; init; }
    }

    private sealed class ItemSetDetailRow
    {
        public int SetId { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool BonusIsSecret { get; init; }
        public string? EffectsHex { get; init; }
        public int Level { get; init; }
    }

    private sealed class ItemSetMemberRow
    {
        public int ItemId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int TypeId { get; init; }
        public int IconId { get; init; }
        public int AppearanceId { get; init; }
        public int Level { get; init; }
    }
}
