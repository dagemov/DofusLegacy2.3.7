using System.Globalization;
using System.Text;
using Sunshine.Protocol.Tools.D2p;

namespace ItemSpritePreviewPipeline.D2p;

internal sealed record D2pPackAuditResult(
    string FilePath,
    long FileSizeBytes,
    bool HeaderValid,
    int EntriesCount,
    int PropertiesCount,
    int OffsetBase,
    IReadOnlyList<string> LinkProperties,
    IReadOnlyList<string> SampleEntryPaths,
    string? Error);

internal static class D2pPackAuditor
{
    public static IReadOnlyList<D2pPackAuditResult> AuditPacks(IEnumerable<string> packPaths)
    {
        return packPaths.Select(AuditPack).ToArray();
    }

    public static string WriteMarkdown(
        DateTimeOffset generatedAtUtc,
        string repoRoot,
        IReadOnlyList<D2pPackAuditResult> packs,
        int? probeIconId,
        IReadOnlyList<D2pIconMatch> probeMatches)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# D2P Pack Audit");
        builder.AppendLine();
        builder.AppendLine($"Generated: `{generatedAtUtc:yyyy-MM-dd HH:mm:ss 'UTC'}`");
        builder.AppendLine($"Repo: `{repoRoot}`");
        builder.AppendLine($"Reader: `Sunshine.Protocol.Tools.D2p.D2pFile` (Sunshine.Protocol.D2pReadOnly)");
        builder.AppendLine();
        builder.AppendLine("## Pack summary");
        builder.AppendLine();
        builder.AppendLine("| File | Size (MB) | Header | Entries | Links | OffsetBase |");
        builder.AppendLine("| --- | ---: | --- | ---: | --- | ---: |");

        foreach (var pack in packs)
        {
            var sizeMb = pack.FileSizeBytes / (1024d * 1024d);
            builder.AppendLine(string.Join(" | ",
                $"`{Path.GetFileName(pack.FilePath)}`",
                sizeMb.ToString("F2", CultureInfo.InvariantCulture),
                pack.HeaderValid ? "OK" : "FAIL",
                pack.EntriesCount.ToString(CultureInfo.InvariantCulture),
                pack.LinkProperties.Count == 0 ? "—" : string.Join(", ", pack.LinkProperties),
                pack.OffsetBase.ToString(CultureInfo.InvariantCulture)));
        }

        builder.AppendLine();
        builder.AppendLine("## Structure notes");
        builder.AppendLine();
        builder.AppendLine("- Header esperado: bytes `0x02`, `0x01` al inicio del archivo.");
        builder.AppendLine("- Índice interno: tabla de 24 bytes al final (`OffsetBase`, `EntriesCount`, offsets de definiciones).");
        builder.AppendLine("- Cada entrada: `UTF path` + `int index` + `int size` (big-endian via `FastBigEndianReader`).");
        builder.AppendLine("- Payload: bytes en `OffsetBase + index`; **no** hay compresión en el lector D2P de Sunshine.");
        builder.AppendLine("- Los nombres internos son rutas relativas (ej. `dofus_png/23012.png`, `amuletos_png/1001.png`).");
        builder.AppendLine();

        foreach (var pack in packs)
        {
            builder.AppendLine($"### `{Path.GetFileName(pack.FilePath)}`");
            builder.AppendLine();
            if (!string.IsNullOrWhiteSpace(pack.Error))
            {
                builder.AppendLine($"Error: `{pack.Error}`");
                builder.AppendLine();
                continue;
            }

            builder.AppendLine($"- Ruta: `{pack.FilePath}`");
            builder.AppendLine($"- Tamaño: `{pack.FileSizeBytes:N0}` bytes");
            builder.AppendLine($"- Entradas: `{pack.EntriesCount}`");
            builder.AppendLine($"- Propiedades: `{pack.PropertiesCount}`");
            if (pack.SampleEntryPaths.Count > 0)
            {
                builder.AppendLine("- Muestra de rutas internas:");
                foreach (var sample in pack.SampleEntryPaths)
                {
                    builder.AppendLine($"  - `{sample}`");
                }
            }

            builder.AppendLine();
        }

        if (probeIconId is > 0)
        {
            builder.AppendLine($"## IconId probe `{probeIconId}`");
            builder.AppendLine();
            if (probeMatches.Count == 0)
            {
                builder.AppendLine("No se encontró ninguna entrada cuyo nombre termine en `{iconId}.png`.");
            }
            else
            {
                builder.AppendLine("| Pack | Entry path | Size | PNG signature |");
                builder.AppendLine("| --- | --- | ---: | --- |");
                foreach (var match in probeMatches)
                {
                    builder.AppendLine(string.Join(" | ",
                        $"`{Path.GetFileName(match.PackPath)}`",
                        $"`{match.EntryPath}`",
                        match.Size.ToString(CultureInfo.InvariantCulture),
                        match.LooksLikePng ? "yes" : "no"));
                }
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static D2pPackAuditResult AuditPack(string packPath)
    {
        if (!File.Exists(packPath))
        {
            return new D2pPackAuditResult(packPath, 0, false, 0, 0, 0, [], [], "File not found.");
        }

        var fileInfo = new FileInfo(packPath);
        try
        {
            using var pack = new D2pFile(packPath);
            var links = pack.Properties
                .Where(p => string.Equals(p.Key, "link", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Value)
                .ToArray();

            var samples = pack.Entries
                .Select(e => e.FullFileName)
                .OrderBy(static n => n, StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToArray();

            return new D2pPackAuditResult(
                packPath,
                fileInfo.Length,
                true,
                pack.IndexTable.EntriesCount,
                pack.IndexTable.PropertiesCount,
                pack.IndexTable.OffsetBase,
                links,
                samples,
                null);
        }
        catch (Exception ex)
        {
            return new D2pPackAuditResult(
                packPath,
                fileInfo.Length,
                false,
                0,
                0,
                0,
                [],
                [],
                ex.Message);
        }
    }
}

internal sealed record D2pIconMatch(
    string PackPath,
    string EntryPath,
    int Size,
    bool LooksLikePng,
    byte[]? PreviewBytes);
