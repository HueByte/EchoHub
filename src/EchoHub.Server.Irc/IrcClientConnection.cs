using System.Net.Sockets;
using System.Text;

namespace EchoHub.Server.Irc;

/// <summary>
/// Manages a single IRC client TCP connection.
/// </summary>
public sealed class IrcClientConnection : IAsyncDisposable
{
    private readonly TcpClient _tcpClient;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    // Connection identity
    public string ConnectionId { get; } = $"irc-{Guid.NewGuid()}";

    // Registration state
    public string? Nickname { get; set; }
    public string? Username { get; set; }
    public string? RealName { get; set; }
    public string? Password { get; set; }
    public Guid? UserId { get; set; }
    public bool IsRegistered { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsSasl { get; set; }
    public bool CapNegotiating { get; set; }

    // Channel state
    public HashSet<string> JoinedChannels { get; } = new(StringComparer.OrdinalIgnoreCase);

    // Away state
    public string? AwayMessage { get; set; }

    public string Hostmask => $"{Nickname}!{Username ?? Nickname}@echohub";

    public IrcClientConnection(TcpClient tcpClient, Stream stream)
    {
        _tcpClient = tcpClient;
        _reader = new StreamReader(stream, Encoding.UTF8);
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true, NewLine = "\r\n" };
    }

    public async Task<string?> ReadLineAsync(CancellationToken ct)
    {
        try
        {
            return await _reader.ReadLineAsync(ct);
        }
        catch
        {
            return null;
        }
    }

    public async Task SendAsync(string line)
    {
        await _writeLock.WaitAsync();
        try
        {
            await _writer.WriteLineAsync(line);
        }
        catch
        {
            // Connection lost — swallow
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public Task SendNumericAsync(string serverName, string numeric, string target, string text)
        => SendAsync($":{serverName} {numeric} {target} {text}");

    public Task SendNumericAsync(string serverName, string numeric, string text)
        => SendNumericAsync(serverName, numeric, Nickname ?? "*", text);

    public async ValueTask DisposeAsync()
    {
        try { _tcpClient.Close(); } catch { }
        _reader.Dispose();
        _writer.Dispose();
        _writeLock.Dispose();
    }
}
