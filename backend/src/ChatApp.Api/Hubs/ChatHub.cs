using ChatApp.Application.DTOs;
using ChatApp.Application.Exceptions;
using ChatApp.Application.Services;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Hubs;

/// <summary>
/// Real-time endpoint at /hubs/chat. The browser connects with ?userId=... and then:
/// <list type="bullet">
/// <item>calls <see cref="SendMessage"/> and <see cref="StartTyping"/>;</item>
/// <item>receives the events defined in <see cref="IChatClient"/>.</item>
/// </list>
/// </summary>
public sealed class ChatHub(
    IUserService userService,
    IRoomService roomService,
    IMessageService messageService) : Hub<IChatClient>
{
    public override async Task OnConnectedAsync()
    {
        try
        {
            await userService.ConnectAsync(CurrentUserId, Context.ConnectionId);
        }
        catch (UnauthorizedException)
        {
            Context.Abort(); // unknown user: close the connection
            return;
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Guid.TryParse(Context.UserIdentifier, out var userId))
        {
            await userService.DisconnectAsync(userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public Task<MessageDto> SendMessage(Guid roomId, string content) =>
        messageService.SendAsync(roomId, CurrentUserId, content, Context.ConnectionAborted);

    /// <summary>Tells the other members that this user is typing. Nothing is saved.</summary>
    public async Task StartTyping(Guid roomId)
    {
        var userId = CurrentUserId;
        var memberIds = await roomService.GetMemberIdsAsync(roomId, userId, Context.ConnectionAborted);
        var me = await userService.GetRequiredAsync(userId, Context.ConnectionAborted);

        var others = memberIds.Where(id => id != userId).Select(id => id.ToString()).ToList();
        await Clients.Users(others).UserTyping(new TypingDto(roomId, userId, me.Nickname));
    }

    private Guid CurrentUserId =>
        Guid.TryParse(Context.UserIdentifier, out var id) ? id : throw new UnauthorizedException();
}
