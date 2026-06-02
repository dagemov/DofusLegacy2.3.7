using System.Data.Common;

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
}
