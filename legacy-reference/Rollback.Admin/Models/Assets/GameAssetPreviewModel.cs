using Rollback.Admin.Models.Common;

namespace Rollback.Admin.Models.Assets;

public sealed class GameAssetPreviewModel
{
    public AdminEntityType EntityType { get; set; }

    public int EntityId { get; set; }

    public int AppearanceId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string PlaceholderLabel { get; set; } = string.Empty;

    public bool HasPreview =>
        !string.IsNullOrWhiteSpace(ImageUrl);

    public bool UsedAppearanceId { get; set; }

    public bool IsManualOverride { get; set; }

    public int? ResolvedAssetId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Hint { get; set; } = string.Empty;
}
