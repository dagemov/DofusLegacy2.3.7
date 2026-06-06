using System.Net;
using System.Text;

namespace ClientItemPublicationPipeline.Package;

internal static class ItemSkinCatalogGalleryGenerator
{
    public static string Generate(
        string galleryDirectory,
        IReadOnlyList<ItemSkinCatalogEntryDto> entries,
        string adminByIconDirectory,
        int maxPerCategory = 40)
    {
        Directory.CreateDirectory(galleryDirectory);
        var htmlPath = Path.Combine(galleryDirectory, "index.html");

        var grouped = entries
            .GroupBy(static e => e.Category, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static g => g.Key, StringComparer.OrdinalIgnoreCase);

        var builder = new StringBuilder();
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"es\"><head><meta charset=\"utf-8\"/>");
        builder.AppendLine("<title>Item skin catalog review</title>");
        builder.AppendLine("<style>");
        builder.AppendLine("body{font-family:Segoe UI,sans-serif;margin:1.5rem;background:#0f1419;color:#e7ecf3;}");
        builder.AppendLine("h1,h2{color:#9fd4ff;} .grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(180px,1fr));gap:12px;}");
        builder.AppendLine(".card{background:#1a2330;border:1px solid #2a3b52;border-radius:8px;padding:10px;}");
        builder.AppendLine(".card img{width:64px;height:64px;image-rendering:pixelated;background:#0b1016;}");
        builder.AppendLine(".meta{font-size:12px;color:#a8b8cc;}");
        builder.AppendLine("</style></head><body>");
        builder.AppendLine("<h1>Item skin catalog — revision</h1>");
        builder.AppendLine("<p>Artefacto temporal. Abrir con <code>file:///</code> o servidor estatico local.</p>");

        foreach (var group in grouped)
        {
            builder.AppendLine($"<h2>{WebUtility.HtmlEncode(group.Key)} ({group.Count()})</h2>");
            builder.AppendLine("<div class=\"grid\">");
            foreach (var entry in group.Take(maxPerCategory))
            {
                var preview = Path.Combine(adminByIconDirectory, $"{entry.IconId}.png");
                var imgSrc = File.Exists(preview)
                    ? new Uri(preview).AbsoluteUri
                    : "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='64' height='64'%3E%3Crect fill='%23334155' width='64' height='64'/%3E%3C/svg%3E";

                builder.AppendLine("<div class=\"card\">");
                builder.AppendLine($"<img src=\"{WebUtility.HtmlEncode(imgSrc)}\" alt=\"icon {entry.IconId}\"/>");
                builder.AppendLine($"<div><strong>{WebUtility.HtmlEncode(entry.NameEs)}</strong></div>");
                builder.AppendLine(
                    $"<div class=\"meta\">ItemId {entry.ItemId} · IconId {entry.IconId} · {WebUtility.HtmlEncode(entry.IconSource)}</div>");
                builder.AppendLine("</div>");
            }

            builder.AppendLine("</div>");
        }

        builder.AppendLine("</body></html>");
        File.WriteAllText(htmlPath, builder.ToString(), Encoding.UTF8);
        return htmlPath;
    }
}
