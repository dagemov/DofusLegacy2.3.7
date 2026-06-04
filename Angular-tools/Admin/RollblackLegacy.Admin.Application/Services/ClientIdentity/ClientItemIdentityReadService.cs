using RollblackLegacy.Admin.Application.Abstractions.ClientIdentity;
using RollblackLegacy.Admin.Application.ClientIdentity;
using RollblackLegacy.Admin.Application.Exceptions;
using RollblackLegacy.Admin.Application.Models.ClientIdentity;
using RollblackLegacy.Admin.Contracts.ClientIdentity;

namespace RollblackLegacy.Admin.Application.Services.ClientIdentity;

public sealed class ClientItemIdentityReadService : IClientItemIdentityReadService
{
    private readonly IClientItemIdentityRepository _repository;
    private readonly IClientItemSourceReader _sourceReader;

    public ClientItemIdentityReadService(
        IClientItemIdentityRepository repository,
        IClientItemSourceReader sourceReader)
    {
        _repository = repository;
        _sourceReader = sourceReader;
    }

    public async Task<ClientItemIdentityCheckResultDto> GetItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        ClientItemIdentityIdParser.EnsureWithinBatchLimit([itemId]);
        EnsurePositiveIds([itemId]);

        var dbItem = (await _repository.GetItemsAsync([itemId], cancellationToken)).SingleOrDefault();
        if (dbItem is null)
        {
            throw new AdminEntityNotFoundException("item", itemId.ToString());
        }

