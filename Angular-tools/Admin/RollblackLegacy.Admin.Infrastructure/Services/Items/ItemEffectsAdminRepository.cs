using Dapper;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Infrastructure.Data;

namespace RollblackLegacy.Admin.Infrastructure.Services.Items;

public sealed class ItemEffectsAdminRepository : IItemEffectsAdminRepository
{
    private readonly AdminDbConnectionFactory _connectionFactory;

    public ItemEffectsAdminRepository(AdminDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ItemEffectsRow?> GetEffectsRowAsync(int itemId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Id AS ItemId,
                TypeId,
                Effects
            FROM items
            WHERE Id = @ItemId
            LIMIT 1;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<ItemEffectsRow>(new CommandDefinition(
            sql,
            new { ItemId = itemId },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateEffectsHexAsync(
        int itemId,
        string effectsHex,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE items
            SET Effects = @Effects
            WHERE Id = @ItemId;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { ItemId = itemId, Effects = effectsHex },
            cancellationToken: cancellationToken));

        return affected > 0;
    }
}
