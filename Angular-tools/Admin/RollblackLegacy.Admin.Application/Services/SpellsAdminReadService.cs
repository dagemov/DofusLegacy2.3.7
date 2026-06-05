using RollblackLegacy.Admin.Application.Abstractions.Spells;
using RollblackLegacy.Admin.Application.Exceptions;
using RollblackLegacy.Admin.Application.Models.Spells;
using RollblackLegacy.Admin.Contracts.Spells;

namespace RollblackLegacy.Admin.Application.Services;

public sealed class SpellsAdminReadService : ISpellsAdminReadService
{
    private const int MaxPageSize = 100;

    private readonly ISpellsAdminReadRepository _repository;

    public SpellsAdminReadService(ISpellsAdminReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<SpellPagedResultDto<SpellCatalogItemDto>> SearchAsync(
        SpellCatalogSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var result = await _repository.SearchAsync(request, cancellationToken);
        var items = result.Items
            .Select(MapItem)
            .ToList();

        return new SpellPagedResultDto<SpellCatalogItemDto>(
            NormalizePage(request.Page),
            NormalizePageSize(request.PageSize),
            result.TotalCount,
            items);
    }

    public async Task<SpellDetailDto> GetByIdAsync(
        short spellId,
        CancellationToken cancellationToken = default)
    {
        EnsurePositiveSpellId(spellId);

        var detail = await _repository.GetByIdAsync(spellId, cancellationToken);
        if (detail is null)
        {
            throw new AdminEntityNotFoundException("spell", spellId.ToString());
        }

        return MapDetail(detail);
    }

    private static SpellCatalogItemDto MapItem(AdminSpellCatalogReadModel item)
    {
        return new SpellCatalogItemDto(
            item.SpellId,
            item.Name,
            item.Description,
            item.TypeId,
            item.TypeLabel,
            item.IconId,
            item.Breeds
                .Select(breed => new SpellBreedSummaryDto(breed.BreedId, breed.Label))
                .ToList(),
            item.LevelCount,
            item.RuntimeAvailable,
            item.ReferenceAvailable);
    }

    private static SpellDetailDto MapDetail(AdminSpellDetailReadModel detail)
    {
        return new SpellDetailDto(
            detail.SpellId,
            detail.Name,
            detail.Description,
            detail.TypeId,
            detail.TypeLabel,
            detail.IconId,
            detail.Breeds
                .Select(breed => new SpellBreedSummaryDto(breed.BreedId, breed.Label))
                .ToList(),
            detail.LevelCount,
            detail.RuntimeAvailable,
            detail.ReferenceAvailable,
            detail.Reference is null
                ? null
                : new SpellReferenceMetadataDto(
                    detail.Reference.SourceDescription,
                    detail.Reference.Name,
                    detail.Reference.Description,
                    detail.Reference.NameId,
                    detail.Reference.DescriptionId,
                    detail.Reference.TypeId,
                    detail.Reference.TypeLabel,
                    detail.Reference.IconId,
                    detail.Reference.BreedIds.ToList(),
                    detail.Reference.LevelCount),
            detail.Levels
                .Select(level => new SpellLevelSummaryDto(
                    level.LevelNumber,
                    level.RuntimeLevelId,
                    level.ReferenceLevelId,
                    level.MinPlayerLevel,
                    level.ApCost,
                    level.MinRange,
                    level.MaxRange,
                    level.CastInLine,
                    level.CastTestLos,
                    level.NeedFreeCell,
                    level.RangeCanBeBoosted,
                    level.CriticalFailureEndsTurn,
                    level.CriticalHitProbability,
                    level.CriticalFailureProbability,
                    level.MaxCastPerTurn,
                    level.MaxCastPerTarget,
                    level.MinCastInterval,
                    level.StatesRequired.ToList(),
                    level.StatesForbidden.ToList(),
                    level.HasEffects,
                    level.HasCriticalEffects,
                    level.RuntimeAvailable,
                    level.ReferenceAvailable))
                .ToList());
    }

    private static void ValidateRequest(SpellCatalogSearchRequest request)
    {
        if (request.SpellId.HasValue && request.SpellId.Value <= 0)
        {
            throw new AdminValidationException(
                "El filtro spellId no es valido.",
                new Dictionary<string, string[]>
                {
                    ["spellId"] = new[]
                    {
                        "spellId debe ser mayor que cero."
                    }
                });
        }

        if (request.BreedId.HasValue && request.BreedId.Value <= 0)
        {
            throw new AdminValidationException(
                "El filtro breedId no es valido.",
                new Dictionary<string, string[]>
                {
                    ["breedId"] = new[]
                    {
                        "breedId debe ser mayor que cero."
                    }
                });
        }

        if (request.TypeId.HasValue && request.TypeId.Value < 0)
        {
            throw new AdminValidationException(
                "El filtro typeId no es valido.",
                new Dictionary<string, string[]>
                {
                    ["typeId"] = new[]
                    {
                        "typeId no puede ser negativo."
            }
                });
        }
    }

    private static void EnsurePositiveSpellId(short spellId)
    {
        if (spellId <= 0)
        {
            throw new AdminValidationException(
                "El spell solicitado no es valido.",
                new Dictionary<string, string[]>
                {
                    ["spellId"] = new[]
                    {
                        "spellId debe ser mayor que cero."
                    }
                });
        }
    }

    private static int NormalizePage(int page) => page <= 0 ? 1 : page;

    private static int NormalizePageSize(int pageSize)
    {
        return pageSize switch
        {
            <= 0 => 20,
            > MaxPageSize => MaxPageSize,
            _ => pageSize,
        };
    }
}
