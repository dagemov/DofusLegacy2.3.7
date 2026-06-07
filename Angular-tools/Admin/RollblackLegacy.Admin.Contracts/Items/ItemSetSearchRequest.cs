using System.ComponentModel.DataAnnotations;

namespace RollblackLegacy.Admin.Contracts.Items;

public sealed class ItemSetSearchRequest
{
    public string? Search { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinLevel { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxLevel { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinParts { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxParts { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
