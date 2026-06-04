using System.Text;
using RollblackLegacy.Admin.Contracts.Items;

namespace ClientItemPublicationPipeline;

internal static class PublicationManifestMarkdownWriter
{
    public static string Write(ItemPublicationManifestDto manifest)
    {
        var body = new StringBuilder();
        body.AppendLine("# Item Publication Manifest (dry-run)");
        body.AppendLine();
        body.AppendLine($"Generated: `{manifest.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss 'UTC'}`");
        body.AppendLine();
        body.AppendLine("## Summary");
        body.AppendLine();
        body.AppendLine($"| Field | Value |");
        body.AppendLine($"| --- | --- |");
        body.AppendLine($"| DbItemId | {manifest.DbItemId} |");
        body.AppendLine($"| TargetClientItemId | {manifest.TargetClientItemId} |");
        body.AppendLine($"| PrimaryState | `{manifest.PrimaryState}` |");
        body.AppendLine($"| ClientKnown | {manifest.ClientKnown} |");
        body.AppendLine($"| CanPublishAutomatically | {manifest.CanPublishAutomatically} |");
        body.AppendLine($"| NameEs | {manifest.NameEs ?? "(null)"} |");
        body.AppendLine($"| NameEn | {manifest.NameEn ?? "(null)"} |");
        body.AppendLine($"| TypeId | {manifest.TypeId} ({manifest.TypeName ?? "?"}) |");
        body.AppendLine($"| IconId | {manifest.IconId} |");
        body.AppendLine($"| DescriptionId | {manifest.DescriptionId} |");
        body.AppendLine($"| SourceTemplateItemId | {manifest.SourceTemplateItemId?.ToString() ?? "(none)"} |");
        body.AppendLine($"| StagingOutputPath | `{manifest.StagingOutputPath}` |");
        body.AppendLine();
        body.AppendLine("## Effects");
        body.AppendLine();
        body.AppendLine(manifest.EffectsSummary);
        body.AppendLine();
        AppendList(body, "States", manifest.States);
        AppendList(body, "BlockingReasons", manifest.BlockingReasons);
        AppendList(body, "RequiredClientActions", manifest.RequiredClientActions);
        AppendList(body, "FilesToPatch", manifest.FilesToPatch);
        AppendList(body, "Risks", manifest.Risks);
        body.AppendLine("## Client paths");
        body.AppendLine();
        body.AppendLine($"ClientRoot: `{manifest.ClientRootPath ?? "(unavailable)"}`");
        return body.ToString();
    }

    private static void AppendList(StringBuilder body, string title, IReadOnlyList<string> values)
    {
        body.AppendLine($"## {title}");
        body.AppendLine();
        if (values.Count == 0)
        {
            body.AppendLine("(none)");
            body.AppendLine();
            return;
        }

        foreach (var value in values)
        {
            body.AppendLine($"- {value}");
        }

        body.AppendLine();
    }
}
