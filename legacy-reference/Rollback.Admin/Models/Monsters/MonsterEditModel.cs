namespace Rollback.Admin.Models.Monsters;

public sealed class MonsterEditModel
{
    public short Id { get; set; }

    public byte Race { get; set; }

    public string DisplayNameOverride { get; set; } = string.Empty;

    public string DescriptionOverride { get; set; } = string.Empty;

    public string ResolvedDisplayName { get; set; } = string.Empty;

    public string NameSource { get; set; } = string.Empty;

    public string ClientDisplayName { get; set; } = string.Empty;

    public int? ClientNameId { get; set; }

    public int? ClientGfxId { get; set; }

    public bool HasNameMismatch { get; set; }

    public string EntityLookString { get; set; } = string.Empty;

    public string ManualAssetRelativePath { get; set; } = string.Empty;

    public string ManualImageUrl { get; set; } = string.Empty;

    public List<MonsterGradeAdminModel> Grades { get; set; } = new();
}
