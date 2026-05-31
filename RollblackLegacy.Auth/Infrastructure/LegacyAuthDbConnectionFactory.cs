using MySqlConnector;

namespace RollblackLegacy.Auth.Infrastructure;

public sealed class LegacyAuthDbConnectionFactory
{
    private readonly string _connectionString;

    public LegacyAuthDbConnectionFactory(string connectionString)
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
