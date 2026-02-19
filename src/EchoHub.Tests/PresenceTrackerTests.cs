using EchoHub.Server.Services;
using Xunit;

namespace EchoHub.Tests;

public class PresenceTrackerTests
{
    [Fact]
    public void UserConnected_IsOnline_ReturnsTrue()
    {
        var tracker = new PresenceTracker();
        tracker.UserConnected("conn1", Guid.NewGuid(), "alice");
        Assert.True(tracker.IsOnline("alice"));
    }

    [Fact]
    public void UserDisconnected_LastConnection_IsOnlineReturnsFalse()
    {
        var tracker = new PresenceTracker();
        tracker.UserConnected("conn1", Guid.NewGuid(), "alice");
        tracker.UserDisconnected("conn1");
        Assert.False(tracker.IsOnline("alice"));
    }

    [Fact]
    public void MultipleConnections_DisconnectOne_StillOnline()
    {
        var tracker = new PresenceTracker();
        var userId = Guid.NewGuid();
        tracker.UserConnected("conn1", userId, "alice");
        tracker.UserConnected("conn2", userId, "alice");
        tracker.UserDisconnected("conn1");
        Assert.True(tracker.IsOnline("alice"));
    }

    [Fact]
    public void JoinChannel_GetOnlineUsersInChannel_ReturnsUser()
    {
        var tracker = new PresenceTracker();
        tracker.UserConnected("conn1", Guid.NewGuid(), "alice");
        tracker.JoinChannel("alice", "general");
        var users = tracker.GetOnlineUsersInChannel("general");
        Assert.Contains("alice", users);
    }

    [Fact]
    public void LeaveChannel_UserNoLongerInChannel()
    {
        var tracker = new PresenceTracker();
        tracker.UserConnected("conn1", Guid.NewGuid(), "alice");
        tracker.JoinChannel("alice", "general");
        tracker.LeaveChannel("alice", "general");
        var users = tracker.GetOnlineUsersInChannel("general");
        Assert.DoesNotContain("alice", users);
    }

    [Fact]
    public void GetChannelsForUser_ReturnsJoinedChannels()
    {
        var tracker = new PresenceTracker();
        tracker.UserConnected("conn1", Guid.NewGuid(), "alice");
        tracker.JoinChannel("alice", "general");
        tracker.JoinChannel("alice", "random");
        var channels = tracker.GetChannelsForUser("alice");
        Assert.Contains("general", channels);
        Assert.Contains("random", channels);
    }
}
