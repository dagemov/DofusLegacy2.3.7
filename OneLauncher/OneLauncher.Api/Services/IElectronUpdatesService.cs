namespace OneLauncher.Api.Services;

public sealed record ElectronUpdateFileResult(
    string FileName,
    string ContentType,
    byte[] Content);

public interface IElectronUpdatesService
{
    bool TryBuildLatestYaml(out string yaml);

    ElectronUpdateFileResult? GetUpdateFile(string fileName);
}
