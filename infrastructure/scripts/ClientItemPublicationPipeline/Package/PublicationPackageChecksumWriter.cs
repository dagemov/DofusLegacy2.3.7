using System.Security.Cryptography;
using System.Text;

namespace ClientItemPublicationPipeline.Package;

internal static class PublicationPackageChecksumWriter
{
    public static IReadOnlyDictionary<string, string> ComputeChecksums(
        string packageDirectory,
        IEnumerable<string> relativePaths)
    {
        var checksums = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var relative in relativePaths)
        {
            var absolute = Path.Combine(packageDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolute))
            {
                continue;
            }

            checksums[relative] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(absolute))).ToLowerInvariant();
        }

        return checksums;
    }

    public static void WriteChecksumsFile(string packageDirectory, IReadOnlyDictionary<string, string> checksums)
    {
        var path = Path.Combine(packageDirectory, PublicationPackagePaths.ChecksumsFile);
        var builder = new StringBuilder();
        builder.AppendLine("# SHA-256 — publication staging package (Phase 3C)");
        foreach (var entry in checksums.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(entry.Value);
            builder.Append("  ");
            builder.AppendLine(entry.Key);
        }

        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
    }
}
