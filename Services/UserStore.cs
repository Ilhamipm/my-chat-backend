using System.Collections.Concurrent;

namespace ChatBackend.Services;

public class UserProfile
{
    public string ConnectionId { get; set; } = string.Empty;
    public string CustomId { get; set; } = string.Empty;
    public string Gender { get; set; } = "Non-binary"; // Default
    public string Interest { get; set; } = "Both"; // Default
}

public class UserStore
{
    // Maps CustomID -> UserProfile
    private readonly ConcurrentDictionary<string, UserProfile> _users = new();
    // Maps ConnectionID -> CustomID
    private readonly ConcurrentDictionary<string, string> _connectionToUser = new();

    public string RegisterUser(string connectionId, string? preferredId = null)
    {
        string customId = preferredId ?? Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        
        var profile = _users.GetOrAdd(customId, id => new UserProfile { CustomId = id });
        profile.ConnectionId = connectionId;

        _connectionToUser[connectionId] = customId;
        return customId;
    }

    public void UpdateProfile(string connectionId, string gender, string interest)
    {
        if (_connectionToUser.TryGetValue(connectionId, out var customId))
        {
            if (_users.TryGetValue(customId, out var profile))
            {
                profile.Gender = gender;
                profile.Interest = interest;
            }
        }
    }

    public UserProfile? GetProfile(string connectionId)
    {
        if (_connectionToUser.TryGetValue(connectionId, out var customId))
        {
            return _users.TryGetValue(customId, out var profile) ? profile : null;
        }
        return null;
    }

    public bool ChangeId(string connectionId, string newId)
    {
        if (_users.ContainsKey(newId)) return false;

        if (_connectionToUser.TryGetValue(connectionId, out var oldId))
        {
            if (_users.TryRemove(oldId, out var profile))
            {
                profile.CustomId = newId;
                _users[newId] = profile;
                _connectionToUser[connectionId] = newId;
                return true;
            }
        }
        return false;
    }

    public string? GetConnectionId(string customId)
    {
        return _users.TryGetValue(customId, out var profile) ? profile.ConnectionId : null;
    }

    public string? GetCustomId(string connectionId)
    {
        return _connectionToUser.TryGetValue(connectionId, out var id) ? id : null;
    }

    public List<string> GetAllCustomIds()
    {
        return _users.Keys.ToList();
    }

    public void RemoveUser(string connectionId)
    {
        if (_connectionToUser.TryRemove(connectionId, out var customId))
        {
            // Note: In a persistent app, we might not want to remove the profile, 
            // just clear the connection ID. But for this demo, we'll keep the logic.
            if (_users.TryGetValue(customId, out var profile))
            {
                profile.ConnectionId = string.Empty;
            }
        }
    }

    public List<(string CustomId, string ConnectionId)> GetAllUsers()
    {
        return _users.Values
            .Where(p => !string.IsNullOrEmpty(p.ConnectionId))
            .Select(p => (p.CustomId, p.ConnectionId))
            .ToList();
    }
}
