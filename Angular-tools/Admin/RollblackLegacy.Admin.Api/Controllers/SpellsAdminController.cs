using Microsoft.AspNetCore.Mvc;
using RollblackLegacy.Admin.Application.Abstractions.Spells;
using RollblackLegacy.Admin.Contracts.Spells;

namespace RollblackLegacy.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/v1/spells")]
public sealed class SpellsAdminController : ControllerBase
{
    private readonly ISpellsAdminReadService _spellsAdminReadService;
    private readonly ISpellsAdminWriteService _spellsAdminWriteService;

    public SpellsAdminController(
        ISpellsAdminReadService spellsAdminReadService,
        ISpellsAdminWriteService spellsAdminWriteService)
    {
        _spellsAdminReadService = spellsAdminReadService;
        _spellsAdminWriteService = spellsAdminWriteService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SpellPagedResultDto<SpellCatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<SpellPagedResultDto<SpellCatalogItemDto>>> GetSpells(
        [FromQuery] SpellCatalogSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _spellsAdminReadService.SearchAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{spellId:int}")]
    [ProducesResponseType(typeof(SpellDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpellDetailDto>> GetSpell(
        [FromRoute] short spellId,
        CancellationToken cancellationToken)
    {
        var result = await _spellsAdminReadService.GetByIdAsync(spellId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{spellId:int}/levels")]
    [ProducesResponseType(typeof(IReadOnlyList<SpellLevelDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SpellLevelDetailDto>>> GetLevels(
        [FromRoute] short spellId,
        CancellationToken cancellationToken)
    {
        var result = await _spellsAdminReadService.GetLevelsAsync(spellId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{spellId:int}/levels/{levelNumber:int}")]
    [ProducesResponseType(typeof(SpellLevelDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpellLevelDetailDto>> GetLevel(
        [FromRoute] short spellId,
        [FromRoute] int levelNumber,
        CancellationToken cancellationToken)
    {
        var result = await _spellsAdminReadService.GetLevelAsync(spellId, levelNumber, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{spellId:int}/levels/{levelNumber:int}")]
    [ProducesResponseType(typeof(SpellLevelUpdateResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SpellLevelUpdateResultDto>> UpdateLevel(
        [FromRoute] short spellId,
        [FromRoute] int levelNumber,
        [FromBody] SpellLevelUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _spellsAdminWriteService.UpdateLevelAsync(
            spellId,
            levelNumber,
            request,
            cancellationToken);
        return Ok(result);
    }
}
