using System.Text.Json;
using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.GameEffects;
using Rollback.Admin.Models.Spells;
using Rollback.Common.Runtime;
using Rollback.Protocol.Enums;
using Rollback.World.CustomEnums;
using Rollback.World.Database.Breeds;
using Rollback.World.Database.Spells;

namespace Rollback.Admin.Services;

public sealed class SpellAdminService
{
    private static readonly JsonSerializerOptions PersistentZoneSyncJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly SpellAdminSchemaService _schemaService;
    private readonly GameEffectEditorService _effectEditorService;
    private readonly ReferenceSpellCatalogService _referenceCatalogService;
    private readonly ClassicSpellDomainService _classicSpellDomainService;
    private readonly AdminEntityTextOverrideService _textOverrideService;
    private readonly AdminRuntimeRevisionService _runtimeRevisionService;
    private readonly SpellClientPublishService _spellClientPublishService;
    private readonly ClientSpellLocalizationService _clientLocalizationService = new();

    public SpellAdminService(
        AdminDbConnectionFactory connectionFactory,
        SpellAdminSchemaService schemaService,
        GameEffectEditorService effectEditorService,
        ReferenceSpellCatalogService referenceCatalogService,
        ClassicSpellDomainService classicSpellDomainService,
        AdminEntityTextOverrideService textOverrideService,
        AdminRuntimeRevisionService runtimeRevisionService,
        SpellClientPublishService spellClientPublishService)
    {
        _connectionFactory = connectionFactory;
        _schemaService = schemaService;
        _effectEditorService = effectEditorService;
        _referenceCatalogService = referenceCatalogService;
        _classicSpellDomainService = classicSpellDomainService;
        _textOverrideService = textOverrideService;
        _runtimeRevisionService = runtimeRevisionService;
        _spellClientPublishService = spellClientPublishService;
    }

    public IReadOnlyList<SpellTypeOption> GetTypeOptions()
    {
        var options = _referenceCatalogService.GetTypeOptions();
        return options.Count > 0
            ? options
            : _clientLocalizationService.GetTypeOptions();
    }

    public IReadOnlyList<AdminLookupOption> GetBreedOptions() =>
        Enum.GetValues<BreedEnum>()
            .Where(breed => breed is >= BreedEnum.Feca and <= BreedEnum.Pandawa)
            .Select(breed => new AdminLookupOption(
                ((int)breed).ToString(),
                $"{GetBreedLabel(breed)} [{(int)breed}]"))
            .ToArray();

