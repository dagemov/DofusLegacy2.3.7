using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sunshine.Protocol.Tools.D2o;

var repoRoot = RepositoryRootResolver.Resolve(AppContext.BaseDirectory);
var stagingRoot = Path.Combine(repoRoot, "Infrastructure", "staging-client", "data", "common");
var sourceItems = Path.Combine(repoRoot, "Client2.3.7", "data", "common", "Items.d2o");
var stagingItems = Path.Combine(stagingRoot, "Items.d2o");
var reportPath = Path.Combine(
    repoRoot,
    "Infrastructure",
    "temporal-artifacts",
    "client-d2o-writer-research",
    "research-results.json");

Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
Directory.CreateDirectory(stagingRoot);

if (!File.Exists(sourceItems))
{
    throw new FileNotFoundException($"Items.d2o no encontrado: {sourceItems}");
}

File.Copy(sourceItems, stagingItems, overwrite: true);

var steps = new List<ResearchStepResult>();
var results = new ResearchResults(
    DateTimeOffset.UtcNow,
    sourceItems,
    stagingItems,
    steps);

steps.Add(RunStep("index_table_integrity", () =>
{
    var before = ReadD2oIndexIds(sourceItems);
    var after = ReadD2oIndexIds(stagingItems);
    var ok = before.SetEquals(after);
    return new StepOutcome(
        ok,
        $"sourceCount={before.Count} stagingCount={after.Count} contains7754={after.Contains(7754)} contains12617={after.Contains(12617)}");
}));

steps.Add(RunStep("file_copy_hash", () =>
{
    var hashSource = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourceItems)));
    var hashStaging = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(stagingItems)));
    var ok = string.Equals(hashSource, hashStaging, StringComparison.Ordinal);
    return new StepOutcome(ok, $"sha256Match={ok}");
}));

steps.Add(RunStep("sunshine_d2o_reader_open_items", () =>
{
    try
    {
        var reader = new D2OReader(stagingItems);
        var count = reader.IndexCount;
        reader.Close();
        return new StepOutcome(true, $"opened IndexCount={count} (class table loaded)");
    }
    catch (Exception ex)
    {
        return new StepOutcome(false, $"{ex.GetType().Name}: {ex.Message}");
    }
}));

steps.Add(RunStep("sunshine_d2o_reader_read_objects_items", () =>
{
    try
    {
        var reader = new D2OReader(stagingItems);
        var objects = reader.ReadObjects(allownulled: true);
        var nonNull = objects.Count(x => x.Value is not null);
        reader.Close();
        return new StepOutcome(true, $"readObjects count={objects.Count} nonNull={nonNull}");
    }
    catch (Exception ex)
    {
        return new StepOutcome(false, $"{ex.GetType().Name}: {ex.Message}");
    }
}));

steps.Add(RunStep("d2o_class_table_probe", () =>
{
    var classNames = ReadD2oClassNames(stagingItems);
    var hasItemClass = classNames.Any(x => string.Equals(x, "Item", StringComparison.Ordinal));
    return new StepOutcome(
        true,
        $"classes={classNames.Count} sample=[{string.Join(", ", classNames.Take(5))}] requiresItemClass={hasItemClass} sunshineTypedCoverage=Breed-only");
}));

steps.Add(RunStep("sunshine_d2owriter_roundtrip_items", () =>
{
    var roundTripPath = Path.Combine(stagingRoot, "Items.roundtrip.d2o");
    if (File.Exists(roundTripPath))
    {
        File.Delete(roundTripPath);
    }

    File.Copy(stagingItems, roundTripPath, overwrite: true);

    try
    {
        using (var writer = new D2OWriter(roundTripPath))
        {
            writer.StartWriting(backupFile: true);
            writer.EndWriting();
        }

        var before = ReadD2oIndexIds(stagingItems);
        var after = ReadD2oIndexIds(roundTripPath);
        var ok = before.SetEquals(after);
        return new StepOutcome(ok, $"roundTripPath={roundTripPath} indexPreserved={ok} before={before.Count} after={after.Count}");
    }
    catch (Exception ex)
    {
        return new StepOutcome(false, $"{ex.GetType().Name}: {ex.Message}");
    }
}));

