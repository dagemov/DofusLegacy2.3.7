using System.Globalization;
using Sunshine.Protocol.Tools.D2p;

namespace ClientItemPublicationPipeline.Package;

internal sealed record CatalogIconResolution(
    bool AvailableInD2p,
    bool AvailableInAdmin,
    string IconSource,
    string? D2pPackFile,
    string? D2pEntryPath);

internal static class CatalogD2pIconResolver
{
    private static readonly string[] IconEntrySuffixes = [".png", ".PNG"];

    public static CatalogIconResolution Resolve(
        int iconId,
        IReadOnlyList<string> bitmapD2pPaths,
        string adminByIconDirectory)
    {
        var adminPath = Path.Combine(adminByIconDirectory, $"{iconId}.png");
        if (File.Exists(adminPath))
        {
            return new CatalogIconResolution(
                AvailableInD2p: true,
                AvailableInAdmin: true,
                IconSource: "admin-by-icon",
                null,
                null);
        }

        var token = iconId.ToString(CultureInfo.InvariantCulture);
        foreach (var packPath in bitmapD2pPaths)
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
                if (LooksLikePng(bytes))
                {
                    return new CatalogIconResolution(
                        true,
                        false,
                        "client-bitmap-d2p",
                        Path.GetFileName(packPath),
                        entry.FullFileName);
                }
            }
        }

        return new CatalogIconResolution(false, false, "missing", null, null);
    }

    public static bool TryExtractPng(IReadOnlyList<string> bitmapD2pPaths, int iconId, string outputPath)
    {
        var token = iconId.ToString(CultureInfo.InvariantCulture);
        foreach (var packPath in bitmapD2pPaths)
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
                if (!LooksLikePng(bytes))
                {
                    continue;
                }

                var parent = Path.GetDirectoryName(outputPath)!;
                Directory.CreateDirectory(parent);
                File.WriteAllBytes(outputPath, bytes);
                return true;
            }
        }

        return false;
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
        bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;
}
