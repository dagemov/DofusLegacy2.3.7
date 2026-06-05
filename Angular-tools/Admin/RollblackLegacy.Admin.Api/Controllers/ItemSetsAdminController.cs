using Microsoft.AspNetCore.Mvc;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/v1/item-sets")]
public sealed class ItemSetsAdminController : ControllerBase
{
    private readonly IItemSetsAdminReadService _itemSetsAdminReadService;

    public ItemSetsAdminController(IItemSetsAdminReadService itemSetsAdminReadService)
    {
        _itemSetsAdminReadService = itemSetsAdminReadService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ItemSetListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ItemSetListItemDto>>> List(CancellationToken cancellationToken)
    {
        var result = await _itemSetsAdminReadService.ListAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{setId:int}")]
    [ProducesResponseType(typeof(ItemSetDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemSetDetailDto>> GetById(int setId, CancellationToken cancellationToken)
    {
        var result = await _itemSetsAdminReadService.GetByIdAsync(setId, cancellationToken);
        return Ok(result);
    }
}
