using ChatApp.Application.Abstractions;
using ChatApp.Domain.Entities;

namespace ChatApp.Application.Tests.Fakes;

/// <summary>Simple in-memory repository so tests run without a database.</summary>
public sealed class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public User Add(string nickname)
    {
        var user = new User(nickname);
        Users.Add(user);
        return user;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByNicknameAsync(string nickname, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.FirstOrDefault(u => string.Equals(u.Nickname, nickname, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>(Users.ToList());

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
