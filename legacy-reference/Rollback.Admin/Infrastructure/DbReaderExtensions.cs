using MySqlConnector;

namespace Rollback.Admin.Infrastructure;

internal static class DbReaderExtensions
{
    public static string GetSafeString(this MySqlDataReader reader, string columnName) =>
        reader.IsDBNull(reader.GetOrdinal(columnName))
            ? string.Empty
            : reader.GetString(reader.GetOrdinal(columnName));

    public static short GetSafeInt16(this MySqlDataReader reader, string columnName, short defaultValue = 0) =>
        reader.IsDBNull(reader.GetOrdinal(columnName))
            ? defaultValue
            : Convert.ToInt16(reader[columnName]);

    public static int GetSafeInt32(this MySqlDataReader reader, string columnName, int defaultValue = 0) =>
        reader.IsDBNull(reader.GetOrdinal(columnName))
            ? defaultValue
            : Convert.ToInt32(reader[columnName]);

    public static byte GetSafeByte(this MySqlDataReader reader, string columnName, byte defaultValue = 0) =>
        reader.IsDBNull(reader.GetOrdinal(columnName))
            ? defaultValue
            : Convert.ToByte(reader[columnName]);

    public static sbyte GetSafeSByte(this MySqlDataReader reader, string columnName, sbyte defaultValue = 0) =>
        reader.IsDBNull(reader.GetOrdinal(columnName))
            ? defaultValue
            : Convert.ToSByte(reader[columnName]);

    public static bool GetSafeBoolean(this MySqlDataReader reader, string columnName, bool defaultValue = false) =>
        reader.IsDBNull(reader.GetOrdinal(columnName))
            ? defaultValue
            : Convert.ToBoolean(reader[columnName]);

    public static long GetSafeInt64(this MySqlDataReader reader, string columnName, long defaultValue = 0) =>
        reader.IsDBNull(reader.GetOrdinal(columnName))
            ? defaultValue
            : Convert.ToInt64(reader[columnName]);

    public static DateTime? GetSafeDateTime(this MySqlDataReader reader, string columnName) =>
        reader.IsDBNull(reader.GetOrdinal(columnName))
            ? null
            : Convert.ToDateTime(reader[columnName]);

    public static byte[] GetSafeBytes(this MySqlDataReader reader, string columnName) =>
        reader.IsDBNull(reader.GetOrdinal(columnName))
            ? Array.Empty<byte>()
            : (byte[])reader[columnName];
}
