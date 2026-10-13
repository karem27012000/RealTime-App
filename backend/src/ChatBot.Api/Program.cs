using ChatBot.Api.Extensions;
using ChatBot.Api.Hubs;
using ChatBot.Api.Middleware;
using ChatBot.Application;
using ChatBot.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Scalar.AspNetCore;

const string AngularCorsPolicy = "Angular";

var builder = WebApplication.CreateBuilder(args);

// ----- Services -----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddSignalR(options => options.AddFilter<HubExceptionFilter>());
builder.Services.AddOpenApi();

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
