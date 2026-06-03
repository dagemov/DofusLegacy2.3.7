using System.Buffers.Binary;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RollblackLegacy.Admin.Application.Abstractions.ClientIdentity;
using RollblackLegacy.Admin.Application.Models.ClientIdentity;
using RollblackLegacy.Admin.Infrastructure.Configuration;

namespace RollblackLegacy.Admin.Infrastructure.Services.ClientIdentity;

public sealed class FileSystemClientItemSourceReader : IClientItemSourceReader
{
    private readonly string _contentRootPath;
    private readonly AdminClientIdentityOptions _options;
    private readonly object _syncRoot = new();
    private ClientIdentitySourceCache? _cache;

    public FileSystemClientItemSourceReader(
        IHostEnvironment hostEnvironment,
        IOptions<AdminClientIdentityOptions> options)
    {
        _contentRootPath = hostEnvironment.ContentRootPath;
        _options = options.Value;
    }

    public Task<ClientItemSourceSnapshot> ReadAsync(ClientItemDbSnapshot dbItem, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            _cache ??= ClientIdentitySourceCache.Load(_contentRootPath, _options);
            return Task.FromResult(BuildSnapshot(dbItem, _cache));
        }
    }

    private static ClientItemSourceSnapshot BuildSnapshot(ClientItemDbSnapshot dbItem, ClientIdentitySourceCache cache)
    {
        var previewPath = ResolvePreviewPath(cache.Paths, dbItem.ItemId, dbItem.IconId);
        if (!cache.ClientDataAvailable)
        {
            return new ClientItemSourceSnapshot(
                dbItem.ItemId,
                ClientDataAvailable: false,
                ClientKnown: false,
                ClientDescriptionId: null,
                ClientNameId: null,
                ClientTypeId: null,
                ClientTypeNameEs: null,
                ClientTypeNameEn: null,
                ClientSetId: null,
                ClientSetNameEs: null,
                ClientSetNameEn: null,
                ClientIconId: null,
                ClientAppearanceId: null,
                AppearanceKnown: dbItem.AppearanceId > 0 ? false : null,
                IconPreviewFound: previewPath is not null,
                PreviewPath: previewPath,
                ClientNameEs: null,
                ClientNameEn: null,
                DbDescriptionEs: null,
                DbDescriptionEn: null,
                ClientRootPath: cache.Paths.ClientRootPath,
                ItemsD2oPath: cache.Paths.ItemsD2oPath,
                ItemTypesD2oPath: cache.Paths.ItemTypesD2oPath,
                ItemSetsD2oPath: cache.Paths.ItemSetsD2oPath,
                AppearancesD2oPath: cache.Paths.AppearancesD2oPath,
                I18nEsPath: cache.Paths.I18nEsPath,
                I18nEnPath: cache.Paths.I18nEnPath,
                FailureReason: cache.FailureReason);
        }

        var clientItem = cache.ItemsD2o.TryReadObject(dbItem.ItemId);
        var clientDescriptionId = clientItem?.GetInt32("descriptionId");
        var clientNameId = clientItem?.GetInt32("nameId");
        var clientTypeId = clientItem?.GetInt32("typeId");
        var clientSetId = clientItem?.GetInt32("itemSetId");
        var clientIconId = clientItem?.GetInt32("iconId");
        var clientAppearanceId = clientItem?.GetInt32("appearanceId");

        var typeObject = clientTypeId.HasValue && cache.ItemTypesD2o.TryReadObject(clientTypeId.Value, out var tmpTypeObject)
            ? tmpTypeObject
            : null;
        var setObject = clientSetId.HasValue && clientSetId.Value > 0 && cache.ItemSetsD2o.TryReadObject(clientSetId.Value, out var tmpSetObject)
            ? tmpSetObject
            : null;

        return new ClientItemSourceSnapshot(
            dbItem.ItemId,
            ClientDataAvailable: true,
            ClientKnown: clientItem is not null,
            ClientDescriptionId: clientDescriptionId,
            ClientNameId: clientNameId,
            ClientTypeId: clientTypeId,
            ClientTypeNameEs: ResolveTypeName(typeObject, cache.I18nEs),
            ClientTypeNameEn: ResolveTypeName(typeObject, cache.I18nEn),
            ClientSetId: clientSetId,
            ClientSetNameEs: ResolveSetName(setObject, cache.I18nEs),
            ClientSetNameEn: ResolveSetName(setObject, cache.I18nEn),
            ClientIconId: clientIconId,
            ClientAppearanceId: clientAppearanceId,
            AppearanceKnown: dbItem.AppearanceId > 0 ? cache.AppearancesD2o.ContainsIndex(dbItem.AppearanceId) : null,
            IconPreviewFound: previewPath is not null,
            PreviewPath: previewPath,
            ClientNameEs: clientNameId.HasValue && cache.I18nEs.TryGetText(clientNameId.Value, out var clientNameEs) ? clientNameEs : null,
            ClientNameEn: clientNameId.HasValue && cache.I18nEn.TryGetText(clientNameId.Value, out var clientNameEn) ? clientNameEn : null,
            DbDescriptionEs: cache.I18nEs.TryGetText(dbItem.DescriptionId, out var dbDescriptionEs) ? dbDescriptionEs : null,
            DbDescriptionEn: cache.I18nEn.TryGetText(dbItem.DescriptionId, out var dbDescriptionEn) ? dbDescriptionEn : null,
            ClientRootPath: cache.Paths.ClientRootPath,
            ItemsD2oPath: cache.Paths.ItemsD2oPath,
            ItemTypesD2oPath: cache.Paths.ItemTypesD2oPath,
            ItemSetsD2oPath: cache.Paths.ItemSetsD2oPath,
            AppearancesD2oPath: cache.Paths.AppearancesD2oPath,
            I18nEsPath: cache.Paths.I18nEsPath,
            I18nEnPath: cache.Paths.I18nEnPath,
            FailureReason: null);
    }

    private static string? ResolvePreviewPath(ClientIdentityRepositoryPaths paths, int itemId, int iconId)
    {
        var byItemPath = Path.Combine(paths.AdminByItemPreviewDirectory, $"{itemId}.png");
        if (File.Exists(byItemPath))
        {
            return byItemPath;
        }

        var byIconPath = iconId > 0
            ? Path.Combine(paths.AdminByIconPreviewDirectory, $"{iconId}.png")
            : null;
        if (byIconPath is not null && File.Exists(byIconPath))
        {
            return byIconPath;
        }

        var manualPath = Path.Combine(paths.AdminManualItemsDirectory, $"{itemId}.png");
        return File.Exists(manualPath) ? manualPath : null;
    }

    private static string? ResolveTypeName(RawD2oObject? typeObject, D2iTextLookup i18n)
    {
        var nameId = typeObject?.GetInt32("nameId");
        return nameId.HasValue && i18n.TryGetText(nameId.Value, out var text) ? text : null;
    }

    private static string? ResolveSetName(RawD2oObject? setObject, D2iTextLookup i18n)
    {
        var nameId = setObject?.GetInt32("nameId");
        return nameId.HasValue && i18n.TryGetText(nameId.Value, out var text) ? text : null;
    }
}

