using System.Diagnostics;
using AnonyMeow.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AnonyMeow.Common.Middleware;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var statusCode = exception is ApiException apiException
            ? apiException.StatusCode
            : StatusCodes.Status500InternalServerError;

        var problemDetails = exception switch
        {
            ApiException apiEx => new ProblemDetails
            {
                Status = statusCode,
                Title = apiEx.Title,
                Detail = apiEx.Detail,
                Extensions = { ["correlationId"] = correlationId }
            },
            _ => new ProblemDetails
            {
                Status = statusCode,
                Title = "An unexpected error occurred.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Detail = environment.IsDevelopment() ? exception.Message : null,
                Extensions = { ["correlationId"] = correlationId }
            }
        };

        if (exception is ApiException apiExceptionWithExtensions)
        {
            foreach (var (key, value) in apiExceptionWithExtensions.Extensions)
            {
                problemDetails.Extensions[key] = value;
            }
        }

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}. CorrelationId: {CorrelationId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                correlationId);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
