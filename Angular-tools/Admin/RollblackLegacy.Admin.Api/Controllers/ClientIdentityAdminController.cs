using Microsoft.AspNetCore.Mvc;
using RollblackLegacy.Admin.Application.Abstractions.ClientIdentity;
using RollblackLegacy.Admin.Application.Exceptions;
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ClientItemIdentityCheckResultDto>>> CheckItems(
        [FromQuery(Name = "ids")] string? ids,
        CancellationToken cancellationToken)
    {
        var request = new ClientItemIdentityCheckRequest(ParseIds(ids));
        var result = await _clientItemIdentityReadService.CheckAsync(request, cancellationToken);
        return Ok(result);
    }

    private static IReadOnlyList<int> ParseIds(string? ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
        {
            throw new AdminValidationException(
                "No item ids were provided for the client identity audit.",
                new Dictionary<string, string[]>
                {
                    ["ids"] = ["Use the ids query string, for example ?ids=7754,12616,12617,39."]
                });
        }

        var values = new List<int>();
        var invalidTokens = new List<string>();

        foreach (var token in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(token, out var value))
            {
                values.Add(value);
            }
            else
            {
                invalidTokens.Add(token);
            }
        }

        if (invalidTokens.Count == 0)
        {
            return values;
        }

        throw new AdminValidationException(
            "One or more item ids could not be parsed.",
            new Dictionary<string, string[]>
            {
                ["ids"] = [$"Invalid values: {string.Join(", ", invalidTokens)}."]
            });
    }
}
