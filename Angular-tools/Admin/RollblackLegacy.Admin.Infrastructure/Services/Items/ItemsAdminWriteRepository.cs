using Dapper;
using MySqlConnector;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Exceptions;
using RollblackLegacy.Admin.Application.Models.Items;
using RollblackLegacy.Admin.Infrastructure.Data;

namespace RollblackLegacy.Admin.Infrastructure.Services.Items;

public sealed class ItemsAdminWriteRepository : IItemsAdminWriteRepository
{
    private const string EmptyEffectsHex = "0000";

    private readonly AdminDbConnectionFactory _connectionFactory;

    public ItemsAdminWriteRepository(AdminDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AdminItemWriteRow?> GetByIdAsync(int itemId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Id AS ItemId,
                Name AS ResolvedName,
                DescriptionId,
                TypeId,
                Level,
                Weight,
                Price,
                Usable,
                Targetable,
                TwoHanded,
                Etheral,
                Criteria AS Conditions,
                IconId,
                AppearanceId,
                ItemSetId AS RawSetId
            FROM items
            WHERE Id = @ItemId
            LIMIT 1;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ItemWriteRow>(new CommandDefinition(
            sql,
            new { ItemId = itemId },
            cancellationToken: cancellationToken));

        return row is null ? null : MapWriteRow(row);
    }

    public async Task<bool> ItemSetExistsAsync(int itemSetId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM items_sets
            WHERE Id = @ItemSetId;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            new { ItemSetId = itemSetId },
            cancellationToken: cancellationToken));

