using System.Diagnostics;
using System.Text.RegularExpressions;
using Rollback.Admin.Models.Items;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class FfdecItemScriptExtractor
{
    private static readonly Regex ItemEntryRegex = new(
        @"^\s*_datas\[(?<id>\d+)\]\s*=\s*(?<factory>Item\.create|Weapon\.createWeapon)\((?<args>.+)\);\s*$",
        RegexOptions.Compiled);

    private readonly ClientDataPathResolver _pathResolver;

    public FfdecItemScriptExtractor(ClientDataPathResolver pathResolver) =>
        _pathResolver = pathResolver;

    public bool IsAvailable =>
        !string.IsNullOrWhiteSpace(_pathResolver.FfdecCliPath) &&
        _pathResolver.CommonDataDirectory is { Length: > 0 } commonDirectory &&
        Directory.Exists(commonDirectory);

    public bool TryExtractChunk(
        short chunkId,
        out Dictionary<short, AdminClientItemMetadata> entries,
        out string sourceDescription)
    {
        entries = new Dictionary<short, AdminClientItemMetadata>();
        sourceDescription = string.Empty;

        if (!IsAvailable)
        {
            sourceDescription = "FFDec no disponible para extraer Items*.swf";
            return false;
        }

        var commonDirectory = _pathResolver.EnsureCommonDataDirectory();
        var swfPath = Path.Combine(commonDirectory, $"Items{chunkId}.swf");
        if (!File.Exists(swfPath))
        {
            sourceDescription = $"No existe Items{chunkId}.swf";
            return false;
        }

        var workspace = _pathResolver.CreateTempWorkspace($"ffdec-items-{chunkId}");
        try
        {
            RunFfdec(
                "-selectclass",
                $"Items{chunkId}",
                "-export",
                "script",
                workspace,
                swfPath);

            var scriptPath = Path.Combine(workspace, "scripts", $"Items{chunkId}.as");
            if (!File.Exists(scriptPath))
            {
                sourceDescription = $"FFDec no exporto Items{chunkId}.as";
                return false;
            }

            foreach (var line in File.ReadLines(scriptPath))
            {
                var match = ItemEntryRegex.Match(line);
                if (!match.Success)
                    continue;

                var arguments = SplitArguments(match.Groups["args"].Value);
                if (arguments.Count < 18)
                    continue;

                if (!short.TryParse(match.Groups["id"].Value, out var itemId) || itemId <= 0)
                    continue;

                if (!TryParseInt(arguments.ElementAtOrDefault(1), out var nameId))
                    continue;

                if (!TryParseShort(arguments.ElementAtOrDefault(2), out var typeId))
                    continue;

                if (!TryParseInt(arguments.ElementAtOrDefault(3), out var descriptionId))
                    continue;

                if (!TryParseInt(arguments.ElementAtOrDefault(4), out var iconId))
                    continue;

                short? appearanceId = null;
                if (arguments.Count >= 17 && TryParseShort(arguments[16], out var parsedAppearanceId))
                    appearanceId = parsedAppearanceId;

                entries[itemId] = new AdminClientItemMetadata
                {
                    ItemId = itemId,
                    TypeId = Enum.IsDefined(typeof(ItemType), (int)typeId) ? (ItemType)typeId : null,
                    NameId = nameId,
                    DescriptionId = descriptionId,
                    IconId = iconId,
                    AppearanceId = appearanceId,
                };
            }

            sourceDescription = entries.Count > 0
                ? $"Extraido con FFDec desde Items{chunkId}.swf"
                : $"FFDec no encontro entradas parseables en Items{chunkId}.swf";
            return entries.Count > 0;
        }
        finally
        {
            try
            {
                Directory.Delete(workspace, recursive: true);
            }
            catch
            {
                // Keep temp leftovers only if cleanup fails.
            }
        }
    }

    private void RunFfdec(params string[] arguments)
    {
        var ffdecPath = _pathResolver.EnsureFfdecCliPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = ffdecPath,
            Arguments = BuildArguments(arguments),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("No se pudo iniciar FFDec.");
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0)
            return;

        var details = string.Join(
            Environment.NewLine,
            new[] { standardOutput, standardError }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim()));

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(details)
                ? $"FFDec fallo con codigo {process.ExitCode}."
                : $"FFDec fallo con codigo {process.ExitCode}:{Environment.NewLine}{details}");
    }

    private static string BuildArguments(IEnumerable<string> arguments) =>
        string.Join(" ", arguments.Select(QuoteArgument));

    private static string QuoteArgument(string value) =>
        value.Contains(' ') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\\\"")}\""
            : value;

    private static List<string> SplitArguments(string rawArguments)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var depth = 0;
        var inString = false;
        var escaped = false;

        foreach (var character in rawArguments)
        {
            if (escaped)
            {
                current.Append(character);
                escaped = false;
                continue;
            }

            if (character == '\\')
            {
                current.Append(character);
                escaped = true;
                continue;
            }

            if (character == '"')
            {
                current.Append(character);
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                switch (character)
                {
                    case '(':
                    case '[':
                    case '<':
                        depth++;
                        current.Append(character);
                        continue;
                    case ')':
                    case ']':
                    case '>':
                        depth--;
                        current.Append(character);
                        continue;
                    case ',' when depth == 0:
                        result.Add(current.ToString().Trim());
                        current.Clear();
                        continue;
                }
            }

            current.Append(character);
        }

        if (current.Length > 0)
            result.Add(current.ToString().Trim());

        return result;
    }

    private static bool TryParseInt(string? value, out int result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return int.TryParse(value.Trim(), out result);
    }

    private static bool TryParseShort(string? value, out short result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return short.TryParse(value.Trim(), out result);
    }
}
