using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.Monsters;

namespace Rollback.Admin.Services;

public sealed class MonsterAdminService
{
    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly AdminEntityAssetOverrideService _assetOverrideService;
    private readonly MonsterCatalogService _monsterCatalogService;

    public MonsterAdminService(
        AdminDbConnectionFactory connectionFactory,
        AdminEntityAssetOverrideService assetOverrideService,
        MonsterCatalogService monsterCatalogService)
    {
        _connectionFactory = connectionFactory;
        _assetOverrideService = assetOverrideService;
        _monsterCatalogService = monsterCatalogService;
    }

    public async Task<AdminPagedResult<MonsterListItem>> GetPagedAsync(AdminPagedQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var normalizedQuery = Normalize(query);
        var search = normalizedQuery.Search.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var referenceMatchedIds = hasSearch
            ? _monsterCatalogService.SearchClientMonsterIds(search).ToArray()
            : Array.Empty<short>();
        var referenceFilter = BuildReferenceFilter("mt", referenceMatchedIds);

        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = $"""
            SELECT COUNT(*)
            FROM monsters_templates mt
            WHERE @hasSearch = 0
               OR CAST(mt.Id AS CHAR) LIKE @search
               OR CAST(mt.Race AS CHAR) LIKE @search
               OR mt.EntityLookString LIKE @search
               {referenceFilter};
            """;
        countCommand.Parameters.AddWithValue("@hasSearch", hasSearch ? 1 : 0);
        countCommand.Parameters.AddWithValue("@search", $"%{search}%");
        AddReferenceParameters(countCommand, referenceMatchedIds);
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                mt.Id,
                mt.Race,
                mt.EntityLookString,
                COALESCE(MIN(mg.Level), 0) AS MinLevel,
                COALESCE(MAX(mg.Level), 0) AS MaxLevel,
                COALESCE(COUNT(DISTINCT mg.Grade), 0) AS GradeCount,
                COALESCE(COUNT(DISTINCT ms.Id), 0) AS SpawnCount,
                COALESCE(COUNT(DISTINCT mrs.Id), 0) AS RareSpawnCount,
                COALESCE(COUNT(DISTINCT msp.SpellId), 0) AS SpellCount,
                COALESCE(COUNT(DISTINCT md.DropId), 0) AS DropCount
            FROM monsters_templates mt
            LEFT JOIN monsters_grades mg ON mg.MonsterId = mt.Id
            LEFT JOIN monsters_spawns ms ON ms.MonsterId = mt.Id
            LEFT JOIN monsters_rare_spawns mrs ON mrs.MonsterId = mt.Id
            LEFT JOIN monsters_spells msp ON msp.MonsterId = mt.Id
            LEFT JOIN monsters_drops md ON md.MonsterId = mt.Id
            WHERE @hasSearch = 0
               OR CAST(mt.Id AS CHAR) LIKE @search
               OR CAST(mt.Race AS CHAR) LIKE @search
               OR mt.EntityLookString LIKE @search
               {referenceFilter}
            GROUP BY mt.Id, mt.Race, mt.EntityLookString
            ORDER BY {ResolveSort(normalizedQuery)}
            LIMIT @offset, @pageSize;
            """;
        command.Parameters.AddWithValue("@hasSearch", hasSearch ? 1 : 0);
        command.Parameters.AddWithValue("@search", $"%{search}%");
        command.Parameters.AddWithValue("@offset", (normalizedQuery.Page - 1) * normalizedQuery.PageSize);
        command.Parameters.AddWithValue("@pageSize", normalizedQuery.PageSize);
        AddReferenceParameters(command, referenceMatchedIds);

        var items = new List<MonsterListItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new MonsterListItem
            {
                Id = reader.GetSafeInt16("Id"),
                Race = reader.GetSafeByte("Race"),
                EntityLookString = reader.GetSafeString("EntityLookString"),
                MinLevel = reader.GetSafeInt16("MinLevel"),
                MaxLevel = reader.GetSafeInt16("MaxLevel"),
                GradeCount = reader.GetSafeInt32("GradeCount"),
                SpawnCount = reader.GetSafeInt32("SpawnCount"),
                RareSpawnCount = reader.GetSafeInt32("RareSpawnCount"),
                SpellCount = reader.GetSafeInt32("SpellCount"),
                DropCount = reader.GetSafeInt32("DropCount"),
            });
        }

        await ApplyPresentationAsync(connection, items, cancellationToken);

        if (items.Count > 0)
        {
            var overrides = await AdminEntityAssetOverrideService.GetManyAsync(
                connection,
                AdminEntityType.Monster,
                items.Select(x => (int)x.Id).ToArray(),
                AdminEntityAssetOverrideService.PreviewPngKind,
                cancellationToken);

            foreach (var item in items)
            {
                if (overrides.TryGetValue(item.Id, out var assetOverride))
                    item.ManualPreviewImageUrl = BuildManualAssetUrl(assetOverride.RelativePath);
            }
        }

        return new AdminPagedResult<MonsterListItem>(items, totalCount, normalizedQuery.Page, normalizedQuery.PageSize);
    }

    public async Task<MonsterEditModel?> GetByIdAsync(short monsterId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var monster = new MonsterEditModel();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT Id, Race, EntityLookString
                FROM monsters_templates
                WHERE Id = @monsterId
                LIMIT 1;
                """;
            command.Parameters.AddWithValue("@monsterId", monsterId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            monster.Id = reader.GetSafeInt16("Id");
            monster.Race = reader.GetSafeByte("Race");
            monster.EntityLookString = reader.GetSafeString("EntityLookString");
        }

        await using (var gradesCommand = connection.CreateCommand())
        {
            gradesCommand.CommandText = """
                SELECT
                    Grade,
                    Level,
                    Health,
                    AP,
                    MP,
                    APDodge,
                    MPDodge,
                    EarthResistance,
                    AirResistance,
                    FireResistance,
                    WaterResistance,
                    NeutralResistance,
                    Wisdom,
                    Strength,
                    Intelligence,
                    Chance,
                    Agility,
                    XP,
                    MinKamas,
                    MaxKamas
                FROM monsters_grades
                WHERE MonsterId = @monsterId
                ORDER BY Grade;
                """;
            gradesCommand.Parameters.AddWithValue("@monsterId", monsterId);

            await using var gradesReader = await gradesCommand.ExecuteReaderAsync(cancellationToken);
            while (await gradesReader.ReadAsync(cancellationToken))
            {
                monster.Grades.Add(new MonsterGradeAdminModel
                {
                    Grade = gradesReader.GetSafeSByte("Grade", 1),
                    Level = gradesReader.GetSafeInt16("Level", 1),
                    Health = gradesReader.GetSafeInt32("Health", 1),
                    AP = gradesReader.GetSafeInt16("AP", 6),
                    MP = gradesReader.GetSafeInt16("MP", 3),
                    APDodge = gradesReader.GetSafeInt16("APDodge"),
                    MPDodge = gradesReader.GetSafeInt16("MPDodge"),
                    EarthResistance = gradesReader.GetSafeInt16("EarthResistance"),
                    AirResistance = gradesReader.GetSafeInt16("AirResistance"),
                    FireResistance = gradesReader.GetSafeInt16("FireResistance"),
                    WaterResistance = gradesReader.GetSafeInt16("WaterResistance"),
                    NeutralResistance = gradesReader.GetSafeInt16("NeutralResistance"),
                    Wisdom = gradesReader.GetSafeInt16("Wisdom"),
                    Strength = gradesReader.GetSafeInt16("Strength"),
                    Intelligence = gradesReader.GetSafeInt16("Intelligence"),
                    Chance = gradesReader.GetSafeInt16("Chance"),
                    Agility = gradesReader.GetSafeInt16("Agility"),
                    Experience = gradesReader.GetSafeInt64("XP"),
                    MinKamas = gradesReader.GetSafeInt32("MinKamas"),
                    MaxKamas = gradesReader.GetSafeInt32("MaxKamas"),
                });
            }
        }

        var assetOverride = await AdminEntityAssetOverrideService.GetAsync(
            connection,
            AdminEntityType.Monster,
            monsterId,
            AdminEntityAssetOverrideService.PreviewPngKind,
            cancellationToken);

        if (assetOverride is not null)
        {
            monster.ManualAssetRelativePath = assetOverride.RelativePath;
            monster.ManualImageUrl = BuildManualAssetUrl(assetOverride.RelativePath);
        }

        var textOverride = await AdminEntityTextOverrideService.GetAsync(
            connection,
            AdminEntityType.Monster,
            monsterId,
            cancellationToken);
        if (textOverride is not null)
        {
            monster.DisplayNameOverride = textOverride.DisplayName;
            monster.DescriptionOverride = textOverride.Description;
        }

        var presentations = await _monsterCatalogService.GetPresentationsAsync(
            connection,
            new[] { monsterId },
            cancellationToken);
        ApplyPresentation(monster, presentations.GetValueOrDefault(monsterId) ?? MonsterCatalogService.Fallback(monsterId));

        return monster;
    }

