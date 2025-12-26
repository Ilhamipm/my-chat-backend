using System.Collections.Concurrent;

namespace ChatBackend.Services;

public class MatchmakingService
{
    private readonly ConcurrentQueue<string> _queue = new();
    private readonly ConcurrentDictionary<string, GameSession> _activeSessions = new();

    public MatchmakingResult AddToQueue(string connectionId)
    {
        if (_activeSessions.ContainsKey(connectionId) || _queue.Contains(connectionId))
        {
            return new MatchmakingResult { IsSearching = true };
        }

        if (_queue.TryDequeue(out var partnerId))
        {
            var sessionId = Guid.NewGuid().ToString();
            var session = new GameSession
            {
                SessionId = sessionId,
                User1 = connectionId,
                User2 = partnerId,
                ControllerId = new Random().Next(0, 2) == 0 ? connectionId : partnerId
            };

            _activeSessions[connectionId] = session;
            _activeSessions[partnerId] = session;

            return new MatchmakingResult { IsSearching = false, PartnerId = partnerId, Session = session };
        }

        _queue.Enqueue(connectionId);
        return new MatchmakingResult { IsSearching = true };
    }

    public void RemoveFromQueue(string connectionId)
    {
        // Note: ConcurrentQueue doesn't easily support removal from middle.
        // For simplicity in this demo, we'll just ignore it if they are dequeue'd and offline.
        // In a real app, you'd use a different structure or check connection status.
    }

    public GameSession? GetSession(string connectionId)
    {
        return _activeSessions.TryGetValue(connectionId, out var session) ? session : null;
    }

    public void EndSession(string connectionId)
    {
        if (_activeSessions.TryRemove(connectionId, out var session))
        {
            _activeSessions.TryRemove(session.User1 == connectionId ? session.User2 : session.User1, out _);
        }
    }
}

public class MatchmakingResult
{
    public bool IsSearching { get; set; }
    public string? PartnerId { get; set; }
    public GameSession? Session { get; set; }
}

public class GameSession
{
    public string SessionId { get; set; } = string.Empty;
    public string User1 { get; set; } = string.Empty;
    public string User2 { get; set; } = string.Empty;
    public string ControllerId { get; set; } = string.Empty; // ID of the user who controls the ball
}
