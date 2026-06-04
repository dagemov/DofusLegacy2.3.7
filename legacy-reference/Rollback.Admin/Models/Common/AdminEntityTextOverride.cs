namespace Rollback.Admin.Models.Common;

public sealed class AdminEntityTextOverride
{
    public AdminEntityType EntityType { get; set; }

    public int EntityId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
