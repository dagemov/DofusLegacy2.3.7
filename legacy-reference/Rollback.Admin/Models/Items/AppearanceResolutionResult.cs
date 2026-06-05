namespace Rollback.Admin.Models.Items;

public sealed class AppearanceResolutionResult
{
    public short AppearanceId { get; init; }

    public short CurrentAppearanceId { get; init; }

    public short? SourceItemId { get; init; }

    public int? SourceIconId { get; init; }

    public string Strategy { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public bool IsFallback { get; init; }

    public bool IsMismatch { get; init; }

    public bool NeedsCorrection { get; init; }
}
