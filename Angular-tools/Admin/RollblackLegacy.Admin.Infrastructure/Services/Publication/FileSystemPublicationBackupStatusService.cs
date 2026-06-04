using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RollblackLegacy.Admin.Application.Abstractions.Publication;
using RollblackLegacy.Admin.Contracts.Publication;

namespace RollblackLegacy.Admin.Infrastructure.Services.Publication;

public sealed class FileSystemPublicationBackupStatusService : IPublicationBackupStatusService
{
    private const int DefaultTargetItemId = 12617;

    private readonly string _contentRootPath;

    public FileSystemPublicationBackupStatusService(IHostEnvironment hostEnvironment)
    {
        _contentRootPath = hostEnvironment.ContentRootPath;
    }

    public Task<PublicationBackupStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var repoRoot = ResolveRepoRoot(_contentRootPath)
            ?? throw new InvalidOperationException("No se pudo resolver la raíz del repo para backup-status.");

        var clientBackup = FindLatestBackup(repoRoot, "client");
        var dbBackup = FindLatestBackup(repoRoot, "db");
        var vpsBackup = FindLatestBackup(repoRoot, "vps");
        var lane = ReadPublishLane(repoRoot);
        var validation = ReadPackageValidation(repoRoot, lane?.TargetItemId ?? DefaultTargetItemId);

        var recoveryNotes = BuildRecoveryReadiness(clientBackup, dbBackup, vpsBackup);
        var blocking = lane?.BlockingReasons?.ToList() ?? [];
        var nextSteps = lane?.NextManualSteps?.ToList() ?? [];

        if (lane is null)
        {
            blocking.Add("lane-state.json no encontrado; ejecutar update-publish-lane.ps1.");
            nextSteps.Add("Infrastructure/scripts/PublicationBackup/update-publish-lane.ps1");
        }

        var status = new PublicationBackupStatusDto(
            clientBackup?.CreatedAtUtc,
            clientBackup?.RelativePath,
            dbBackup?.CreatedAtUtc,
            dbBackup?.RelativePath,
            vpsBackup?.CreatedAtUtc,
            vpsBackup?.RelativePath,
            validation?.CheckedAtUtc ?? lane?.LastValidationUtc,
            validation?.ValidationStatus ?? lane?.LastValidationStatus,
            lane?.PublishLaneStatus ?? PublicationPublishLaneStatuses.NeedsValidation,
            lane?.TargetItemId ?? DefaultTargetItemId,
            lane?.StagingPackagePath,
            lane?.ProductionPublishBlocked ?? true,
            blocking,
            recoveryNotes,
            nextSteps,
            DateTimeOffset.UtcNow);

        return Task.FromResult(status);
    }

    private static IReadOnlyList<string> BuildRecoveryReadiness(
        BackupSnapshot? client,
        BackupSnapshot? db,
        BackupSnapshot? vps)
    {
        var notes = new List<string>();
        notes.Add(client is not null
            ? $"Client backup listo ({client.RelativePath}). Restore dry-run: restore-client.ps1 -BackupId <folder>."
            : "Sin backup cliente; ejecutar backup-client.ps1 con CONFIRM_BACKUP=1.");
        notes.Add(db is not null
            ? $"DB backup listo ({db.RelativePath}). Restore dry-run: restore-db.ps1 -BackupId <folder>."
            : "Sin backup DB local; ejecutar backup-db.ps1 con CONFIRM_BACKUP=1.");
        notes.Add(vps is not null
            ? $"Inventario VPS capturado ({vps.RelativePath})."
            : "Sin inventario VPS; ejecutar backup-vps-state.ps1 con CONFIRM_BACKUP=1 (requiere SSH).");
        notes.Add("Restore execute nunca apunta a Client2.3.7 real; cliente → client-restore-sandbox.");
        return notes;
    }

    private static PackageValidationSnapshot? ReadPackageValidation(string repoRoot, int itemId)
    {
        var path = Path.Combine(
            repoRoot,
            "Infrastructure",
            "staging-client",
            "publication-package-phase3c",
            itemId.ToString(),
            "validation-report.json");

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            var document = JsonSerializer.Deserialize<PackageValidationSnapshot>(stream, JsonOptions);
            return document;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static PublishLaneSnapshot? ReadPublishLane(string repoRoot)
    {
        var path = Path.Combine(repoRoot, "Infrastructure", "staging-client", "publish-lane", "lane-state.json");
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<PublishLaneSnapshot>(stream, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static BackupSnapshot? FindLatestBackup(string repoRoot, string category)
    {
        var root = Path.Combine(repoRoot, "backups", category);
        if (!Directory.Exists(root))
        {
            return null;
        }

        var latest = Directory.GetDirectories(root)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderByDescending(name => name, StringComparer.Ordinal)
            .FirstOrDefault();

        if (latest is null)
        {
            return null;
        }

        var manifestPath = Path.Combine(root, latest, "manifest.json");
        DateTimeOffset? createdAt = null;
        if (File.Exists(manifestPath))
        {
            try
            {
                using var stream = File.OpenRead(manifestPath);
                var manifest = JsonSerializer.Deserialize<BackupManifestSnapshot>(stream, JsonOptions);
                if (!string.IsNullOrWhiteSpace(manifest?.CreatedAtUtc))
                {
                    createdAt = DateTimeOffset.Parse(manifest.CreatedAtUtc);
                }
            }
            catch (JsonException)
            {
                createdAt = null;
            }
        }

        return new BackupSnapshot(
            createdAt,
            $"backups/{category}/{latest}");
    }

    private static string? ResolveRepoRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Angular-tools", "Admin"))
                && Directory.Exists(Path.Combine(directory.FullName, "docs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record BackupSnapshot(DateTimeOffset? CreatedAtUtc, string RelativePath);

    private sealed class BackupManifestSnapshot
    {
        public string? CreatedAtUtc { get; init; }
    }

    private sealed class PublishLaneSnapshot
    {
        public string? PublishLaneStatus { get; init; }
        public int TargetItemId { get; init; }
        public string? StagingPackagePath { get; init; }
        public DateTimeOffset? LastValidationUtc { get; init; }
        public string? LastValidationStatus { get; init; }
        public bool ProductionPublishBlocked { get; init; } = true;
        public List<string>? BlockingReasons { get; init; }
        public List<string>? NextManualSteps { get; init; }
    }

    private sealed class PackageValidationSnapshot
    {
        public string? ValidationStatus { get; init; }
        public string? CheckedAt { get; init; }

        public DateTimeOffset? CheckedAtUtc =>
            string.IsNullOrWhiteSpace(CheckedAt) ? null : DateTimeOffset.Parse(CheckedAt);
    }
}
