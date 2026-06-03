using System.Globalization;
using System.Text;
using RollblackLegacy.Admin.Contracts.ClientIdentity;

namespace ItemSpritePreviewPipeline;

internal static class SpritePreviewAuditRunner
{
    public static IReadOnlyList<SpritePreviewAuditRow> BuildRows(
        SpritePreviewPaths paths,
        IReadOnlyList<ClientItemIdentityCheckResultDto> identityResults)
    {
        return identityResults
            .Select(item => BuildRow(paths, item))
            .ToArray();
    }

    public static string WriteMarkdown(
        DateTimeOffset generatedAtUtc,
        SpritePreviewPaths paths,
        IReadOnlyList<SpritePreviewAuditRow> rows,
        AppearanceProbeResult? appearanceProbe)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Item Sprite Preview — Audit Report");
        builder.AppendLine();
        builder.AppendLine($"Generated: `{generatedAtUtc:yyyy-MM-dd HH:mm:ss 'UTC'}`");
        builder.AppendLine($"Mode: `audit` (read-only, sin extracción D2P)");
        builder.AppendLine();
        builder.AppendLine("## Environment");
        builder.AppendLine();
        builder.AppendLine($"- Repo: `{paths.RepoRoot}`");
        builder.AppendLine($"- Client root: `{paths.ClientRoot}`");
        builder.AppendLine($"- Items.d2o: `{(File.Exists(paths.ItemsD2oPath) ? "present" : "missing")}` → `{paths.ItemsD2oPath}`");
        builder.AppendLine($"- Appearances.d2o: `{(File.Exists(paths.AppearancesD2oPath) ? "present" : "missing")}` → `{paths.AppearancesD2oPath}`");
        builder.AppendLine($"- Item bitmap D2P: `{paths.DescribeD2pPacks()}`");
        builder.AppendLine($"- Angular by-icon: `{paths.ByIconDirectory}`");
        builder.AppendLine($"- Angular by-item: `{paths.ByItemDirectory}`");
        builder.AppendLine($"- Angular by-appearance: `{paths.ByAppearanceDirectory}`");
        builder.AppendLine($"- Legacy unpacked bitmap: `{(paths.LegacyItemBitmapDirectory ?? "(not present in legacy-reference)")}`");
        builder.AppendLine($"- D2P index lookup in Phase 1: `NOT_IMPLEMENTED`");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine("| ItemId | DB Name | IconId | AppearanceId | ClientKnown | IconPreview | AppearancePreview | Auto-resolve | Source hint |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(" | ",
                $"`{row.ItemId}`",
                Escape(row.DbName),
                FormatId(row.IconId),
                FormatId(row.AppearanceId),
                row.ClientKnown ? "yes" : "no",
                row.IconPreviewAvailable ? "yes" : "no",
                row.AppearancePreviewAvailable ? "yes" : "no",
                row.CanResolveAutomatically ? "yes" : "no",
                Escape(row.ClientAssetSourceHint)));
        }

        builder.AppendLine();
        builder.AppendLine("## Detailed cases");
        builder.AppendLine();

        foreach (var row in rows)
        {
            builder.AppendLine($"### Item `{row.ItemId}` — {Escape(row.DbName)}");
            builder.AppendLine();
            builder.AppendLine($"- IconId: `{FormatId(row.IconId)}`");
            builder.AppendLine($"- AppearanceId: `{FormatId(row.AppearanceId)}`");
            builder.AppendLine($"- ClientKnown: `{row.ClientKnown}`");
            builder.AppendLine($"- Icon preview available (curated): `{row.IconPreviewAvailable}`");
            builder.AppendLine($"- Appearance preview available (curated): `{row.AppearancePreviewAvailable}`");
            builder.AppendLine($"- Curated icon file: `{row.CuratedIconSourceFile ?? "(missing)"}`");
            builder.AppendLine($"- Curated appearance file: `{row.CuratedAppearanceSourceFile ?? "(missing)"}`");
            builder.AppendLine($"- Client asset source: `{row.ClientAssetSourceHint}`");
            builder.AppendLine($"- Requires client patch: `{row.RequiresClientPatch}`");
            builder.AppendLine($"- Can resolve automatically (Phase 1): `{row.CanResolveAutomatically}`");
            builder.AppendLine($"- Recommended next step: {row.RecommendedNextStep}");
            builder.AppendLine();
        }

        if (appearanceProbe is not null)
        {
            builder.AppendLine("## Appearance probe (control)");
            builder.AppendLine();
            builder.AppendLine($"- AppearanceId: `{appearanceProbe.AppearanceId}`");
            builder.AppendLine($"- Hypothesis: `{appearanceProbe.Hypothesis}`");
            builder.AppendLine($"- Exists in Appearances.d2o: `{appearanceProbe.ExistsInAppearancesD2o}`");
            builder.AppendLine($"- Curated by-appearance PNG: `{appearanceProbe.CuratedPath ?? "(missing)"}`");
            builder.AppendLine($"- Notes: {appearanceProbe.Notes}");
            builder.AppendLine();
        }

        builder.AppendLine("## Strategy reminder");
        builder.AppendLine();
        builder.AppendLine("- D2P actual (`bitmap*.d2p`, `vector*.d2p`): requiere lector compatible Dofus 2.x en Phase 2+.");
        builder.AppendLine("- SWF legacy (`legacy-reference`): JPEXS/FFDec aplica a SWF/PNG exportados, no sustituye D2P.");
        builder.AppendLine("- PNG curados: copia manual mínima a `src/assets/item-previews/by-icon|by-item|by-appearance`.");
        return builder.ToString();
    }

    public static string WriteDocsTable(IReadOnlyList<SpritePreviewAuditRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("| Caso | ItemId | IconId | AppearanceId | ClientKnown | IconPreviewAvailable | AppearancePreviewAvailable | SourceFile | RecommendedNextStep |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");

        foreach (var row in rows)
        {
            var source = row.CuratedIconSourceFile
                ?? row.CuratedAppearanceSourceFile
                ?? row.ClientAssetSourceHint;
            builder.Append('|');
            builder.AppendLine(string.Join(" | ",
                Escape(row.DbName),
                row.ItemId.ToString(CultureInfo.InvariantCulture),
                FormatId(row.IconId),
                FormatId(row.AppearanceId),
                row.ClientKnown ? "yes" : "no",
                row.IconPreviewAvailable ? "yes" : "no",
                row.AppearancePreviewAvailable ? "yes" : "no",
                Escape(source),
                Escape(row.RecommendedNextStep)));
        }

        return builder.ToString();
    }

    private static SpritePreviewAuditRow BuildRow(SpritePreviewPaths paths, ClientItemIdentityCheckResultDto item)
    {
        var iconId = item.DbIconId ?? item.ClientIconId;
        var appearanceId = item.DbAppearanceId ?? item.ClientAppearanceId;

        var byItemPath = Path.Combine(paths.ByItemDirectory, $"{item.ItemId}.png");
        var byIconPath = iconId > 0
            ? Path.Combine(paths.ByIconDirectory, $"{iconId}.png")
            : null;
        var byAppearancePath = appearanceId > 0
            ? Path.Combine(paths.ByAppearanceDirectory, $"{appearanceId}.png")
            : null;

        var hasByItem = File.Exists(byItemPath);
        var hasByIcon = byIconPath is not null && File.Exists(byIconPath);
        var hasByAppearance = byAppearancePath is not null && File.Exists(byAppearancePath);

        var iconPreviewAvailable = hasByItem || hasByIcon || item.IconPreviewFound;
        var appearancePreviewAvailable = hasByAppearance;

        string? curatedIcon = hasByItem
            ? byItemPath
            : hasByIcon
                ? byIconPath
                : item.PreviewPath;

        var requiresPatch = item.Status.NeedsClientPatch || !item.ClientKnown;

        var clientSourceHint = BuildClientSourceHint(paths, iconId, appearanceId, hasByIcon, hasByItem);

        var canAutoResolve = iconPreviewAvailable || appearancePreviewAvailable;

        var nextStep = BuildRecommendedNextStep(
            item,
            iconId,
            appearanceId,
            iconPreviewAvailable,
            appearancePreviewAvailable,
            requiresPatch,
            paths.ClientDataPresent);

        return new SpritePreviewAuditRow(
            item.ItemId,
            item.DbName,
            iconId,
            appearanceId,
            item.ClientKnown,
            iconPreviewAvailable,
            appearancePreviewAvailable,
            curatedIcon,
            hasByAppearance ? byAppearancePath : null,
            clientSourceHint,
            requiresPatch,
            canAutoResolve,
            nextStep);
    }

    private static string BuildClientSourceHint(
        SpritePreviewPaths paths,
        int? iconId,
        int? appearanceId,
        bool hasCuratedIcon,
        bool hasCuratedByItem)
    {
        if (hasCuratedByItem || hasCuratedIcon)
        {
            return "Angular curated PNG";
        }

        if (iconId is > 0 && paths.ItemBitmapD2pPaths.Count > 0)
        {
            return $"Client D2P packs ({paths.DescribeD2pPacks()}); index lookup pending Phase 2";
        }

        if (appearanceId is > 0 && File.Exists(paths.AppearancesD2oPath))
        {
            return "Appearances.d2o + gfx pipeline (not wired in Phase 1)";
        }

        if (paths.LegacyItemBitmapDirectory is not null && iconId is > 0)
        {
            var legacyPng = Path.Combine(paths.LegacyItemBitmapDirectory, $"{iconId}.png");
            if (File.Exists(legacyPng))
            {
                return $"Legacy unpacked PNG: {legacyPng}";
            }
        }

        return paths.ClientDataPresent
            ? "No curated PNG; client packs present but not indexed"
            : "Client data or D2P packs missing in workspace";
    }

    private static string BuildRecommendedNextStep(
        ClientItemIdentityCheckResultDto item,
        int? iconId,
        int? appearanceId,
        bool iconPreviewAvailable,
        bool appearancePreviewAvailable,
        bool requiresPatch,
        bool clientDataPresent)
    {
        if (requiresPatch)
        {
            return item.Status.RecommendedAction;
        }

        if (iconPreviewAvailable && (appearanceId is null or <= 0 || appearancePreviewAvailable))
        {
            return "Mantener catálogo curado; validar runtime en Angular.";
        }

        if (iconId is > 0 && !iconPreviewAvailable && clientDataPresent)
        {
            return $"Phase 2: extraer IconId {iconId} desde D2P o copiar manualmente a by-icon/{iconId}.png (máx. 1–3 assets por fase).";
        }

        if (appearanceId is > 0 && !appearancePreviewAvailable)
        {
            var appearanceKnown = item.Appearance.Exists == true;
            return appearanceKnown
                ? $"Definir pipeline appearance: curar by-appearance/{appearanceId}.png tras investigar gfx vinculado en Appearances.d2o."
                : $"Corregir AppearanceId {appearanceId} en DB o publicar entrada en Appearances.d2o antes de preview equipado.";
        }

        return "Sin acción de preview en Phase 1; item ya alineado con identidad cliente.";
    }

    private static string Escape(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Replace("|", "\\|").Replace("\n", " ");

    private static string FormatId(int? value) =>
        value is > 0 ? value.Value.ToString(CultureInfo.InvariantCulture) : "—";
}

internal sealed record AppearanceProbeResult(
    int AppearanceId,
    string Hypothesis,
    bool? ExistsInAppearancesD2o,
    string? CuratedPath,
    string Notes);
