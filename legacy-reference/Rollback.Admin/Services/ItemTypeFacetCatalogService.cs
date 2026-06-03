using Rollback.Admin.Models.Items;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public static class ItemTypeFacetCatalogService
{
    private static readonly IReadOnlyList<ItemTypeFacet> _facets = new[]
    {
        new ItemTypeFacet("hat", "Sombrero", new[] { ItemType.Chapeau }),
        new ItemTypeFacet("cape", "Capa", new[] { ItemType.Cape, ItemType.SacADos }),
        new ItemTypeFacet("amulet", "Amuleto", new[] { ItemType.Amulette }),
        new ItemTypeFacet("ring", "Anillo", new[] { ItemType.Anneau }),
        new ItemTypeFacet("belt", "Cinturon", new[] { ItemType.Ceinture }),
        new ItemTypeFacet("boots", "Botas", new[] { ItemType.Bottes }),
        new ItemTypeFacet("weapon", "Arma", new[] { ItemType.Arc, ItemType.Baguette, ItemType.Baton, ItemType.Dague, ItemType.Epee, ItemType.Marteau, ItemType.Pelle, ItemType.Hache, ItemType.Faux, ItemType.FiletDeCapture }),
        new ItemTypeFacet("shield", "Escudo", new[] { ItemType.Bouclier }),
        new ItemTypeFacet("pet", "Mascota", new[] { ItemType.Familier }),
        new ItemTypeFacet("mount", "Montura", new[] { ItemType.Dragodinde }),
        new ItemTypeFacet("dofus", "Dofus", new[] { ItemType.Dofus }),
    };

    public static IReadOnlyList<ItemTypeFacet> GetFacets() =>
        _facets;

    public static IReadOnlyCollection<ItemType> ResolveTypes(string? key) =>
        _facets.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))?.Types
        ?? Array.Empty<ItemType>();
}
