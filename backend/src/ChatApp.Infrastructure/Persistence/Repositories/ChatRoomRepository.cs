using ChatApp.Application.Abstractions;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Persistence.Repositories;

public sealed class ChatRoomRepository(AppDbContext dbContext) : IChatRoomRepository
{
    public Task<ChatRoom?> GetWithMembersAsync(Guid roomId, CancellationToken cancellationToken = default) =>
        dbContext.Rooms
            .Include(r => r.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);

    public async Task<IReadOnlyList<RoomSummary>> GetRoomsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var rooms = await RoomsWithMembers()
            .Where(r => r.Members.Any(m => m.UserId == userId))
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(cancellationToken);

        return await WithLastMessagesAsync(rooms, cancellationToken);
    }

    public async Task<IReadOnlyList<RoomSummary>> GetJoinableGroupsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var rooms = await RoomsWithMembers()
            .Where(r => r.Type == RoomType.Group && !r.Members.Any(m => m.UserId == userId))
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return await WithLastMessagesAsync(rooms, cancellationToken);
    }

    public Task<ChatRoom?> GetPrivateRoomAsync(string privateKey, CancellationToken cancellationToken = default) =>
        dbContext.Rooms
            .Include(r => r.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(r => r.PrivateKey == privateKey, cancellationToken);

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(Guid roomId, DateTime? before, int take, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Messages
            .AsNoTracking()
            .Include(m => m.Sender)
            .Where(m => m.RoomId == roomId);

        if (before is not null)
        {
            query = query.Where(m => m.CreatedAt < before);
        }

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ChatRoom room, CancellationToken cancellationToken = default) =>
        await dbContext.Rooms.AddAsync(room, cancellationToken);

    public async Task AddMessageAsync(Message message, CancellationToken cancellationToken = default) =>
        await dbContext.Messages.AddAsync(message, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<ChatRoom> RoomsWithMembers() =>
        dbContext.Rooms
            .AsNoTracking()
            .Include(r => r.Members).ThenInclude(m => m.User);

    /// <summary>
    /// Loads the newest message of each room with one extra query
    /// (SQLite can't do "top 1 per group" inside an Include).
    /// </summary>
    private async Task<IReadOnlyList<RoomSummary>> WithLastMessagesAsync(List<ChatRoom> rooms, CancellationToken cancellationToken)
    {
        var roomIds = rooms.Select(r => r.Id).ToList();

        var lastMessages = await dbContext.Messages
            .AsNoTracking()
            .Include(m => m.Sender)
            .Where(m => roomIds.Contains(m.RoomId)
                && m.CreatedAt == dbContext.Messages.Where(x => x.RoomId == m.RoomId).Max(x => x.CreatedAt))
            .ToListAsync(cancellationToken);

        var byRoom = lastMessages
            .GroupBy(m => m.RoomId)
            .ToDictionary(g => g.Key, g => g.First());

        return rooms.Select(r => new RoomSummary(r, byRoom.GetValueOrDefault(r.Id))).ToList();
    }
}
