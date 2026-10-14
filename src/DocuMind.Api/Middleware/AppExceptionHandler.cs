using DocuMind.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.Api.Middleware;

/// <summary>
/// Maps known application exceptions to safe, structured HTTP responses. Never exposes stack
/// traces or exception details for anything unexpected — those are logged server-side only.
/// </summary>
public class AppExceptionHandler(ILogger<AppExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, errors) = exception switch
        {
            NotFoundException notFound => (StatusCodes.Status404NotFound, "Not Found", new[] { notFound.Message }),
            AppValidationException validation => (StatusCodes.Status400BadRequest, "Validation Failed", validation.Errors.ToArray()),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", []),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
        };
        if (errors.Length > 0)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
