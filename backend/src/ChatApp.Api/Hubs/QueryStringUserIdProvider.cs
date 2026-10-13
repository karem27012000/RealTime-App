using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Hubs;

/// <summary>
/// Tells SignalR which user owns a connection, using the "userId" query string.
/// This is what makes <c>Clients.User(id)</c> / <c>Clients.Users(ids)</c> work,
/// reaching every tab the user has open.
/// </summary>
public sealed class QueryStringUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var value = connection.GetHttpContext()?.Request.Query["userId"].ToString();
        return Guid.TryParse(value, out var id) ? id.ToString() : null;
    }
}
