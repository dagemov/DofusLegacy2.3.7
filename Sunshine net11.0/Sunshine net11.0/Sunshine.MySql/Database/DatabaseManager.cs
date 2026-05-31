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

        public static string ConnectionString { get; private set; }

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
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database.xml");

            if (!File.Exists(path))
                throw new FileNotFoundException("Database.xml not found.", path);

            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rawLine in File.ReadAllLines(path))
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
                var builder = new MySqlConnectionStringBuilder
                {
                    Server = GetSetting(settings, "Hostname", "localhost"),
                    Port = GetPort(settings),
                    Database = GetSetting(settings, "Database", "sunshine"),
                    UserID = GetSetting(settings, "Username", "root"),
                    Password = GetSetting(settings, "Password", string.Empty),
                    AllowUserVariables = true
                };

                ConnectionString = builder.ConnectionString;
                _connection = new MySqlConnection(ConnectionString);
                Logger.Write("[ Server MYSQL ] Initialization Database");
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
