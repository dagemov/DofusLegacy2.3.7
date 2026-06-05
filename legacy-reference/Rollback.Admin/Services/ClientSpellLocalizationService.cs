using Rollback.Admin.Models.Spells;

namespace Rollback.Admin.Services;

internal sealed class ClientSpellLocalizationService
{
    private readonly ClientI18nTextService _i18nTextService = new();
    private readonly ClientSpellMetadataService _metadataService = new();
    private readonly ClientSpellTypeCatalogService _typeCatalogService = new();

    public AdminClientSpellText Get(short spellId)
    {
        var text = new AdminClientSpellText { SpellId = spellId };
        var metadata = _metadataService.Get(spellId);
        if (metadata.SpellId <= 0 || metadata.SpellId != spellId)
            return text;

        text.TypeId = metadata.TypeId;
        text.TypeLabel = metadata.TypeId.HasValue
            ? _typeCatalogService.GetDisplayName(metadata.TypeId.Value)
            : string.Empty;
        text.NameId = metadata.NameId;
        text.DescriptionId = metadata.DescriptionId;
        text.IconId = metadata.IconId;
        text.Name = metadata.NameId.HasValue && _i18nTextService.TryGetText(metadata.NameId.Value, out var name)
            ? NormalizeClientName(name)
            : string.Empty;
        text.Description = metadata.DescriptionId.HasValue && _i18nTextService.TryGetText(metadata.DescriptionId.Value, out var description)
            ? NormalizeClientDescription(description)
            : string.Empty;
        return text;
    }

    public IReadOnlyList<SpellTypeOption> GetTypeOptions() =>
        _typeCatalogService.GetOptions();

    public string GetTypeLabel(sbyte typeId) =>
        _typeCatalogService.GetDisplayName(typeId);

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
