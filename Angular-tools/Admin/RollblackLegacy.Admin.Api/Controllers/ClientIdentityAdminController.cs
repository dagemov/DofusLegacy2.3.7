using Microsoft.AspNetCore.Mvc;
using RollblackLegacy.Admin.Application.Abstractions.ClientIdentity;
using RollblackLegacy.Admin.Application.ClientIdentity;
using RollblackLegacy.Admin.Contracts.ClientIdentity;

namespace RollblackLegacy.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/v1/client-identity/items")]
public sealed class ClientIdentityAdminController : ControllerBase
{
    private readonly IClientItemIdentityReadService _clientItemIdentityReadService;

    public ClientIdentityAdminController(IClientItemIdentityReadService clientItemIdentityReadService)
    {
        _clientItemIdentityReadService = clientItemIdentityReadService;
    }

    [HttpGet("{itemId:int}")]
    [ProducesResponseType(typeof(ClientItemIdentityCheckResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientItemIdentityCheckResultDto>> GetItem(
        [FromRoute] int itemId,
        CancellationToken cancellationToken)
    {
        var result = await _clientItemIdentityReadService.GetItemAsync(itemId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("check")]
    [ProducesResponseType(typeof(IReadOnlyList<ClientItemIdentityCheckResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ClientItemIdentityCheckResultDto>>> CheckItems(
        [FromQuery(Name = "ids")] string? ids,
        CancellationToken cancellationToken)
    {
        var request = new ClientItemIdentityCheckRequest(ClientItemIdentityIdParser.Parse(ids));
        var result = await _clientItemIdentityReadService.CheckAsync(request, cancellationToken);
        return Ok(result);
    }
}
