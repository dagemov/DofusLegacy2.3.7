using System.Buffers.Binary;
using System.Text;

namespace ClientItemPublicationPipeline.D2i;

/// <summary>
/// Read/write D2I (client i18n) files: [data pool][indexSize:int][id,offset pairs...].
/// </summary>
internal sealed class D2iFile
{
    private readonly List<D2iEntry> _entries = new();

    public string? SourcePath { get; private set; }

    public IReadOnlyList<D2iEntry> Entries => _entries;

    public int Count => _entries.Count;

    public static D2iFile Load(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var file = new D2iFile { SourcePath = path };
        var reader = new D2iBinaryCursor(bytes);
        var dataSize = reader.ReadInt32BigEndian();
        reader.Position = dataSize;
        var indexSize = reader.ReadInt32BigEndian();
        var end = dataSize + 4 + indexSize;
        reader.Position = dataSize + 4;

        while (reader.Position < end)
        {
            var id = reader.ReadInt32BigEndian();
            var offset = reader.ReadInt32BigEndian();
            var textReader = new D2iBinaryCursor(bytes) { Position = offset };
            var length = textReader.ReadUInt16BigEndian();
            var text = textReader.ReadUtfBytes(length);
            file._entries.Add(new D2iEntry(id, offset, text));
        }

        return file;
    }

    public static D2iInspectResult Inspect(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var reader = new D2iBinaryCursor(bytes);
        var dataSize = reader.ReadInt32BigEndian();
        reader.Position = dataSize;
        var indexSize = reader.ReadInt32BigEndian();
        var indexCount = indexSize / 8;
        var file = Load(path);
        var minId = file._entries.Count > 0 ? file._entries.Min(e => e.Id) : 0;
        var maxId = file._entries.Count > 0 ? file._entries.Max(e => e.Id) : 0;

        return new D2iInspectResult(
            path,
            bytes.Length,
            dataSize,
            indexSize,
            indexCount,
            minId,
            maxId,
            HasMagicHeader(bytes));
    }

    private static bool HasMagicHeader(byte[] bytes) =>
        bytes.Length >= 3
        && bytes[0] != (byte)'D'
        && bytes[1] != (byte)'2'
        && bytes[2] != (byte)'I';

    public bool TryGetText(int id, out string? text)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == id);
        if (entry is null)
        {
            text = null;
            return false;
        }

        text = entry.Text;
        return true;
    }

    public int AllocateNextId() =>
        _entries.Count == 0 ? 1 : _entries.Max(e => e.Id) + 1;

    public void AppendText(int id, string text)
    {
        if (_entries.Any(e => e.Id == id))
        {
            throw new InvalidOperationException($"El textId {id} ya existe; use actualización explícita si se implementa.");
        }

        _entries.Add(new D2iEntry(id, -1, text));
    }

    public void AppendTexts(IEnumerable<(int Id, string Text)> items)
    {
        foreach (var (id, text) in items)
        {
            AppendText(id, text);
        }
    }

    public void Save(string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var ordered = _entries.OrderBy(e => e.Id).ToList();
        using var stringPool = new MemoryStream();
        var stringWriter = new BinaryWriter(stringPool, Encoding.UTF8, leaveOpen: true);
        var indexPairs = new List<(int Id, int Offset)>(ordered.Count);

        foreach (var entry in ordered)
        {
            var offset = 4 + (int)stringPool.Position;
            var bytes = Encoding.UTF8.GetBytes(entry.Text);
            if (bytes.Length > ushort.MaxValue)
            {
                throw new InvalidOperationException($"Texto demasiado largo para textId {entry.Id} ({bytes.Length} bytes).");
            }

            WriteUInt16BigEndian(stringWriter, (ushort)bytes.Length);
            stringWriter.Write(bytes);
            indexPairs.Add((entry.Id, offset));
        }

        var dataSize = 4 + (int)stringPool.Length;
        using var indexStream = new MemoryStream();
        var indexWriter = new BinaryWriter(indexStream, Encoding.UTF8, leaveOpen: true);
        WriteInt32BigEndian(indexWriter, indexPairs.Count * 8);
        foreach (var (id, offset) in indexPairs)
        {
            WriteInt32BigEndian(indexWriter, id);
            WriteInt32BigEndian(indexWriter, offset);
        }

        using var output = File.Create(outputPath);
        WriteInt32BigEndian(output, dataSize);
        stringPool.Position = 0;
        stringPool.CopyTo(output);
        indexStream.Position = 0;
        indexStream.CopyTo(output);
    }

    public static void CopyToStaging(string sourcePath, string stagingPath)
    {
        var directory = Path.GetDirectoryName(stagingPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.Copy(sourcePath, stagingPath, overwrite: true);
    }

    private static void WriteInt32BigEndian(BinaryWriter writer, int value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        writer.Write(buffer);
    }

    private static void WriteInt32BigEndian(Stream stream, int value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteUInt16BigEndian(BinaryWriter writer, ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
        writer.Write(buffer);
    }
}

internal sealed record D2iEntry(int Id, int OriginalOffset, string Text);

internal sealed record D2iInspectResult(
    string Path,
    int FileSizeBytes,
    int DataSize,
    int IndexSize,
    int IndexCount,
    int MinTextId,
    int MaxTextId,
    bool NoD2iMagicHeader);
