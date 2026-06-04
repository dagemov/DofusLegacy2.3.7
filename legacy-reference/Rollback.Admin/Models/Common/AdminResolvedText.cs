namespace Rollback.Admin.Models.Common;

public sealed class AdminResolvedText
{
    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string SourceLabel { get; set; } = string.Empty;

    public string ClientDisplayName { get; set; } = string.Empty;

    public string ClientDescription { get; set; } = string.Empty;

    public string OverrideDisplayName { get; set; } = string.Empty;

    public string OverrideDescription { get; set; } = string.Empty;

    public bool HasOverride =>
        !string.IsNullOrWhiteSpace(OverrideDisplayName) || !string.IsNullOrWhiteSpace(OverrideDescription);

    public bool HasClientText =>
        !string.IsNullOrWhiteSpace(ClientDisplayName) || !string.IsNullOrWhiteSpace(ClientDescription);
}
