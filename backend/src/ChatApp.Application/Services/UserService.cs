using ChatApp.Application.Abstractions;
using ChatApp.Application.DTOs;
using ChatApp.Application.Exceptions;
using ChatApp.Application.Mappings;
using ChatApp.Domain.Entities;

namespace ChatApp.Application.Services;

public interface IUserService
{
    /// <summary>Returns the user with this nickname, creating them the first time.</summary>
    Task<UserDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Throws <see cref="UnauthorizedException"/> if the user does not exist.</summary>
    Task<UserDto> GetRequiredAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Called by the hub when a connection opens. Returns the user.</summary>
    Task<UserDto> ConnectAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default);

    /// <summary>Called by the hub when a connection closes.</summary>
    Task DisconnectAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default);
}

public sealed class UserService(
    IUserRepository users,
    IPresenceTracker presence,
    IChatNotifier notifier) : IUserService
{
    public async Task<UserDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var nickname = User.NormalizeNickname(request.Nickname);

        var user = await users.GetByNicknameAsync(nickname, cancellationToken);
        if (user is null)
        {
            user = new User(nickname);
            await users.AddAsync(user, cancellationToken);
            await users.SaveChangesAsync(cancellationToken);
        }

        return user.ToDto(presence);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var all = await users.GetAllAsync(cancellationToken);
        return all
            .Select(u => u.ToDto(presence))
            .OrderByDescending(u => u.IsOnline)
            .ThenBy(u => u.Nickname)
            .ToList();
    }

    public async Task<UserDto> GetRequiredAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken) ?? throw new UnauthorizedException();
        return user.ToDto(presence);
    }

    public async Task<UserDto> ConnectAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken) ?? throw new UnauthorizedException();

        if (presence.UserConnected(userId, connectionId))
        {
            await notifier.PresenceChangedAsync(user.ToDto(presence), cancellationToken);
        }

        return user.ToDto(presence);
    }

    public async Task DisconnectAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default)
    {
        if (!presence.UserDisconnected(userId, connectionId))
        {
            return; // still connected from another tab
        }

        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return;
        }

        user.MarkSeen();
        await users.SaveChangesAsync(cancellationToken);
        await notifier.PresenceChangedAsync(user.ToDto(presence), cancellationToken);
    }
}
