using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.Monsters;

namespace Rollback.Admin.Services;

public sealed class MapSpawnAdminService
{
    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly MonsterCatalogService _monsterCatalogService;

    public MapSpawnAdminService(
        AdminDbConnectionFactory connectionFactory,
        MonsterCatalogService monsterCatalogService)
    {
        _connectionFactory = connectionFactory;
        _monsterCatalogService = monsterCatalogService;
    }

    public async Task<AdminPagedResult<MapSpawnOverview>> GetMapsAsync(
        AdminPagedQuery query,
        bool onlyEmptyMaps = false,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var normalized = Normalize(query);
        var search = normalized.Search.Trim();
        var hasCoordinates = TryParseCoordinates(search, out var coordinateX, out var coordinateY);

        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = """
            SELECT COUNT(*)
            FROM (
                SELECT
                    wm.Id,
                    COALESCE(COUNT(DISTINCT ms.Id), 0) AS DirectSpawnCount,
                    COALESCE(COUNT(DISTINCT sas.Id), 0) AS SubAreaSpawnCount
                FROM world_maps wm
                LEFT JOIN monsters_spawns ms ON ms.MapId = wm.Id
                LEFT JOIN monsters_spawns sas ON sas.SubAreaId = wm.SubAreaId AND sas.MapId IS NULL
                WHERE (@search = ''
                   OR (@hasCoordinates = 1 AND wm.X = @coordX AND wm.Y = @coordY)
                   OR CAST(wm.Id AS CHAR) LIKE @wildSearch
                   OR CAST(wm.SubAreaId AS CHAR) LIKE @wildSearch
                   OR CAST(wm.X AS CHAR) LIKE @wildSearch
                   OR CAST(wm.Y AS CHAR) LIKE @wildSearch)
                GROUP BY wm.Id
                HAVING @onlyEmpty = 0 OR (DirectSpawnCount + SubAreaSpawnCount) = 0
            ) q;
            """;
        countCommand.Parameters.AddWithValue("@search", search);
        countCommand.Parameters.AddWithValue("@hasCoordinates", hasCoordinates ? 1 : 0);
        countCommand.Parameters.AddWithValue("@coordX", coordinateX);
        countCommand.Parameters.AddWithValue("@coordY", coordinateY);
        countCommand.Parameters.AddWithValue("@wildSearch", $"%{search}%");
        countCommand.Parameters.AddWithValue("@onlyEmpty", onlyEmptyMaps ? 1 : 0);
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                wm.Id AS MapId,
                wm.SubAreaId,
                wm.X,
                wm.Y,
                wm.SpawnDisabled,
                COALESCE(COUNT(DISTINCT ms.Id), 0) AS DirectSpawnCount,
                COALESCE(COUNT(DISTINCT sas.Id), 0) AS SubAreaSpawnCount
            FROM world_maps wm
            LEFT JOIN monsters_spawns ms ON ms.MapId = wm.Id
            LEFT JOIN monsters_spawns sas ON sas.SubAreaId = wm.SubAreaId AND sas.MapId IS NULL
            WHERE (@search = ''
               OR (@hasCoordinates = 1 AND wm.X = @coordX AND wm.Y = @coordY)
               OR CAST(wm.Id AS CHAR) LIKE @wildSearch
               OR CAST(wm.SubAreaId AS CHAR) LIKE @wildSearch
               OR CAST(wm.X AS CHAR) LIKE @wildSearch
               OR CAST(wm.Y AS CHAR) LIKE @wildSearch)
            GROUP BY wm.Id, wm.SubAreaId, wm.X, wm.Y, wm.SpawnDisabled
            HAVING @onlyEmpty = 0 OR (DirectSpawnCount + SubAreaSpawnCount) = 0
            ORDER BY wm.X ASC, wm.Y ASC, wm.Id ASC
            LIMIT @offset, @pageSize;
            """;
        command.Parameters.AddWithValue("@search", search);
        command.Parameters.AddWithValue("@hasCoordinates", hasCoordinates ? 1 : 0);
        command.Parameters.AddWithValue("@coordX", coordinateX);
        command.Parameters.AddWithValue("@coordY", coordinateY);
        command.Parameters.AddWithValue("@wildSearch", $"%{search}%");
        command.Parameters.AddWithValue("@onlyEmpty", onlyEmptyMaps ? 1 : 0);
        command.Parameters.AddWithValue("@offset", (normalized.Page - 1) * normalized.PageSize);
        command.Parameters.AddWithValue("@pageSize", normalized.PageSize);

        var items = new List<MapSpawnOverview>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new MapSpawnOverview
            {
                MapId = reader.GetSafeInt32("MapId"),
                SubAreaId = reader.GetSafeInt16("SubAreaId"),
                X = reader.GetSafeSByte("X"),
                Y = reader.GetSafeSByte("Y"),
                SpawnDisabled = reader.GetSafeBoolean("SpawnDisabled"),
                DirectSpawnCount = reader.GetSafeInt32("DirectSpawnCount"),
                SubAreaSpawnCount = reader.GetSafeInt32("SubAreaSpawnCount"),
            });
        }

        return new AdminPagedResult<MapSpawnOverview>(items, totalCount, normalized.Page, normalized.PageSize);
    }

    public async Task<IReadOnlyList<MonsterSpawnAdminModel>> GetSpawnsForMapAsync(int mapId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        short? subAreaId = null;
        await using (var mapCommand = connection.CreateCommand())
        {
            mapCommand.CommandText = "SELECT SubAreaId FROM world_maps WHERE Id = @mapId LIMIT 1;";
            mapCommand.Parameters.AddWithValue("@mapId", mapId);
            var scalar = await mapCommand.ExecuteScalarAsync(cancellationToken);
            if (scalar is not null && scalar is not DBNull)
                subAreaId = Convert.ToInt16(scalar);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                ms.Id,
                ms.MapId,
                ms.SubAreaId,
                ms.MonsterId,
                ms.MinGrade,
                ms.MaxGrade,
                ms.Probability,
                ms.Disabled,
                COALESCE(MIN(mg.Level), 0) AS MinLevel,
                COALESCE(MAX(mg.Level), 0) AS MaxLevel
            FROM monsters_spawns ms
            LEFT JOIN monsters_grades mg ON mg.MonsterId = ms.MonsterId
            WHERE ms.MapId = @mapId
               OR (@subAreaId IS NOT NULL AND ms.MapId IS NULL AND ms.SubAreaId = @subAreaId)
            GROUP BY ms.Id, ms.MapId, ms.SubAreaId, ms.MonsterId, ms.MinGrade, ms.MaxGrade, ms.Probability, ms.Disabled
            ORDER BY ms.MapId IS NULL, MinLevel ASC, MaxLevel ASC, ms.MonsterId ASC, ms.Id ASC;
            """;
        command.Parameters.AddWithValue("@mapId", mapId);
        command.Parameters.Add("@subAreaId", MySqlDbType.Int16).Value = subAreaId.HasValue ? subAreaId.Value : DBNull.Value;

        var items = new List<MonsterSpawnAdminModel>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var monsterId = reader.GetSafeInt16("MonsterId");
            var minLevel = reader.GetSafeInt16("MinLevel");
            var maxLevel = reader.GetSafeInt16("MaxLevel");
            int? rowMapId = reader.IsDBNull(reader.GetOrdinal("MapId")) ? null : reader.GetInt32(reader.GetOrdinal("MapId"));

            items.Add(new MonsterSpawnAdminModel
            {
                Id = reader.GetSafeInt32("Id"),
                MapId = rowMapId,
                SubAreaId = reader.IsDBNull(reader.GetOrdinal("SubAreaId")) ? null : reader.GetInt16(reader.GetOrdinal("SubAreaId")),
                MonsterId = monsterId,
                MonsterLabel = $"Monstruo #{monsterId} · lvl {minLevel}-{maxLevel}",
                MinGrade = reader.GetSafeSByte("MinGrade", 1),
                MaxGrade = reader.GetSafeSByte("MaxGrade", 1),
                Probability = reader.GetSafeByte("Probability", 5),
                Disabled = reader.GetSafeBoolean("Disabled"),
                Source = rowMapId.HasValue ? "mapa" : "subarea",
            });
        }

