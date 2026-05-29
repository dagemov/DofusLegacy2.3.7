using Dapper;
using MySqlConnector;
using RollblackLegacy.Website.Application.Abstractions;
using RollblackLegacy.Website.Domain.Accounts;

namespace RollblackLegacy.Website.Infrastructure.Persistence;

public sealed class LegacyAuthAccountRepository : ILegacyAccountRepository
{
    private const string ContactTableName = "website_account_contacts";
    private readonly LegacyWebsiteDbConnectionFactory _connectionFactory;

    public LegacyAuthAccountRepository(LegacyWebsiteDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM accounts
            WHERE LOWER(Username) = LOWER(@Username)
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateOpenConnection();

        long count = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            sql,
            new { Username = username },
            cancellationToken: cancellationToken));

        return count > 0;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        await EnsureContactTableAsync(connection, cancellationToken);

        const string sidecarSql = """
            SELECT COUNT(*)
            FROM website_account_contacts
            WHERE LOWER(Email) = LOWER(@Email);
            """;

        long sidecarCount = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            sidecarSql,
            new { Email = email },
            cancellationToken: cancellationToken));

        if (sidecarCount > 0)
            return true;

        if (!await SupportsAccountEmailColumnAsync(connection, cancellationToken))
            return false;

        const string accountSql = """
            SELECT COUNT(*)
            FROM accounts
            WHERE Email IS NOT NULL
              AND LOWER(Email) = LOWER(@Email);
            """;

        long accountCount = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            accountSql,
            new { Email = email },
            cancellationToken: cancellationToken));

        return accountCount > 0;
    }

    public async Task<LegacyAccountSchemaCapabilities> CreateAsync(
        LegacyAccountRegistration registration,
        CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        await EnsureContactTableAsync(connection, cancellationToken);

        bool supportsAccountEmailColumn = await SupportsAccountEmailColumnAsync(connection, cancellationToken);

        using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string insertAccountSql = """
            INSERT INTO accounts
            (
                Username,
                Password,
                Nickname,
                Role,
                SecretQuestion,
                SecretAnswer,
                IsBanned,
                Ticket,
                RegisteredIP,
                Tokens,
                NewTokens
            )
            VALUES
            (
                @Username,
                @Password,
                @Nickname,
                @Role,
                @SecretQuestion,
                @SecretAnswer,
                @IsBanned,
                @Ticket,
                @RegisteredIP,
                @Tokens,
                @NewTokens
            );
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertAccountSql,
            new
            {
                registration.Username,
                Password = registration.PasswordHash,
                registration.Nickname,
                registration.Role,
                registration.SecretQuestion,
                registration.SecretAnswer,
                IsBanned = registration.IsBanned,
                registration.Ticket,
                RegisteredIP = registration.RegisteredIp,
                registration.Tokens,
                registration.NewTokens,
            },
            transaction,
            cancellationToken: cancellationToken));

        long accountId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT LAST_INSERT_ID();",
            transaction: transaction,
            cancellationToken: cancellationToken));

        const string insertContactSql = """
            INSERT INTO website_account_contacts
            (
                AccountId,
                Email,
                CreatedAtUtc
            )
            VALUES
            (
                @AccountId,
                @Email,
                UTC_TIMESTAMP()
            );
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertContactSql,
            new
            {
                AccountId = accountId,
                registration.Email,
            },
            transaction,
            cancellationToken: cancellationToken));

        if (supportsAccountEmailColumn)
        {
            const string updateEmailSql = """
                UPDATE accounts
                SET Email = @Email
                WHERE Id = @AccountId;
                """;

            await connection.ExecuteAsync(new CommandDefinition(
                updateEmailSql,
                new
                {
                    AccountId = accountId,
                    registration.Email,
                },
                transaction,
                cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);

        return new LegacyAccountSchemaCapabilities
        {
            SupportsAccountEmailColumn = supportsAccountEmailColumn,
            UsesWebsiteContactTable = true,
        };
    }

    private static async Task EnsureContactTableAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        string sql = $"""
            CREATE TABLE IF NOT EXISTS {ContactTableName}
            (
                AccountId INT NOT NULL,
                Email VARCHAR(255) NOT NULL,
                CreatedAtUtc DATETIME NOT NULL,
                PRIMARY KEY (AccountId),
                UNIQUE KEY UX_{ContactTableName}_email (Email),
                CONSTRAINT FK_{ContactTableName}_accounts
                    FOREIGN KEY (AccountId) REFERENCES accounts (Id)
                    ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));
    }

    private static async Task<bool> SupportsAccountEmailColumnAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'accounts'
              AND COLUMN_NAME = 'Email';
            """;

        long count = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));

        return count > 0;
    }
}