internal sealed record ClientIdentityRepositoryPaths(
    string RepoRootPath,
    string ClientRootPath,
    string AdminAngularRootPath,
    string ItemsD2oPath,
    string ItemTypesD2oPath,
    string ItemSetsD2oPath,
    string AppearancesD2oPath,
    string I18nEsPath,
    string I18nEnPath,
    string AdminByItemPreviewDirectory,
    string AdminByIconPreviewDirectory,
    string AdminManualItemsDirectory);

internal static class ClientIdentityRepositoryPathResolver
{
    public static ClientIdentityRepositoryPaths Resolve(string contentRootPath, AdminClientIdentityOptions options)
    {
        var repoRoot = ResolveRepoRoot(contentRootPath);
        var clientRootPath = ResolvePathOrDefault(
            options.ClientRootPath,
            contentRootPath,
            Path.Combine(repoRoot, "Client2.3.7"));
        var adminAngularRootPath = ResolvePathOrDefault(
            options.AdminAngularRootPath,
            contentRootPath,
            Path.Combine(repoRoot, "Angular-tools", "Admin", "RollblackLegacy.Admin.Angular"));

        return new ClientIdentityRepositoryPaths(
            repoRoot,
            clientRootPath,
            adminAngularRootPath,
            Path.Combine(clientRootPath, "data", "common", "Items.d2o"),
            Path.Combine(clientRootPath, "data", "common", "ItemTypes.d2o"),
            Path.Combine(clientRootPath, "data", "common", "ItemSets.d2o"),
            Path.Combine(clientRootPath, "data", "common", "Appearances.d2o"),
            Path.Combine(clientRootPath, "data", "i18n", "i18n_es.d2i"),
            Path.Combine(clientRootPath, "data", "i18n", "i18n_en.d2i"),
            Path.Combine(adminAngularRootPath, "src", "assets", "item-previews", "by-item"),
            Path.Combine(adminAngularRootPath, "src", "assets", "item-previews", "by-icon"),
            Path.Combine(adminAngularRootPath, "src", "assets", "manual-assets", "items"));
    }

