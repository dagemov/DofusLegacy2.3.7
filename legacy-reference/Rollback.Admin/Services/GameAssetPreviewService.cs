using Rollback.Admin.Models.Assets;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.Items;

namespace Rollback.Admin.Services;

public sealed class GameAssetPreviewService
{
    private readonly ClientDataPathResolver _pathResolver;
    private readonly string? _itemBitmapDirectory;
    private readonly Dictionary<string, string> _itemFilesByBaseName;
    private readonly ClientItemMetadataService _clientItemMetadataService;

    public string? ItemBitmapDirectory =>
        _itemBitmapDirectory;

    public bool HasItemAssets =>
        !string.IsNullOrWhiteSpace(_itemBitmapDirectory) && _itemFilesByBaseName.Count > 0;

    public GameAssetPreviewService(
        ClientDataPathResolver pathResolver,
        ClientItemMetadataService clientItemMetadataService)
    {
        _pathResolver = pathResolver;
        _clientItemMetadataService = clientItemMetadataService;
        _itemBitmapDirectory = _pathResolver.ItemBitmapDirectory;
        if (_itemBitmapDirectory is null)
        {
            _itemFilesByBaseName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        _itemFilesByBaseName = Directory
            .EnumerateFiles(_itemBitmapDirectory, "*.png", SearchOption.AllDirectories)
            .GroupBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public GameAssetPreviewModel Resolve(GameAssetPreviewRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.ManualImageUrl))
        {
            return new GameAssetPreviewModel
            {
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                AppearanceId = request.AppearanceId,
                PlaceholderLabel = string.IsNullOrWhiteSpace(request.PlaceholderLabel) ? request.CategoryLabel : request.PlaceholderLabel,
                ImageUrl = request.ManualImageUrl,
                IsManualOverride = true,
                Status = "Preview manual",
                Hint = "Este preview usa un PNG asociado manualmente desde el panel admin.",
            };
        }

        return request.EntityType switch
        {
            AdminEntityType.Item => ResolveItemPreview(request),
            AdminEntityType.Monster or AdminEntityType.Npc => new GameAssetPreviewModel
            {
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                AppearanceId = request.AppearanceId,
                PlaceholderLabel = string.IsNullOrWhiteSpace(request.PlaceholderLabel) ? request.CategoryLabel : request.PlaceholderLabel,
                Status = "Preview pendiente para esta entidad",
                Hint = "La base reusable ya existe. Monsters y NPCs pueden usar este mismo componente mientras definimos su resolver definitivo.",
            },
            _ => new GameAssetPreviewModel
            {
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                AppearanceId = request.AppearanceId,
                PlaceholderLabel = string.IsNullOrWhiteSpace(request.PlaceholderLabel) ? "?" : request.PlaceholderLabel,
                Status = "Sin preview",
                Hint = "No hay un resolver de assets configurado para esta entidad.",
            },
        };
    }

    public string ResolveItemPreviewUrl(int itemId, int appearanceId = 0)
    {
        var result = Resolve(new GameAssetPreviewRequest
        {
            EntityType = AdminEntityType.Item,
            EntityId = itemId,
            AppearanceId = appearanceId,
        });

        return result.ImageUrl;
    }

    public int? ParseItemBitmapAssetId(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        var normalized = rawValue.Trim();
        if (normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith(".swf", StringComparison.OrdinalIgnoreCase))
        {
            normalized = Path.GetFileNameWithoutExtension(normalized);
        }

        return int.TryParse(normalized, out var assetId) && assetId > 0
            ? assetId
            : null;
    }

    public GameAssetPreviewModel ResolveItemBitmapPreview(int assetId, string placeholderLabel = "PNG")
    {
        if (!HasItemAssets)
        {
            return new GameAssetPreviewModel
            {
                EntityType = AdminEntityType.Item,
                EntityId = assetId,
                PlaceholderLabel = placeholderLabel,
                Status = "No se detecto el pack local de iconos",
                Hint = "Hace falta client/app/content/gfx/items/bitmap para validar bitmaps exportados.",
            };
        }

        if (assetId <= 0)
        {
            return new GameAssetPreviewModel
            {
                EntityType = AdminEntityType.Item,
                EntityId = assetId,
                PlaceholderLabel = placeholderLabel,
                Status = "Id de bitmap invalido",
                Hint = "Escribe un numero como 16134, 16134.png o 16134.swf.",
            };
        }

        var bitmapPath = ResolveItemFile(assetId);
        if (!string.IsNullOrWhiteSpace(bitmapPath))
        {
            return new GameAssetPreviewModel
            {
                EntityType = AdminEntityType.Item,
                EntityId = assetId,
                PlaceholderLabel = placeholderLabel,
                ImageUrl = BuildItemAssetUrl(bitmapPath),
                ResolvedAssetId = assetId,
                Status = "Bitmap encontrado",
                Hint = $"Se detecto {assetId}.png en items/bitmap. Esto valida el asset exportado, aunque no cambia por si solo el datacenter del cliente.",
            };
        }

        return new GameAssetPreviewModel
        {
            EntityType = AdminEntityType.Item,
            EntityId = assetId,
            PlaceholderLabel = placeholderLabel,
            ResolvedAssetId = assetId,
            Status = "Bitmap no encontrado",
            Hint = $"No existe {assetId}.png en items/bitmap. Revisa el export SWF->PNG o el id visual real.",
        };
    }

