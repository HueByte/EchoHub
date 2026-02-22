using System.Net.Sockets;
using System.Text;
using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Irc;

namespace EchoHub.Tests.Irc;

/// <summary>
/// A duplex stream that reads from one buffer and writes to another,
/// allowing test code to inject input and capture output.
/// </summary>
internal sealed class TestDuplexStream : Stream
{
    private readonly MemoryStream _readBuffer;
    private readonly MemoryStream _writeBuffer = new();

    public TestDuplexStream(string input = "")
    {
        _readBuffer = new MemoryStream(Encoding.UTF8.GetBytes(input));
    }

    public string GetOutput()
    {
        var raw = Encoding.UTF8.GetString(_writeBuffer.ToArray());
        // Strip UTF-8 BOM emitted by StreamWriter
        return raw.TrimStart('\uFEFF');
    }

    public List<string> GetOutputLines() =>
        GetOutput().Split("\r\n", StringSplitOptions.RemoveEmptyEntries).ToList();

    // Read from the input buffer
    public override int Read(byte[] buffer, int offset, int count) =>
        _readBuffer.Read(buffer, offset, count);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
        _readBuffer.ReadAsync(buffer, offset, count, ct);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
        _readBuffer.ReadAsync(buffer, ct);

    // Write to the output buffer
    public override void Write(byte[] buffer, int offset, int count) =>
        _writeBuffer.Write(buffer, offset, count);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
        _writeBuffer.WriteAsync(buffer, offset, count, ct);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default) =>
        _writeBuffer.WriteAsync(buffer, ct);

    public override void Flush() => _writeBuffer.Flush();
    public override Task FlushAsync(CancellationToken ct) => _writeBuffer.FlushAsync(ct);

    public override bool CanRead => true;
    public override bool CanWrite => true;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _readBuffer.Dispose();
            _writeBuffer.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Creates IrcClientConnections backed by test streams for unit testing.
/// </summary>
internal static class TestIrcConnectionFactory
{
    /// <summary>
    /// Creates a test IRC connection with the given input lines.
    /// Returns the connection and the test stream (for inspecting output).
    /// </summary>
    public static (IrcClientConnection Connection, TestDuplexStream Stream) Create(params string[] inputLines)
    {
        var input = string.Join("\r\n", inputLines);
        if (inputLines.Length > 0) input += "\r\n";

        var stream = new TestDuplexStream(input);
        var tcpClient = new TcpClient();
        var conn = new IrcClientConnection(tcpClient, stream);
        return (conn, stream);
    }

    /// <summary>
    /// Creates a pre-authenticated, registered IRC connection.
    /// </summary>
    public static (IrcClientConnection Connection, TestDuplexStream Stream) CreateAuthenticated(
        string nickname = "alice", Guid? userId = null, params string[] inputLines)
    {
        var (conn, stream) = Create(inputLines);
        conn.Nickname = nickname;
        conn.Username = nickname;
        conn.UserId = userId ?? Guid.NewGuid();
        conn.IsRegistered = true;
        conn.IsAuthenticated = true;
        return (conn, stream);
    }
}

/// <summary>
/// Fake encryption service that uses a simple reversible prefix-based scheme.
/// Encrypt("hello") → "$ENC$hello", Decrypt("$ENC$hello") → "hello".
/// </summary>
internal sealed class FakeEncryptionService : IMessageEncryptionService
{
    private const string Prefix = "$ENC$";

    public bool EncryptDatabaseEnabled => true;

    public string Encrypt(string plaintext) => $"{Prefix}{plaintext}";

    public string Decrypt(string content)
    {
        if (content.StartsWith(Prefix))
            return content[Prefix.Length..];
        return content;
    }

    public string? EncryptNullable(string? value) =>
        value is not null ? Encrypt(value) : null;

    public string? DecryptNullable(string? value) =>
        value is not null ? Decrypt(value) : null;
}

/// <summary>
/// Fake chat service that records method calls and returns pre-configured results.
/// </summary>
internal sealed class FakeChatService : IChatService
{
    // Recorded calls
    public List<string> ConnectedUsers { get; } = [];
    public List<string> DisconnectedConnections { get; } = [];
    public List<(string Channel, string Username)> JoinedChannels { get; } = [];
    public List<(string Channel, string Username)> LeftChannels { get; } = [];
    public List<(string Channel, string Content)> SentMessages { get; } = [];
    public List<(string Username, UserStatus Status)> StatusUpdates { get; } = [];

