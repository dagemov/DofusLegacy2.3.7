using Microsoft.AspNetCore.Mvc;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/v1/item-icons")]
public sealed class ItemIconsAdminController : ControllerBase
{
    private readonly IItemsAdminReadService _itemsAdminReadService;

    public ItemIconsAdminController(IItemsAdminReadService itemsAdminReadService)
    {
        _itemsAdminReadService = itemsAdminReadService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ItemPagedResultDto<ItemIconOptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemPagedResultDto<ItemIconOptionDto>>> GetItemIcons(
        [FromQuery] ItemIconSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _itemsAdminReadService.SearchIconsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("category-stats")]
    [ProducesResponseType(typeof(ItemIconCategoryStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemIconCategoryStatsDto>> GetCategoryStats(CancellationToken cancellationToken)
    {
        var result = await _itemsAdminReadService.GetIconCategoryStatsAsync(cancellationToken);
        return Ok(result);
    }
}
