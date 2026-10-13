using ChatBot.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ChatBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IChatService, ChatService>();

        return services;
    }
}
