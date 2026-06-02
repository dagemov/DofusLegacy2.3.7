using System.Data.Common;
using MySqlConnector;

namespace RollblackLegacy.Admin.Infrastructure.Configuration;

public sealed class AdminDatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool AllowDevelopmentPlaceholderConnectionString { get; set; }

    public bool HasUsableConnectionString()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            return false;
        }

        if (!HasPlaceholderPassword(ConnectionString))
        {
            return true;
        }

        return AllowDevelopmentPlaceholderConnectionString;
    }

    public AdminDatabaseConnectionTarget GetSafeConnectionTarget()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            return new AdminDatabaseConnectionTarget(null, null, null, null);
        }

        try
        {
            var builder = new MySqlConnectionStringBuilder(ConnectionString);
            var host = string.IsNullOrWhiteSpace(builder.Server) ? null : builder.Server;
            var port = builder.Port > 0 ? (int?)builder.Port : null;
            var user = string.IsNullOrWhiteSpace(builder.UserID) ? null : builder.UserID;

            return new AdminDatabaseConnectionTarget(
                host,
                port,
                user,
                host is not null ? !IsLocalHost(host) : null);
        }
        catch (ArgumentException)
        {
            return new AdminDatabaseConnectionTarget(null, null, null, null);
        }
    }

    private static bool HasPlaceholderPassword(string connectionString)
    {
        try
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            if (TryGetPassword(builder, out var password))
            {
                return string.Equals(password, "change-me", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
        catch (ArgumentException)
        {
            return connectionString.Contains("Password=change-me;", StringComparison.OrdinalIgnoreCase)
                || connectionString.Contains("Pwd=change-me;", StringComparison.OrdinalIgnoreCase);
        }
    }

    private static bool TryGetPassword(DbConnectionStringBuilder builder, out string password)
    {
        password = string.Empty;

        if (builder.TryGetValue("Password", out var passwordValue) && passwordValue is not null)
        {
            password = passwordValue.ToString() ?? string.Empty;
            return true;
        }

        if (builder.TryGetValue("Pwd", out var pwdValue) && pwdValue is not null)
        {
            password = pwdValue.ToString() ?? string.Empty;
            return true;
        }

        return false;
    }

    private static bool IsLocalHost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record AdminDatabaseConnectionTarget(
    string? Host,
    int? Port,
    string? User,
    bool? IsRemote);
