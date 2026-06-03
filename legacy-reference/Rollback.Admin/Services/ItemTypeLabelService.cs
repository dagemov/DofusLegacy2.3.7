using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public static class ItemTypeLabelService
{
    public static string GetDisplayName(ItemType itemType) =>
        itemType switch
        {
            ItemType.Amulette => "Amuleto",
            ItemType.Anneau => "Anillo",
            ItemType.Ceinture => "Cinturon",
            ItemType.Bottes => "Botas",
            ItemType.Chapeau => "Sombrero",
            ItemType.Cape => "Capa",
            ItemType.Dofus => "Dofus",
            ItemType.Baguette => "Varita",
            ItemType.Baton => "Baston",
            ItemType.Epee => "Espada",
            ItemType.Dague => "Daga",
            ItemType.Marteau => "Martillo",
            ItemType.Pelle => "Pala",
            ItemType.Arc => "Arco",
            ItemType.Bouclier => "Escudo",
            ItemType.Familier => "Mascota",
            ItemType.Dragodinde => "Montura",
            _ => itemType.ToString()
        };

    public static string ToPlural(string typeLabel) =>
        typeLabel.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? typeLabel : $"{typeLabel}s";

    public static string GetShortCode(ItemType itemType) =>
        itemType switch
        {
            ItemType.Amulette => "AM",
            ItemType.Anneau => "AN",
            ItemType.Ceinture => "CI",
            ItemType.Bottes => "BO",
            ItemType.Chapeau => "SO",
            ItemType.Cape => "CA",
            ItemType.Dofus => "DF",
            ItemType.Baguette or ItemType.Baton or ItemType.Epee or ItemType.Dague or ItemType.Marteau or ItemType.Pelle or ItemType.Arc => "AR",
            ItemType.Bouclier => "ES",
            ItemType.Familier => "MA",
            ItemType.Dragodinde => "MO",
            _ => "IT",
        };
}
