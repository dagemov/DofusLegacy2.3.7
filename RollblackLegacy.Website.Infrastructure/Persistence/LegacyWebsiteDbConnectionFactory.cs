using MySqlConnector;

namespace RollblackLegacy.Website.Infrastructure.Persistence;

public sealed class LegacyWebsiteDbConnectionFactory
{
    private readonly string _connectionString;

    public LegacyWebsiteDbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public MySqlConnection CreateOpenConnection()
    {
        var connection = new MySqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
