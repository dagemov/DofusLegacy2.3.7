using System.ComponentModel.DataAnnotations;

namespace RollblackLegacy.Admin.Contracts.Items;

public sealed class ItemIconSearchRequest
{
    public string? Search { get; set; }

    public string? NameEs { get; set; }

    public string? NameEn { get; set; }

    [Range(1, int.MaxValue)]
    public int? ItemId { get; set; }

    [Range(1, int.MaxValue)]
    public int? IconId { get; set; }

    /// <summary>by-icon | by-category</summary>
    public string? CatalogMode { get; set; }

    public string? Category { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 24;
}
