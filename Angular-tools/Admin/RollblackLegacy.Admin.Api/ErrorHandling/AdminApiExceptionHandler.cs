using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using RollblackLegacy.Admin.Application.Exceptions;

namespace RollblackLegacy.Admin.Api.ErrorHandling;

public sealed class AdminApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<AdminApiExceptionHandler> _logger;
    private readonly IHostEnvironment _hostEnvironment;

    public AdminApiExceptionHandler(
        ILogger<AdminApiExceptionHandler> logger,
        IHostEnvironment hostEnvironment)
    {
        _logger = logger;
        _hostEnvironment = hostEnvironment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Admin API request failed. {Method} {Path} traceId={TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

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

    private ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
    {
        ProblemDetails problemDetails = exception switch
        {
            AdminEntityNotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "No se encontró el recurso solicitado en Admin.",
                Detail = notFound.Message,
                Type = "https://httpstatuses.com/404",
            },
            AdminValidationException validation => new ValidationProblemDetails(
                validation.Errors.ToDictionary(x => x.Key, x => x.Value))
            {
                Status = validation.StatusCode,
                Title = validation.StatusCode == StatusCodes.Status422UnprocessableEntity
                    ? "Los datos enviados para el item no son válidos."
                    : "La solicitud no es válida.",
                Detail = validation.Message,
                Type = validation.StatusCode == StatusCodes.Status422UnprocessableEntity
                    ? "https://httpstatuses.com/422"
                    : "https://httpstatuses.com/400",
            },
            AdminConflictException conflict => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "La operación entró en conflicto con los datos actuales de Sunshine.",
                Detail = conflict.Message,
                Type = "https://httpstatuses.com/409",
            },
            AdminNotConfiguredException notConfigured => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "SunshineAdmin no está configurado.",
                Detail = notConfigured.Message,
                Type = "https://httpstatuses.com/503",
            },
            MySqlException => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "No se pudo conectar con la base de datos Sunshine.",
                Detail = "Verifica la conexión SunshineAdmin y la disponibilidad de la base de datos, luego intenta de nuevo.",
                Type = "https://httpstatuses.com/500",
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "El Admin API no pudo procesar la solicitud.",
                Detail = BuildUnexpectedFailureDetail(exception),
                Type = "https://httpstatuses.com/500",
            },
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        return problemDetails;
    }

    private string BuildUnexpectedFailureDetail(Exception exception)
    {
        if (_hostEnvironment.IsDevelopment())
        {
            return exception.Message;
        }

        return "Ocurrió un error inesperado. Intenta de nuevo con el traceId informado si el problema persiste.";
    }
}
