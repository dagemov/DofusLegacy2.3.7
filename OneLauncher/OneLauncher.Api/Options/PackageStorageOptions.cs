namespace OneLauncher.Api.Options;

public sealed class PackageStorageOptions
{
    public const string SectionName = "PackageStorage";

    public string RootPath { get; set; } = string.Empty;
}