    // Configurable results
    public List<MessageDto> HistoryToReturn { get; set; } = [];
    public string? JoinError { get; set; }
    public string? SendMessageError { get; set; }
    public (Guid UserId, string Username)? AuthResult { get; set; }
    public UserProfileDto? ProfileToReturn { get; set; }
    public List<string> ChannelsForUserToReturn { get; set; } = [];
    public List<UserPresenceDto> OnlineUsersToReturn { get; set; } = [];

    public Task UserConnectedAsync(string connectionId, Guid userId, string username)
    {
        ConnectedUsers.Add(username);
        return Task.CompletedTask;
    }

    public Task<string?> UserDisconnectedAsync(string connectionId)
    {
        DisconnectedConnections.Add(connectionId);
        return Task.FromResult<string?>(null);
    }

    public Task<(List<MessageDto> History, string? Error)> JoinChannelAsync(
        string connectionId, Guid userId, string username, string channelName)
    {
        JoinedChannels.Add((channelName, username));
        return Task.FromResult((HistoryToReturn, JoinError));
    }

    public Task LeaveChannelAsync(string connectionId, string username, string channelName)
    {
        LeftChannels.Add((channelName, username));
        return Task.CompletedTask;
    }

    public Task<string?> SendMessageAsync(Guid userId, string username, string channelName, string content)
    {
        SentMessages.Add((channelName, content));
        return Task.FromResult(SendMessageError);
    }

    public Task<List<MessageDto>> GetChannelHistoryAsync(string channelName, int count) =>
        Task.FromResult(HistoryToReturn);

    public Task<string?> UpdateStatusAsync(Guid userId, string username, UserStatus status, string? statusMessage)
    {
        StatusUpdates.Add((username, status));
        return Task.FromResult<string?>(null);
    }

    public Task<List<UserPresenceDto>> GetOnlineUsersAsync(string channelName) =>
        Task.FromResult(OnlineUsersToReturn);

    public Task BroadcastMessageAsync(string channelName, MessageDto message) =>
        Task.CompletedTask;

    public Task BroadcastChannelUpdatedAsync(ChannelDto channel, string? channelName = null) =>
        Task.CompletedTask;

    public Task<UserProfileDto?> GetUserProfileAsync(string username) =>
        Task.FromResult(ProfileToReturn);

    public Task<List<string>> GetChannelsForUserAsync(string username) =>
        Task.FromResult(ChannelsForUserToReturn);

    public Task<(Guid UserId, string Username)?> AuthenticateUserAsync(string username, string password) =>
        Task.FromResult(AuthResult);
}

/// <summary>
/// Fake channel service that records method calls and returns pre-configured results.
/// </summary>
internal sealed class FakeChannelService : IChannelService
{
    // Configurable results
    public (string? Topic, bool Exists) TopicResult { get; set; } = (null, true);
    public List<ChannelListItem> ChannelListToReturn { get; set; } = [];
    public ChannelDto? ChannelByNameToReturn { get; set; }
    public ChannelOperationResult? CreateResult { get; set; }
    public ChannelOperationResult? UpdateTopicResult { get; set; }
    public ChannelOperationResult? DeleteResult { get; set; }
    public (bool Success, string? Error) MembershipResult { get; set; } = (true, null);

    public Task<PaginatedResponse<ChannelDto>> GetChannelsAsync(Guid userId, int offset, int limit) =>
        Task.FromResult(new PaginatedResponse<ChannelDto>([], 0, offset, limit));

    public Task<ChannelOperationResult> CreateChannelAsync(Guid creatorUserId, string name, string? topic, bool isPublic) =>
        Task.FromResult(CreateResult ?? ChannelOperationResult.Fail(ChannelError.ValidationFailed, "Not configured"));

    public Task<ChannelOperationResult> UpdateTopicAsync(Guid callerUserId, string channelName, string? topic) =>
        Task.FromResult(UpdateTopicResult ?? ChannelOperationResult.Fail(ChannelError.ValidationFailed, "Not configured"));

    public Task<ChannelOperationResult> DeleteChannelAsync(Guid callerUserId, string channelName) =>
        Task.FromResult(DeleteResult ?? ChannelOperationResult.Fail(ChannelError.ValidationFailed, "Not configured"));

    public Task<(string? Topic, bool Exists)> GetChannelTopicAsync(string channelName) =>
        Task.FromResult(TopicResult);

    public Task<List<ChannelListItem>> GetChannelListAsync() =>
        Task.FromResult(ChannelListToReturn);

    public Task<ChannelDto?> GetChannelByNameAsync(string channelName) =>
        Task.FromResult(ChannelByNameToReturn);

    public Task<(bool Success, string? Error)> EnsureChannelMembershipAsync(Guid userId, string channelName) =>
        Task.FromResult(MembershipResult);
}