    private static string ResolveRepoRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            var hasAdmin = Directory.Exists(Path.Combine(directory.FullName, "Angular-tools", "Admin"));
            var hasDocs = Directory.Exists(Path.Combine(directory.FullName, "docs"));
            if (hasAdmin && hasDocs)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("No se pudo resolver la raiz del repo oficial para Client Identity Audit.");
    }

    private static string ResolvePathOrDefault(string configuredPath, string contentRootPath, string fallbackAbsolutePath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return fallbackAbsolutePath;
        }

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
    }
}

internal sealed class ClientIdentitySourceCache
{
    private ClientIdentitySourceCache(ClientIdentityRepositoryPaths paths)
    {
        Paths = paths;
    }

    public ClientIdentityRepositoryPaths Paths { get; }

    public bool ClientDataAvailable { get; private set; }

    public string? FailureReason { get; private set; }

    public RawD2oFile ItemsD2o { get; private set; } = null!;

    public RawD2oFile ItemTypesD2o { get; private set; } = null!;

    public RawD2oFile ItemSetsD2o { get; private set; } = null!;

    public RawD2oFile AppearancesD2o { get; private set; } = null!;

    public D2iTextLookup I18nEs { get; private set; } = null!;

    public D2iTextLookup I18nEn { get; private set; } = null!;

    public static ClientIdentitySourceCache Load(string contentRootPath, AdminClientIdentityOptions options)
    {
        var paths = ClientIdentityRepositoryPathResolver.Resolve(contentRootPath, options);
        var cache = new ClientIdentitySourceCache(paths);

        try
        {
            var d2oReader = new RawD2oReader();
            cache.ItemsD2o = d2oReader.Load(paths.ItemsD2oPath);
            cache.ItemTypesD2o = d2oReader.Load(paths.ItemTypesD2oPath);
            cache.ItemSetsD2o = d2oReader.Load(paths.ItemSetsD2oPath);
            cache.AppearancesD2o = d2oReader.Load(paths.AppearancesD2oPath);
            cache.I18nEs = D2iTextLookup.Load(paths.I18nEsPath);
            cache.I18nEn = D2iTextLookup.Load(paths.I18nEnPath);
            cache.ClientDataAvailable = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            cache.ClientDataAvailable = false;
            cache.FailureReason = exception.Message;
        }

        return cache;
    }
}

internal sealed class RawD2oReader
{
    public RawD2oFile Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"No existe el archivo D2O requerido: {path}");
        }

        var bytes = File.ReadAllBytes(path);
        var reader = new BinaryCursor(bytes);

        var header = reader.ReadAscii(3);
        if (!string.Equals(header, "D2O", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Header D2O invalido en '{path}'.");
        }

        var headerOffset = reader.ReadInt32BigEndian();
        reader.Position = headerOffset;

        var indexLength = reader.ReadInt32BigEndian();
        if (indexLength < 0 || indexLength % 8 != 0)
        {
            throw new InvalidDataException($"Index length invalido en '{path}'.");
        }

        var indexTable = new Dictionary<int, int>(indexLength / 8);
        for (var offset = 0; offset < indexLength; offset += 8)
        {
            var objectId = reader.ReadInt32BigEndian();
            var objectOffset = reader.ReadInt32BigEndian();
            indexTable[objectId] = objectOffset;
        }

        var classCount = reader.ReadInt32BigEndian();
        var classes = new Dictionary<int, RawD2oClassDefinition>(classCount);

        for (var index = 0; index < classCount; index++)
        {
            var classId = reader.ReadInt32BigEndian();
            var className = reader.ReadUtf();
            var packageName = reader.ReadUtf();
            var fieldCount = reader.ReadInt32BigEndian();
            var fields = new List<RawD2oFieldDefinition>(fieldCount);

            for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
            {
                var fieldName = reader.ReadUtf();
                var fieldType = (RawD2oFieldType)reader.ReadInt32BigEndian();
                var vectorTypes = new List<RawD2oVectorType>();

                if (fieldType == RawD2oFieldType.List)
                {
                    while (true)
                    {
                        var vectorName = reader.ReadUtf();
                        var vectorType = (RawD2oFieldType)reader.ReadInt32BigEndian();
                        vectorTypes.Add(new RawD2oVectorType(vectorType, vectorName));
                        if (vectorType != RawD2oFieldType.List)
                        {
                            break;
                        }
                    }
                }

                fields.Add(new RawD2oFieldDefinition(fieldName, fieldType, vectorTypes));
            }

            classes[classId] = new RawD2oClassDefinition(classId, className, packageName, fields);
        }

        return new RawD2oFile(path, bytes, indexTable, classes);
    }
}

