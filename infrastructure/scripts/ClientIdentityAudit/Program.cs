using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

var options = AuditOptions.Parse(args);
var repoRoot = RepositoryRootResolver.Resolve(AppContext.BaseDirectory);
var paths = RepositoryPaths.FromRepoRoot(repoRoot);

var connectionString = ConfigurationLoader.LoadConnectionString(paths.AdminApiConfigDirectory);
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("SunshineAdmin no esta configurado. Crea o ajusta appsettings.Development.local.json antes de ejecutar la tool.");
    return 2;
}

var d2oReader = new RawD2oReader();
var itemsD2o = d2oReader.Load(paths.ItemsD2oPath);
var itemTypesD2o = d2oReader.Load(paths.ItemTypesD2oPath);
var itemSetsD2o = d2oReader.Load(paths.ItemSetsD2oPath);
var appearancesD2o = d2oReader.Load(paths.AppearancesD2oPath);
var i18nEs = D2iTextLookup.Load(paths.I18nEsPath);
var i18nEn = D2iTextLookup.Load(paths.I18nEnPath);

var dbItems = await LoadDbItemsAsync(connectionString, options.ItemIds);
var reportItems = BuildAuditItems(
    options.ItemIds,
    dbItems,
    itemsD2o,
    itemTypesD2o,
    itemSetsD2o,
    appearancesD2o,
    i18nEs,
    i18nEn,
    paths);

var report = MarkdownReportWriter.Write(new AuditReport(
    GeneratedAtUtc: DateTimeOffset.UtcNow,
    RepoRoot: repoRoot,
    Items: reportItems,
    Sources: new AuditSources(
        paths.ItemsD2oPath,
        paths.ItemTypesD2oPath,
        paths.ItemSetsD2oPath,
        paths.AppearancesD2oPath,
        paths.I18nEsPath,
        paths.I18nEnPath,
        paths.Bitmap0D2pPath,
        paths.Bitmap1D2pPath)));

if (!string.IsNullOrWhiteSpace(options.OutputPath))
{
    var outputPath = Path.IsPathRooted(options.OutputPath)
        ? options.OutputPath
        : Path.GetFullPath(Path.Combine(repoRoot, options.OutputPath));
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    await File.WriteAllTextAsync(outputPath, report, Encoding.UTF8);
    Console.WriteLine($"Reporte escrito en: {outputPath}");
}
else
{
    Console.WriteLine(report);
}

return 0;

static async Task<Dictionary<int, DbItemRecord>> LoadDbItemsAsync(string connectionString, IReadOnlyList<int> itemIds)
{
    const string sql = """
        SELECT
            i.Id,
            i.Name,
            i.DescriptionId,
            i.TypeId,
            i.Level,
            i.IconId,
            i.AppearanceId,
            i.ItemSetId
        FROM items AS i
        WHERE i.Id IN @ItemIds
        ORDER BY i.Id;
        """;

    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();

    var rows = await connection.QueryAsync<DbItemRecord>(sql, new { ItemIds = itemIds.ToArray() });
    return rows.ToDictionary(row => row.Id);
}

