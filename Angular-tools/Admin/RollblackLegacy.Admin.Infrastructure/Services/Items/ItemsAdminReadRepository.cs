using Dapper;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Models.Items;
using RollblackLegacy.Admin.Contracts.Common;
using RollblackLegacy.Admin.Contracts.Items;
using RollblackLegacy.Admin.Infrastructure.Data;
using RollblackLegacy.Admin.Infrastructure.Items;
using Microsoft.Extensions.Hosting;

namespace RollblackLegacy.Admin.Infrastructure.Services.Items;

public sealed class ItemsAdminReadRepository : IItemsAdminReadRepository
{
    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly AdminProtocolCatalog _protocolCatalog;
    private readonly IHostEnvironment _hostEnvironment;

    public ItemsAdminReadRepository(
        AdminDbConnectionFactory connectionFactory,
        AdminProtocolCatalog protocolCatalog,
        IHostEnvironment hostEnvironment)
    {
        _connectionFactory = connectionFactory;
        _protocolCatalog = protocolCatalog;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<AdminPagedItemsReadModel> SearchAsync(
        ItemSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        const string countSql = """
            SELECT COUNT(*)
            FROM items AS i
            LEFT JOIN items_sets AS s ON s.Id = i.ItemSetId
            WHERE (@Search IS NULL OR i.Name LIKE CONCAT('%', @Search, '%'))
              AND (@ItemId IS NULL OR i.Id = @ItemId)
              AND (@IconId IS NULL OR i.IconId = @IconId)
              AND (@TypeId IS NULL OR i.TypeId = @TypeId)
              AND (@LevelMin IS NULL OR i.Level >= @LevelMin)
              AND (@LevelMax IS NULL OR i.Level <= @LevelMax);
            """;

        const string pageSql = """
            SELECT
                i.Id AS ItemId,
                i.Name AS ResolvedName,
                i.TypeId,
                i.Level,
                i.IconId,
                i.AppearanceId,
                i.ItemSetId AS RawSetId,
                s.Name AS SetName
            FROM items AS i
            LEFT JOIN items_sets AS s ON s.Id = i.ItemSetId
            WHERE (@Search IS NULL OR i.Name LIKE CONCAT('%', @Search, '%'))
              AND (@ItemId IS NULL OR i.Id = @ItemId)
              AND (@IconId IS NULL OR i.IconId = @IconId)
              AND (@TypeId IS NULL OR i.TypeId = @TypeId)
              AND (@LevelMin IS NULL OR i.Level >= @LevelMin)
              AND (@LevelMax IS NULL OR i.Level <= @LevelMax)
            ORDER BY i.Id
            LIMIT @Offset, @PageSize;
            """;

        var parameters = new
        {
            Search = NormalizeSearch(request.Search),
            request.ItemId,
            request.IconId,
            request.TypeId,
            request.LevelMin,
            request.LevelMax,
            Offset = (request.Page - 1) * request.PageSize,
            request.PageSize,
        };

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            countSql,
            parameters,
            cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<ItemListRow>(new CommandDefinition(
            pageSql,
            parameters,
            cancellationToken: cancellationToken));

        var items = rows
            .Select(x => new AdminItemListReadModel(
                x.ItemId,
                x.ResolvedName,
                x.TypeId,
                _protocolCatalog.GetItemTypeLabel(x.TypeId),
                x.Level,
                NormalizeSetId(x.RawSetId),
                x.SetName,
                x.IconId,
                x.AppearanceId))
            .ToList();

        return new AdminPagedItemsReadModel(totalCount, items);
    }

    public async Task<ItemPagedResultDto<ItemIconOptionDto>> SearchIconsAsync(
        ItemIconSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = NormalizeSearch(request.Search);
        var iconRoot = AdminRepositoryPathResolver.ResolveAdminAngularByIconRoot(_hostEnvironment.ContentRootPath);

        if (!Directory.Exists(iconRoot))
        {
            return new ItemPagedResultDto<ItemIconOptionDto>(
                request.Page,
                request.PageSize,
                0,
                []);
        }

        var options = Directory
            .EnumerateFiles(iconRoot, "*.png", SearchOption.TopDirectoryOnly)
            .Select(TryMapIconOption)
            .Where(x => x is not null)
            .Select(x => x!)
            .Where(x => !request.IconId.HasValue || x.IconId == request.IconId.Value)
            .Where(x => normalizedSearch is null || MatchesSearch(x, normalizedSearch))
            .OrderBy(x => x.IconId)
            .ToList();

        var totalCount = options.Count;
        var offset = Math.Max(0, (request.Page - 1) * request.PageSize);
        var paged = options
            .Skip(offset)
            .Take(request.PageSize)
            .ToList();

        var metadataByIconId = await TryLoadIconMetadataAsync(
            paged.Select(x => x.IconId).ToArray(),
            cancellationToken);

        var hydrated = paged
            .Select(option =>
            {
                if (!metadataByIconId.TryGetValue(option.IconId, out var metadata))
                {
                    return option;
                }

                return option with
                {
                    LinkedItemCount = metadata.LinkedItemCount,
                    SampleItemNames = metadata.SampleItemNames
                };
            })
            .ToList();

        return new ItemPagedResultDto<ItemIconOptionDto>(
            request.Page,
            request.PageSize,
            totalCount,
            hydrated);
    }

    public async Task<AdminItemDetailReadModel?> GetByIdAsync(int itemId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                i.Id AS ItemId,
                i.Name AS ResolvedName,
                i.DescriptionId,
                i.TypeId,
                i.Level,
                i.Weight,
                i.Price,
                i.Usable,
                i.Targetable,
                i.TwoHanded,
                i.Etheral,
                i.Criteria,
                i.IconId,
                i.AppearanceId,
                i.ItemSetId AS RawSetId,
                i.Effects,
                s.Name AS SetName
            FROM items AS i
            LEFT JOIN items_sets AS s ON s.Id = i.ItemSetId
            WHERE i.Id = @ItemId
            LIMIT 1;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ItemDetailRow>(new CommandDefinition(
            sql,
            new { ItemId = itemId },
            cancellationToken: cancellationToken));

        if (row is null)
            return null;

        return new AdminItemDetailReadModel(
            row.ItemId,
            row.ResolvedName,
            row.DescriptionId,
            row.TypeId,
            _protocolCatalog.GetItemTypeLabel(row.TypeId),
            row.Level,
            row.Weight,
            row.Price,
            row.Usable,
            row.Targetable,
            row.TwoHanded,
            row.Etheral,
            row.Criteria,
            row.IconId,
            row.AppearanceId,
            NormalizeSetId(row.RawSetId),
            row.SetName,
            _protocolCatalog.DecodeItemEffects(row.Effects));
    }

    public Task<IReadOnlyList<AdminOptionDto>> GetTypeOptionsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AdminOptionDto> options = _protocolCatalog.GetItemTypeOptions();
        return Task.FromResult(options);
    }

