using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.Monsters;

namespace Rollback.Admin.Services;

public sealed class MonsterGroupAdminService
{
    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly AdminBootstrapService _bootstrapService;
    private readonly MonsterCatalogService _monsterCatalogService;

    public MonsterGroupAdminService(
        AdminDbConnectionFactory connectionFactory,
        AdminBootstrapService bootstrapService,
        MonsterCatalogService monsterCatalogService)
    {
        _connectionFactory = connectionFactory;
        _bootstrapService = bootstrapService;
        _monsterCatalogService = monsterCatalogService;
    }

    public async Task<AdminPagedResult<MonsterGroupListItem>> GetPagedAsync(AdminPagedQuery query, CancellationToken cancellationToken = default)
    {
        await _bootstrapService.EnsureSchemaAsync(cancellationToken);

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var normalized = Normalize(query);
        var search = normalized.Search.Trim();

        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = """
            SELECT COUNT(*)
            FROM admin_monster_groups g
            WHERE @search = ''
               OR g.Name LIKE @wildSearch
               OR g.Notes LIKE @wildSearch;
            """;
        countCommand.Parameters.AddWithValue("@search", search);
        countCommand.Parameters.AddWithValue("@wildSearch", $"%{search}%");
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                g.Id,
                g.Name,
                g.Notes,
                COALESCE(COUNT(DISTINCT e.Id), 0) AS EntryCount,
                COALESCE(COUNT(DISTINCT a.Id), 0) AS AssignmentCount,
                MAX(a.LastSyncedAt) AS LastSyncedAt
            FROM admin_monster_groups g
            LEFT JOIN admin_monster_group_entries e ON e.MonsterGroupId = g.Id
            LEFT JOIN admin_monster_group_assignments a ON a.MonsterGroupId = g.Id
            WHERE @search = ''
               OR g.Name LIKE @wildSearch
               OR g.Notes LIKE @wildSearch
            GROUP BY g.Id, g.Name, g.Notes
            ORDER BY g.Id DESC
            LIMIT @offset, @pageSize;
            """;
        command.Parameters.AddWithValue("@search", search);
        command.Parameters.AddWithValue("@wildSearch", $"%{search}%");
        command.Parameters.AddWithValue("@offset", (normalized.Page - 1) * normalized.PageSize);
        command.Parameters.AddWithValue("@pageSize", normalized.PageSize);

        var items = new List<MonsterGroupListItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new MonsterGroupListItem
            {
                Id = reader.GetSafeInt32("Id"),
                Name = reader.GetSafeString("Name"),
                Notes = reader.GetSafeString("Notes"),
                EntryCount = reader.GetSafeInt32("EntryCount"),
                AssignmentCount = reader.GetSafeInt32("AssignmentCount"),
                LastSyncedAt = reader.GetSafeDateTime("LastSyncedAt"),
            });
        }

        return new AdminPagedResult<MonsterGroupListItem>(items, totalCount, normalized.Page, normalized.PageSize);
    }

    public async Task<MonsterGroupEditModel?> GetByIdAsync(int groupId, CancellationToken cancellationToken = default)
    {
        await _bootstrapService.EnsureSchemaAsync(cancellationToken);

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var model = new MonsterGroupEditModel();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT Id, Name, Notes
                FROM admin_monster_groups
                WHERE Id = @groupId
                LIMIT 1;
                """;
            command.Parameters.AddWithValue("@groupId", groupId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            model.Id = reader.GetSafeInt32("Id");
            model.Name = reader.GetSafeString("Name");
            model.Notes = reader.GetSafeString("Notes");
        }

        await using (var entriesCommand = connection.CreateCommand())
        {
            entriesCommand.CommandText = """
                SELECT
                    e.Id,
                    e.MonsterId,
                    e.MinGrade,
                    e.MaxGrade,
                    e.Probability,
                    e.Disabled,
                    COALESCE(MIN(mg.Level), 0) AS MinLevel,
                    COALESCE(MAX(mg.Level), 0) AS MaxLevel
                FROM admin_monster_group_entries e
                LEFT JOIN monsters_grades mg ON mg.MonsterId = e.MonsterId
                WHERE e.MonsterGroupId = @groupId
                GROUP BY e.Id, e.MonsterId, e.MinGrade, e.MaxGrade, e.Probability, e.Disabled
                ORDER BY e.Id;
                """;
            entriesCommand.Parameters.AddWithValue("@groupId", groupId);

            await using var reader = await entriesCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var monsterId = reader.GetSafeInt16("MonsterId");
                var minLevel = reader.GetSafeInt16("MinLevel");
                var maxLevel = reader.GetSafeInt16("MaxLevel");

                model.Entries.Add(new MonsterGroupEntryAdminModel
                {
                    Id = reader.GetSafeInt32("Id"),
                    MonsterId = monsterId,
                    MonsterLabel = $"Monstruo #{monsterId} · lvl {minLevel}-{maxLevel}",
                    MinGrade = reader.GetSafeSByte("MinGrade", 1),
                    MaxGrade = reader.GetSafeSByte("MaxGrade", 1),
                    Probability = reader.GetSafeByte("Probability", 5),
                    Disabled = reader.GetSafeBoolean("Disabled"),
                });
            }
        }

