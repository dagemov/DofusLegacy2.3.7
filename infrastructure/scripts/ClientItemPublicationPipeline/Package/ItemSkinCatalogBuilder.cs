using System.Text;
using System.Text.Json;
using D2oItem = Sunshine.Protocol.Tools.D2o.Classes.Item;

namespace ClientItemPublicationPipeline.Package;

internal sealed class ItemSkinCatalogBuilder
{
    public ItemSkinCatalogBuildResult Build(
        ClientSkinCatalogPaths paths,
        string outputDirectory,
        bool excludeWeapons = true)
    {
        Directory.CreateDirectory(outputDirectory);

        if (!File.Exists(paths.ItemsD2oPath))
        {
            throw new FileNotFoundException($"Items.d2o no encontrado: {paths.ItemsD2oPath}");
        }

        var es = D2i.D2iFile.Load(paths.I18nEsPath);
        var en = D2i.D2iFile.Load(paths.I18nEnPath);
        var weaponRegistry = ItemTypeWeaponRegistry.Build(paths.ItemTypesD2oPath, es, en);
        weaponRegistry.WriteExclusionReport(outputDirectory);

        var itemIds = ClientPatchD2oIndex.ReadIds(paths.ItemsD2oPath).OrderBy(static id => id).ToList();
        var reader = new Sunshine.Protocol.Tools.D2o.D2OReader(paths.ItemsD2oPath);

        var entries = new List<ItemSkinCatalogEntryDto>();
        var skippedWeapons = 0;
        var skippedUnreadable = 0;
        var skippedUncategorized = 0;

        foreach (var itemId in itemIds)
        {
            D2oItem item;
            try
            {
                item = reader.ReadObject<D2oItem>(itemId, true);
            }
            catch
            {
                skippedUnreadable++;
                continue;
            }

            if (excludeWeapons && weaponRegistry.IsWeapon(item.typeId))
            {
                skippedWeapons++;
                continue;
            }

            var category = ItemSkinCategoryMap.ResolveCategory(item.typeId);
            if (!ItemSkinCategoryMap.IsSupportedExportCategory(category))
            {
                skippedUncategorized++;
                continue;
            }

            es.TryGetText(item.nameId, out var nameEs);
            en.TryGetText(item.nameId, out var nameEn);

            var typeNameEs = ResolveTypeName(item.typeId);
            var typeNameEn = typeNameEs;
            var icon = CatalogD2pIconResolver.Resolve(item.iconId, paths.BitmapD2pPaths, paths.AdminByIconDirectory);
            var targetPath = $"src/assets/item-previews/by-category/{category}/{item.iconId}.png";

            entries.Add(new ItemSkinCatalogEntryDto(
                itemId,
                nameEs ?? string.Empty,
                nameEn ?? string.Empty,
                item.typeId,
                typeNameEs ?? Enum.GetName(typeof(Sunshine.Protocol.Enums.ItemTypeEnum), item.typeId) ?? $"Type{item.typeId}",
                typeNameEn ?? typeNameEs ?? string.Empty,
                category,
                item.iconId,
                item.appearanceId,
                ClientKnown: true,
                IconPreviewAvailable: icon.AvailableInAdmin || icon.AvailableInD2p,
                icon.IconSource,
                targetPath,
                ExcludedReason: null));
        }

        reader.Close();

        var summary = new ItemSkinCatalogSummaryDto(
            DateTimeOffset.UtcNow,
            itemIds.Count,
            entries.Count,
            skippedWeapons,
            skippedUnreadable,
            skippedUncategorized,
            entries.Count(static e => e.IconPreviewAvailable),
            weaponRegistry.ExcludedTypeIds.Count,
            entries
                .GroupBy(static e => e.Category, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(static g => g.Key, static g => g.Count(), StringComparer.OrdinalIgnoreCase));

        var jsonPath = Path.Combine(outputDirectory, "item-skin-catalog.json");
        var mdPath = Path.Combine(outputDirectory, "item-skin-catalog.md");
        var payload = new { summary, entries, weaponExclusions = weaponRegistry.Exclusions };
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8);
        File.WriteAllText(mdPath, WriteMarkdown(summary, entries), Encoding.UTF8);

        return new ItemSkinCatalogBuildResult(outputDirectory, jsonPath, mdPath, summary, entries, weaponRegistry);
    }

    private static string ResolveTypeName(int typeId) =>
        Enum.IsDefined(typeof(Sunshine.Protocol.Enums.ItemTypeEnum), typeId)
            ? Enum.GetName(typeof(Sunshine.Protocol.Enums.ItemTypeEnum), typeId) ?? $"Type{typeId}"
            : $"Type{typeId}";

    private static string WriteMarkdown(ItemSkinCatalogSummaryDto summary, IReadOnlyList<ItemSkinCatalogEntryDto> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Item skin catalog (by category)");
        builder.AppendLine();
        builder.AppendLine($"- Generado: `{summary.GeneratedAtUtc:O}`");
        builder.AppendLine($"- Indice Items.d2o: **{summary.TotalIndexEntries}**");
        builder.AppendLine($"- Catalogo: **{summary.CatalogEntries}**");
        builder.AppendLine($"- Armas excluidas: **{summary.SkippedWeapons}**");
        builder.AppendLine($"- TypeIds arma registrados: **{summary.WeaponTypeIdsExcluded}**");
        builder.AppendLine($"- Sin categoria exportable: **{summary.SkippedUncategorized}**");
        builder.AppendLine($"- Con preview: **{summary.WithIconPreview}**");
        builder.AppendLine();
        builder.AppendLine("## Por categoría");
        foreach (var pair in summary.Categories.OrderBy(static p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"- `{pair.Key}`: {pair.Value}");
        }

        builder.AppendLine();
        builder.AppendLine("## Muestra dofus (hasta 15)");
        foreach (var entry in entries.Where(static e => e.Category == "dofus").Take(15))
        {
            builder.AppendLine(
                $"- `{entry.ItemId}` | {entry.NameEs} | IconId {entry.IconId} | {entry.IconSource} | preview={entry.IconPreviewAvailable}");
        }

        return builder.ToString();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}

internal sealed record ItemSkinCatalogEntryDto(
    int ItemId,
    string NameEs,
    string NameEn,
    int TypeId,
    string TypeNameEs,
    string TypeNameEn,
    string Category,
    int IconId,
    int AppearanceId,
    bool ClientKnown,
    bool IconPreviewAvailable,
    string IconSource,
    string TargetAngularPath,
    string? ExcludedReason);

internal sealed record ItemSkinCatalogSummaryDto(
    DateTimeOffset GeneratedAtUtc,
    int TotalIndexEntries,
    int CatalogEntries,
    int SkippedWeapons,
    int SkippedUnreadable,
    int SkippedUncategorized,
    int WithIconPreview,
    int WeaponTypeIdsExcluded,
    IReadOnlyDictionary<string, int> Categories);

internal sealed record ItemSkinCatalogBuildResult(
    string OutputDirectory,
    string JsonPath,
    string MarkdownPath,
    ItemSkinCatalogSummaryDto Summary,
    IReadOnlyList<ItemSkinCatalogEntryDto> Entries,
    ItemTypeWeaponRegistry WeaponRegistry);
