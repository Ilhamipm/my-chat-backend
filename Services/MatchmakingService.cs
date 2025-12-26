using System.Collections.Concurrent;

namespace ChatBackend.Services;

public class MatchmakingService
{
    private readonly ConcurrentQueue<string> _waitingUsers = new();
    private readonly ConcurrentDictionary<string, string> _matches = new();
    private readonly ConcurrentDictionary<string, bool> _preferences = new(); // ConnectionId -> wantsControl

    public bool JoinQueue(string connectionId)
    {
        if (_waitingUsers.Contains(connectionId)) return false;
        _waitingUsers.Enqueue(connectionId);
        return true;
    }

    public (string? player1, string? player2) TryMatch()
    {
        if (_waitingUsers.Count >= 2)
        {
            if (_waitingUsers.TryDequeue(out var p1) && _waitingUsers.TryDequeue(out var p2))
            {
                _matches[p1] = p2;
                _matches[p2] = p1;
                return (p1, p2);
            }
        }
        return (null, null);
    }

    public void SetPreference(string connectionId, bool wantsControl)
    {
        _preferences[connectionId] = wantsControl;
    }

    public string? GetOpponent(string connectionId)
    {
        return _matches.TryGetValue(connectionId, out var opponent) ? opponent : null;
    }

    public (string controller, string follower) ResolveRoles(string p1, string p2)
    {
        _preferences.TryGetValue(p1, out var pref1);
        _preferences.TryGetValue(p2, out var pref2);

        if (pref1 && !pref2) return (p1, p2);
        if (!pref1 && pref2) return (p2, p1);

        // If both want same or no preference, choose random
        var random = new Random();
        return random.Next(2) == 0 ? (p1, p2) : (p2, p1);
    }

    public void RemoveUser(string connectionId)
    {
        if (_matches.TryRemove(connectionId, out var opponent))
        {
            _matches.TryRemove(opponent, out _);
        }
        _preferences.TryRemove(connectionId, out _);
        // Queue removal is tricky with ConcurrentQueue, but we can filter it out during matching if needed.
        // For simplicity in this demo, let's assume users don't frequently drop while in queue.
    }
}
