using Anthropic;
using ChatBot.Application.Abstractions;
using ChatBot.Infrastructure.AI;
using ChatBot.Infrastructure.Persistence;
using ChatBot.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChatBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is missing.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IConversationRepository, ConversationRepository>();

        // AI provider
        var section = configuration.GetSection(ChatBotOptions.SectionName);
        services.Configure<ChatBotOptions>(section);

        var chatBotOptions = section.Get<ChatBotOptions>() ?? new ChatBotOptions();
        var useClaude = string.Equals(chatBotOptions.Provider, "Claude", StringComparison.OrdinalIgnoreCase);

        if (useClaude)
        {
            services.AddSingleton(sp =>
            {
                var claude = sp.GetRequiredService<IOptions<ChatBotOptions>>().Value.Claude;
                var apiKey = string.IsNullOrWhiteSpace(claude.ApiKey)
                    ? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
                    : claude.ApiKey;

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    throw new InvalidOperationException(
                        "ChatBot:Provider is 'Claude' but no API key was found. " +
                        "Set ChatBot:Claude:ApiKey with user-secrets or the ANTHROPIC_API_KEY environment variable.");
                }

                return new AnthropicClient { ApiKey = apiKey };
            });
            services.AddScoped<IChatCompletionService, ClaudeChatCompletionService>();
        }
        else
        {
            services.AddSingleton<IChatCompletionService, DemoChatCompletionService>();
        }

        return services;
    }
}
