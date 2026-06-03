namespace Rollback.Admin.Models.Spells;

public enum SpellAuditStatus
{
    Aligned = 0,
    Legacy = 1,
    MetadataMissing = 2,
    RuntimeDrift = 3,
    MissingRuntime = 4,
    ExcludedModernClass = 5,
    Ambiguous = 6,
}