internal sealed class RawD2oFile
{
    private const int NullIdentifier = unchecked((int)0xAAAAAAAA);

    private readonly byte[] _buffer;
    private readonly IReadOnlyDictionary<int, int> _indexes;
    private readonly IReadOnlyDictionary<int, RawD2oClassDefinition> _classes;

    public RawD2oFile(
        string path,
        byte[] buffer,
        IReadOnlyDictionary<int, int> indexes,
        IReadOnlyDictionary<int, RawD2oClassDefinition> classes)
    {
        Path = path;
        _buffer = buffer;
        _indexes = indexes;
        _classes = classes;
    }

    public string Path { get; }

    public bool ContainsIndex(int index) => _indexes.ContainsKey(index);

    public RawD2oObject? TryReadObject(int index)
    {
        return TryReadObject(index, out var value) ? value : null;
    }

    public bool TryReadObject(int index, out RawD2oObject? value)
    {
        value = null;
        if (!_indexes.TryGetValue(index, out var offset))
        {
            return false;
        }

        var reader = new BinaryCursor(_buffer) { Position = offset };
        var classId = reader.ReadInt32BigEndian();
        if (!_classes.TryGetValue(classId, out var classDefinition))
        {
            return false;
        }

        var fields = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in classDefinition.Fields)
        {
            fields[field.Name] = ReadField(reader, field.Type, field.VectorTypes, 0);
        }

        value = new RawD2oObject(index, classDefinition.Name, fields);
        return true;
    }

    private object? ReadField(BinaryCursor reader, RawD2oFieldType type, IReadOnlyList<RawD2oVectorType> vectorTypes, int depth)
    {
        return type switch
        {
            RawD2oFieldType.Int => reader.ReadInt32BigEndian(),
            RawD2oFieldType.Bool => reader.ReadByte() != 0,
            RawD2oFieldType.String => reader.ReadUtf(),
            RawD2oFieldType.Double => reader.ReadDoubleBigEndian(),
            RawD2oFieldType.I18N => reader.ReadInt32BigEndian(),
            RawD2oFieldType.UInt => reader.ReadUInt32BigEndian(),
            RawD2oFieldType.List => ReadList(reader, vectorTypes, depth),
            _ => ReadObject(reader),
        };
    }

    private List<object?> ReadList(BinaryCursor reader, IReadOnlyList<RawD2oVectorType> vectorTypes, int depth)
    {
        var count = reader.ReadInt32BigEndian();
        var list = new List<object?>(count);
        var entryType = vectorTypes[depth].Type;

        for (var index = 0; index < count; index++)
        {
            list.Add(ReadField(reader, entryType, vectorTypes, depth + 1));
        }

        return list;
    }

    private object? ReadObject(BinaryCursor reader)
    {
        var classId = reader.ReadInt32BigEndian();
        if (classId == NullIdentifier)
        {
            return null;
        }

        if (!_classes.TryGetValue(classId, out var classDefinition))
        {
            return null;
        }

        var fields = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in classDefinition.Fields)
        {
            fields[field.Name] = ReadField(reader, field.Type, field.VectorTypes, 0);
        }

        return new RawD2oObject(null, classDefinition.Name, fields);
    }
}

internal sealed class D2iTextLookup
{
    private readonly byte[] _buffer;
    private readonly Dictionary<int, int> _indexOffsets;

