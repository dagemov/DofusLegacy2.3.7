using Rollback.Admin.Models.Items;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public static class ItemAuditEvaluator
{
    public static string ResolveDisplayName(
        short itemId,
        string? overrideName,
        string? clientName,
        string? referenceName)
    {
        if (!string.IsNullOrWhiteSpace(overrideName))
            return overrideName.Trim();

        if (!string.IsNullOrWhiteSpace(clientName))
            return clientName.Trim();

        if (!string.IsNullOrWhiteSpace(referenceName))
            return referenceName.Trim();

        return $"Item #{itemId}";
    }

    public static string ResolveDescription(
        string? overrideDescription,
        string? clientDescription,
        string? referenceDescription) =>
        !string.IsNullOrWhiteSpace(overrideDescription)
            ? overrideDescription.Trim()
            : !string.IsNullOrWhiteSpace(clientDescription)
                ? clientDescription.Trim()
                : (referenceDescription ?? string.Empty).Trim();

    public static string ResolveIdentitySourceLabel(
        string? overrideName,
        string? overrideDescription,
        AdminClientItemText client,
        ReferenceItemIdentity? reference)
    {
        if (!string.IsNullOrWhiteSpace(overrideName) || !string.IsNullOrWhiteSpace(overrideDescription))
            return "Manual";

        if (!string.IsNullOrWhiteSpace(client.Name) || !string.IsNullOrWhiteSpace(client.Description))
            return "Cliente ES";

        if (reference is { HasResolvedText: true })
            return "Referencia";

        if (reference is not null)
            return "Referencia sin texto";

        return "Fallback";
    }

    public static ItemAuditSnapshot Build(ItemDiagnosticReport report)
    {
        var runtime = report.Runtime;
        var reference = report.Reference;
        var client = report.Client;
        var differences = new List<string>();

        var runtimeExists = runtime is not null;
        var hasReferenceIdentity = reference is not null;
        var hasClientMetadata = client.ItemId > 0 &&
                                (client.NameId.HasValue ||
                                 client.DescriptionId.HasValue ||
                                 client.IconId.HasValue ||
                                 client.ClientTypeId.HasValue);
        var hasDisplayName = !string.IsNullOrWhiteSpace(report.DisplayName) &&
                             !string.Equals(report.DisplayName, $"Item #{report.ItemId}", StringComparison.OrdinalIgnoreCase);
        var hasClientIcon = client.IconId is > 0;
        var hasManualAsset = !string.IsNullOrWhiteSpace(report.ManualAssetRelativePath);

        if (runtime is not null && reference is not null)
        {
            if ((short)runtime.TypeId != reference.TypeId)
                differences.Add($"TypeId runtime={(short)runtime.TypeId} vs referencia={reference.TypeId}");

            if (runtime.Level != reference.Level)
                differences.Add($"Level runtime={runtime.Level} vs referencia={reference.Level}");

            if (runtime.ItemSetId != reference.ItemSetId)
                differences.Add($"ItemSetId runtime={runtime.ItemSetId} vs referencia={reference.ItemSetId}");

            if (reference.AppearanceId > 0 && runtime.AppearanceId > 0 && runtime.AppearanceId != reference.AppearanceId)
                differences.Add($"AppearanceId runtime={runtime.AppearanceId} vs referencia={reference.AppearanceId}");
        }

        if (runtimeExists && !hasClientMetadata)
            differences.Add("El cliente actual no tiene datacenter conocido para este ItemId.");

        if (reference is not null && !reference.HasResolvedText)
            differences.Add("La referencia trae NameId/DescriptionId, pero los textos no se resolvieron de forma confiable con el i18n disponible.");

        if (!hasClientIcon)
            differences.Add(hasManualAsset
                ? "Hay PNG manual para el panel, pero el cliente no tiene IconId resoluble para inventario/vendor."
                : "No hay IconId cliente resoluble para preview inventario/vendor.");

        if (runtime is { TypeId: 0 })
            differences.Add("El template runtime tiene TypeId invalido.");

        if (runtime is { Level: <= 0 })
            differences.Add("El template runtime tiene Level <= 0.");

        var status = BuildStatus(runtimeExists, hasReferenceIdentity, hasClientMetadata, hasDisplayName, hasClientIcon, runtime, reference, differences);
        return new ItemAuditSnapshot
        {
            Status = status,
            StatusLabel = GetStatusLabel(status),
            Summary = BuildSummary(status, runtimeExists, hasReferenceIdentity, hasClientMetadata, hasDisplayName, hasClientIcon),
            IdentitySourceLabel = ResolveIdentitySourceLabel(report.OverrideName, report.OverrideDescription, client, reference),
            IsRuntimeAvailable = runtimeExists,
            HasReferenceIdentity = hasReferenceIdentity,
            HasClientMetadata = hasClientMetadata,
            HasDisplayName = hasDisplayName,
            HasClientIcon = hasClientIcon,
            HasManualAsset = hasManualAsset,
            IsLegacyRuntimeItem = status == ItemAuditStatus.LegacyRuntimeItem,
            Differences = differences,
        };
    }

    public static ItemClientVisibilitySnapshot BuildClientVisibility(ItemDiagnosticReport report, bool hasBitmapAsset)
    {
        var hasClientDefinition = report.Client.ItemId > 0 &&
                                  (report.Client.NameId.HasValue ||
                                   report.Client.DescriptionId.HasValue ||
                                   report.Client.IconId.HasValue ||
                                   report.Client.ClientTypeId.HasValue);
        var hasResolvedText = !string.IsNullOrWhiteSpace(report.Client.Name) ||
                              !string.IsNullOrWhiteSpace(report.Client.Description) ||
                              !string.IsNullOrWhiteSpace(report.OverrideName) ||
                              !string.IsNullOrWhiteSpace(report.OverrideDescription);
        var hasBitmapMapping = report.Client.IconId is > 0;
        var usesManualAdminAsset = !string.IsNullOrWhiteSpace(report.ManualAssetRelativePath);
        var details = new List<string>();

        details.Add(hasClientDefinition
            ? "El cliente actual conoce este ItemId en su datacenter de Items."
            : "El cliente actual no conoce este ItemId en Items*.swf. El runtime por si solo no alcanza.");

        details.Add(hasResolvedText
            ? "Hay texto visible resoluble para el panel o el cliente."
            : "No hay nombre/descripcion resolubles para mostrar el item con identidad legible.");

        details.Add(hasBitmapMapping
            ? $"El cliente resuelve IconId {report.Client.IconId}."
            : "No hay IconId cliente asociado a este ItemId.");

        details.Add(hasBitmapAsset
            ? "Existe bitmap local para el IconId resuelto."
            : usesManualAdminAsset
                ? "Solo hay PNG manual administrativo. Eso no hace visible el item dentro del cliente."
                : "No hay bitmap local resoluble para el cliente.");

        var isClientVisibleEnough = hasClientDefinition && hasBitmapMapping && hasBitmapAsset;
        var statusLabel = isClientVisibleEnough
            ? "Visible en cliente"
            : hasClientDefinition
                ? "Definicion cliente incompleta"
                : "Runtime-only no visible";
        var summary = isClientVisibleEnough
            ? "El cliente tiene definicion, IconId y bitmap local suficientes para reconocer este item."
            : hasClientDefinition
                ? "El cliente conoce el ItemId, pero falta alguna capa visual o textual para una identidad completa."
                : "El item existe en runtime/admin, pero el cliente no lo conoce en Items*.swf. No aparecera correctamente dentro del juego sin ampliar el datacenter cliente.";

        return new ItemClientVisibilitySnapshot
        {
            HasClientDefinition = hasClientDefinition,
            HasResolvedText = hasResolvedText,
            HasBitmapMapping = hasBitmapMapping,
            HasBitmapAsset = hasBitmapAsset,
            UsesManualAdminAsset = usesManualAdminAsset,
            IsClientVisibleEnough = isClientVisibleEnough,
            StatusLabel = statusLabel,
            Summary = summary,
            Details = details,
        };
    }

    public static string BuildIdGuidance(ItemDiagnosticReport report)
    {
        var itemId = report.ItemId;
        if (report.Runtime is not null)
            return $"El item #{itemId} ya existe en runtime. Guardar actualizara el template existente.";

        if (report.Client.ItemId > 0 &&
            (!string.IsNullOrWhiteSpace(report.Client.Name) || report.Client.IconId is > 0))
        {
            var displayName = ResolveDisplayName(itemId, report.OverrideName, report.Client.Name, report.Reference?.Name);
            return $"El Id #{itemId} no existe en runtime, pero el cliente ya lo conoce como {displayName}. Si lo reutilizas, el juego heredara esa identidad visual/textual.";
        }

        if (report.Reference is not null)
        {
            return $"El Id #{itemId} existe en la referencia, pero el cliente actual no lo conoce en Items*.swf. El template se guardara en DB, pero no sera visible correctamente dentro del juego sin ampliar el datacenter cliente.";
        }

        return $"El Id #{itemId} esta libre, pero no existe ni en runtime ni en el datacenter conocido del cliente. Crear este item solo lo deja en DB; no garantiza visibilidad real dentro del juego.";
    }

    private static ItemAuditStatus BuildStatus(
        bool runtimeExists,
        bool hasReferenceIdentity,
        bool hasClientMetadata,
        bool hasDisplayName,
        bool hasClientIcon,
        RuntimeItemIdentitySnapshot? runtime,
        ReferenceItemIdentity? reference,
        IReadOnlyCollection<string> differences)
    {
        if (!runtimeExists && (hasReferenceIdentity || hasClientMetadata))
            return ItemAuditStatus.MissingRuntime;

        if (!runtimeExists)
            return ItemAuditStatus.RuntimeOnly;

        if (runtime is { TypeId: 0 } || runtime is { Level: <= 0 })
            return ItemAuditStatus.IncompleteTemplate;

        if (!hasDisplayName)
            return ItemAuditStatus.MissingName;

        if (!hasClientMetadata)
            return hasReferenceIdentity
                ? ItemAuditStatus.MissingClientMetadata
                : ItemAuditStatus.RuntimeOnly;

        if (!hasClientIcon)
            return ItemAuditStatus.MissingIcon;

        if (!hasReferenceIdentity)
            return ItemAuditStatus.LegacyRuntimeItem;

        if (differences.Count > 0)
            return ItemAuditStatus.Ambiguous;

        return ItemAuditStatus.Aligned;
    }

    private static string GetStatusLabel(ItemAuditStatus status) =>
        status switch
        {
            ItemAuditStatus.Aligned => "Alineado",
            ItemAuditStatus.LegacyRuntimeItem => "Legacy runtime",
            ItemAuditStatus.MissingClientMetadata => "Falta metadata cliente",
            ItemAuditStatus.MissingReferenceMetadata => "Falta metadata referencia",
            ItemAuditStatus.MissingName => "Falta nombre",
            ItemAuditStatus.MissingIcon => "Falta icono",
            ItemAuditStatus.IncompleteTemplate => "Template incompleto",
            ItemAuditStatus.RuntimeOnly => "Solo runtime",
            ItemAuditStatus.MissingRuntime => "Falta runtime",
            _ => "Ambiguo",
        };

    private static string BuildSummary(
        ItemAuditStatus status,
        bool runtimeExists,
        bool hasReferenceIdentity,
        bool hasClientMetadata,
        bool hasDisplayName,
        bool hasClientIcon) =>
        status switch
        {
            ItemAuditStatus.Aligned => "Runtime, cliente y referencia convergen lo suficiente para editar sin ir a ciegas.",
            ItemAuditStatus.LegacyRuntimeItem => "El item vive en runtime y el cliente lo conoce, pero la referencia sana no lo cubre bien. Se conserva como legacy por Id.",
            ItemAuditStatus.MissingClientMetadata => "El item puede existir en DB o referencia, pero el cliente actual no tiene datacenter usable para resolverlo.",
            ItemAuditStatus.MissingReferenceMetadata => "El runtime vive, pero la referencia no aporta identidad suficiente para enriquecerlo.",
            ItemAuditStatus.MissingName => "El item no tiene un nombre visible confiable. Necesita override manual o mejor metadata.",
            ItemAuditStatus.MissingIcon => "El item no tiene icono cliente resoluble para inventario/vendors.",
            ItemAuditStatus.IncompleteTemplate => "El template runtime tiene campos minimos inconsistentes.",
            ItemAuditStatus.RuntimeOnly => runtimeExists
                ? "El item solo existe en runtime. No hay capa de identidad externa suficiente para el cliente actual."
                : "No hay runtime ni metadata cliente confiable para este Id.",
            ItemAuditStatus.MissingRuntime => "La referencia o el cliente conocen el item, pero el runtime actual no tiene template.",
            _ => BuildAmbiguousSummary(runtimeExists, hasReferenceIdentity, hasClientMetadata, hasDisplayName, hasClientIcon),
        };

    private static string BuildAmbiguousSummary(
        bool runtimeExists,
        bool hasReferenceIdentity,
        bool hasClientMetadata,
        bool hasDisplayName,
        bool hasClientIcon)
    {
        var parts = new List<string>();
        parts.Add(runtimeExists ? "runtime presente" : "runtime ausente");
        parts.Add(hasReferenceIdentity ? "referencia presente" : "sin referencia");
        parts.Add(hasClientMetadata ? "cliente presente" : "cliente ausente");
        if (!hasDisplayName)
            parts.Add("sin nombre visible");
        if (!hasClientIcon)
            parts.Add("sin icono cliente");

        return "El item mezcla fuentes inconsistentes: " + string.Join(", ", parts) + ".";
    }
}
