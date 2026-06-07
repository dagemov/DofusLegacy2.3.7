using RollblackLegacy.Admin.Application.Abstractions.Spells;
using RollblackLegacy.Admin.Application.Exceptions;
using RollblackLegacy.Admin.Application.Models.Spells;
using RollblackLegacy.Admin.Contracts.Spells;

namespace RollblackLegacy.Admin.Application.Services;

public sealed class SpellsAdminWriteService : ISpellsAdminWriteService
{
    private readonly ISpellsAdminReadRepository _readRepository;
    private readonly ISpellsAdminWriteRepository _writeRepository;

    public SpellsAdminWriteService(
        ISpellsAdminReadRepository readRepository,
        ISpellsAdminWriteRepository writeRepository)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
    }

    public async Task<SpellLevelUpdateResultDto> UpdateLevelAsync(
        short spellId,
        int levelNumber,
        SpellLevelUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePositiveSpellId(spellId);
        EnsurePositiveLevelNumber(levelNumber);

        var spell = await _readRepository.GetByIdAsync(spellId, cancellationToken);
        if (spell is null)
        {
            throw new AdminEntityNotFoundException("spell", spellId.ToString());
        }

        if (!spell.RuntimeAvailable)
        {
            throw new AdminConflictException(
                $"El spell #{spellId} no existe en runtime; solo esta disponible en referencia y no puede editarse por niveles.");
        }

        var existingLevel = await _readRepository.GetLevelAsync(spellId, levelNumber, cancellationToken);
        if (existingLevel is null || !existingLevel.RuntimeAvailable)
        {
            throw new AdminEntityNotFoundException("spell level", $"{spellId}:{levelNumber}");
        }

        ValidateRequest(request, existingLevel);

        var draft = new AdminSpellLevelUpdateDraft(
            request.ApCost ?? existingLevel.ApCost,
            request.MinRange ?? existingLevel.MinRange,
            request.MaxRange ?? existingLevel.MaxRange,
            request.CastInLine ?? existingLevel.CastInLine,
            request.CastTestLos ?? existingLevel.CastTestLos,
            request.CriticalHitProbability ?? existingLevel.CriticalHitProbability,
            request.CriticalFailureProbability ?? existingLevel.CriticalFailureProbability,
            request.NeedFreeCell ?? existingLevel.NeedFreeCell,
            request.MinCastInterval ?? existingLevel.MinCastInterval,
            request.MaxCastPerTurn ?? existingLevel.MaxCastPerTurn,
            request.MaxCastPerTarget ?? existingLevel.MaxCastPerTarget,
            request.CastInDiagonal,
            request.NeedTakenCell,
            request.InitialCooldown);

        var updateResult = await _writeRepository.UpdateLevelAsync(
            spellId,
            levelNumber,
            draft,
            cancellationToken);
        if (updateResult is null)
        {
            throw new AdminEntityNotFoundException("spell level", $"{spellId}:{levelNumber}");
        }

        var updatedLevel = await _readRepository.GetLevelAsync(spellId, levelNumber, cancellationToken);
        if (updatedLevel is null)
        {
            throw new AdminEntityNotFoundException("spell level", $"{spellId}:{levelNumber}");
        }

        return new SpellLevelUpdateResultDto(
            updateResult.SpellId,
            updateResult.LevelNumber,
            updateResult.WriteStrategy,
            SpellsAdminReadService.MapLevelDetail(updatedLevel),
            updateResult.Warnings.ToList());
    }

    private static void ValidateRequest(
        SpellLevelUpdateRequest request,
        AdminSpellLevelDetailReadModel existingLevel)
    {
        if (request.ApCost is null &&
            request.MinRange is null &&
            request.MaxRange is null &&
            request.CastInLine is null &&
            request.CastInDiagonal is null &&
            request.CastTestLos is null &&
            request.CriticalHitProbability is null &&
            request.CriticalFailureProbability is null &&
            request.NeedFreeCell is null &&
            request.NeedTakenCell is null &&
            request.MinCastInterval is null &&
            request.InitialCooldown is null &&
            request.MaxCastPerTurn is null &&
            request.MaxCastPerTarget is null)
        {
            throw new AdminValidationException(
                "La actualizacion del nivel no contiene ningun campo editable.",
                new Dictionary<string, string[]>
                {
                    ["request"] =
                    [
                        "Debes enviar al menos un campo editable para actualizar el nivel."
                    ]
                });
        }

        if (existingLevel.RuntimeLevelId.HasValue)
        {
            var unsupportedFields = new List<string>();
            if (request.CastInDiagonal.HasValue)
            {
                unsupportedFields.Add("castInDiagonal");
            }

            if (request.NeedTakenCell.HasValue)
            {
                unsupportedFields.Add("needTakenCell");
            }

            if (request.InitialCooldown.HasValue)
            {
                unsupportedFields.Add("initialCooldown");
            }

            if (unsupportedFields.Count > 0)
            {
                throw new AdminValidationException(
                    "El esquema legacy activo no soporta algunos campos de nivel solicitados.",
                    unsupportedFields.ToDictionary(
                        field => field,
                        field => new[]
                        {
                            $"{field} no puede editarse contra el esquema legacy activo porque el Spell Builder original no persistia ese dato por nivel."
                        }));
            }
        }

        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (request.ApCost is < 0)
        {
            errors["apCost"] = ["apCost no puede ser negativo."];
        }

        if (request.MinRange is < 0)
        {
            errors["minRange"] = ["minRange no puede ser negativo."];
        }

        if (request.MaxRange is < 0)
        {
            errors["maxRange"] = ["maxRange no puede ser negativo."];
        }

        if (request.CriticalHitProbability is < 0)
        {
            errors["criticalHitProbability"] = ["criticalHitProbability no puede ser negativo."];
        }

        if (request.CriticalFailureProbability is < 0)
        {
            errors["criticalFailureProbability"] = ["criticalFailureProbability no puede ser negativo."];
        }

        if (request.MinCastInterval is < 0)
        {
            errors["minCastInterval"] = ["minCastInterval no puede ser negativo."];
        }

        if (request.InitialCooldown is < 0)
        {
            errors["initialCooldown"] = ["initialCooldown no puede ser negativo."];
        }

        if (request.MaxCastPerTurn is < 0)
        {
            errors["maxCastPerTurn"] = ["maxCastPerTurn no puede ser negativo."];
        }

        if (request.MaxCastPerTarget is < 0)
        {
            errors["maxCastPerTarget"] = ["maxCastPerTarget no puede ser negativo."];
        }

        var effectiveMinRange = request.MinRange ?? existingLevel.MinRange;
        var effectiveMaxRange = request.MaxRange ?? existingLevel.MaxRange;
        if (effectiveMaxRange < effectiveMinRange)
        {
            errors["maxRange"] = ["maxRange debe ser mayor o igual a minRange."];
        }

        if (errors.Count > 0)
        {
            throw new AdminValidationException(
                "La actualizacion del nivel contiene valores invalidos.",
                errors);
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
                    ["spellId"] = ["spellId debe ser mayor que cero."]
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
                    ["levelNumber"] = ["levelNumber debe ser mayor que cero."]
                });
        }
    }
}