    private D2iTextLookup(string path, byte[] buffer, Dictionary<int, int> indexOffsets)
    {
        Path = path;
        _buffer = buffer;
        _indexOffsets = indexOffsets;
    }

    public string Path { get; }

    public static D2iTextLookup Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"No existe el archivo D2I requerido: {path}");
        }

        var buffer = File.ReadAllBytes(path);
        var reader = new BinaryCursor(buffer);
        var dataSize = reader.ReadInt32BigEndian();
        reader.Position = dataSize;
        var indexSize = reader.ReadInt32BigEndian();
        var indexOffsets = new Dictionary<int, int>(indexSize / 8);
        var end = dataSize + 4 + indexSize;

        reader.Position = dataSize + 4;
        while (reader.Position < end)
        {
            var id = reader.ReadInt32BigEndian();
            var offset = reader.ReadInt32BigEndian();
            indexOffsets[id] = offset;
        }

        return new D2iTextLookup(path, buffer, indexOffsets);
    }

    public bool TryGetText(int id, out string? value)
    {
        if (_indexOffsets.TryGetValue(id, out var offset))
        {
            value = ReadReferencedString(offset);
            return true;
        }

        value = null;
        return false;
    }

    private string ReadReferencedString(int offset)
    {
        var reader = new BinaryCursor(_buffer) { Position = offset };
        var length = reader.ReadUInt16BigEndian();
        return reader.ReadUtfBytes(length);
    }
}

internal sealed class BinaryCursor
{
    private readonly byte[] _buffer;

    public BinaryCursor(byte[] buffer)
    {
        _buffer = buffer;
    }

    public int Position { get; set; }

    public byte ReadByte() => _buffer[Position++];

    public string ReadAscii(int length)
    {
        var value = Encoding.ASCII.GetString(_buffer, Position, length);
        Position += length;
        return value;
    }

    public ushort ReadUInt16BigEndian()
    {
        var value = BinaryPrimitives.ReadUInt16BigEndian(_buffer.AsSpan(Position, 2));
        Position += 2;
        return value;
    }

    public uint ReadUInt32BigEndian()
    {
        var value = BinaryPrimitives.ReadUInt32BigEndian(_buffer.AsSpan(Position, 4));
        Position += 4;
        return value;
    }

    public int ReadInt32BigEndian()
    {
        var value = BinaryPrimitives.ReadInt32BigEndian(_buffer.AsSpan(Position, 4));
        Position += 4;
        return value;
    }

    public double ReadDoubleBigEndian()
    {
        var bits = BinaryPrimitives.ReadInt64BigEndian(_buffer.AsSpan(Position, 8));
        Position += 8;
        return BitConverter.Int64BitsToDouble(bits);
    }

    public string ReadUtf()
    {
        var length = ReadUInt16BigEndian();
        return ReadUtfBytes(length);
    }

    public string ReadUtfBytes(int length)
    {
        var value = Encoding.UTF8.GetString(_buffer, Position, length);
        Position += length;
        return value;
    }
}

internal enum RawD2oFieldType
{
    Int = -1,
    Bool = -2,
    String = -3,
    Double = -4,
    I18N = -5,
    UInt = -6,
    List = -99
}

internal sealed record RawD2oVectorType(RawD2oFieldType Type, string Name);

internal sealed record RawD2oFieldDefinition(
    string Name,
    RawD2oFieldType Type,
    IReadOnlyList<RawD2oVectorType> VectorTypes);

internal sealed record RawD2oClassDefinition(
    int Id,
    string Name,
    string PackageName,
    IReadOnlyList<RawD2oFieldDefinition> Fields);

internal sealed class RawD2oObject
{
    private readonly IReadOnlyDictionary<string, object?> _fields;

    public RawD2oObject(int? index, string className, IReadOnlyDictionary<string, object?> fields)
    {
        Index = index;
        ClassName = className;
        _fields = fields;
    }

    public int? Index { get; }

    public string ClassName { get; }

    public int? GetInt32(string fieldName)
    {
        if (!_fields.TryGetValue(fieldName, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            int intValue => intValue,
            uint uintValue when uintValue <= int.MaxValue => (int)uintValue,
            long longValue when longValue is >= int.MinValue and <= int.MaxValue => (int)longValue,
            _ => null,
        };
    }
}