        await ApplyMonsterPresentationAsync(connection, items, cancellationToken);
        return items;
    }

    public async Task<MapSpawnOverview?> GetMapAsync(int mapId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                wm.Id AS MapId,
                wm.SubAreaId,
                wm.X,
                wm.Y,
                wm.SpawnDisabled,
                COALESCE(COUNT(DISTINCT ms.Id), 0) AS DirectSpawnCount,
                COALESCE(COUNT(DISTINCT sas.Id), 0) AS SubAreaSpawnCount
            FROM world_maps wm
            LEFT JOIN monsters_spawns ms ON ms.MapId = wm.Id
            LEFT JOIN monsters_spawns sas ON sas.SubAreaId = wm.SubAreaId AND sas.MapId IS NULL
            WHERE wm.Id = @mapId
            GROUP BY wm.Id, wm.SubAreaId, wm.X, wm.Y, wm.SpawnDisabled
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@mapId", mapId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new MapSpawnOverview
        {
            MapId = reader.GetSafeInt32("MapId"),
            SubAreaId = reader.GetSafeInt16("SubAreaId"),
            X = reader.GetSafeSByte("X"),
            Y = reader.GetSafeSByte("Y"),
            SpawnDisabled = reader.GetSafeBoolean("SpawnDisabled"),
            DirectSpawnCount = reader.GetSafeInt32("DirectSpawnCount"),
            SubAreaSpawnCount = reader.GetSafeInt32("SubAreaSpawnCount"),
        };
    }

    public async Task SaveSpawnAsync(MonsterSpawnAdminModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        if (model.MapId.HasValue && !model.SubAreaId.HasValue)
        {
            await using var mapLookup = connection.CreateCommand();
            mapLookup.CommandText = "SELECT SubAreaId FROM world_maps WHERE Id = @mapId LIMIT 1;";
            mapLookup.Parameters.AddWithValue("@mapId", model.MapId.Value);
            var scalar = await mapLookup.ExecuteScalarAsync(cancellationToken);
            if (scalar is not null && scalar is not DBNull)
                model.SubAreaId = Convert.ToInt16(scalar);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = model.Id <= 0
            ? """
              INSERT INTO monsters_spawns (SubAreaId, MapId, MonsterId, MinGrade, MaxGrade, Probability, Disabled)
              VALUES (@subAreaId, @mapId, @monsterId, @minGrade, @maxGrade, @probability, @disabled);
              """
            : """
              UPDATE monsters_spawns
              SET SubAreaId = @subAreaId,
                  MapId = @mapId,
                  MonsterId = @monsterId,
                  MinGrade = @minGrade,
                  MaxGrade = @maxGrade,
                  Probability = @probability,
                  Disabled = @disabled
              WHERE Id = @id;
              """;

        if (model.Id > 0)
            command.Parameters.AddWithValue("@id", model.Id);

        command.Parameters.Add("@subAreaId", MySqlDbType.Int16).Value = model.SubAreaId.HasValue ? model.SubAreaId.Value : DBNull.Value;
        command.Parameters.Add("@mapId", MySqlDbType.Int32).Value = model.MapId.HasValue ? model.MapId.Value : DBNull.Value;
        command.Parameters.AddWithValue("@monsterId", model.MonsterId);
        command.Parameters.AddWithValue("@minGrade", model.MinGrade);
        command.Parameters.AddWithValue("@maxGrade", model.MaxGrade);
        command.Parameters.AddWithValue("@probability", model.Probability);
        command.Parameters.AddWithValue("@disabled", model.Disabled);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> MaterializeInheritedSpawnsAsync(int mapId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        short? subAreaId = null;
        await using (var mapLookup = connection.CreateCommand())
        {
            mapLookup.CommandText = "SELECT SubAreaId FROM world_maps WHERE Id = @mapId LIMIT 1;";
            mapLookup.Parameters.AddWithValue("@mapId", mapId);
            var scalar = await mapLookup.ExecuteScalarAsync(cancellationToken);
            if (scalar is not null && scalar is not DBNull)
                subAreaId = Convert.ToInt16(scalar);
        }

        if (!subAreaId.HasValue)
            return 0;

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO monsters_spawns (SubAreaId, MapId, MonsterId, MinGrade, MaxGrade, Probability, Disabled)
            SELECT
                inherited.SubAreaId,
                @mapId,
                inherited.MonsterId,
                inherited.MinGrade,
                inherited.MaxGrade,
                inherited.Probability,
                inherited.Disabled
            FROM monsters_spawns inherited
            LEFT JOIN monsters_spawns direct
                ON direct.MapId = @mapId
               AND direct.MonsterId = inherited.MonsterId
               AND direct.MinGrade = inherited.MinGrade
               AND direct.MaxGrade = inherited.MaxGrade
               AND direct.Probability = inherited.Probability
               AND direct.Disabled = inherited.Disabled
            WHERE inherited.MapId IS NULL
              AND inherited.SubAreaId = @subAreaId
              AND direct.Id IS NULL;
            """;
        command.Parameters.AddWithValue("@mapId", mapId);
        command.Parameters.AddWithValue("@subAreaId", subAreaId.Value);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> DeleteDirectSpawnsAsync(int mapId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM monsters_spawns WHERE MapId = @mapId;";
        command.Parameters.AddWithValue("@mapId", mapId);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SetMapSpawnDisabledAsync(int mapId, bool disabled, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE world_maps
            SET SpawnDisabled = @disabled
            WHERE Id = @mapId;
            """;
        command.Parameters.AddWithValue("@disabled", disabled);
        command.Parameters.AddWithValue("@mapId", mapId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteSpawnAsync(int spawnId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM monsters_spawns WHERE Id = @spawnId;";
        command.Parameters.AddWithValue("@spawnId", spawnId);
        await command.ExecuteNonQueryAsync(cancellationToken);
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
        IReadOnlyCollection<MonsterSpawnAdminModel> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return;

        var presentations = await _monsterCatalogService.GetPresentationsAsync(
            connection,
            items.Select(item => item.MonsterId),
            cancellationToken);

        foreach (var item in items)
            if (presentations.TryGetValue(item.MonsterId, out var presentation))
                item.MonsterLabel = $"{presentation.DisplayName} #{item.MonsterId}";
    }

    private static bool TryParseCoordinates(string search, out sbyte x, out sbyte y)
    {
        x = 0;
        y = 0;

        if (string.IsNullOrWhiteSpace(search))
            return false;

        var normalized = search.Trim().Trim('(', ')');
        var pieces = normalized.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pieces.Length != 2)
            return false;

        return sbyte.TryParse(pieces[0], out x) && sbyte.TryParse(pieces[1], out y);
    }
}
