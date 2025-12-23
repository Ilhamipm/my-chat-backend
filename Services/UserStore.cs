using System.Collections.Concurrent;

namespace ChatBackend.Services;

public class UserStore
{
    // Maps CustomID -> ConnectionID
    private readonly ConcurrentDictionary<string, string> _users = new();
    // Maps ConnectionID -> CustomID
    private readonly ConcurrentDictionary<string, string> _connectionToUser = new();

    public string RegisterUser(string connectionId)
    {
        string customId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        _users[customId] = connectionId;
        _connectionToUser[connectionId] = customId;
        return customId;
    }

    public string? GetConnectionId(string customId)
    {
        return _users.TryGetValue(customId, out var cid) ? cid : null;
    }

    public string? GetCustomId(string connectionId)
    {
        return _connectionToUser.TryGetValue(connectionId, out var id) ? id : null;
    }

    public void RemoveUser(string connectionId)
    {
        if (_connectionToUser.TryRemove(connectionId, out var customId))
        {
            _users.TryRemove(customId, out _);
        }
    }
}
