using Dapper;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Models.Items;
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

    public async Task<IReadOnlyList<AdminItemSetListReadModel>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                s.Id AS SetId,
                s.Name,
                s.Effects AS EffectsHex,
                COUNT(i.Id) AS ItemCount
            FROM items_sets AS s
            LEFT JOIN items AS i ON i.ItemSetId = s.Id
            GROUP BY s.Id, s.Name, s.Effects
            ORDER BY s.Name;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ItemSetListRow>(new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));

        return rows
            .Select(row => new AdminItemSetListReadModel(
                row.SetId,
                row.Name,
                row.ItemCount,
                row.EffectsHex ?? string.Empty))
            .ToList();
    }

    public async Task<AdminItemSetDetailReadModel?> GetByIdAsync(int setId, CancellationToken cancellationToken = default)
    {
        const string setSql = """
            SELECT
                Id AS SetId,
                Name,
                BonusIsSecret,
                Effects AS EffectsHex
            FROM items_sets
            WHERE Id = @SetId
            LIMIT 1;
            """;

        const string itemsSql = """
            SELECT
                i.Id AS ItemId,
                i.Name,
                i.TypeId,
                i.IconId,
                i.AppearanceId
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
                row.AppearanceId))
            .ToList();

        return new AdminItemSetDetailReadModel(
            setRow.SetId,
            setRow.Name,
            setRow.BonusIsSecret,
            setRow.EffectsHex ?? string.Empty,
            members);
    }

    private sealed class ItemSetListRow
    {
        public int SetId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int ItemCount { get; init; }
        public string? EffectsHex { get; init; }
    }

    private sealed class ItemSetDetailRow
    {
        public int SetId { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool BonusIsSecret { get; init; }
        public string? EffectsHex { get; init; }
    }

    private sealed class ItemSetMemberRow
    {
        public int ItemId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int TypeId { get; init; }
        public int IconId { get; init; }
        public int AppearanceId { get; init; }
    }
}
