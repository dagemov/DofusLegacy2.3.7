using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using RollblackLegacy.Admin.Application.Exceptions;

namespace RollblackLegacy.Admin.Api.ErrorHandling;

public sealed class AdminApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = CreateProblemDetails(httpContext, exception);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            problemDetails.GetType(),
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
        return true;
    }

    private static ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
    {
        ProblemDetails problemDetails = exception switch
        {
            AdminEntityNotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "The requested admin resource was not found.",
                Detail = notFound.Message,
                Type = "https://httpstatuses.com/404",
            },
            AdminValidationException validation => new ValidationProblemDetails(
                validation.Errors.ToDictionary(x => x.Key, x => x.Value))
            {
                Status = validation.StatusCode,
                Title = validation.StatusCode == StatusCodes.Status422UnprocessableEntity
                    ? "The item write payload is invalid."
                    : "The request is invalid.",
                Detail = validation.Message,
                Type = validation.StatusCode == StatusCodes.Status422UnprocessableEntity
                    ? "https://httpstatuses.com/422"
                    : "https://httpstatuses.com/400",
            },
            AdminConflictException conflict => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "The admin write operation conflicted with current Sunshine data.",
                Detail = conflict.Message,
                Type = "https://httpstatuses.com/409",
            },
            AdminNotConfiguredException notConfigured => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "SunshineAdmin is not configured.",
                Detail = notConfigured.Message,
                Type = "https://httpstatuses.com/500",
            },
            MySqlException => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "The admin API could not reach the Sunshine database.",
                Detail = "Check the SunshineAdmin connection string and database availability, then retry the request.",
                Type = "https://httpstatuses.com/500",
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "The admin API failed to process the request.",
                Detail = "An unexpected error occurred. Retry with the reported traceId if the problem persists.",
                Type = "https://httpstatuses.com/500",
            },
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        return problemDetails;
    }
}
