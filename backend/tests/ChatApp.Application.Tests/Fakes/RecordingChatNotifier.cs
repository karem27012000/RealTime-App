using ChatApp.Application.Abstractions;
using ChatApp.Application.DTOs;

namespace ChatApp.Application.Tests.Fakes;

/// <summary>Records every real-time event instead of sending it, so tests can check them.</summary>
public sealed class RecordingChatNotifier : IChatNotifier
{
    public List<(MessageDto Message, List<Guid> Recipients)> Messages { get; } = [];

    public List<(Guid UserId, RoomDto Room)> RoomsAdded { get; } = [];

    public List<UserDto> PresenceChanges { get; } = [];

    public Task MessageReceivedAsync(MessageDto message, IEnumerable<Guid> recipientIds, CancellationToken cancellationToken = default)
    {
        Messages.Add((message, recipientIds.ToList()));
        return Task.CompletedTask;
    }

    public Task RoomAddedAsync(Guid userId, RoomDto room, CancellationToken cancellationToken = default)
    {
        RoomsAdded.Add((userId, room));
        return Task.CompletedTask;
    }

    public Task MembersChangedAsync(Guid roomId, int memberCount, IEnumerable<Guid> recipientIds, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task PresenceChangedAsync(UserDto user, CancellationToken cancellationToken = default)
    {
        PresenceChanges.Add(user);
        return Task.CompletedTask;
    }
}
