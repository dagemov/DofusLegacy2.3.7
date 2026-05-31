using OneLauncher.Api.Contracts;

namespace OneLauncher.Api.Services;

public interface ILauncherManifestService
{
    LauncherManifestDto GetManifest();
}
