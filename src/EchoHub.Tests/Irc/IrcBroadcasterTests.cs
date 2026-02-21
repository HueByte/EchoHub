using System.Collections.Concurrent;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Irc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EchoHub.Tests.Irc;

public class IrcBroadcasterTests
{
    private readonly IrcOptions _options = new() { ServerName = "testserver", Enabled = true };
    private readonly FakeEncryptionService _encryption = new();
    private readonly IrcGatewayService _gateway;
    private readonly IrcBroadcaster _broadcaster;

    public IrcBroadcasterTests()
    {
        var services = new ServiceCollection()
            .AddSingleton<IOptions<IrcOptions>>(Options.Create(_options))
            .BuildServiceProvider();

        _gateway = new IrcGatewayService(
            Options.Create(_options), services, NullLogger<IrcGatewayService>.Instance);

        _broadcaster = new IrcBroadcaster(_gateway, _encryption);
    }

    /// <summary>
    /// Injects a test connection into the gateway's internal connection map.
    /// </summary>
    private IrcClientConnection AddConnection(string nickname, params string[] channels)
    {
        var (conn, _) = TestIrcConnectionFactory.CreateAuthenticated(nickname);

        foreach (var ch in channels)
            conn.JoinChannel(ch);

        // Insert into gateway's ConcurrentDictionary via the public IReadOnlyDictionary
        var connections = (ConcurrentDictionary<string, IrcClientConnection>)_gateway.Connections;
        connections[conn.ConnectionId] = conn;

        return conn;
    }

    private static List<string> CaptureOutput(IrcClientConnection conn)
    {
        // We need to get the stream from the connection — but it's private.
        // Since we used TestIrcConnectionFactory, the TestDuplexStream was passed to the constructor.
        // We can't easily access it. Instead, we create connections differently for these tests.
        // Let's use a different approach.
        throw new NotSupportedException("Use AddConnectionWithCapture instead");
    }

    /// <summary>
    /// Creates a connection that can capture output and injects it into the gateway.
    /// </summary>
    private (IrcClientConnection Connection, TestDuplexStream Stream) AddConnectionWithCapture(
        string nickname, params string[] channels)
    {
        var (conn, stream) = TestIrcConnectionFactory.CreateAuthenticated(nickname);

        foreach (var ch in channels)
            conn.JoinChannel(ch);

        var connections = (ConcurrentDictionary<string, IrcClientConnection>)_gateway.Connections;
        connections[conn.ConnectionId] = conn;

        return (conn, stream);
    }

    // ── SendMessageToChannelAsync ────────────────────────────────────────

    [Fact]
    public async Task SendMessage_DecryptsContent()
    {
        var (_, stream) = AddConnectionWithCapture("bob", "general");

        var encryptedContent = _encryption.Encrypt("Hello world!");
        var message = new MessageDto(
            Guid.NewGuid(), encryptedContent, "alice", null, "general",
            MessageType.Text, null, null, DateTimeOffset.UtcNow);

        await _broadcaster.SendMessageToChannelAsync("general", message);

        var output = stream.GetOutputLines();
        Assert.Contains(output, l => l.Contains("Hello world!"));
        Assert.DoesNotContain(output, l => l.Contains("$ENC$"));
    }

    [Fact]
    public async Task SendMessage_SkipsSender()
    {
        var (_, aliceStream) = AddConnectionWithCapture("alice", "general");
        var (_, bobStream) = AddConnectionWithCapture("bob", "general");

        var message = new MessageDto(
            Guid.NewGuid(), _encryption.Encrypt("Hi"), "alice", null, "general",
            MessageType.Text, null, null, DateTimeOffset.UtcNow);

        await _broadcaster.SendMessageToChannelAsync("general", message);

        // Alice (sender) should NOT receive the message
        Assert.Empty(aliceStream.GetOutputLines());

        // Bob should receive it
        Assert.NotEmpty(bobStream.GetOutputLines());
    }

    [Fact]
    public async Task SendMessage_OnlySendsToChannelMembers()
    {
        var (_, generalStream) = AddConnectionWithCapture("bob", "general");
        var (_, randomStream) = AddConnectionWithCapture("charlie", "random");

        var message = new MessageDto(
            Guid.NewGuid(), _encryption.Encrypt("Hi"), "alice", null, "general",
            MessageType.Text, null, null, DateTimeOffset.UtcNow);

        await _broadcaster.SendMessageToChannelAsync("general", message);

        Assert.NotEmpty(generalStream.GetOutputLines());
        Assert.Empty(randomStream.GetOutputLines());
    }

    // ── SendUserJoinedAsync ──────────────────────────────────────────────

    [Fact]
    public async Task SendUserJoined_NotifiesOtherMembers()
    {
        var (_, bobStream) = AddConnectionWithCapture("bob", "general");

        await _broadcaster.SendUserJoinedAsync("general", "alice");

        var output = bobStream.GetOutputLines();
        Assert.Contains(output, l => l.Contains("JOIN #general") && l.Contains("alice"));
    }

    [Fact]
    public async Task SendUserJoined_ExcludesConnectionId()
    {
        var (conn, excludedStream) = AddConnectionWithCapture("alice", "general");
        var (_, bobStream) = AddConnectionWithCapture("bob", "general");

        await _broadcaster.SendUserJoinedAsync("general", "alice", conn.ConnectionId);

        // Excluded connection should not get the message
        Assert.Empty(excludedStream.GetOutputLines());
        Assert.NotEmpty(bobStream.GetOutputLines());
    }

    // ── SendUserLeftAsync ────────────────────────────────────────────────

