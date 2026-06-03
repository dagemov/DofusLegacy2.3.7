using Microsoft.AspNetCore.Components.Forms;
using Rollback.Admin.Models.Common;

namespace Rollback.Web.Services;

public sealed class AdminAssetUploadService
{
    private const long MaxUploadSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp",
    };

    private readonly IWebHostEnvironment _environment;

    public AdminAssetUploadService(IWebHostEnvironment environment) =>
        _environment = environment;

    public async Task<string> SaveEntityPreviewAsync(
        AdminEntityType entityType,
        int entityId,
        IBrowserFile file,
        CancellationToken cancellationToken = default)
    {
        if (entityId <= 0)
            throw new InvalidOperationException("Guarda primero un Id valido antes de subir un PNG manual.");

        if (file is null)
            throw new InvalidOperationException("No se recibio ningun archivo.");

        var extension = NormalizeExtension(file);
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Solo se aceptan PNG/JPG/WEBP para el preview manual.");

        var root = GetAdminAssetsRoot();
        var folder = Path.Combine(root, GetEntityFolder(entityType));
        Directory.CreateDirectory(folder);

        var fileName = $"{entityId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}";
        var fullPath = Path.Combine(folder, fileName);

        await using var input = file.OpenReadStream(MaxUploadSizeBytes, cancellationToken);
        await using var output = File.Create(fullPath);
        await input.CopyToAsync(output, cancellationToken);

        return $"{GetEntityFolder(entityType)}/{fileName}".Replace('\\', '/');
    }

    public Task DeleteIfExistsAsync(string? relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.CompletedTask;

        var root = Path.GetFullPath(GetAdminAssetsRoot());
        var combined = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!combined.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        if (File.Exists(combined))
            File.Delete(combined);

        return Task.CompletedTask;
    }

    private string GetAdminAssetsRoot()
    {
        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");

        return Path.Combine(webRoot, "admin-assets");
    }

    private static string GetEntityFolder(AdminEntityType entityType) =>
        entityType switch
        {
            AdminEntityType.Item => "items",
            AdminEntityType.Monster => "monsters",
            AdminEntityType.Npc => "npcs",
            _ => "misc",
        };

    private static string NormalizeExtension(IBrowserFile file)
    {
        var extension = Path.GetExtension(file.Name);
        if (!string.IsNullOrWhiteSpace(extension))
            return extension.ToLowerInvariant();

        return file.ContentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            _ => string.Empty,
        };
    }
}
