namespace ClientItemPublicationPipeline.Package;

internal static class ClientPatchD2oIndex
{
    public static HashSet<int> ReadIds(string d2oPath)
    {
        using var stream = File.OpenRead(d2oPath);
        var header = new byte[3];
        stream.ReadExactly(header);
        var buffer = new byte[4];
        stream.ReadExactly(buffer);
        stream.Position = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer);
        stream.ReadExactly(buffer);
        var indexLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer);
        var count = indexLength / 8;
        var ids = new HashSet<int>(count);
        for (var index = 0; index < count; index++)
        {
            stream.ReadExactly(buffer);
            var id = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer);
            stream.ReadExactly(buffer);
            ids.Add(id);
        }

        return ids;
    }
}
