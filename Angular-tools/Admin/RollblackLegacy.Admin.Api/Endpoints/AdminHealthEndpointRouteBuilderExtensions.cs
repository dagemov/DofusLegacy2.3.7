using RollblackLegacy.Admin.Application.Abstractions;
using RollblackLegacy.Admin.Contracts.Health;

namespace RollblackLegacy.Admin.Api.Endpoints;

public static class AdminHealthEndpointRouteBuilderExtensions
{
    private const string ServiceName = "RollblackLegacy.Admin.Api";

    public static IEndpointRouteBuilder MapAdminHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/v1");

        group.MapGet("/health", () =>
            TypedResults.Ok(new AdminHealthResponse("ok", ServiceName)));

        group.MapGet("/health/db", async (IAdminDatabaseHealthService healthService, CancellationToken cancellationToken) =>
        {
            var result = await healthService.CheckAsync(cancellationToken);

            return TypedResults.Ok(new AdminDatabaseHealthResponse(
                result.Status,
                ServiceName,
                result.Database,
                result.Message,
                result.CheckedAtUtc,
                result.Host,
                result.Port,
                result.User,
                result.IsRemote));
        });

        return endpoints;
    }
}
