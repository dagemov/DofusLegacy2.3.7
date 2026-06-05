using System.Buffers.Binary;
using System.Text;

namespace ClientItemPublicationPipeline.D2o;

internal sealed class D2oSchemaParser
{
    public static D2oFileSchema Parse(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var reader = new BinaryCursor(bytes);
        var header = reader.ReadAscii(3);
        if (header != "D2O")
        {
            throw new InvalidDataException($"Invalid D2O header in '{path}'.");
        }

        reader.Position = reader.ReadInt32BigEndian();
        var indexLength = reader.ReadInt32BigEndian();
        var indexCount = indexLength / 8;
        reader.Position += indexLength;

        var classCount = reader.ReadInt32BigEndian();
        var classes = new List<D2oClassSchema>(classCount);
        for (var i = 0; i < classCount; i++)
        {
            var classId = reader.ReadInt32BigEndian();
            var className = reader.ReadUtf();
            var packageName = reader.ReadUtf();
            var fieldCount = reader.ReadInt32BigEndian();
            var fields = new List<D2oFieldSchema>(fieldCount);
            for (var f = 0; f < fieldCount; f++)
            {
                var fieldName = reader.ReadUtf();
                var fieldType = (D2oFieldTypeId)reader.ReadInt32BigEndian();
                var vectorTypes = new List<D2oVectorTypeSchema>();
                if (fieldType == D2oFieldTypeId.List)
                {
                    while (true)
                    {
                        var vectorName = reader.ReadUtf();
                        var vectorType = (D2oFieldTypeId)reader.ReadInt32BigEndian();
                        vectorTypes.Add(new D2oVectorTypeSchema(vectorType, vectorName));
                        if (vectorType != D2oFieldTypeId.List)
                        {
                            break;
                        }
                    }
                }

                fields.Add(new D2oFieldSchema(fieldName, fieldType, vectorTypes));
            }

            classes.Add(new D2oClassSchema(classId, className, packageName, fields));
        }

        return new D2oFileSchema(path, indexCount, classes);
    }
}

internal enum D2oFieldTypeId
{
    Int = -1,
    Bool = -2,
    String = -3,
    Double = -4,
    I18N = -5,
    UInt = -6,
    List = -99
}

internal sealed record D2oFileSchema(string Path, int IndexCount, IReadOnlyList<D2oClassSchema> Classes);

internal sealed record D2oClassSchema(int ClassId, string Name, string PackageName, IReadOnlyList<D2oFieldSchema> Fields);

internal sealed record D2oFieldSchema(string Name, D2oFieldTypeId Type, IReadOnlyList<D2oVectorTypeSchema> VectorTypes);

internal sealed record D2oVectorTypeSchema(D2oFieldTypeId Type, string Name);

internal sealed class BinaryCursor
{
    private readonly byte[] _buffer;

    public BinaryCursor(byte[] buffer) => _buffer = buffer;

    public int Position { get; set; }

    public string ReadAscii(int length)
    {
        var value = Encoding.ASCII.GetString(_buffer, Position, length);
        Position += length;
        return value;
    }

    public int ReadInt32BigEndian()
    {
        var value = BinaryPrimitives.ReadInt32BigEndian(_buffer.AsSpan(Position, 4));
        Position += 4;
        return value;
    }

    public string ReadUtf()
    {
        var length = BinaryPrimitives.ReadUInt16BigEndian(_buffer.AsSpan(Position, 2));
        Position += 2;
        var value = Encoding.UTF8.GetString(_buffer, Position, length);
        Position += length;
        return value;
    }
}
