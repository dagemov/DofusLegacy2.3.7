using System.Security.Cryptography;
using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.Items;
using Rollback.Common.Logging;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class ItemAppearanceResolverService
{
    private static readonly object BitmapHashIndexSyncRoot = new();
    private static string? _bitmapHashIndexRoot;
    private static Dictionary<string, List<int>>? _bitmapIdsByHash;
    private static readonly object SourceAssetHashIndexSyncRoot = new();
    private static readonly Dictionary<ItemType, (string Root, Dictionary<string, List<int>> Index)> SourceAssetHashIndices = new();

    private readonly ClientDataPathResolver _pathResolver;
    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly AdminEntityClientMetadataService _clientMetadataStore;
    private readonly ClientItemMetadataService _clientItemMetadataService;
    private readonly ReferenceItemCatalogService _referenceCatalogService;
    private readonly ItemAppearanceCatalogService _appearanceCatalogService;

    public ItemAppearanceResolverService(
        ClientDataPathResolver pathResolver,
        AdminDbConnectionFactory connectionFactory,
        AdminEntityClientMetadataService clientMetadataStore,
        ClientItemMetadataService clientItemMetadataService,
        ReferenceItemCatalogService referenceItemCatalogService,
        ItemAppearanceCatalogService appearanceCatalogService)
    {
        _pathResolver = pathResolver;
        _connectionFactory = connectionFactory;
        _clientMetadataStore = clientMetadataStore;
        _clientItemMetadataService = clientItemMetadataService;
        _referenceCatalogService = referenceItemCatalogService;
        _appearanceCatalogService = appearanceCatalogService;
    }

    public async Task<AppearanceResolutionResult?> ResolveAndFixIfInvalidAsync(
        ItemEditModel model,
        CancellationToken cancellationToken = default) =>
        await ResolveAndPersistAsync(model, overwriteExisting: false, cancellationToken);

    public async Task<AppearanceResolutionResult?> AnalyzeAsync(
        ItemEditModel model,
        CancellationToken cancellationToken = default) =>
        await ResolveAsync(model, ignoreCurrentAppearance: true, cancellationToken);

    public async Task<AppearanceResolutionResult?> ResolveAndPersistAsync(
        ItemEditModel model,
        bool overwriteExisting,
        CancellationToken cancellationToken = default)
    {
        if (!SupportsEquippedAppearance(model.TypeId))
            return null;

        var resolution = await AnalyzeAsync(model, cancellationToken);
        if (resolution?.AppearanceId is not > 0)
        {
            Logger.Instance.LogWarn(
                $"Could not resolve appearance for item {model.Id} (type {(short)model.TypeId}, currentAppearance {model.AppearanceId}).");
            return null;
        }

        if (!ShouldPersistResolution(model, resolution, overwriteExisting))
            return resolution;

        model.AppearanceId = resolution.AppearanceId;
        await PersistResolvedAppearanceAsync(model, resolution, cancellationToken);

        if (resolution.IsMismatch)
        {
            Logger.Instance.LogInfo(
                "Corrected suspicious appearance mismatch on item {0}: {1} -> {2} using strategy {3}.",
                model.Id,
                resolution.CurrentAppearanceId,
                resolution.AppearanceId,
                resolution.Strategy);
        }
        else
        {
            Logger.Instance.LogInfo(
                "Fixed appearance for item {0} -> {1} using strategy {2}.",
                model.Id,
                resolution.AppearanceId,
                resolution.Strategy);
        }

        return resolution;
    }

    public async Task<AppearanceResolutionResult?> ResolveAsync(
        ItemEditModel model,
        bool ignoreCurrentAppearance = false,
        CancellationToken cancellationToken = default)
    {
        if (!SupportsEquippedAppearance(model.TypeId))
            return null;

        var selfMetadata = _clientItemMetadataService.Get(model.Id);

        if (TryResolveFromReferenceIdentity(model, out var referenceMatch))
        {
            Logger.Instance.LogInfo(
                "Resolved appearanceId {0} for item {1} from reference identity (iconId {2}).",
                referenceMatch!.AppearanceId,
                model.Id,
                referenceMatch.SourceIconId ?? 0);

            return FinalizeResolution(model, referenceMatch!, ignoreCurrentAppearance);
        }

        if (TryResolveFromManualAssetHash(model, out var manualMatch))
        {
            Logger.Instance.LogInfo(
                "Resolved appearanceId {0} for item {1} from manual asset hash (source item {2}, iconId {3}).",
                manualMatch!.AppearanceId,
                model.Id,
                manualMatch.SourceItemId ?? 0,
                manualMatch.SourceIconId ?? 0);

            return FinalizeResolution(model, manualMatch!, ignoreCurrentAppearance);
        }

        if (TryResolveFromPublishedBitmapHash(model, selfMetadata, out var bitmapHashMatch))
        {
            Logger.Instance.LogInfo(
                "Resolved appearanceId {0} for item {1} from published bitmap hash (source item {2}, iconId {3}).",
                bitmapHashMatch!.AppearanceId,
                model.Id,
                bitmapHashMatch.SourceItemId ?? 0,
                bitmapHashMatch.SourceIconId ?? 0);

            return FinalizeResolution(model, bitmapHashMatch!, ignoreCurrentAppearance);
        }

        if (TryResolveFromKnownIcon(model, selfMetadata, out var iconMatch))
        {
            Logger.Instance.LogInfo(
                "Resolved appearanceId {0} for item {1} from shared icon metadata (source item {2}, iconId {3}).",
                iconMatch!.AppearanceId,
                model.Id,
                iconMatch.SourceItemId ?? 0,
                iconMatch.SourceIconId ?? 0);

            return FinalizeResolution(model, iconMatch!, ignoreCurrentAppearance);
        }

        if (selfMetadata.AppearanceId is > 0)
        {
            var result = new AppearanceResolutionResult
            {
                AppearanceId = selfMetadata.AppearanceId.Value,
                SourceItemId = model.Id,
                SourceIconId = selfMetadata.IconId,
                Strategy = "metadata-cliente-directa",
                Message = $"AppearanceId {selfMetadata.AppearanceId.Value} reutilizado desde la metadata cliente ya conocida para el item #{model.Id}.",
            };

            Logger.Instance.LogInfo(
                "Resolved appearanceId {0} for item {1} from client metadata (iconId {2}).",
                result.AppearanceId,
                model.Id,
                result.SourceIconId ?? 0);

            return FinalizeResolution(model, result, ignoreCurrentAppearance);
        }

        var suggestedAppearance = await _appearanceCatalogService.GetSuggestedAppearanceAsync(model.TypeId, cancellationToken);
        if (suggestedAppearance is not > 0)
        {
            Logger.Instance.LogWarn(
                $"Item {model.Id} has no valid appearance candidate. TypeId {(short)model.TypeId}, clientIconId {model.ClientIconId ?? 0}.");
            return null;
        }

        var fallback = new AppearanceResolutionResult
        {
            AppearanceId = suggestedAppearance.Value,
            Strategy = "fallback-por-tipo",
            Message = $"AppearanceId {suggestedAppearance.Value} asignado como fallback por tipo para {ItemTypeLabelService.GetDisplayName(model.TypeId)} porque no hubo una coincidencia mas precisa en cliente.",
        };

        Logger.Instance.LogWarn(
            $"Item {model.Id} resolved appearanceId {fallback.AppearanceId} using fallback by type {(short)model.TypeId}.");

        return FinalizeResolution(model, fallback, ignoreCurrentAppearance);
    }

    private bool TryResolveFromManualAssetHash(ItemEditModel model, out AppearanceResolutionResult? result)
    {
        result = null;

        var manualAssetPath = ResolveManualAssetPath(model.ManualAssetRelativePath);
        if (string.IsNullOrWhiteSpace(manualAssetPath))
            return false;

        var hash = ComputeSha256(manualAssetPath);
        if (string.IsNullOrWhiteSpace(hash))
            return false;

        var bitmapIdsByHash = GetBitmapIdsByHash();
        if (!bitmapIdsByHash.TryGetValue(hash, out var bitmapIds) || bitmapIds.Count == 0)
            return false;

        result = ResolveFromReferenceOrClientIconIds(
            model,
            bitmapIds,
            "hash-png-manual",
            $"AppearanceId {{0}} resuelto desde el PNG manual asociado al item #{model.Id}, reutilizando un bitmap cliente conocido ({string.Join("/", bitmapIds)}).");

        return result is not null;
    }

    private bool TryResolveFromPublishedBitmapHash(
        ItemEditModel model,
        AdminClientItemMetadata selfMetadata,
        out AppearanceResolutionResult? result)
    {
        result = null;

        var iconIds = new HashSet<int>();
        if (model.ClientIconId is > 0)
            iconIds.Add(model.ClientIconId.Value);

        if (selfMetadata.IconId is > 0)
            iconIds.Add(selfMetadata.IconId.Value);

        if (iconIds.Count == 0)
            return false;

        var bitmapDirectory = _pathResolver.ItemBitmapDirectory;
        if (string.IsNullOrWhiteSpace(bitmapDirectory) || !Directory.Exists(bitmapDirectory))
            return false;

        foreach (var iconId in iconIds)
        {
            var bitmapPath = Path.Combine(bitmapDirectory, $"{iconId}.png");
            if (!File.Exists(bitmapPath))
                continue;

            var hash = ComputeSha256(bitmapPath);
            if (string.IsNullOrWhiteSpace(hash))
                continue;

            var sourceIconIds = GetSourceAssetIconIdsByHash(model.TypeId, hash);
            if (sourceIconIds.Count == 0)
                continue;

            result = ResolveFromReferenceOrClientIconIds(
                model,
                sourceIconIds,
                "bitmap-publicado-compartido",
                $"AppearanceId {{0}} resuelto desde el bitmap publicado del item #{model.Id}, que coincide con assets cliente sanos ({string.Join("/", sourceIconIds)}).");

            if (result is not null)
                return true;
        }

        return false;
    }

    private bool TryResolveFromKnownIcon(
        ItemEditModel model,
        AdminClientItemMetadata selfMetadata,
        out AppearanceResolutionResult? result)
    {
        result = null;

        var iconIds = new HashSet<int>();
        if (model.ClientIconId is > 0)
            iconIds.Add(model.ClientIconId.Value);

        if (selfMetadata.IconId is > 0)
            iconIds.Add(selfMetadata.IconId.Value);

        if (iconIds.Count == 0)
            return false;

        result = ResolveFromReferenceOrClientIconIds(
            model,
            iconIds,
            "icono-cliente-compartido",
            $"AppearanceId {{0}} resuelto buscando otro item cliente del mismo tipo que comparte IconId {string.Join("/", iconIds)}.");

        return result is not null;
    }

    private bool TryResolveFromReferenceIdentity(
        ItemEditModel model,
        out AppearanceResolutionResult? result)
    {
        result = null;

        var reference = _referenceCatalogService.GetItem(model.Id);
        if (reference is null ||
            reference.AppearanceId is <= 0 ||
            reference.TypeId != (short)model.TypeId)
        {
            return false;
        }

        result = new AppearanceResolutionResult
        {
            AppearanceId = (short)reference.AppearanceId,
            SourceItemId = reference.ItemId,
            SourceIconId = reference.IconId,
            Strategy = "referencia-cliente-original",
            Message = $"AppearanceId {reference.AppearanceId} resuelto desde la referencia cliente original del item #{reference.ItemId}.",
        };

        return true;
    }

    private AppearanceResolutionResult? ResolveFromReferenceOrClientIconIds(
        ItemEditModel model,
        IEnumerable<int> iconIds,
        string strategy,
        string messageTemplate)
    {
        var iconIdSet = iconIds
            .Where(x => x > 0)
            .Distinct()
            .ToHashSet();

        if (iconIdSet.Count == 0)
            return null;

        var referenceCandidate = _referenceCatalogService.GetAllItems().Values
            .Where(x => x.ItemId > 0 &&
                        x.ItemId != model.Id &&
                        x.AppearanceId > 0 &&
                        x.IconId > 0 &&
                        iconIdSet.Contains(x.IconId) &&
                        x.TypeId == (short)model.TypeId)
            .OrderByDescending(x => x.AppearanceId)
            .ThenBy(x => x.ItemId)
            .FirstOrDefault();

        if (referenceCandidate is not null)
        {
            return new AppearanceResolutionResult
            {
                AppearanceId = (short)referenceCandidate.AppearanceId,
                SourceItemId = referenceCandidate.ItemId,
                SourceIconId = referenceCandidate.IconId,
                Strategy = strategy,
                Message = string.Format(messageTemplate, referenceCandidate.AppearanceId) +
                          $" Item fuente de referencia: #{referenceCandidate.ItemId} (IconId {referenceCandidate.IconId}).",
            };
        }

        var candidates = _clientItemMetadataService.GetAll()
            .Where(x => x.ItemId > 0 &&
                        x.ItemId != model.Id &&
                        x.AppearanceId is > 0 &&
                        x.IconId is > 0 &&
                        iconIdSet.Contains(x.IconId.Value) &&
                        x.TypeId == model.TypeId)
            .OrderByDescending(GetCandidateScore)
            .ThenBy(x => x.ItemId)
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var selected = candidates[0];
        return new AppearanceResolutionResult
        {
            AppearanceId = selected.AppearanceId!.Value,
            SourceItemId = selected.ItemId,
            SourceIconId = selected.IconId,
            Strategy = strategy,
            Message = string.Format(messageTemplate, selected.AppearanceId.Value) +
                      $" Item fuente: #{selected.ItemId} (IconId {selected.IconId}).",
        };
    }

    private int GetCandidateScore(AdminClientItemMetadata candidate)
    {
        var score = 0;

        if (_referenceCatalogService.GetItem(candidate.ItemId) is not null)
            score += 100;

        if (candidate.AppearanceId is > 0)
            score += 10;

        if (candidate.IconId is > 0)
            score += 5;

        return score;
    }

    private IReadOnlyDictionary<string, List<int>> GetBitmapIdsByHash()
    {
        var bitmapDirectory = _pathResolver.ItemBitmapDirectory;
        if (string.IsNullOrWhiteSpace(bitmapDirectory) || !Directory.Exists(bitmapDirectory))
            return new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        lock (BitmapHashIndexSyncRoot)
        {
            if (_bitmapIdsByHash is not null &&
                string.Equals(_bitmapHashIndexRoot, bitmapDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return _bitmapIdsByHash;
            }

            var index = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            foreach (var pngPath in Directory.EnumerateFiles(bitmapDirectory, "*.png", SearchOption.TopDirectoryOnly))
            {
                if (!int.TryParse(Path.GetFileNameWithoutExtension(pngPath), out var bitmapId) || bitmapId <= 0)
                    continue;

                var hash = ComputeSha256(pngPath);
                if (string.IsNullOrWhiteSpace(hash))
                    continue;

                if (!index.TryGetValue(hash, out var bitmapIds))
                {
                    bitmapIds = new List<int>();
                    index[hash] = bitmapIds;
                }

                bitmapIds.Add(bitmapId);
            }

            _bitmapHashIndexRoot = bitmapDirectory;
            _bitmapIdsByHash = index;
            return _bitmapIdsByHash;
        }
    }

    private IReadOnlyList<int> GetSourceAssetIconIdsByHash(ItemType itemType, string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return Array.Empty<int>();

        var sourceDirectory = ResolveSourceAssetDirectory(itemType);
        if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
            return Array.Empty<int>();

        lock (SourceAssetHashIndexSyncRoot)
        {
            if (!SourceAssetHashIndices.TryGetValue(itemType, out var cached) ||
                !string.Equals(cached.Root, sourceDirectory, StringComparison.OrdinalIgnoreCase))
            {
                cached = (sourceDirectory, BuildHashIndex(sourceDirectory));
                SourceAssetHashIndices[itemType] = cached;
            }

            return cached.Index.TryGetValue(hash, out var sourceIconIds)
                ? sourceIconIds
                : Array.Empty<int>();
        }
    }

    private static Dictionary<string, List<int>> BuildHashIndex(string directory)
    {
        var index = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pngPath in Directory.EnumerateFiles(directory, "*.png", SearchOption.TopDirectoryOnly))
        {
            if (!int.TryParse(Path.GetFileNameWithoutExtension(pngPath), out var sourceIconId) || sourceIconId <= 0)
                continue;

            var hash = ComputeSha256(pngPath);
            if (string.IsNullOrWhiteSpace(hash))
                continue;

            if (!index.TryGetValue(hash, out var iconIds))
            {
                iconIds = new List<int>();
                index[hash] = iconIds;
            }

            iconIds.Add(sourceIconId);
        }

        return index;
    }

    private string? ResolveSourceAssetDirectory(ItemType itemType) =>
        itemType switch
        {
            ItemType.Chapeau => ResolveClientItemAssetDirectory("sombreros"),
            ItemType.Cape => ResolveClientItemAssetDirectory("capas"),
            ItemType.Familier => ResolveClientItemAssetDirectory("mascotas"),
            _ => null,
        };

    private string? ResolveClientItemAssetDirectory(string leafDirectory)
    {
        var clientApplicationDirectory = _pathResolver.ClientApplicationDirectory;
        if (string.IsNullOrWhiteSpace(clientApplicationDirectory))
            return null;

        var candidate = Path.Combine(clientApplicationDirectory, "content", "gfx", "items", leafDirectory);
        return Directory.Exists(candidate)
            ? candidate
            : null;
    }

    private string? ResolveManualAssetPath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            string.IsNullOrWhiteSpace(_pathResolver.WebAdminAssetsRootDirectory) ||
            !Directory.Exists(_pathResolver.WebAdminAssetsRootDirectory))
        {
            return null;
        }

        var root = Path.GetFullPath(_pathResolver.WebAdminAssetsRootDirectory);
        var candidate = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(candidate))
            return null;

        return candidate;
    }

    private static string? ComputeSha256(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(stream);
            return Convert.ToHexString(hashBytes);
        }
        catch
        {
            return null;
        }
    }

    private static bool SupportsEquippedAppearance(ItemType itemType) =>
        itemType is ItemType.Chapeau or ItemType.Cape or ItemType.Familier;

    private AppearanceResolutionResult FinalizeResolution(
        ItemEditModel model,
        AppearanceResolutionResult resolution,
        bool ignoreCurrentAppearance)
    {
        var currentAppearanceId = model.AppearanceId;
        var isFallback = string.Equals(resolution.Strategy, "fallback-por-tipo", StringComparison.OrdinalIgnoreCase);
        var isMismatch = currentAppearanceId > 0 &&
                         resolution.AppearanceId > 0 &&
                         currentAppearanceId != resolution.AppearanceId;
        var needsCorrection = currentAppearanceId <= 0 ||
                              (isMismatch && !isFallback);

        if (!ignoreCurrentAppearance &&
            currentAppearanceId > 0 &&
            !needsCorrection)
        {
            Logger.Instance.LogInfo(
                "Item appearance resolver inspected item {0}: current appearance {1} already matches the active pipeline.",
                model.Id,
                currentAppearanceId);
        }

        return new AppearanceResolutionResult
        {
            AppearanceId = resolution.AppearanceId,
            CurrentAppearanceId = currentAppearanceId,
            SourceItemId = resolution.SourceItemId,
            SourceIconId = resolution.SourceIconId,
            Strategy = resolution.Strategy,
            Message = resolution.Message,
            IsFallback = isFallback,
            IsMismatch = isMismatch,
            NeedsCorrection = needsCorrection,
        };
    }

    private static bool ShouldPersistResolution(
        ItemEditModel model,
        AppearanceResolutionResult resolution,
        bool overwriteExisting) =>
        resolution.AppearanceId > 0 &&
        model.AppearanceId != resolution.AppearanceId &&
        (overwriteExisting || resolution.NeedsCorrection);

    private async Task PersistResolvedAppearanceAsync(
        ItemEditModel model,
        AppearanceResolutionResult resolution,
        CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await EnsureAppearanceBackupTableAsync(connection, transaction, cancellationToken);
        await BackupAppearanceSnapshotAsync(connection, transaction, model, resolution, cancellationToken);

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE items_templates
                SET AppearanceId = @appearanceId
                WHERE Id = @itemId;
                """;
            command.Parameters.AddWithValue("@appearanceId", model.AppearanceId);
            command.Parameters.AddWithValue("@itemId", model.Id);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        var existingMetadata = await _clientMetadataStore.GetAsync(AdminEntityType.Item, model.Id, cancellationToken: cancellationToken);
        var resolvedClientMetadata = _clientItemMetadataService.Get(model.Id);

        await AdminEntityClientMetadataService.SaveAsync(
            connection,
            (MySqlTransaction?)transaction,
            AdminEntityType.Item,
            model.Id,
            existingMetadata?.NameId ?? resolvedClientMetadata.NameId ?? model.ReferenceNameId ?? 0,
            existingMetadata?.DescriptionId ?? resolvedClientMetadata.DescriptionId ?? model.ReferenceDescriptionId ?? 0,
            existingMetadata?.IconId ?? resolvedClientMetadata.IconId ?? model.ClientIconId ?? 0,
            model.AppearanceId,
            cancellationToken: cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task EnsureAppearanceBackupTableAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS admin_item_appearance_autofix_phase1_backup (
                ItemId SMALLINT NOT NULL,
                TypeId SMALLINT NOT NULL,
                PreviousAppearanceId SMALLINT NOT NULL,
                ClientAppearanceId SMALLINT NOT NULL DEFAULT 0,
                ManualAssetRelativePath VARCHAR(255) NOT NULL DEFAULT '',
                ResolvedAppearanceId SMALLINT NOT NULL DEFAULT 0,
                SourceItemId SMALLINT NOT NULL DEFAULT 0,
                SourceIconId INT NOT NULL DEFAULT 0,
                ResolutionStrategy VARCHAR(64) NOT NULL DEFAULT '',
                CapturedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (ItemId)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task BackupAppearanceSnapshotAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        ItemEditModel model,
        AppearanceResolutionResult resolution,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO admin_item_appearance_autofix_phase1_backup
            (
                ItemId,
                TypeId,
                PreviousAppearanceId,
                ClientAppearanceId,
                ManualAssetRelativePath,
                ResolvedAppearanceId,
                SourceItemId,
                SourceIconId,
                ResolutionStrategy,
                CapturedAt
            )
            VALUES
            (
                @itemId,
                @typeId,
                @previousAppearanceId,
                @clientAppearanceId,
                @manualAssetRelativePath,
                @resolvedAppearanceId,
                @sourceItemId,
                @sourceIconId,
                @resolutionStrategy,
                UTC_TIMESTAMP()
            )
            ON DUPLICATE KEY UPDATE
                TypeId = VALUES(TypeId),
                PreviousAppearanceId = VALUES(PreviousAppearanceId),
                ClientAppearanceId = VALUES(ClientAppearanceId),
                ManualAssetRelativePath = VALUES(ManualAssetRelativePath),
                ResolvedAppearanceId = VALUES(ResolvedAppearanceId),
                SourceItemId = VALUES(SourceItemId),
                SourceIconId = VALUES(SourceIconId),
                ResolutionStrategy = VALUES(ResolutionStrategy),
                CapturedAt = VALUES(CapturedAt);
            """;
        command.Parameters.AddWithValue("@itemId", model.Id);
        command.Parameters.AddWithValue("@typeId", (short)model.TypeId);
        command.Parameters.AddWithValue("@previousAppearanceId", resolution.CurrentAppearanceId);
        command.Parameters.AddWithValue("@clientAppearanceId", model.ClientAppearanceId ?? 0);
        command.Parameters.AddWithValue("@manualAssetRelativePath", model.ManualAssetRelativePath ?? string.Empty);
        command.Parameters.AddWithValue("@resolvedAppearanceId", resolution.AppearanceId);
        command.Parameters.AddWithValue("@sourceItemId", resolution.SourceItemId ?? 0);
        command.Parameters.AddWithValue("@sourceIconId", resolution.SourceIconId ?? 0);
        command.Parameters.AddWithValue("@resolutionStrategy", resolution.Strategy ?? string.Empty);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
