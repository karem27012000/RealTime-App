namespace ChatBot.Domain.Common;

/// <summary>
/// Base class for every entity: gives it an Id and a creation timestamp.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
}
