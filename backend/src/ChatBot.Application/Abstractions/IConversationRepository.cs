using ChatBot.Domain.Entities;

namespace ChatBot.Application.Abstractions;

/// <summary>
/// Data access for conversations. Implemented in the Infrastructure layer
/// so the Application layer does not depend on EF Core.
/// </summary>
public interface IConversationRepository
{
    Task<IReadOnlyList<Conversation>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Conversation?> GetWithMessagesAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default);

    void Remove(Conversation conversation);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