    private GameAssetPreviewModel ResolveItemPreview(GameAssetPreviewRequest request)
    {
        var clientMetadata = request.EntityId > 0 && request.EntityId <= short.MaxValue
            ? _clientItemMetadataService.Get((short)request.EntityId)
            : new AdminClientItemMetadata { ItemId = (short)Math.Clamp(request.EntityId, short.MinValue, short.MaxValue) };

        var placeholder = string.IsNullOrWhiteSpace(request.PlaceholderLabel)
            ? request.CategoryLabel
            : request.PlaceholderLabel;

        if (!HasItemAssets)
        {
            return new GameAssetPreviewModel
            {
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                AppearanceId = request.AppearanceId,
                PlaceholderLabel = placeholder,
                Status = "No se detecto el pack local de iconos",
                Hint = "Hace falta client/app/content/gfx/items/bitmap para mostrar previews reales. No hace falta internet si ese pack ya existe localmente.",
            };
        }

        if (clientMetadata.IconId is > 0)
        {
            var iconPath = ResolveItemFile(clientMetadata.IconId.Value);
            if (!string.IsNullOrWhiteSpace(iconPath))
            {
                return new GameAssetPreviewModel
                {
                    EntityType = request.EntityType,
                    EntityId = request.EntityId,
                    AppearanceId = request.AppearanceId,
                    PlaceholderLabel = placeholder,
                    ImageUrl = BuildItemAssetUrl(iconPath),
                    ResolvedAssetId = clientMetadata.IconId,
                    Status = "Preview resuelto por IconId cliente",
                    Hint = BuildResolvedHint(request.EntityId, clientMetadata),
                };
            }

            return new GameAssetPreviewModel
            {
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                AppearanceId = request.AppearanceId,
                PlaceholderLabel = placeholder,
                ResolvedAssetId = clientMetadata.IconId,
                Status = "IconId cliente sin PNG local",
                Hint = $"El cliente resuelve item #{request.EntityId} con iconId {clientMetadata.IconId}, pero no existe {clientMetadata.IconId}.png en items/bitmap.",
            };
        }

        return new GameAssetPreviewModel
        {
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            AppearanceId = request.AppearanceId,
            PlaceholderLabel = placeholder,
            Status = "Sin IconId cliente",
            Hint = BuildMissingMetadataHint(request.AppearanceId, clientMetadata.AppearanceId),
        };
    }

    private string ResolveItemFile(int id)
    {
        if (id <= 0)
            return string.Empty;

        var key = id.ToString();
        if (_itemFilesByBaseName.TryGetValue(key, out var path) && File.Exists(path))
            return path;

        if (string.IsNullOrWhiteSpace(_itemBitmapDirectory))
            return string.Empty;

        var directPath = Path.Combine(_itemBitmapDirectory, $"{id}.png");
        if (!File.Exists(directPath))
            return string.Empty;

        _itemFilesByBaseName[key] = directPath;
        return directPath;
    }

    private string BuildItemAssetUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(_itemBitmapDirectory))
            return string.Empty;

        var relativePath = Path.GetRelativePath(_itemBitmapDirectory, path)
            .Replace('\\', '/');

        return $"/game-assets/items/{relativePath}";
    }

    private static string BuildResolvedHint(int itemId, AdminClientItemMetadata clientMetadata)
    {
        var appearanceHint = clientMetadata.AppearanceId is > 0
            ? $" AppearanceId cliente: {clientMetadata.AppearanceId}."
            : string.Empty;

        return $"Item #{itemId} usa iconId {clientMetadata.IconId} para el bitmap del inventario.{appearanceHint}";
    }

    private static string BuildMissingMetadataHint(int currentAppearanceId, short? clientAppearanceId)
    {
        var appearanceValue = clientAppearanceId ?? (currentAppearanceId > 0 ? (short?)currentAppearanceId : null);
        return appearanceValue is > 0
            ? $"No se pudo resolver un IconId cliente. AppearanceId {appearanceValue} controla el skin equipado, no el PNG del inventario."
            : "No se pudo resolver un IconId cliente desde Items*.swf. Usa override manual o revisa el datacenter del cliente.";
    }
}
