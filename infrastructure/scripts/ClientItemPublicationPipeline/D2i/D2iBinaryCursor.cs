using System.Buffers.Binary;
using System.Text;

namespace ClientItemPublicationPipeline.D2i;

internal sealed class D2iBinaryCursor(byte[] buffer)
{
    public int Position { get; set; }

    public int ReadInt32BigEndian()
    {
        var value = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(Position, 4));
        Position += 4;
        return value;
    }

    public ushort ReadUInt16BigEndian()
    {
        var value = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(Position, 2));
        Position += 2;
        return value;
    }

    public string ReadUtfBytes(int length)
    {
        var value = Encoding.UTF8.GetString(buffer, Position, length);
        Position += length;
        return value;
    }
}
