using System.Collections.Concurrent;

namespace ChatBackend.Services;

public class UserStore
{
    // Maps CustomID -> ConnectionID
    private readonly ConcurrentDictionary<string, string> _users = new();
    // Maps ConnectionID -> CustomID
    private readonly ConcurrentDictionary<string, string> _connectionToUser = new();

    public string RegisterUser(string connectionId, string? preferredId = null)
    {
        string customId = preferredId ?? Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        
        // If preferred ID is taken, generate a new one
        if (_users.ContainsKey(customId)) 
        {
            if (preferredId != null) return "ERROR_TAKEN";
            customId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }

        _users[customId] = connectionId;
        _connectionToUser[connectionId] = customId;
        return customId;
    }

    public bool ChangeId(string connectionId, string newId)
    {
        if (_users.ContainsKey(newId)) return false;

        if (_connectionToUser.TryGetValue(connectionId, out var oldId))
        {
            _users.TryRemove(oldId, out _);
            _users[newId] = connectionId;
            _connectionToUser[connectionId] = newId;
            return true;
        }
        return false;
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
