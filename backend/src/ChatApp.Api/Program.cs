using ChatApp.Api.Extensions;
using ChatApp.Api.Hubs;
using ChatApp.Api.Middleware;
using ChatApp.Application;
using ChatApp.Application.Abstractions;
using ChatApp.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Scalar.AspNetCore;

const string AngularCorsPolicy = "Angular";

var builder = WebApplication.CreateBuilder(args);

// ----- Services -----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSignalR(options => options.AddFilter<HubExceptionFilter>());
builder.Services.AddSingleton<IUserIdProvider, QueryStringUserIdProvider>();
builder.Services.AddScoped<IChatNotifier, SignalRChatNotifier>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

    options.AddPolicy(AngularCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()); // SignalR needs credentials enabled
});

var app = builder.Build();

// ----- HTTP pipeline -----
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.MapScalarApiReference();    // /scalar  -> interactive API docs
}

app.UseCors(AngularCorsPolicy);

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

await app.ApplyMigrationsAsync();

app.Run();
