namespace Rollback.Admin.Models.Monsters;

public sealed class MonsterCatalogPresentation
{
    public short MonsterId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string NameSource { get; set; } = "fallback";

    public string ClientDisplayName { get; set; } = string.Empty;

    public int? ClientNameId { get; set; }

    public int? ClientGfxId { get; set; }

    public bool HasClientReference { get; set; }

    public bool HasNameMismatch { get; set; }

    public string BuildLabel(short minLevel, short maxLevel, byte? race = null)
    {
        var raceLabel = race.HasValue ? $" - familia {race.Value}" : string.Empty;
        return $"{DisplayName} #{MonsterId} - lvl {minLevel}-{maxLevel}{raceLabel}";
    }
}
