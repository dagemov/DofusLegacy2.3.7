using Microsoft.AspNetCore.Mvc;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/v1")]
public sealed class ItemEffectsAdminController : ControllerBase
{
    private readonly IItemEffectsAdminService _itemEffectsAdminService;

    public ItemEffectsAdminController(IItemEffectsAdminService itemEffectsAdminService)
    {
        _itemEffectsAdminService = itemEffectsAdminService;
    }

    [HttpGet("items/{itemId:int}/effects/edit")]
    [ProducesResponseType(typeof(ItemEffectsEditDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemEffectsEditDto>> GetEffectsEdit(
        [FromRoute] int itemId,
        CancellationToken cancellationToken)
    {
        var result = await _itemEffectsAdminService.GetEditAsync(itemId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("items/{itemId:int}/effects")]
    [ProducesResponseType(typeof(ItemEffectsUpdateResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ItemEffectsUpdateResultDto>> UpdateEffects(
        [FromRoute] int itemId,
        [FromBody] ItemEffectsUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _itemEffectsAdminService.UpdateAsync(itemId, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("item-effects/options")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminEffectOptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminEffectOptionDto>>> GetEffectOptions(
        CancellationToken cancellationToken)
    {
        var result = await _itemEffectsAdminService.GetOptionsAsync(cancellationToken);
        return Ok(result);
    }
}
