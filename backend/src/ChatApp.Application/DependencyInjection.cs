using ChatApp.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IMessageService, MessageService>();

        return services;
    }
}
