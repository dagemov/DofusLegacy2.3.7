namespace OneLauncher.Api.Options;

public sealed class PackageStorageOptions
{
    public const string SectionName = "PackageStorage";

    public string RootPath { get; set; } = string.Empty;

    public string ManifestFileName { get; set; } = "Updates.xml";

    public string PublicBaseUrl { get; set; } = "https://rollblack-legacy.onesv.online";
}
