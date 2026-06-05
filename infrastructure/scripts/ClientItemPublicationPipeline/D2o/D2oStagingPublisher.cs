using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sunshine.Protocol.Tools.D2o;
using D2oItem = Sunshine.Protocol.Tools.D2o.Classes.Item;

namespace ClientItemPublicationPipeline.D2o;

internal sealed class D2oStagingPublisher
{
    public D2oInspectClassResult InspectClass(string sourceItemsPath, string outputDirectory, string? focusClass)
    {
        var schema = D2oSchemaParser.Parse(sourceItemsPath);
        Directory.CreateDirectory(outputDirectory);

        var markdown = D2oSchemaReportWriter.WriteMarkdown(schema, focusClass);
        var json = D2oSchemaReportWriter.WriteJson(schema);
        var mdPath = Path.Combine(outputDirectory, "client-d2o-item-schema-report.md");
        var jsonPath = Path.Combine(outputDirectory, "d2o-schema.json");
        File.WriteAllText(mdPath, markdown, Encoding.UTF8);
        File.WriteAllText(jsonPath, json, Encoding.UTF8);

        return new D2oInspectClassResult(schema.IndexCount, schema.Classes.Count, mdPath, jsonPath);
    }

    public D2oRoundTripResult RoundTrip(string sourceItemsPath, string stagingDirectory)
    {
        Directory.CreateDirectory(stagingDirectory);
        var stagingItems = Path.Combine(stagingDirectory, "Items.d2o");
        File.Copy(sourceItemsPath, stagingItems, overwrite: true);

        var before = ReadIndexSummary(sourceItemsPath);
        var read = new D2OReader(stagingItems);
        var objects = read.ReadObjects(allownulled: true);
        read.Close();

        var roundTripPath = Path.Combine(stagingDirectory, "Items.roundtrip.d2o");
        if (File.Exists(roundTripPath))
        {
            File.Delete(roundTripPath);
        }

        File.Copy(stagingItems, roundTripPath, overwrite: true);

        using (var writer = new D2OWriter(roundTripPath))
        {
            writer.StartWriting(backupFile: false);
            writer.EndWriting();
        }

        var afterReader = new D2OReader(roundTripPath);
        var afterCount = afterReader.IndexCount;
        var item7754 = afterReader.ReadObject<D2oItem>(7754, true);
        afterReader.Close();

        var after = ReadIndexSummary(roundTripPath);
        var reportPath = Path.Combine(stagingDirectory, "client-d2o-roundtrip-report.md");
        File.WriteAllText(reportPath, BuildRoundTripReport(before, after, objects.Count, item7754), Encoding.UTF8);

        return new D2oRoundTripResult(
            before.Count,
            after.Count,
            before.Contains(7754) && after.Contains(7754),
            item7754 is not null,
            item7754?.typeId,
            item7754?.iconId,
            item7754?.appearanceId,
            stagingItems,
            roundTripPath,
            reportPath);
    }

    public D2oCloneItemResult CloneItem(
        string sourceItemsPath,
        string stagingDirectory,
        int sourceItemId,
        int targetItemId,
        int typeId,
        int iconId,
        int appearanceId)
    {
        Directory.CreateDirectory(stagingDirectory);
        var stagingItems = Path.Combine(stagingDirectory, "Items.d2o");
        if (!File.Exists(stagingItems))
        {
            File.Copy(sourceItemsPath, stagingItems, overwrite: true);
        }

        var reader = new D2OReader(stagingItems);
        var source = reader.ReadObject<D2oItem>(sourceItemId, true)
            ?? throw new InvalidOperationException($"Item #{sourceItemId} no encontrado o no es clase Item.");
        reader.Close();

        var clone = CloneItemRecord(source, targetItemId, typeId, iconId, appearanceId);

        using var writer = new D2OWriter(stagingItems);
        writer.StartWriting(backupFile: false);
        writer.Write(clone, targetItemId);
        writer.EndWriting();

        var verifyReader = new D2OReader(stagingItems);
        var exists = verifyReader.Indexes.ContainsKey(targetItemId);
        var written = exists ? verifyReader.ReadObject<D2oItem>(targetItemId, true) : null;
        verifyReader.Close();

        return new D2oCloneItemResult(
            sourceItemId,
            targetItemId,
            exists,
            written?.typeId,
            written?.iconId,
            written?.appearanceId,
            written?.nameId,
            written?.descriptionId,
            stagingItems);
    }

