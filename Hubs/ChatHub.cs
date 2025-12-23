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

    public async Task Register(string? preferredId = null)
    {
        string result = _userStore.RegisterUser(Context.ConnectionId, preferredId);
        await Clients.Caller.SendAsync("UserRegistered", result);
    }

    public async Task ChangeId(string newId)
    {
        if (string.IsNullOrWhiteSpace(newId) || newId.Length > 20)
        {
            await Clients.Caller.SendAsync("Error", "Invalid ID. Use 1-20 characters.");
            return;
        }

        bool success = _userStore.ChangeId(Context.ConnectionId, newId);
        if (success)
        {
            await Clients.Caller.SendAsync("UserRegistered", newId);
        }
        else
        {
            await Clients.Caller.SendAsync("Error", "This ID is already taken.");
        }
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
