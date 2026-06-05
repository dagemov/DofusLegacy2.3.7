using System.ComponentModel.DataAnnotations;

namespace RollblackLegacy.Admin.Contracts.Spells;

public sealed class SpellCatalogSearchRequest
{
    public string? Search { get; set; }

    [Range(1, short.MaxValue)]
    public short? SpellId { get; set; }

    [Range(1, int.MaxValue)]
    public int? BreedId { get; set; }

    [Range(0, int.MaxValue)]
    public int? TypeId { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
