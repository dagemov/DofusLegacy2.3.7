namespace Rollback.Admin.Models.Common;

public sealed class I18nCatalogEntry
{
    public int TextId { get; set; }

    public string Text { get; set; } = string.Empty;

    public string SourceFile { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;

    public string EntityType { get; set; } = "Unknown";
}
