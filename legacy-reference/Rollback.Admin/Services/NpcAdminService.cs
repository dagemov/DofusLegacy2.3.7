using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.Npcs;

namespace Rollback.Admin.Services;

public sealed class NpcAdminService
{
    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly NpcClientPublishService _npcClientPublishService;

    public NpcAdminService(
        AdminDbConnectionFactory connectionFactory,
        NpcClientPublishService npcClientPublishService)
    {
        _connectionFactory = connectionFactory;
        _npcClientPublishService = npcClientPublishService;
    }

    public async Task<AdminPagedResult<NpcListItem>> GetPagedAsync(AdminPagedQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var normalized = Normalize(query);
        var search = normalized.Search.Trim();

        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = """
            SELECT COUNT(*)
            FROM npcs_templates nt
            WHERE @search = ''
               OR CAST(nt.Id AS CHAR) LIKE @wildSearch
               OR nt.Name LIKE @wildSearch
               OR nt.EntityLookString LIKE @wildSearch;
            """;
        countCommand.Parameters.AddWithValue("@search", search);
        countCommand.Parameters.AddWithValue("@wildSearch", $"%{search}%");
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                nt.Id,
                nt.Name,
                nt.Gender,
                nt.EntityLookString,
                COALESCE(COUNT(DISTINCT ns.Id), 0) AS SpawnCount,
                MIN(ns.MapId) AS MapId,
                MIN(ns.CellId) AS CellId,
                GROUP_CONCAT(DISTINCT na.Action ORDER BY na.Priority, na.Id SEPARATOR ', ') AS ActionLabel
            FROM npcs_templates nt
            LEFT JOIN npcs_spawns ns ON ns.NpcId = nt.Id
            LEFT JOIN npcs_actions na ON na.NpcId = nt.Id
            WHERE @search = ''
               OR CAST(nt.Id AS CHAR) LIKE @wildSearch
               OR nt.Name LIKE @wildSearch
               OR nt.EntityLookString LIKE @wildSearch
            GROUP BY nt.Id, nt.Name, nt.Gender, nt.EntityLookString
            ORDER BY nt.Id
            LIMIT @offset, @pageSize;
            """;
        command.Parameters.AddWithValue("@search", search);
        command.Parameters.AddWithValue("@wildSearch", $"%{search}%");
        command.Parameters.AddWithValue("@offset", (normalized.Page - 1) * normalized.PageSize);
        command.Parameters.AddWithValue("@pageSize", normalized.PageSize);

        var items = new List<NpcListItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new NpcListItem
            {
                Id = reader.GetSafeInt16("Id"),
                Name = reader.GetSafeString("Name"),
                Gender = reader.GetSafeBoolean("Gender"),
                EntityLookString = reader.GetSafeString("EntityLookString"),
                SpawnCount = reader.GetSafeInt32("SpawnCount"),
                MapId = reader.IsDBNull(reader.GetOrdinal("MapId")) ? null : reader.GetInt32(reader.GetOrdinal("MapId")),
                CellId = reader.IsDBNull(reader.GetOrdinal("CellId")) ? null : reader.GetInt16(reader.GetOrdinal("CellId")),
                ActionLabel = reader.GetSafeString("ActionLabel"),
            });
        }

        return new AdminPagedResult<NpcListItem>(items, totalCount, normalized.Page, normalized.PageSize);
    }

    public async Task<NpcEditModel?> GetByIdAsync(short npcId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var model = new NpcEditModel();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT Id, Name, Gender, EntityLookString
                FROM npcs_templates
                WHERE Id = @npcId
                LIMIT 1;
                """;
            command.Parameters.AddWithValue("@npcId", npcId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            model.Id = reader.GetSafeInt16("Id");
            model.Name = reader.GetSafeString("Name");
            model.Gender = reader.GetSafeBoolean("Gender");
            model.EntityLookString = reader.GetSafeString("EntityLookString");
        }

        await using (var spawnCommand = connection.CreateCommand())
        {
            spawnCommand.CommandText = """
                SELECT Id, MapId, CellId, Direction
                FROM npcs_spawns
                WHERE NpcId = @npcId
                ORDER BY Id
                LIMIT 1;
                """;
            spawnCommand.Parameters.AddWithValue("@npcId", npcId);

            await using var reader = await spawnCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.SpawnId = reader.GetSafeInt32("Id");
                model.MapId = reader.GetSafeInt32("MapId");
                model.CellId = reader.GetSafeInt16("CellId");
                model.Direction = reader.GetSafeByte("Direction");
            }
        }

