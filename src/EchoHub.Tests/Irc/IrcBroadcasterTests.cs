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
            Guid.NewGuid(), encryptedContent, "alice", null, "general", DateTimeOffset.UtcNow);

        await _broadcaster.SendMessageToChannelAsync("general", message);

        var output = stream.GetOutputLines();
        Assert.Contains(output, l => l.Contains("Hello world!"));
        Assert.DoesNotContain(output, l => l.Contains("$ENC$"));
    }

    [Fact]
    public async Task SendMessage_ImageAttachment_SendsLinkLineOnly()
    {
        var (_, stream) = AddConnectionWithCapture("bob", "general");

        var attachment = new AttachmentDto(
            AttachmentKind.Image, "/api/files/abc", "photo.png", 1234,
            _encryption.Encrypt("line1\nline2"));
        var message = new MessageDto(
            Guid.NewGuid(), _encryption.Encrypt("look at this"), "alice", null, "general",
            DateTimeOffset.UtcNow, [attachment]);

        await _broadcaster.SendMessageToChannelAsync("general", message);

        var output = stream.GetOutputLines();
        Assert.Contains(output, l => l.Contains("[Image: photo.png]") && l.Contains("/api/files/abc"));
        // ASCII preview art is never sent to IRC clients — images are links only
        Assert.DoesNotContain(output, l => l.Contains("line1"));
        Assert.DoesNotContain(output, l => l.Contains("$ENC$"));
    }

    [Fact]
    public async Task SendMessage_SkipsOnlyOriginConnection()
    {
        var (aliceConn, aliceStream) = AddConnectionWithCapture("alice", "general");
        var (_, bobStream) = AddConnectionWithCapture("bob", "general");

        var message = new MessageDto(
            Guid.NewGuid(), _encryption.Encrypt("Hi"), "alice", null, "general", DateTimeOffset.UtcNow);

        await _broadcaster.SendMessageToChannelAsync("general", message, aliceConn.ConnectionId);

        // The connection that sent it should NOT get an echo
        Assert.Empty(aliceStream.GetOutputLines());

        // Bob should receive it
        Assert.NotEmpty(bobStream.GetOutputLines());
    }

    [Fact]
    public async Task SendMessage_SendersOtherSessionsStillReceive()
    {
        // Same account online twice (e.g. TUI + IRC, or two IRC clients): a message sent
        // from one session must still reach the other — skipping by nickname used to
        // swallow these until the IRC client reconnected.
        var (_, ircStream) = AddConnectionWithCapture("alice", "general");

        var message = new MessageDto(
            Guid.NewGuid(), _encryption.Encrypt("sent from the TUI"), "alice", null, "general", DateTimeOffset.UtcNow);

        // Origin is a SignalR connection, not this IRC one
        await _broadcaster.SendMessageToChannelAsync("general", message, "signalr-conn-123");

        Assert.Contains(ircStream.GetOutputLines(), l => l.Contains("sent from the TUI"));
    }

    [Fact]
    public async Task SendMessage_OnlySendsToChannelMembers()
    {
        var (_, generalStream) = AddConnectionWithCapture("bob", "general");
        var (_, randomStream) = AddConnectionWithCapture("charlie", "random");

        var message = new MessageDto(
            Guid.NewGuid(), _encryption.Encrypt("Hi"), "alice", null, "general", DateTimeOffset.UtcNow);

        await _broadcaster.SendMessageToChannelAsync("general", message);

        Assert.NotEmpty(generalStream.GetOutputLines());
        Assert.Empty(randomStream.GetOutputLines());
    }

    // ── SendUserJoinedAsync ──────────────────────────────────────────────

    [Fact]
    public async Task SendUserJoined_NotifiesOtherMembers()
    {
        var (_, bobStream) = AddConnectionWithCapture("bob", "general");

        await _broadcaster.SendUserJoinedAsync("general", "alice", null);

        var output = bobStream.GetOutputLines();
        Assert.Contains(output, l => l.Contains("JOIN #general") && l.Contains("alice"));
    }

    [Fact]
    public async Task SendUserJoined_ExcludesConnectionId()
    {
        var (conn, excludedStream) = AddConnectionWithCapture("alice", "general");
        var (_, bobStream) = AddConnectionWithCapture("bob", "general");

        await _broadcaster.SendUserJoinedAsync("general", "alice", null, conn.ConnectionId);

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

    // ── IRCv3 Tags (server-time, msgid, reply) ───────────────────────────

    [Fact]
    public async Task SendMessage_WithServerTimeCap_IncludesTimeTag()
    {
        var (conn, stream) = AddConnectionWithCapture("bob", "general");
        conn.EnableCap("server-time");

        var message = new MessageDto(
            Guid.NewGuid(), _encryption.Encrypt("Hi"), "alice", null, "general",
            new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));

        await _broadcaster.SendMessageToChannelAsync("general", message);

        var output = stream.GetOutputLines();
        Assert.Contains(output, l => l.StartsWith("@time=2024-01-15T10:30:00.000Z"));
    }

    [Fact]
    public async Task SendMessage_WithoutServerTimeCap_NoTimeTag()
    {
        var (_, stream) = AddConnectionWithCapture("bob", "general");

        var message = new MessageDto(
            Guid.NewGuid(), _encryption.Encrypt("Hi"), "alice", null, "general", DateTimeOffset.UtcNow);

        await _broadcaster.SendMessageToChannelAsync("general", message);

        var output = stream.GetOutputLines();
        Assert.All(output, l => Assert.DoesNotContain("@time=", l));
    }

    [Fact]
    public async Task SendMessage_WithMessageTagsCap_IncludesMsgid()
    {
        var (conn, stream) = AddConnectionWithCapture("bob", "general");
        conn.EnableCap("message-tags");

        var msgId = Guid.NewGuid();
        var message = new MessageDto(
            msgId, _encryption.Encrypt("Hi"), "alice", null, "general", DateTimeOffset.UtcNow);

        await _broadcaster.SendMessageToChannelAsync("general", message);

        var output = stream.GetOutputLines();
        Assert.Contains(output, l => l.Contains($"msgid={msgId:D}"));
    }

    [Fact]
    public async Task SendMessage_WithMessageTagsAndReply_IncludesReplyTag()
    {
        var (conn, stream) = AddConnectionWithCapture("bob", "general");
        conn.EnableCap("message-tags");

        var replyToId = Guid.NewGuid();
        var message = new MessageDto(
            Guid.NewGuid(), _encryption.Encrypt("Hello!"), "alice", null, "general", DateTimeOffset.UtcNow,
            ReplyTo: new ReplyRefDto(replyToId, "bob", _encryption.Encrypt("Original")));

        await _broadcaster.SendMessageToChannelAsync("general", message);

        var output = stream.GetOutputLines();
        Assert.Contains(output, l => l.Contains($"+reply={replyToId:D}"));
    }

    [Fact]
    public async Task SendMessage_WithAllTags_FormatsCorrectly()
    {
        var (conn, stream) = AddConnectionWithCapture("bob", "general");
        conn.EnableCap("server-time");
        conn.EnableCap("message-tags");

        var msgId = Guid.NewGuid();
        var sentAt = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.Zero);
        var message = new MessageDto(
            msgId, _encryption.Encrypt("Hey"), "alice", null, "general", sentAt);

        await _broadcaster.SendMessageToChannelAsync("general", message);

        var output = stream.GetOutputLines();
        Assert.Contains(output, l => l.StartsWith("@time=2024-06-15T14:30:00.000Z;msgid="));
    }

    [Fact]
    public async Task SendUserJoined_WithServerTime_IncludesTimeTag()
    {
        var (conn, stream) = AddConnectionWithCapture("bob", "general");
        conn.EnableCap("server-time");

        await _broadcaster.SendUserJoinedAsync("general", "alice", null);

        var output = stream.GetOutputLines();
        Assert.Contains(output, l => l.StartsWith("@time=") && l.Contains("JOIN"));
    }

    // ── Multiline Batch ──────────────────────────────────────────────────

    [Fact]
    public async Task SendMessage_WithMultilineCap_WrapsInBatch()
    {
        var (conn, stream) = AddConnectionWithCapture("bob", "general");
        conn.EnableCap("draft/multiline");
        conn.EnableCap("batch");

        var message = new MessageDto(
            Guid.NewGuid(), _encryption.Encrypt("Line1\nLine2"), "alice", null, "general", DateTimeOffset.UtcNow);

        await _broadcaster.SendMessageToChannelAsync("general", message);

        var output = stream.GetOutputLines();
        // BATCH start should use the user's prefix, not the server name
        Assert.Contains(output, l => l.Contains(":alice!alice@echohub BATCH +") && l.Contains("draft/multiline"));
        Assert.Contains(output, l => l.Contains("BATCH -"));
    }

    [Fact]
    public async Task SendMessage_WithoutMultilineCap_SendsLinesDirectly()
    {
        var (_, stream) = AddConnectionWithCapture("bob", "general");

        var message = new MessageDto(
            Guid.NewGuid(), _encryption.Encrypt("Line1\nLine2"), "alice", null, "general", DateTimeOffset.UtcNow);

        await _broadcaster.SendMessageToChannelAsync("general", message);

        var output = stream.GetOutputLines();
        Assert.All(output, l => Assert.DoesNotContain("BATCH", l));
    }

    [Fact]
    public async Task SendMessage_SingleLineWithMultilineCap_NoBatch()
    {
        var (conn, stream) = AddConnectionWithCapture("bob", "general");
        conn.EnableCap("draft/multiline");
        conn.EnableCap("batch");

        var message = new MessageDto(
            Guid.NewGuid(), _encryption.Encrypt("Just one line"), "alice", null, "general", DateTimeOffset.UtcNow);

        await _broadcaster.SendMessageToChannelAsync("general", message);

        var output = stream.GetOutputLines();
        Assert.All(output, l => Assert.DoesNotContain("BATCH", l));
    }

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
