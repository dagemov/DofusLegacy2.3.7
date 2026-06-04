using System.Text;
using RollblackLegacy.Admin.Application.Abstractions.ClientIdentity;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Services.Items;

public sealed class ItemPublicationManifestService : IItemPublicationManifestService
{
    private const int DefaultDofusSourceTemplateItemId = 7754;

    private readonly IItemsAdminReadService _itemsAdminReadService;
    private readonly IClientItemIdentityReadService _clientItemIdentityReadService;
    private readonly IItemClientPublicationInspector _publicationInspector;

    public ItemPublicationManifestService(
        IItemsAdminReadService itemsAdminReadService,
        IClientItemIdentityReadService clientItemIdentityReadService,
        IItemClientPublicationInspector publicationInspector)
    {
        _itemsAdminReadService = itemsAdminReadService;
        _clientItemIdentityReadService = clientItemIdentityReadService;
        _publicationInspector = publicationInspector;
    }

    public async Task<ItemPublicationManifestDto> GetManifestAsync(int itemId, CancellationToken cancellationToken = default)
    {
        var detail = await _itemsAdminReadService.GetItemAsync(itemId, cancellationToken);
        var identity = await _clientItemIdentityReadService.GetItemAsync(itemId, cancellationToken);
        var inspection = await _publicationInspector.InspectAsync(itemId, detail.TypeId, cancellationToken);

        var clientKnown = inspection.TemplateKnown && identity.ClientKnown;
        var states = new List<string>();
        var blockingReasons = new List<string>();
        var requiredActions = new List<string>();
        var filesToPatch = new List<string>();
        var risks = BuildDefaultRisks();

        var nameEs = FirstNonEmpty(identity.ClientNameEs.Text, detail.ResolvedName, identity.DbName);
        var nameEn = FirstNonEmpty(identity.ClientNameEn.Text, identity.DbName);
        var typeName = FirstNonEmpty(identity.ClientTypeNameEs, detail.TypeName);
        var effectsSummary = BuildEffectsSummary(detail.Effects);
        var sourceTemplateItemId = ResolveSourceTemplateItemId(clientKnown, detail.TypeId);

        if (!inspection.ClientDataAvailable)
        {
            states.Add(ItemPublicationManifestStates.BlockedManualReview);
            blockingReasons.Add(inspection.FailureReason ?? "Client metadata is unavailable for publication audit.");
            requiredActions.Add("Restore read-only access to Client2.3.7 metadata paths.");
        }
        else if (clientKnown)
        {
            states.Add(ItemPublicationManifestStates.ReadyToStage);
            blockingReasons.Add("El template ya existe en Items.d2o; no requiere publicación automática.");
            requiredActions.Add("Validar QA runtime; omitir patch de cliente salvo cambio de identidad.");
        }
        else
        {
            states.Add(ItemPublicationManifestStates.BlockedClientWriterMissing);
            states.Add(ItemPublicationManifestStates.BlockedI18nWriterMissing);
            blockingReasons.Add("Sunshine.Protocol incluye D2OWriter sin clase Item tipada ni pipeline validado para Items.d2o.");
            blockingReasons.Add("No existe D2I writer documentado en el repo para i18n_es.d2i / i18n_en.d2i.");
            requiredActions.Add($"Añadir template {itemId} en Items.d2o (staging copy, no Client2.3.7 original).");
            requiredActions.Add("Asignar nameId/descriptionId y textos ES/EN en i18n_es.d2i e i18n_en.d2i.");
            requiredActions.Add("Regenerar lane de launcher/patch (data.meta, VerInfo.rec) tras Phase 2+.");

            filesToPatch.Add("data/common/Items.d2o");
            filesToPatch.Add("data/i18n/i18n_es.d2i");
            filesToPatch.Add("data/i18n/i18n_en.d2i");

            if (!inspection.TypeKnown)
            {
                states.Add(ItemPublicationManifestStates.BlockedUnknownType);
                blockingReasons.Add($"TypeId {detail.TypeId} no resuelve en ItemTypes.d2o del cliente.");
                filesToPatch.Add("data/common/ItemTypes.d2o");
            }

            if (detail.IconId <= 0)
            {
                states.Add(ItemPublicationManifestStates.BlockedInvalidIcon);
                blockingReasons.Add("IconId en DB es cero o inválido.");
            }
            else if (!identity.IconPreviewFound && !IsPreviewOperational(detail.PreviewState))
            {
                states.Add(ItemPublicationManifestStates.BlockedInvalidIcon);
                blockingReasons.Add($"IconId {detail.IconId} sin preview curado ni verificación D2P en este manifiesto.");
                filesToPatch.Add("content/gfx/items/bitmap0.d2p");
                filesToPatch.Add("content/gfx/items/bitmap1.d2p");
                requiredActions.Add($"Verificar que IconId {detail.IconId} exista en bitmap*.d2p o importar icono a staging.");
            }

            if (!identity.DescriptionEs.Exists || !identity.DescriptionEn.Exists)
            {
                states.Add(ItemPublicationManifestStates.BlockedI18nWriterMissing);
                blockingReasons.Add($"DescriptionId {detail.DescriptionId} no resuelve en i18n ES/EN para el nombre de item en cliente.");
            }

            if (sourceTemplateItemId.HasValue)
            {
                requiredActions.Add($"Clonar campos de referencia desde template cliente {sourceTemplateItemId} (solo staging).");
            }

            states.Add(ItemPublicationManifestStates.BlockedManualReview);
            blockingReasons.Add("Phase 1 solo permite dry-run; la publicación automática está deshabilitada.");
        }

        if (detail.AppearanceId > 0 && identity.Appearance.Exists == false)
        {
            if (!states.Contains(ItemPublicationManifestStates.BlockedManualReview, StringComparer.Ordinal))
            {
                states.Add(ItemPublicationManifestStates.BlockedManualReview);
            }

            blockingReasons.Add($"AppearanceId {detail.AppearanceId} no existe en Appearances.d2o.");
            filesToPatch.Add("data/common/Appearances.d2o");
        }

        var primaryState = ResolvePrimaryState(clientKnown, states);
        var stagingPath = $"Infrastructure/temporal-artifacts/client-item-publication/{itemId}";

        return new ItemPublicationManifestDto(
            detail.ItemId,
            detail.ItemId,
            nameEs,
            nameEn,
            detail.DescriptionId,
            detail.TypeId,
            typeName,
            detail.IconId,
            detail.AppearanceId,
            effectsSummary,
            detail.Criteria,
            sourceTemplateItemId,
            clientKnown,
            primaryState,
            states.Distinct(StringComparer.Ordinal).ToArray(),
            requiredActions.Distinct(StringComparer.Ordinal).ToArray(),
            filesToPatch.Distinct(StringComparer.Ordinal).ToArray(),
            risks,
            CanPublishAutomatically: false,
            blockingReasons.Distinct(StringComparer.Ordinal).ToArray(),
            inspection.ClientRootPath,
            stagingPath,
            DateTimeOffset.UtcNow);
    }

