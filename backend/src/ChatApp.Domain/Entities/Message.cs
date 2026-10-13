using ChatApp.Domain.Common;

namespace ChatApp.Domain.Entities;

public class Message : BaseEntity
{
    public const int ContentMaxLength = 2000;

    // Required by EF Core.
    private Message() { }

    internal Message(Guid roomId, Guid senderId, string content)
    {
        RoomId = roomId;
        SenderId = senderId;
        Content = content;
    }

    public Guid RoomId { get; private set; }

    public Guid SenderId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public User? Sender { get; private set; }
}
