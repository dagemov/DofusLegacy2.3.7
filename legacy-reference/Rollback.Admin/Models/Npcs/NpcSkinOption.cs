namespace Rollback.Admin.Models.Npcs;

public sealed class NpcSkinOption
{
    public short SampleId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string SourceLabel { get; set; } = string.Empty;

    public bool Gender { get; set; }

    public string EntityLookString { get; set; } = string.Empty;
}