        await ApplyMonsterPresentationAsync(connection, model.Entries, cancellationToken);

        await using (var assignmentsCommand = connection.CreateCommand())
        {
            assignmentsCommand.CommandText = """
                SELECT
                    a.Id,
                    a.MonsterGroupId,
                    a.MapId,
                    a.SubAreaId,
                    a.ProbabilityOverride,
                    a.Disabled,
                    a.LastSyncedAt,
                    wm.X,
                    wm.Y
                FROM admin_monster_group_assignments a
                LEFT JOIN world_maps wm ON wm.Id = a.MapId
                WHERE a.MonsterGroupId = @groupId
                ORDER BY a.Id;
                """;
            assignmentsCommand.Parameters.AddWithValue("@groupId", groupId);

            await using var reader = await assignmentsCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                int? mapId = reader.IsDBNull(reader.GetOrdinal("MapId")) ? null : reader.GetInt32(reader.GetOrdinal("MapId"));
                short? subAreaId = reader.IsDBNull(reader.GetOrdinal("SubAreaId")) ? null : reader.GetInt16(reader.GetOrdinal("SubAreaId"));
                var targetLabel = mapId.HasValue
                    ? $"Mapa {mapId.Value} ({reader.GetSafeSByte("X")},{reader.GetSafeSByte("Y")})"
                    : subAreaId.HasValue
                        ? $"Subarea {subAreaId.Value}"
                        : "Sin destino";

                model.Assignments.Add(new MonsterGroupAssignmentAdminModel
                {
                    Id = reader.GetSafeInt32("Id"),
                    MonsterGroupId = reader.GetSafeInt32("MonsterGroupId"),
                    MapId = mapId,
                    SubAreaId = subAreaId,
                    ProbabilityOverride = reader.IsDBNull(reader.GetOrdinal("ProbabilityOverride"))
                        ? null
                        : reader.GetByte(reader.GetOrdinal("ProbabilityOverride")),
                    Disabled = reader.GetSafeBoolean("Disabled"),
                    LastSyncedAt = reader.GetSafeDateTime("LastSyncedAt"),
                    TargetLabel = targetLabel,
                });
            }
        }

        return model;
    }

    public async Task<int> SaveAsync(MonsterGroupEditModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        await _bootstrapService.EnsureSchemaAsync(cancellationToken);

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (model.Id <= 0)
            {
                await using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO admin_monster_groups (Name, Notes)
                    VALUES (@name, @notes);
                    SELECT LAST_INSERT_ID();
                    """;
                insert.Parameters.AddWithValue("@name", model.Name.Trim());
                insert.Parameters.AddWithValue("@notes", model.Notes.Trim());
                model.Id = Convert.ToInt32(await insert.ExecuteScalarAsync(cancellationToken));
            }
            else
            {
                await using var update = connection.CreateCommand();
                update.Transaction = transaction;
                update.CommandText = """
                    UPDATE admin_monster_groups
                    SET Name = @name,
                        Notes = @notes
                    WHERE Id = @id;
                    """;
                update.Parameters.AddWithValue("@id", model.Id);
                update.Parameters.AddWithValue("@name", model.Name.Trim());
                update.Parameters.AddWithValue("@notes", model.Notes.Trim());
                await update.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var deleteEntries = connection.CreateCommand())
            {
                deleteEntries.Transaction = transaction;
                deleteEntries.CommandText = "DELETE FROM admin_monster_group_entries WHERE MonsterGroupId = @groupId;";
                deleteEntries.Parameters.AddWithValue("@groupId", model.Id);
                await deleteEntries.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var entry in model.Entries)
            {
                await using var insertEntry = connection.CreateCommand();
                insertEntry.Transaction = transaction;
                insertEntry.CommandText = """
                    INSERT INTO admin_monster_group_entries
                        (MonsterGroupId, MonsterId, MinGrade, MaxGrade, Probability, Disabled)
                    VALUES
                        (@groupId, @monsterId, @minGrade, @maxGrade, @probability, @disabled);
                    """;
                insertEntry.Parameters.AddWithValue("@groupId", model.Id);
                insertEntry.Parameters.AddWithValue("@monsterId", entry.MonsterId);
                insertEntry.Parameters.AddWithValue("@minGrade", entry.MinGrade);
                insertEntry.Parameters.AddWithValue("@maxGrade", entry.MaxGrade);
                insertEntry.Parameters.AddWithValue("@probability", entry.Probability);
                insertEntry.Parameters.AddWithValue("@disabled", entry.Disabled);
                await insertEntry.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return model.Id;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task SaveAssignmentAsync(MonsterGroupAssignmentAdminModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        await _bootstrapService.EnsureSchemaAsync(cancellationToken);

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = model.Id <= 0
            ? """
              INSERT INTO admin_monster_group_assignments
                  (MonsterGroupId, MapId, SubAreaId, ProbabilityOverride, Disabled, LastSyncedAt)
              VALUES
                  (@groupId, @mapId, @subAreaId, @probabilityOverride, @disabled, NULL);
              """
            : """
              UPDATE admin_monster_group_assignments
              SET MapId = @mapId,
                  SubAreaId = @subAreaId,
                  ProbabilityOverride = @probabilityOverride,
                  Disabled = @disabled
              WHERE Id = @id;
              """;
        if (model.Id > 0)
            command.Parameters.AddWithValue("@id", model.Id);

        command.Parameters.AddWithValue("@groupId", model.MonsterGroupId);
        command.Parameters.Add("@mapId", MySqlDbType.Int32).Value = model.MapId.HasValue ? model.MapId.Value : DBNull.Value;
        command.Parameters.Add("@subAreaId", MySqlDbType.Int16).Value = model.SubAreaId.HasValue ? model.SubAreaId.Value : DBNull.Value;
        command.Parameters.Add("@probabilityOverride", MySqlDbType.UByte).Value = model.ProbabilityOverride.HasValue ? model.ProbabilityOverride.Value : DBNull.Value;
        command.Parameters.AddWithValue("@disabled", model.Disabled);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int groupId, CancellationToken cancellationToken = default)
    {
        await _bootstrapService.EnsureSchemaAsync(cancellationToken);

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM admin_monster_groups WHERE Id = @groupId;";
        command.Parameters.AddWithValue("@groupId", groupId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAssignmentAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        await _bootstrapService.EnsureSchemaAsync(cancellationToken);

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM admin_monster_group_assignments WHERE Id = @assignmentId;";
        command.Parameters.AddWithValue("@assignmentId", assignmentId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<MonsterGroupSyncResult> SyncAssignmentAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        await _bootstrapService.EnsureSchemaAsync(cancellationToken);

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var assignment = await LoadAssignmentAsync(connection, transaction, assignmentId, cancellationToken)
                ?? throw new InvalidOperationException($"No se encontro la asignacion {assignmentId}.");

            var entries = await LoadEntriesAsync(connection, transaction, assignment.MonsterGroupId, cancellationToken);
            var result = new MonsterGroupSyncResult { AssignmentId = assignmentId };

            foreach (var entry in entries)
            {
                await using var existingCommand = connection.CreateCommand();
                existingCommand.Transaction = transaction;
                existingCommand.CommandText = """
                    SELECT Id
                    FROM monsters_spawns
                    WHERE MonsterId = @monsterId
                      AND ((@mapId IS NULL AND MapId IS NULL) OR MapId = @mapId)
                      AND ((@subAreaId IS NULL AND SubAreaId IS NULL) OR SubAreaId = @subAreaId)
                      AND MinGrade = @minGrade
                      AND MaxGrade = @maxGrade
                    LIMIT 1;
                    """;
                existingCommand.Parameters.Add("@mapId", MySqlDbType.Int32).Value = assignment.MapId.HasValue ? assignment.MapId.Value : DBNull.Value;
                existingCommand.Parameters.Add("@subAreaId", MySqlDbType.Int16).Value = assignment.SubAreaId.HasValue ? assignment.SubAreaId.Value : DBNull.Value;
                existingCommand.Parameters.AddWithValue("@monsterId", entry.MonsterId);
                existingCommand.Parameters.AddWithValue("@minGrade", entry.MinGrade);
                existingCommand.Parameters.AddWithValue("@maxGrade", entry.MaxGrade);

                var existingId = await existingCommand.ExecuteScalarAsync(cancellationToken);
                var probability = assignment.ProbabilityOverride ?? entry.Probability;
                var disabled = assignment.Disabled || entry.Disabled;

                if (existingId is null)
                {
                    await using var insertCommand = connection.CreateCommand();
                    insertCommand.Transaction = transaction;
                    insertCommand.CommandText = """
                        INSERT INTO monsters_spawns
                            (SubAreaId, MapId, MonsterId, MinGrade, MaxGrade, Probability, Disabled)
                        VALUES
                            (@subAreaId, @mapId, @monsterId, @minGrade, @maxGrade, @probability, @disabled);
                        """;
                    insertCommand.Parameters.Add("@subAreaId", MySqlDbType.Int16).Value = assignment.SubAreaId.HasValue ? assignment.SubAreaId.Value : DBNull.Value;
                    insertCommand.Parameters.Add("@mapId", MySqlDbType.Int32).Value = assignment.MapId.HasValue ? assignment.MapId.Value : DBNull.Value;
                    insertCommand.Parameters.AddWithValue("@monsterId", entry.MonsterId);
                    insertCommand.Parameters.AddWithValue("@minGrade", entry.MinGrade);
                    insertCommand.Parameters.AddWithValue("@maxGrade", entry.MaxGrade);
                    insertCommand.Parameters.AddWithValue("@probability", probability);
                    insertCommand.Parameters.AddWithValue("@disabled", disabled);
                    await insertCommand.ExecuteNonQueryAsync(cancellationToken);
                    result.UpsertedCount++;
                    result.InsertedCount++;
                }
                else
                {
                    await using var updateCommand = connection.CreateCommand();
                    updateCommand.Transaction = transaction;
                    updateCommand.CommandText = """
                        UPDATE monsters_spawns
                        SET Probability = @probability,
                            Disabled = @disabled
                        WHERE Id = @id;
                        """;
                    updateCommand.Parameters.AddWithValue("@id", Convert.ToInt32(existingId));
                    updateCommand.Parameters.AddWithValue("@probability", probability);
                    updateCommand.Parameters.AddWithValue("@disabled", disabled);
                    await updateCommand.ExecuteNonQueryAsync(cancellationToken);
                    result.UpsertedCount++;
                    result.UpdatedCount++;
                }
            }

            await using (var syncCommand = connection.CreateCommand())
            {
                syncCommand.Transaction = transaction;
                syncCommand.CommandText = """
                    UPDATE admin_monster_group_assignments
                    SET LastSyncedAt = @now
                    WHERE Id = @assignmentId;
                    """;
                syncCommand.Parameters.AddWithValue("@now", DateTime.UtcNow);
                syncCommand.Parameters.AddWithValue("@assignmentId", assignmentId);
                await syncCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static AdminPagedQuery Normalize(AdminPagedQuery query)
    {
        query.Page = query.Page <= 0 ? 1 : query.Page;
        query.PageSize = query.PageSize switch
        {
            <= 0 => 25,
            > 100 => 100,
            _ => query.PageSize,
        };
        return query;
    }

    private async Task ApplyMonsterPresentationAsync(
        MySqlConnection connection,
        IReadOnlyCollection<MonsterGroupEntryAdminModel> entries,
        CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
            return;

        var presentations = await _monsterCatalogService.GetPresentationsAsync(
            connection,
            entries.Select(entry => entry.MonsterId),
            cancellationToken);

        foreach (var entry in entries)
            if (presentations.TryGetValue(entry.MonsterId, out var presentation))
                entry.MonsterLabel = $"{presentation.DisplayName} #{entry.MonsterId}";
    }

    private static async Task<MonsterGroupAssignmentAdminModel?> LoadAssignmentAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int assignmentId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT Id, MonsterGroupId, MapId, SubAreaId, ProbabilityOverride, Disabled
            FROM admin_monster_group_assignments
            WHERE Id = @assignmentId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@assignmentId", assignmentId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new MonsterGroupAssignmentAdminModel
        {
            Id = reader.GetSafeInt32("Id"),
            MonsterGroupId = reader.GetSafeInt32("MonsterGroupId"),
            MapId = reader.IsDBNull(reader.GetOrdinal("MapId")) ? null : reader.GetInt32(reader.GetOrdinal("MapId")),
            SubAreaId = reader.IsDBNull(reader.GetOrdinal("SubAreaId")) ? null : reader.GetInt16(reader.GetOrdinal("SubAreaId")),
            ProbabilityOverride = reader.IsDBNull(reader.GetOrdinal("ProbabilityOverride")) ? null : reader.GetByte(reader.GetOrdinal("ProbabilityOverride")),
            Disabled = reader.GetSafeBoolean("Disabled"),
        };
    }

    private static async Task<List<MonsterGroupEntryAdminModel>> LoadEntriesAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int groupId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT Id, MonsterId, MinGrade, MaxGrade, Probability, Disabled
            FROM admin_monster_group_entries
            WHERE MonsterGroupId = @groupId
            ORDER BY Id;
            """;
        command.Parameters.AddWithValue("@groupId", groupId);

        var items = new List<MonsterGroupEntryAdminModel>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new MonsterGroupEntryAdminModel
            {
                Id = reader.GetSafeInt32("Id"),
                MonsterId = reader.GetSafeInt16("MonsterId"),
                MinGrade = reader.GetSafeSByte("MinGrade", 1),
                MaxGrade = reader.GetSafeSByte("MaxGrade", 1),
                Probability = reader.GetSafeByte("Probability", 5),
                Disabled = reader.GetSafeBoolean("Disabled"),
            });
        }

        return items;
    }
}
