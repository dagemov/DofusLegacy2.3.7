using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class ClientEffectDefinitionService
{
    private static readonly Regex EffectEntryRegex = new(
        @"^\s*(?:this\.)?_datas\[(?<id>\d+)\]\s*=\s*Effect\.create\((?<args>.+)\);\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly object SyncRoot = new();
    private static readonly ClientDataPathResolver PathResolver = new();
    private static Lazy<ClientEffectDefinitionState> _state = CreateState();

    private readonly ClientI18nTextService _i18nTextService = new();

    public string SourceDescription =>
        _state.Value.SourceDescription;

    public bool TryGet(EffectId effectId, out ClientEffectDefinition definition) =>
        TryGet((int)effectId, out definition);

    public bool TryGet(int effectId, out ClientEffectDefinition definition)
    {
        if (_state.Value.EntriesById.TryGetValue(effectId, out definition!))
            return true;

        definition = default!;
        return false;
    }

    public bool TryGetDescription(EffectId effectId, out string description)
    {
        description = string.Empty;
        return TryGet(effectId, out var definition) &&
               definition.DescriptionId > 0 &&
               _i18nTextService.TryGetText(definition.DescriptionId, out description);
    }

    public static void InvalidateCache()
    {
        lock (SyncRoot)
            _state = CreateState();
    }

    private static Lazy<ClientEffectDefinitionState> CreateState() =>
        new(LoadState, LazyThreadSafetyMode.ExecutionAndPublication);

    private static ClientEffectDefinitionState LoadState()
    {
        var entries = new Dictionary<int, ClientEffectDefinition>();

        if (string.IsNullOrWhiteSpace(PathResolver.FfdecCliPath))
            return new ClientEffectDefinitionState(entries, "FFDec no disponible para extraer Effects0.swf");

        var commonDirectory = PathResolver.CommonDataDirectory;
        if (string.IsNullOrWhiteSpace(commonDirectory) || !Directory.Exists(commonDirectory))
            return new ClientEffectDefinitionState(entries, "No se encontro client/app/data/common");

        var swfPath = Path.Combine(commonDirectory, "Effects0.swf");
        if (!File.Exists(swfPath))
            return new ClientEffectDefinitionState(entries, "No existe Effects0.swf");

        var workspace = PathResolver.CreateTempWorkspace("ffdec-effects");
        try
        {
            RunFfdec(
                PathResolver.EnsureFfdecCliPath(),
                "-selectclass",
                "Effects0",
                "-export",
                "script",
                workspace,
                swfPath);

            var scriptPath = Path.Combine(workspace, "scripts", "Effects0.as");
            if (!File.Exists(scriptPath))
                return new ClientEffectDefinitionState(entries, "FFDec no exporto Effects0.as");

            foreach (var line in File.ReadLines(scriptPath, Encoding.UTF8))
            {
                var match = EffectEntryRegex.Match(line);
                if (!match.Success || !int.TryParse(match.Groups["id"].Value, out var effectId) || effectId <= 0)
                    continue;

                var arguments = SplitArguments(match.Groups["args"].Value);
                if (arguments.Count < 7)
                    continue;

                if (!TryReadInt(arguments[1], out var descriptionId))
                    continue;

                entries[effectId] = new ClientEffectDefinition(
                    effectId,
                    descriptionId,
                    TryReadInt(arguments[2], out var iconId) ? iconId : 0,
                    TryReadInt(arguments[3], out var characteristic) ? characteristic : 0,
                    TryReadInt(arguments[4], out var category) ? category : 0,
                    ParseNullableString(arguments[5]),
                    TryReadBool(arguments[6], out var showInTooltip) && showInTooltip);
            }

            var sourceDescription = entries.Count > 0
                ? "Extraido automaticamente desde Effects0.swf"
                : "No se pudo reconstruir metadata de efectos desde Effects0.swf";

            return new ClientEffectDefinitionState(entries, sourceDescription);
        }
        finally
        {
            try
            {
                Directory.Delete(workspace, recursive: true);
            }
            catch
            {
                // Best effort cleanup.
            }
        }
    }

    private static void RunFfdec(string ffdecPath, params string[] arguments)
    {
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
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Select(text => text.Trim()));

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

    private static bool TryReadInt(string value, out int result) =>
        int.TryParse(value, out result);

    private static bool TryReadBool(string value, out bool result) =>
        bool.TryParse(value, out result);

    private static string? ParseNullableString(string value)
    {
        if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
            return null;

        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            return value[1..^1].Replace("\\\"", "\"");

        return value;
    }

    public sealed record ClientEffectDefinition(
        int EffectId,
        int DescriptionId,
        int IconId,
        int Characteristic,
        int Category,
        string? Operator,
        bool ShowInTooltip);

    private sealed class ClientEffectDefinitionState
    {
        public ClientEffectDefinitionState(
            Dictionary<int, ClientEffectDefinition> entriesById,
            string sourceDescription)
        {
            EntriesById = entriesById;
            SourceDescription = sourceDescription;
        }

        public Dictionary<int, ClientEffectDefinition> EntriesById { get; }

        public string SourceDescription { get; }
    }
}