        return await BuildAsync(dbItem, cancellationToken);
    }

    public async Task<IReadOnlyList<ClientItemIdentityCheckResultDto>> CheckAsync(ClientItemIdentityCheckRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ItemIds.Count == 0)
        {
            throw new AdminValidationException(
                "No item ids were provided for the client identity audit.",
                new Dictionary<string, string[]>
                {
                    ["ids"] = ["At least one positive item id is required."]
                });
        }

        ClientItemIdentityIdParser.EnsureWithinBatchLimit(request.ItemIds);
        EnsurePositiveIds(request.ItemIds);

        var orderedIds = request.ItemIds.Distinct().ToArray();
        var dbItems = await _repository.GetItemsAsync(orderedIds, cancellationToken);
        var byId = dbItems.ToDictionary(x => x.ItemId);
        var results = new List<ClientItemIdentityCheckResultDto>(orderedIds.Length);

        foreach (var itemId in orderedIds)
        {
            if (!byId.TryGetValue(itemId, out var dbItem))
            {
                throw new AdminEntityNotFoundException("item", itemId.ToString());
            }

            results.Add(await BuildAsync(dbItem, cancellationToken));
        }

        return results;
    }

    private async Task<ClientItemIdentityCheckResultDto> BuildAsync(ClientItemDbSnapshot dbItem, CancellationToken cancellationToken)
    {
        var source = await _sourceReader.ReadAsync(dbItem, cancellationToken);

        var statuses = new List<string>();
        if (!source.ClientDataAvailable)
        {
            statuses.Add("CLIENT_DATA_UNAVAILABLE");
        }
        else if (source.ClientKnown)
        {
            statuses.Add("SAFE_EXISTING_TEMPLATE");
            statuses.Add("CLIENT_KNOWN");
        }
        else
        {
            statuses.Add("CLIENT_UNKNOWN");
            statuses.Add("NEEDS_CLIENT_PATCH");
        }

        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(source.DbDescriptionEs))
        {
            statuses.Add("I18N_MISSING_ES");
            warnings.Add("DescriptionId DB no resolvio texto en i18n_es.d2i.");
        }

        if (string.IsNullOrWhiteSpace(source.DbDescriptionEn))
        {
            statuses.Add("I18N_MISSING_EN");
            warnings.Add("DescriptionId DB no resolvio texto en i18n_en.d2i.");
        }

        if (dbItem.IconId <= 0)
        {
            statuses.Add("ICON_MISSING");
            warnings.Add("El item no tiene IconId usable en DB.");
        }
        else if (source.IconPreviewFound)
        {
            statuses.Add("ICON_PREVIEW_FOUND");
        }
        else
        {
            statuses.Add("ICON_PREVIEW_MISSING");
            warnings.Add("No hay preview curado por item ni por icono.");
        }

        if (dbItem.AppearanceId > 0 && source.AppearanceKnown == false)
        {
            statuses.Add("APPEARANCE_UNKNOWN");
            warnings.Add("AppearanceId > 0 no existe en Appearances.d2o.");
        }

        if (!source.ClientDataAvailable && !string.IsNullOrWhiteSpace(source.FailureReason))
        {
            warnings.Add(source.FailureReason);
        }

        var primaryStatus = !source.ClientDataAvailable
            ? "CLIENT_DATA_UNAVAILABLE"
            : statuses.Contains("CLIENT_UNKNOWN", StringComparer.Ordinal)
                ? "CLIENT_UNKNOWN"
                : statuses.Contains("I18N_MISSING_ES", StringComparer.Ordinal) || statuses.Contains("I18N_MISSING_EN", StringComparer.Ordinal)
                    ? "NEEDS_CLIENT_PATCH"
                    : "SAFE_EXISTING_TEMPLATE";

        var recommendedAction = !source.ClientDataAvailable
            ? "Restaurar acceso read-only al metadata del cliente antes de confiar en la auditoria de identidad."
            : source.ClientKnown
                ? "Seguir con QA runtime; el template ya existe en cliente."
                : $"Publicar el template {dbItem.ItemId} en Items.d2o y alinear i18n antes de declararlo visible.";

        return new ClientItemIdentityCheckResultDto(
            dbItem.ItemId,
            dbItem.Name,
            dbItem.DescriptionId,
            source.ClientDescriptionId,
            source.ClientNameId,
            source.ClientKnown,
            new ClientItemIdentityStatusDto(
                primaryStatus,
                source.ClientKnown,
                NeedsClientPatch: source.ClientDataAvailable && !source.ClientKnown,
                statuses.Distinct(StringComparer.Ordinal).ToArray(),
                warnings,
                recommendedAction),
            new ClientItemI18nResolutionDto("es", dbItem.DescriptionId, !string.IsNullOrWhiteSpace(source.DbDescriptionEs), source.DbDescriptionEs, source.I18nEsPath),
            new ClientItemI18nResolutionDto("en", dbItem.DescriptionId, !string.IsNullOrWhiteSpace(source.DbDescriptionEn), source.DbDescriptionEn, source.I18nEnPath),
            new ClientItemI18nResolutionDto("es", source.ClientNameId, !string.IsNullOrWhiteSpace(source.ClientNameEs), source.ClientNameEs, source.I18nEsPath),
            new ClientItemI18nResolutionDto("en", source.ClientNameId, !string.IsNullOrWhiteSpace(source.ClientNameEn), source.ClientNameEn, source.I18nEnPath),
            dbItem.TypeId,
            source.ClientTypeId,
            source.ClientTypeNameEs,
            source.ClientTypeNameEn,
            NormalizeOptional(dbItem.ItemSetId),
            NormalizeOptional(source.ClientSetId),
            source.ClientSetNameEs,
            source.ClientSetNameEn,
            dbItem.IconId,
            source.ClientIconId,
            NormalizeOptional(dbItem.AppearanceId),
            NormalizeOptional(source.ClientAppearanceId),
            new ClientItemAppearanceResolutionDto(
                NormalizeOptional(dbItem.AppearanceId),
                dbItem.AppearanceId > 0 ? source.AppearanceKnown : null,
                source.AppearancesD2oPath),
            source.IconPreviewFound,
            source.PreviewPath,
            source.ItemsD2oPath,
            source.ItemTypesD2oPath,
            source.ItemSetsD2oPath,
            source.AppearancesD2oPath,
            source.I18nEsPath,
            source.I18nEnPath);
    }

    private static void EnsurePositiveIds(IReadOnlyList<int> itemIds)
    {
        var invalidIds = itemIds.Where(x => x <= 0).Distinct().ToArray();
        if (invalidIds.Length == 0)
        {
            return;
        }

        throw new AdminValidationException(
            "One or more requested item ids are invalid.",
            new Dictionary<string, string[]>
            {
                ["ids"] = [$"All item ids must be greater than zero. Invalid values: {string.Join(", ", invalidIds)}."]
            });
    }

    private static int? NormalizeOptional(int value) => value > 0 ? value : null;

    private static int? NormalizeOptional(int? value) => value.GetValueOrDefault() > 0 ? value : null;
}