    public async Task SaveAsync(MonsterEditModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var upsertMonster = connection.CreateCommand())
            {
                upsertMonster.Transaction = transaction;
                upsertMonster.CommandText = """
                    INSERT INTO monsters_templates (Id, EntityLookString, Race)
                    VALUES (@id, @entityLookString, @race)
                    ON DUPLICATE KEY UPDATE
                        EntityLookString = VALUES(EntityLookString),
                        Race = VALUES(Race);
                    """;
                upsertMonster.Parameters.AddWithValue("@id", model.Id);
                upsertMonster.Parameters.AddWithValue("@entityLookString", model.EntityLookString.Trim());
                upsertMonster.Parameters.AddWithValue("@race", model.Race);
                await upsertMonster.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var deleteGrades = connection.CreateCommand())
            {
                deleteGrades.Transaction = transaction;
                deleteGrades.CommandText = "DELETE FROM monsters_grades WHERE MonsterId = @monsterId;";
                deleteGrades.Parameters.AddWithValue("@monsterId", model.Id);
                await deleteGrades.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var grade in model.Grades.OrderBy(x => x.Grade))
            {
                await using var insertGrade = connection.CreateCommand();
                insertGrade.Transaction = transaction;
                insertGrade.CommandText = """
                    INSERT INTO monsters_grades
                    (
                        MonsterId,
                        Grade,
                        Level,
                        Health,
                        AP,
                        MP,
                        APDodge,
                        MPDodge,
                        EarthResistance,
                        AirResistance,
                        FireResistance,
                        WaterResistance,
                        NeutralResistance,
                        Wisdom,
                        Strength,
                        Intelligence,
                        Chance,
                        Agility,
                        XP,
                        MinKamas,
                        MaxKamas
                    )
                    VALUES
                    (
                        @monsterId,
                        @grade,
                        @level,
                        @health,
                        @ap,
                        @mp,
                        @apDodge,
                        @mpDodge,
                        @earthResistance,
                        @airResistance,
                        @fireResistance,
                        @waterResistance,
                        @neutralResistance,
                        @wisdom,
                        @strength,
                        @intelligence,
                        @chance,
                        @agility,
                        @experience,
                        @minKamas,
                        @maxKamas
                    );
                    """;
                insertGrade.Parameters.AddWithValue("@monsterId", model.Id);
                insertGrade.Parameters.AddWithValue("@grade", grade.Grade);
                insertGrade.Parameters.AddWithValue("@level", grade.Level);
                insertGrade.Parameters.AddWithValue("@health", grade.Health);
                insertGrade.Parameters.AddWithValue("@ap", grade.AP);
                insertGrade.Parameters.AddWithValue("@mp", grade.MP);
                insertGrade.Parameters.AddWithValue("@apDodge", grade.APDodge);
                insertGrade.Parameters.AddWithValue("@mpDodge", grade.MPDodge);
                insertGrade.Parameters.AddWithValue("@earthResistance", grade.EarthResistance);
                insertGrade.Parameters.AddWithValue("@airResistance", grade.AirResistance);
                insertGrade.Parameters.AddWithValue("@fireResistance", grade.FireResistance);
                insertGrade.Parameters.AddWithValue("@waterResistance", grade.WaterResistance);
                insertGrade.Parameters.AddWithValue("@neutralResistance", grade.NeutralResistance);
                insertGrade.Parameters.AddWithValue("@wisdom", grade.Wisdom);
                insertGrade.Parameters.AddWithValue("@strength", grade.Strength);
                insertGrade.Parameters.AddWithValue("@intelligence", grade.Intelligence);
                insertGrade.Parameters.AddWithValue("@chance", grade.Chance);
                insertGrade.Parameters.AddWithValue("@agility", grade.Agility);
                insertGrade.Parameters.AddWithValue("@experience", grade.Experience);
                insertGrade.Parameters.AddWithValue("@minKamas", grade.MinKamas);
                insertGrade.Parameters.AddWithValue("@maxKamas", grade.MaxKamas);
                await insertGrade.ExecuteNonQueryAsync(cancellationToken);
            }

