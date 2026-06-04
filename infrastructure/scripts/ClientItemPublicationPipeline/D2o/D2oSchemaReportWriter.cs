using System.Text;
using System.Text.Json;

namespace ClientItemPublicationPipeline.D2o;

internal static class D2oSchemaReportWriter
{
    public static string WriteMarkdown(D2oFileSchema schema, string? focusClass)
    {
        var body = new StringBuilder();
        body.AppendLine("# D2O schema report");
        body.AppendLine();
        body.AppendLine($"Source: `{schema.Path}`");
        body.AppendLine($"Index entries: `{schema.IndexCount}`");
        body.AppendLine();

        var classes = string.IsNullOrWhiteSpace(focusClass)
            ? schema.Classes
            : schema.Classes.Where(c => string.Equals(c.Name, focusClass, StringComparison.Ordinal)).ToArray();

        foreach (var cls in classes)
        {
            body.AppendLine($"## {cls.Name}");
            body.AppendLine();
            body.AppendLine($"Package: `{cls.PackageName}`");
            body.AppendLine($"ClassId: `{cls.ClassId}`");
            body.AppendLine();
            body.AppendLine("| # | Field | Type | Vector chain |");
            body.AppendLine("| ---: | --- | --- | --- |");
            for (var i = 0; i < cls.Fields.Count; i++)
            {
                var field = cls.Fields[i];
                body.AppendLine($"| {i + 1} | `{field.Name}` | `{field.Type}` | `{FormatVector(field.VectorTypes)}` |");
            }

            body.AppendLine();
        }

        return body.ToString();
    }

    public static string WriteJson(D2oFileSchema schema) =>
        JsonSerializer.Serialize(
            schema.Classes.Select(c => new
            {
                c.ClassId,
                c.Name,
                c.PackageName,
                Fields = c.Fields.Select(f => new
                {
                    f.Name,
                    Type = f.Type.ToString(),
                    VectorTypes = f.VectorTypes.Select(v => new { Type = v.Type.ToString(), v.Name }).ToArray()
                })
            }),
            new JsonSerializerOptions { WriteIndented = true });

    private static string FormatVector(IReadOnlyList<D2oVectorTypeSchema> vectorTypes) =>
        vectorTypes.Count == 0
            ? "-"
            : string.Join(" -> ", vectorTypes.Select(v => $"{v.Type}:{v.Name}"));
}
