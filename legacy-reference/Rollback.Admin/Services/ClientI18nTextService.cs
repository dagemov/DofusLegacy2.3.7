using System.Text;
using System.Text.RegularExpressions;

namespace Rollback.Admin.Services;

public sealed class ClientI18nTextService
{
    private static readonly Regex TextEntryRegex = new(
        "_datas\\[(?<id>\\d+)\\]\\s*=\\s*\"(?<text>(?:\\\\.|[^\"])*)\";",
        RegexOptions.Compiled);

    private static readonly object SyncRoot = new();
    private static Lazy<ClientI18nState> _state = CreateState();

    public bool HasSpanishPack =>
        _state.Value.HasSpanishPack;

    public bool TryGetText(int id, out string text) =>
        _state.Value.TextsById.TryGetValue(id, out text!);

    public static void InvalidateCache()
    {
        lock (SyncRoot)
            _state = CreateState();
    }

    private static Lazy<ClientI18nState> CreateState() =>
        new(LoadState, LazyThreadSafetyMode.ExecutionAndPublication);

    private static ClientI18nState LoadState()
    {
        var textsById = new Dictionary<int, string>();
        var tmpDirectory = FindSpanishTmpDirectory();
        if (tmpDirectory is null)
            return new ClientI18nState(textsById);

        foreach (var file in Directory.EnumerateFiles(tmpDirectory, "i18n*.as", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var contents = File.ReadAllText(file, Encoding.UTF8);
                foreach (Match match in TextEntryRegex.Matches(contents))
                {
                    if (!int.TryParse(match.Groups["id"].Value, out var id))
                        continue;

                    textsById[id] = DecodeText(match.Groups["text"].Value);
                }
            }
            catch
            {
                // Optional local client pack.
            }
        }

        return new ClientI18nState(textsById);
    }

    private static string DecodeText(string input) =>
        input.Replace("\\\"", "\"")
             .Replace("\\n", "\n")
             .Replace("\\r", "\r")
             .Replace("\\t", "\t")
             .Replace("\\\\", "\\")
             .Trim();

    private static string? FindSpanishTmpDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "client", "app", "data", "i18n_es", "tmp");
            if (Directory.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        return null;
    }

    private sealed class ClientI18nState
    {
        public ClientI18nState(Dictionary<int, string> textsById)
        {
            TextsById = textsById;
            HasSpanishPack = textsById.Count > 0;
        }

        public Dictionary<int, string> TextsById { get; }

        public bool HasSpanishPack { get; }
    }
}
