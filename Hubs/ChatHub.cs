using Microsoft.AspNetCore.SignalR;
using ChatBackend.Services;

namespace ChatBackend.Hubs;

public class ChatHub : Hub
{
    private readonly UserStore _userStore;
    private readonly MessageService _messageService;

    public ChatHub(UserStore userStore, MessageService messageService)
    {
        _userStore = userStore;
        _messageService = messageService;
    }

    public async Task Register(string? preferredId = null)
    {
        string result = _userStore.RegisterUser(Context.ConnectionId, preferredId);
        await Clients.Caller.SendAsync("UserRegistered", result);
        await SendUnreadMessages(result);
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
            await SendUnreadMessages(newId);
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
        else if (senderId != null)
        {
            _messageService.SaveUnreadMessage(targetId, senderId, message);
            await Clients.Caller.SendAsync("Error", $"ID {targetId} sedang offline. Pesan Anda telah disimpan dan akan disampaikan saat dia online.");
        }
        else
        {
            await Clients.Caller.SendAsync("Error", "Gagal mengirim pesan. Silakan coba lagi.");
        }
    }

    private async Task SendUnreadMessages(string userId)
    {
        var unread = _messageService.RetrieveAndMoveUnreadMessages(userId);
        foreach (var msg in unread)
        {
            // We use the same method to receive messages as live messages
            await Clients.Caller.SendAsync("ReceiveMessage", (string)msg.SenderId, (string)msg.Message);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _userStore.RemoveUser(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
