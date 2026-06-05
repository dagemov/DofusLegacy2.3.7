using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Characters;
using Rollback.Admin.Models.Common;
using Rollback.Protocol.Enums;

namespace Rollback.Admin.Services;

public sealed class CharacterAdminService
{
    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly AdminSpecialSpellAssignmentService _specialSpellAssignments;
    private readonly SpellAdminService _spellAdminService;

    public CharacterAdminService(
        AdminDbConnectionFactory connectionFactory,
        AdminSpecialSpellAssignmentService specialSpellAssignments,
        SpellAdminService spellAdminService)
    {
        _connectionFactory = connectionFactory;
        _specialSpellAssignments = specialSpellAssignments;
        _spellAdminService = spellAdminService;
    }

    public async Task<AdminPagedResult<AdminCharacterListItem>> GetPagedAsync(AdminPagedQuery query, CancellationToken cancellationToken = default)
    {
        await EnsureSpecialSpellDefinitionsAsync(cancellationToken);

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await _specialSpellAssignments.EnsureSchemaAndRecoverLegacyAssignmentsAsync(connection, cancellationToken);

        var normalized = Normalize(query);
        var search = normalized.Search.Trim();

        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = """
            SELECT COUNT(*)
            FROM characters c
            WHERE @search = '' OR c.Name LIKE @wildSearch OR CAST(c.Id AS CHAR) LIKE @wildSearch;
            """;
        countCommand.Parameters.AddWithValue("@search", search);
        countCommand.Parameters.AddWithValue("@wildSearch", $"%{search}%");
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                c.Id,
                COALESCE(wc.AccountId, 0) AS AccountId,
                COALESCE(a.Role, 0) AS AccountRole,
                c.Name,
                c.Breed,
                c.Experience,
                c.Kamas,
                c.MapId,
                COALESCE((SELECT MAX(e.Level) FROM experiences e WHERE e.Characters <= c.Experience), 1) AS Level
            FROM `{_connectionFactory.WorldDatabaseName}`.characters c
            LEFT JOIN `{_connectionFactory.AuthDatabaseName}`.worlds_characters wc
                ON wc.CharacterId = c.Id
            LEFT JOIN `{_connectionFactory.AuthDatabaseName}`.accounts a
                ON a.Id = wc.AccountId
            WHERE @search = '' OR c.Name LIKE @wildSearch OR CAST(c.Id AS CHAR) LIKE @wildSearch
            ORDER BY c.Id DESC
            LIMIT @offset, @pageSize;
            """;
        command.Parameters.AddWithValue("@search", search);
        command.Parameters.AddWithValue("@wildSearch", $"%{search}%");
        command.Parameters.AddWithValue("@offset", (normalized.Page - 1) * normalized.PageSize);
        command.Parameters.AddWithValue("@pageSize", normalized.PageSize);

        var items = new List<AdminCharacterListItem>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new AdminCharacterListItem
                {
                    Id = reader.GetSafeInt32("Id"),
                    AccountId = reader.GetSafeInt32("AccountId"),
                    AccountRole = (GameHierarchyEnum)reader.GetSafeInt32("AccountRole"),
                    Name = reader.GetSafeString("Name"),
                    Breed = (BreedEnum)reader.GetSafeInt32("Breed"),
                    Experience = reader.GetSafeInt64("Experience"),
                    Kamas = reader.GetSafeInt32("Kamas"),
                    MapId = reader.GetSafeInt32("MapId"),
                    Level = reader.GetSafeByte("Level", 1),
                });
            }
        }

        await PopulateAssignedSpecialSpellsAsync(connection, items, cancellationToken);

        return new AdminPagedResult<AdminCharacterListItem>(items, totalCount, normalized.Page, normalized.PageSize);
    }

    public async Task<string> GrantMatanzaAsync(int characterId, CancellationToken cancellationToken = default)
        => await GrantSpecialSpellAsync(characterId, AdminSpellPresets.MatanzaSpellId, cancellationToken);

    public async Task<string> GrantDoomAsync(int characterId, CancellationToken cancellationToken = default)
        => await GrantSpecialSpellAsync(characterId, AdminSpellPresets.DoomSpellId, cancellationToken);

    public async Task<string> GrantSpecialSpellAsync(int characterId, short spellId, CancellationToken cancellationToken = default)
    {
        if (!AdminSpellPresets.TryGet(spellId, out var definition))
            throw new InvalidOperationException($"El spell staff #{spellId} no esta soportado.");

        if (!definition.CanAssignFromAdmin)
            throw new InvalidOperationException($"{definition.Name} esta congelado por incompatibilidad cliente y no puede asignarse desde Characters Admin.");

        await EnsureSpecialSpellDefinitionsAsync(cancellationToken);

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await _specialSpellAssignments.EnsureSchemaAndRecoverLegacyAssignmentsAsync(connection, cancellationToken);

        await using var loadCommand = connection.CreateCommand();
        loadCommand.CommandText = $"""
            SELECT
                c.Id,
                c.Name,
                COALESCE(wc.AccountId, 0) AS AccountId,
                COALESCE(a.Role, 0) AS AccountRole
            FROM `{_connectionFactory.WorldDatabaseName}`.characters c
            LEFT JOIN `{_connectionFactory.AuthDatabaseName}`.worlds_characters wc
                ON wc.CharacterId = c.Id
            LEFT JOIN `{_connectionFactory.AuthDatabaseName}`.accounts a
                ON a.Id = wc.AccountId
            WHERE c.Id = @characterId
            LIMIT 1;
            """;
        loadCommand.Parameters.AddWithValue("@characterId", characterId);

        string characterName;
        GameHierarchyEnum accountRole;
        await using (var reader = await loadCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException($"No existe el personaje #{characterId}.");

            characterName = reader.GetSafeString("Name");
            accountRole = (GameHierarchyEnum)reader.GetSafeInt32("AccountRole");
        }

        if (accountRole <= GameHierarchyEnum.PLAYER)
            throw new InvalidOperationException($"{definition.Name} solo puede asignarse a personajes vinculados a cuentas admin o staff.");

        await _specialSpellAssignments.AssignAsync(connection, characterId, spellId, cancellationToken);
        return definition.IsGrimoireSafe
            ? $"{definition.Name} fue asignado a {characterName} como spell staff/admin visible y cliente-safe. Debe aparecer en el grimoire tras refrescar spells o reconectar."
            : $"{definition.Name} fue asignado a {characterName} en carril staff/admin seguro. No se publica en el grimoire normal de este cliente; si habia estado legacy roto, reconectar limpia el personaje.";
    }

    public async Task<string> RevokeSpecialSpellAsync(int characterId, short spellId, CancellationToken cancellationToken = default)
    {
        if (!AdminSpellPresets.TryGet(spellId, out var definition))
            throw new InvalidOperationException($"El spell staff #{spellId} no esta soportado.");

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await _specialSpellAssignments.EnsureSchemaAndRecoverLegacyAssignmentsAsync(connection, cancellationToken);

        await using var loadCommand = connection.CreateCommand();
        loadCommand.CommandText = """
            SELECT Name
            FROM characters
            WHERE Id = @characterId
            LIMIT 1;
            """;
        loadCommand.Parameters.AddWithValue("@characterId", characterId);

        var characterName = Convert.ToString(await loadCommand.ExecuteScalarAsync(cancellationToken));
        if (string.IsNullOrWhiteSpace(characterName))
            throw new InvalidOperationException($"No existe el personaje #{characterId}.");

        await _specialSpellAssignments.RevokeAsync(connection, characterId, spellId, cancellationToken);
        return $"{definition.Name} fue quitado de {characterName}. Si habia residuos legacy en el carril normal de spells, reconectar termina de limpiarlos.";
    }

    public IReadOnlyList<AdminSpecialSpellDefinition> GetAvailableSpecialSpells() =>
        AdminSpellPresets.All
            .OrderByDescending(x => x.CanAssignFromAdmin)
            .ThenByDescending(x => x.IsGrimoireSafe)
            .ThenBy(x => x.PreferredPosition)
            .ToArray();

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

    private async Task PopulateAssignedSpecialSpellsAsync(
        MySqlConnector.MySqlConnection connection,
        List<AdminCharacterListItem> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return;

        var byCharacterId = await _specialSpellAssignments.LoadAssignmentsAsync(
            connection,
            items.Select(x => x.Id).ToArray(),
            cancellationToken);

        foreach (var item in items)
            item.AssignedSpecialSpells = byCharacterId.TryGetValue(item.Id, out var assignedSpells)
                ? assignedSpells.OrderBy(x => x.Name).ToArray()
                : Array.Empty<AdminCharacterSpecialSpellItem>();
    }

    private async Task EnsureSpecialSpellDefinitionsAsync(CancellationToken cancellationToken)
    {
        foreach (var specialSpell in AdminSpellPresets.All)
            await _spellAdminService.EnsureSpecialSpellAsync(specialSpell.SpellId, cancellationToken);
    }
}
