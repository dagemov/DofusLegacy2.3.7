using Dapper;
using RollblackLegacy.Admin.Application.Abstractions.ClientIdentity;
using RollblackLegacy.Admin.Application.Models.ClientIdentity;
using RollblackLegacy.Admin.Infrastructure.Data;

namespace RollblackLegacy.Admin.Infrastructure.Services.ClientIdentity;

public sealed class MySqlClientItemIdentityRepository : IClientItemIdentityRepository
{
    private readonly AdminDbConnectionFactory _connectionFactory;

    public MySqlClientItemIdentityRepository(AdminDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ClientItemDbSnapshot>> GetItemsAsync(IReadOnlyList<int> itemIds, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                i.Id,
                i.Name,
                i.DescriptionId,
                i.TypeId,
                i.Level,
                i.IconId,
                i.AppearanceId,
                i.ItemSetId
            FROM items AS i
            WHERE i.Id IN @ItemIds
            ORDER BY i.Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { ItemIds = itemIds.Distinct().ToArray() }, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<ClientItemDbRow>(command);

        return rows
            .Select(static row => new ClientItemDbSnapshot(
                row.Id,
                row.Name,
                row.DescriptionId,
                row.TypeId,
                row.Level,
                row.IconId,
                row.AppearanceId,
                row.ItemSetId))
            .ToArray();
    }

    private sealed class ClientItemDbRow
    {
        public int Id { get; init; }

        public string? Name { get; init; }

        public int DescriptionId { get; init; }

        public int TypeId { get; init; }

        public int Level { get; init; }

        public int IconId { get; init; }

        public int AppearanceId { get; init; }

        public int ItemSetId { get; init; }
    }
}