        return count > 0;
    }

    public async Task<IReadOnlySet<int>> GetWeaponTypeIdsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT DISTINCT TypeId
            FROM items_weapons
            ORDER BY TypeId;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.ToHashSet();
    }

    public async Task<AdminItemWriteRow> CreateAsync(AdminItemWriteDraft draft, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var nextItemId = 0;
        var nextDescriptionId = 0;
        var locked = false;

        try
        {
            await connection.ExecuteAsync(new CommandDefinition("LOCK TABLES items WRITE;", cancellationToken: cancellationToken));
            locked = true;

            nextItemId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COALESCE(MAX(Id), 0) + 1 FROM items;",
                cancellationToken: cancellationToken));

            nextDescriptionId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COALESCE(MAX(DescriptionId), 0) + 1 FROM items;",
                cancellationToken: cancellationToken));

            const string insertSql = """
                INSERT INTO items (
                    Id,
                    Weight,
                    Name,
                    TypeId,
                    DescriptionId,
                    IconId,
                    Level,
                    Cursed,
                    UseAnimationId,
                    Usable,
                    Targetable,
                    Price,
                    TwoHanded,
                    Etheral,
                    ItemSetId,
                    Criteria,
                    HideEffects,
                    AppearanceId,
                    RecipeIdsCSV,
                    FavoriteSubAreasCSV,
                    BonusIsSecret,
                    FavoriteSubAreasBonus,
                    Effects
                )
                VALUES (
                    @Id,
                    @Weight,
                    @Name,
                    @TypeId,
                    @DescriptionId,
                    @IconId,
                    @Level,
                    @Cursed,
                    @UseAnimationId,
                    @Usable,
                    @Targetable,
                    @Price,
                    @TwoHanded,
                    @Etheral,
                    @ItemSetId,
                    @Criteria,
                    @HideEffects,
                    @AppearanceId,
                    @RecipeIdsCSV,
                    @FavoriteSubAreasCSV,
                    @BonusIsSecret,
                    @FavoriteSubAreasBonus,
                    @Effects
                );
                """;

            await connection.ExecuteAsync(new CommandDefinition(
                insertSql,
                new
                {
                    Id = nextItemId,
                    draft.Weight,
                    Name = draft.ResolvedName,
                    draft.TypeId,
                    DescriptionId = nextDescriptionId,
                    draft.IconId,
                    draft.Level,
                    Cursed = false,
                    UseAnimationId = -1,
                    draft.Usable,
                    draft.Targetable,
                    draft.Price,
                    draft.TwoHanded,
                    draft.Etheral,
                    ItemSetId = draft.SetId ?? -1,
                    Criteria = draft.Conditions,
                    HideEffects = false,
                    draft.AppearanceId,
                    RecipeIdsCSV = string.Empty,
                    FavoriteSubAreasCSV = string.Empty,
                    BonusIsSecret = false,
                    FavoriteSubAreasBonus = 0,
                    Effects = EmptyEffectsHex
                },
                cancellationToken: cancellationToken));

            return new AdminItemWriteRow(
                nextItemId,
                draft.ResolvedName,
                nextDescriptionId,
                draft.TypeId,
                draft.Level,
                draft.Weight,
                draft.Price,
                draft.Usable,
                draft.Targetable,
                draft.TwoHanded,
                draft.Etheral,
                draft.Conditions,
                draft.IconId,
                draft.AppearanceId,
                draft.SetId);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new AdminConflictException(
                "The new item could not be created because the generated template id or description id conflicted with an existing row. Retry the operation.");
        }
        finally
        {
            if (locked)
            {
                try
                {
                    await connection.ExecuteAsync(new CommandDefinition("UNLOCK TABLES;", cancellationToken: cancellationToken));
                }
                catch
                {
                    // connection disposal will release the lock if unlock fails
                }
            }
        }
    }

    public async Task<AdminItemWriteRow?> UpdateAsync(int itemId, AdminItemWriteDraft draft, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string selectSql = """
            SELECT
                Id AS ItemId,
                Name AS ResolvedName,
                DescriptionId,
                TypeId,
                Level,
                Weight,
                Price,
                Usable,
                Targetable,
                TwoHanded,
                Etheral,
                Criteria AS Conditions,
                IconId,
                AppearanceId,
                ItemSetId AS RawSetId
            FROM items
            WHERE Id = @ItemId
            LIMIT 1;
            """;

        var existing = await connection.QuerySingleOrDefaultAsync<ItemWriteRow>(new CommandDefinition(
            selectSql,
            new { ItemId = itemId },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return null;
        }

        const string updateSql = """
            UPDATE items
            SET
                Weight = @Weight,
                Name = @Name,
                TypeId = @TypeId,
                IconId = @IconId,
                Level = @Level,
                Usable = @Usable,
                Targetable = @Targetable,
                Price = @Price,
                TwoHanded = @TwoHanded,
                Etheral = @Etheral,
                ItemSetId = @ItemSetId,
                Criteria = @Criteria,
                AppearanceId = @AppearanceId
            WHERE Id = @ItemId
            LIMIT 1;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            updateSql,
            new
            {
                ItemId = itemId,
                draft.Weight,
                Name = draft.ResolvedName,
                draft.TypeId,
                draft.IconId,
                draft.Level,
                draft.Usable,
                draft.Targetable,
                draft.Price,
                draft.TwoHanded,
                draft.Etheral,
                ItemSetId = draft.SetId ?? -1,
                Criteria = draft.Conditions,
                draft.AppearanceId
            },
            cancellationToken: cancellationToken));

        return new AdminItemWriteRow(
            itemId,
            draft.ResolvedName,
            existing.DescriptionId,
            draft.TypeId,
            draft.Level,
            draft.Weight,
            draft.Price,
            draft.Usable,
            draft.Targetable,
            draft.TwoHanded,
            draft.Etheral,
            draft.Conditions,
            draft.IconId,
            draft.AppearanceId,
            draft.SetId);
    }

    public async Task<AdminItemWriteRow?> DuplicateAsync(int sourceItemId, AdminItemWriteDraft draft, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sourceSql = """
            SELECT
                Id,
                Weight,
                Name,
                TypeId,
                DescriptionId,
                IconId,
                Level,
                Cursed,
                UseAnimationId,
                Usable,
                Targetable,
                Price,
                TwoHanded,
                Etheral,
                ItemSetId,
                Criteria,
                HideEffects,
                AppearanceId,
                RecipeIdsCSV,
                FavoriteSubAreasCSV,
                BonusIsSecret,
                FavoriteSubAreasBonus,
                Effects
            FROM items
            WHERE Id = @ItemId
            LIMIT 1;
            """;

        var source = await connection.QuerySingleOrDefaultAsync<PersistedItemRow>(new CommandDefinition(
            sourceSql,
            new { ItemId = sourceItemId },
            cancellationToken: cancellationToken));

        if (source is null)
        {
            return null;
        }

        var nextItemId = 0;
        var nextDescriptionId = 0;
        var locked = false;

        try
        {
            await connection.ExecuteAsync(new CommandDefinition("LOCK TABLES items WRITE;", cancellationToken: cancellationToken));
            locked = true;

            nextItemId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COALESCE(MAX(Id), 0) + 1 FROM items;",
                cancellationToken: cancellationToken));

            nextDescriptionId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COALESCE(MAX(DescriptionId), 0) + 1 FROM items;",
                cancellationToken: cancellationToken));

            const string insertSql = """
                INSERT INTO items (
                    Id,
                    Weight,
                    Name,
                    TypeId,
                    DescriptionId,
                    IconId,
                    Level,
                    Cursed,
                    UseAnimationId,
                    Usable,
                    Targetable,
                    Price,
                    TwoHanded,
                    Etheral,
                    ItemSetId,
                    Criteria,
                    HideEffects,
                    AppearanceId,
                    RecipeIdsCSV,
                    FavoriteSubAreasCSV,
                    BonusIsSecret,
                    FavoriteSubAreasBonus,
                    Effects
                )
                VALUES (
                    @Id,
                    @Weight,
                    @Name,
                    @TypeId,
                    @DescriptionId,
                    @IconId,
                    @Level,
                    @Cursed,
                    @UseAnimationId,
                    @Usable,
                    @Targetable,
                    @Price,
                    @TwoHanded,
                    @Etheral,
                    @ItemSetId,
                    @Criteria,
                    @HideEffects,
                    @AppearanceId,
                    @RecipeIdsCSV,
                    @FavoriteSubAreasCSV,
                    @BonusIsSecret,
                    @FavoriteSubAreasBonus,
                    @Effects
                );
                """;

            await connection.ExecuteAsync(new CommandDefinition(
                insertSql,
                new
                {
                    Id = nextItemId,
                    Weight = draft.Weight,
                    Name = draft.ResolvedName,
                    draft.TypeId,
                    DescriptionId = nextDescriptionId,
                    draft.IconId,
                    draft.Level,
                    source.Cursed,
                    source.UseAnimationId,
                    draft.Usable,
                    draft.Targetable,
                    draft.Price,
                    draft.TwoHanded,
                    draft.Etheral,
                    ItemSetId = draft.SetId ?? -1,
                    Criteria = draft.Conditions,
                    source.HideEffects,
                    draft.AppearanceId,
                    source.RecipeIdsCSV,
                    source.FavoriteSubAreasCSV,
                    source.BonusIsSecret,
                    source.FavoriteSubAreasBonus,
                    Effects = string.IsNullOrWhiteSpace(source.Effects) ? EmptyEffectsHex : source.Effects
                },
                cancellationToken: cancellationToken));

            return new AdminItemWriteRow(
                nextItemId,
                draft.ResolvedName,
                nextDescriptionId,
                draft.TypeId,
                draft.Level,
                draft.Weight,
                draft.Price,
                draft.Usable,
                draft.Targetable,
                draft.TwoHanded,
                draft.Etheral,
                draft.Conditions,
                draft.IconId,
                draft.AppearanceId,
                draft.SetId);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new AdminConflictException(
                "The duplicate item could not be created because the generated template id or description id conflicted with an existing row. Retry the operation.");
        }
        finally
        {
            if (locked)
            {
                try
                {
                    await connection.ExecuteAsync(new CommandDefinition("UNLOCK TABLES;", cancellationToken: cancellationToken));
                }
                catch
                {
                    // connection disposal will release the lock if unlock fails
                }
            }
        }
    }

    private static AdminItemWriteRow MapWriteRow(ItemWriteRow row)
    {
        return new AdminItemWriteRow(
            row.ItemId,
            row.ResolvedName ?? string.Empty,
            row.DescriptionId,
            row.TypeId,
            row.Level,
            row.Weight,
            row.Price,
            row.Usable,
            row.Targetable,
            row.TwoHanded,
            row.Etheral,
            NormalizeConditions(row.Conditions),
            row.IconId,
            row.AppearanceId,
            row.RawSetId > 0 ? row.RawSetId : null);
    }

    private static string NormalizeConditions(string? conditions)
    {
        return string.IsNullOrWhiteSpace(conditions) ? "null" : conditions.Trim();
    }

    private sealed class ItemWriteRow
    {
        public int ItemId { get; set; }

        public string? ResolvedName { get; set; }

        public int DescriptionId { get; set; }

        public int TypeId { get; set; }

        public int Level { get; set; }

        public int Weight { get; set; }

        public double Price { get; set; }

        public bool Usable { get; set; }

        public bool Targetable { get; set; }

        public bool TwoHanded { get; set; }

        public bool Etheral { get; set; }

        public string? Conditions { get; set; }

        public int IconId { get; set; }

        public int AppearanceId { get; set; }

        public int RawSetId { get; set; }
    }

    private sealed class PersistedItemRow
    {
        public int Id { get; set; }

        public int Weight { get; set; }

        public string Name { get; set; } = string.Empty;

        public int TypeId { get; set; }

        public int DescriptionId { get; set; }

        public int IconId { get; set; }

        public int Level { get; set; }

        public bool Cursed { get; set; }

        public int UseAnimationId { get; set; }

        public bool Usable { get; set; }

        public bool Targetable { get; set; }

        public double Price { get; set; }

        public bool TwoHanded { get; set; }

        public bool Etheral { get; set; }

        public int ItemSetId { get; set; }

        public string? Criteria { get; set; }

        public bool HideEffects { get; set; }

        public int AppearanceId { get; set; }

        public string RecipeIdsCSV { get; set; } = string.Empty;

        public string FavoriteSubAreasCSV { get; set; } = string.Empty;

        public bool BonusIsSecret { get; set; }

        public int FavoriteSubAreasBonus { get; set; }

        public string? Effects { get; set; }
    }
}
