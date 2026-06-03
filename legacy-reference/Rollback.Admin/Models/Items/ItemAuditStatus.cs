namespace Rollback.Admin.Models.Items;

public enum ItemAuditStatus
{
    Aligned = 0,
    LegacyRuntimeItem = 1,
    MissingClientMetadata = 2,
    MissingReferenceMetadata = 3,
    MissingName = 4,
    MissingIcon = 5,
    IncompleteTemplate = 6,
    RuntimeOnly = 7,
    MissingRuntime = 8,
    Ambiguous = 9,
}
