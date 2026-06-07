using Dapper;
using MySqlConnector;
using RollblackLegacy.Admin.Application.Abstractions.Spells;
using RollblackLegacy.Admin.Application.Exceptions;
using RollblackLegacy.Admin.Application.Models.Spells;
using RollblackLegacy.Admin.Contracts.Spells;
using RollblackLegacy.Admin.Infrastructure.Data;
using RollblackLegacy.Admin.Infrastructure.Spells;

namespace RollblackLegacy.Admin.Infrastructure.Services.Spells;

public sealed class SpellsAdminReadRepository : ISpellsAdminReadRepository, ISpellsAdminWriteRepository
{
    private const int SpellAdminEntityType = 4;

    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly ReferenceSpellCatalogReader _referenceCatalogReader;

    public SpellsAdminReadRepository(
        AdminDbConnectionFactory connectionFactory,
        ReferenceSpellCatalogReader referenceCatalogReader)
    {
        _connectionFactory = connectionFactory;
        _referenceCatalogReader = referenceCatalogReader;
    }

    public async Task<AdminPagedSpellsReadModel> SearchAsync(
        SpellCatalogSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var schema = await DetectSchemaAsync(connection, cancellationToken);
        var referenceSnapshot = _referenceCatalogReader.GetSnapshot();
        var runtimeHeaders = await LoadRuntimeHeadersAsync(connection, schema, cancellationToken);
        var breedIdsBySpellId = await LoadBreedIdsAsync(connection, schema, cancellationToken);
        var overridesBySpellId = schema.HasTextOverrides
            ? await LoadTextOverridesAsync(connection, cancellationToken)
            : new Dictionary<short, SpellTextOverride>();

        var runtimeById = runtimeHeaders.ToDictionary(row => row.SpellId);
        var candidateSpellIds = runtimeById.Keys
            .Concat(referenceSnapshot.ClassicSpellIds)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        var filtered = new List<AdminSpellCatalogReadModel>(candidateSpellIds.Length);
        foreach (var spellId in candidateSpellIds)
        {
            runtimeById.TryGetValue(spellId, out var runtime);
            referenceSnapshot.SpellsById.TryGetValue(spellId, out var reference);
            overridesBySpellId.TryGetValue(spellId, out var textOverride);

            if (runtime is null && reference is null)
            {
                continue;
            }

            var breeds = ResolveBreeds(spellId, breedIdsBySpellId, reference)
                .Select(MapBreed)
                .ToArray();
            var typeId = runtime?.TypeId ?? reference?.TypeId;
            var typeLabel = ResolveTypeLabel(typeId, runtime?.RuntimeTypeLabel, reference, referenceSnapshot);
            var item = new AdminSpellCatalogReadModel(
                spellId,
                FirstNonEmpty(textOverride?.DisplayName, runtime?.Name, reference?.Name),
                FirstNonEmpty(textOverride?.Description, runtime?.Description, reference?.Description),
                typeId,
                typeLabel,
                ResolveIconId(runtime, reference),
                breeds,
                runtime?.LevelCount ?? reference?.LevelCount ?? 0,
                RuntimeAvailable: runtime is not null,
                ReferenceAvailable: reference is not null);

            if (!MatchesFilters(item, request))
            {
                continue;
            }

            filtered.Add(item);
        }

        var page = NormalizePage(request.Page);
        var pageSize = NormalizePageSize(request.PageSize);
        var totalCount = filtered.Count;
        var items = filtered
            .OrderBy(item => item.SpellId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return new AdminPagedSpellsReadModel(totalCount, items);
    }

    public async Task<AdminSpellDetailReadModel?> GetByIdAsync(
        short spellId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var schema = await DetectSchemaAsync(connection, cancellationToken);
        var referenceSnapshot = _referenceCatalogReader.GetSnapshot();
        referenceSnapshot.SpellsById.TryGetValue(spellId, out var reference);

        var runtime = await LoadRuntimeHeaderAsync(connection, schema, spellId, cancellationToken);
        if (runtime is null && reference is null)
        {
            return null;
        }

        var runtimeBreedIds = await LoadBreedIdsForSpellAsync(connection, schema, spellId, cancellationToken);
        var textOverride = schema.HasTextOverrides
            ? await LoadTextOverrideAsync(connection, spellId, cancellationToken)
            : null;
        var levels = await LoadLevelSummariesAsync(
            connection,
            schema,
            spellId,
            runtime,
            reference,
            cancellationToken);

        var typeId = runtime?.TypeId ?? reference?.TypeId;
        var typeLabel = ResolveTypeLabel(typeId, runtime?.RuntimeTypeLabel, reference, referenceSnapshot);
        var breeds = ResolveBreeds(runtimeBreedIds, reference)
            .Select(MapBreed)
            .ToArray();
        var referenceMetadata = reference is null
            ? null
            : new AdminSpellReferenceReadModel(
                referenceSnapshot.SourceDescription,
                reference.Name,
                reference.Description,
                reference.NameId,
                reference.DescriptionId,
                reference.TypeId,
                reference.TypeLabel,
                reference.IconId,
                reference.BreedIds.ToArray(),
                reference.LevelCount);

        return new AdminSpellDetailReadModel(
            spellId,
            FirstNonEmpty(textOverride?.DisplayName, runtime?.Name, reference?.Name),
            FirstNonEmpty(textOverride?.Description, runtime?.Description, reference?.Description),
            typeId,
            typeLabel,
            ResolveIconId(runtime, reference),
            breeds,
            levels.Count,
            RuntimeAvailable: runtime is not null,
            ReferenceAvailable: reference is not null,
            referenceMetadata,
            levels);
    }

    public async Task<IReadOnlyList<AdminSpellLevelDetailReadModel>?> GetLevelsAsync(
        short spellId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var schema = await DetectSchemaAsync(connection, cancellationToken);
        var referenceSnapshot = _referenceCatalogReader.GetSnapshot();
        referenceSnapshot.SpellsById.TryGetValue(spellId, out var reference);

        var runtime = await LoadRuntimeHeaderAsync(connection, schema, spellId, cancellationToken);
        if (runtime is null && reference is null)
        {
            return null;
        }

        return await LoadLevelDetailsAsync(
            connection,
            schema,
            spellId,
            runtime,
            reference,
            cancellationToken);
    }

    public async Task<AdminSpellLevelDetailReadModel?> GetLevelAsync(
        short spellId,
        int levelNumber,
        CancellationToken cancellationToken = default)
    {
        if (levelNumber <= 0)
        {
            return null;
        }

        var levels = await GetLevelsAsync(spellId, cancellationToken);
        if (levels is null || levelNumber > levels.Count)
        {
            return null;
        }

        return levels[levelNumber - 1];
    }

    public async Task<AdminSpellLevelUpdateResultModel?> UpdateLevelAsync(
        short spellId,
        int levelNumber,
        AdminSpellLevelUpdateDraft draft,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var schema = await DetectSchemaAsync(connection, cancellationToken);
        var runtime = await LoadRuntimeHeaderAsync(connection, schema, spellId, cancellationToken);
        if (runtime is null || levelNumber <= 0)
        {
            return null;
        }

        return schema.LevelSchema switch
        {
            SpellLevelSchema.CurrentRows => await UpdateCurrentSpellLevelAsync(
                connection,
                spellId,
                levelNumber,
                draft,
                cancellationToken),
            SpellLevelSchema.LegacyRows => await UpdateLegacySpellLevelAsync(
                connection,
                runtime.LevelsCsv,
                spellId,
                levelNumber,
                draft,
                cancellationToken),
            _ => throw new AdminNotConfiguredException(
                "No se encontro un esquema de `spells_levels` compatible para escribir niveles de spells."),
        };
    }

    private static bool MatchesFilters(AdminSpellCatalogReadModel item, SpellCatalogSearchRequest request)
    {
        if (request.SpellId.HasValue && item.SpellId != request.SpellId.Value)
        {
            return false;
        }

        if (request.BreedId.HasValue && item.Breeds.All(breed => breed.BreedId != request.BreedId.Value))
        {
            return false;
        }

        if (request.TypeId.HasValue && item.TypeId != request.TypeId.Value)
        {
            return false;
        }

        var normalizedSearch = NormalizeSearch(request.Search);
        if (normalizedSearch is null)
        {
            return true;
        }

        if (item.SpellId.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (item.TypeId?.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        if (item.IconId?.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        if (Contains(item.Name, normalizedSearch) ||
            Contains(item.Description, normalizedSearch) ||
            Contains(item.TypeLabel, normalizedSearch))
        {
            return true;
        }

        return item.Breeds.Any(breed =>
            breed.BreedId.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
            Contains(breed.Label, normalizedSearch));
    }

    private async Task<SpellCatalogSchema> DetectSchemaAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TABLE_NAME AS TableName, COLUMN_NAME AS ColumnName
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND TABLE_NAME IN ('spells', 'spells_templates', 'spells_levels', 'breeds_spells', 'admin_entity_text_overrides');
            """;

        var rows = await connection.QueryAsync<SchemaColumnRow>(new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));

        var columnsByTable = rows
            .GroupBy(row => row.TableName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => new HashSet<string>(group.Select(row => row.ColumnName), StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

        var headerSchema = ResolveHeaderSchema(columnsByTable);
        var levelSchema = ResolveLevelSchema(columnsByTable);
        var breedSchema = ResolveBreedSchema(columnsByTable);
        var hasTextOverrides = columnsByTable.TryGetValue("admin_entity_text_overrides", out var overrideColumns) &&
                               overrideColumns.Contains("EntityType") &&
                               overrideColumns.Contains("EntityId") &&
                               overrideColumns.Contains("DisplayName") &&
                               overrideColumns.Contains("Description");

        return new SpellCatalogSchema(headerSchema, levelSchema, breedSchema, hasTextOverrides);
    }

    private static SpellHeaderSchema ResolveHeaderSchema(
        IReadOnlyDictionary<string, HashSet<string>> columnsByTable)
    {
        if (columnsByTable.TryGetValue("spells", out var currentColumns) &&
            currentColumns.Contains("Spell") &&
            currentColumns.Contains("TypeId") &&
            currentColumns.Contains("SpellLevelsIdsCSV"))
        {
            return SpellHeaderSchema.CurrentSpells;
        }

        if (columnsByTable.TryGetValue("spells_templates", out var legacyColumns) &&
            legacyColumns.Contains("Id") &&
            legacyColumns.Contains("TypeId") &&
            legacyColumns.Contains("SpellLevelsCSV"))
        {
            return SpellHeaderSchema.LegacyTemplates;
        }

        throw new AdminNotConfiguredException(
            "No se encontro una tabla de cabecera de spells compatible. Se esperaba `spells` o `spells_templates`.");
    }

    private static SpellLevelSchema ResolveLevelSchema(
        IReadOnlyDictionary<string, HashSet<string>> columnsByTable)
    {
        if (!columnsByTable.TryGetValue("spells_levels", out var levelColumns))
        {
            return SpellLevelSchema.None;
        }

        if (levelColumns.Contains("Id") &&
            levelColumns.Contains("APCost") &&
            levelColumns.Contains("BinaryEffects") &&
            levelColumns.Contains("BinaryCriticalEffects"))
        {
            return SpellLevelSchema.LegacyRows;
        }

        if (levelColumns.Contains("SpellId") &&
            levelColumns.Contains("ApCost") &&
            levelColumns.Contains("Range") &&
            levelColumns.Contains("MinRange") &&
            levelColumns.Contains("Effects") &&
            levelColumns.Contains("CriticalEffects"))
        {
            return SpellLevelSchema.CurrentRows;
        }

        return SpellLevelSchema.None;
    }

    private static BreedLinkSchema ResolveBreedSchema(
        IReadOnlyDictionary<string, HashSet<string>> columnsByTable)
    {
        if (!columnsByTable.TryGetValue("breeds_spells", out var breedColumns))
        {
            return BreedLinkSchema.None;
        }

        if (breedColumns.Contains("SpellId") && breedColumns.Contains("BreedId"))
        {
            return BreedLinkSchema.Legacy;
        }

        if (breedColumns.Contains("Spell") && breedColumns.Contains("Breed"))
        {
            return BreedLinkSchema.Current;
        }

        return BreedLinkSchema.None;
    }

    private async Task<IReadOnlyList<RuntimeSpellHeaderRow>> LoadRuntimeHeadersAsync(
        MySqlConnection connection,
        SpellCatalogSchema schema,
        CancellationToken cancellationToken)
    {
        return schema.HeaderSchema switch
        {
            SpellHeaderSchema.CurrentSpells => await LoadCurrentSpellHeadersAsync(connection, cancellationToken),
            SpellHeaderSchema.LegacyTemplates => await LoadLegacySpellHeadersAsync(connection, cancellationToken),
            _ => Array.Empty<RuntimeSpellHeaderRow>(),
        };
    }

    private async Task<RuntimeSpellHeaderRow?> LoadRuntimeHeaderAsync(
        MySqlConnection connection,
        SpellCatalogSchema schema,
        short spellId,
        CancellationToken cancellationToken)
    {
        return schema.HeaderSchema switch
        {
            SpellHeaderSchema.CurrentSpells => await LoadCurrentSpellHeaderAsync(connection, spellId, cancellationToken),
            SpellHeaderSchema.LegacyTemplates => await LoadLegacySpellHeaderAsync(connection, spellId, cancellationToken),
            _ => null,
        };
    }

    private static async Task<IReadOnlyList<RuntimeSpellHeaderRow>> LoadCurrentSpellHeadersAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                Spell AS SpellId,
                Name,
                Description,
                TypeId,
                IconId,
                SpellLevelsIdsCSV AS LevelsCsv
            FROM spells
            ORDER BY Spell;
            """;

        var rows = await connection.QueryAsync<CurrentSpellHeaderRow>(new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));

        return rows
            .Where(row => row.SpellId > 0)
            .Select(MapCurrentHeader)
            .ToArray();
    }

    private static async Task<RuntimeSpellHeaderRow?> LoadCurrentSpellHeaderAsync(
        MySqlConnection connection,
        short spellId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                Spell AS SpellId,
                Name,
                Description,
                TypeId,
                IconId,
                SpellLevelsIdsCSV AS LevelsCsv
            FROM spells
            WHERE Spell = @SpellId
            LIMIT 1;
            """;

        var row = await connection.QuerySingleOrDefaultAsync<CurrentSpellHeaderRow>(new CommandDefinition(
            sql,
            new
            {
                SpellId = spellId,
            },
            cancellationToken: cancellationToken));

        return row is null || row.SpellId <= 0
            ? null
            : MapCurrentHeader(row);
    }

    private static RuntimeSpellHeaderRow MapCurrentHeader(CurrentSpellHeaderRow row) =>
        new(
            row.SpellId,
            row.Name,
            row.Description,
            row.TypeId,
            IconId: row.IconId > 0 ? row.IconId : null,
            CountCsv(row.LevelsCsv),
            RuntimeTypeLabel: null,
            NormalizeCsv(row.LevelsCsv));

    private static async Task<IReadOnlyList<RuntimeSpellHeaderRow>> LoadLegacySpellHeadersAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                Id AS SpellId,
                TypeId,
                SpellLevelsCSV AS LevelsCsv
            FROM spells_templates
            ORDER BY Id;
            """;

        var rows = await connection.QueryAsync<LegacySpellHeaderRow>(new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));

        return rows
            .Where(row => row.SpellId > 0)
            .Select(MapLegacyHeader)
            .ToArray();
    }

    private static async Task<RuntimeSpellHeaderRow?> LoadLegacySpellHeaderAsync(
        MySqlConnection connection,
        short spellId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                Id AS SpellId,
                TypeId,
                SpellLevelsCSV AS LevelsCsv
            FROM spells_templates
            WHERE Id = @SpellId
            LIMIT 1;
            """;

        var row = await connection.QuerySingleOrDefaultAsync<LegacySpellHeaderRow>(new CommandDefinition(
            sql,
            new
            {
                SpellId = spellId,
            },
            cancellationToken: cancellationToken));

        return row is null || row.SpellId <= 0
            ? null
            : MapLegacyHeader(row);
    }

    private static RuntimeSpellHeaderRow MapLegacyHeader(LegacySpellHeaderRow row) =>
        new(
            row.SpellId,
            Name: null,
            Description: null,
            row.TypeId,
            IconId: null,
            CountCsv(row.LevelsCsv),
            RuntimeTypeLabel: null,
            NormalizeCsv(row.LevelsCsv));

    private async Task<IReadOnlyDictionary<short, IReadOnlyList<int>>> LoadBreedIdsAsync(
        MySqlConnection connection,
        SpellCatalogSchema schema,
        CancellationToken cancellationToken)
    {
        if (schema.BreedLinkSchema == BreedLinkSchema.None)
        {
            return new Dictionary<short, IReadOnlyList<int>>();
        }

        var sql = schema.BreedLinkSchema switch
        {
            BreedLinkSchema.Legacy => """
                SELECT SpellId, BreedId
                FROM breeds_spells;
                """,
            BreedLinkSchema.Current => """
                SELECT Spell AS SpellId, Breed AS BreedId
                FROM breeds_spells;
                """,
            _ => string.Empty,
        };

        var rows = await connection.QueryAsync<BreedLinkRow>(new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));

        return rows
            .Where(row => row.SpellId > 0 && row.BreedId > 0)
            .GroupBy(row => row.SpellId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<int>)group
                    .Select(row => row.BreedId)
                    .Distinct()
                    .OrderBy(value => value)
                    .ToArray());
    }

    private async Task<IReadOnlyList<int>> LoadBreedIdsForSpellAsync(
        MySqlConnection connection,
        SpellCatalogSchema schema,
        short spellId,
        CancellationToken cancellationToken)
    {
        if (schema.BreedLinkSchema == BreedLinkSchema.None)
        {
            return Array.Empty<int>();
        }

        var sql = schema.BreedLinkSchema switch
        {
            BreedLinkSchema.Legacy => """
                SELECT BreedId
                FROM breeds_spells
                WHERE SpellId = @SpellId;
                """,
            BreedLinkSchema.Current => """
                SELECT Breed AS BreedId
                FROM breeds_spells
                WHERE Spell = @SpellId;
                """,
            _ => string.Empty,
        };

        var rows = await connection.QueryAsync<int>(new CommandDefinition(
            sql,
            new
            {
                SpellId = spellId,
            },
            cancellationToken: cancellationToken));

        return rows
            .Where(value => value > 0)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
    }

    private static async Task<Dictionary<short, SpellTextOverride>> LoadTextOverridesAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                EntityId AS SpellId,
                DisplayName,
                Description
            FROM admin_entity_text_overrides
            WHERE EntityType = @EntityType;
            """;

        var rows = await connection.QueryAsync<SpellTextOverrideRow>(new CommandDefinition(
            sql,
            new
            {
                EntityType = SpellAdminEntityType,
            },
            cancellationToken: cancellationToken));

        return rows
            .Where(row => row.SpellId > 0)
            .ToDictionary(
                row => row.SpellId,
                row => new SpellTextOverride(row.DisplayName, row.Description));
    }

    private static async Task<SpellTextOverride?> LoadTextOverrideAsync(
        MySqlConnection connection,
        short spellId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                DisplayName,
                Description
            FROM admin_entity_text_overrides
            WHERE EntityType = @EntityType
              AND EntityId = @SpellId
            LIMIT 1;
            """;

        var row = await connection.QuerySingleOrDefaultAsync<SingleSpellTextOverrideRow>(new CommandDefinition(
            sql,
            new
            {
                EntityType = SpellAdminEntityType,
                SpellId = spellId,
            },
            cancellationToken: cancellationToken));

        return row is null
            ? null
            : new SpellTextOverride(row.DisplayName, row.Description);
    }

    private async Task<IReadOnlyList<AdminSpellLevelSummaryReadModel>> LoadLevelSummariesAsync(
        MySqlConnection connection,
        SpellCatalogSchema schema,
        short spellId,
        RuntimeSpellHeaderRow? runtime,
        ReferenceSpellCatalogReader.ReferenceSpellCatalogEntry? reference,
        CancellationToken cancellationToken)
    {
        if (runtime is null)
        {
            return BuildReferenceOnlyLevelSummaries(reference);
        }

        if (schema.LevelSchema == SpellLevelSchema.None)
        {
            throw new AdminNotConfiguredException(
                "No se encontro un esquema de `spells_levels` compatible para leer detalle de spells.");
        }

        var runtimeLevels = schema.LevelSchema switch
        {
            SpellLevelSchema.CurrentRows => await LoadCurrentSpellLevelsAsync(connection, spellId, cancellationToken),
            SpellLevelSchema.LegacyRows => await LoadLegacySpellLevelsAsync(connection, runtime.LevelsCsv, cancellationToken),
            _ => Array.Empty<RuntimeSpellLevelSummary>(),
        };

        return BuildMergedLevelSummaries(runtimeLevels, reference);
    }

    private static IReadOnlyList<AdminSpellLevelSummaryReadModel> BuildReferenceOnlyLevelSummaries(
        ReferenceSpellCatalogReader.ReferenceSpellCatalogEntry? reference)
    {
        if (reference is null || reference.OrderedLevelIds.Count == 0)
        {
            return Array.Empty<AdminSpellLevelSummaryReadModel>();
        }

        return reference.OrderedLevelIds
            .Select((levelId, index) =>
            {
                reference.LevelsById.TryGetValue(levelId, out var referenceLevel);
                return MapLevelSummary(index + 1, runtime: null, referenceLevel);
            })
            .ToArray();
    }

    private static IReadOnlyList<AdminSpellLevelSummaryReadModel> BuildMergedLevelSummaries(
        IReadOnlyList<RuntimeSpellLevelSummary> runtimeLevels,
        ReferenceSpellCatalogReader.ReferenceSpellCatalogEntry? reference)
    {
        var referenceLevelIds = reference?.OrderedLevelIds ?? Array.Empty<int>();
        var totalLevels = Math.Max(runtimeLevels.Count, referenceLevelIds.Count);
        if (totalLevels == 0)
        {
            return Array.Empty<AdminSpellLevelSummaryReadModel>();
        }

        var result = new List<AdminSpellLevelSummaryReadModel>(totalLevels);
        for (var index = 0; index < totalLevels; index++)
        {
            var runtimeLevel = index < runtimeLevels.Count
                ? runtimeLevels[index]
                : null;
            ReferenceSpellCatalogReader.ReferenceSpellLevelEntry? referenceLevel = null;
            if (index < referenceLevelIds.Count &&
                reference is not null &&
                reference.LevelsById.TryGetValue(referenceLevelIds[index], out var resolvedReferenceLevel))
            {
                referenceLevel = resolvedReferenceLevel;
            }

            result.Add(MapLevelSummary(index + 1, runtimeLevel, referenceLevel));
        }

        return result;
    }

    private static AdminSpellLevelSummaryReadModel MapLevelSummary(
        int levelNumber,
        RuntimeSpellLevelSummary? runtime,
        ReferenceSpellCatalogReader.ReferenceSpellLevelEntry? reference)
    {
        return new AdminSpellLevelSummaryReadModel(
            levelNumber,
            runtime?.RuntimeLevelId,
            reference?.LevelId,
            runtime?.MinPlayerLevel ?? reference?.MinPlayerLevel ?? 0,
            runtime?.ApCost ?? reference?.ApCost ?? 0,
            runtime?.MinRange ?? reference?.MinRange ?? 0,
            runtime?.MaxRange ?? reference?.MaxRange ?? 0,
            runtime?.CastInLine ?? reference?.CastInLine ?? false,
            runtime?.CastTestLos ?? reference?.CastTestLos ?? false,
            runtime?.NeedFreeCell ?? reference?.NeedFreeCell ?? false,
            runtime?.RangeCanBeBoosted ?? reference?.RangeCanBeBoosted ?? false,
            runtime?.CriticalFailureEndsTurn ?? reference?.CriticalFailureEndsTurn ?? false,
            runtime?.CriticalHitProbability ?? reference?.CriticalHitProbability ?? 0,
            runtime?.CriticalFailureProbability ?? reference?.CriticalFailureProbability ?? 0,
            runtime?.MaxCastPerTurn ?? reference?.MaxCastPerTurn ?? 0,
            runtime?.MaxCastPerTarget ?? reference?.MaxCastPerTarget ?? 0,
            runtime?.MinCastInterval ?? reference?.MinCastInterval ?? 0,
            runtime?.StatesRequired ?? ParseCsvShorts(reference?.StatesRequiredCsv),
            runtime?.StatesForbidden ?? ParseCsvShorts(reference?.StatesForbiddenCsv),
            runtime?.HasEffects ?? reference?.HasEffects ?? false,
            runtime?.HasCriticalEffects ?? reference?.HasCriticalEffects ?? false,
            RuntimeAvailable: runtime is not null,
            ReferenceAvailable: reference is not null);
    }

    private static async Task<IReadOnlyList<RuntimeSpellLevelSummary>> LoadCurrentSpellLevelsAsync(
        MySqlConnection connection,
        short spellId,
        CancellationToken cancellationToken)
    {
        // Mirror the current Sunshine runtime manager, which reads spells_levels rows by native row order.
        const string sql = """
            SELECT
                SpellId,
                SpellBreed,
                ApCost,
                MinRange,
                `Range` AS MaxRange,
                CastInLine,
                CastTestLos,
                NeedFreeCell,
                RangeCanBeBoosted,
                CriticalFailureEndsTurn,
                CriticalHitProbability,
                CriticalFailureProbability,
                MaxCastPerTurn,
                MaxCastPerTarget,
                MinCastInterval,
                MinPlayerLevel,
                StatesRequiredCSV,
                StatesForbiddenCSV,
                Effects,
                CriticalEffects
            FROM spells_levels
            WHERE SpellId = @SpellId;
            """;

        var rows = await connection.QueryAsync<CurrentSpellLevelRow>(new CommandDefinition(
            sql,
            new
            {
                SpellId = spellId,
            },
            cancellationToken: cancellationToken));

        return rows
            .Select(row => new RuntimeSpellLevelSummary(
                RuntimeLevelId: null,
                row.ApCost,
                ParseNonNegativeInt(row.MinRange),
                ParseNonNegativeInt(row.MaxRange),
                row.CastInLine,
                row.CastTestLos,
                row.NeedFreeCell,
                row.RangeCanBeBoosted,
                row.CriticalFailureEndsTurn,
                row.CriticalHitProbability,
                row.CriticalFailureProbability,
                row.MaxCastPerTurn,
                row.MaxCastPerTarget,
                row.MinCastInterval,
                row.MinPlayerLevel,
                ParseCsvShorts(row.StatesRequiredCsv),
                ParseCsvShorts(row.StatesForbiddenCsv),
                HasSerializedPayload(row.Effects),
                HasSerializedPayload(row.CriticalEffects)))
            .ToArray();
    }

    private static async Task<IReadOnlyList<RuntimeSpellLevelSummary>> LoadLegacySpellLevelsAsync(
        MySqlConnection connection,
        string? levelsCsv,
        CancellationToken cancellationToken)
    {
        var levelIds = ParseCsvInts(levelsCsv);
        if (levelIds.Count == 0)
        {
            return Array.Empty<RuntimeSpellLevelSummary>();
        }

        var sql = $"""
            SELECT
                Id AS RuntimeLevelId,
                APCost AS ApCost,
                MinRange,
                MaxRange,
                CastInLine,
                CastTestLOS AS CastTestLos,
                NeedFreeCell,
                RangeCanBeBoosted,
                CriticalFailureEndsTurn,
                CriticalHitProbability,
                CriticalFailureProbability,
                MaxCastPerTurn,
                MaxCastPerTarget,
                MinCastInterval,
                MinPlayerLevel,
                StatesRequiredCSV,
                StatesForbiddenCSV,
                BinaryEffects,
                BinaryCriticalEffects
            FROM spells_levels
            WHERE Id IN ({string.Join(",", levelIds)});
            """;

        var rows = await connection.QueryAsync<LegacySpellLevelRow>(new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));
        var rowsById = rows.ToDictionary(row => row.RuntimeLevelId);

        var result = new List<RuntimeSpellLevelSummary>(levelIds.Count);
        foreach (var levelId in levelIds)
        {
            if (!rowsById.TryGetValue(levelId, out var row))
            {
                continue;
            }

            result.Add(new RuntimeSpellLevelSummary(
                row.RuntimeLevelId,
                row.ApCost,
                row.MinRange,
                row.MaxRange,
                row.CastInLine,
                row.CastTestLos,
                row.NeedFreeCell,
                row.RangeCanBeBoosted,
                row.CriticalFailureEndsTurn,
                row.CriticalHitProbability,
                row.CriticalFailureProbability,
                row.MaxCastPerTurn,
                row.MaxCastPerTarget,
                row.MinCastInterval,
                row.MinPlayerLevel,
                ParseCsvShorts(row.StatesRequiredCsv),
                ParseCsvShorts(row.StatesForbiddenCsv),
                HasBinaryPayload(row.BinaryEffects),
                HasBinaryPayload(row.BinaryCriticalEffects)));
        }

        return result;
    }

    private async Task<IReadOnlyList<AdminSpellLevelDetailReadModel>> LoadLevelDetailsAsync(
        MySqlConnection connection,
        SpellCatalogSchema schema,
        short spellId,
        RuntimeSpellHeaderRow? runtime,
        ReferenceSpellCatalogReader.ReferenceSpellCatalogEntry? reference,
        CancellationToken cancellationToken)
    {
        if (runtime is null)
        {
            return BuildReferenceOnlyLevelDetails(reference);
        }

        if (schema.LevelSchema == SpellLevelSchema.None)
        {
            throw new AdminNotConfiguredException(
                "No se encontro un esquema de `spells_levels` compatible para leer niveles detallados de spells.");
        }

        var runtimeLevels = schema.LevelSchema switch
        {
            SpellLevelSchema.CurrentRows => await LoadCurrentSpellLevelDetailsAsync(connection, spellId, cancellationToken),
            SpellLevelSchema.LegacyRows => await LoadLegacySpellLevelDetailsAsync(connection, runtime.LevelsCsv, cancellationToken),
            _ => Array.Empty<RuntimeSpellLevelDetail>(),
        };

        return BuildMergedLevelDetails(runtimeLevels, reference);
    }

    private static IReadOnlyList<AdminSpellLevelDetailReadModel> BuildReferenceOnlyLevelDetails(
        ReferenceSpellCatalogReader.ReferenceSpellCatalogEntry? reference)
    {
        if (reference is null || reference.OrderedLevelIds.Count == 0)
        {
            return Array.Empty<AdminSpellLevelDetailReadModel>();
        }

        return reference.OrderedLevelIds
            .Select((levelId, index) =>
            {
                reference.LevelsById.TryGetValue(levelId, out var referenceLevel);
                return MapLevelDetail(index + 1, runtime: null, referenceLevel);
            })
            .ToArray();
    }

    private static IReadOnlyList<AdminSpellLevelDetailReadModel> BuildMergedLevelDetails(
        IReadOnlyList<RuntimeSpellLevelDetail> runtimeLevels,
        ReferenceSpellCatalogReader.ReferenceSpellCatalogEntry? reference)
    {
        var referenceLevelIds = reference?.OrderedLevelIds ?? Array.Empty<int>();
        var totalLevels = Math.Max(runtimeLevels.Count, referenceLevelIds.Count);
        if (totalLevels == 0)
        {
            return Array.Empty<AdminSpellLevelDetailReadModel>();
        }

        var result = new List<AdminSpellLevelDetailReadModel>(totalLevels);
        for (var index = 0; index < totalLevels; index++)
        {
            var runtimeLevel = index < runtimeLevels.Count
                ? runtimeLevels[index]
                : null;
            ReferenceSpellCatalogReader.ReferenceSpellLevelEntry? referenceLevel = null;
            if (index < referenceLevelIds.Count &&
                reference is not null &&
                reference.LevelsById.TryGetValue(referenceLevelIds[index], out var resolvedReferenceLevel))
            {
                referenceLevel = resolvedReferenceLevel;
            }

            result.Add(MapLevelDetail(index + 1, runtimeLevel, referenceLevel));
        }

        return result;
    }

    private static AdminSpellLevelDetailReadModel MapLevelDetail(
        int levelNumber,
        RuntimeSpellLevelDetail? runtime,
        ReferenceSpellCatalogReader.ReferenceSpellLevelEntry? reference)
    {
        return new AdminSpellLevelDetailReadModel(
            levelNumber,
            runtime?.RuntimeLevelId,
            reference?.LevelId,
            runtime?.MinPlayerLevel ?? reference?.MinPlayerLevel ?? 0,
            runtime?.ApCost ?? reference?.ApCost ?? 0,
            runtime?.MinRange ?? reference?.MinRange ?? 0,
            runtime?.MaxRange ?? reference?.MaxRange ?? 0,
            runtime?.CastInLine ?? reference?.CastInLine ?? false,
            runtime?.CastInDiagonal ?? reference?.CastInDiagonal ?? false,
            runtime?.CastTestLos ?? reference?.CastTestLos ?? false,
            runtime?.NeedFreeCell ?? reference?.NeedFreeCell ?? false,
            runtime?.NeedTakenCell ?? reference?.NeedTakenCell ?? false,
            runtime?.RangeCanBeBoosted ?? reference?.RangeCanBeBoosted ?? false,
            runtime?.CriticalFailureEndsTurn ?? reference?.CriticalFailureEndsTurn ?? false,
            runtime?.CriticalHitProbability ?? reference?.CriticalHitProbability ?? 0,
            runtime?.CriticalFailureProbability ?? reference?.CriticalFailureProbability ?? 0,
            runtime?.MaxCastPerTurn ?? reference?.MaxCastPerTurn ?? 0,
            runtime?.MaxCastPerTarget ?? reference?.MaxCastPerTarget ?? 0,
            runtime?.MinCastInterval ?? reference?.MinCastInterval ?? 0,
            runtime?.InitialCooldown ?? reference?.InitialCooldown ?? 0,
            runtime?.StatesRequired ?? ParseCsvShorts(reference?.StatesRequiredCsv),
            runtime?.StatesForbidden ?? ParseCsvShorts(reference?.StatesForbiddenCsv),
            runtime?.HasEffects ?? reference?.HasEffects ?? false,
            runtime?.HasCriticalEffects ?? reference?.HasCriticalEffects ?? false,
            RuntimeAvailable: runtime is not null,
            ReferenceAvailable: reference is not null);
    }

    private static async Task<IReadOnlyList<RuntimeSpellLevelDetail>> LoadCurrentSpellLevelDetailsAsync(
        MySqlConnection connection,
        short spellId,
        CancellationToken cancellationToken)
    {
        var rows = await LoadCurrentSpellLevelWriteRowsAsync(connection, spellId, cancellationToken);
        return rows
            .Select(row => new RuntimeSpellLevelDetail(
                RuntimeLevelId: null,
                row.ApCost,
                ParseNonNegativeInt(row.MinRange),
                ParseNonNegativeInt(row.Range),
                row.CastInLine,
                row.CastInDiagonal,
                row.CastTestLos,
                row.NeedFreeCell,
                row.NeedTakenCell,
                row.RangeCanBeBoosted,
                row.CriticalFailureEndsTurn,
                row.CriticalHitProbability,
                row.CriticalFailureProbability,
                row.MaxCastPerTurn,
                row.MaxCastPerTarget,
                row.MinCastInterval,
                ParseNonNegativeInt(row.InitialCooldown),
                row.MinPlayerLevel,
                ParseCsvShorts(row.StatesRequiredCsv),
                ParseCsvShorts(row.StatesForbiddenCsv),
                HasSerializedPayload(row.Effects),
                HasSerializedPayload(row.CriticalEffects)))
            .ToArray();
    }

    private static async Task<IReadOnlyList<RuntimeSpellLevelDetail>> LoadLegacySpellLevelDetailsAsync(
        MySqlConnection connection,
        string? levelsCsv,
        CancellationToken cancellationToken)
    {
        var levelIds = ParseCsvInts(levelsCsv);
        if (levelIds.Count == 0)
        {
            return Array.Empty<RuntimeSpellLevelDetail>();
        }

        var sql = $"""
            SELECT
                Id AS RuntimeLevelId,
                APCost AS ApCost,
                MinRange,
                MaxRange,
                CastInLine,
                CastTestLOS AS CastTestLos,
                NeedFreeCell,
                RangeCanBeBoosted,
                CriticalFailureEndsTurn,
                CriticalHitProbability,
                CriticalFailureProbability,
                MaxCastPerTurn,
                MaxCastPerTarget,
                MinCastInterval,
                MinPlayerLevel,
                StatesRequiredCSV,
                StatesForbiddenCSV,
                BinaryEffects,
                BinaryCriticalEffects
            FROM spells_levels
            WHERE Id IN ({string.Join(",", levelIds)});
            """;

        var rows = await connection.QueryAsync<LegacySpellLevelRow>(new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));
        var rowsById = rows.ToDictionary(row => row.RuntimeLevelId);

        var result = new List<RuntimeSpellLevelDetail>(levelIds.Count);
        foreach (var levelId in levelIds)
        {
            if (!rowsById.TryGetValue(levelId, out var row))
            {
                continue;
            }

            result.Add(new RuntimeSpellLevelDetail(
                row.RuntimeLevelId,
                row.ApCost,
                row.MinRange,
                row.MaxRange,
                row.CastInLine,
                CastInDiagonal: null,
                row.CastTestLos,
                row.NeedFreeCell,
                NeedTakenCell: null,
                row.RangeCanBeBoosted,
                row.CriticalFailureEndsTurn,
                row.CriticalHitProbability,
                row.CriticalFailureProbability,
                row.MaxCastPerTurn,
                row.MaxCastPerTarget,
                row.MinCastInterval,
                InitialCooldown: null,
                row.MinPlayerLevel,
                ParseCsvShorts(row.StatesRequiredCsv),
                ParseCsvShorts(row.StatesForbiddenCsv),
                HasBinaryPayload(row.BinaryEffects),
                HasBinaryPayload(row.BinaryCriticalEffects)));
        }

        return result;
    }

    private async Task<AdminSpellLevelUpdateResultModel?> UpdateCurrentSpellLevelAsync(
        MySqlConnection connection,
        short spellId,
        int levelNumber,
        AdminSpellLevelUpdateDraft draft,
        CancellationToken cancellationToken)
    {
        await LockCurrentSpellLevelsAsync(connection, cancellationToken);

        try
        {
            var originalRows = (await LoadCurrentSpellLevelWriteRowsAsync(connection, spellId, cancellationToken)).ToList();
            if (originalRows.Count == 0 || levelNumber > originalRows.Count)
            {
                return null;
            }

            var rewrittenRows = originalRows
                .Select(CloneCurrentSpellLevelWriteRow)
                .ToList();
            ApplyDraft(rewrittenRows[levelNumber - 1], draft);

            await DeleteCurrentSpellLevelsAsync(connection, spellId, cancellationToken);

            try
            {
                await InsertCurrentSpellLevelsAsync(connection, rewrittenRows, cancellationToken);
            }
            catch
            {
                await RestoreCurrentSpellLevelsAsync(connection, spellId, originalRows, cancellationToken);
                throw;
            }

            return new AdminSpellLevelUpdateResultModel(
                spellId,
                levelNumber,
                "current-runtime-row-rewrite",
                new[]
                {
                    "El esquema actual no tiene Id por nivel; se reescribieron las filas del spell preservando el orden runtime que consume Sunshine."
                });
        }
        finally
        {
            await UnlockCurrentSpellLevelsAsync(connection, cancellationToken);
        }
    }

    private async Task<AdminSpellLevelUpdateResultModel?> UpdateLegacySpellLevelAsync(
        MySqlConnection connection,
        string? levelsCsv,
        short spellId,
        int levelNumber,
        AdminSpellLevelUpdateDraft draft,
        CancellationToken cancellationToken)
    {
        var levelIds = ParseCsvInts(levelsCsv);
        if (levelIds.Count == 0 || levelNumber > levelIds.Count)
        {
            return null;
        }

        var targetLevelId = levelIds[levelNumber - 1];
        const string sql = """
            UPDATE spells_levels
            SET
                APCost = @ApCost,
                MinRange = @MinRange,
                MaxRange = @MaxRange,
                CastInLine = @CastInLine,
                CastTestLOS = @CastTestLos,
                NeedFreeCell = @NeedFreeCell,
                CriticalHitProbability = @CriticalHitProbability,
                CriticalFailureProbability = @CriticalFailureProbability,
                MaxCastPerTurn = @MaxCastPerTurn,
                MaxCastPerTarget = @MaxCastPerTarget,
                MinCastInterval = @MinCastInterval
            WHERE Id = @LevelId;
            """;

        var affectedRows = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                LevelId = targetLevelId,
                draft.ApCost,
                draft.MinRange,
                draft.MaxRange,
                draft.CastInLine,
                CastTestLos = draft.CastTestLos,
                draft.NeedFreeCell,
                draft.CriticalHitProbability,
                draft.CriticalFailureProbability,
                draft.MaxCastPerTurn,
                draft.MaxCastPerTarget,
                draft.MinCastInterval,
            },
            cancellationToken: cancellationToken));

        if (affectedRows <= 0)
        {
            return null;
        }

        return new AdminSpellLevelUpdateResultModel(
            spellId,
            levelNumber,
            "legacy-level-id-update",
            Array.Empty<string>());
    }

    private static async Task LockCurrentSpellLevelsAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "LOCK TABLES spells_levels WRITE;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UnlockCurrentSpellLevelsAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "UNLOCK TABLES;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<CurrentSpellLevelWriteRow>> LoadCurrentSpellLevelWriteRowsAsync(
        MySqlConnection connection,
        short spellId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                SpellId,
                SpellBreed,
                ApCost,
                `Range`,
                CastInLine,
                CastInDiagonal,
                CastTestLos,
                CriticalHitProbability,
                StatesRequiredCSV,
                CriticalFailureProbability,
                NeedFreeCell,
                NeedFreeTrapCell,
                NeedTakenCell,
                RangeCanBeBoosted,
                MaxStack,
                MaxCastPerTurn,
                MaxCastPerTarget,
                MinCastInterval,
                InitialCooldown,
                GlobalCooldown,
                MinPlayerLevel,
                CriticalFailureEndsTurn,
                HideEffects,
                Hidden,
                MinRange,
                StatesForbiddenCSV,
                Effects,
                CriticalEffects
            FROM spells_levels
            WHERE SpellId = @SpellId;
            """;

        var rows = await connection.QueryAsync<CurrentSpellLevelWriteRow>(new CommandDefinition(
            sql,
            new
            {
                SpellId = spellId,
            },
            cancellationToken: cancellationToken));

        return rows.ToArray();
    }

    private static CurrentSpellLevelWriteRow CloneCurrentSpellLevelWriteRow(CurrentSpellLevelWriteRow source) =>
        new()
        {
            SpellId = source.SpellId,
            SpellBreed = source.SpellBreed,
            ApCost = source.ApCost,
            Range = source.Range,
            CastInLine = source.CastInLine,
            CastInDiagonal = source.CastInDiagonal,
            CastTestLos = source.CastTestLos,
            CriticalHitProbability = source.CriticalHitProbability,
            StatesRequiredCsv = source.StatesRequiredCsv,
            CriticalFailureProbability = source.CriticalFailureProbability,
            NeedFreeCell = source.NeedFreeCell,
            NeedFreeTrapCell = source.NeedFreeTrapCell,
            NeedTakenCell = source.NeedTakenCell,
            RangeCanBeBoosted = source.RangeCanBeBoosted,
            MaxStack = source.MaxStack,
            MaxCastPerTurn = source.MaxCastPerTurn,
            MaxCastPerTarget = source.MaxCastPerTarget,
            MinCastInterval = source.MinCastInterval,
            InitialCooldown = source.InitialCooldown,
            GlobalCooldown = source.GlobalCooldown,
            MinPlayerLevel = source.MinPlayerLevel,
            CriticalFailureEndsTurn = source.CriticalFailureEndsTurn,
            HideEffects = source.HideEffects,
            Hidden = source.Hidden,
            MinRange = source.MinRange,
            StatesForbiddenCsv = source.StatesForbiddenCsv,
            Effects = source.Effects,
            CriticalEffects = source.CriticalEffects,
        };

    private static void ApplyDraft(CurrentSpellLevelWriteRow target, AdminSpellLevelUpdateDraft draft)
    {
        target.ApCost = draft.ApCost;
        target.MinRange = draft.MinRange;
        target.Range = draft.MaxRange;
        target.CastInLine = draft.CastInLine;
        target.CastTestLos = draft.CastTestLos;
        target.CriticalHitProbability = draft.CriticalHitProbability;
        target.CriticalFailureProbability = draft.CriticalFailureProbability;
        target.NeedFreeCell = draft.NeedFreeCell;
        target.MinCastInterval = draft.MinCastInterval;
        target.MaxCastPerTurn = draft.MaxCastPerTurn;
        target.MaxCastPerTarget = draft.MaxCastPerTarget;

        if (draft.CastInDiagonal.HasValue)
        {
            target.CastInDiagonal = draft.CastInDiagonal.Value;
        }

        if (draft.NeedTakenCell.HasValue)
        {
            target.NeedTakenCell = draft.NeedTakenCell.Value;
        }

        if (draft.InitialCooldown.HasValue)
        {
            target.InitialCooldown = draft.InitialCooldown.Value;
        }
    }

    private static async Task DeleteCurrentSpellLevelsAsync(
        MySqlConnection connection,
        short spellId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM spells_levels
            WHERE SpellId = @SpellId;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                SpellId = spellId,
            },
            cancellationToken: cancellationToken));
    }

    private static async Task RestoreCurrentSpellLevelsAsync(
        MySqlConnection connection,
        short spellId,
        IReadOnlyList<CurrentSpellLevelWriteRow> originalRows,
        CancellationToken cancellationToken)
    {
        await DeleteCurrentSpellLevelsAsync(connection, spellId, cancellationToken);
        await InsertCurrentSpellLevelsAsync(connection, originalRows, cancellationToken);
    }

    private static async Task InsertCurrentSpellLevelsAsync(
        MySqlConnection connection,
        IReadOnlyList<CurrentSpellLevelWriteRow> rows,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO spells_levels
            (
                SpellId,
                SpellBreed,
                ApCost,
                `Range`,
                CastInLine,
                CastInDiagonal,
                CastTestLos,
                CriticalHitProbability,
                StatesRequiredCSV,
                CriticalFailureProbability,
                NeedFreeCell,
                NeedFreeTrapCell,
                NeedTakenCell,
                RangeCanBeBoosted,
                MaxStack,
                MaxCastPerTurn,
                MaxCastPerTarget,
                MinCastInterval,
                InitialCooldown,
                GlobalCooldown,
                MinPlayerLevel,
                CriticalFailureEndsTurn,
                HideEffects,
                Hidden,
                MinRange,
                StatesForbiddenCSV,
                Effects,
                CriticalEffects
            )
            VALUES
            (
                @SpellId,
                @SpellBreed,
                @ApCost,
                @Range,
                @CastInLine,
                @CastInDiagonal,
                @CastTestLos,
                @CriticalHitProbability,
                @StatesRequiredCsv,
                @CriticalFailureProbability,
                @NeedFreeCell,
                @NeedFreeTrapCell,
                @NeedTakenCell,
                @RangeCanBeBoosted,
                @MaxStack,
                @MaxCastPerTurn,
                @MaxCastPerTarget,
                @MinCastInterval,
                @InitialCooldown,
                @GlobalCooldown,
                @MinPlayerLevel,
                @CriticalFailureEndsTurn,
                @HideEffects,
                @Hidden,
                @MinRange,
                @StatesForbiddenCsv,
                @Effects,
                @CriticalEffects
            );
            """;

        foreach (var row in rows)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                row,
                cancellationToken: cancellationToken));
        }
    }

    private static IReadOnlyList<int> ResolveBreeds(
        short spellId,
        IReadOnlyDictionary<short, IReadOnlyList<int>> runtimeBreedIds,
        ReferenceSpellCatalogReader.ReferenceSpellCatalogEntry? reference)
    {
        if (runtimeBreedIds.TryGetValue(spellId, out var breeds) && breeds.Count > 0)
        {
            return breeds;
        }

        return reference?.BreedIds ?? Array.Empty<int>();
    }

    private static IReadOnlyList<int> ResolveBreeds(
        IReadOnlyList<int> runtimeBreedIds,
        ReferenceSpellCatalogReader.ReferenceSpellCatalogEntry? reference)
    {
        return runtimeBreedIds.Count > 0
            ? runtimeBreedIds
            : reference?.BreedIds ?? Array.Empty<int>();
    }

    private static AdminSpellBreedReadModel MapBreed(int breedId) =>
        new(breedId, ResolveBreedLabel(breedId));

    private static string? ResolveBreedLabel(int breedId)
    {
        return breedId switch
        {
            1 => "Feca",
            2 => "Ocra",
            3 => "Osamodas",
            4 => "Enutrof",
            5 => "Sram",
            6 => "Xelor",
            7 => "Aniripsa",
            8 => "Iop",
            9 => "Zurcarak",
            10 => "Sadida",
            11 => "Sacrogrito",
            12 => "Pandawa",
            _ => null,
        };
    }

    private static string? ResolveTypeLabel(
        int? typeId,
        string? runtimeTypeLabel,
        ReferenceSpellCatalogReader.ReferenceSpellCatalogEntry? reference,
        ReferenceSpellCatalogReader.ReferenceSpellCatalogSnapshot referenceSnapshot)
    {
        if (!string.IsNullOrWhiteSpace(runtimeTypeLabel))
        {
            return runtimeTypeLabel;
        }

        if (!string.IsNullOrWhiteSpace(reference?.TypeLabel))
        {
            return reference.TypeLabel;
        }

        if (typeId.HasValue && referenceSnapshot.TypeLabels.TryGetValue(typeId.Value, out var typeLabel))
        {
            return typeLabel;
        }

        return null;
    }

    private static int? ResolveIconId(
        RuntimeSpellHeaderRow? runtime,
        ReferenceSpellCatalogReader.ReferenceSpellCatalogEntry? reference)
    {
        if (runtime?.IconId is > 0)
        {
            return runtime.IconId;
        }

        return reference?.IconId;
    }

    private static string? NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim();

    private static bool Contains(string? value, string search) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(search, StringComparison.OrdinalIgnoreCase);

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static int NormalizePage(int page) => page <= 0 ? 1 : page;

    private static int NormalizePageSize(int pageSize)
    {
        return pageSize switch
        {
            <= 0 => 20,
            > 100 => 100,
            _ => pageSize,
        };
    }

    private static int CountCsv(string? csv)
    {
        return (csv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Count(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string NormalizeCsv(string? csv) =>
        string.Join(
            ",",
            (csv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value)));

    private static IReadOnlyList<int> ParseCsvInts(string? csv) =>
        NormalizeCsv(csv)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var parsed) ? parsed : 0)
            .Where(value => value > 0)
            .ToArray();

    private static IReadOnlyList<short> ParseCsvShorts(string? csv) =>
        NormalizeCsv(csv)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value =>
                short.TryParse(value, out var parsed)
                    ? parsed
                    : (short)0)
            .Where(value => value > 0)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();

    private static bool HasSerializedPayload(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        if (string.Equals(normalized, "null", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[2..];
        }

        return normalized.Any(character => character != '0');
    }

    private static bool HasBinaryPayload(byte[]? buffer) =>
        buffer is { Length: > 0 } && buffer.Any(value => value != 0);

    private static int ParseNonNegativeInt(int value) => value < 0 ? 0 : value;

    private sealed record SpellCatalogSchema(
        SpellHeaderSchema HeaderSchema,
        SpellLevelSchema LevelSchema,
        BreedLinkSchema BreedLinkSchema,
        bool HasTextOverrides);

    private enum SpellHeaderSchema
    {
        CurrentSpells = 0,
        LegacyTemplates = 1,
    }

    private enum SpellLevelSchema
    {
        None = 0,
        CurrentRows = 1,
        LegacyRows = 2,
    }

    private enum BreedLinkSchema
    {
        None = 0,
        Current = 1,
        Legacy = 2,
    }

    private sealed record RuntimeSpellHeaderRow(
        short SpellId,
        string? Name,
        string? Description,
        int? TypeId,
        int? IconId,
        int LevelCount,
        string? RuntimeTypeLabel,
        string? LevelsCsv);

    private sealed record RuntimeSpellLevelSummary(
        int? RuntimeLevelId,
        int ApCost,
        int MinRange,
        int MaxRange,
        bool CastInLine,
        bool CastTestLos,
        bool NeedFreeCell,
        bool RangeCanBeBoosted,
        bool CriticalFailureEndsTurn,
        int CriticalHitProbability,
        int CriticalFailureProbability,
        int MaxCastPerTurn,
        int MaxCastPerTarget,
        int MinCastInterval,
        int MinPlayerLevel,
        IReadOnlyList<short> StatesRequired,
        IReadOnlyList<short> StatesForbidden,
        bool HasEffects,
        bool HasCriticalEffects);

    private sealed record RuntimeSpellLevelDetail(
        int? RuntimeLevelId,
        int ApCost,
        int MinRange,
        int MaxRange,
        bool CastInLine,
        bool? CastInDiagonal,
        bool CastTestLos,
        bool NeedFreeCell,
        bool? NeedTakenCell,
        bool RangeCanBeBoosted,
        bool CriticalFailureEndsTurn,
        int CriticalHitProbability,
        int CriticalFailureProbability,
        int MaxCastPerTurn,
        int MaxCastPerTarget,
        int MinCastInterval,
        int? InitialCooldown,
        int MinPlayerLevel,
        IReadOnlyList<short> StatesRequired,
        IReadOnlyList<short> StatesForbidden,
        bool HasEffects,
        bool HasCriticalEffects);

    private sealed record SpellTextOverride(
        string? DisplayName,
        string? Description);

    private sealed class SchemaColumnRow
    {
        public string TableName { get; set; } = string.Empty;

        public string ColumnName { get; set; } = string.Empty;
    }

    private sealed class CurrentSpellHeaderRow
    {
        public short SpellId { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public int TypeId { get; set; }

        public int IconId { get; set; }

        public string? LevelsCsv { get; set; }
    }

    private sealed class LegacySpellHeaderRow
    {
        public short SpellId { get; set; }

        public int TypeId { get; set; }

        public string? LevelsCsv { get; set; }
    }

    private sealed class BreedLinkRow
    {
        public short SpellId { get; set; }

        public int BreedId { get; set; }
    }

    private sealed class SpellTextOverrideRow
    {
        public short SpellId { get; set; }

        public string? DisplayName { get; set; }

        public string? Description { get; set; }
    }

    private sealed class SingleSpellTextOverrideRow
    {
        public string? DisplayName { get; set; }

        public string? Description { get; set; }
    }

    private sealed class CurrentSpellLevelRow
    {
        public int SpellId { get; set; }

        public int SpellBreed { get; set; }

        public int ApCost { get; set; }

        public int MinRange { get; set; }

        public int MaxRange { get; set; }

        public bool CastInLine { get; set; }

        public bool CastTestLos { get; set; }

        public bool NeedFreeCell { get; set; }

        public bool RangeCanBeBoosted { get; set; }

        public bool CriticalFailureEndsTurn { get; set; }

        public int CriticalHitProbability { get; set; }

        public int CriticalFailureProbability { get; set; }

        public int MaxCastPerTurn { get; set; }

        public int MaxCastPerTarget { get; set; }

        public int MinCastInterval { get; set; }

        public int MinPlayerLevel { get; set; }

        public string? StatesRequiredCsv { get; set; }

        public string? StatesForbiddenCsv { get; set; }

        public string? Effects { get; set; }

        public string? CriticalEffects { get; set; }
    }

    private sealed class CurrentSpellLevelWriteRow
    {
        public int SpellId { get; set; }

        public int SpellBreed { get; set; }

        public int ApCost { get; set; }

        public int Range { get; set; }

        public bool CastInLine { get; set; }

        public bool CastInDiagonal { get; set; }

        public bool CastTestLos { get; set; }

        public int CriticalHitProbability { get; set; }

        public string? StatesRequiredCsv { get; set; }

        public int CriticalFailureProbability { get; set; }

        public bool NeedFreeCell { get; set; }

        public bool NeedFreeTrapCell { get; set; }

        public bool NeedTakenCell { get; set; }

        public bool RangeCanBeBoosted { get; set; }

        public int MaxStack { get; set; }

        public int MaxCastPerTurn { get; set; }

        public int MaxCastPerTarget { get; set; }

        public int MinCastInterval { get; set; }

        public int InitialCooldown { get; set; }

        public int GlobalCooldown { get; set; }

        public int MinPlayerLevel { get; set; }

        public bool CriticalFailureEndsTurn { get; set; }

        public bool HideEffects { get; set; }

        public bool Hidden { get; set; }

        public int MinRange { get; set; }

        public string? StatesForbiddenCsv { get; set; }

        public string? Effects { get; set; }

        public string? CriticalEffects { get; set; }
    }

    private sealed class LegacySpellLevelRow
    {
        public int RuntimeLevelId { get; set; }

        public int ApCost { get; set; }

        public int MinRange { get; set; }

        public int MaxRange { get; set; }

        public bool CastInLine { get; set; }

        public bool CastTestLos { get; set; }

        public bool NeedFreeCell { get; set; }

        public bool RangeCanBeBoosted { get; set; }

        public bool CriticalFailureEndsTurn { get; set; }

        public int CriticalHitProbability { get; set; }

        public int CriticalFailureProbability { get; set; }

        public int MaxCastPerTurn { get; set; }

        public int MaxCastPerTarget { get; set; }

        public int MinCastInterval { get; set; }

        public int MinPlayerLevel { get; set; }

        public string? StatesRequiredCsv { get; set; }

        public string? StatesForbiddenCsv { get; set; }

        public byte[]? BinaryEffects { get; set; }

        public byte[]? BinaryCriticalEffects { get; set; }
    }
}
