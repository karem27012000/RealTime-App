using ChatBot.Application.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace ChatBot.Api.Hubs;

/// <summary>
/// SignalR hides exception messages from clients by default. This filter passes
/// our own "expected" errors (not found, validation) through as readable HubExceptions.
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
        catch (Exception ex) when (ex is NotFoundException or ValidationException)
        {
            throw new HubException(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in hub method {Method}", invocationContext.HubMethodName);
            throw new HubException("Something went wrong. Please try again.");
        }
    }
}
