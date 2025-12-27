using System.Collections.Concurrent;

namespace ChatBackend.Services;

public class MatchmakingService
{
    private readonly List<UserProfile> _queue = new();
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<string, GameSession> _activeSessions = new();

    public MatchmakingResult AddToQueue(string connectionId, UserProfile? profile)
    {
        if (profile == null) return new MatchmakingResult { IsSearching = false };

        lock (_lock)
        {
            if (_activeSessions.ContainsKey(connectionId) || _queue.Any(p => p.ConnectionId == connectionId))
            {
                return new MatchmakingResult { IsSearching = true };
            }

            // Look for a compatible partner
            var partner = _queue.FirstOrDefault(p => IsCompatible(profile, p));

            if (partner != null)
            {
                _queue.Remove(partner);

                var sessionId = Guid.NewGuid().ToString();
                var session = new GameSession
                {
                    SessionId = sessionId,
                    User1 = connectionId,
                    User2 = partner.ConnectionId,
                    ControllerId = new Random().Next(0, 2) == 0 ? connectionId : partner.ConnectionId
                };

                _activeSessions[connectionId] = session;
                _activeSessions[partner.ConnectionId] = session;

                return new MatchmakingResult { IsSearching = false, PartnerId = partner.ConnectionId, Session = session };
            }

            _queue.Add(profile);
            return new MatchmakingResult { IsSearching = true };
        }
    }

    private bool IsCompatible(UserProfile a, UserProfile b)
    {
        // A is interested in B
        bool aInterestedInB = a.Interest == "Both" || a.Interest == b.Gender;
        // B is interested in A
        bool bInterestedInA = b.Interest == "Both" || b.Interest == a.Gender;

        return aInterestedInB && bInterestedInA;
    }

    public void RemoveFromQueue(string connectionId)
    {
        lock (_lock)
        {
            var item = _queue.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (item != null) _queue.Remove(item);
        }
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

    public string GetUserStatus(string connectionId)
    {
        if (_activeSessions.ContainsKey(connectionId))
            return "Playing";
        
        lock (_lock)
        {
            if (_queue.Any(p => p.ConnectionId == connectionId))
                return "Matchmaking";
        }
        return "Idle";
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
