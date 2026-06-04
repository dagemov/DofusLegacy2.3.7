using System.Globalization;
using System.Text;

namespace ItemSpritePreviewPipeline.D2p;

internal sealed record CuratedCopyPlan(
    int IconId,
    string? SourceD2pPath,
    string? SourceEntryPath,
    string TargetPath,
    bool WillOverwrite,
    bool PngSignatureValid,
    bool TargetInsideByIcon,
    bool SourceFound,
    string Message);

internal static class CuratedIconCopyPlanner
{
    public static CuratedCopyPlan Plan(
        SpritePreviewPaths paths,
        int iconId,
        IReadOnlyList<D2pIconMatch> matches)
    {
        var targetPath = Path.GetFullPath(Path.Combine(paths.ByIconDirectory, $"{iconId}.png"));
        var willOverwrite = File.Exists(targetPath);
        var targetInsideByIcon = IsUnderDirectory(targetPath, Path.GetFullPath(paths.ByIconDirectory));

        if (matches.Count == 0)
        {
            return new CuratedCopyPlan(
                iconId,
                null,
                null,
                targetPath,
                willOverwrite,
                false,
                targetInsideByIcon,
                false,
                $"No se encontró entrada D2P para IconId {iconId}.");
        }

        var preferred = matches
            .OrderByDescending(m => m.LooksLikePng)
            .ThenBy(m => m.EntryPath, StringComparer.OrdinalIgnoreCase)
            .First();

        return new CuratedCopyPlan(
            iconId,
            preferred.PackPath,
            preferred.EntryPath,
            targetPath,
            willOverwrite,
            preferred.LooksLikePng,
            targetInsideByIcon,
            true,
            preferred.LooksLikePng
                ? "Listo para copia curada controlada."
                : "Entrada D2P encontrada pero el payload no es PNG válido.");
    }

    public static string WriteDryRunReport(CuratedCopyPlan plan)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Curated copy — dry run");
        builder.AppendLine();
        builder.AppendLine($"| Campo | Valor |");
        builder.AppendLine($"| --- | --- |");
        builder.AppendLine($"| iconId | `{plan.IconId}` |");
        builder.AppendLine($"| source d2p | `{plan.SourceD2pPath ?? "(missing)"}` |");
        builder.AppendLine($"| source entry | `{plan.SourceEntryPath ?? "(missing)"}` |");
        builder.AppendLine($"| target path | `{plan.TargetPath}` |");
        builder.AppendLine($"| will overwrite | `{(plan.WillOverwrite ? "yes" : "no")}` |");
        builder.AppendLine($"| png signature valid | `{(plan.PngSignatureValid ? "yes" : "no")}` |");
        builder.AppendLine($"| target inside by-icon | `{(plan.TargetInsideByIcon ? "yes" : "no")}` |");
        builder.AppendLine();
        builder.AppendLine($"Mensaje: {plan.Message}");
        builder.AppendLine();
        builder.AppendLine("No se copió ningún archivo (dry-run).");
        return builder.ToString();
    }

    public static void PrintDryRunConsole(CuratedCopyPlan plan)
    {
        Console.WriteLine("=== Dry-run curated copy ===");
        Console.WriteLine($"iconId: {plan.IconId}");
        Console.WriteLine($"source d2p: {plan.SourceD2pPath ?? "(missing)"}");
        Console.WriteLine($"source entry: {plan.SourceEntryPath ?? "(missing)"}");
        Console.WriteLine($"target path: {plan.TargetPath}");
        Console.WriteLine($"will overwrite: {(plan.WillOverwrite ? "yes" : "no")}");
        Console.WriteLine($"png signature valid: {(plan.PngSignatureValid ? "yes" : "no")}");
        Console.WriteLine($"target inside by-icon: {(plan.TargetInsideByIcon ? "yes" : "no")}");
        Console.WriteLine($"message: {plan.Message}");
    }

    public static bool CanApproveCopy(CuratedCopyPlan plan, bool allowOverwrite, out string error)
    {
        if (!plan.SourceFound)
        {
            error = plan.Message;
            return false;
        }

        if (!plan.PngSignatureValid)
        {
            error = "No se puede copiar: firma PNG inválida.";
            return false;
        }

        if (!plan.TargetInsideByIcon)
        {
            error = "No se puede copiar: la ruta destino está fuera de by-icon/.";
            return false;
        }

        if (plan.WillOverwrite && !allowOverwrite)
        {
            error = "El PNG curado ya existe. Usa --overwrite-curated para reemplazarlo.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool IsUnderDirectory(string filePath, string directoryPath)
    {
        var normalizedDirectory = directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return filePath.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase)
            || string.Equals(filePath, directoryPath, StringComparison.OrdinalIgnoreCase);
    }
}