    private static D2oItem CloneItemRecord(D2oItem source, int targetItemId, int typeId, int iconId, int appearanceId) =>
        new()
        {
            id = targetItemId,
            nameId = source.nameId,
            typeId = typeId,
            descriptionId = source.descriptionId,
            iconId = iconId,
            level = source.level,
            weight = source.weight,
            cursed = source.cursed,
            useAnimationId = source.useAnimationId,
            usable = source.usable,
            targetable = source.targetable,
            price = source.price,
            twoHanded = source.twoHanded,
            etheral = source.etheral,
            itemSetId = source.itemSetId,
            criteria = source.criteria,
            hideEffects = source.hideEffects,
            appearanceId = appearanceId,
            recipeIds = source.recipeIds is null ? null : new List<uint>(source.recipeIds),
            bonusIsSecret = source.bonusIsSecret,
            possibleEffects = source.possibleEffects is null ? null : new List<object>(source.possibleEffects),
            favoriteSubAreas = source.favoriteSubAreas is null ? null : new List<uint>(source.favoriteSubAreas),
            favoriteSubAreasBonus = source.favoriteSubAreasBonus
        };

    private static HashSet<int> ReadIndexSummary(string path)
    {
        using var stream = File.OpenRead(path);
        var header = new byte[3];
        stream.ReadExactly(header);
        var buffer = new byte[4];
        stream.ReadExactly(buffer);
        var headerOffset = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer);
        stream.Position = headerOffset;
        stream.ReadExactly(buffer);
        var indexLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer);
        var ids = new HashSet<int>(indexLength / 8);
        for (var i = 0; i < indexLength; i += 8)
        {
            stream.ReadExactly(buffer);
            ids.Add(System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer));
            stream.ReadExactly(buffer);
        }

        return ids;
    }

    private static string BuildRoundTripReport(
        HashSet<int> before,
        HashSet<int> after,
        int objectCount,
        D2oItem? item7754)
    {
        var body = new StringBuilder();
        body.AppendLine("# D2O round-trip report (staging)");
        body.AppendLine();
        body.AppendLine($"Date: `{DateTimeOffset.UtcNow:u}`");
        body.AppendLine($"Index before: `{before.Count}`");
        body.AppendLine($"Index after: `{after.Count}`");
        body.AppendLine($"Objects read: `{objectCount}`");
        body.AppendLine($"Index preserved: `{(before.SetEquals(after) ? "yes" : "no")}`");
        body.AppendLine($"Item 7754 present after: `{after.Contains(7754)}`");
        if (item7754 is not null)
        {
            body.AppendLine($"Item 7754 typeId: `{item7754.typeId}`");
            body.AppendLine($"Item 7754 iconId: `{item7754.iconId}`");
            body.AppendLine($"Item 7754 appearanceId: `{item7754.appearanceId}`");
            body.AppendLine($"Item 7754 nameId: `{item7754.nameId}`");
        }

        return body.ToString();
    }
}

internal sealed record D2oInspectClassResult(int IndexCount, int ClassCount, string MarkdownPath, string JsonPath);

internal sealed record D2oRoundTripResult(
    int BeforeCount,
    int AfterCount,
    bool Index7754Preserved,
    bool Item7754Readable,
    int? Item7754TypeId,
    int? Item7754IconId,
    int? Item7754AppearanceId,
    string StagingItemsPath,
    string RoundTripPath,
    string ReportPath);

internal sealed record D2oCloneItemResult(
    int SourceItemId,
    int TargetItemId,
    bool TargetExists,
    int? TypeId,
    int? IconId,
    int? AppearanceId,
    int? NameId,
    int? DescriptionId,
    string StagingItemsPath);
