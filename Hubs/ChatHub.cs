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

    public async Task FindMatch()
    {
        string? userId = _userStore.GetCustomId(Context.ConnectionId);
        if (userId == null) return;

        bool joined = _matchmakingService.JoinQueue(Context.ConnectionId);
        if (joined)
        {
            await Clients.Caller.SendAsync("WaitingForMatch");
            var (p1, p2) = _matchmakingService.TryMatch();
            if (p1 != null && p2 != null)
            {
                await Clients.Clients(p1, p2).SendAsync("MatchFound");
            }
        }
    }

    public async Task SetRolePreference(bool wantControl)
    {
        _matchmakingService.SetPreference(Context.ConnectionId, wantControl);
        string? opponent = _matchmakingService.GetOpponent(Context.ConnectionId);
        
        if (opponent != null)
        {
            // If both set preference, resolve roles
            var (controller, follower) = _matchmakingService.ResolveRoles(Context.ConnectionId, opponent);
            await Clients.Client(controller).SendAsync("RoleAssigned", true);
            await Clients.Client(follower).SendAsync("RoleAssigned", false);
        }
    }

    public async Task StartGame()
    {
        string? opponent = _matchmakingService.GetOpponent(Context.ConnectionId);
        if (opponent != null)
        {
            await Clients.Clients(Context.ConnectionId, opponent).SendAsync("GameStarted");
        }
    }

    public async Task UpdateBallSpeed(float speed)
    {
        string? opponent = _matchmakingService.GetOpponent(Context.ConnectionId);
        if (opponent != null)
        {
            await Clients.Client(opponent).SendAsync("BallSpeedUpdated", speed);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _matchmakingService.RemoveUser(Context.ConnectionId);
        _userStore.RemoveUser(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
