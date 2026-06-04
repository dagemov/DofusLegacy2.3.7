using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Rollback.Admin.Services;

public sealed class FfdecSpellLevelScriptExtractor
{
    private static readonly Regex SpellLevelEntryRegex = new(
        @"^\s*(?:this\.)?_datas\[(?<id>\d+)\]\s*=\s*SpellLevel\.create\((?<args>.+)\);\s*$",
        RegexOptions.Compiled);

    private readonly ClientDataPathResolver _pathResolver;

    public FfdecSpellLevelScriptExtractor(ClientDataPathResolver pathResolver) =>
        _pathResolver = pathResolver;

    public bool IsAvailable =>
        !string.IsNullOrWhiteSpace(_pathResolver.FfdecCliPath) &&
        _pathResolver.CommonDataDirectory is { Length: > 0 } commonDirectory &&
        Directory.Exists(commonDirectory);

    public bool TryExtractChunk(
        short chunkId,
        out Dictionary<int, SpellLevelScriptEntry> entries,
        out string sourceDescription)
    {
        entries = new Dictionary<int, SpellLevelScriptEntry>();
        sourceDescription = string.Empty;

        if (!IsAvailable)
        {
            sourceDescription = "FFDec no disponible para extraer SpellLevels*.swf";
            return false;
        }

        var commonDirectory = _pathResolver.EnsureCommonDataDirectory();
        var swfPath = Path.Combine(commonDirectory, $"SpellLevels{chunkId}.swf");
        if (!File.Exists(swfPath))
        {
            sourceDescription = $"No existe SpellLevels{chunkId}.swf";
            return false;
        }

        var workspace = _pathResolver.CreateTempWorkspace($"ffdec-spelllevels-{chunkId}");
        try
        {
            if (!TryExportChunkScript(chunkId, workspace, out var scriptPath, out sourceDescription))
            {
                return false;
            }

            foreach (var line in File.ReadLines(scriptPath, Encoding.UTF8))
            {
                var match = SpellLevelEntryRegex.Match(line);
                if (!match.Success)
                    continue;

                if (!int.TryParse(match.Groups["id"].Value, out var levelId) || levelId <= 0)
                    continue;

                var rawArguments = SplitArguments(match.Groups["args"].Value);
                entries[levelId] = new SpellLevelScriptEntry(levelId, line.Trim(), rawArguments);
            }

            sourceDescription = entries.Count > 0
                ? $"Extraido con FFDec desde SpellLevels{chunkId}.swf"
                : $"FFDec no encontro entradas parseables en SpellLevels{chunkId}.swf";
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
                // Keep leftovers only if cleanup fails.
            }
        }
    }

    public bool TryReadChunkScript(short chunkId, out string scriptContent, out string sourceDescription)
    {
        scriptContent = string.Empty;
        sourceDescription = string.Empty;

        if (!IsAvailable)
        {
            sourceDescription = "FFDec no disponible para extraer SpellLevels*.swf";
            return false;
        }

        var workspace = _pathResolver.CreateTempWorkspace($"ffdec-spelllevels-script-{chunkId}");
        try
        {
            if (!TryExportChunkScript(chunkId, workspace, out var scriptPath, out sourceDescription))
                return false;

            scriptContent = File.ReadAllText(scriptPath, Encoding.UTF8);
            sourceDescription = $"Script exportado desde SpellLevels{chunkId}.swf";
            return true;
        }
        finally
        {
            try
            {
                Directory.Delete(workspace, recursive: true);
            }
            catch
            {
                // Keep leftovers only if cleanup fails.
            }
        }
    }

    private bool TryExportChunkScript(short chunkId, string workspace, out string scriptPath, out string sourceDescription)
    {
        scriptPath = string.Empty;
        sourceDescription = string.Empty;

        var commonDirectory = _pathResolver.EnsureCommonDataDirectory();
        var swfPath = Path.Combine(commonDirectory, $"SpellLevels{chunkId}.swf");
        if (!File.Exists(swfPath))
        {
            sourceDescription = $"No existe SpellLevels{chunkId}.swf";
            return false;
        }

        RunFfdec(
            "-selectclass",
            $"SpellLevels{chunkId}",
            "-export",
            "script",
            workspace,
            swfPath);

        scriptPath = Path.Combine(workspace, "scripts", $"SpellLevels{chunkId}.as");
        if (File.Exists(scriptPath))
            return true;

        sourceDescription = $"FFDec no exporto SpellLevels{chunkId}.as";
        return false;
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
        var current = new StringBuilder();
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
                    case '{':
                        depth++;
                        current.Append(character);
                        continue;
                    case ')':
                    case ']':
                    case '>':
                    case '}':
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

    public sealed record SpellLevelScriptEntry(
        int LevelId,
        string RawLine,
        IReadOnlyList<string> RawArguments);
}
