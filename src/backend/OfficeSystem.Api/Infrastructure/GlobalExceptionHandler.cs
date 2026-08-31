using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace OfficeSystem.Api.Infrastructure;

/// <summary>
/// Turns anything that escapes a handler into a ProblemDetails response, so the
/// client never receives an HTML error page or a stack trace.
/// </summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        ProblemDetails problem = exception switch
        {
            // Model binding rejected the body (bad JSON, an unknown enum name, a
            // malformed date). That is the caller's mistake, not a server fault.
            BadHttpRequestException badRequest => Describe(
                httpContext,
                StatusCodes.Status400BadRequest,
                "That request could not be read.",
                badRequest.Message),
            _ => Describe(
                httpContext,
                StatusCodes.Status500InternalServerError,
                "Something went wrong.",
                "The request could not be completed. Please try again.")
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation(
                "Rejected {Method} {Path}: {Reason}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);

        return true;
    }

    private static ProblemDetails Describe(HttpContext httpContext, int status, string title, string detail)
        => new()
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        };
}
