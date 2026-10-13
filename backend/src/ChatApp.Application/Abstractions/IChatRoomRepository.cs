using ChatApp.Domain.Entities;

namespace ChatApp.Application.Abstractions;

/// <summary>A room together with its newest message (for the chat list preview).</summary>
public sealed record RoomSummary(ChatRoom Room, Message? LastMessage);

public interface IChatRoomRepository
{
    /// <summary>Loads a room with its members (not its messages).</summary>
    Task<ChatRoom?> GetWithMembersAsync(Guid roomId, CancellationToken cancellationToken = default);

    /// <summary>Rooms the user belongs to, with members and the latest message, newest activity first.</summary>
    Task<IReadOnlyList<RoomSummary>> GetRoomsForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Group rooms the user has not joined yet.</summary>
    Task<IReadOnlyList<RoomSummary>> GetJoinableGroupsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ChatRoom?> GetPrivateRoomAsync(string privateKey, CancellationToken cancellationToken = default);

    /// <summary>Messages of a room, newest first, older than <paramref name="before"/> when given.</summary>
    Task<IReadOnlyList<Message>> GetMessagesAsync(Guid roomId, DateTime? before, int take, CancellationToken cancellationToken = default);

    Task AddAsync(ChatRoom room, CancellationToken cancellationToken = default);

    Task AddMessageAsync(Message message, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
