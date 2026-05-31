namespace OneLauncher.Api.Services;

public interface IPackageFileService
{
    PackageFileResult? GetPackage(string packageName);
}

public sealed record PackageFileResult(
    string FileName,
    string ContentType,
    byte[] Content);
