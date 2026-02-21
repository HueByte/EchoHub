using EchoHub.Server.Irc;
using Xunit;

namespace EchoHub.Tests.Irc;

public class IrcClientConnectionTests
{
    // ── Connection identity ──────────────────────────────────────────────

    [Fact]
    public void ConnectionId_StartsWithIrcPrefix()
    {
        var (conn, _) = TestIrcConnectionFactory.Create();
        Assert.StartsWith("irc-", conn.ConnectionId);
    }

    [Fact]
    public void ConnectionId_IsUnique()
    {
        var (conn1, _) = TestIrcConnectionFactory.Create();
        var (conn2, _) = TestIrcConnectionFactory.Create();
        Assert.NotEqual(conn1.ConnectionId, conn2.ConnectionId);
    }

    // ── Hostmask ─────────────────────────────────────────────────────────

    [Fact]
    public void Hostmask_WithNicknameAndUsername_FormatsCorrectly()
    {
        var (conn, _) = TestIrcConnectionFactory.Create();
        conn.Nickname = "alice";
        conn.Username = "alice_user";

        Assert.Equal("alice!alice_user@echohub", conn.Hostmask);
    }

    [Fact]
    public void Hostmask_WithoutUsername_FallsBackToNickname()
    {
        var (conn, _) = TestIrcConnectionFactory.Create();
        conn.Nickname = "alice";

        Assert.Equal("alice!alice@echohub", conn.Hostmask);
    }

    // ── I/O ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReadLineAsync_ReturnsInputLines()
    {
        var (conn, _) = TestIrcConnectionFactory.Create("PING", "PONG");

        var line1 = await conn.ReadLineAsync(CancellationToken.None);
        var line2 = await conn.ReadLineAsync(CancellationToken.None);

        Assert.Equal("PING", line1);
        Assert.Equal("PONG", line2);
    }

    [Fact]
    public async Task ReadLineAsync_EndOfStream_ReturnsNull()
    {
        var (conn, _) = TestIrcConnectionFactory.Create("PING");

        await conn.ReadLineAsync(CancellationToken.None); // consume "PING"
        var result = await conn.ReadLineAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SendAsync_WritesToOutput()
    {
        var (conn, stream) = TestIrcConnectionFactory.Create();

        await conn.SendAsync(":server 001 alice :Welcome!");

        var output = stream.GetOutputLines();
        Assert.Single(output);
        Assert.Equal(":server 001 alice :Welcome!", output[0]);
    }

    [Fact]
    public async Task SendAsync_MultipleLines_AllCaptured()
    {
        var (conn, stream) = TestIrcConnectionFactory.Create();

        await conn.SendAsync("line1");
        await conn.SendAsync("line2");
        await conn.SendAsync("line3");

        var output = stream.GetOutputLines();
        Assert.Equal(3, output.Count);
    }

    [Fact]
    public async Task SendNumericAsync_FormatsCorrectly()
    {
        var (conn, stream) = TestIrcConnectionFactory.Create();
        conn.Nickname = "alice";

        await conn.SendNumericAsync("testserver", "001", ":Welcome!");

        var output = stream.GetOutputLines();
        Assert.Single(output);
        Assert.Equal(":testserver 001 alice :Welcome!", output[0]);
    }

    [Fact]
    public async Task SendNumericAsync_NoNickname_UsesStar()
    {
        var (conn, stream) = TestIrcConnectionFactory.Create();

        await conn.SendNumericAsync("testserver", "451", ":Not registered");

        var output = stream.GetOutputLines();
        Assert.Contains("*", output[0]);
    }

    // ── Thread-safe channel operations ───────────────────────────────────

    [Fact]
    public void JoinChannel_AddsChannel()
    {
        var (conn, _) = TestIrcConnectionFactory.Create();
        conn.JoinChannel("general");

        Assert.True(conn.IsInChannel("general"));
    }

    [Fact]
    public void LeaveChannel_RemovesChannel()
    {
        var (conn, _) = TestIrcConnectionFactory.Create();
        conn.JoinChannel("general");
        conn.LeaveChannel("general");

        Assert.False(conn.IsInChannel("general"));
    }

    [Fact]
    public void IsInChannel_CaseInsensitive()
    {
        var (conn, _) = TestIrcConnectionFactory.Create();
        conn.JoinChannel("General");

        Assert.True(conn.IsInChannel("general"));
        Assert.True(conn.IsInChannel("GENERAL"));
    }

    [Fact]
    public void GetJoinedChannels_ReturnsSnapshot()
    {
        var (conn, _) = TestIrcConnectionFactory.Create();
        conn.JoinChannel("general");
        conn.JoinChannel("random");

        var channels = conn.GetJoinedChannels();
        Assert.Equal(2, channels.Count);
        Assert.Contains("general", channels);
        Assert.Contains("random", channels);

        // Modifying the returned list shouldn't affect the connection state
        channels.Clear();
        Assert.True(conn.IsInChannel("general"));
    }

    [Fact]
    public async Task JoinedChannels_ConcurrentAccess_DoesNotThrow()
    {
        var (conn, _) = TestIrcConnectionFactory.Create();

        // Simulate concurrent reads and writes (broadcaster reads while handler writes)
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var writerTask = Task.Run(async () =>
        {
            for (int i = 0; i < 1000 && !cts.IsCancellationRequested; i++)
            {
                conn.JoinChannel($"channel-{i}");
                await Task.Yield();
                if (i % 3 == 0) conn.LeaveChannel($"channel-{i}");
            }
        }, cts.Token);

        var readerTask = Task.Run(async () =>
        {
            for (int i = 0; i < 1000 && !cts.IsCancellationRequested; i++)
            {
                _ = conn.IsInChannel($"channel-{i}");
                _ = conn.GetJoinedChannels();
                await Task.Yield();
            }
        }, cts.Token);

        // Should complete without exceptions
        await Task.WhenAll(writerTask, readerTask);
    }

    // ── Default state ────────────────────────────────────────────────────

    [Fact]
    public void NewConnection_IsNotRegistered()
    {
        var (conn, _) = TestIrcConnectionFactory.Create();

        Assert.False(conn.IsRegistered);
        Assert.False(conn.IsAuthenticated);
        Assert.Null(conn.Nickname);
        Assert.Null(conn.Username);
        Assert.Null(conn.UserId);
        Assert.Null(conn.AwayMessage);
    }
}
