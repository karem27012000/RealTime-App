using ChatApp.Application.Abstractions;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;

namespace ChatApp.Application.Tests.Fakes;

/// <param name="users">Used to fill in <see cref="RoomMember.User"/>, like EF Core's Include() does.</param>
public sealed class FakeChatRoomRepository(FakeUserRepository users) : IChatRoomRepository
{
    public List<ChatRoom> Rooms { get; } = [];

    public List<Message> Messages { get; } = [];

    public Task<ChatRoom?> GetWithMembersAsync(Guid roomId, CancellationToken cancellationToken = default) =>
        Task.FromResult(LoadUsers(Rooms.FirstOrDefault(r => r.Id == roomId)));

    public Task<IReadOnlyList<RoomSummary>> GetRoomsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Summaries(Rooms.Where(r => r.IsMember(userId))));

    public Task<IReadOnlyList<RoomSummary>> GetJoinableGroupsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Summaries(Rooms.Where(r => r.Type == RoomType.Group && !r.IsMember(userId))));

    public Task<ChatRoom?> GetPrivateRoomAsync(string privateKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(LoadUsers(Rooms.FirstOrDefault(r => r.PrivateKey == privateKey)));

    public Task<IReadOnlyList<Message>> GetMessagesAsync(Guid roomId, DateTime? before, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Message>>(Messages
            .Where(m => m.RoomId == roomId && (before == null || m.CreatedAt < before))
            .OrderByDescending(m => m.CreatedAt)
            .Take(take)
            .ToList());

    public Task AddAsync(ChatRoom room, CancellationToken cancellationToken = default)
    {
        Rooms.Add(room);
        return Task.CompletedTask;
    }

    public Task AddMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    private IReadOnlyList<RoomSummary> Summaries(IEnumerable<ChatRoom> rooms) =>
        rooms.Select(r => new RoomSummary(
                LoadUsers(r)!,
                Messages.Where(m => m.RoomId == r.Id).OrderByDescending(m => m.CreatedAt).FirstOrDefault()))
            .ToList();

    private ChatRoom? LoadUsers(ChatRoom? room)
    {
        // RoomMember.User has a private setter (only EF Core sets it), so tests use reflection.
        var userProperty = typeof(RoomMember).GetProperty(nameof(RoomMember.User))!;
        foreach (var member in room?.Members ?? [])
        {
            userProperty.SetValue(member, users.Users.FirstOrDefault(u => u.Id == member.UserId));
        }

        return room;
    }
}
