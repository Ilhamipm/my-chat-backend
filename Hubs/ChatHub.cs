using Microsoft.AspNetCore.SignalR;
using ChatBackend.Services;

namespace ChatBackend.Hubs;

public class ChatHub : Hub
{
    private readonly UserStore _userStore;

    public ChatHub(UserStore userStore)
    {
        _userStore = userStore;
    }

    public async Task Register()
    {
        string customId = _userStore.RegisterUser(Context.ConnectionId);
        await Clients.Caller.SendAsync("UserRegistered", customId);
    }

    public async Task SendMessageToId(string targetId, string message)
    {
        string? targetConnectionId = _userStore.GetConnectionId(targetId);
        string? senderId = _userStore.GetCustomId(Context.ConnectionId);

        if (targetConnectionId != null && senderId != null)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveMessage", senderId, message);
        }
        else
        {
            await Clients.Caller.SendAsync("Error", "Target ID not found or registration required.");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _userStore.RemoveUser(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
