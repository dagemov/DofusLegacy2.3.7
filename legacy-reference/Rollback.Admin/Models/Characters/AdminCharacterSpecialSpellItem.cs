namespace Rollback.Admin.Models.Characters;

public sealed class AdminCharacterSpecialSpellItem
{
    public short SpellId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ModeLabel { get; set; } = string.Empty;

    public string ClientCompatibilityLabel { get; set; } = string.Empty;
}
