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

    /// <summary>
    /// Returns true if this is a new join, false if the user was already in the channel.
    /// </summary>
    public bool JoinChannel(string username, string channelName)
    {
        lock (_lock)
        {
            if (!_userChannels.TryGetValue(username, out var channels))
            {
                channels = new HashSet<string>();
                _userChannels[username] = channels;
            }

            return channels.Add(channelName);
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

    /// <summary>
    /// Get all unique connection IDs for users who share any of the given channels.
    /// </summary>
    public List<string> GetConnectionsInChannels(List<string> channels)
    {
        lock (_lock)
        {
            var usernames = new HashSet<string>();
            foreach (var channel in channels)
            {
                foreach (var (username, userChannels) in _userChannels)
                {
                    if (userChannels.Contains(channel))
                        usernames.Add(username);
                }
            }

            var connections = new List<string>();
            foreach (var username in usernames)
            {
                if (_userConnections.TryGetValue(username, out var conns))
                    connections.AddRange(conns);
            }

            return connections;
        }
    }

    public string? GetUsernameForConnection(string connectionId)
    {
        return _connections.TryGetValue(connectionId, out var info) ? info.username : null;
    }

    public bool IsOnline(string username)
    {
        return _userConnections.TryGetValue(username, out var connections) && connections.Count > 0;
    }

    public int GetOnlineUserCount()
    {
        return _userConnections.Count;
    }

    /// <summary>
    /// Forcibly remove a user from all tracking. Returns their connection IDs and channels
    /// so the caller can broadcast departures and force-disconnect connections.
    /// </summary>
    public (List<string> ConnectionIds, List<string> Channels) ForceRemoveUser(string username)
    {
        lock (_lock)
        {
            var channels = _userChannels.TryRemove(username, out var ch)
                ? ch.ToList()
                : [];

            var connectionIds = _userConnections.TryRemove(username, out var conns)
                ? conns.ToList()
                : [];

            foreach (var connId in connectionIds)
                _connections.TryRemove(connId, out _);

            return (connectionIds, channels);
        }
    }
}
