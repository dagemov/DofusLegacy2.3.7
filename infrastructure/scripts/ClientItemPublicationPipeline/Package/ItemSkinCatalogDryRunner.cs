using System.Text;
using System.Text.Json;
using D2oItem = Sunshine.Protocol.Tools.D2o.Classes.Item;

namespace ClientItemPublicationPipeline.Package;

internal sealed class ItemSkinCatalogDryRunner
{
    public ItemSkinCatalogDryRunResult Run(
        string repoRoot,
        string outputDirectory,
        string clientRoot,
        bool excludeWeapons = true)
    {
        Directory.CreateDirectory(outputDirectory);

        var itemsPath = Path.Combine(clientRoot, "data", "common", "Items.d2o");
        var esPath = Path.Combine(clientRoot, "data", "i18n", "i18n_es.d2i");
        var enPath = Path.Combine(clientRoot, "data", "i18n", "i18n_en.d2i");
        var adminByIcon = Path.Combine(
            repoRoot,
            "Angular-tools",
            "Admin",
            "RollblackLegacy.Admin.Angular",
            "src",
            "assets",
            "item-previews",
            "by-icon");

        if (!File.Exists(itemsPath))
        {
            throw new FileNotFoundException($"Items.d2o no encontrado: {itemsPath}");
        }

        var es = D2i.D2iFile.Load(esPath);
        var en = D2i.D2iFile.Load(enPath);
        var itemIds = ClientPatchD2oIndex.ReadIds(itemsPath).OrderBy(static id => id).ToList();
        var reader = new Sunshine.Protocol.Tools.D2o.D2OReader(itemsPath);

        var entries = new List<ItemSkinCatalogEntry>();
        var skippedWeapons = 0;
        var skippedOther = 0;

        foreach (var itemId in itemIds)
        {
            D2oItem item;
            try
            {
                item = reader.ReadObject<D2oItem>(itemId, true);
            }
            catch
            {
                skippedOther++;
                continue;
            }

            if (excludeWeapons && WeaponTypeFilter.IsWeapon(item.typeId))
            {
                skippedWeapons++;
                continue;
            }

            var category = ItemSkinCategoryMap.ResolveCategory(item.typeId, item.itemSetId);
            var typeName = Enum.IsDefined(typeof(Sunshine.Protocol.Enums.ItemTypeEnum), item.typeId)
                ? Enum.GetName(typeof(Sunshine.Protocol.Enums.ItemTypeEnum), item.typeId) ?? $"Type{item.typeId}"
                : $"Type{item.typeId}";

            es.TryGetText(item.nameId, out var nameEs);
            en.TryGetText(item.nameId, out var nameEn);
            var iconPreviewPath = Path.Combine(adminByIcon, $"{item.iconId}.png");
            var iconPreviewAvailable = File.Exists(iconPreviewPath);

            entries.Add(new ItemSkinCatalogEntry(
                itemId,
                item.typeId,
                typeName,
                nameEs ?? string.Empty,
                nameEn ?? string.Empty,
                item.iconId,
                item.appearanceId,
                ClientKnown: true,
                iconPreviewAvailable,
                category));
        }

        reader.Close();

        var summary = new ItemSkinCatalogSummary(
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            TotalIndexEntries: itemIds.Count,
            CatalogEntries: entries.Count,
            SkippedWeapons: skippedWeapons,
            SkippedUnreadable: skippedOther,
            WithIconPreview: entries.Count(static e => e.IconPreviewAvailable),
            Categories: entries
                .GroupBy(static e => e.Category, StringComparer.OrdinalIgnoreCase)
                .OrderBy(static g => g.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(static g => g.Key, static g => g.Count(), StringComparer.OrdinalIgnoreCase),
            PlannedAngularFolders: ItemSkinCategoryMap.PlannedAngularFolders);

        var jsonPath = Path.Combine(outputDirectory, "item-skin-catalog.json");
        var mdPath = Path.Combine(outputDirectory, "item-skin-catalog.md");

        var payload = new { summary, entries };
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(payload, JsonWriteOptions), Encoding.UTF8);
        File.WriteAllText(mdPath, WriteMarkdown(summary, entries), Encoding.UTF8);

        return new ItemSkinCatalogDryRunResult(outputDirectory, jsonPath, mdPath, summary);
    }

    private static string WriteMarkdown(ItemSkinCatalogSummary summary, IReadOnlyList<ItemSkinCatalogEntry> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Item skin catalog dry-run");
        builder.AppendLine();
        builder.AppendLine($"- Generado: `{summary.GeneratedAtUtc:O}`");
        builder.AppendLine($"- Entradas indice Items.d2o: **{summary.TotalIndexEntries}**");
        builder.AppendLine($"- Catalogo (sin armas): **{summary.CatalogEntries}**");
        builder.AppendLine($"- Armas excluidas: **{summary.SkippedWeapons}**");
        builder.AppendLine($"- Con preview by-icon en Admin: **{summary.WithIconPreview}**");
        builder.AppendLine();
        builder.AppendLine("## Categorias");
        foreach (var pair in summary.Categories.OrderBy(static p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"- `{pair.Key}`: {pair.Value}");
        }

        builder.AppendLine();
        builder.AppendLine("## Muestra (primeros 25)");
        foreach (var entry in entries.Take(25))
        {
            builder.AppendLine(
                $"- `{entry.ItemId}` | {entry.Category} | {entry.TypeName} | IconId {entry.IconId} | preview={entry.IconPreviewAvailable} | {entry.NameEs}");
        }

        builder.AppendLine();
        builder.AppendLine("## Filtros planificados en Angular");
        builder.AppendLine("- NameEs / NameEn / ItemId / IconId / TypeName");
        builder.AppendLine("- Sin copia masiva de PNG en esta fase");
        return builder.ToString();
    }

    private static readonly JsonSerializerOptions JsonWriteOptions = new() { WriteIndented = true };
}

internal sealed record ItemSkinCatalogEntry(
    int ItemId,
    int TypeId,
    string TypeName,
    string NameEs,
    string NameEn,
    int IconId,
    int AppearanceId,
    bool ClientKnown,
    bool IconPreviewAvailable,
    string Category);

internal sealed record ItemSkinCatalogSummary(
    DateTimeOffset GeneratedAtUtc,
    int TotalIndexEntries,
    int CatalogEntries,
    int SkippedWeapons,
    int SkippedUnreadable,
    int WithIconPreview,
    IReadOnlyDictionary<string, int> Categories,
    IReadOnlyList<string> PlannedAngularFolders);

internal sealed record ItemSkinCatalogDryRunResult(
    string OutputDirectory,
    string JsonPath,
    string MarkdownPath,
    ItemSkinCatalogSummary Summary);
