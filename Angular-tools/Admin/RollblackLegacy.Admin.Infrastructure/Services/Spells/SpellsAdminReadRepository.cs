using Dapper;
using MySqlConnector;
using RollblackLegacy.Admin.Application.Abstractions.Spells;
using RollblackLegacy.Admin.Application.Exceptions;
using RollblackLegacy.Admin.Application.Models.Spells;
using RollblackLegacy.Admin.Contracts.Spells;
using RollblackLegacy.Admin.Infrastructure.Data;
using RollblackLegacy.Admin.Infrastructure.Spells;

namespace RollblackLegacy.Admin.Infrastructure.Services.Spells;

public sealed class SpellsAdminReadRepository : ISpellsAdminReadRepository
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

    private async Task<SpellCatalogSchema> DetectSchemaAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TABLE_NAME AS TableName, COLUMN_NAME AS ColumnName
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND TABLE_NAME IN ('spells', 'spells_templates', 'breeds_spells', 'admin_entity_text_overrides');
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
        var breedSchema = ResolveBreedSchema(columnsByTable);
        var hasTextOverrides = columnsByTable.TryGetValue("admin_entity_text_overrides", out var overrideColumns) &&
                               overrideColumns.Contains("EntityType") &&
                               overrideColumns.Contains("EntityId") &&
                               overrideColumns.Contains("DisplayName") &&
                               overrideColumns.Contains("Description");

        return new SpellCatalogSchema(headerSchema, breedSchema, hasTextOverrides);
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
            .Select(row => new RuntimeSpellHeaderRow(
                row.SpellId,
                row.Name,
                row.Description,
                row.TypeId,
                IconId: row.IconId > 0 ? row.IconId : null,
                CountCsv(row.LevelsCsv),
                RuntimeTypeLabel: null))
            .ToArray();
    }

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
            .Select(row => new RuntimeSpellHeaderRow(
                row.SpellId,
                Name: null,
                Description: null,
                row.TypeId,
                IconId: null,
                CountCsv(row.LevelsCsv),
                RuntimeTypeLabel: null))
            .ToArray();
    }

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

    private static int CountCsv(string? csv)
    {
        return (csv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Count(value => !string.IsNullOrWhiteSpace(value));
    }

    private sealed record SpellCatalogSchema(
        SpellHeaderSchema HeaderSchema,
        BreedLinkSchema BreedLinkSchema,
        bool HasTextOverrides);

    private enum SpellHeaderSchema
    {
        CurrentSpells = 0,
        LegacyTemplates = 1,
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
        string? RuntimeTypeLabel);

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
}
