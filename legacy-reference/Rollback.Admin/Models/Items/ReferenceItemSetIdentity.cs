namespace Rollback.Admin.Models.Items;

public sealed class ReferenceItemSetIdentity
{
    public short SetId { get; set; }

    public int NameId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ItemsCsv { get; set; } = string.Empty;

    public IReadOnlyList<short> ItemIds { get; set; } = Array.Empty<short>();
}
