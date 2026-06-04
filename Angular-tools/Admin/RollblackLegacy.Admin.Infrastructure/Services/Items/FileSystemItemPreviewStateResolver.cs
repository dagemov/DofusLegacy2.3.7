using Microsoft.Extensions.Hosting;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Contracts.Items;
using RollblackLegacy.Admin.Infrastructure.Items;

namespace RollblackLegacy.Admin.Infrastructure.Services.Items;

public sealed class FileSystemItemPreviewStateResolver : IItemPreviewStateResolver
{
    private readonly string _itemPreviewRoot;
    private readonly string _manualItemsRoot;

    public FileSystemItemPreviewStateResolver(IHostEnvironment hostEnvironment)
    {
        _itemPreviewRoot = AdminRepositoryPathResolver.ResolveAdminAngularItemPreviewsRoot(hostEnvironment.ContentRootPath);
        _manualItemsRoot = AdminRepositoryPathResolver.ResolveAdminAngularManualItemsRoot(hostEnvironment.ContentRootPath);
    }

    public ItemPreviewStateDto Resolve(int? itemId, int? iconId)
    {
        var normalizedItemId = itemId.GetValueOrDefault();
        var normalizedIconId = iconId.GetValueOrDefault();

        var byItemPath = normalizedItemId > 0 ? $"/assets/item-previews/by-item/{normalizedItemId}.png" : string.Empty;
        var byIconPath = normalizedIconId > 0 ? $"/assets/item-previews/by-icon/{normalizedIconId}.png" : string.Empty;
        var manualPath = normalizedItemId > 0 ? $"/manual-assets/items/{normalizedItemId}.png" : string.Empty;

        var byItemPhysicalPath = normalizedItemId > 0
            ? Path.Combine(_itemPreviewRoot, "by-item", $"{normalizedItemId}.png")
            : string.Empty;
        var byIconPhysicalPath = normalizedIconId > 0
            ? Path.Combine(_itemPreviewRoot, "by-icon", $"{normalizedIconId}.png")
            : string.Empty;
        var manualPhysicalPath = normalizedItemId > 0
            ? Path.Combine(_manualItemsRoot, $"{normalizedItemId}.png")
            : string.Empty;

        var hasAnyPreviewDirectory =
            Directory.Exists(Path.Combine(_itemPreviewRoot, "by-item")) ||
            Directory.Exists(Path.Combine(_itemPreviewRoot, "by-icon")) ||
            Directory.Exists(_manualItemsRoot);

        var manualExists = !string.IsNullOrWhiteSpace(manualPhysicalPath) && File.Exists(manualPhysicalPath);
        var byItemExists = !string.IsNullOrWhiteSpace(byItemPhysicalPath) && File.Exists(byItemPhysicalPath);
        var byIconExists = !string.IsNullOrWhiteSpace(byIconPhysicalPath) && File.Exists(byIconPhysicalPath);

        var state = "UNKNOWN";
        var previewSource = "PLACEHOLDER";
        string? resolvedPath = null;
        var fallbackUsed = "PLACEHOLDER";

        if (manualExists)
        {
            state = "MANUAL";
            previewSource = "MANUAL";
            resolvedPath = manualPath;
            fallbackUsed = "NONE";
        }
        else if (byItemExists)
        {
            state = "FOUND";
            previewSource = "BY_ITEM";
            resolvedPath = byItemPath;
            fallbackUsed = "NONE";
        }
        else if (byIconExists)
        {
            state = "FOUND";
            previewSource = "BY_ICON";
            resolvedPath = byIconPath;
            fallbackUsed = "BY_ICON";
        }
        else if (hasAnyPreviewDirectory)
        {
            state = "MISSING";
        }

        return new ItemPreviewStateDto(
            state,
            byItemPath,
            byIconPath,
            manualPath,
            previewSource,
            resolvedPath,
            fallbackUsed);
    }
}
