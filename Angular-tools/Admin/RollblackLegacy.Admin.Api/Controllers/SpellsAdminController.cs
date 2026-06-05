using Microsoft.AspNetCore.Mvc;
using RollblackLegacy.Admin.Application.Abstractions.Spells;
using RollblackLegacy.Admin.Contracts.Spells;

namespace RollblackLegacy.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/v1/spells")]
public sealed class SpellsAdminController : ControllerBase
{
    private readonly ISpellsAdminReadService _spellsAdminReadService;

    public SpellsAdminController(ISpellsAdminReadService spellsAdminReadService)
    {
        _spellsAdminReadService = spellsAdminReadService;
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
}
