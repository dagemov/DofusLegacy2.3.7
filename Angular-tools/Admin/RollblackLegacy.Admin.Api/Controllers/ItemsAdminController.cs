using Microsoft.AspNetCore.Mvc;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Contracts.Common;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/v1/items")]
public sealed class ItemsAdminController : ControllerBase
{
    private readonly IItemsAdminReadService _itemsAdminReadService;
    private readonly IItemsAdminWriteService _itemsAdminWriteService;

    public ItemsAdminController(
        IItemsAdminReadService itemsAdminReadService,
        IItemsAdminWriteService itemsAdminWriteService)
    {
        _itemsAdminReadService = itemsAdminReadService;
        _itemsAdminWriteService = itemsAdminWriteService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ItemPagedResultDto<ItemListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemPagedResultDto<ItemListItemDto>>> GetItems(
        [FromQuery] ItemSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _itemsAdminReadService.SearchAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{itemId:int}")]
    [ProducesResponseType(typeof(ItemDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemDetailDto>> GetItem(
        [FromRoute] int itemId,
        CancellationToken cancellationToken)
    {
        var result = await _itemsAdminReadService.GetItemAsync(itemId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{itemId:int}/identity")]
    [ProducesResponseType(typeof(ItemClientIdentityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemClientIdentityDto>> GetItemIdentity(
        [FromRoute] int itemId,
        CancellationToken cancellationToken)
    {
        var result = await _itemsAdminReadService.GetIdentityAsync(itemId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{itemId:int}/qa-summary")]
    [ProducesResponseType(typeof(ItemQaSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemQaSummaryDto>> GetQaSummary(
        [FromRoute] int itemId,
        CancellationToken cancellationToken)
    {
        var result = await _itemsAdminReadService.GetQaSummaryAsync(itemId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{itemId:int}/publication-status")]
    [ProducesResponseType(typeof(ItemPublicationStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemPublicationStatusDto>> GetPublicationStatus(
        [FromRoute] int itemId,
        CancellationToken cancellationToken)
    {
        var result = await _itemsAdminReadService.GetPublicationStatusAsync(itemId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("preview-state")]
    [ProducesResponseType(typeof(ItemPreviewStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ItemPreviewStateDto>> ResolvePreviewState(
        [FromQuery] int? itemId,
        [FromQuery] int? iconId,
        CancellationToken cancellationToken)
    {
        var result = await _itemsAdminWriteService.ResolvePreviewStateAsync(itemId, iconId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("appearance-preview-state")]
    [ProducesResponseType(typeof(ItemAppearancePreviewStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ItemAppearancePreviewStateDto>> ResolveAppearancePreviewState(
        [FromQuery] int appearanceId,
        [FromQuery] bool? appearanceKnown,
        CancellationToken cancellationToken)
    {
        var result = await _itemsAdminWriteService.ResolveAppearancePreviewStateAsync(
            appearanceId,
            appearanceKnown,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("types/options")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminOptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminOptionDto>>> GetItemTypeOptions(
        CancellationToken cancellationToken)
    {
        var result = await _itemsAdminReadService.GetTypeOptionsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("/api/admin/v1/item-sets/options")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminOptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminOptionDto>>> GetItemSetOptions(
        CancellationToken cancellationToken)
    {
        var result = await _itemsAdminReadService.GetItemSetOptionsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ItemWriteResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ItemWriteResultDto>> CreateItem(
        [FromBody] ItemCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _itemsAdminWriteService.CreateAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{itemId:int}")]
    [ProducesResponseType(typeof(ItemWriteResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ItemWriteResultDto>> UpdateItem(
        [FromRoute] int itemId,
        [FromBody] ItemUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _itemsAdminWriteService.UpdateAsync(itemId, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{itemId:int}/duplicate")]
    [ProducesResponseType(typeof(ItemWriteResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ItemWriteResultDto>> DuplicateItem(
        [FromRoute] int itemId,
        [FromBody] ItemDuplicateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _itemsAdminWriteService.DuplicateAsync(itemId, request, cancellationToken);
        return Ok(result);
    }
}
