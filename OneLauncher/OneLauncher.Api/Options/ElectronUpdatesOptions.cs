namespace OneLauncher.Api.Options;

public sealed class ElectronUpdatesOptions
{
    public const string SectionName = "ElectronUpdates";

    public string RootPath { get; set; } = string.Empty;

    public string PublicBaseUrl { get; set; } = "https://rollblack-legacy.onesv.online/api/launcher/electron-updates";

    public string FileName { get; set; } = "rollblack-legacy.exe";

    public string Version { get; set; } = "1.0.0";
}
