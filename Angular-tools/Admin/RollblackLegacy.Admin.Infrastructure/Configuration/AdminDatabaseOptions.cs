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

        if (!ConnectionString.Contains("change-me", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return AllowDevelopmentPlaceholderConnectionString;
    }
}
