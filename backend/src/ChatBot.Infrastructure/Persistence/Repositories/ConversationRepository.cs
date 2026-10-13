using ChatBot.Application.Abstractions;
using ChatBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Infrastructure.Persistence.Repositories;

public sealed class ConversationRepository(AppDbContext dbContext) : IConversationRepository
{
    public async Task<IReadOnlyList<Conversation>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Conversations
            .AsNoTracking()
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);

    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Conversation?> GetWithMessagesAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default) =>
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);

    public void Remove(Conversation conversation) =>
        dbContext.Conversations.Remove(conversation);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