steps.Add(RunStep("item_12617_publish_feasibility", () =>
{
    var ids = ReadD2oIndexIds(stagingItems);
    if (ids.Contains(12617))
    {
        return new StepOutcome(true, "12617 already present in staging Items.d2o index");
    }

    if (!ids.Contains(7754))
    {
        return new StepOutcome(false, "Template 7754 missing; cannot clone for 12617 PoC");
    }

    return new StepOutcome(
        false,
        "12617 absent. Sunshine D2OWriter requires typed Item class (not in repo). Phase 3 needs generic D2O editor or generated Item.cs.");
}));

var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
await File.WriteAllTextAsync(reportPath, json, Encoding.UTF8);

Console.WriteLine($"Research report: {reportPath}");
foreach (var step in steps)
{
    Console.WriteLine($"{step.Name}: {(step.Outcome.Success ? "PASS" : "FAIL")} — {step.Outcome.Detail}");
}

return steps.Any(x => x.Name == "sunshine_d2owriter_roundtrip_items" && x.Outcome.Success) ? 0 : 1;

static ResearchStepResult RunStep(string name, Func<StepOutcome> action)
{
    try
    {
        return new ResearchStepResult(name, action());
    }
    catch (Exception ex)
    {
        return new ResearchStepResult(name, new StepOutcome(false, $"{ex.GetType().Name}: {ex.Message}"));
    }
}

static List<string> ReadD2oClassNames(string path)
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
    stream.Position = headerOffset + 4 + indexLength;
    stream.ReadExactly(buffer);
    var classCount = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer);
    var names = new List<string>(classCount);
    for (var i = 0; i < classCount; i++)
    {
        stream.ReadExactly(buffer);
        _ = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer);
        var nameLen = stream.ReadByte() << 8 | stream.ReadByte();
        var nameBytes = new byte[nameLen];
        stream.ReadExactly(nameBytes);
        var className = Encoding.UTF8.GetString(nameBytes);
        names.Add(className);
        var pkgLen = stream.ReadByte() << 8 | stream.ReadByte();
        stream.Seek(pkgLen, SeekOrigin.Current);
        stream.ReadExactly(buffer);
        var fieldCount = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer);
        for (var f = 0; f < fieldCount; f++)
        {
            var fnLen = stream.ReadByte() << 8 | stream.ReadByte();
            stream.Seek(fnLen, SeekOrigin.Current);
            stream.ReadExactly(buffer);
            var fieldType = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer);
            if (fieldType == -99)
            {
                while (true)
                {
                    var vnLen = stream.ReadByte() << 8 | stream.ReadByte();
                    stream.Seek(vnLen, SeekOrigin.Current);
                    stream.ReadExactly(buffer);
                    var vt = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer);
                    if (vt != -99)
                    {
                        break;
                    }
                }
            }
        }
    }

    return names;
}

static HashSet<int> ReadD2oIndexIds(string path)
{
    using var stream = File.OpenRead(path);
    var header = new byte[3];
    stream.ReadExactly(header);
    if (!Encoding.ASCII.GetString(header).Equals("D2O", StringComparison.Ordinal))
    {
        throw new InvalidDataException("Invalid D2O header.");
    }

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
        var id = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer);
        stream.ReadExactly(buffer);
        ids.Add(id);
    }

    return ids;
}

internal sealed record StepOutcome(bool Success, string Detail);

internal sealed record ResearchStepResult(string Name, StepOutcome Outcome);

internal sealed record ResearchResults(
    DateTimeOffset ExecutedAtUtc,
    string SourceItemsPath,
    string StagingItemsPath,
    IReadOnlyList<ResearchStepResult> Steps);

internal static class RepositoryRootResolver
{
    public static string Resolve(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Angular-tools", "Admin"))
                && Directory.Exists(Path.Combine(directory.FullName, "docs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repo root not found.");
    }
}
