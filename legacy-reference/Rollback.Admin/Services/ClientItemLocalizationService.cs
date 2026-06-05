using Rollback.Admin.Models.Items;

namespace Rollback.Admin.Services;

public sealed class ClientItemLocalizationService
{
    private readonly ClientI18nTextService _i18nTextService = new();
    private readonly ClientItemMetadataService _metadataService = new();

    public AdminClientItemText Get(short itemId)
    {
        var text = new AdminClientItemText { ItemId = itemId };
        var metadata = _metadataService.Get(itemId);
        if (metadata.ItemId <= 0 || metadata.ItemId != itemId)
            return text;

        text.ClientTypeId = metadata.TypeId;
        text.NameId = metadata.NameId;
        text.DescriptionId = metadata.DescriptionId;
        text.IconId = metadata.IconId;
        text.ClientAppearanceId = metadata.AppearanceId;
        text.Name = metadata.NameId.HasValue && _i18nTextService.TryGetText(metadata.NameId.Value, out var name)
            ? NormalizeClientName(name)
            : string.Empty;
        text.Description = metadata.DescriptionId.HasValue && _i18nTextService.TryGetText(metadata.DescriptionId.Value, out var description)
            ? NormalizeClientDescription(description)
            : string.Empty;
        return text;
    }

    private static string NormalizeClientName(string text)
    {
        var normalized = Normalize(text);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        if (IsPlaceholder(normalized) || LooksLikeDescription(normalized))
            return string.Empty;

        return normalized;
    }

    private static string NormalizeClientDescription(string text)
    {
        var normalized = Normalize(text);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        if (IsPlaceholder(normalized) || LooksLikeTitle(normalized))
            return string.Empty;

        return normalized;
    }

    private static string Normalize(string text) =>
        (text ?? string.Empty).Trim();

    private static bool IsPlaceholder(string text) =>
        string.Equals(text, "#1", StringComparison.OrdinalIgnoreCase) ||
        text.StartsWith('#');

    private static bool LooksLikeTitle(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (text.Contains('\n') || text.Contains('\r'))
            return false;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length <= 8 && text.Length <= 80 && !text.Contains('.') && !text.Contains(';') && !text.Contains(':');
    }

    private static bool LooksLikeDescription(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (text.Contains('\n') || text.Contains('\r'))
            return true;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length >= 10 || text.Length >= 90 || text.Contains('.') || text.Contains(';') || text.Contains('!') || text.Contains('?');
    }
}
