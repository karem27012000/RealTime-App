using ChatApp.Application.Abstractions;
using ChatApp.Application.DTOs;
using ChatApp.Application.Exceptions;
using ChatApp.Application.Mappings;
using ChatApp.Domain.Entities;

namespace ChatApp.Application.Services;

public interface IMessageService
{
    /// <summary>Returns up to <paramref name="take"/> messages, oldest first. Use <paramref name="before"/> to load older pages.</summary>
    Task<IReadOnlyList<MessageDto>> GetMessagesAsync(Guid roomId, Guid userId, DateTime? before, int take, CancellationToken cancellationToken = default);

    /// <summary>Saves the message and pushes it in real time to every member of the room.</summary>
    Task<MessageDto> SendAsync(Guid roomId, Guid userId, string content, CancellationToken cancellationToken = default);
}

public sealed class MessageService(
    IChatRoomRepository rooms,
    IUserRepository users,
    IChatNotifier notifier) : IMessageService
{
    public const int MaxPageSize = 100;

    public async Task<IReadOnlyList<MessageDto>> GetMessagesAsync(
        Guid roomId, Guid userId, DateTime? before, int take, CancellationToken cancellationToken = default)
    {
        var room = await rooms.GetWithMembersAsync(roomId, cancellationToken)
            ?? throw new NotFoundException(nameof(ChatRoom), roomId);

        if (!room.IsMember(userId))
        {
            throw new ForbiddenException("You are not a member of this room.");
        }

        var page = await rooms.GetMessagesAsync(roomId, before, Math.Clamp(take, 1, MaxPageSize), cancellationToken);

        // The repository returns newest first (for paging); the UI wants oldest first.
        return page.Reverse().Select(m => m.ToDto()).ToList();
    }

    public async Task<MessageDto> SendAsync(Guid roomId, Guid userId, string content, CancellationToken cancellationToken = default)
    {
        var sender = await users.GetByIdAsync(userId, cancellationToken) ?? throw new UnauthorizedException();

        var room = await rooms.GetWithMembersAsync(roomId, cancellationToken)
            ?? throw new NotFoundException(nameof(ChatRoom), roomId);

        if (!room.IsMember(userId))
        {
            throw new ForbiddenException("You are not a member of this room.");
        }

        var message = room.PostMessage(userId, content);
        await rooms.AddMessageAsync(message, cancellationToken);
        await rooms.SaveChangesAsync(cancellationToken);

        var dto = message.ToDto(sender.Nickname);
        await notifier.MessageReceivedAsync(dto, room.Members.Select(m => m.UserId), cancellationToken);
        return dto;
    }
}
