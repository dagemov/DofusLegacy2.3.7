namespace OneLauncher.Api.Contracts;

public sealed record LauncherManifestDto(
    string Version,
    IReadOnlyList<LauncherPackageDto> Packages,
    LauncherStatusDto Launcher);

public sealed record LauncherPackageDto(
    string Name,
    string Url,
    string Checksum,
    long Size);

public sealed record LauncherStatusDto(
    string MinimumVersion,
    string Status);
