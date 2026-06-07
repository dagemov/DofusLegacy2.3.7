using Microsoft.AspNetCore.Mvc;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/v1/item-sets")]
public sealed class ItemSetsAdminController : ControllerBase
{
    private readonly IItemSetsAdminReadService _readService;
    private readonly IItemSetsAdminWriteService _writeService;

    public ItemSetsAdminController(
        IItemSetsAdminReadService readService,
        IItemSetsAdminWriteService writeService)
    {
        _readService = readService;
        _writeService = writeService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ItemPagedResultDto<ItemSetListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemPagedResultDto<ItemSetListItemDto>>> Search(
        [FromQuery] ItemSetSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _readService.SearchAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{setId:int}")]
    [ProducesResponseType(typeof(ItemSetDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemSetDetailDto>> GetById(int setId, CancellationToken cancellationToken)
    {
        var result = await _readService.GetByIdAsync(setId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ItemSetWriteResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ItemSetWriteResultDto>> Create(
        [FromBody] ItemSetCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _writeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { setId = result.SetId }, result);
    }

    [HttpPut("{setId:int}")]
    [ProducesResponseType(typeof(ItemSetWriteResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ItemSetWriteResultDto>> Update(
        int setId,
        [FromBody] ItemSetUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _writeService.UpdateAsync(setId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{setId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int setId, CancellationToken cancellationToken)
    {
        await _writeService.DeleteAsync(setId, cancellationToken);
        return NoContent();
    }
}