    public async Task<IReadOnlyList<AdminOptionDto>> GetItemSetOptionsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Id AS Value,
                Name AS Label
            FROM items_sets
            ORDER BY Name;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<ItemSetOptionRow>(new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));

        return rows
            .Select(x => new AdminOptionDto((int)x.Value, x.Label))
            .ToList();
    }

    private static string? NormalizeSearch(string? search)
    {
        return string.IsNullOrWhiteSpace(search) ? null : search.Trim();
    }

    private static int? NormalizeSetId(int rawSetId)
    {
        return rawSetId > 0 ? rawSetId : null;
    }

    private static ItemIconOptionDto? TryMapIconOption(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        if (!int.TryParse(fileName, out var iconId) || iconId <= 0)
        {
            return null;
        }

        return new ItemIconOptionDto(
            iconId,
            $"/assets/item-previews/by-icon/{iconId}.png",
            "FOUND",
            "CURATED_BY_ICON",
            HasPreview: true,
            LinkedItemCount: 0,
            SampleItemNames: []);
    }

    private static bool MatchesSearch(ItemIconOptionDto option, string search)
    {
        if (option.IconId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return option.SampleItemNames.Any(name => name.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Dictionary<int, ItemIconMetadata>> TryLoadIconMetadataAsync(
        IReadOnlyCollection<int> iconIds,
        CancellationToken cancellationToken)
    {
        if (iconIds.Count == 0)
        {
            return [];
        }

        const string sql = """
            SELECT
                IconId,
                COUNT(*) AS LinkedItemCount,
                GROUP_CONCAT(Name ORDER BY Level ASC SEPARATOR '||') AS SampleNames
            FROM items
            WHERE IconId IN @IconIds
            GROUP BY IconId;
            """;

        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            var rows = await connection.QueryAsync<ItemIconMetadataRow>(new CommandDefinition(
                sql,
                new { IconIds = iconIds.ToArray() },
                cancellationToken: cancellationToken));

            return rows.ToDictionary(
                row => row.IconId,
                row => new ItemIconMetadata(
                    row.LinkedItemCount,
                    row.SampleNames?
                        .Split("||", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(3)
                        .ToArray() ?? []));
        }
        catch
        {
            return [];
        }
    }

    private sealed class ItemListRow
    {
        public int ItemId { get; set; }

        public string? ResolvedName { get; set; }

        public int TypeId { get; set; }

        public int Level { get; set; }

        public int IconId { get; set; }

        public int AppearanceId { get; set; }

        public int RawSetId { get; set; }

        public string? SetName { get; set; }
    }

    private sealed class ItemDetailRow
    {
        public int ItemId { get; set; }

        public string? ResolvedName { get; set; }

        public int DescriptionId { get; set; }

        public int TypeId { get; set; }

        public int Level { get; set; }

        public int Weight { get; set; }

        public double Price { get; set; }

        public bool Usable { get; set; }

        public bool Targetable { get; set; }

        public bool TwoHanded { get; set; }

        public bool Etheral { get; set; }

        public string? Criteria { get; set; }

        public int IconId { get; set; }

        public int AppearanceId { get; set; }

        public int RawSetId { get; set; }

        public string? Effects { get; set; }

        public string? SetName { get; set; }
    }

    private sealed class ItemSetOptionRow
    {
        public uint Value { get; set; }

        public string Label { get; set; } = string.Empty;
    }

    private sealed class ItemIconMetadataRow
    {
        public int IconId { get; set; }

        public int LinkedItemCount { get; set; }

        public string? SampleNames { get; set; }
    }

    private sealed record ItemIconMetadata(
        int LinkedItemCount,
        IReadOnlyList<string> SampleItemNames);
}