static IReadOnlyList<ItemAuditResult> BuildAuditItems(
    IReadOnlyList<int> itemIds,
    IReadOnlyDictionary<int, DbItemRecord> dbItems,
    RawD2oFile itemsD2o,
    RawD2oFile itemTypesD2o,
    RawD2oFile itemSetsD2o,
    RawD2oFile appearancesD2o,
    D2iTextLookup i18nEs,
    D2iTextLookup i18nEn,
    RepositoryPaths paths)
{
    var results = new List<ItemAuditResult>(itemIds.Count);

    foreach (var itemId in itemIds)
    {
        dbItems.TryGetValue(itemId, out var dbItem);
        var clientItem = itemsD2o.TryReadObject(itemId);

        var dbDescriptionId = dbItem?.DescriptionId;
        var clientDescriptionId = clientItem?.GetInt32("descriptionId");
        var clientNameId = clientItem?.GetInt32("nameId");
        var dbTypeId = dbItem?.TypeId;
        var clientTypeId = clientItem?.GetInt32("typeId");
        var dbIconId = dbItem?.IconId;
        var clientIconId = clientItem?.GetInt32("iconId");
        var dbAppearanceId = dbItem?.AppearanceId;
        var clientAppearanceId = clientItem?.GetInt32("appearanceId");
        var dbSetId = dbItem?.ItemSetId;
        var clientSetId = clientItem?.GetInt32("itemSetId");

        var statuses = new List<string>();
        if (clientItem is null)
        {
            statuses.Add("CLIENT_UNKNOWN");
            statuses.Add("NEEDS_CLIENT_PATCH");
        }
        else
        {
            statuses.Add("CLIENT_KNOWN");
            statuses.Add("SAFE_EXISTING_TEMPLATE");
        }

        if (dbDescriptionId.HasValue && !i18nEs.TryGetText(dbDescriptionId.Value, out _))
        {
            statuses.Add("I18N_MISSING_ES");
        }

        if (dbDescriptionId.HasValue && !i18nEn.TryGetText(dbDescriptionId.Value, out _))
        {
            statuses.Add("I18N_MISSING_EN");
        }

        if (!dbIconId.HasValue || dbIconId.Value <= 0)
        {
            statuses.Add("ICON_MISSING");
        }

        if (dbAppearanceId.HasValue && dbAppearanceId.Value > 0 && !appearancesD2o.ContainsIndex(dbAppearanceId.Value))
        {
            statuses.Add("APPEARANCE_UNKNOWN");
        }

        var previewByIconPath = dbIconId.HasValue && dbIconId.Value > 0
            ? Path.Combine(paths.AdminByIconPreviewDirectory, $"{dbIconId.Value}.png")
            : null;
        var previewByItemPath = Path.Combine(paths.AdminByItemPreviewDirectory, $"{itemId}.png");
        var previewPath = File.Exists(previewByItemPath)
            ? previewByItemPath
            : previewByIconPath is not null && File.Exists(previewByIconPath)
                ? previewByIconPath
                : null;

        var typeObject = clientTypeId.HasValue && itemTypesD2o.TryReadObject(clientTypeId.Value, out var tmpTypeObject)
            ? tmpTypeObject
            : null;
        var setObject = clientSetId.HasValue && clientSetId.Value > 0 && itemSetsD2o.TryReadObject(clientSetId.Value, out var tmpSetObject)
            ? tmpSetObject
            : null;

        results.Add(new ItemAuditResult(
            ItemId: itemId,
            DbName: dbItem?.Name,
            DbDescriptionId: dbDescriptionId,
            ClientNameId: clientNameId,
            ClientNameEs: clientNameId.HasValue && i18nEs.TryGetText(clientNameId.Value, out var clientNameEs) ? clientNameEs : null,
            ClientNameEn: clientNameId.HasValue && i18nEn.TryGetText(clientNameId.Value, out var clientNameEn) ? clientNameEn : null,
            DbDescriptionEs: dbDescriptionId.HasValue && i18nEs.TryGetText(dbDescriptionId.Value, out var dbDescriptionEs) ? dbDescriptionEs : null,
            DbDescriptionEn: dbDescriptionId.HasValue && i18nEn.TryGetText(dbDescriptionId.Value, out var dbDescriptionEn) ? dbDescriptionEn : null,
            ClientKnown: clientItem is not null,
            DbDescriptionMatchesClient: dbDescriptionId.HasValue && clientDescriptionId.HasValue && dbDescriptionId.Value == clientDescriptionId.Value,
            DbIconMatchesClient: dbIconId.HasValue && clientIconId.HasValue && dbIconId.Value == clientIconId.Value,
            DbAppearanceMatchesClient: NormalizeOptional(dbAppearanceId) == NormalizeOptional(clientAppearanceId),
            DbTypeId: dbTypeId,
            ClientTypeId: clientTypeId,
            ClientTypeNameEs: ResolveTypeName(typeObject, i18nEs),
            ClientTypeNameEn: ResolveTypeName(typeObject, i18nEn),
            DbSetId: dbSetId,
            ClientSetId: clientSetId,
            ClientSetNameEs: ResolveSetName(setObject, i18nEs),
            ClientSetNameEn: ResolveSetName(setObject, i18nEn),
            DbIconId: dbIconId,
            ClientIconId: clientIconId,
            DbAppearanceId: dbAppearanceId,
            ClientAppearanceId: clientAppearanceId,
            AppearanceKnown: dbAppearanceId.HasValue && dbAppearanceId.Value > 0 ? appearancesD2o.ContainsIndex(dbAppearanceId.Value) : null,
            PreviewPath: previewPath,
            BitmapPacksPresent: File.Exists(paths.Bitmap0D2pPath) || File.Exists(paths.Bitmap1D2pPath),
            Statuses: statuses.Distinct(StringComparer.Ordinal).ToArray()));
    }

    return results;
}

