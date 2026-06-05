namespace Rollback.Admin.Models.Spells;

public sealed class SpellAuditSnapshot
{
    public SpellAuditStatus Status { get; set; } = SpellAuditStatus.Ambiguous;

    public string StatusLabel { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string DomainLabel { get; set; } = string.Empty;

    public string IdentitySourceLabel { get; set; } = string.Empty;

    public bool IsClassicDomain { get; set; }

    public bool IsSupportOrCommon { get; set; }

    public bool IsRuntimeAvailable { get; set; }

    public bool HasReferenceIdentity { get; set; }

    public bool IsExcludedModernClass { get; set; }

    public bool IsLegacy { get; set; }

    public bool IsAmbiguous { get; set; }

    public int RuntimeLevelCount { get; set; }

    public int ReferenceLevelCount { get; set; }

    public IReadOnlyList<string> HeaderDifferences { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> LevelDifferences { get; set; } = Array.Empty<string>();
}
