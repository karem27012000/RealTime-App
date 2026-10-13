using ChatApp.Application.Abstractions;
using ChatApp.Infrastructure.Persistence;
using ChatApp.Infrastructure.Persistence.Repositories;
using ChatApp.Infrastructure.Presence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is missing.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IChatRoomRepository, ChatRoomRepository>();

        services.AddSingleton<IPresenceTracker, InMemoryPresenceTracker>();

        return services;
    }
}
