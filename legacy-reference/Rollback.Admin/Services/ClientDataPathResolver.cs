namespace Rollback.Admin.Services;

public sealed class ClientDataPathResolver
{
    private readonly Lazy<string?> _rollbackRoot;
    private readonly Lazy<string?> _workspaceRoot;

    public ClientDataPathResolver() =>
        (_rollbackRoot, _workspaceRoot) = (
            new Lazy<string?>(FindRollbackRoot, LazyThreadSafetyMode.ExecutionAndPublication),
            new Lazy<string?>(FindWorkspaceRoot, LazyThreadSafetyMode.ExecutionAndPublication));

    public string? RepoRoot =>
        _rollbackRoot.Value;

    public string? CommonDataDirectory =>
        CombineIfWorkspaceRoot("client", "app", "data", "common");

    public string? ClientApplicationDirectory =>
        CombineIfWorkspaceRoot("client", "app");

    public string? ClientUiDirectory =>
        CombineIfWorkspaceRoot("client", "app", "ui");

    public string? SpanishI18nDirectory =>
        CombineIfWorkspaceRoot("client", "app", "data", "i18n_es");

    public string? SpanishI18nTmpDirectory =>
        CombineIfWorkspaceRoot("client", "app", "data", "i18n_es", "tmp");

    public string? ItemBitmapDirectory =>
        CombineIfWorkspaceRoot("client", "app", "content", "gfx", "items", "bitmap");

    public string? WebAdminItemAssetsDirectory =>
        CombineIfRollbackRoot("Rollback.Web", "wwwroot", "admin-assets", "items");

    public string? WebAdminAssetsRootDirectory =>
        CombineIfRollbackRoot("Rollback.Web", "wwwroot", "admin-assets");

    public string? GeneratedItemMapPath =>
        CommonDataDirectory is { Length: > 0 } commonDirectory
            ? Path.Combine(commonDirectory, "item-client-map.generated.json")
            : null;

    public string? DofusSwfPath =>
        ClientApplicationDirectory is { Length: > 0 } appDirectory
            ? Path.Combine(appDirectory, "Dofus.swf")
            : null;

    public string? GameUiCoreSwfPath =>
        ClientUiDirectory is { Length: > 0 } uiDirectory
            ? Path.Combine(uiDirectory, "Ankama_GameUiCore", "GameUiCore.swf")
            : null;

    public string? FfdecCliPath
    {
        get
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "FFDec", "ffdec-cli.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "FFDec", "ffdec-cli.exe"),
            };

