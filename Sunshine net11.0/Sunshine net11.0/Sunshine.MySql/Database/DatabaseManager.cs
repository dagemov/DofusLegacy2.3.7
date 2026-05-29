using MySql.Data.MySqlClient;
using Sunshine.Logs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Sunshine.Mysql.Database
{
    public static class DatabaseManager
    {
        private static MySqlConnection _connection;
        private static readonly object _locker = new object();
        private static readonly string _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database.xml");

        public static string ConnectionString { get; private set; }
        public static string SettingsPath => _settingsPath;

        public static MySqlConnection Connection
        {
            get
            {
                lock (_locker)
                {
                    return _connection;
                }
            }
            set
            {
                _connection = value;
            }
        }

        private static Dictionary<string, string> LoadSettings()
        {
            if (!File.Exists(_settingsPath))
                throw new FileNotFoundException("Database.xml not found.", _settingsPath);

            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rawLine in File.ReadAllLines(_settingsPath))
            {
                var line = (rawLine ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//") || line.StartsWith(";"))
                    continue;

                int separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                var key = line.Substring(0, separatorIndex).Trim();
                var value = line.Substring(separatorIndex + 1).Trim();

                if (!string.IsNullOrWhiteSpace(key))
                    settings[key] = value;
            }

            return settings;
        }

        private static string GetSetting(IDictionary<string, string> settings, string key, string defaultValue = "")
        {
            string value;
            return settings.TryGetValue(key, out value) ? value : defaultValue;
        }

        private static uint GetPort(IDictionary<string, string> settings)
        {
            uint port;
            return uint.TryParse(GetSetting(settings, "Port", "3306"), NumberStyles.Integer, CultureInfo.InvariantCulture, out port)
                ? port
                : 3306;
        }

        private static string NormalizeHost(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "127.0.0.1";

            return string.Equals(value, "localhost", StringComparison.OrdinalIgnoreCase)
                ? "127.0.0.1"
                : value;
        }

        public static MySqlConnection CreateConnection()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new InvalidOperationException("Database connection has not been initialized yet.");

            return new MySqlConnection(ConnectionString);
        }

        public static void Initilize()
        {
            try
            {
                var settings = LoadSettings();
                var server = NormalizeHost(GetSetting(settings, "Hostname", "127.0.0.1"));
                var database = GetSetting(settings, "Database", "sunshine");
                var userId = GetSetting(settings, "Username", "sunshine");
                var password = GetSetting(settings, "Password", string.Empty);
                var builder = new MySqlConnectionStringBuilder
                {
                    Server = server,
                    Port = GetPort(settings),
                    Database = database,
                    UserID = userId,
                    Password = password,
                    AllowUserVariables = true
                };

                ConnectionString = builder.ConnectionString;
                _connection = new MySqlConnection(ConnectionString);
                Logger.Write("[ Server MYSQL ] Initialization Database");
                Logger.Write($"[ Server MYSQL ] Runtime config Path={_settingsPath}");
                Logger.Write($"[ Server MYSQL ] Runtime config Host={server}; Port={builder.Port}; Database={database}; User={userId}; PasswordSet={!string.IsNullOrWhiteSpace(password)}");
                Logger.Write("[ Server MYSQL ] Opening Database....");
                _connection.Open();
                Logger.Write("[ Server MYSQL ] Connected to the Database");
            }
            catch (Exception e)
            {
                Logger.WriteError(e.ToString());
            }
        }
    }
}
