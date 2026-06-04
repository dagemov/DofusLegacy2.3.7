using System.Buffers.Binary;
using System.Text;

var itemsPath = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Client2.3.7", "data", "common", "Items.d2o"));

var focus = args.Length > 1 ? args[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : null;
var bytes = File.ReadAllBytes(itemsPath);
var cursor = new Cursor(bytes);
if (cursor.ReadAscii(3) != "D2O")
{
    throw new InvalidDataException("Invalid header");
}

cursor.Position = cursor.ReadInt32BigEndian();
var indexLength = cursor.ReadInt32BigEndian();
cursor.Position += indexLength;
var classCount = cursor.ReadInt32BigEndian();

for (var i = 0; i < classCount; i++)
{
    var classId = cursor.ReadInt32BigEndian();
    var className = cursor.ReadUtf();
    var package = cursor.ReadUtf();
    var fieldCount = cursor.ReadInt32BigEndian();
    if (focus is not null && !focus.Contains(className, StringComparer.Ordinal))
    {
        SkipFields(cursor, fieldCount);
        continue;
    }

    Console.WriteLine($"=== {className} ({package}) classId={classId} fields={fieldCount} ===");
    for (var f = 0; f < fieldCount; f++)
    {
        var fieldName = cursor.ReadUtf();
        var fieldType = cursor.ReadInt32BigEndian();
        var vector = "";
        if (fieldType == -99)
        {
            while (true)
            {
                var vectorName = cursor.ReadUtf();
                var vectorType = cursor.ReadInt32BigEndian();
                vector += $" -> {vectorType}:{vectorName}";
                if (vectorType != -99)
                {
                    break;
                }
            }
        }

        Console.WriteLine($"  {f + 1}. {fieldName} : {fieldType}{vector}");
    }

    Console.WriteLine();
}

static void SkipFields(Cursor cursor, int fieldCount)
{
    for (var f = 0; f < fieldCount; f++)
    {
        cursor.ReadUtf();
        var fieldType = cursor.ReadInt32BigEndian();
        if (fieldType == -99)
        {
            while (true)
            {
                cursor.ReadUtf();
                var vectorType = cursor.ReadInt32BigEndian();
                if (vectorType != -99)
                {
                    break;
                }
            }
        }
    }
}

internal sealed class Cursor(byte[] buffer)
{
    public int Position { get; set; }

    public string ReadAscii(int length)
    {
        var value = Encoding.ASCII.GetString(buffer, Position, length);
        Position += length;
        return value;
    }

    public int ReadInt32BigEndian()
    {
        var value = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(Position, 4));
        Position += 4;
        return value;
    }

    public string ReadUtf()
    {
        var length = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(Position, 2));
        Position += 2;
        var value = Encoding.UTF8.GetString(buffer, Position, length);
        Position += length;
        return value;
    }
}
