using ChatApp.Application.Abstractions;
using ChatApp.Application.DTOs;
using ChatApp.Application.Exceptions;
using ChatApp.Application.Mappings;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;

namespace ChatApp.Application.Services;

public interface IRoomService
{
    Task<IReadOnlyList<RoomDto>> GetMyRoomsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoomDto>> GetJoinableGroupsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<RoomDetailsDto> GetDetailsAsync(Guid roomId, Guid userId, CancellationToken cancellationToken = default);

    Task<RoomDto> CreateGroupAsync(Guid userId, CreateGroupRequest request, CancellationToken cancellationToken = default);

    Task<RoomDto> StartPrivateChatAsync(Guid userId, StartPrivateChatRequest request, CancellationToken cancellationToken = default);

    Task<RoomDto> JoinGroupAsync(Guid roomId, Guid userId, CancellationToken cancellationToken = default);

    Task LeaveGroupAsync(Guid roomId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Ids of all members, after checking that <paramref name="userId"/> is one of them.</summary>
    Task<IReadOnlyList<Guid>> GetMemberIdsAsync(Guid roomId, Guid userId, CancellationToken cancellationToken = default);
}

public sealed class RoomService(
    IChatRoomRepository rooms,
    IUserRepository users,
    IPresenceTracker presence,
    IChatNotifier notifier) : IRoomService
{
    public async Task<IReadOnlyList<RoomDto>> GetMyRoomsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var list = await rooms.GetRoomsForUserAsync(userId, cancellationToken);
        return list.Select(s => s.Room.ToDto(userId, s.LastMessage)).ToList();
    }

    public async Task<IReadOnlyList<RoomDto>> GetJoinableGroupsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var list = await rooms.GetJoinableGroupsAsync(userId, cancellationToken);
        return list.Select(s => s.Room.ToDto(userId, s.LastMessage)).ToList();
    }

    public async Task<RoomDetailsDto> GetDetailsAsync(Guid roomId, Guid userId, CancellationToken cancellationToken = default)
    {
        var room = await GetRoomForMemberAsync(roomId, userId, cancellationToken);
        return room.ToDetailsDto(userId, presence);
    }

    public async Task<RoomDto> CreateGroupAsync(Guid userId, CreateGroupRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureUserExistsAsync(userId, cancellationToken);

        var room = ChatRoom.CreateGroup(request.Name, userId);
        await rooms.AddAsync(room, cancellationToken);
        await rooms.SaveChangesAsync(cancellationToken);

        var dto = (await ReloadAsync(room.Id, cancellationToken)).ToDto(userId);
        await notifier.RoomAddedAsync(userId, dto, cancellationToken);
        return dto;
    }

    public async Task<RoomDto> StartPrivateChatAsync(Guid userId, StartPrivateChatRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureUserExistsAsync(userId, cancellationToken);
        _ = await users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        // Reuse the existing private chat between these two users, if there is one.
        var existing = await rooms.GetPrivateRoomAsync(ChatRoom.BuildPrivateKey(userId, request.UserId), cancellationToken);
        if (existing is not null)
        {
            return existing.ToDto(userId);
        }

        var room = ChatRoom.CreatePrivate(userId, request.UserId);
        await rooms.AddAsync(room, cancellationToken);
        await rooms.SaveChangesAsync(cancellationToken);

        var saved = await ReloadAsync(room.Id, cancellationToken);
        await notifier.RoomAddedAsync(userId, saved.ToDto(userId), cancellationToken);
        await notifier.RoomAddedAsync(request.UserId, saved.ToDto(request.UserId), cancellationToken);

        return saved.ToDto(userId);
    }

    public async Task<RoomDto> JoinGroupAsync(Guid roomId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureUserExistsAsync(userId, cancellationToken);

        var room = await rooms.GetWithMembersAsync(roomId, cancellationToken)
            ?? throw new NotFoundException(nameof(ChatRoom), roomId);

        room.Join(userId);
        await rooms.SaveChangesAsync(cancellationToken);

        var saved = await ReloadAsync(roomId, cancellationToken);
        var dto = saved.ToDto(userId);
        await notifier.RoomAddedAsync(userId, dto, cancellationToken);
        await notifier.MembersChangedAsync(roomId, saved.Members.Count, saved.Members.Select(m => m.UserId), cancellationToken);
        return dto;
    }

    public async Task LeaveGroupAsync(Guid roomId, Guid userId, CancellationToken cancellationToken = default)
    {
        var room = await GetRoomForMemberAsync(roomId, userId, cancellationToken);

        room.Leave(userId);
        await rooms.SaveChangesAsync(cancellationToken);

        await notifier.MembersChangedAsync(roomId, room.Members.Count, room.Members.Select(m => m.UserId), cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetMemberIdsAsync(Guid roomId, Guid userId, CancellationToken cancellationToken = default)
    {
        var room = await GetRoomForMemberAsync(roomId, userId, cancellationToken);
        return room.Members.Select(m => m.UserId).ToList();
    }

    private async Task<ChatRoom> GetRoomForMemberAsync(Guid roomId, Guid userId, CancellationToken cancellationToken)
    {
        var room = await rooms.GetWithMembersAsync(roomId, cancellationToken)
            ?? throw new NotFoundException(nameof(ChatRoom), roomId);

        if (!room.IsMember(userId))
        {
            throw new ForbiddenException("You are not a member of this room.");
        }

        return room;
    }

    private async Task<ChatRoom> ReloadAsync(Guid roomId, CancellationToken cancellationToken) =>
        await rooms.GetWithMembersAsync(roomId, cancellationToken)
            ?? throw new NotFoundException(nameof(ChatRoom), roomId);

    private async Task EnsureUserExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        _ = await users.GetByIdAsync(userId, cancellationToken) ?? throw new UnauthorizedException();
    }
}
