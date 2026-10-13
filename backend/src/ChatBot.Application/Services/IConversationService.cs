using ChatBot.Application.DTOs;

namespace ChatBot.Application.Services;

public interface IConversationService
{
    Task<IReadOnlyList<ConversationDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ConversationDetailsDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ConversationDto> CreateAsync(CreateConversationRequest request, CancellationToken cancellationToken = default);

    Task<ConversationDto> RenameAsync(Guid id, RenameConversationRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
