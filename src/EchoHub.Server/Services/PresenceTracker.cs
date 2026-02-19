using System.Collections.Concurrent;

namespace EchoHub.Server.Services;

public class PresenceTracker
{
    private readonly ConcurrentDictionary<string, (Guid userId, string username)> _connections = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _userChannels = new();

    private readonly object _lock = new();

    public void UserConnected(string connectionId, Guid userId, string username)
    {
        _connections[connectionId] = (userId, username);

        lock (_lock)
        {
            if (!_userConnections.TryGetValue(username, out var connections))
            {
                connections = new HashSet<string>();
                _userConnections[username] = connections;
            }

            connections.Add(connectionId);
        }
    }

    public string? UserDisconnected(string connectionId)
    {
        if (!_connections.TryRemove(connectionId, out var userInfo))
            return null;

        var username = userInfo.username;

        lock (_lock)
        {
            if (_userConnections.TryGetValue(username, out var connections))
            {
                connections.Remove(connectionId);

                if (connections.Count == 0)
                {
                    _userConnections.TryRemove(username, out _);
                    _userChannels.TryRemove(username, out _);
                }
            }
        }

        return username;
    }

    public void JoinChannel(string username, string channelName)
    {
        lock (_lock)
        {
            if (!_userChannels.TryGetValue(username, out var channels))
            {
                channels = new HashSet<string>();
                _userChannels[username] = channels;
            }

            channels.Add(channelName);
        }
    }

    public void LeaveChannel(string username, string channelName)
    {
        lock (_lock)
        {
            if (_userChannels.TryGetValue(username, out var channels))
            {
                channels.Remove(channelName);
            }
        }
    }

    public List<string> GetOnlineUsersInChannel(string channelName)
    {
        var users = new List<string>();

        lock (_lock)
        {
            foreach (var (username, channels) in _userChannels)
            {
                if (channels.Contains(channelName))
                {
                    users.Add(username);
                }
            }
        }

        return users;
    }

    public List<string> GetChannelsForUser(string username)
    {
        lock (_lock)
        {
            if (_userChannels.TryGetValue(username, out var channels))
            {
                return channels.ToList();
            }
        }

        return [];
    }

    public bool IsOnline(string username)
    {
        return _userConnections.TryGetValue(username, out var connections) && connections.Count > 0;
    }

    public int GetOnlineUserCount()
    {
        return _userConnections.Count;
    }
}
