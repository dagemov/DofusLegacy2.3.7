namespace Rollback.Admin.Models.Common;

public sealed class AdminSaveResult
{
    public static AdminSaveResult Empty { get; } = new();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Infos { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public bool HasWarnings =>
        Warnings.Count > 0;

    public bool HasInfos =>
        Infos.Count > 0;

    public bool HasErrors =>
        Errors.Count > 0;
}