    private static string ResolvePrimaryState(bool clientKnown, IReadOnlyList<string> states)
    {
        if (clientKnown)
        {
            return ItemPublicationManifestStates.ReadyToStage;
        }

        if (states.Contains(ItemPublicationManifestStates.BlockedUnknownType, StringComparer.Ordinal))
        {
            return ItemPublicationManifestStates.BlockedUnknownType;
        }

        if (states.Contains(ItemPublicationManifestStates.BlockedInvalidIcon, StringComparer.Ordinal))
        {
            return ItemPublicationManifestStates.BlockedInvalidIcon;
        }

        if (states.Contains(ItemPublicationManifestStates.BlockedClientWriterMissing, StringComparer.Ordinal))
        {
            return ItemPublicationManifestStates.BlockedClientWriterMissing;
        }

        if (states.Contains(ItemPublicationManifestStates.BlockedI18nWriterMissing, StringComparer.Ordinal))
        {
            return ItemPublicationManifestStates.BlockedI18nWriterMissing;
        }

        if (states.Contains(ItemPublicationManifestStates.BlockedManualReview, StringComparer.Ordinal))
        {
            return ItemPublicationManifestStates.BlockedManualReview;
        }

        return ItemPublicationManifestStates.BlockedManualReview;
    }

    private static int? ResolveSourceTemplateItemId(bool clientKnown, int typeId)
    {
        if (clientKnown)
        {
            return null;
        }

        return typeId == 23 ? DefaultDofusSourceTemplateItemId : null;
    }

    private static bool IsPreviewOperational(ItemPreviewStateDto previewState) =>
        string.Equals(previewState.State, "FOUND", StringComparison.OrdinalIgnoreCase)
        || string.Equals(previewState.State, "CURATED", StringComparison.OrdinalIgnoreCase);

    private static string BuildEffectsSummary(IReadOnlyList<ItemEffectDto> effects)
    {
        if (effects.Count == 0)
        {
            return "(sin efectos en DB)";
        }

        var builder = new StringBuilder();
        for (var index = 0; index < effects.Count; index++)
        {
            if (index > 0)
            {
                builder.Append("; ");
            }

            var effect = effects[index];
            builder.Append(effect.EffectId);
            builder.Append(' ');
            builder.Append(effect.Description);
            builder.Append(" [");
            builder.Append(effect.Value);
            builder.Append('/');
            builder.Append(effect.DiceNum);
            builder.Append('/');
            builder.Append(effect.DiceSide);
            builder.Append(']');
        }

        return builder.ToString();
    }

    private static IReadOnlyList<string> BuildDefaultRisks() =>
    [
        "Publicar Items.d2o sin i18n puede dejar tooltips vacíos.",
        "Parchear solo archivos locales sin launcher deja clientes QA desactualizados.",
        "Sobrescribir Client2.3.7 original rompe la línea base del workspace.",
        "Phase 1 no ejecuta escritura; validar siempre en copia staging."
    ];

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
