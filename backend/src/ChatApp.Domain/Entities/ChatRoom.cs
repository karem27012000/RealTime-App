using ChatApp.Domain.Common;
using ChatApp.Domain.Enums;
using ChatApp.Domain.Exceptions;

namespace ChatApp.Domain.Entities;

/// <summary>
/// A place where messages are exchanged: either a private chat between two users
/// or a named group. All membership and messaging rules live here.
/// </summary>
public class ChatRoom : BaseEntity
{
    public const int NameMaxLength = 50;

    private readonly List<RoomMember> _members = [];

    // Required by EF Core.
    private ChatRoom() { }

    private ChatRoom(RoomType type, string? name, Guid createdById, string? privateKey)
    {
        Type = type;
        Name = name;
        CreatedById = createdById;
        PrivateKey = privateKey;
        UpdatedAt = CreatedAt;
    }

    public RoomType Type { get; private set; }

    /// <summary>Group name. Null for private chats (the UI shows the other user's nickname).</summary>
    public string? Name { get; private set; }

    public Guid CreatedById { get; private set; }

    /// <summary>
    /// For private chats only: both user ids in a fixed order, so the database can
    /// guarantee there is only one private chat per pair of users.
    /// </summary>
    public string? PrivateKey { get; private set; }

    /// <summary>Time of the last activity, used to sort the chat list.</summary>
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<RoomMember> Members => _members.AsReadOnly();

    public static ChatRoom CreateGroup(string? name, Guid creatorId)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length is < 2 or > NameMaxLength)
        {
            throw new DomainException($"Group name must be between 2 and {NameMaxLength} characters.");
        }

        var room = new ChatRoom(RoomType.Group, trimmed, creatorId, privateKey: null);
        room._members.Add(new RoomMember(room.Id, creatorId));
        return room;
    }

    public static ChatRoom CreatePrivate(Guid userId, Guid otherUserId)
    {
        if (userId == otherUserId)
        {
            throw new DomainException("You cannot start a private chat with yourself.");
        }

        var room = new ChatRoom(RoomType.Private, name: null, userId, BuildPrivateKey(userId, otherUserId));
        room._members.Add(new RoomMember(room.Id, userId));
        room._members.Add(new RoomMember(room.Id, otherUserId));
        return room;
    }

    public static string BuildPrivateKey(Guid a, Guid b) =>
        a.CompareTo(b) < 0 ? $"{a}:{b}" : $"{b}:{a}";

    public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);

    public void Join(Guid userId)
    {
        if (Type != RoomType.Group)
        {
            throw new DomainException("You can only join group rooms.");
        }

        if (!IsMember(userId))
        {
            _members.Add(new RoomMember(Id, userId));
        }
    }

    public void Leave(Guid userId)
    {
        if (Type != RoomType.Group)
        {
            throw new DomainException("You cannot leave a private chat.");
        }

        _members.RemoveAll(m => m.UserId == userId);
    }

    /// <summary>
    /// Creates a new message from a member of this room. The caller is responsible for saving it.
    /// </summary>
    public Message PostMessage(Guid senderId, string? content)
    {
        if (!IsMember(senderId))
        {
            throw new DomainException("You are not a member of this room.");
        }

        var text = content?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            throw new DomainException("Message cannot be empty.");
        }

        if (text.Length > Message.ContentMaxLength)
        {
            throw new DomainException($"Message cannot be longer than {Message.ContentMaxLength} characters.");
        }

        UpdatedAt = DateTime.UtcNow;
        return new Message(Id, senderId, text);
    }
}
