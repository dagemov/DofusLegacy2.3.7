using System.Globalization;
using System.Text;
using Sunshine.Protocol.Tools.D2p;

namespace ItemSpritePreviewPipeline.D2p;

internal sealed record D2pIconExtractionResult(
    int IconId,
    bool Success,
    string? SourcePackPath,
    string? SourceEntryPath,
    string? OutputFilePath,
    int PayloadSize,
    bool LooksLikePng,
    string Message);

internal static class D2pIconExtractor
{
    private static readonly string[] IconEntrySuffixes = [".png", ".PNG"];

    public static IReadOnlyList<D2pIconMatch> FindMatches(IEnumerable<string> packPaths, int iconId)
    {
        var matches = new List<D2pIconMatch>();
        var token = iconId.ToString(CultureInfo.InvariantCulture);

        foreach (var packPath in packPaths)
        {
            if (!File.Exists(packPath))
            {
                continue;
            }

            using var pack = new D2pFile(packPath);
            foreach (var entry in pack.Entries)
            {
                if (!EntryMatchesIcon(entry.FullFileName, token))
                {
                    continue;
                }

                var bytes = pack.ReadFile(entry);
                matches.Add(new D2pIconMatch(
                    packPath,
                    entry.FullFileName,
                    bytes.Length,
                    LooksLikePng(bytes),
                    bytes));
            }
        }

        return matches;
    }

    public static D2pIconExtractionResult ExtractIcon(
        IEnumerable<string> packPaths,
        int iconId,
        string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var matches = FindMatches(packPaths, iconId);
        if (matches.Count == 0)
        {
            return new D2pIconExtractionResult(
                iconId,
                false,
                null,
                null,
                null,
                0,
                false,
                $"No se encontró ninguna entrada D2P para IconId {iconId}.");
        }

        var preferred = matches
            .OrderByDescending(m => m.LooksLikePng)
            .ThenBy(m => m.EntryPath, StringComparer.OrdinalIgnoreCase)
            .First();

        if (!preferred.LooksLikePng)
        {
            return new D2pIconExtractionResult(
                iconId,
                false,
                preferred.PackPath,
                preferred.EntryPath,
                null,
                preferred.Size,
                false,
                "La entrada existe pero el payload no tiene firma PNG; revisar formato o capa de transformación.");
        }

        var outputPath = Path.Combine(outputDirectory, $"{iconId}.png");
        File.WriteAllBytes(outputPath, preferred.PreviewBytes!);

        return new D2pIconExtractionResult(
            iconId,
            true,
            preferred.PackPath,
            preferred.EntryPath,
            outputPath,
            preferred.Size,
            true,
            $"Extraído desde `{Path.GetFileName(preferred.PackPath)}` → `{preferred.EntryPath}`.");
    }

    public static string WriteExtractionMarkdown(D2pIconExtractionResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# D2P Icon Extraction");
        builder.AppendLine();
        builder.AppendLine($"- IconId: `{result.IconId}`");
        builder.AppendLine($"- Success: `{result.Success}`");
        builder.AppendLine($"- Source pack: `{result.SourcePackPath ?? "(n/a)"}`");
        builder.AppendLine($"- Source entry: `{result.SourceEntryPath ?? "(n/a)"}`");
        builder.AppendLine($"- Output: `{result.OutputFilePath ?? "(n/a)"}`");
        builder.AppendLine($"- Payload size: `{result.PayloadSize}`");
        builder.AppendLine($"- PNG signature: `{result.LooksLikePng}`");
        builder.AppendLine($"- Message: {result.Message}");
        return builder.ToString();
    }

    private static bool EntryMatchesIcon(string fullFileName, string iconToken)
    {
        var fileName = Path.GetFileName(fullFileName);
        foreach (var suffix in IconEntrySuffixes)
        {
            if (string.Equals(fileName, iconToken + suffix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LooksLikePng(byte[] bytes) =>
        bytes.Length >= 8
        && bytes[0] == 0x89
        && bytes[1] == 0x50
        && bytes[2] == 0x4E
        && bytes[3] == 0x47;
}