        await using (var actionCommand = connection.CreateCommand())
        {
            actionCommand.CommandText = """
                SELECT Id, Action, StringCriterion, Priority, ParametersCSV
                FROM npcs_actions
                WHERE NpcId = @npcId
                ORDER BY Priority, Id
                LIMIT 1;
                """;
            actionCommand.Parameters.AddWithValue("@npcId", npcId);

            await using var reader = await actionCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.PrimaryActionId = reader.GetSafeInt32("Id");
                model.PrimaryActionAlias = reader.GetSafeString("Action");
                model.PrimaryActionParameters = reader.GetSafeString("ParametersCSV");
                model.StringCriterion = reader.GetSafeString("StringCriterion");
                model.Priority = reader.GetSafeInt16("Priority");
                ApplyActionMode(model);
            }
        }

        return model;
    }

    public async Task<short> GetNextAvailableIdAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(Id), 0) + 1 FROM npcs_templates;";
        return Convert.ToInt16(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task SaveAsync(NpcEditModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var existingMessagesCsv = string.Empty;
            var existingRepliesCsv = string.Empty;
            await using (var existingTemplate = connection.CreateCommand())
            {
                existingTemplate.Transaction = transaction;
                existingTemplate.CommandText = """
                    SELECT MessagesCSV, RepliesCSV
                    FROM npcs_templates
                    WHERE Id = @npcId
                    LIMIT 1;
                    """;
                existingTemplate.Parameters.AddWithValue("@npcId", model.Id);
                await using var reader = await existingTemplate.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    existingMessagesCsv = reader.GetSafeString("MessagesCSV");
                    existingRepliesCsv = reader.GetSafeString("RepliesCSV");
                }
            }

            var actionAlias = ResolveActionAlias(model);
            var actionParameters = ResolveActionParameters(model);
            var actionsCsv = ResolveActionsCsv(actionAlias);

            await using (var templateCommand = connection.CreateCommand())
            {
                templateCommand.Transaction = transaction;
                templateCommand.CommandText = """
                    INSERT INTO npcs_templates (Id, Name, Gender, EntityLookString, MessagesCSV, RepliesCSV, ActionsCSV)
                    VALUES (@id, @name, @gender, @entityLookString, @messagesCsv, @repliesCsv, @actionsCsv)
                    ON DUPLICATE KEY UPDATE
                        Name = VALUES(Name),
                        Gender = VALUES(Gender),
                        EntityLookString = VALUES(EntityLookString),
                        ActionsCSV = VALUES(ActionsCSV);
                    """;
                templateCommand.Parameters.AddWithValue("@id", model.Id);
                templateCommand.Parameters.AddWithValue("@name", model.Name.Trim());
                templateCommand.Parameters.AddWithValue("@gender", model.Gender);
                templateCommand.Parameters.AddWithValue("@entityLookString", model.EntityLookString.Trim());
                templateCommand.Parameters.AddWithValue("@messagesCsv", existingMessagesCsv);
                templateCommand.Parameters.AddWithValue("@repliesCsv", existingRepliesCsv);
                templateCommand.Parameters.AddWithValue("@actionsCsv", actionsCsv);
                await templateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            if (model.PrimaryActionId is > 0)
            {
                await using var updateAction = connection.CreateCommand();
                updateAction.Transaction = transaction;
                updateAction.CommandText = """
                    UPDATE npcs_actions
                    SET NpcId = @npcId,
                        Action = @action,
                        StringCriterion = @stringCriterion,
                        Priority = @priority,
                        ParametersCSV = @parametersCsv
                    WHERE Id = @id;
                    """;
                updateAction.Parameters.AddWithValue("@id", model.PrimaryActionId.Value);
                updateAction.Parameters.AddWithValue("@npcId", model.Id);
                updateAction.Parameters.AddWithValue("@action", actionAlias);
                updateAction.Parameters.AddWithValue("@stringCriterion", NormalizeNullable(model.StringCriterion));
                updateAction.Parameters.AddWithValue("@priority", model.Priority);
                updateAction.Parameters.AddWithValue("@parametersCsv", actionParameters);
                await updateAction.ExecuteNonQueryAsync(cancellationToken);
            }
            else
            {
                await using var insertAction = connection.CreateCommand();
                insertAction.Transaction = transaction;
                insertAction.CommandText = """
                    INSERT INTO npcs_actions (Id, NpcId, Action, StringCriterion, Priority, ParametersCSV)
                    VALUES (@id, @npcId, @action, @stringCriterion, @priority, @parametersCsv);
                    """;
                insertAction.Parameters.AddWithValue("@id", await GetNextTableIdAsync(connection, transaction, "npcs_actions", cancellationToken));
                insertAction.Parameters.AddWithValue("@npcId", model.Id);
                insertAction.Parameters.AddWithValue("@action", actionAlias);
                insertAction.Parameters.AddWithValue("@stringCriterion", NormalizeNullable(model.StringCriterion));
                insertAction.Parameters.AddWithValue("@priority", model.Priority);
                insertAction.Parameters.AddWithValue("@parametersCsv", actionParameters);
                await insertAction.ExecuteNonQueryAsync(cancellationToken);
            }

            if (model.SpawnId is > 0)
            {
                await using var updateSpawn = connection.CreateCommand();
                updateSpawn.Transaction = transaction;
                updateSpawn.CommandText = """
                    UPDATE npcs_spawns
                    SET NpcId = @npcId,
                        MapId = @mapId,
                        CellId = @cellId,
                        Direction = @direction,
                        StringCriterion = NULL
                    WHERE Id = @id;
                    """;
                updateSpawn.Parameters.AddWithValue("@id", model.SpawnId.Value);
                updateSpawn.Parameters.AddWithValue("@npcId", model.Id);
                updateSpawn.Parameters.AddWithValue("@mapId", model.MapId);
                updateSpawn.Parameters.AddWithValue("@cellId", model.CellId);
                updateSpawn.Parameters.AddWithValue("@direction", model.Direction);
                await updateSpawn.ExecuteNonQueryAsync(cancellationToken);
            }
            else
            {
                await using var insertSpawn = connection.CreateCommand();
                insertSpawn.Transaction = transaction;
                insertSpawn.CommandText = """
                    INSERT INTO npcs_spawns (Id, NpcId, MapId, CellId, Direction, StringCriterion)
                    VALUES (@id, @npcId, @mapId, @cellId, @direction, NULL);
                    """;
                insertSpawn.Parameters.AddWithValue("@id", await GetNextTableIdAsync(connection, transaction, "npcs_spawns", cancellationToken));
                insertSpawn.Parameters.AddWithValue("@npcId", model.Id);
                insertSpawn.Parameters.AddWithValue("@mapId", model.MapId);
                insertSpawn.Parameters.AddWithValue("@cellId", model.CellId);
                insertSpawn.Parameters.AddWithValue("@direction", model.Direction);
                await insertSpawn.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        await _npcClientPublishService.PublishAsync(model.Id, cancellationToken);
    }

    public async Task DeleteAsync(short npcId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var deletions = new[]
            {
                "DELETE FROM npcs_spawns WHERE NpcId = @npcId;",
                "DELETE FROM npcs_actions WHERE NpcId = @npcId;",
                "DELETE FROM npcs_templates WHERE Id = @npcId;",
            };

            foreach (var sql in deletions)
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@npcId", npcId);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void ApplyActionMode(NpcEditModel model)
    {
        if (model.PrimaryActionAlias.Equals("Shop", StringComparison.OrdinalIgnoreCase))
        {
            model.PrimaryActionMode = NpcPrimaryActionMode.Shop;
            model.ShopCanSell = bool.TryParse(model.PrimaryActionParameters, out var canSell) && canSell;
            return;
        }

        if (model.PrimaryActionAlias.Equals("Talk", StringComparison.OrdinalIgnoreCase))
        {
            model.PrimaryActionMode = NpcPrimaryActionMode.Talk;
            model.TalkMessageId = short.TryParse(model.PrimaryActionParameters, out var messageId)
                ? messageId
                : null;
            return;
        }

        model.PrimaryActionMode = NpcPrimaryActionMode.Raw;
    }

    private static string ResolveActionAlias(NpcEditModel model) =>
        model.PrimaryActionMode switch
        {
            NpcPrimaryActionMode.Shop => "Shop",
            NpcPrimaryActionMode.Talk => "Talk",
            _ => string.IsNullOrWhiteSpace(model.PrimaryActionAlias) ? "Shop" : model.PrimaryActionAlias.Trim(),
        };

    private static string ResolveActionParameters(NpcEditModel model) =>
        model.PrimaryActionMode switch
        {
            NpcPrimaryActionMode.Shop => model.ShopCanSell ? "true" : "false",
            NpcPrimaryActionMode.Talk => model.TalkMessageId?.ToString() ?? string.Empty,
            _ => model.PrimaryActionParameters.Trim(),
        };

    private static string ResolveActionsCsv(string actionAlias) =>
        actionAlias.Equals("Shop", StringComparison.OrdinalIgnoreCase)
            ? "1"
            : "3";

    private static object NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static async Task<int> GetNextTableIdAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT COALESCE(MAX(Id), 0) + 1 FROM {tableName};";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
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
}
