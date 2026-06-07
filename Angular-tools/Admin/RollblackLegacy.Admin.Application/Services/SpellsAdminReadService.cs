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

    public async Task<IReadOnlyList<SpellLevelDetailDto>> GetLevelsAsync(
        short spellId,
        CancellationToken cancellationToken = default)
    {
        EnsurePositiveSpellId(spellId);

        var levels = await _repository.GetLevelsAsync(spellId, cancellationToken);
        if (levels is null)
        {
            throw new AdminEntityNotFoundException("spell", spellId.ToString());
        }

        return levels
            .Select(MapLevelDetail)
            .ToList();
    }

    public async Task<SpellLevelDetailDto> GetLevelAsync(
        short spellId,
        int levelNumber,
        CancellationToken cancellationToken = default)
    {
        EnsurePositiveSpellId(spellId);
        EnsurePositiveLevelNumber(levelNumber);

        var level = await _repository.GetLevelAsync(spellId, levelNumber, cancellationToken);
        if (level is null)
        {
            throw new AdminEntityNotFoundException("spell level", $"{spellId}:{levelNumber}");
        }

        return MapLevelDetail(level);
    }

    public async Task<SpellLevelEffectsDto> GetLevelEffectsAsync(
        short spellId,
        int levelNumber,
        CancellationToken cancellationToken = default)
    {
        EnsurePositiveSpellId(spellId);
        EnsurePositiveLevelNumber(levelNumber);

        var effects = await _repository.GetLevelEffectsAsync(spellId, levelNumber, cancellationToken);
        if (effects is null)
        {
            throw new AdminEntityNotFoundException("spell level effects", $"{spellId}:{levelNumber}");
        }

        return new SpellLevelEffectsDto(
            effects.SpellId,
            effects.LevelNumber,
            MapEffectCollection(effects.Effects),
            MapEffectCollection(effects.CriticalEffects));
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

    internal static SpellLevelDetailDto MapLevelDetail(AdminSpellLevelDetailReadModel level)
    {
        return new SpellLevelDetailDto(
            level.LevelNumber,
            level.RuntimeLevelId,
            level.ReferenceLevelId,
            level.MinPlayerLevel,
            level.ApCost,
            level.MinRange,
            level.MaxRange,
            level.CastInLine,
            level.CastInDiagonal,
            level.CastTestLos,
            level.NeedFreeCell,
            level.NeedTakenCell,
            level.RangeCanBeBoosted,
            level.CriticalFailureEndsTurn,
            level.CriticalHitProbability,
            level.CriticalFailureProbability,
            level.MaxCastPerTurn,
            level.MaxCastPerTarget,
            level.MinCastInterval,
            level.InitialCooldown,
            level.StatesRequired.ToList(),
            level.StatesForbidden.ToList(),
            level.HasEffects,
            level.HasCriticalEffects,
            level.RuntimeAvailable,
            level.ReferenceAvailable);
    }

    private static SpellEffectCollectionDto MapEffectCollection(AdminSpellEffectCollectionReadModel collection)
    {
        return new SpellEffectCollectionDto(
            collection.RuntimeAvailable,
            collection.ReferenceAvailable,
            collection.RuntimeSource,
            collection.ReferenceSource,
            collection.RuntimeRows.Select(MapEffectRow).ToList(),
            collection.ReferenceRows.Select(MapEffectRow).ToList(),
            collection.RuntimeWarnings.ToList(),
            collection.ReferenceWarnings.ToList());
    }

    private static SpellEffectRowDto MapEffectRow(AdminSpellEffectRowReadModel row)
    {
        return new SpellEffectRowDto(
            row.RowIndex,
            row.EffectId,
            row.Label,
            row.ProtocolName,
            row.Group,
            row.OperatorMode,
            row.Value,
            row.MinValue,
            row.MaxValue,
            row.Delay,
            row.Random,
            row.Duration,
            row.TargetType,
            row.ZoneShape,
            row.ZoneMinSize,
            row.ZoneSize,
            row.PreviewText);
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

    private static void EnsurePositiveLevelNumber(int levelNumber)
    {
        if (levelNumber <= 0)
        {
            throw new AdminValidationException(
                "El nivel solicitado no es valido.",
                new Dictionary<string, string[]>
                {
                    ["levelNumber"] = new[]
                    {
                        "levelNumber debe ser mayor que cero."
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
