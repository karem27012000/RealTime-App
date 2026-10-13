using ChatBot.Application.DTOs;
using ChatBot.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatBot.Api.Controllers;

[ApiController]
[Route("api/conversations")]
[Produces("application/json")]
public sealed class ConversationsController(
    IConversationService conversationService,
    IChatService chatService) : ControllerBase
{
    /// <summary>Lists all conversations, newest first.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ConversationDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await conversationService.GetAllAsync(cancellationToken));

    /// <summary>Gets one conversation with all of its messages.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ConversationDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await conversationService.GetByIdAsync(id, cancellationToken));

    /// <summary>Starts a new conversation.</summary>
    [HttpPost]
    [ProducesResponseType<ConversationDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateConversationRequest request, CancellationToken cancellationToken)
    {
        var conversation = await conversationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = conversation.Id }, conversation);
    }

    /// <summary>Renames a conversation.</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType<ConversationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rename(Guid id, RenameConversationRequest request, CancellationToken cancellationToken) =>
        Ok(await conversationService.RenameAsync(id, request, cancellationToken));

    /// <summary>Deletes a conversation and its messages.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await conversationService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Sends a message and waits for the full bot reply.
    /// The Angular app uses the SignalR hub (/hubs/chat) instead, to stream the reply as it is written.
    /// </summary>
    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType<MessageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage(Guid id, SendMessageRequest request, CancellationToken cancellationToken) =>
        Ok(await chatService.SendMessageAsync(id, request.Content, cancellationToken));
}
