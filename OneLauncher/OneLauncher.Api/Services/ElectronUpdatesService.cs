using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using OneLauncher.Api.Options;

namespace OneLauncher.Api.Services;

public sealed class ElectronUpdatesService : IElectronUpdatesService
{
    private const string InstallerContentType = "application/octet-stream";
    private const string YamlContentType = "text/yaml";

    private readonly IOptionsMonitor<ElectronUpdatesOptions> _options;
    private readonly ILogger<ElectronUpdatesService> _logger;

    public ElectronUpdatesService(
        IOptionsMonitor<ElectronUpdatesOptions> options,
        ILogger<ElectronUpdatesService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool TryBuildLatestYaml(out string yaml)
    {
        yaml = string.Empty;
        string? installerPath = ResolveInstallerPath();
        if (installerPath is null)
        {
            return false;
        }

        ElectronUpdatesOptions config = _options.CurrentValue;
        FileInfo fileInfo = new(installerPath);
        byte[] content = File.ReadAllBytes(installerPath);
        string sha512 = Convert.ToBase64String(SHA512.HashData(content));
        string releaseDate = fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");

        var builder = new StringBuilder();
        builder.AppendLine($"version: {config.Version}");
        builder.AppendLine("files:");
        builder.AppendLine($"  - url: {config.FileName}");
        builder.AppendLine($"    sha512: {sha512}");
        builder.AppendLine($"    size: {fileInfo.Length}");
        builder.AppendLine($"path: {config.FileName}");
        builder.AppendLine($"sha512: {sha512}");
        builder.AppendLine($"releaseDate: '{releaseDate}'");

        yaml = builder.ToString();
        _logger.LogInformation(
            "Built electron latest.yml for {FileName} v{Version} ({Bytes} bytes)",
            config.FileName,
            config.Version,
            fileInfo.Length);

        return true;
    }

    public ElectronUpdateFileResult? GetUpdateFile(string fileName)
    {
        ElectronUpdatesOptions config = _options.CurrentValue;
        string safeName = Path.GetFileName(fileName);

        if (string.Equals(safeName, "latest.yml", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryBuildLatestYaml(out string yaml))
            {
                return null;
            }

            return new ElectronUpdateFileResult("latest.yml", YamlContentType, Encoding.UTF8.GetBytes(yaml));
        }

        if (!string.Equals(safeName, config.FileName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string? installerPath = ResolveInstallerPath();
        if (installerPath is null)
        {
            return null;
        }

        byte[] content = File.ReadAllBytes(installerPath);
        return new ElectronUpdateFileResult(config.FileName, InstallerContentType, content);
    }

    private string? ResolveInstallerPath()
    {
        ElectronUpdatesOptions config = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(config.RootPath))
        {
            _logger.LogWarning("ElectronUpdates RootPath is not configured.");
            return null;
        }

        string installerPath = Path.Combine(config.RootPath, config.FileName);
        if (!File.Exists(installerPath))
        {
            _logger.LogWarning("Electron update installer missing: {InstallerPath}", installerPath);
            return null;
        }

        return installerPath;
    }
}
