using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Sunshine.BaseServer.Configuration
{
    public static class GameConfig
    {
        private static readonly object SyncRoot = new object();
        private static Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static string _path;
        private static string DefaultPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.xml");

        public static void Load(string path = null)
        {
            lock (SyncRoot)
            {
                path = string.IsNullOrWhiteSpace(path) ? DefaultPath : path;
                _path = path;
                _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                if (!File.Exists(path))
                    return;

                foreach (var rawLine in File.ReadAllLines(path))
                {
                    var line = (rawLine ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//") || line.StartsWith(";"))
                        continue;

                    int separatorIndex = line.IndexOf('=');
                    if (separatorIndex > 0)
                    {
                        var key = line.Substring(0, separatorIndex).Trim();
                        var value = line.Substring(separatorIndex + 1).Trim();
                        if (!string.IsNullOrWhiteSpace(key))
                            _values[key] = value;
                        continue;
                    }

                    var xmlMatch = Regex.Match(line, @"^<(?<key>[^/>]+)>\s*(?<value>.*?)\s*</\1>$");
                    if (xmlMatch.Success)
                        _values[xmlMatch.Groups["key"].Value.Trim()] = xmlMatch.Groups["value"].Value.Trim();
                }
            }
        }

        public static void Reload()
        {
            Load(_path ?? DefaultPath);
        }

        public static string GetString(string key, string defaultValue = "")
        {
            lock (SyncRoot)
            {
                if (_values.Count == 0)
                    Load(_path ?? DefaultPath);

                string value;
                return _values.TryGetValue(key, out value) ? value : defaultValue;
            }
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            var raw = GetString(key, defaultValue.ToString(CultureInfo.InvariantCulture));
            int value;
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : defaultValue;
        }

        public static double GetDouble(string key, double defaultValue = 0d)
        {
            var raw = GetString(key, defaultValue.ToString(CultureInfo.InvariantCulture));
            double value;
            return double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value) ? value : defaultValue;
        }
    }
}
