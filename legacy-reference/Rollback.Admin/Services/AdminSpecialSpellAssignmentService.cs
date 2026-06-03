using MySqlConnector;
using Rollback.Admin.Models.Characters;
using Rollback.World.Game.Spells;

namespace Rollback.Admin.Services;

public sealed class AdminSpecialSpellAssignmentService
{
    private readonly AdminBootstrapService _bootstrapService;

    public AdminSpecialSpellAssignmentService(AdminBootstrapService bootstrapService) =>
        _bootstrapService = bootstrapService;

    public async Task EnsureSchemaAndRecoverLegacyAssignmentsAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken = default)
    {
        await _bootstrapService.EnsureSchemaAsync(cancellationToken);
        await EnsureBackupSchemaAsync(connection, cancellationToken);
        await EnsureVisibleGrimoireBackupSchemaAsync(connection, cancellationToken);
        await EnsureFrozenAssignmentBackupSchemaAsync(connection, cancellationToken);
        await BackupLegacyAssignmentsAsync(connection, cancellationToken);
        await BackupVisibleAssignmentsAsync(connection, cancellationToken);
        await BackupFrozenUnsafeAssignmentsAsync(connection, cancellationToken);
        await MigrateLegacyAssignmentsAsync(connection, cancellationToken);
        await FreezeUnsafeAssignmentsAsync(connection, cancellationToken);
        await SyncVisibleAssignmentsToCharactersSpellsAsync(connection, cancellationToken);
        await CleanupUnsafeCharacterSpellLaneAsync(connection, cancellationToken);
    }

    public async Task<Dictionary<int, List<AdminCharacterSpecialSpellItem>>> LoadAssignmentsAsync(
        MySqlConnection connection,
        IReadOnlyCollection<int> characterIds,
        CancellationToken cancellationToken = default)
    {
        var result = characterIds.ToDictionary(id => id, _ => new List<AdminCharacterSpecialSpellItem>());
        if (characterIds.Count == 0)
            return result;

        await using var command = connection.CreateCommand();

        var characterParameters = characterIds.Select((id, index) =>
        {
            var parameterName = $"@characterId{index}";
            command.Parameters.AddWithValue(parameterName, id);
            return parameterName;
        }).ToArray();

        var spellIds = AdminSpellPresets.All.Select(x => x.SpellId).ToArray();
        var spellParameters = spellIds.Select((id, index) =>
        {
            var parameterName = $"@spellId{index}";
            command.Parameters.AddWithValue(parameterName, id);
            return parameterName;
        }).ToArray();

        command.CommandText = $"""
            SELECT CharacterId, SpellId
            FROM admin_character_special_spells
            WHERE CharacterId IN ({string.Join(", ", characterParameters)})
              AND SpellId IN ({string.Join(", ", spellParameters)})
            ORDER BY CharacterId ASC, SpellId ASC;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var characterId = reader.GetInt32(reader.GetOrdinal("CharacterId"));
            var spellId = reader.GetInt16(reader.GetOrdinal("SpellId"));

            if (!result.TryGetValue(characterId, out var assigned) ||
                !AdminSpellPresets.TryGet(spellId, out var definition))
            {
                continue;
            }

            assigned.Add(new AdminCharacterSpecialSpellItem
            {
                SpellId = definition.SpellId,
                Name = definition.Name,
                ModeLabel = definition.AssignmentModeLabel,
                ClientCompatibilityLabel = definition.ClientCompatibilityLabel,
            });
        }

        return result;
    }

    public async Task AssignAsync(
        MySqlConnection connection,
        int characterId,
        short spellId,
        CancellationToken cancellationToken = default)
    {
        var normalizedSpellId = StaffSpecialSpellPolicy.NormalizeAssignedSpellId(spellId);
        if (!AdminSpellPresets.TryGet(normalizedSpellId, out var definition))
            throw new InvalidOperationException($"El spell staff #{spellId} no esta soportado.");

        await using var upsert = connection.CreateCommand();
        upsert.CommandText = """
            INSERT INTO admin_character_special_spells (CharacterId, SpellId, AssignedAt, UpdatedAt)
            VALUES (@characterId, @spellId, UTC_TIMESTAMP(), UTC_TIMESTAMP())
            ON DUPLICATE KEY UPDATE
                UpdatedAt = UTC_TIMESTAMP();
            """;
        upsert.Parameters.AddWithValue("@characterId", characterId);
        upsert.Parameters.AddWithValue("@spellId", normalizedSpellId);
        await upsert.ExecuteNonQueryAsync(cancellationToken);

        if (definition.IsGrimoireSafe)
            await UpsertVisibleCharacterSpellAsync(connection, characterId, definition, cancellationToken);
        else
            await DeleteCharacterSpellLaneAsync(connection, characterId, normalizedSpellId, cancellationToken);
    }

    public async Task RevokeAsync(
        MySqlConnection connection,
        int characterId,
        short spellId,
        CancellationToken cancellationToken = default)
    {
        var normalizedSpellId = StaffSpecialSpellPolicy.NormalizeAssignedSpellId(spellId);

        await using var delete = connection.CreateCommand();
        delete.CommandText = """
            DELETE FROM admin_character_special_spells
            WHERE CharacterId = @characterId AND SpellId IN (@spellId, @legacySpellId);
            """;
        delete.Parameters.AddWithValue("@characterId", characterId);
        delete.Parameters.AddWithValue("@spellId", normalizedSpellId);
        delete.Parameters.AddWithValue("@legacySpellId", ResolveLegacySpellId(normalizedSpellId));
        await delete.ExecuteNonQueryAsync(cancellationToken);

        await DeleteCharacterSpellLaneAsync(connection, characterId, normalizedSpellId, cancellationToken);
    }

    private static async Task EnsureBackupSchemaAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS admin_character_special_spells_phase1_backup (
                CharacterId INT NOT NULL,
                SpellId SMALLINT NOT NULL,
                SpellLevel TINYINT NOT NULL DEFAULT 1,
                Position TINYINT UNSIGNED NOT NULL DEFAULT 63,
                BackedUpAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (CharacterId, SpellId)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureVisibleGrimoireBackupSchemaAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS admin_character_special_spells_phase2_backup (
                CharacterId INT NOT NULL,
                SpellId SMALLINT NOT NULL,
                SpellLevel TINYINT NOT NULL DEFAULT 1,
                Position TINYINT UNSIGNED NOT NULL DEFAULT 63,
                SourceLane VARCHAR(24) NOT NULL DEFAULT 'characters_spells',
                BackedUpAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (CharacterId, SpellId, SourceLane)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureFrozenAssignmentBackupSchemaAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS admin_character_special_spells_phase4_backup (
                CharacterId INT NOT NULL,
                SpellId SMALLINT NOT NULL,
                SourceLane VARCHAR(32) NOT NULL DEFAULT 'admin_character_special_spells',
                BackedUpAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (CharacterId, SpellId, SourceLane)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task BackupLegacyAssignmentsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO admin_character_special_spells_phase1_backup (CharacterId, SpellId, SpellLevel, Position, BackedUpAt)
            SELECT cs.OwnerId, cs.SpellId, cs.SpellLevel, cs.Position, UTC_TIMESTAMP()
            FROM characters_spells cs
            WHERE cs.SpellId IN (@legacyMatanza, @legacyDoom)
            ON DUPLICATE KEY UPDATE
                SpellLevel = VALUES(SpellLevel),
                Position = VALUES(Position),
                BackedUpAt = UTC_TIMESTAMP();
            """;
        BindLegacySpellParameters(command);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task BackupVisibleAssignmentsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await using (var backupCharacterSpells = connection.CreateCommand())
        {
            backupCharacterSpells.CommandText = """
                INSERT INTO admin_character_special_spells_phase2_backup (CharacterId, SpellId, SpellLevel, Position, SourceLane, BackedUpAt)
                SELECT cs.OwnerId, cs.SpellId, cs.SpellLevel, cs.Position, 'characters_spells', UTC_TIMESTAMP()
                FROM characters_spells cs
                WHERE cs.SpellId IN (@legacyMatanza, @legacyDoom, @matanza, @doom)
                ON DUPLICATE KEY UPDATE
                    SpellLevel = VALUES(SpellLevel),
                    Position = VALUES(Position),
                    BackedUpAt = UTC_TIMESTAMP();
                """;
            BindAllKnownSpellParameters(backupCharacterSpells);
            await backupCharacterSpells.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var backupAssignments = connection.CreateCommand();
        backupAssignments.CommandText = """
            INSERT INTO admin_character_special_spells_phase2_backup (CharacterId, SpellId, SpellLevel, Position, SourceLane, BackedUpAt)
            SELECT acss.CharacterId, acss.SpellId, 1, 63, 'admin_character_special_spells', UTC_TIMESTAMP()
            FROM admin_character_special_spells acss
            WHERE acss.SpellId IN (@legacyMatanza, @legacyDoom, @matanza, @doom)
            ON DUPLICATE KEY UPDATE
                SpellLevel = VALUES(SpellLevel),
                Position = VALUES(Position),
                BackedUpAt = UTC_TIMESTAMP();
            """;
        BindAllKnownSpellParameters(backupAssignments);
        await backupAssignments.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task BackupFrozenUnsafeAssignmentsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await using var backupAssignments = connection.CreateCommand();
        backupAssignments.CommandText = """
            INSERT INTO admin_character_special_spells_phase4_backup (CharacterId, SpellId, SourceLane, BackedUpAt)
            SELECT acss.CharacterId, acss.SpellId, 'admin_character_special_spells', UTC_TIMESTAMP()
            FROM admin_character_special_spells acss
            WHERE acss.SpellId IN (@legacyMatanza, @legacyDoom, @matanza, @doom)
            ON DUPLICATE KEY UPDATE
                BackedUpAt = UTC_TIMESTAMP();
            """;
        BindAllKnownSpellParameters(backupAssignments);
        await backupAssignments.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MigrateLegacyAssignmentsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await using (var insert = connection.CreateCommand())
        {
            insert.CommandText = """
                INSERT INTO admin_character_special_spells (CharacterId, SpellId, AssignedAt, UpdatedAt)
                SELECT cs.OwnerId,
                       CASE
                           WHEN cs.SpellId = @legacyMatanza THEN @matanza
                           WHEN cs.SpellId = @legacyDoom THEN @doom
                           ELSE cs.SpellId
                       END,
                       UTC_TIMESTAMP(),
                       UTC_TIMESTAMP()
                FROM characters_spells cs
                WHERE cs.SpellId IN (@legacyMatanza, @legacyDoom)
                ON DUPLICATE KEY UPDATE
                    UpdatedAt = UTC_TIMESTAMP();
                """;
            BindAllKnownSpellParameters(insert);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var migrateAdminAssignments = connection.CreateCommand())
        {
            migrateAdminAssignments.CommandText = """
                INSERT INTO admin_character_special_spells (CharacterId, SpellId, AssignedAt, UpdatedAt)
                SELECT acss.CharacterId,
                       CASE
                           WHEN acss.SpellId = @legacyMatanza THEN @matanza
                           WHEN acss.SpellId = @legacyDoom THEN @doom
                           ELSE acss.SpellId
                       END,
                       acss.AssignedAt,
                       UTC_TIMESTAMP()
                FROM admin_character_special_spells acss
                WHERE acss.SpellId IN (@legacyMatanza, @legacyDoom)
                ON DUPLICATE KEY UPDATE
                    UpdatedAt = UTC_TIMESTAMP();
                """;
            BindAllKnownSpellParameters(migrateAdminAssignments);
            await migrateAdminAssignments.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var delete = connection.CreateCommand())
        {
            delete.CommandText = """
                DELETE FROM admin_character_special_spells
                WHERE SpellId IN (@legacyMatanza, @legacyDoom);
                """;
            BindAllKnownSpellParameters(delete);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var delete = connection.CreateCommand())
        {
            delete.CommandText = """
                DELETE FROM characters_spells
                WHERE SpellId IN (@legacyMatanza, @legacyDoom);
                """;
            BindAllKnownSpellParameters(delete);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task FreezeUnsafeAssignmentsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await using var delete = connection.CreateCommand();
        delete.CommandText = """
            DELETE FROM admin_character_special_spells
            WHERE SpellId IN (@legacyMatanza, @legacyDoom, @matanza, @doom);
            """;
        BindAllKnownSpellParameters(delete);
        await delete.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SyncVisibleAssignmentsToCharactersSpellsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        foreach (var definition in AdminSpellPresets.All.Where(x => x.IsGrimoireSafe))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO characters_spells (OwnerId, SpellId, SpellLevel, Position)
                SELECT acss.CharacterId, @spellId, 1, @position
                FROM admin_character_special_spells acss
                WHERE acss.SpellId = @spellId
                ON DUPLICATE KEY UPDATE
                    SpellLevel = VALUES(SpellLevel),
                    Position = VALUES(Position);
                """;
            command.Parameters.AddWithValue("@spellId", definition.SpellId);
            command.Parameters.AddWithValue("@position", definition.PreferredPosition);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task CleanupUnsafeCharacterSpellLaneAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await using var delete = connection.CreateCommand();
        delete.CommandText = """
            DELETE FROM characters_spells
            WHERE SpellId IN (@legacyMatanza, @legacyDoom, @matanza, @doom);
            """;
        BindAllKnownSpellParameters(delete);
        await delete.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DeleteCharacterSpellLaneAsync(
        MySqlConnection connection,
        int characterId,
        short spellId,
        CancellationToken cancellationToken)
    {
        var legacySpellId = ResolveLegacySpellId(spellId);

        await using var delete = connection.CreateCommand();
        delete.CommandText = """
            DELETE FROM characters_spells
            WHERE OwnerId = @characterId AND SpellId IN (@spellId, @legacySpellId);
            """;
        delete.Parameters.AddWithValue("@characterId", characterId);
        delete.Parameters.AddWithValue("@spellId", spellId);
        delete.Parameters.AddWithValue("@legacySpellId", legacySpellId);
        await delete.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertVisibleCharacterSpellAsync(
        MySqlConnection connection,
        int characterId,
        AdminSpecialSpellDefinition definition,
        CancellationToken cancellationToken)
    {
        await using var upsert = connection.CreateCommand();
        upsert.CommandText = """
            INSERT INTO characters_spells (OwnerId, SpellId, SpellLevel, Position)
            VALUES (@characterId, @spellId, 1, @position)
            ON DUPLICATE KEY UPDATE
                SpellLevel = VALUES(SpellLevel),
                Position = VALUES(Position);
            """;
        upsert.Parameters.AddWithValue("@characterId", characterId);
        upsert.Parameters.AddWithValue("@spellId", definition.SpellId);
        upsert.Parameters.AddWithValue("@position", definition.PreferredPosition);
        await upsert.ExecuteNonQueryAsync(cancellationToken);
    }

    private static short ResolveLegacySpellId(short normalizedSpellId) =>
        normalizedSpellId switch
        {
            var spellId when spellId == StaffSpecialSpellPolicy.MatanzaSpellId => StaffSpecialSpellPolicy.LegacyMatanzaSpellId,
            var spellId when spellId == StaffSpecialSpellPolicy.DoomSpellId => StaffSpecialSpellPolicy.LegacyDoomSpellId,
            _ => normalizedSpellId,
        };

    private static void BindLegacySpellParameters(MySqlCommand command)
    {
        command.Parameters.AddWithValue("@legacyMatanza", StaffSpecialSpellPolicy.LegacyMatanzaSpellId);
        command.Parameters.AddWithValue("@legacyDoom", StaffSpecialSpellPolicy.LegacyDoomSpellId);
    }

    private static void BindAllKnownSpellParameters(MySqlCommand command)
    {
        BindLegacySpellParameters(command);
        command.Parameters.AddWithValue("@matanza", StaffSpecialSpellPolicy.MatanzaSpellId);
        command.Parameters.AddWithValue("@doom", StaffSpecialSpellPolicy.DoomSpellId);
    }
}
