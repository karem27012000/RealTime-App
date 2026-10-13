using ChatApp.Domain.Common;
using ChatApp.Domain.Exceptions;

namespace ChatApp.Domain.Entities;

/// <summary>
/// A person using the chat. Users are identified only by a unique nickname (no password).
/// </summary>
public class User : BaseEntity
{
    public const int NicknameMinLength = 2;
    public const int NicknameMaxLength = 30;

    // Required by EF Core.
    private User() { }

    public User(string nickname)
    {
        Nickname = NormalizeNickname(nickname);
        LastSeenAt = CreatedAt;
    }

    public string Nickname { get; private set; } = string.Empty;

    public DateTime LastSeenAt { get; private set; }

    public void MarkSeen() => LastSeenAt = DateTime.UtcNow;

    public static string NormalizeNickname(string? nickname)
    {
        var value = nickname?.Trim() ?? string.Empty;

        if (value.Length < NicknameMinLength || value.Length > NicknameMaxLength)
        {
            throw new DomainException(
                $"Nickname must be between {NicknameMinLength} and {NicknameMaxLength} characters.");
        }

        if (!value.All(c => char.IsLetterOrDigit(c) || c is '_' or '-' or '.' or ' '))
        {
            throw new DomainException("Nickname can only contain letters, numbers, spaces, '_', '-' and '.'.");
        }

        return value;
    }
}
