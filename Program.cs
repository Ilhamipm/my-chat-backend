using ChatBackend.Hubs;
using ChatBackend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<MessageService>();
builder.Services.AddSingleton<MatchmakingService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors();

app.MapHub<ChatHub>("/chatHub");

// Removed the conflicting root MapGet
app.MapGet("/api/status", () => "Chat Backend is running!");

app.Run();
