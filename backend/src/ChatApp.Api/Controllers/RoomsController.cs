using ChatApp.Application.DTOs;
using ChatApp.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Controllers;

/// <summary>
/// Chat rooms of the current user (send the "X-User-Id" header).
/// </summary>
[Route("api/rooms")]
public sealed class RoomsController(IRoomService roomService, IMessageService messageService) : ApiControllerBase
{
    /// <summary>Private chats and groups the current user belongs to, most recent activity first.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RoomDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyRooms(CancellationToken cancellationToken) =>
        Ok(await roomService.GetMyRoomsAsync(CurrentUserId, cancellationToken));

    /// <summary>Group rooms the current user can join.</summary>
    [HttpGet("groups")]
    [ProducesResponseType<IReadOnlyList<RoomDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJoinableGroups(CancellationToken cancellationToken) =>
        Ok(await roomService.GetJoinableGroupsAsync(CurrentUserId, cancellationToken));

    /// <summary>Room details with its members.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<RoomDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(Guid id, CancellationToken cancellationToken) =>
        Ok(await roomService.GetDetailsAsync(id, CurrentUserId, cancellationToken));

    /// <summary>Creates a new group; the creator becomes its first member.</summary>
    [HttpPost("groups")]
    [ProducesResponseType<RoomDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGroup(CreateGroupRequest request, CancellationToken cancellationToken)
    {
        var room = await roomService.CreateGroupAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetDetails), new { id = room.Id }, room);
    }

    /// <summary>Opens the private chat with another user (creates it the first time).</summary>
    [HttpPost("private")]
    [ProducesResponseType<RoomDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartPrivateChat(StartPrivateChatRequest request, CancellationToken cancellationToken) =>
        Ok(await roomService.StartPrivateChatAsync(CurrentUserId, request, cancellationToken));

    /// <summary>Joins a group room.</summary>
    [HttpPost("{id:guid}/join")]
    [ProducesResponseType<RoomDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Join(Guid id, CancellationToken cancellationToken) =>
        Ok(await roomService.JoinGroupAsync(id, CurrentUserId, cancellationToken));

    /// <summary>Leaves a group room.</summary>
    [HttpPost("{id:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Leave(Guid id, CancellationToken cancellationToken)
    {
        await roomService.LeaveGroupAsync(id, CurrentUserId, cancellationToken);
        return NoContent();
    }

    /// <summary>Messages of a room, oldest first. Pass <c>before</c> (the oldest message date you have) to load older ones.</summary>
    [HttpGet("{id:guid}/messages")]
    [ProducesResponseType<IReadOnlyList<MessageDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMessages(Guid id, [FromQuery] DateTime? before, [FromQuery] int take = 50, CancellationToken cancellationToken = default) =>
        Ok(await messageService.GetMessagesAsync(id, CurrentUserId, before, take, cancellationToken));

    /// <summary>
    /// Sends a message. Members receive it in real time over SignalR.
    /// (The Angular app sends through the hub instead; this endpoint is handy for testing.)
    /// </summary>
    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType<MessageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendMessage(Guid id, SendMessageRequest request, CancellationToken cancellationToken) =>
        Ok(await messageService.SendAsync(id, CurrentUserId, request.Content, cancellationToken));
}
