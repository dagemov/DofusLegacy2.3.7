using Rollback.Admin.Models.GameEffects;

namespace Rollback.Admin.Models.Items;

public sealed class ItemSetEditModel
{
    public short Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ReferenceName { get; set; } = string.Empty;

    public bool UsesRuntimeName { get; set; }

    public List<ItemListItem> Items { get; set; } = new();

    public List<GameEffectTierEditModel> BonusTiers { get; set; } = new();

    public string RawBinaryEffectsBase64 { get; set; } = string.Empty;

    public string ItemsCsv =>
        string.Join(",", Items.Select(x => x.Id));
}
