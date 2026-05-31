namespace OneLauncher.Api.Options;

public sealed class LauncherManifestOptions
{
    public const string SectionName = "LauncherManifest";

    public string Version { get; set; } = "2.0.0";
    public List<LauncherPackageOptions> Packages { get; set; } = [];
    public LauncherStatusOptions Launcher { get; set; } = new();
}

public sealed class LauncherPackageOptions
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Checksum { get; set; } = "TEMP";
    public long Size { get; set; }
}

public sealed class LauncherStatusOptions
{
    public string MinimumVersion { get; set; } = "1.0.0";
    public string Status { get; set; } = "online";
}
