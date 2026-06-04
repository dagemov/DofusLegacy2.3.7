using System.Text;
using System.Text.Json;

namespace ClientItemPublicationPipeline.Package;

internal sealed record ItemSkinCatalogExportResult(
    string Category,
    int Limit,
    bool DryRun,
    bool ApprovedCopy,
    int PlannedCopies,
    int Copied,
    IReadOnlyList<string> Messages);

internal sealed class ItemSkinCatalogExporter
{
    public ItemSkinCatalogExportResult ExportCurated(
        ClientSkinCatalogPaths paths,
        ItemSkinCatalogBuildResult catalog,
        string category,
        int limit,
        bool dryRun,
        bool approveCuratedCopy)
    {
        var messages = new List<string>();
        if (!ItemSkinCategoryMap.IsSupportedExportCategory(category))
        {
            throw new ArgumentException(
                $"Categoria no soportada: {category}. Usa: {string.Join(", ", ItemSkinCategoryMap.ExportCategories)}");
        }

        var candidates = catalog.Entries
            .Where(e => string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase))
            .Where(e => e.IconPreviewAvailable)
            .OrderBy(e => e.ItemId)
            .Take(limit)
            .ToList();

        var targetDir = Path.Combine(paths.AdminByCategoryRoot, category);
        var copied = 0;

        foreach (var entry in candidates)
        {
            var targetFile = Path.Combine(targetDir, $"{entry.IconId}.png");
            if (dryRun || !approveCuratedCopy)
            {
                messages.Add($"[plan] {entry.ItemId} → {targetFile} ({entry.IconSource})");
                continue;
            }

            if (category != "dofus")
            {
                messages.Add($"[blocked] categoria {category} solo dry-run en Phase 6B");
                continue;
            }

            if (File.Exists(targetFile))
            {
                messages.Add($"[skip-existing] {targetFile}");
                copied++;
                continue;
            }

            var adminIcon = Path.Combine(paths.AdminByIconDirectory, $"{entry.IconId}.png");
            if (File.Exists(adminIcon))
            {
                Directory.CreateDirectory(targetDir);
                File.Copy(adminIcon, targetFile, overwrite: false);
                copied++;
                messages.Add($"[copied-from-admin] {entry.IconId}");
                continue;
            }

            if (CatalogD2pIconResolver.TryExtractPng(paths.BitmapD2pPaths, entry.IconId, targetFile))
            {
                copied++;
                messages.Add($"[copied-from-d2p] {entry.IconId}");
            }
            else
            {
                messages.Add($"[missing-png] IconId {entry.IconId}");
            }
        }

        var reportDir = Path.Combine(catalog.OutputDirectory, "export");
        Directory.CreateDirectory(reportDir);
        var reportPath = Path.Combine(reportDir, $"export-{category}-report.json");
        File.WriteAllText(
            reportPath,
            JsonSerializer.Serialize(
                new
                {
                    category,
                    limit,
                    dryRun,
                    approveCuratedCopy,
                    planned = candidates.Count,
                    copied,
                    messages
                },
                new JsonSerializerOptions { WriteIndented = true }),
            Encoding.UTF8);

        return new ItemSkinCatalogExportResult(
            category,
            limit,
            dryRun,
            approveCuratedCopy,
            candidates.Count,
            copied,
            messages);
    }
}
