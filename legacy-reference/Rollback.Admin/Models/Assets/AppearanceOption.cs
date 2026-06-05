namespace Rollback.Admin.Models.Assets;

public sealed class AppearanceOption
{
    public short AppearanceId { get; set; }

    public string Label { get; set; } = string.Empty;

    public string PreviewUrl { get; set; } = string.Empty;

    public bool HasPreview =>
        !string.IsNullOrWhiteSpace(PreviewUrl);
}
