using ChatBot.Application.Abstractions;
using ChatBot.Application.DTOs;
using ChatBot.Application.Exceptions;
using ChatBot.Application.Mappings;
using ChatBot.Domain.Entities;

namespace ChatBot.Application.Services;

public sealed class ConversationService(IConversationRepository repository) : IConversationService
{
    public async Task<IReadOnlyList<ConversationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var conversations = await repository.GetAllAsync(cancellationToken);
        return conversations.Select(c => c.ToDto()).ToList();
    }

    public async Task<ConversationDetailsDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var conversation = await repository.GetWithMessagesAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Conversation), id);

        return conversation.ToDetailsDto();
    }

    public async Task<ConversationDto> CreateAsync(CreateConversationRequest request, CancellationToken cancellationToken = default)
    {
        var conversation = new Conversation(request.Title);

        await repository.AddAsync(conversation, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return conversation.ToDto();
    }

    public async Task<ConversationDto> RenameAsync(Guid id, RenameConversationRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ValidationException("Title is required.");
        }

        var conversation = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Conversation), id);

        conversation.Rename(request.Title);
        await repository.SaveChangesAsync(cancellationToken);

        return conversation.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var conversation = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Conversation), id);

        repository.Remove(conversation);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
