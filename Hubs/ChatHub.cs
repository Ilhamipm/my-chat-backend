using Microsoft.AspNetCore.SignalR;
using ChatBackend.Services;

namespace ChatBackend.Hubs;

public class ChatHub : Hub
{
    private readonly UserStore _userStore;
    private readonly MessageService _messageService;
    private readonly MatchmakingService _matchmakingService;

    public ChatHub(UserStore userStore, MessageService messageService, MatchmakingService matchmakingService)
    {
        _userStore = userStore;
        _messageService = messageService;
        _matchmakingService = matchmakingService;
    }

    public async Task Register(string? preferredId = null)
    {
        string result = _userStore.RegisterUser(Context.ConnectionId, preferredId);
        await Clients.Caller.SendAsync("UserRegistered", result);
        await NotifyUnreadCount(result);
        await BroadcastOnlineUsers();
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
            await NotifyUnreadCount(newId);
            await BroadcastOnlineUsers();
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

    public async Task FetchUnreadMessages()
    {
        string? userId = _userStore.GetCustomId(Context.ConnectionId);
        if (userId != null)
        {
            await SendUnreadMessages(userId);
            // After fetching, notify with 0 unread
            await Clients.Caller.SendAsync("UnreadNotification", 0);
        }
    }

    private async Task NotifyUnreadCount(string userId)
    {
        int count = _messageService.CountUnreadMessages(userId);
        if (count > 0)
        {
            await Clients.Caller.SendAsync("UnreadNotification", count);
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

    public async Task JoinMatchmaking()
    {
        var result = _matchmakingService.AddToQueue(Context.ConnectionId);
        if (result.IsSearching)
        {
            await Clients.Caller.SendAsync("MatchmakingStatus", "Searching");
        }
        else if (result.PartnerId != null && result.Session != null)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("MatchFound", result.PartnerId, result.Session.ControllerId == Context.ConnectionId);
            await Clients.Client(result.PartnerId).SendAsync("MatchFound", Context.ConnectionId, result.Session.ControllerId == result.PartnerId);
        }
        await BroadcastOnlineUsers();
    }

    public async Task LeaveMatchmaking()
    {
        _matchmakingService.RemoveFromQueue(Context.ConnectionId);
        await Clients.Caller.SendAsync("MatchmakingStatus", "Idle");
        await BroadcastOnlineUsers();
    }

    public async Task StartGame()
    {
        var session = _matchmakingService.GetSession(Context.ConnectionId);
        if (session != null)
        {
            await Clients.Client(session.User1).SendAsync("GameStart");
            await Clients.Client(session.User2).SendAsync("GameStart");
            await BroadcastOnlineUsers();
        }
    }

    public async Task QuitGame()
    {
        var session = _matchmakingService.GetSession(Context.ConnectionId);
        if (session != null)
        {
            var partnerId = session.User1 == Context.ConnectionId ? session.User2 : session.User1;
            await Clients.Client(partnerId).SendAsync("PartnerDisconnected");
            _matchmakingService.EndSession(Context.ConnectionId);
            await BroadcastOnlineUsers();
        }
    }

    public async Task UpdateBallSpeed(float speed)
    {
        var session = _matchmakingService.GetSession(Context.ConnectionId);
        if (session != null && session.ControllerId == Context.ConnectionId)
        {
            await Clients.Client(session.User1 == Context.ConnectionId ? session.User2 : session.User1).SendAsync("UpdateSpeed", speed);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var session = _matchmakingService.GetSession(Context.ConnectionId);
        if (session != null)
        {
            var partnerId = session.User1 == Context.ConnectionId ? session.User2 : session.User1;
            await Clients.Client(partnerId).SendAsync("PartnerDisconnected");
            _matchmakingService.EndSession(Context.ConnectionId);
        }
        
        _userStore.RemoveUser(Context.ConnectionId);
        await BroadcastOnlineUsers();
        await base.OnDisconnectedAsync(exception);
    }

    private async Task BroadcastOnlineUsers()
    {
        var allUsers = _userStore.GetAllUsers();
        var userStatuses = allUsers.Select(u => new {
            Id = u.CustomId,
            Status = _matchmakingService.GetUserStatus(u.ConnectionId)
        }).ToList();
        
        await Clients.All.SendAsync("UpdateOnlineUsers", userStatuses);
    }
}