            await AdminEntityTextOverrideService.SaveAsync(
                connection,
                AdminEntityType.Monster,
                model.Id,
                model.DisplayNameOverride,
                model.DescriptionOverride,
                transaction,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteAsync(short monsterId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var deletions = new[]
            {
                "DELETE FROM monsters_drops WHERE MonsterId = @monsterId;",
                "DELETE FROM monsters_spells WHERE MonsterId = @monsterId;",
                "DELETE FROM monsters_rare_spawns WHERE MonsterId = @monsterId;",
                "DELETE FROM monsters_spawns WHERE MonsterId = @monsterId;",
                "DELETE FROM monsters_grades WHERE MonsterId = @monsterId;",
                "DELETE FROM monsters_templates WHERE Id = @monsterId;",
                "DELETE FROM admin_monster_group_entries WHERE MonsterId = @monsterId;",
                "DELETE FROM admin_entity_asset_overrides WHERE EntityType = 'Monster' AND EntityId = @monsterId;",
                "DELETE FROM admin_entity_text_overrides WHERE EntityType = 'Monster' AND EntityId = @monsterId;",
            };

            foreach (var sql in deletions)
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@monsterId", monsterId);
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

    public async Task<IReadOnlyList<AdminLookupOption>> GetLookupAsync(string search, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var normalizedSearch = search.Trim();
        var referenceMatchedIds = !string.IsNullOrWhiteSpace(normalizedSearch)
            ? _monsterCatalogService.SearchClientMonsterIds(normalizedSearch).ToArray()
            : Array.Empty<short>();
        var referenceFilter = BuildReferenceFilter("mt", referenceMatchedIds);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                mt.Id,
                mt.Race,
                COALESCE(MIN(mg.Level), 0) AS MinLevel,
                COALESCE(MAX(mg.Level), 0) AS MaxLevel
            FROM monsters_templates mt
            LEFT JOIN monsters_grades mg ON mg.MonsterId = mt.Id
            WHERE @search = ''
               OR CAST(mt.Id AS CHAR) LIKE @wildSearch
               OR mt.EntityLookString LIKE @wildSearch
               {referenceFilter}
            GROUP BY mt.Id, mt.Race
            ORDER BY mt.Id
            LIMIT 50;
            """;
        command.Parameters.AddWithValue("@search", normalizedSearch);
        command.Parameters.AddWithValue("@wildSearch", $"%{normalizedSearch}%");
        AddReferenceParameters(command, referenceMatchedIds);

        var items = new List<AdminLookupOption>();
        var rows = new List<(short Id, byte Race, short MinLevel, short MaxLevel)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetSafeInt16("Id");
            var race = reader.GetSafeByte("Race");
            var minLevel = reader.GetSafeInt16("MinLevel");
            var maxLevel = reader.GetSafeInt16("MaxLevel");

            rows.Add((id, race, minLevel, maxLevel));
            items.Add(new AdminLookupOption(id.ToString(), $"Monstruo #{id} · lvl {minLevel}-{maxLevel}"));
        }

        var presentations = await _monsterCatalogService.GetPresentationsAsync(
            connection,
            rows.Select(row => row.Id),
            cancellationToken);

        items = rows
            .Select(row =>
            {
                var presentation = presentations.GetValueOrDefault(row.Id) ?? MonsterCatalogService.Fallback(row.Id);
                return new AdminLookupOption(
                    row.Id.ToString(),
                    presentation.BuildLabel(row.MinLevel, row.MaxLevel, row.Race),
                    BuildMonsterHint(presentation));
            })
            .ToList();

        return items;
    }

    public async Task SaveManualAssetAsync(short monsterId, string? relativePath, CancellationToken cancellationToken = default)
    {
        await _assetOverrideService.SaveAsync(
            AdminEntityType.Monster,
            monsterId,
            relativePath,
            AdminEntityAssetOverrideService.PreviewPngKind,
            cancellationToken);
    }

    public async Task ClearManualAssetAsync(short monsterId, CancellationToken cancellationToken = default)
    {
        await _assetOverrideService.DeleteAsync(
            AdminEntityType.Monster,
            monsterId,
            AdminEntityAssetOverrideService.PreviewPngKind,
            cancellationToken);
    }

    private static string BuildManualAssetUrl(string relativePath) =>
        string.IsNullOrWhiteSpace(relativePath)
            ? string.Empty
            : $"/admin-assets/{relativePath.Trim().Replace('\\', '/')}";

    private async Task ApplyPresentationAsync(
        MySqlConnection connection,
        IReadOnlyCollection<MonsterListItem> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return;

        var presentations = await _monsterCatalogService.GetPresentationsAsync(
            connection,
            items.Select(item => item.Id),
            cancellationToken);

        foreach (var item in items)
            ApplyPresentation(item, presentations.GetValueOrDefault(item.Id) ?? MonsterCatalogService.Fallback(item.Id));
    }

    private static void ApplyPresentation(MonsterListItem item, MonsterCatalogPresentation presentation)
    {
        item.DisplayName = presentation.DisplayName;
        item.NameSource = presentation.NameSource;
        item.ClientDisplayName = presentation.ClientDisplayName;
        item.ClientNameId = presentation.ClientNameId;
        item.ClientGfxId = presentation.ClientGfxId;
        item.HasNameMismatch = presentation.HasNameMismatch;
    }

    private static void ApplyPresentation(MonsterEditModel model, MonsterCatalogPresentation presentation)
    {
        model.ResolvedDisplayName = presentation.DisplayName;
        model.NameSource = presentation.NameSource;
        model.ClientDisplayName = presentation.ClientDisplayName;
        model.ClientNameId = presentation.ClientNameId;
        model.ClientGfxId = presentation.ClientGfxId;
        model.HasNameMismatch = presentation.HasNameMismatch;
    }

    private static string BuildMonsterHint(MonsterCatalogPresentation presentation)
    {
        var bits = new List<string> { $"source: {presentation.NameSource}" };
        if (presentation.ClientNameId.HasValue)
            bits.Add($"NameId {presentation.ClientNameId.Value}");

        if (presentation.ClientGfxId.HasValue)
            bits.Add($"GfxId {presentation.ClientGfxId.Value}");

        if (presentation.HasNameMismatch)
            bits.Add($"cliente dice \"{presentation.ClientDisplayName}\"");

        return string.Join(" - ", bits);
    }

    private static string BuildReferenceFilter(string tableAlias, IReadOnlyList<short> ids) =>
        ids.Count == 0
            ? string.Empty
            : $"OR {tableAlias}.Id IN ({string.Join(",", ids.Select((_, index) => $"@referenceId{index}"))})";

    private static void AddReferenceParameters(MySqlCommand command, IReadOnlyList<short> ids)
    {
        for (var index = 0; index < ids.Count; index++)
            command.Parameters.AddWithValue($"@referenceId{index}", ids[index]);
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

    private static string ResolveSort(AdminPagedQuery query)
    {
        var direction = query.Descending ? "DESC" : "ASC";
        return query.SortBy?.ToLowerInvariant() switch
        {
            "minlevel" => $"MinLevel {direction}, mt.Id ASC",
            "maxlevel" => $"MaxLevel {direction}, mt.Id ASC",
            "race" => $"mt.Race {direction}, mt.Id ASC",
            "spawns" => $"SpawnCount {direction}, mt.Id ASC",
            _ => $"mt.Id {direction}",
        };
    }
}
