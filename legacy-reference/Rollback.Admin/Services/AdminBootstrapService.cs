using MySqlConnector;
using Rollback.Admin.Infrastructure;

namespace Rollback.Admin.Services;

public sealed class AdminBootstrapService
{
    private readonly AdminDbConnectionFactory _connectionFactory;

    public AdminBootstrapService(AdminDbConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var commands = new[]
        {
            """
            CREATE TABLE IF NOT EXISTS admin_entity_text_overrides (
                EntityType VARCHAR(24) NOT NULL,
                EntityId INT NOT NULL,
                DisplayName VARCHAR(180) NOT NULL DEFAULT '',
                Description TEXT NOT NULL,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                PRIMARY KEY (EntityType, EntityId)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
            """,
            """
            CREATE TABLE IF NOT EXISTS admin_entity_asset_overrides (
                EntityType VARCHAR(24) NOT NULL,
                EntityId INT NOT NULL,
                AssetKind VARCHAR(32) NOT NULL DEFAULT 'PreviewPng',
                RelativePath VARCHAR(255) NOT NULL,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                PRIMARY KEY (EntityType, EntityId, AssetKind)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
            """,
            """
            CREATE TABLE IF NOT EXISTS admin_entity_client_metadata (
                EntityType VARCHAR(24) NOT NULL,
                EntityId INT NOT NULL,
                LanguageCode VARCHAR(8) NOT NULL DEFAULT 'es',
                NameId INT NOT NULL DEFAULT 0,
                DescriptionId INT NOT NULL DEFAULT 0,
                IconId INT NOT NULL DEFAULT 0,
                AppearanceId INT NOT NULL DEFAULT 0,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                PRIMARY KEY (EntityType, EntityId, LanguageCode)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
            """,
            """
            ALTER TABLE admin_entity_client_metadata
                ADD COLUMN IF NOT EXISTS AppearanceId INT NOT NULL DEFAULT 0 AFTER IconId;
            """,
            """
            CREATE TABLE IF NOT EXISTS admin_runtime_revisions (
                Domain VARCHAR(48) NOT NULL,
                Revision BIGINT NOT NULL DEFAULT 0,
                UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                PRIMARY KEY (Domain)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
            """,
            """
            CREATE TABLE IF NOT EXISTS admin_character_special_spells (
                CharacterId INT NOT NULL,
                SpellId SMALLINT NOT NULL,
                AssignedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                PRIMARY KEY (CharacterId, SpellId)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
            """,
            """
            CREATE TABLE IF NOT EXISTS admin_spell_trigger_payload_sync (
                OuterSpellId SMALLINT NOT NULL,
                OuterLevelNumber TINYINT NOT NULL,
                EffectSide VARCHAR(16) NOT NULL,
                LinkedSpellId SMALLINT NOT NULL,
                LinkedLevelNumber TINYINT NOT NULL,
                PayloadJson LONGTEXT NOT NULL,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                PRIMARY KEY (OuterSpellId, OuterLevelNumber, EffectSide)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
            """,
            """
            CREATE TABLE IF NOT EXISTS admin_monster_groups (
                Id INT NOT NULL AUTO_INCREMENT,
                Name VARCHAR(120) NOT NULL,
                Notes VARCHAR(512) NOT NULL DEFAULT '',
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                PRIMARY KEY (Id),
                UNIQUE KEY UX_admin_monster_groups_Name (Name)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
            """,
            """
            CREATE TABLE IF NOT EXISTS admin_monster_group_entries (
                Id INT NOT NULL AUTO_INCREMENT,
                MonsterGroupId INT NOT NULL,
                MonsterId SMALLINT NOT NULL,
                MinGrade TINYINT NOT NULL DEFAULT 1,
                MaxGrade TINYINT NOT NULL DEFAULT 1,
                Probability TINYINT UNSIGNED NOT NULL DEFAULT 5,
                Disabled BIT NOT NULL DEFAULT b'0',
                PRIMARY KEY (Id),
                CONSTRAINT FK_admin_monster_group_entries_group
                    FOREIGN KEY (MonsterGroupId) REFERENCES admin_monster_groups (Id)
                    ON DELETE CASCADE,
                UNIQUE KEY UX_admin_monster_group_entries_unique (MonsterGroupId, MonsterId, MinGrade, MaxGrade)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
            """,
            """
            CREATE TABLE IF NOT EXISTS admin_monster_group_assignments (
                Id INT NOT NULL AUTO_INCREMENT,
                MonsterGroupId INT NOT NULL,
                MapId INT NULL,
                SubAreaId SMALLINT NULL,
                ProbabilityOverride TINYINT UNSIGNED NULL,
                Disabled BIT NOT NULL DEFAULT b'0',
                LastSyncedAt DATETIME NULL,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                PRIMARY KEY (Id),
                CONSTRAINT FK_admin_monster_group_assignments_group
                    FOREIGN KEY (MonsterGroupId) REFERENCES admin_monster_groups (Id)
                    ON DELETE CASCADE,
                UNIQUE KEY UX_admin_monster_group_assignments_target (MonsterGroupId, MapId, SubAreaId)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
            """,
        };

        foreach (var sql in commands)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
