using Rollback.Admin.Models.Common;

namespace Rollback.Admin.Models.Assets;

public sealed class GameAssetPreviewRequest
{
    public AdminEntityType EntityType { get; set; }

    public int EntityId { get; set; }

    public int AppearanceId { get; set; }

    public string ManualImageUrl { get; set; } = string.Empty;

    public string CategoryLabel { get; set; } = string.Empty;

    public string PlaceholderLabel { get; set; } = string.Empty;
}
