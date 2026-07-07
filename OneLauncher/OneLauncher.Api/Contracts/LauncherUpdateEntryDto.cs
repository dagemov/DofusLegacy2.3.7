namespace OneLauncher.Api.Contracts;

public sealed record LauncherUpdateEntryDto(
    string Version,
    string File,
    string Url,
    string Checksum,
    long Size);
