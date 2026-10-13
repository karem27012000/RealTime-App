using ChatApp.Application.Exceptions;
using ChatApp.Domain.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Hubs;

/// <summary>
/// SignalR hides exception messages from clients by default. This filter passes our own
/// "expected" errors through as readable HubExceptions and hides everything else.
/// </summary>
public sealed class HubExceptionFilter(ILogger<HubExceptionFilter> logger) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (Exception ex) when (ex is DomainException or NotFoundException or ForbiddenException or UnauthorizedException)
        {
            throw new HubException(ex.Message);
        }
        catch (Exception ex) when (ex is not HubException)
        {
            logger.LogError(ex, "Error in hub method {Method}", invocationContext.HubMethodName);
            throw new HubException("Something went wrong. Please try again.");
        }
    }
}
