using ChatBot.Application.Abstractions;
using ChatBot.Domain.Entities;

namespace ChatBot.Application.Tests.Fakes;

/// <summary>
/// Simple in-memory repository so tests run without a database.
/// </summary>
public sealed class InMemoryConversationRepository : IConversationRepository
{
    public List<Conversation> Conversations { get; } = [];

    public int SaveChangesCalls { get; private set; }

    public Task<IReadOnlyList<Conversation>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Conversation>>(Conversations.OrderByDescending(c => c.UpdatedAt).ToList());

    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Conversations.FirstOrDefault(c => c.Id == id));

    public Task<Conversation?> GetWithMessagesAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, cancellationToken);

    public Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        Conversations.Add(conversation);
        return Task.CompletedTask;
    }

    public void Remove(Conversation conversation) => Conversations.Remove(conversation);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalls++;
        return Task.CompletedTask;
    }
}
