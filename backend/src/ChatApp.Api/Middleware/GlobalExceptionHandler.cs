using ChatApp.Application.Exceptions;
using ChatApp.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Middleware;

/// <summary>
/// Turns exceptions into consistent JSON error responses (RFC 9457 "problem details"),
/// so controllers don't need try/catch blocks.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            DomainException => (StatusCodes.Status400BadRequest, "Invalid request"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Not logged in"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Not allowed"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            _ => (StatusCodes.Status500InternalServerError, "Something went wrong")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                // Don't leak internal error details to the client.
                Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message,
            },
        });
    }
}