    [Fact]
    public async Task SendUserLeft_NotifiesOtherMembers()
    {
        var (_, bobStream) = AddConnectionWithCapture("bob", "general");
        AddConnectionWithCapture("alice", "general");

        await _broadcaster.SendUserLeftAsync("general", "alice");

        var output = bobStream.GetOutputLines();
        Assert.Contains(output, l => l.Contains("PART #general") && l.Contains("alice"));
    }

    [Fact]
    public async Task SendUserLeft_SkipsSender()
    {
        var (_, aliceStream) = AddConnectionWithCapture("alice", "general");

        await _broadcaster.SendUserLeftAsync("general", "alice");

        Assert.Empty(aliceStream.GetOutputLines());
    }

    // ── SendChannelUpdatedAsync ──────────────────────────────────────────

    [Fact]
    public async Task SendChannelUpdated_WithTopic_SendsTopicMessage()
    {
        var (_, stream) = AddConnectionWithCapture("bob", "general");

        var channel = new ChannelDto(
            Guid.NewGuid(), "general", "New topic!", true, 0, DateTimeOffset.UtcNow);

        await _broadcaster.SendChannelUpdatedAsync(channel, "general");

        var output = stream.GetOutputLines();
        Assert.Contains(output, l => l.Contains("TOPIC #general") && l.Contains("New topic!"));
    }

    [Fact]
    public async Task SendChannelUpdated_NullTopic_DoesNotSend()
    {
        var (_, stream) = AddConnectionWithCapture("bob", "general");

        var channel = new ChannelDto(
            Guid.NewGuid(), "general", null, true, 0, DateTimeOffset.UtcNow);

        await _broadcaster.SendChannelUpdatedAsync(channel, "general");

        Assert.Empty(stream.GetOutputLines());
    }

    // ── SendErrorAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task SendError_IrcConnection_SendsNotice()
    {
        var (conn, stream) = AddConnectionWithCapture("alice", "general");

        await _broadcaster.SendErrorAsync(conn.ConnectionId, "Something went wrong");

        var output = stream.GetOutputLines();
        Assert.Contains(output, l => l.Contains("NOTICE") && l.Contains("Something went wrong"));
    }

    [Fact]
    public async Task SendError_NonIrcConnection_DoesNothing()
    {
        // SignalR connection IDs don't start with "irc-"
        await _broadcaster.SendErrorAsync("signalr-connection-123", "Error");
        // No crash, no output — the method silently returns
    }

    // ── SendUserKickedAsync ──────────────────────────────────────────────

    [Fact]
    public async Task SendUserKicked_NotifiesChannel()
    {
        var (_, stream) = AddConnectionWithCapture("bob", "general");

        await _broadcaster.SendUserKickedAsync("general", "alice", "Spam");

        var output = stream.GetOutputLines();
        Assert.Contains(output, l => l.Contains("KICK #general alice") && l.Contains("Spam"));
    }

    // ── SendUserBannedAsync ──────────────────────────────────────────────

    [Fact]
    public async Task SendUserBanned_NotifiesBannedUser()
    {
        var (_, stream) = AddConnectionWithCapture("alice", "general");

        await _broadcaster.SendUserBannedAsync("alice", "Repeated violations");

        var output = stream.GetOutputLines();
        Assert.Contains(output, l => l.Contains("NOTICE") && l.Contains("banned"));
    }

    // ── SendMessageDeletedAsync ──────────────────────────────────────────

    [Fact]
    public async Task SendMessageDeleted_NotifiesChannel()
    {
        var (_, stream) = AddConnectionWithCapture("bob", "general");
        var msgId = Guid.NewGuid();

        await _broadcaster.SendMessageDeletedAsync("general", msgId);

        var output = stream.GetOutputLines();
        Assert.Contains(output, l => l.Contains("deleted") && l.Contains(msgId.ToString()));
    }

    // ── SendChannelNukedAsync ────────────────────────────────────────────

    [Fact]
    public async Task SendChannelNuked_NotifiesChannel()
    {
        var (_, stream) = AddConnectionWithCapture("bob", "general");

        await _broadcaster.SendChannelNukedAsync("general");

        var output = stream.GetOutputLines();
        Assert.Contains(output, l => l.Contains("cleared"));
    }

    // ── ForceDisconnectUserAsync ─────────────────────────────────────────

    [Fact]
    public async Task ForceDisconnect_IrcConnection_SendsErrorAndCloses()
    {
        var (conn, stream) = AddConnectionWithCapture("alice", "general");

        await _broadcaster.ForceDisconnectUserAsync([conn.ConnectionId], "Banned");

        var output = stream.GetOutputLines();
        Assert.Contains(output, l => l.Contains("ERROR") && l.Contains("Banned"));
    }

    [Fact]
    public async Task ForceDisconnect_NonIrcConnection_Ignores()
    {
        // Should not throw when given SignalR connection IDs
        await _broadcaster.ForceDisconnectUserAsync(["signalr-abc", "signalr-def"], "Banned");
    }

    // ── SendUserStatusChangedAsync ───────────────────────────────────────

    [Fact]
    public async Task SendUserStatusChanged_IsNoOp()
    {
        var (_, stream) = AddConnectionWithCapture("bob", "general");

        var presence = new UserPresenceDto(
            "alice", null, null, UserStatus.Away, "brb", ServerRole.Member);

        await _broadcaster.SendUserStatusChangedAsync(["general"], presence);

        // IRC doesn't push status changes — clients use WHOIS/WHO
        Assert.Empty(stream.GetOutputLines());
    }
}