    public async Task<AdminPagedResult<SpellListItem>> GetPagedAsync(
        AdminPagedQuery query,
        sbyte? typeId = null,
        bool onlyWithCriticalEffects = false,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var normalized = Normalize(query);
        var search = normalized.Search.Trim();
        var runtimeHeaders = await LoadSpellHeadersAsync(connection, cancellationToken);
        var runtimeHeadersById = runtimeHeaders.ToDictionary(row => row.Id);
        var runtimeLevelsById = await LoadRuntimeLevelSummariesAsync(connection, cancellationToken);
        var domain = await _classicSpellDomainService.BuildAsync(runtimeHeadersById.Keys, cancellationToken);
        var adminSpellIds = domain.AdminSpellIds.OrderBy(id => id).ToArray();
        var overrides = await _textOverrideService.GetManyAsync(
            AdminEntityType.Spell,
            adminSpellIds.Select(id => (int)id),
            cancellationToken);

        var items = new List<SpellListItem>(adminSpellIds.Length);
        foreach (var spellId in adminSpellIds)
        {
            var hasRuntimeHeader = runtimeHeadersById.TryGetValue(spellId, out var runtimeHeaderValue);
            SpellHeaderRow? runtimeHeader = hasRuntimeHeader ? runtimeHeaderValue : null;
            var reference = _referenceCatalogService.Get(spellId);
            overrides.TryGetValue(spellId, out var textOverride);
            var client = _clientLocalizationService.Get(spellId);
            var identity = ResolveIdentity(spellId, runtimeHeader, reference, textOverride, client);

            if (typeId.HasValue && identity.DisplayTypeId != typeId.Value)
                continue;

            if (!MatchesSearch(search, spellId, identity))
                continue;

            var runtimeLevelIds = runtimeHeader is SpellHeaderRow header
                ? ParseLevelIds(header.LevelsCsv)
                : Array.Empty<int>();
            var runtimeLevels = runtimeLevelIds
                .Where(levelId => runtimeLevelsById.ContainsKey(levelId))
                .Select(levelId => runtimeLevelsById[levelId])
                .ToArray();
            var criticalLevelCount = runtimeLevels.Count(level => level.HasCriticalEffects);
            if (onlyWithCriticalEffects && criticalLevelCount == 0)
                continue;

            var audit = BuildAudit(spellId, domain, runtimeHeader, reference, runtimeLevels);
            audit.IdentitySourceLabel = identity.IdentitySourceLabel;
            items.Add(new SpellListItem
            {
                Id = spellId,
                TypeId = identity.DisplayTypeId,
                TypeLabel = identity.DisplayTypeLabel,
                Name = identity.DisplayName,
                Description = identity.DisplayDescription,
                LevelCount = runtimeHeader is SpellHeaderRow
                    ? runtimeLevelIds.Length
                    : reference?.OrderedLevelIds.Count ?? 0,
                CriticalLevelCount = criticalLevelCount,
                MaxPlayerLevel = runtimeLevels.Length == 0
                    ? (byte)0
                    : (byte)Math.Min(byte.MaxValue, runtimeLevels.Max(level => level.MinPlayerLevel)),
                DisplayIconId = identity.DisplayIconId,
                Audit = audit,
            });
        }

        var totalCount = items.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)normalized.PageSize));
        var currentPage = Math.Min(normalized.Page, totalPages);
        var pagedItems = items
            .OrderBy(item => item.Id)
            .Skip((currentPage - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToArray();

        return new AdminPagedResult<SpellListItem>(pagedItems, totalCount, currentPage, normalized.PageSize);
    }

    public async Task<SpellEditModel?> GetByIdAsync(short spellId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var runtimeHeader = await GetHeaderAsync(connection, spellId, cancellationToken);
        var reference = _referenceCatalogService.Get(spellId);
        if (runtimeHeader is null && reference is null)
            return null;

        var textOverride = await AdminEntityTextOverrideService.GetAsync(
            connection,
            AdminEntityType.Spell,
            spellId,
            cancellationToken);
        var assignedBreedIds = await LoadAssignedBreedIdsAsync(connection, spellId, cancellationToken);
        var client = _clientLocalizationService.Get(spellId);
        var identity = ResolveIdentity(spellId, runtimeHeader, reference, textOverride, client);
        var domain = await _classicSpellDomainService.BuildAsync(
            runtimeHeader is SpellHeaderRow ? new[] { spellId } : Array.Empty<short>(),
            cancellationToken);

        var levels = new List<SpellLevelEditModel>();
        var runtimeLevels = Array.Empty<RuntimeSpellLevelSummary>();
        var runtimeLevelsCsv = string.Empty;
        if (runtimeHeader is SpellHeaderRow spellHeader)
        {
            runtimeLevelsCsv = NormalizeCsv(spellHeader.LevelsCsv);
            var levelIds = ParseLevelIds(spellHeader.LevelsCsv);
            var levelsById = (await LoadLevelsAsync(connection, levelIds, cancellationToken))
                .ToDictionary(level => level.Id);

            for (var index = 0; index < levelIds.Length; index++)
            {
                if (!levelsById.TryGetValue(levelIds[index], out var record))
                    continue;

                var mappedLevel = _schemaService.MapLevel(record, index + 1);
                NormalizeSpellLevel(mappedLevel);
                levels.Add(mappedLevel);
            }

            runtimeLevels = levelIds
                .Where(levelsById.ContainsKey)
                .Select(levelId => MapRuntimeLevelSummary(levelsById[levelId]))
                .ToArray();
        }

        var audit = BuildAudit(spellId, domain, runtimeHeader, reference, runtimeLevels);
        audit.IdentitySourceLabel = identity.IdentitySourceLabel;
        if (levels.Count == 0 && runtimeHeader is SpellHeaderRow)
            levels.Add(_schemaService.CreateDefaultLevel(1));

        return new SpellEditModel
        {
            Id = spellId,
            TypeId = runtimeHeader?.TypeId ?? identity.DisplayTypeId,
            TypeLabel = runtimeHeader is SpellHeaderRow runtimeSpell
                ? GetRuntimeTypeLabel(runtimeSpell.TypeId)
                : identity.DisplayTypeLabel,
            Name = ResolveDisplayName(identity.DisplayName, spellId),
            Description = identity.DisplayDescription,
            OverrideName = textOverride?.DisplayName ?? string.Empty,
            OverrideDescription = textOverride?.Description ?? string.Empty,
            ReferenceName = reference?.Name ?? string.Empty,
            ReferenceDescription = reference?.Description ?? string.Empty,
            ReferenceNameId = reference?.NameId,
            ReferenceDescriptionId = reference?.DescriptionId,
            ReferenceTypeId = reference?.TypeId,
            ReferenceTypeLabel = reference?.TypeLabel ?? string.Empty,
            ReferenceIconId = reference?.IconId,
            ReferenceLevelIdsCsv = reference?.SpellLevelsIdsCsv ?? string.Empty,
            RuntimeLevelIdsCsv = runtimeLevelsCsv,
            ClientName = client.Name,
            ClientDescription = client.Description,
            ClientIconId = client.IconId,
            DisplayIconId = identity.DisplayIconId,
            AssignedBreedIds = assignedBreedIds.ToList(),
            ReferenceBreedIds = reference?.SpellBreeds
                .Where(breed => breed is >= 1 and <= 12)
                .Distinct()
                .OrderBy(breed => breed)
                .ToList()
                ?? new List<int>(),
            RuntimeExists = runtimeHeader is not null,
            Audit = audit,
            Levels = levels,
        };
    }

    public async Task<short> GetNextAvailableIdAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id FROM spells_templates WHERE Id > 0 ORDER BY Id ASC;";

        short nextId = 1;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var currentId = reader.GetSafeInt16("Id");
            if (currentId < nextId)
                continue;

            if (currentId == nextId)
            {
                if (nextId == short.MaxValue)
                    throw new InvalidOperationException("No quedan IDs libres en el rango soportado por spells_templates.Id.");

                nextId++;
                continue;
            }

            break;
        }

        return nextId;
    }

    public async Task<bool> ExistsAsync(short spellId, CancellationToken cancellationToken = default)
    {
        if (spellId <= 0)
            return false;

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM spells_templates WHERE Id = @spellId LIMIT 1;";
        command.Parameters.AddWithValue("@spellId", spellId);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null and not DBNull;
    }

    public async Task<short> EnsureMatanzaAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSpecialSpellAsync(AdminSpellPresets.MatanzaSpellId, cancellationToken);
        return AdminSpellPresets.MatanzaSpellId;
    }

    public async Task<short> EnsureDoomAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSpecialSpellAsync(AdminSpellPresets.DoomSpellId, cancellationToken);
        return AdminSpellPresets.DoomSpellId;
    }

    public async Task<short> EnsureRollBackAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSpecialSpellAsync(AdminSpellPresets.RollBackSpellId, cancellationToken);
        return AdminSpellPresets.RollBackSpellId;
    }

    public async Task<short> EnsureSpecialSpellAsync(short spellId, CancellationToken cancellationToken = default)
    {
        if (!AdminSpellPresets.TryGet(spellId, out var definition))
            throw new InvalidOperationException($"No existe un preset staff para el spell #{spellId}.");

        if (await ExistsAsync(spellId, cancellationToken))
            return spellId;

        var visualReference = await GetByIdAsync(definition.VisualReferenceSpellId, cancellationToken);
        var model = BuildSpecialSpellModel(definition, visualReference);
        await SaveAsync(model, cancellationToken);
        return spellId;
    }

    public async Task<SpellReferenceSummary> GetReferencesAsync(short spellId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        return await GetReferencesAsync(connection, spellId, cancellationToken);
    }

    public async Task<AdminSaveResult> SaveRuntimeAsync(SpellEditModel model, CancellationToken cancellationToken = default)
    {
        ValidateModel(model);
        NormalizeLevels(model.Levels);
        NormalizeBreedAssignments(model.AssignedBreedIds);

        var reference = _referenceCatalogService.Get(model.Id);
        var client = _clientLocalizationService.Get(model.Id);
        var warnings = BuildWarnings(model, reference, client);
        var infos = new List<string>();

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var existingHeader = await GetHeaderAsync(connection, model.Id, cancellationToken);
        var references = await GetReferencesAsync(connection, model.Id, cancellationToken);
        if (existingHeader is not null && references.MaxReferencedLevel > model.Levels.Count)
        {
            throw new InvalidOperationException(
                $"No puedes dejar el hechizo con solo {model.Levels.Count} nivel(es): characters_spells o monsters_spells ya usan hasta el nivel {references.MaxReferencedLevel}.");
        }

        var currentLevelIds = existingHeader is not SpellHeaderRow existingSpellHeader
            ? Array.Empty<int>()
            : ParseLevelIds(existingSpellHeader.LevelsCsv);

        var nextLevelId = await GetMaxLevelIdAsync(connection, cancellationToken);
        foreach (var level in model.Levels.Where(level => level.Id <= 0))
            level.Id = ++nextLevelId;

        var newLevelIds = model.Levels.Select(level => level.Id).Distinct().ToArray();
        var removedLevelIds = currentLevelIds
            .Except(newLevelIds)
            .ToArray();

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var level in model.Levels.OrderBy(level => level.LevelNumber))
                await SaveLevelAsync(connection, transaction, level, cancellationToken);

            await using (var saveTemplate = connection.CreateCommand())
            {
                saveTemplate.Transaction = transaction;
                saveTemplate.CommandText = """
                    INSERT INTO spells_templates (Id, TypeId, SpellLevelsCSV)
                    VALUES (@id, @typeId, @levelsCsv)
                    ON DUPLICATE KEY UPDATE
                        TypeId = VALUES(TypeId),
                        SpellLevelsCSV = VALUES(SpellLevelsCSV);
                    """;
                saveTemplate.Parameters.AddWithValue("@id", model.Id);
                saveTemplate.Parameters.AddWithValue("@typeId", model.TypeId);
                saveTemplate.Parameters.AddWithValue("@levelsCsv", string.Join(",", model.Levels.OrderBy(level => level.LevelNumber).Select(level => level.Id)));
                await saveTemplate.ExecuteNonQueryAsync(cancellationToken);
            }

            await SaveBreedAssignmentsAsync(connection, transaction, model.Id, model.AssignedBreedIds, cancellationToken);

            await AdminEntityTextOverrideService.SaveAsync(
                connection,
                AdminEntityType.Spell,
                model.Id,
                model.OverrideName,
                model.OverrideDescription,
                transaction,
                cancellationToken);

            if (removedLevelIds.Length > 0)
            {
                await using var deleteRemovedLevels = connection.CreateCommand();
                deleteRemovedLevels.Transaction = transaction;
                deleteRemovedLevels.CommandText = $"DELETE FROM spells_levels WHERE Id IN ({string.Join(",", removedLevelIds)});";
                await deleteRemovedLevels.ExecuteNonQueryAsync(cancellationToken);
            }

            var persistentZoneSync = await SyncPersistentZonePayloadsAsync(connection, transaction, model, cancellationToken);
            infos.AddRange(persistentZoneSync.Infos);
            warnings.AddRange(persistentZoneSync.Warnings);

            await transaction.CommitAsync(cancellationToken);
            await _runtimeRevisionService.TouchAsync(connection, AdminRuntimeDomainNames.Spells, cancellationToken);

            return infos.Count == 0 && warnings.Count == 0
                ? AdminSaveResult.Empty
                : new AdminSaveResult
                {
                    Infos = infos.ToArray(),
                    Warnings = warnings.ToArray(),
                };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static SpellEditModel BuildSpecialSpellModel(AdminSpecialSpellDefinition definition, SpellEditModel? visualReference)
    {
        var effects = definition.SpellId switch
        {
            AdminSpellPresets.MatanzaSpellId => new List<GameEffectEditRow>
            {
                CreateDamageRow(EffectId.EffectDamageNeutral, 999),
                CreateDamageRow(EffectId.EffectDamageEarth, 999),
                CreateDamageRow(EffectId.EffectDamageFire, 999),
                CreateDamageRow(EffectId.EffectDamageWater, 999),
                CreateDamageRow(EffectId.EffectDamageAir, 999),
            },
            AdminSpellPresets.DoomSpellId => new List<GameEffectEditRow>
            {
                CreateDamageRow(EffectId.EffectDamageNeutral, 5000),
            },
            AdminSpellPresets.RollBackSpellId => new List<GameEffectEditRow>
            {
                CreateDamageRow(EffectId.EffectDamageNeutral, 999),
                CreateDamageRow(EffectId.EffectDamageEarth, 999),
                CreateDamageRow(EffectId.EffectDamageFire, 999),
                CreateDamageRow(EffectId.EffectDamageWater, 999),
                CreateDamageRow(EffectId.EffectDamageAir, 999),
            },
            _ => throw new InvalidOperationException($"No hay constructor de efectos para el spell staff #{definition.SpellId}."),
        };

        var level = new SpellLevelEditModel
        {
            LevelNumber = 1,
            APCost = 2,
            MinRange = 1,
            MaxRange = 8,
            CastInLine = false,
            CastTestLOS = true,
            NeedFreeCell = false,
            RangeCanBeBoosted = true,
            CriticalFailureEndsTurn = false,
            CriticalHitProbability = 0,
            CriticalFailureProbability = 0,
            MaxCastPerTurn = 0,
            MaxCastPerTarget = 0,
            MinCastInterval = 0,
            MinPlayerLevel = 1,
            Effects = effects,
            CriticalEffects = new(),
            StatesRequired = new(),
            StatesForbidden = new(),
        };

        var iconId = visualReference?.DisplayIconId
            ?? visualReference?.ClientIconId
            ?? visualReference?.ReferenceIconId
            ?? 0;

        return new SpellEditModel
        {
            Id = definition.SpellId,
            TypeId = visualReference?.TypeId ?? 0,
            TypeLabel = visualReference?.TypeLabel ?? "Custom",
            Name = definition.Name,
            Description = definition.Description,
            OverrideName = definition.Name,
            OverrideDescription = definition.Description,
            RuntimeExists = true,
            ClientIconId = iconId > 0 ? iconId : null,
            DisplayIconId = iconId > 0 ? iconId : null,
            AssignedBreedIds = definition.AssignedBreedIds.ToList(),
            Levels = new List<SpellLevelEditModel> { level },
        };
    }

    private static GameEffectEditRow CreateDamageRow(EffectId effectId, short value) =>
        new()
        {
            EffectId = effectId,
            Kind = EffectEditorKind.Dice,
            Random = 0,
            Duration = 0,
            TargetType = (SpellTargetType)32767,
            Shape = (SpellShape)76,
            ZoneSize = 63,
            Value = 0,
            MinValue = value,
            MaxValue = value,
        };

    public async Task<AdminSaveResult> SaveAsync(SpellEditModel model, CancellationToken cancellationToken = default)
    {
        var runtimeSave = await SaveRuntimeAsync(model, cancellationToken);
        var clientPublish = await _spellClientPublishService.PublishAsync(model, cancellationToken);

        return new AdminSaveResult
        {
            Infos = string.IsNullOrWhiteSpace(clientPublish.Summary)
                ? runtimeSave.Infos
                : runtimeSave.Infos.Concat(new[] { clientPublish.Summary }).ToArray(),
            Warnings = runtimeSave.Warnings.Concat(clientPublish.Warnings).ToArray(),
            Errors = runtimeSave.Errors,
        };
    }

    public async Task DeleteAsync(short spellId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var references = await GetReferencesAsync(connection, spellId, cancellationToken);
        if (references.HasBlockingReferences)
            throw new InvalidOperationException("Este hechizo todavia esta referenciado por personajes, monstruos, razas o NPCs. Limpia esas referencias antes de eliminarlo.");

        var header = await GetHeaderAsync(connection, spellId, cancellationToken);
        if (header is null)
            return;

        var levelIds = ParseLevelIds(header.Value.LevelsCsv);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using (var deleteTemplate = connection.CreateCommand())
            {
                deleteTemplate.Transaction = transaction;
                deleteTemplate.CommandText = "DELETE FROM spells_templates WHERE Id = @spellId;";
                deleteTemplate.Parameters.AddWithValue("@spellId", spellId);
                await deleteTemplate.ExecuteNonQueryAsync(cancellationToken);
            }

            if (levelIds.Length > 0)
            {
                await using var deleteLevels = connection.CreateCommand();
                deleteLevels.Transaction = transaction;
                deleteLevels.CommandText = $"DELETE FROM spells_levels WHERE Id IN ({string.Join(",", levelIds)});";
                await deleteLevels.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            await _runtimeRevisionService.TouchAsync(connection, AdminRuntimeDomainNames.Spells, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public static string FormatDisplayNameWithId(string? displayName, short spellId)
    {
        var normalized = (displayName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return $"Hechizo #{spellId}";

        if (normalized.Contains($"#{spellId}", StringComparison.OrdinalIgnoreCase))
            return normalized;

        return $"{normalized} [#{spellId}]";
    }

    private async Task<List<SpellHeaderRow>> LoadSpellHeadersAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, TypeId, SpellLevelsCSV
            FROM spells_templates
            ORDER BY Id ASC;
            """;

        var rows = new List<SpellHeaderRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SpellHeaderRow(
                reader.GetSafeInt16("Id"),
                (sbyte)reader.GetSafeInt16("TypeId"),
                reader.GetSafeString("SpellLevelsCSV")));
        }

        return rows;
    }

    private async Task<Dictionary<int, RuntimeSpellLevelSummary>> LoadRuntimeLevelSummariesAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                Id,
                APCost,
                MinRange,
                MaxRange,
                CastInLine,
                CastTestLOS,
                NeedFreeCell,
                RangeCanBeBoosted,
                CriticalFailureEndsTurn,
                CriticalHitProbability,
                CriticalFailureProbability,
                MaxCastPerTurn,
                MaxCastPerTarget,
                MinCastInterval,
                MinPlayerLevel,
                BinaryCriticalEffects,
                StatesRequiredCSV,
                StatesForbiddenCSV
            FROM spells_levels
            ORDER BY Id ASC;
            """;

        var rows = new Dictionary<int, RuntimeSpellLevelSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetSafeInt32("Id");
            rows[id] = new RuntimeSpellLevelSummary(
                id,
                reader.GetSafeByte("APCost"),
                reader.GetSafeSByte("MinRange"),
                reader.GetSafeSByte("MaxRange"),
                reader.GetSafeBoolean("CastInLine"),
                reader.GetSafeBoolean("CastTestLOS"),
                reader.GetSafeBoolean("NeedFreeCell"),
                reader.GetSafeBoolean("RangeCanBeBoosted"),
                reader.GetSafeBoolean("CriticalFailureEndsTurn"),
                reader.GetSafeSByte("CriticalHitProbability"),
                reader.GetSafeSByte("CriticalFailureProbability"),
                reader.GetSafeByte("MaxCastPerTurn"),
                reader.GetSafeByte("MaxCastPerTarget"),
                reader.GetSafeByte("MinCastInterval"),
                reader.GetSafeByte("MinPlayerLevel"),
                NormalizeCsv(reader.GetSafeString("StatesRequiredCSV")),
                NormalizeCsv(reader.GetSafeString("StatesForbiddenCSV")),
                reader.GetSafeBytes("BinaryCriticalEffects") is { Length: > 0 });
        }

        return rows;
    }

    private static bool MatchesSearch(
        string search,
        short spellId,
        ResolvedSpellIdentity identity)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        return Contains(spellId.ToString(), search) ||
               Contains(identity.DisplayTypeId.ToString(), search) ||
               Contains(identity.DisplayName, search) ||
               Contains(identity.DisplayDescription, search) ||
               Contains(identity.DisplayTypeLabel, search) ||
               Contains(identity.ReferenceName, search) ||
               Contains(identity.ReferenceDescription, search) ||
               Contains(identity.ClientName, search) ||
               Contains(identity.ClientDescription, search) ||
               Contains(identity.DisplayIconId?.ToString(), search);
    }

    private static bool Contains(string? value, string search) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(search, StringComparison.OrdinalIgnoreCase);

    private static string ResolveDisplayName(string? name, short spellId) =>
        FormatDisplayNameWithId(name, spellId);

    private async Task<SpellHeaderRow?> GetHeaderAsync(
        MySqlConnection connection,
        short spellId,
        MySqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT Id, TypeId, SpellLevelsCSV
            FROM spells_templates
            WHERE Id = @spellId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@spellId", spellId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new SpellHeaderRow(
            reader.GetSafeInt16("Id"),
            (sbyte)reader.GetSafeInt16("TypeId"),
            reader.GetSafeString("SpellLevelsCSV"));
    }

    private Task<SpellHeaderRow?> GetHeaderAsync(
        MySqlConnection connection,
        short spellId,
        CancellationToken cancellationToken) =>
        GetHeaderAsync(connection, spellId, null, cancellationToken);

    private static int[] ParseLevelIds(string levelsCsv) =>
        NormalizeCsv(levelsCsv)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var levelId) ? levelId : 0)
            .Where(levelId => levelId > 0)
            .ToArray();

    private async Task<List<SpellLevelRecord>> LoadLevelsAsync(
        MySqlConnection connection,
        IReadOnlyCollection<int> levelIds,
        MySqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (levelIds.Count == 0)
            return new List<SpellLevelRecord>();

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT * FROM spells_levels WHERE Id IN ({string.Join(",", levelIds)}) ORDER BY Id ASC;";

        var rows = new List<SpellLevelRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SpellLevelRecord
            {
                Id = reader.GetSafeInt32("Id"),
                APCost = reader.GetSafeByte("APCost"),
                MinRange = reader.GetSafeSByte("MinRange"),
                MaxRange = reader.GetSafeSByte("MaxRange"),
                CastInLine = reader.GetSafeBoolean("CastInLine"),
                CastTestLOS = reader.GetSafeBoolean("CastTestLOS"),
                NeedFreeCell = reader.GetSafeBoolean("NeedFreeCell"),
                RangeCanBeBoosted = reader.GetSafeBoolean("RangeCanBeBoosted"),
                CriticalFailureEndsTurn = reader.GetSafeBoolean("CriticalFailureEndsTurn"),
                CriticalHitProbability = reader.GetSafeSByte("CriticalHitProbability"),
                CriticalFailureProbability = reader.GetSafeSByte("CriticalFailureProbability"),
                MaxCastPerTurn = reader.GetSafeByte("MaxCastPerTurn"),
                MaxCastPerTarget = reader.GetSafeByte("MaxCastPerTarget"),
                MinCastInterval = reader.GetSafeByte("MinCastInterval"),
                MinPlayerLevel = reader.GetSafeByte("MinPlayerLevel"),
                BinaryEffects = reader.GetSafeBytes("BinaryEffects"),
                BinaryCriticalEffects = reader.GetSafeBytes("BinaryCriticalEffects"),
                StatesRequiredCSV = reader.GetSafeString("StatesRequiredCSV"),
                StatesForbiddenCSV = reader.GetSafeString("StatesForbiddenCSV"),
            });
        }

        return rows;
    }

    private Task<List<SpellLevelRecord>> LoadLevelsAsync(
        MySqlConnection connection,
        IReadOnlyCollection<int> levelIds,
        CancellationToken cancellationToken) =>
        LoadLevelsAsync(connection, levelIds, null, cancellationToken);

    private async Task<int> GetMaxLevelIdAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT IFNULL(MAX(Id), 0) FROM spells_levels;";
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar switch
        {
            int intValue => intValue,
            long longValue when longValue <= int.MaxValue => (int)longValue,
            decimal decimalValue when decimalValue <= int.MaxValue => (int)decimalValue,
            _ => 0,
        };
    }

    private static void NormalizeBreedAssignments(List<int> breedIds)
    {
        var normalized = breedIds
            .Where(breedId => breedId is >= (int)BreedEnum.Feca and <= (int)BreedEnum.Pandawa)
            .Distinct()
            .OrderBy(breedId => breedId)
            .ToList();

        breedIds.Clear();
        breedIds.AddRange(normalized);
    }

    private async Task<IReadOnlyCollection<int>> LoadAssignedBreedIdsAsync(
        MySqlConnection connection,
        short spellId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT BreedId
            FROM breeds_spells
            WHERE SpellId = @spellId
            ORDER BY BreedId ASC;
            """;
        command.Parameters.AddWithValue("@spellId", spellId);

        var breedIds = new List<int>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var breedId = reader.GetSafeInt32("BreedId");
            if (breedId is >= (int)BreedEnum.Feca and <= (int)BreedEnum.Pandawa)
                breedIds.Add(breedId);
        }

        return breedIds
            .Distinct()
            .OrderBy(breedId => breedId)
            .ToArray();
    }

    private async Task SaveBreedAssignmentsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        short spellId,
        IReadOnlyCollection<int> breedIds,
        CancellationToken cancellationToken)
    {
        await using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM breeds_spells WHERE SpellId = @spellId;";
            deleteCommand.Parameters.AddWithValue("@spellId", spellId);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        if (breedIds.Count == 0)
            return;

        foreach (var breedId in breedIds.OrderBy(value => value))
        {
            await using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = """
                INSERT INTO breeds_spells (BreedId, SpellId)
                VALUES (@breedId, @spellId);
                """;
            insertCommand.Parameters.AddWithValue("@breedId", breedId);
            insertCommand.Parameters.AddWithValue("@spellId", spellId);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task SaveLevelAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        SpellLevelEditModel level,
        CancellationToken cancellationToken)
    {
        var record = new SpellLevelRecord { Id = level.Id };
        _schemaService.ApplyLevel(level, record);
        await SaveLevelRecordAsync(connection, transaction, record, cancellationToken);
    }

    private static async Task SaveLevelRecordAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        SpellLevelRecord record,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO spells_levels
            (
                Id, APCost, MinRange, MaxRange, CastInLine, CastTestLOS,
                NeedFreeCell, RangeCanBeBoosted, CriticalFailureEndsTurn,
                CriticalHitProbability, CriticalFailureProbability,
                MaxCastPerTurn, MaxCastPerTarget, MinCastInterval, MinPlayerLevel,
                BinaryEffects, BinaryCriticalEffects, StatesRequiredCSV, StatesForbiddenCSV
            )
            VALUES
            (
                @id, @apCost, @minRange, @maxRange, @castInLine, @castTestLos,
                @needFreeCell, @rangeCanBeBoosted, @criticalFailureEndsTurn,
                @criticalHitProbability, @criticalFailureProbability,
                @maxCastPerTurn, @maxCastPerTarget, @minCastInterval, @minPlayerLevel,
                @binaryEffects, @binaryCriticalEffects, @statesRequiredCsv, @statesForbiddenCsv
            )
            ON DUPLICATE KEY UPDATE
                APCost = VALUES(APCost),
                MinRange = VALUES(MinRange),
                MaxRange = VALUES(MaxRange),
                CastInLine = VALUES(CastInLine),
                CastTestLOS = VALUES(CastTestLOS),
                NeedFreeCell = VALUES(NeedFreeCell),
                RangeCanBeBoosted = VALUES(RangeCanBeBoosted),
                CriticalFailureEndsTurn = VALUES(CriticalFailureEndsTurn),
                CriticalHitProbability = VALUES(CriticalHitProbability),
                CriticalFailureProbability = VALUES(CriticalFailureProbability),
                MaxCastPerTurn = VALUES(MaxCastPerTurn),
                MaxCastPerTarget = VALUES(MaxCastPerTarget),
                MinCastInterval = VALUES(MinCastInterval),
                MinPlayerLevel = VALUES(MinPlayerLevel),
                BinaryEffects = VALUES(BinaryEffects),
                BinaryCriticalEffects = VALUES(BinaryCriticalEffects),
                StatesRequiredCSV = VALUES(StatesRequiredCSV),
                StatesForbiddenCSV = VALUES(StatesForbiddenCSV);
            """;
        command.Parameters.AddWithValue("@id", record.Id);
        command.Parameters.AddWithValue("@apCost", record.APCost);
        command.Parameters.AddWithValue("@minRange", record.MinRange);
        command.Parameters.AddWithValue("@maxRange", record.MaxRange);
        command.Parameters.AddWithValue("@castInLine", record.CastInLine);
        command.Parameters.AddWithValue("@castTestLos", record.CastTestLOS);
        command.Parameters.AddWithValue("@needFreeCell", record.NeedFreeCell);
        command.Parameters.AddWithValue("@rangeCanBeBoosted", record.RangeCanBeBoosted);
        command.Parameters.AddWithValue("@criticalFailureEndsTurn", record.CriticalFailureEndsTurn);
        command.Parameters.AddWithValue("@criticalHitProbability", record.CriticalHitProbability);
        command.Parameters.AddWithValue("@criticalFailureProbability", record.CriticalFailureProbability);
        command.Parameters.AddWithValue("@maxCastPerTurn", record.MaxCastPerTurn);
        command.Parameters.AddWithValue("@maxCastPerTarget", record.MaxCastPerTarget);
        command.Parameters.AddWithValue("@minCastInterval", record.MinCastInterval);
        command.Parameters.AddWithValue("@minPlayerLevel", record.MinPlayerLevel);
        command.Parameters.Add("@binaryEffects", MySqlDbType.Blob).Value = record.BinaryEffects;
        command.Parameters.Add("@binaryCriticalEffects", MySqlDbType.Blob).Value = record.BinaryCriticalEffects;
        command.Parameters.AddWithValue("@statesRequiredCsv", record.StatesRequiredCSV);
        command.Parameters.AddWithValue("@statesForbiddenCsv", record.StatesForbiddenCSV);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SpellReferenceSummary> GetReferencesAsync(
        MySqlConnection connection,
        short spellId,
        CancellationToken cancellationToken)
    {
        var summary = new SpellReferenceSummary();

        summary.CharacterCount = await GetCountAsync(
            connection,
            "SELECT COUNT(*) FROM characters_spells WHERE SpellId = @spellId;",
            spellId,
            cancellationToken);
        summary.BreedCount = await GetCountAsync(
            connection,
            "SELECT COUNT(*) FROM breeds_spells WHERE SpellId = @spellId;",
            spellId,
            cancellationToken);
        summary.MonsterCount = await GetCountAsync(
            connection,
            "SELECT COUNT(*) FROM monsters_spells WHERE SpellId = @spellId;",
            spellId,
            cancellationToken);
        summary.NpcLearnReplyCount = await GetCountAsync(
            connection,
            """
            SELECT COUNT(*)
            FROM npcs_replies
            WHERE Action = 'LearnSpell'
              AND (Parameters = @spellIdText OR Parameters LIKE CONCAT(@spellIdText, ';%'));
            """,
            spellId,
            cancellationToken,
            addTextParameter: true);
        summary.MaxCharacterLevel = await GetMaxSpellLevelAsync(connection, "characters_spells", spellId, cancellationToken);
        summary.MaxMonsterLevel = await GetMaxSpellLevelAsync(connection, "monsters_spells", spellId, cancellationToken);

        return summary;
    }

    private static async Task<int> GetCountAsync(
        MySqlConnection connection,
        string sql,
        short spellId,
        CancellationToken cancellationToken,
        bool addTextParameter = false)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@spellId", spellId);
        if (addTextParameter)
            command.Parameters.AddWithValue("@spellIdText", spellId.ToString());

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar switch
        {
            int intValue => intValue,
            long longValue when longValue <= int.MaxValue => (int)longValue,
            decimal decimalValue when decimalValue <= int.MaxValue => (int)decimalValue,
            _ => 0,
        };
    }

    private static async Task<sbyte> GetMaxSpellLevelAsync(
        MySqlConnection connection,
        string tableName,
        short spellId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT IFNULL(MAX(SpellLevel), 0) FROM {tableName} WHERE SpellId = @spellId;";
        command.Parameters.AddWithValue("@spellId", spellId);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar switch
        {
            sbyte sbyteValue => sbyteValue,
            byte byteValue when byteValue <= sbyte.MaxValue => (sbyte)byteValue,
            short shortValue when shortValue <= sbyte.MaxValue => (sbyte)shortValue,
            int intValue when intValue <= sbyte.MaxValue => (sbyte)intValue,
            long longValue when longValue <= sbyte.MaxValue => (sbyte)longValue,
            decimal decimalValue when decimalValue <= sbyte.MaxValue => (sbyte)decimalValue,
            _ => 0,
        };
    }

    private void ValidateModel(SpellEditModel model)
    {
        if (model.Id <= 0)
            throw new InvalidOperationException("El hechizo necesita un Id valido de spells_templates.");

        if (model.TypeId < 0)
            throw new InvalidOperationException("El TypeId del hechizo no puede ser negativo.");

        if (!model.RuntimeExists)
            throw new InvalidOperationException("Este hechizo solo existe en referencia. No se puede guardar hasta reconstruir su runtime de forma controlada.");

        if (model.Levels.Count == 0)
            throw new InvalidOperationException("El hechizo necesita al menos un nivel.");
    }

    private List<string> BuildWarnings(
        SpellEditModel model,
        ReferenceSpellIdentity? reference,
        AdminClientSpellText clientText)
    {
        var warnings = new List<string>();

        if (reference is not null && reference.TypeId != model.TypeId)
        {
            warnings.Add(
                $"La referencia sana resuelve el hechizo #{model.Id} con TypeId {reference.TypeId}, pero guardaste {model.TypeId}. Revisa si realmente querias desalinearlo del catalogo clasico.");
        }
        else if (reference is null && clientText.TypeId.HasValue && clientText.TypeId.Value != model.TypeId)
        {
            warnings.Add(
                $"El fallback cliente resuelve el hechizo #{model.Id} con TypeId {clientText.TypeId}, pero guardaste {model.TypeId}. Verifica si el cambio es intencional.");
        }

        var levelsWithoutEffects = model.Levels
            .Where(level => level.Effects.Count == 0)
            .Select(level => level.LevelNumber)
            .ToArray();
        if (levelsWithoutEffects.Length > 0)
        {
            warnings.Add($"Hay niveles sin efectos normales configurados: {string.Join(", ", levelsWithoutEffects)}.");
        }

        var inconsistentCrits = model.Levels
            .Where(level => level.CriticalEffects.Count > 0 && level.CriticalHitProbability <= 0)
            .Select(level => level.LevelNumber)
            .ToArray();
        if (inconsistentCrits.Length > 0)
        {
            warnings.Add($"Hay efectos criticos en niveles sin probabilidad critica positiva: {string.Join(", ", inconsistentCrits)}.");
        }

        if (reference?.HasClassicBreedLevels == true && model.AssignedBreedIds.Count == 0)
        {
            warnings.Add("La referencia clasica asocia este hechizo a una o mas razas, pero no quedo asignado a ninguna clase en breeds_spells.");
        }

        warnings.AddRange(BuildPersistentZoneWarnings(model));

        return warnings;
    }

    private static IEnumerable<string> BuildPersistentZoneWarnings(SpellEditModel model)
    {
        foreach (var level in model.Levels.OrderBy(level => level.LevelNumber))
        {
            var normalWarning = BuildPersistentZoneWarning(model.Id, level.LevelNumber, false, level.Effects);
            if (!string.IsNullOrWhiteSpace(normalWarning))
                yield return normalWarning;

            var criticalWarning = BuildPersistentZoneWarning(model.Id, level.LevelNumber, true, level.CriticalEffects);
            if (!string.IsNullOrWhiteSpace(criticalWarning))
                yield return criticalWarning;
        }
    }

    private static string? BuildPersistentZoneWarning(short spellId, int levelNumber, bool critical, IReadOnlyList<GameEffectEditRow> effects)
    {
        var payloadCount = effects.Count(effect => !IsPersistentZoneContainerEffect(effect.EffectId));
        if (payloadCount == 0)
            return null;

        var links = effects
            .Select(TryParsePersistentZoneLink)
            .Where(link => link is not null)
            .Select(link => link!.Value)
            .Distinct()
            .ToArray();

        if (links.Length == 0)
            return null;

        var segment = critical ? "critico" : "normal";
        if (links.Length > 1)
        {
            return $"Nivel {levelNumber} ({segment}) del hechizo #{spellId}: hay {payloadCount} efecto(s) extra junto a multiples links de glifo/trampa. La sincronizacion runtime queda ambigua y no se aplicara automaticamente.";
        }

        var link = links[0];
        return $"Nivel {levelNumber} ({segment}) del hechizo #{spellId}: los {payloadCount} efecto(s) extra se sincronizaran al spell persistente interno #{link.LinkedSpellId} nivel {link.LinkedLevelNumber}, que es el que realmente se ejecuta al triggerear la zona.";
    }

    private async Task<PersistentZoneSyncResult> SyncPersistentZonePayloadsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        SpellEditModel model,
        CancellationToken cancellationToken)
    {
        var result = new PersistentZoneSyncResult();
        var currentPlans = BuildPersistentZoneSyncPlans(model);
        var existingStates = await LoadPersistentZoneSyncStatesAsync(connection, transaction, model.Id, cancellationToken);

        var allKeys = currentPlans.Keys
            .Union(existingStates.Keys)
            .OrderBy(key => key.LevelNumber)
            .ThenBy(key => key.IsCritical)
            .ToArray();

        foreach (var key in allKeys)
        {
            currentPlans.TryGetValue(key, out var plan);
            existingStates.TryGetValue(key, out var state);

            if (plan is not null && plan.IsAmbiguous)
            {
                if (!string.IsNullOrWhiteSpace(plan.Warning))
                    result.Warnings.Add(plan.Warning);

                continue;
            }

            if (plan is null)
            {
                if (state is not null)
                {
                    var removal = await ApplyPersistentZonePayloadDiffAsync(
                        connection,
                        transaction,
                        state.LinkedSpellId,
                        state.LinkedLevelNumber,
                        key.IsCritical,
                        SpellTargetType.None,
                        state.Payload,
                        Array.Empty<GameEffectEditRow>(),
                        cancellationToken);

                    result.Warnings.AddRange(removal.Warnings);
                    if (removal.Applied)
                    {
                        result.Infos.Add(
                            $"Se limpio la sincronizacion persistente previa del hechizo #{model.Id}, nivel {key.LevelNumber} {(key.IsCritical ? "critico" : "normal")}.");
                    }

                    await DeletePersistentZoneSyncStateAsync(connection, transaction, model.Id, key, cancellationToken);
                }

                continue;
            }

            if (state is not null)
            {
                var previousRemoval = await ApplyPersistentZonePayloadDiffAsync(
                    connection,
                    transaction,
                    state.LinkedSpellId,
                    state.LinkedLevelNumber,
                    key.IsCritical,
                    SpellTargetType.None,
                    state.Payload,
                    Array.Empty<GameEffectEditRow>(),
                    cancellationToken);

                result.Warnings.AddRange(previousRemoval.Warnings);
            }

            if (plan.Payload.Count == 0)
            {
                if (state is not null)
                    await DeletePersistentZoneSyncStateAsync(connection, transaction, model.Id, key, cancellationToken);

                continue;
            }

            var applyResult = await ApplyPersistentZonePayloadDiffAsync(
                connection,
                transaction,
                plan.LinkedSpellId,
                plan.LinkedLevelNumber,
                key.IsCritical,
                plan.DefaultTargetType,
                Array.Empty<GameEffectEditRow>(),
                plan.Payload,
                cancellationToken);

            result.Warnings.AddRange(applyResult.Warnings);

            if (applyResult.Applied)
            {
                result.Infos.Add(
                    $"Hechizo #{model.Id}, nivel {key.LevelNumber} {(key.IsCritical ? "critico" : "normal")}: payload persistente sincronizado hacia #{plan.LinkedSpellId} nivel {plan.LinkedLevelNumber} (efectos finales: {applyResult.FinalEffectCount}, targets nuevos: {applyResult.TargetSummary}).");
            }

            if (applyResult.SynchronizedPayload.Count > 0)
            {
                await SavePersistentZoneSyncStateAsync(
                    connection,
                    transaction,
                    model.Id,
                    plan with { Payload = applyResult.SynchronizedPayload },
                    cancellationToken);
            }
            else
            {
                await DeletePersistentZoneSyncStateAsync(connection, transaction, model.Id, key, cancellationToken);
            }
        }

        return result;
    }

    private Dictionary<PersistentZoneSyncKey, PersistentZoneSyncPlan> BuildPersistentZoneSyncPlans(SpellEditModel model)
    {
        var plans = new Dictionary<PersistentZoneSyncKey, PersistentZoneSyncPlan>();

        foreach (var level in model.Levels)
        {
            AddPersistentZoneSyncPlan(plans, model.Id, level.LevelNumber, false, level.Effects);
            AddPersistentZoneSyncPlan(plans, model.Id, level.LevelNumber, true, level.CriticalEffects);
        }

        return plans;
    }

    private void AddPersistentZoneSyncPlan(
        Dictionary<PersistentZoneSyncKey, PersistentZoneSyncPlan> plans,
        short outerSpellId,
        int levelNumber,
        bool isCritical,
        IReadOnlyList<GameEffectEditRow> effects)
    {
        var key = new PersistentZoneSyncKey(levelNumber, isCritical);
        var payload = effects
            .Where(effect => !IsPersistentZoneContainerEffect(effect.EffectId))
            .Select(CloneEffect)
            .ToList();
        var links = effects
            .Select(TryParsePersistentZoneLink)
            .Where(link => link is not null)
            .Select(link => link!.Value)
            .Distinct()
            .ToArray();

        if (links.Length == 0)
            return;

        if (links.Length > 1)
        {
            plans[key] = PersistentZoneSyncPlan.CreateAmbiguous(
                key,
                outerSpellId,
                payload,
                $"Hechizo #{outerSpellId}, nivel {levelNumber} {(isCritical ? "critico" : "normal")}: hay multiples links persistentes y no es seguro elegir automaticamente el spell interno correcto.");
            return;
        }

        var link = links[0];
        plans[key] = PersistentZoneSyncPlan.Create(
            key,
            outerSpellId,
            link.LinkedSpellId,
            link.LinkedLevelNumber,
            link.DefaultTargetType,
            payload);
    }

    private async Task<ApplyPersistentZonePayloadResult> ApplyPersistentZonePayloadDiffAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        short linkedSpellId,
        int linkedLevelNumber,
        bool isCritical,
        SpellTargetType fallbackTargetType,
        IReadOnlyList<GameEffectEditRow> removePayload,
        IReadOnlyList<GameEffectEditRow> addPayload,
        CancellationToken cancellationToken)
    {
        var result = new ApplyPersistentZonePayloadResult();

        if (linkedSpellId <= 0 || linkedLevelNumber <= 0)
        {
            result.Warnings.Add("Se detecto un link persistente invalido (SpellId/Level <= 0).");
            return result;
        }

        var header = await GetHeaderAsync(connection, linkedSpellId, transaction, cancellationToken);
        if (header is null)
        {
            result.Warnings.Add($"No se encontro el spell interno #{linkedSpellId} para sincronizar payload persistente.");
            return result;
        }

        var linkedLevelIds = ParseLevelIds(header.Value.LevelsCsv);
        if (linkedLevelNumber > linkedLevelIds.Length)
        {
            result.Warnings.Add($"El spell interno #{linkedSpellId} no tiene nivel {linkedLevelNumber}.");
            return result;
        }

        var levelId = linkedLevelIds[linkedLevelNumber - 1];
        var records = await LoadLevelsAsync(connection, new[] { levelId }, transaction, cancellationToken);
        var record = records.FirstOrDefault();
        if (record is null)
        {
            result.Warnings.Add($"No se pudo cargar spells_levels.Id {levelId} del spell interno #{linkedSpellId}.");
            return result;
        }

        var model = _schemaService.MapLevel(record, linkedLevelNumber);
        NormalizeSpellLevel(model);

        var targetEffects = isCritical ? model.CriticalEffects : model.Effects;
        var changed = false;

        if (removePayload.Count > 0)
        {
            var filtered = targetEffects
                .Where(existing => !removePayload.Any(remove => AreEffectRowsEquivalent(existing, remove)))
                .ToList();

            changed = filtered.Count != targetEffects.Count;
            if (changed)
            {
                targetEffects.Clear();
                targetEffects.AddRange(filtered);
            }
        }

        foreach (var effect in addPayload)
        {
            if (targetEffects.Any(existing => AreEffectRowsEquivalent(existing, effect)))
                continue;

            var cloned = CloneEffect(effect);
            if (cloned.TargetType is SpellTargetType.None && fallbackTargetType is not SpellTargetType.None)
                cloned.TargetType = fallbackTargetType;
            targetEffects.Add(cloned);
            result.SynchronizedPayload.Add(CloneEffect(cloned));
            result.InsertedTargetTypes.Add(cloned.TargetType);
            changed = true;
        }

        if (!changed)
            return result;

        NormalizeSpellEffects(targetEffects);
        _schemaService.ApplyLevel(model, record);
        await SaveLevelRecordAsync(connection, transaction, record, cancellationToken);
        result.Applied = true;
        result.FinalEffectCount = targetEffects.Count;
        return result;
    }

    private async Task<Dictionary<PersistentZoneSyncKey, PersistentZoneSyncState>> LoadPersistentZoneSyncStatesAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        short outerSpellId,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<PersistentZoneSyncKey, PersistentZoneSyncState>();

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT OuterLevelNumber, EffectSide, LinkedSpellId, LinkedLevelNumber, PayloadJson
            FROM admin_spell_trigger_payload_sync
            WHERE OuterSpellId = @outerSpellId;
            """;
        command.Parameters.AddWithValue("@outerSpellId", outerSpellId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var key = new PersistentZoneSyncKey(
                reader.GetSafeByte("OuterLevelNumber"),
                string.Equals(reader.GetSafeString("EffectSide"), "critical", StringComparison.OrdinalIgnoreCase));

            var payload = DeserializePersistentZonePayload(reader.GetSafeString("PayloadJson"));
            result[key] = new PersistentZoneSyncState(
                reader.GetSafeInt16("LinkedSpellId"),
                reader.GetSafeByte("LinkedLevelNumber"),
                payload);
        }

        return result;
    }

    private async Task SavePersistentZoneSyncStateAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        short outerSpellId,
        PersistentZoneSyncPlan plan,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO admin_spell_trigger_payload_sync (
                OuterSpellId,
                OuterLevelNumber,
                EffectSide,
                LinkedSpellId,
                LinkedLevelNumber,
                PayloadJson
            )
            VALUES (
                @outerSpellId,
                @outerLevelNumber,
                @effectSide,
                @linkedSpellId,
                @linkedLevelNumber,
                @payloadJson
            )
            ON DUPLICATE KEY UPDATE
                LinkedSpellId = VALUES(LinkedSpellId),
                LinkedLevelNumber = VALUES(LinkedLevelNumber),
                PayloadJson = VALUES(PayloadJson);
            """;
        command.Parameters.AddWithValue("@outerSpellId", outerSpellId);
        command.Parameters.AddWithValue("@outerLevelNumber", plan.Key.LevelNumber);
        command.Parameters.AddWithValue("@effectSide", plan.Key.IsCritical ? "critical" : "normal");
        command.Parameters.AddWithValue("@linkedSpellId", plan.LinkedSpellId);
        command.Parameters.AddWithValue("@linkedLevelNumber", plan.LinkedLevelNumber);
        command.Parameters.AddWithValue("@payloadJson", SerializePersistentZonePayload(plan.Payload));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task DeletePersistentZoneSyncStateAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        short outerSpellId,
        PersistentZoneSyncKey key,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM admin_spell_trigger_payload_sync
            WHERE OuterSpellId = @outerSpellId
              AND OuterLevelNumber = @outerLevelNumber
              AND EffectSide = @effectSide;
            """;
        command.Parameters.AddWithValue("@outerSpellId", outerSpellId);
        command.Parameters.AddWithValue("@outerLevelNumber", key.LevelNumber);
        command.Parameters.AddWithValue("@effectSide", key.IsCritical ? "critical" : "normal");
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool IsPersistentZoneContainerEffect(EffectId effectId) =>
        effectId is EffectId.EffectGlyph or EffectId.EffectGlyph402 or EffectId.EffectTrap;

    private static PersistentZoneLink? TryParsePersistentZoneLink(GameEffectEditRow effect)
    {
        if (!IsPersistentZoneContainerEffect(effect.EffectId) || effect.MinValue <= 0 || effect.MaxValue <= 0)
            return null;

        return new PersistentZoneLink((short)effect.MinValue, effect.MaxValue, effect.TargetType);
    }

    private static string SerializePersistentZonePayload(IReadOnlyList<GameEffectEditRow> payload) =>
        JsonSerializer.Serialize(payload.ToArray(), PersistentZoneSyncJsonOptions);

    private static List<GameEffectEditRow> DeserializePersistentZonePayload(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<GameEffectEditRow>();

        return JsonSerializer.Deserialize<List<GameEffectEditRow>>(json, PersistentZoneSyncJsonOptions) ?? new List<GameEffectEditRow>();
    }

    private static bool AreEffectRowsEquivalent(GameEffectEditRow left, GameEffectEditRow right) =>
        left.EffectId == right.EffectId &&
        left.Kind == right.Kind &&
        left.Random == right.Random &&
        left.Duration == right.Duration &&
        left.TargetType == right.TargetType &&
        left.Shape == right.Shape &&
        left.ZoneSize == right.ZoneSize &&
        left.Value == right.Value &&
        left.MinValue == right.MinValue &&
        left.MaxValue == right.MaxValue &&
        string.Equals(left.TextValue ?? string.Empty, right.TextValue ?? string.Empty, StringComparison.Ordinal) &&
        left.DurationDays == right.DurationDays &&
        left.DurationHours == right.DurationHours &&
        left.DurationMinutes == right.DurationMinutes &&
        Nullable.Equals(left.DateValue, right.DateValue) &&
        left.MountId == right.MountId &&
        left.MountExpirationDate.Equals(right.MountExpirationDate) &&
        left.MountModelId == right.MountModelId;

    private GameEffectEditRow CloneEffect(GameEffectEditRow effect)
    {
        var clone = new GameEffectEditRow
        {
            EffectId = effect.EffectId,
            Kind = effect.Kind,
            Random = effect.Random,
            Duration = effect.Duration,
            TargetType = effect.TargetType,
            Shape = effect.Shape,
            ZoneSize = effect.ZoneSize,
            Value = effect.Value,
            MinValue = effect.MinValue,
            MaxValue = effect.MaxValue,
            TextValue = effect.TextValue,
            DurationDays = effect.DurationDays,
            DurationHours = effect.DurationHours,
            DurationMinutes = effect.DurationMinutes,
            DateValue = effect.DateValue,
            MountId = effect.MountId,
            MountExpirationDate = effect.MountExpirationDate,
            MountModelId = effect.MountModelId,
        };
        _effectEditorService.UpdateDisplay(clone);
        return clone;
    }

    private void NormalizeLevels(List<SpellLevelEditModel> levels)
    {
        var ordered = levels
            .OrderBy(level => level.LevelNumber <= 0 ? int.MaxValue : level.LevelNumber)
            .ThenBy(level => level.Id)
            .ToList();

        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].LevelNumber = index + 1;
            NormalizeSpellLevel(ordered[index]);
        }

        levels.Clear();
        levels.AddRange(ordered);
    }

    private void NormalizeSpellLevel(SpellLevelEditModel level)
    {
        if (level.MaxRange < level.MinRange)
            level.MaxRange = level.MinRange;

        var required = level.StatesRequired
            .Where(value => value > 0)
            .Distinct()
            .OrderBy(value => value)
            .ToList();
        var forbidden = level.StatesForbidden
            .Where(value => value > 0)
            .Distinct()
            .Where(value => !required.Contains(value))
            .OrderBy(value => value)
            .ToList();
        level.StatesRequired = required;
        level.StatesForbidden = forbidden;

        NormalizeSpellEffects(level.Effects);
        NormalizeSpellEffects(level.CriticalEffects);
    }

    private void NormalizeSpellEffects(List<GameEffectEditRow> rows)
    {
        foreach (var row in rows)
        {
            row.Kind = EffectEditorKind.Dice;
            _effectEditorService.UpdateDisplay(row);
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

    private ResolvedSpellIdentity ResolveIdentity(
        short spellId,
        SpellHeaderRow? runtimeHeader,
        ReferenceSpellIdentity? reference,
        AdminEntityTextOverride? textOverride,
        AdminClientSpellText client)
    {
        var overrideName = (textOverride?.DisplayName ?? string.Empty).Trim();
        var overrideDescription = (textOverride?.Description ?? string.Empty).Trim();
        var referenceName = reference?.Name ?? string.Empty;
        var referenceDescription = reference?.Description ?? string.Empty;
        var clientName = client.Name;
        var clientDescription = client.Description;

        var displayName = FirstNonEmpty(overrideName, referenceName, clientName);
        var displayDescription = FirstNonEmpty(overrideDescription, referenceDescription, clientDescription);
        var displayTypeId = reference?.TypeId
            ?? (client.TypeId is short clientTypeId
                ? SafeSByte(clientTypeId)
                : runtimeHeader?.TypeId ?? 0);
        var displayTypeLabel = !string.IsNullOrWhiteSpace(reference?.TypeLabel)
            ? reference.TypeLabel
            : !string.IsNullOrWhiteSpace(client.TypeLabel)
                ? client.TypeLabel
                : GetRuntimeTypeLabel(displayTypeId);
        var displayIconId = reference?.IconId > 0
            ? reference.IconId
            : client.IconId;

        var identitySourceLabel = !string.IsNullOrWhiteSpace(overrideName) || !string.IsNullOrWhiteSpace(overrideDescription)
            ? reference is not null
                ? "Override admin + referencia sana"
                : "Override admin"
            : reference is not null
                ? "Referencia sana"
                : !string.IsNullOrWhiteSpace(client.Name) || !string.IsNullOrWhiteSpace(client.Description) || client.IconId.HasValue
                    ? "Fallback cliente"
                    : "Runtime minimo";

        return new ResolvedSpellIdentity(
            FormatDisplayNameWithId(displayName, spellId),
            displayDescription,
            displayTypeId,
            displayTypeLabel,
            displayIconId,
            identitySourceLabel,
            referenceName,
            referenceDescription,
            clientName,
            clientDescription);
    }

    private SpellAuditSnapshot BuildAudit(
        short spellId,
        ClassicSpellDomainSnapshot domain,
        SpellHeaderRow? runtimeHeader,
        ReferenceSpellIdentity? reference,
        IReadOnlyList<RuntimeSpellLevelSummary> runtimeLevels)
    {
        var classification = domain.Classify(spellId);
        var headerDiffs = new List<string>();
        var levelDiffs = new List<string>();
        var runtimeExists = runtimeHeader is not null;
        var hasReferenceIdentity = reference is not null;
        var runtimeLevelCount = runtimeLevels.Count;
        var referenceLevelCount = reference?.OrderedLevelIds.Count ?? 0;

        if (runtimeHeader is SpellHeaderRow runtimeSpellHeader && reference is not null)
        {
            if (runtimeSpellHeader.TypeId != reference.TypeId)
            {
                headerDiffs.Add($"TypeId runtime {runtimeSpellHeader.TypeId} vs referencia {reference.TypeId}.");
            }

            var runtimeLevelsCsv = NormalizeCsv(runtimeSpellHeader.LevelsCsv);
            var referenceLevelsCsv = NormalizeCsv(reference.SpellLevelsIdsCsv);
            if (!string.Equals(runtimeLevelsCsv, referenceLevelsCsv, StringComparison.Ordinal))
            {
                headerDiffs.Add($"Orden de niveles runtime [{runtimeLevelsCsv}] vs referencia [{referenceLevelsCsv}].");
            }

            if (runtimeLevelCount != referenceLevelCount)
            {
                headerDiffs.Add($"Cantidad de niveles runtime {runtimeLevelCount} vs referencia {referenceLevelCount}.");
            }

            var pairedLevelCount = Math.Min(runtimeLevelCount, referenceLevelCount);
            for (var index = 0; index < pairedLevelCount; index++)
            {
                var runtimeLevel = runtimeLevels[index];
                var referenceLevelId = reference.OrderedLevelIds[index];
                if (!reference.LevelsById.TryGetValue(referenceLevelId, out var referenceLevel))
                {
                    levelDiffs.Add($"Nivel {index + 1}: la referencia no pudo resolver el levelId {referenceLevelId}.");
                    continue;
                }

                CompareLevelField(levelDiffs, index + 1, "APCost", runtimeLevel.APCost, referenceLevel.APCost);
                CompareLevelField(levelDiffs, index + 1, "MinRange", runtimeLevel.MinRange, referenceLevel.MinRange);
                CompareLevelField(levelDiffs, index + 1, "MaxRange", runtimeLevel.MaxRange, referenceLevel.MaxRange);
                CompareLevelField(levelDiffs, index + 1, "CastInLine", runtimeLevel.CastInLine, referenceLevel.CastInLine);
                CompareLevelField(levelDiffs, index + 1, "CastTestLOS", runtimeLevel.CastTestLos, referenceLevel.CastTestLos);
                CompareLevelField(levelDiffs, index + 1, "NeedFreeCell", runtimeLevel.NeedFreeCell, referenceLevel.NeedFreeCell);
                CompareLevelField(levelDiffs, index + 1, "RangeCanBeBoosted", runtimeLevel.RangeCanBeBoosted, referenceLevel.RangeCanBeBoosted);
                CompareLevelField(levelDiffs, index + 1, "CriticalFailureEndsTurn", runtimeLevel.CriticalFailureEndsTurn, referenceLevel.CriticalFailureEndsTurn);
                CompareLevelField(levelDiffs, index + 1, "CriticalHitProbability", runtimeLevel.CriticalHitProbability, referenceLevel.CriticalHitProbability);
                CompareLevelField(levelDiffs, index + 1, "CriticalFailureProbability", runtimeLevel.CriticalFailureProbability, referenceLevel.CriticalFailureProbability);
                CompareLevelField(levelDiffs, index + 1, "MaxCastPerTurn", runtimeLevel.MaxCastPerTurn, referenceLevel.MaxCastPerTurn);
                CompareLevelField(levelDiffs, index + 1, "MaxCastPerTarget", runtimeLevel.MaxCastPerTarget, referenceLevel.MaxCastPerTarget);
                CompareLevelField(levelDiffs, index + 1, "MinCastInterval", runtimeLevel.MinCastInterval, referenceLevel.MinCastInterval);
                CompareLevelField(levelDiffs, index + 1, "MinPlayerLevel", runtimeLevel.MinPlayerLevel, referenceLevel.MinPlayerLevel);
                CompareLevelField(levelDiffs, index + 1, "StatesRequiredCSV", runtimeLevel.StatesRequiredCsv, referenceLevel.StatesRequiredCsv);
                CompareLevelField(levelDiffs, index + 1, "StatesForbiddenCSV", runtimeLevel.StatesForbiddenCsv, referenceLevel.StatesForbiddenCsv);
            }

            if (runtimeLevelCount < referenceLevelCount)
            {
                foreach (var missingLevelId in reference.OrderedLevelIds.Skip(runtimeLevelCount))
                    levelDiffs.Add($"Falta en runtime el nivel esperado {missingLevelId}.");
            }

            if (runtimeLevelCount > referenceLevelCount)
            {
                foreach (var extraLevel in runtimeLevels.Skip(referenceLevelCount))
                    levelDiffs.Add($"Runtime tiene un nivel extra {extraLevel.Id} sin contraparte en referencia.");
            }
        }

        var metadataMissing = reference is null ||
                              string.IsNullOrWhiteSpace(reference.Name) ||
                              string.IsNullOrWhiteSpace(reference.Description) ||
                              string.IsNullOrWhiteSpace(reference.TypeLabel) ||
                              reference.IconId <= 0;

        var status = classification switch
        {
            SpellDomainClassification.ExcludedModernClass => SpellAuditStatus.ExcludedModernClass,
            _ when !runtimeExists && hasReferenceIdentity => SpellAuditStatus.MissingRuntime,
            _ when !runtimeExists => SpellAuditStatus.Ambiguous,
            _ when !hasReferenceIdentity => SpellAuditStatus.Legacy,
            _ when headerDiffs.Count > 0 || levelDiffs.Count > 0 => SpellAuditStatus.RuntimeDrift,
            _ when metadataMissing => SpellAuditStatus.MetadataMissing,
            _ => SpellAuditStatus.Aligned,
        };

        return new SpellAuditSnapshot
        {
            Status = status,
            StatusLabel = GetAuditStatusLabel(status),
            Summary = BuildAuditSummary(status, classification, headerDiffs.Count, levelDiffs.Count),
            DomainLabel = GetDomainLabel(classification),
            IdentitySourceLabel = string.Empty,
            IsClassicDomain = classification == SpellDomainClassification.ClassicClass,
            IsSupportOrCommon = classification == SpellDomainClassification.SupportOrCommon,
            IsRuntimeAvailable = runtimeExists,
            HasReferenceIdentity = hasReferenceIdentity,
            IsExcludedModernClass = classification == SpellDomainClassification.ExcludedModernClass,
            IsLegacy = status == SpellAuditStatus.Legacy,
            IsAmbiguous = classification == SpellDomainClassification.Ambiguous,
            RuntimeLevelCount = runtimeLevelCount,
            ReferenceLevelCount = referenceLevelCount,
            HeaderDifferences = headerDiffs,
            LevelDifferences = levelDiffs,
        };
    }

    private string GetRuntimeTypeLabel(sbyte typeId)
    {
        var referenceLabel = _referenceCatalogService.GetTypeLabel(typeId);
        return !string.IsNullOrWhiteSpace(referenceLabel)
            ? referenceLabel
            : _clientLocalizationService.GetTypeLabel(typeId);
    }

    private static string GetBreedLabel(BreedEnum breed) =>
        breed switch
        {
            BreedEnum.Cra => "Ocra",
            BreedEnum.Ecaflip => "Zurcarak",
            BreedEnum.Eniripsa => "Aniripsa",
            BreedEnum.Enutrof => "Anutrof",
            BreedEnum.Feca => "Feca",
            BreedEnum.Iop => "Yopuka",
            BreedEnum.Osamodas => "Osamodas",
            BreedEnum.Pandawa => "Pandawa",
            BreedEnum.Sacrieur => "Sacrogrito",
            BreedEnum.Sadida => "Sadida",
            BreedEnum.Sram => "Sram",
            BreedEnum.Xelor => "Xelor",
            _ => breed.ToString(),
        };

    private static RuntimeSpellLevelSummary MapRuntimeLevelSummary(SpellLevelRecord level) =>
        new(
            level.Id,
            level.APCost,
            level.MinRange,
            level.MaxRange,
            level.CastInLine,
            level.CastTestLOS,
            level.NeedFreeCell,
            level.RangeCanBeBoosted,
            level.CriticalFailureEndsTurn,
            level.CriticalHitProbability,
            level.CriticalFailureProbability,
            level.MaxCastPerTurn,
            level.MaxCastPerTarget,
            level.MinCastInterval,
            level.MinPlayerLevel,
            NormalizeCsv(level.StatesRequiredCSV),
            NormalizeCsv(level.StatesForbiddenCSV),
            level.BinaryCriticalEffects is { Length: > 0 });

    private static void CompareLevelField<T>(
        ICollection<string> levelDiffs,
        int levelNumber,
        string fieldName,
        T runtimeValue,
        T referenceValue)
        where T : IEquatable<T>
    {
        if (runtimeValue.Equals(referenceValue))
            return;

        levelDiffs.Add($"Nivel {levelNumber}: {fieldName} runtime {runtimeValue} vs referencia {referenceValue}.");
    }

    private static string BuildAuditSummary(
        SpellAuditStatus status,
        SpellDomainClassification classification,
        int headerDiffCount,
        int levelDiffCount)
    {
        return status switch
        {
            SpellAuditStatus.Aligned => "Identidad sana y comportamiento runtime alineados dentro del dominio clasico.",
            SpellAuditStatus.Legacy => "Hechizo runtime legado: se conserva por Id y sigue administrable aunque la referencia moderna no lo cubra bien.",
            SpellAuditStatus.MetadataMissing => classification == SpellDomainClassification.SupportOrCommon
                ? "Spell runtime de soporte/comun sin metadata sana suficiente en la referencia."
                : "La referencia existe, pero sigue faltando parte de la identidad sana (texto, icono o tipo).",
            SpellAuditStatus.RuntimeDrift => $"Se detectaron {headerDiffCount} diferencia(s) de template y {levelDiffCount} diferencia(s) en niveles compartidos.",
            SpellAuditStatus.MissingRuntime => "La referencia sana clasica existe, pero el runtime actual no tiene template ni niveles cargados.",
            SpellAuditStatus.ExcludedModernClass => "Hechizo moderno excluido del dominio clasico del servidor.",
            _ => "No hay evidencia suficiente para clasificar el spell de forma segura con la referencia y el runtime actuales.",
        };
    }

    private static string GetAuditStatusLabel(SpellAuditStatus status) =>
        status switch
        {
            SpellAuditStatus.Aligned => "aligned",
            SpellAuditStatus.Legacy => "legacy",
            SpellAuditStatus.MetadataMissing => "metadata-missing",
            SpellAuditStatus.RuntimeDrift => "runtime-drift",
            SpellAuditStatus.MissingRuntime => "missing-runtime",
            SpellAuditStatus.ExcludedModernClass => "excluded-modern-class",
            _ => "ambiguous",
        };

    private static string GetDomainLabel(SpellDomainClassification classification) =>
        classification switch
        {
            SpellDomainClassification.ClassicClass => "Clase clasica",
            SpellDomainClassification.SupportOrCommon => "Soporte / comun runtime",
            SpellDomainClassification.ExcludedModernClass => "Clase moderna excluida",
            _ => "Ambiguo",
        };

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim()
        ?? string.Empty;

    private static sbyte SafeSByte(short value) =>
        value switch
        {
            > sbyte.MaxValue => sbyte.MaxValue,
            < sbyte.MinValue => sbyte.MinValue,
            _ => (sbyte)value,
        };

    private static string NormalizeCsv(string? csv) =>
        string.Join(
            ",",
            (csv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value)));

    private readonly record struct SpellHeaderRow(short Id, sbyte TypeId, string LevelsCsv);

    private readonly record struct RuntimeSpellLevelSummary(
        int Id,
        int APCost,
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
        string StatesRequiredCsv,
        string StatesForbiddenCsv,
        bool HasCriticalEffects);

    private sealed record ResolvedSpellIdentity(
        string DisplayName,
        string DisplayDescription,
        sbyte DisplayTypeId,
        string DisplayTypeLabel,
        int? DisplayIconId,
        string IdentitySourceLabel,
        string ReferenceName,
        string ReferenceDescription,
        string ClientName,
        string ClientDescription);

    private readonly record struct PersistentZoneSyncKey(int LevelNumber, bool IsCritical);

    private readonly record struct PersistentZoneLink(short LinkedSpellId, int LinkedLevelNumber, SpellTargetType DefaultTargetType);

    private sealed record PersistentZoneSyncState(
        short LinkedSpellId,
        int LinkedLevelNumber,
        List<GameEffectEditRow> Payload);

    private sealed record PersistentZoneSyncPlan(
        PersistentZoneSyncKey Key,
        short OuterSpellId,
        short LinkedSpellId,
        int LinkedLevelNumber,
        SpellTargetType DefaultTargetType,
        List<GameEffectEditRow> Payload,
        bool IsAmbiguous,
        string Warning)
    {
        public static PersistentZoneSyncPlan Create(
            PersistentZoneSyncKey key,
            short outerSpellId,
            short linkedSpellId,
            int linkedLevelNumber,
            SpellTargetType defaultTargetType,
            List<GameEffectEditRow> payload) =>
            new(key, outerSpellId, linkedSpellId, linkedLevelNumber, defaultTargetType, payload, false, string.Empty);

        public static PersistentZoneSyncPlan CreateAmbiguous(
            PersistentZoneSyncKey key,
            short outerSpellId,
            List<GameEffectEditRow> payload,
            string warning) =>
            new(key, outerSpellId, 0, 0, SpellTargetType.None, payload, true, warning);
    }

    private sealed class PersistentZoneSyncResult
    {
        public List<string> Infos { get; } = new();

        public List<string> Warnings { get; } = new();
    }

    private sealed class ApplyPersistentZonePayloadResult
    {
        public bool Applied { get; set; }

        public int FinalEffectCount { get; set; }

        public List<GameEffectEditRow> SynchronizedPayload { get; } = new();

        public List<SpellTargetType> InsertedTargetTypes { get; } = new();

        public string TargetSummary =>
            InsertedTargetTypes.Count == 0
                ? "-"
                : string.Join(", ", InsertedTargetTypes.Select(targetType => targetType.ToString()));

        public List<string> Warnings { get; } = new();
    }
}