static int? NormalizeOptional(int? value) => value.GetValueOrDefault() <= 0 ? null : value;

static string? ResolveTypeName(RawD2oObject? typeObject, D2iTextLookup i18n)
{
    var nameId = typeObject?.GetInt32("nameId");
    return nameId.HasValue && i18n.TryGetText(nameId.Value, out var text) ? text : null;
}

static string? ResolveSetName(RawD2oObject? setObject, D2iTextLookup i18n)
{
    var nameId = setObject?.GetInt32("nameId");
    return nameId.HasValue && i18n.TryGetText(nameId.Value, out var text) ? text : null;
}

internal sealed record AuditOptions(IReadOnlyList<int> ItemIds, string? OutputPath)
{
    public static AuditOptions Parse(string[] args)
    {
        var items = new List<int>();
        string? output = null;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--items" when index + 1 < args.Length:
                    items.AddRange(args[++index]
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(static value => int.Parse(value, CultureInfo.InvariantCulture)));
                    break;
                case "--output" when index + 1 < args.Length:
                    output = args[++index];
                    break;
            }
        }

        if (items.Count == 0)
        {
            items.AddRange([7754, 12616, 12617, 39]);
        }

        return new AuditOptions(items.Distinct().ToArray(), output);
    }
}

internal static class RepositoryRootResolver
{
    public static string Resolve(string startPath)
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

        throw new DirectoryNotFoundException("No se pudo resolver la raiz del repo oficial desde la tool.");
    }
}

internal sealed record RepositoryPaths(
    string RepoRoot,
    string AdminApiConfigDirectory,
    string ItemsD2oPath,
    string ItemTypesD2oPath,
    string ItemSetsD2oPath,
    string AppearancesD2oPath,
    string I18nEsPath,
    string I18nEnPath,
    string Bitmap0D2pPath,
    string Bitmap1D2pPath,
    string AdminByItemPreviewDirectory,
    string AdminByIconPreviewDirectory)
{
    public static RepositoryPaths FromRepoRoot(string repoRoot)
    {
        return new RepositoryPaths(
            RepoRoot: repoRoot,
            AdminApiConfigDirectory: Path.Combine(repoRoot, "Angular-tools", "Admin", "RollblackLegacy.Admin.Api"),
            ItemsD2oPath: Path.Combine(repoRoot, "Client2.3.7", "data", "common", "Items.d2o"),
            ItemTypesD2oPath: Path.Combine(repoRoot, "Client2.3.7", "data", "common", "ItemTypes.d2o"),
            ItemSetsD2oPath: Path.Combine(repoRoot, "Client2.3.7", "data", "common", "ItemSets.d2o"),
            AppearancesD2oPath: Path.Combine(repoRoot, "Client2.3.7", "data", "common", "Appearances.d2o"),
            I18nEsPath: Path.Combine(repoRoot, "Client2.3.7", "data", "i18n", "i18n_es.d2i"),
            I18nEnPath: Path.Combine(repoRoot, "Client2.3.7", "data", "i18n", "i18n_en.d2i"),
            Bitmap0D2pPath: Path.Combine(repoRoot, "Client2.3.7", "content", "gfx", "items", "bitmap0.d2p"),
            Bitmap1D2pPath: Path.Combine(repoRoot, "Client2.3.7", "content", "gfx", "items", "bitmap1.d2p"),
            AdminByItemPreviewDirectory: Path.Combine(repoRoot, "Angular-tools", "Admin", "RollblackLegacy.Admin.Angular", "src", "assets", "item-previews", "by-item"),
            AdminByIconPreviewDirectory: Path.Combine(repoRoot, "Angular-tools", "Admin", "RollblackLegacy.Admin.Angular", "src", "assets", "item-previews", "by-icon"));
    }
}

