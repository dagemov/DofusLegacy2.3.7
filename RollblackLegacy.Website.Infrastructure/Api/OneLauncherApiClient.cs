using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RollblackLegacy.Auth.Contracts;
using RollblackLegacy.Website.Application.Abstractions;

namespace RollblackLegacy.Website.Infrastructure.Api;

public sealed class OneLauncherApiClient : IOneLauncherApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<OneLauncherApiClient> _logger;

    public OneLauncherApiClient(HttpClient httpClient, ILogger<OneLauncherApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AuthOperationResult> RegisterAsync(
        AuthRegisterRequest request,
        string? remoteIp,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/auth/register")
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };

        if (!string.IsNullOrWhiteSpace(remoteIp))
            message.Headers.TryAddWithoutValidation("X-Forwarded-For", remoteIp);

        return await SendAuthAsync(message, cancellationToken);
    }

    public async Task<AuthOperationResult> LoginAsync(
        AuthLoginRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };

        return await SendAuthAsync(message, cancellationToken);
    }

    public async Task<UsernameAvailabilityResult> CheckUsernameAvailabilityAsync(
        string? username,
        CancellationToken cancellationToken)
    {
        string encoded = Uri.EscapeDataString(username ?? string.Empty);
        string requestUri = $"api/auth/check-username?username={encoded}";

        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken);
            response.EnsureSuccessStatusCode();
            UsernameAvailabilityApiDto? payload = await response.Content.ReadFromJsonAsync<UsernameAvailabilityApiDto>(
                JsonOptions,
                cancellationToken);

            if (payload is null)
                throw new InvalidOperationException("Empty username availability response.");

            return new UsernameAvailabilityResult
            {
                HasValue = payload.HasValue,
                IsAvailable = payload.IsAvailable,
                Message = payload.Message,
                Tone = payload.Tone,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Username availability request failed.");
            return new UsernameAvailabilityResult
            {
                HasValue = true,
                IsAvailable = false,
                Message = "No se pudo validar ahora mismo.",
                Tone = "warning",
            };
        }
    }

    private async Task<AuthOperationResult> SendAuthAsync(
        HttpRequestMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.SendAsync(message, cancellationToken);
            AuthApiDto? payload = await response.Content.ReadFromJsonAsync<AuthApiDto>(JsonOptions, cancellationToken);

            if (payload is null)
            {
                return new AuthOperationResult
                {
                    Success = false,
                    Title = "Servicio no disponible",
                    Message = "La API del launcher no devolvio una respuesta valida.",
                };
            }

            return Map(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auth API request failed for {Method} {Uri}", message.Method, message.RequestUri);
            return new AuthOperationResult
            {
                Success = false,
                Title = "Servicio no disponible",
                Message = "No pudimos contactar la API del launcher. Intentalo de nuevo en unos minutos.",
            };
        }
    }

    private static AuthOperationResult Map(AuthApiDto payload) =>
        new()
        {
            Success = payload.Success,
            Title = payload.Title,
            Message = payload.Message,
            AccountId = payload.AccountId,
            Username = payload.Username,
            Nickname = payload.Nickname,
            Email = payload.Email,
            EmailWasStored = payload.EmailWasStored,
            UsesWebsiteContactTable = payload.UsesWebsiteContactTable,
        };

    private sealed record AuthApiDto(
        bool Success,
        string Title,
        string Message,
        int? AccountId,
        string? Username,
        string? Nickname,
        string? Email,
        bool EmailWasStored,
        bool UsesWebsiteContactTable);

    private sealed record UsernameAvailabilityApiDto(
        bool HasValue,
        bool IsAvailable,
        string Message,
        string Tone);
}
