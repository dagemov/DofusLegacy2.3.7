using Microsoft.AspNetCore.Mvc;
using RollblackLegacy.Auth.Abstractions;
using RollblackLegacy.Auth.Contracts;

namespace OneLauncher.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthApiResponse>> Register(
        [FromBody] AuthRegisterRequest request,
        CancellationToken cancellationToken)
    {
        string? remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        AuthOperationResult result = await _authService.RegisterAsync(request, remoteIp, cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning("Registration failed for {Username}: {Title}", request.Username, result.Title);
            int statusCode = result.Title.Contains("en uso", StringComparison.OrdinalIgnoreCase)
                || result.Title.Contains("registrado", StringComparison.OrdinalIgnoreCase)
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;

            return StatusCode(statusCode, AuthApiResponse.From(result));
        }

        _logger.LogInformation("Account registered: {Username}", result.Username);
        return Ok(AuthApiResponse.From(result));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthApiResponse>> Login(
        [FromBody] AuthLoginRequest request,
        CancellationToken cancellationToken)
    {
        AuthOperationResult result = await _authService.LoginAsync(request, cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning("Login failed for {Username}: {Title}", request.Username, result.Title);
            return Unauthorized(AuthApiResponse.From(result));
        }

        _logger.LogInformation("Login succeeded for {Username}", result.Username);
        return Ok(AuthApiResponse.From(result));
    }

    [HttpGet("check-username")]
    public async Task<ActionResult<UsernameAvailabilityApiResponse>> CheckUsername(
        [FromQuery] string? username,
        CancellationToken cancellationToken)
    {
        UsernameAvailabilityResult result = await _authService.CheckUsernameAvailabilityAsync(
            username,
            cancellationToken);

        return Ok(UsernameAvailabilityApiResponse.From(result));
    }
}

public sealed record AuthApiResponse(
    bool Success,
    string Title,
    string Message,
    int? AccountId,
    string? Username,
    string? Nickname,
    string? Email,
    bool EmailWasStored,
    bool UsesWebsiteContactTable)
{
    public static AuthApiResponse From(AuthOperationResult result) =>
        new(
            result.Success,
            result.Title,
            result.Message,
            result.AccountId,
            result.Username,
            result.Nickname,
            result.Email,
            result.EmailWasStored,
            result.UsesWebsiteContactTable);
}

public sealed record UsernameAvailabilityApiResponse(
    bool HasValue,
    bool IsAvailable,
    string Message,
    string Tone)
{
    public static UsernameAvailabilityApiResponse From(UsernameAvailabilityResult result) =>
        new(result.HasValue, result.IsAvailable, result.Message, result.Tone);
}
