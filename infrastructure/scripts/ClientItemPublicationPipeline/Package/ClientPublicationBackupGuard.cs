using System.Security.Cryptography;
using System.Text.Json;

namespace ClientItemPublicationPipeline.Package;

internal static class ClientPublicationBackupGuard
{
    private static readonly string[] RequiredRelativeFiles =
    [
        PublicationPackagePaths.ItemsRelative,
        PublicationPackagePaths.I18nEsRelative,
        PublicationPackagePaths.I18nEnRelative
    ];

    public static bool TryResolveLatestBackup(string repoRoot, out string backupDirectory, out List<string> errors)
    {
        errors = [];
        backupDirectory = string.Empty;
        var clientBackupsRoot = Path.Combine(repoRoot, "backups", "client");
        if (!Directory.Exists(clientBackupsRoot))
        {
            errors.Add("No existe backups/client/. Ejecutar backup-client con CONFIRM_BACKUP=1.");
            return false;
        }

        var candidates = Directory.GetDirectories(clientBackupsRoot)
            .OrderByDescending(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var candidate in candidates)
        {
            var manifestPath = Path.Combine(candidate, "manifest.json");
            var checksumsPath = Path.Combine(candidate, "checksums.sha256");
            if (!File.Exists(manifestPath) || !File.Exists(checksumsPath))
            {
                continue;
            }

            var missing = RequiredRelativeFiles
                .Where(relative => !File.Exists(Path.Combine(candidate, relative.Replace('/', Path.DirectorySeparatorChar))))
                .ToList();

            if (missing.Count > 0)
            {
                continue;
            }

            backupDirectory = candidate;
            return true;
        }

        errors.Add("No hay backup client valido (manifest.json + checksums + Items/i18n).");
        return false;
    }

    public static bool BackupMatchesClient(string clientRoot, string backupDirectory, List<string> errors, List<string> checks)
    {
        foreach (var relative in RequiredRelativeFiles)
        {
            var clientPath = Path.Combine(clientRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            var backupPath = Path.Combine(backupDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(clientPath))
            {
                errors.Add($"Cliente sin archivo: {relative}");
                continue;
            }

            if (!File.Exists(backupPath))
            {
                errors.Add($"Backup sin archivo: {relative}");
                continue;
            }

            var clientHash = HashFile(clientPath);
            var backupHash = HashFile(backupPath);
            if (!string.Equals(clientHash, backupHash, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    $"Backup desactualizado para {relative}. Vuelve a ejecutar backup-client antes de publicar.");
            }
            else
            {
                checks.Add($"Backup coincide con cliente actual: {relative}");
            }
        }

        return errors.Count == 0;
    }

    private static string HashFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
}