internal static class ConfigurationLoader
{
    public static string? LoadConnectionString(string adminApiConfigDirectory)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(adminApiConfigDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.example.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.vps.example.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: false)
            .Build();

        return configuration.GetConnectionString("SunshineAdmin");
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

internal sealed class DbItemRecord
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int DescriptionId { get; set; }

    public int TypeId { get; set; }

    public int Level { get; set; }

    public int IconId { get; set; }

    public int AppearanceId { get; set; }

    public int ItemSetId { get; set; }
}

internal sealed record ItemAuditResult(
    int ItemId,
    string? DbName,
    int? DbDescriptionId,
    int? ClientNameId,
    string? ClientNameEs,
    string? ClientNameEn,
    string? DbDescriptionEs,
    string? DbDescriptionEn,
    bool ClientKnown,
    bool DbDescriptionMatchesClient,
    bool DbIconMatchesClient,
    bool DbAppearanceMatchesClient,
    int? DbTypeId,
    int? ClientTypeId,
    string? ClientTypeNameEs,
    string? ClientTypeNameEn,
    int? DbSetId,
    int? ClientSetId,
    string? ClientSetNameEs,
    string? ClientSetNameEn,
    int? DbIconId,
    int? ClientIconId,
    int? DbAppearanceId,
    int? ClientAppearanceId,
    bool? AppearanceKnown,
    string? PreviewPath,
    bool BitmapPacksPresent,
    IReadOnlyList<string> Statuses);

internal sealed record AuditSources(
    string ItemsD2oPath,
    string ItemTypesD2oPath,
    string ItemSetsD2oPath,
    string AppearancesD2oPath,
    string I18nEsPath,
    string I18nEnPath,
    string Bitmap0D2pPath,
    string Bitmap1D2pPath);

internal sealed record AuditReport(
    DateTimeOffset GeneratedAtUtc,
    string RepoRoot,
    IReadOnlyList<ItemAuditResult> Items,
    AuditSources Sources);

