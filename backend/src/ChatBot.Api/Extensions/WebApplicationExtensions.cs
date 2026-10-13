using ChatBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Api.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Creates the SQLite database and applies any pending EF Core migrations on startup.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
