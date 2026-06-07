using Dapper;
using MySqlConnector;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Exceptions;
using RollblackLegacy.Admin.Application.Models.Items;
using RollblackLegacy.Admin.Infrastructure.Data;

namespace RollblackLegacy.Admin.Infrastructure.Services.Items;

public sealed class ItemSetsAdminWriteRepository : IItemSetsAdminWriteRepository
{
    private readonly AdminDbConnectionFactory _connectionFactory;

    public ItemSetsAdminWriteRepository(AdminDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(AdminItemSetWriteDraft draft, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var nextSetId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COALESCE(MAX(Id), 0) + 1 FROM items_sets;",
                transaction: transaction,
                cancellationToken: cancellationToken));

            const string insertSql = """
                INSERT INTO items_sets (Id, Name, BonusIsSecret, Effects)
                VALUES (@SetId, @Name, 0, @EffectsHex);
                """;

            await connection.ExecuteAsync(new CommandDefinition(
                insertSql,
                new
                {
                    SetId = nextSetId,
                    Name = draft.Name.Trim(),
                    EffectsHex = draft.EffectsHex,
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await SyncMembershipAsync(connection, transaction, nextSetId, draft.ItemIds, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return nextSetId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateAsync(int setId, AdminItemSetWriteDraft draft, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string updateSql = """
                UPDATE items_sets
                SET Name = @Name,
                    Effects = @EffectsHex
                WHERE Id = @SetId;
                """;

            var affected = await connection.ExecuteAsync(new CommandDefinition(
                updateSql,
                new
                {
                    SetId = setId,
                    Name = draft.Name.Trim(),
                    EffectsHex = draft.EffectsHex,
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (affected == 0)
            {
                throw new AdminEntityNotFoundException("item-set", setId.ToString());
            }

            await SyncMembershipAsync(connection, transaction, setId, draft.ItemIds, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteAsync(int setId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE items SET ItemSetId = -1 WHERE ItemSetId = @SetId;",
                new { SetId = setId },
                transaction: transaction,
                cancellationToken: cancellationToken));

            var affected = await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM items_sets WHERE Id = @SetId;",
                new { SetId = setId },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (affected == 0)
            {
                throw new AdminEntityNotFoundException("item-set", setId.ToString());
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task SyncMembershipAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int setId,
        IReadOnlyList<int> itemIds,
        CancellationToken cancellationToken)
    {
        var normalizedIds = itemIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (normalizedIds.Length > 0)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                """
                UPDATE items
                SET ItemSetId = -1
                WHERE ItemSetId <> @SetId
                  AND Id IN @ItemIds;
                """,
                new { SetId = setId, ItemIds = normalizedIds },
                transaction: transaction,
                cancellationToken: cancellationToken));
        }

        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE items
            SET ItemSetId = -1
            WHERE ItemSetId = @SetId
              AND (@HasItems = 0 OR Id NOT IN @ItemIds);
            """,
            new
            {
                SetId = setId,
                HasItems = normalizedIds.Length > 0 ? 1 : 0,
                ItemIds = normalizedIds.Length > 0 ? normalizedIds : new[] { -1 },
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (normalizedIds.Length == 0)
        {
            return;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE items
            SET ItemSetId = @SetId
            WHERE Id IN @ItemIds;
            """,
            new { SetId = setId, ItemIds = normalizedIds },
            transaction: transaction,
            cancellationToken: cancellationToken));
    }
}