internal static class MarkdownReportWriter
{
    public static string Write(AuditReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Client Identity Item Check Report");
        builder.AppendLine();
        builder.AppendLine($"Generated: `{report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss 'UTC'}`");
        builder.AppendLine();
        builder.AppendLine("## Inputs");
        builder.AppendLine();
        builder.AppendLine($"- Repo: `{report.RepoRoot}`");
        builder.AppendLine($"- Items.d2o: `{report.Sources.ItemsD2oPath}`");
        builder.AppendLine($"- ItemTypes.d2o: `{report.Sources.ItemTypesD2oPath}`");
        builder.AppendLine($"- ItemSets.d2o: `{report.Sources.ItemSetsD2oPath}`");
        builder.AppendLine($"- Appearances.d2o: `{report.Sources.AppearancesD2oPath}`");
        builder.AppendLine($"- i18n_es.d2i: `{report.Sources.I18nEsPath}`");
        builder.AppendLine($"- i18n_en.d2i: `{report.Sources.I18nEnPath}`");
        builder.AppendLine($"- bitmap packs: `{report.Sources.Bitmap0D2pPath}`, `{report.Sources.Bitmap1D2pPath}`");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine("| ItemId | DB Name | Client | Statuses | Preview |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");

        foreach (var item in report.Items)
        {
            builder.AppendLine($"| `{item.ItemId}` | {Escape(item.DbName)} | {(item.ClientKnown ? "KNOWN" : "UNKNOWN")} | {Escape(string.Join(", ", item.Statuses))} | {Escape(item.PreviewPath is null ? "missing" : Path.GetFileName(item.PreviewPath))} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Detailed results");
        builder.AppendLine();

        foreach (var item in report.Items)
        {
            builder.AppendLine($"### Item `{item.ItemId}`");
            builder.AppendLine();
            builder.AppendLine($"- DB Name: `{item.DbName ?? "(missing)"}`");
            builder.AppendLine($"- Client known: `{item.ClientKnown}`");
            builder.AppendLine($"- Statuses: `{string.Join(", ", item.Statuses)}`");
            builder.AppendLine($"- Preview path: `{item.PreviewPath ?? "(missing)"}`");
            builder.AppendLine($"- Bitmap packs present: `{item.BitmapPacksPresent}`");
            builder.AppendLine($"- DB DescriptionId: `{item.DbDescriptionId?.ToString() ?? "(missing)"}`");
            builder.AppendLine($"- Client NameId: `{item.ClientNameId?.ToString() ?? "(missing)"}`");
            builder.AppendLine($"- DB Description ES: `{NormalizeInline(item.DbDescriptionEs)}`");
            builder.AppendLine($"- DB Description EN: `{NormalizeInline(item.DbDescriptionEn)}`");
            builder.AppendLine($"- Client Name ES: `{NormalizeInline(item.ClientNameEs)}`");
            builder.AppendLine($"- Client Name EN: `{NormalizeInline(item.ClientNameEn)}`");
            builder.AppendLine($"- DescriptionId matches client: `{item.DbDescriptionMatchesClient}`");
            builder.AppendLine($"- DB TypeId / Client TypeId: `{item.DbTypeId?.ToString() ?? "(missing)"} / {item.ClientTypeId?.ToString() ?? "(missing)"}`");
            builder.AppendLine($"- Client Type ES / EN: `{NormalizeInline(item.ClientTypeNameEs)}` / `{NormalizeInline(item.ClientTypeNameEn)}`");
            builder.AppendLine($"- DB SetId / Client SetId: `{item.DbSetId?.ToString() ?? "(missing)"} / {item.ClientSetId?.ToString() ?? "(missing)"}`");
            builder.AppendLine($"- Client Set ES / EN: `{NormalizeInline(item.ClientSetNameEs)}` / `{NormalizeInline(item.ClientSetNameEn)}`");
            builder.AppendLine($"- DB IconId / Client IconId: `{item.DbIconId?.ToString() ?? "(missing)"} / {item.ClientIconId?.ToString() ?? "(missing)"}`");
            builder.AppendLine($"- DB AppearanceId / Client AppearanceId: `{item.DbAppearanceId?.ToString() ?? "(missing)"} / {item.ClientAppearanceId?.ToString() ?? "(missing)"}`");
            builder.AppendLine($"- Icon matches client: `{item.DbIconMatchesClient}`");
            builder.AppendLine($"- Appearance matches client: `{item.DbAppearanceMatchesClient}`");
            builder.AppendLine($"- Appearance known: `{item.AppearanceKnown?.ToString() ?? "(n/a)"}`");
            builder.AppendLine();
        }

        builder.AppendLine("## Interpretation");
        builder.AppendLine();
        builder.AppendLine("- `CLIENT_KNOWN`: el `ItemId` existe en `Items.d2o`.");
        builder.AppendLine("- `CLIENT_UNKNOWN`: el `ItemId` no existe en `Items.d2o`.");
        builder.AppendLine("- `SAFE_EXISTING_TEMPLATE`: el cliente ya conoce el template actual.");
        builder.AppendLine("- `NEEDS_CLIENT_PATCH`: hace falta publicar template cliente o alinear metadata.");
        builder.AppendLine("- `I18N_MISSING_ES` / `I18N_MISSING_EN`: `DescriptionId` DB no resolvio en ese idioma.");
        builder.AppendLine("- `ICON_MISSING`: el item no trae `IconId` usable en DB.");
        builder.AppendLine("- `APPEARANCE_UNKNOWN`: `AppearanceId` > 0, pero no existe en `Appearances.d2o`.");
        return builder.ToString();
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Replace("|", "\\|");

    private static string NormalizeInline(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "(missing)";
        }

        return value.Replace("`", "'").Replace("\r", " ").Replace("\n", " ").Trim();
    }
}
