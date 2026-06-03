using Microsoft.Extensions.Hosting;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Contracts.Items;
using RollblackLegacy.Admin.Infrastructure.Items;

namespace RollblackLegacy.Admin.Infrastructure.Services.Items;

public sealed class FileSystemItemAppearancePreviewStateResolver : IItemAppearancePreviewStateResolver
{
    private readonly string _byAppearanceDirectory;

    public FileSystemItemAppearancePreviewStateResolver(IHostEnvironment hostEnvironment)
    {
        _byAppearanceDirectory = AdminRepositoryPathResolver.ResolveAdminAngularByAppearanceRoot(hostEnvironment.ContentRootPath);
    }

    public ItemAppearancePreviewStateDto Resolve(
        int appearanceId,
        bool? appearanceKnown,
        string? appearancesD2oPath = null)
    {
        var normalizedAppearanceId = appearanceId;
        var byAppearancePath = normalizedAppearanceId > 0
            ? $"/assets/item-previews/by-appearance/{normalizedAppearanceId}.png"
            : string.Empty;

        if (normalizedAppearanceId <= 0)
        {
            return new ItemAppearancePreviewStateDto(
                normalizedAppearanceId,
                null,
                "NOT_APPLICABLE",
                byAppearancePath,
                "NONE",
                null,
                appearancesD2oPath);
        }

        var physicalPath = Path.Combine(_byAppearanceDirectory, $"{normalizedAppearanceId}.png");
        var curatedExists = File.Exists(physicalPath);

        if (curatedExists)
        {
            return new ItemAppearancePreviewStateDto(
                normalizedAppearanceId,
                appearanceKnown,
                "CURATED_BY_APPEARANCE",
                byAppearancePath,
                "CURATED_BY_APPEARANCE",
                byAppearancePath,
                appearancesD2oPath);
        }

        var state = appearanceKnown == false ? "UNKNOWN" : "MISSING";
        return new ItemAppearancePreviewStateDto(
            normalizedAppearanceId,
            appearanceKnown,
            state,
            byAppearancePath,
            "PLACEHOLDER",
            null,
            appearancesD2oPath);
    }
}
