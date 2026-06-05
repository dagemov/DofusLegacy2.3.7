using System.ComponentModel.DataAnnotations;

namespace RollblackLegacy.Admin.Contracts.Items;

public sealed class ItemSearchRequest
{
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int? ItemId { get; set; }

    [Range(1, int.MaxValue)]
    public int? IconId { get; set; }

    [Range(1, int.MaxValue)]
    public int? TypeId { get; set; }

    [Range(0, int.MaxValue)]
    public int? LevelMin { get; set; }

    [Range(0, int.MaxValue)]
    public int? LevelMax { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
