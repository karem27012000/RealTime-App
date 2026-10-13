namespace ChatApp.Domain.Entities;

/// <summary>
/// Links a user to a room (many-to-many between <see cref="User"/> and <see cref="ChatRoom"/>).
/// </summary>
public class RoomMember
{
    // Required by EF Core.
    private RoomMember() { }

    internal RoomMember(Guid roomId, Guid userId)
    {
        RoomId = roomId;
        UserId = userId;
        JoinedAt = DateTime.UtcNow;
    }

    public Guid RoomId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime JoinedAt { get; private set; }

    public User? User { get; private set; }
}