            return candidates.FirstOrDefault(File.Exists);
        }
    }

    public string EnsureRepoRoot() =>
        RepoRoot ?? throw new InvalidOperationException("No se pudo localizar la raiz del repo de Rollback.");

    public string EnsureCommonDataDirectory() =>
        CommonDataDirectory is { Length: > 0 } path && Directory.Exists(path)
            ? path
            : throw new InvalidOperationException("No se encontro client/app/data/common.");

    public string EnsureClientApplicationDirectory() =>
        ClientApplicationDirectory is { Length: > 0 } path && Directory.Exists(path)
            ? path
            : throw new InvalidOperationException("No se encontro client/app.");

    public string EnsureClientUiDirectory() =>
        ClientUiDirectory is { Length: > 0 } path && Directory.Exists(path)
            ? path
            : throw new InvalidOperationException("No se encontro client/app/ui.");

    public string EnsureSpanishI18nDirectory() =>
        SpanishI18nDirectory is { Length: > 0 } path && Directory.Exists(path)
            ? path
            : throw new InvalidOperationException("No se encontro client/app/data/i18n_es.");

    public string EnsureSpanishI18nTmpDirectory() =>
        SpanishI18nTmpDirectory is { Length: > 0 } path && Directory.Exists(path)
            ? path
            : throw new InvalidOperationException("No se encontro client/app/data/i18n_es/tmp.");

    public string EnsureItemBitmapDirectory()
    {
        var path = ItemBitmapDirectory ?? throw new InvalidOperationException("No se pudo resolver client/app/content/gfx/items/bitmap.");
        Directory.CreateDirectory(path);
        return path;
    }

    public string EnsureWebAdminItemAssetsDirectory() =>
        WebAdminItemAssetsDirectory is { Length: > 0 } path && Directory.Exists(path)
            ? path
            : throw new InvalidOperationException("No se encontro Rollback.Web/wwwroot/admin-assets/items.");

    public string EnsureWebAdminAssetsRootDirectory() =>
        WebAdminAssetsRootDirectory is { Length: > 0 } path && Directory.Exists(path)
            ? path
            : throw new InvalidOperationException("No se encontro Rollback.Web/wwwroot/admin-assets.");

    public string EnsureFfdecCliPath() =>
        FfdecCliPath ?? throw new InvalidOperationException("No se encontro ffdec-cli.exe. Instala FFDec para publicar items en el cliente.");

    public string EnsureDofusSwfPath() =>
        DofusSwfPath is { Length: > 0 } path && File.Exists(path)
            ? path
            : throw new InvalidOperationException("No se encontro client/app/Dofus.swf.");

    public string EnsureGameUiCoreSwfPath() =>
        GameUiCoreSwfPath is { Length: > 0 } path && File.Exists(path)
            ? path
            : throw new InvalidOperationException("No se encontro client/app/ui/Ankama_GameUiCore/GameUiCore.swf.");

    public string CreateTempWorkspace(string prefix)
    {
        var repoRoot = EnsureRepoRoot();
        var workspace = Path.Combine(repoRoot, "tmp", $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspace);
        return workspace;
    }

    private string? CombineIfRollbackRoot(params string[] segments) =>
        RepoRoot is { Length: > 0 } rollbackRoot
            ? Path.Combine(new[] { rollbackRoot }.Concat(segments).ToArray())
            : null;

    private string? CombineIfWorkspaceRoot(params string[] segments) =>
        _workspaceRoot.Value is { Length: > 0 } workspaceRoot
            ? Path.Combine(new[] { workspaceRoot }.Concat(segments).ToArray())
            : null;

    private static string? FindRollbackRoot()
    {
        foreach (var startPath in EnumerateRootCandidates())
        {
            var current = new DirectoryInfo(startPath);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "Rollback.sln")))
                    return current.FullName;

                var nestedRollbackDirectory = Path.Combine(current.FullName, "Rollback");
                if (File.Exists(Path.Combine(nestedRollbackDirectory, "Rollback.sln")))
                    return nestedRollbackDirectory;

                current = current.Parent;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateRootCandidates()
    {
        var envRepoRoot = Environment.GetEnvironmentVariable("ROLLBACK_REPO_ROOT");
        if (!string.IsNullOrWhiteSpace(envRepoRoot))
            yield return envRepoRoot;

        if (!string.IsNullOrWhiteSpace(AppContext.BaseDirectory))
            yield return AppContext.BaseDirectory;

        if (!string.IsNullOrWhiteSpace(Environment.CurrentDirectory) &&
            !string.Equals(AppContext.BaseDirectory, Environment.CurrentDirectory, StringComparison.OrdinalIgnoreCase))
        {
            yield return Environment.CurrentDirectory;
        }
    }

    private string? FindWorkspaceRoot()
    {
        var rollbackRoot = RepoRoot;
        if (string.IsNullOrWhiteSpace(rollbackRoot))
            return null;

        var rollbackDirectory = new DirectoryInfo(rollbackRoot);
        if (Directory.Exists(Path.Combine(rollbackDirectory.FullName, "client")))
            return rollbackDirectory.FullName;

        if (rollbackDirectory.Parent is not null &&
            Directory.Exists(Path.Combine(rollbackDirectory.Parent.FullName, "client")))
        {
            return rollbackDirectory.Parent.FullName;
        }

        return rollbackDirectory.FullName;
    }
}
