namespace Rollback.Admin.Models.Common;

public sealed class AdminEntityClientMetadata
{
    public AdminEntityType EntityType { get; set; }

    public int EntityId { get; set; }

    public string LanguageCode { get; set; } = "es";

    public int NameId { get; set; }

    public int DescriptionId { get; set; }

    public int IconId { get; set; }

    public int AppearanceId { get; set; }
}
