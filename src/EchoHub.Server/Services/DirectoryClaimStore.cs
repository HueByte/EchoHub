using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace EchoHub.Server.Services;

/// <summary>
/// Persists and exposes the EchoHubSpace directory claim — the opaque token issued on first
/// registration and the row's stable <c>ServerId</c>. Also surfaces ephemeral registration
/// status (success/failure code, conflicting hosts) for operator-facing endpoints.
///
/// Persistence uses atomic write (tmp + rename). Treat the file contents as a secret.
/// </summary>
public sealed class DirectoryClaimStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _filePath;
    private readonly ILogger<DirectoryClaimStore> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private PersistedClaim _persisted = new(null, null);
    private RegistrationStatus _status = new(false, null, null, null, null);

    public DirectoryClaimStore(IConfiguration configuration, ILogger<DirectoryClaimStore> logger)
    {
        _logger = logger;
        _filePath = ResolveFilePath(configuration);
        Load();
    }

    public string FilePath => _filePath;

    public string? ClaimToken => Volatile.Read(ref _persisted).ClaimToken;
    public Guid? ServerId => Volatile.Read(ref _persisted).ServerId;

    public RegistrationStatus Status => Volatile.Read(ref _status);

    /// <summary>
    /// Persist a freshly-issued claim token alongside the server's stable ServerId.
    /// Called exactly once per row's lifetime — on first claim. Atomic on-disk swap.
    /// </summary>
    public async Task SaveClaimAsync(string claimToken, Guid serverId, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            var next = new PersistedClaim(claimToken, serverId);
            await WriteAtomicAsync(next, ct);
            Volatile.Write(ref _persisted, next);
            _logger.LogInformation("Persisted directory claim token for ServerId {ServerId} at {Path}", serverId, _filePath);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Update only the ServerId — used when re-registering with an existing token (Success path,
    /// hub returns ServerId again but no fresh token). No-op if the value is unchanged.
    /// </summary>
    public async Task UpdateServerIdAsync(Guid serverId, CancellationToken ct = default)
    {
        var current = Volatile.Read(ref _persisted);
        if (current.ServerId == serverId)
            return;

        await _writeLock.WaitAsync(ct);
        try
        {
            var next = current with { ServerId = serverId };
            await WriteAtomicAsync(next, ct);
            Volatile.Write(ref _persisted, next);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void SetSuccess(Guid serverId)
    {
        Volatile.Write(ref _status, new RegistrationStatus(
            IsRegistered: true,
            ServerId: serverId,
            LastRegisteredAt: DateTimeOffset.UtcNow,
            LastError: null,
            ConflictingHosts: null));
    }

    public void SetFailure(string errorCode, string[]? conflictingHosts)
    {
        var current = Volatile.Read(ref _status);
        Volatile.Write(ref _status, current with
        {
            IsRegistered = false,
            LastError = errorCode,
            ConflictingHosts = conflictingHosts,
        });
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
            return;

        try
        {
            using var stream = File.OpenRead(_filePath);
            var loaded = JsonSerializer.Deserialize<PersistedClaim>(stream, JsonOptions);
            if (loaded is not null)
            {
                _persisted = loaded;
                _logger.LogInformation("Loaded directory claim from {Path} (ServerId {ServerId})", _filePath, loaded.ServerId);
            }
        }
        catch (Exception ex)
        {
            // Don't crash startup over a corrupt state file — log and proceed as if no claim exists.
            // Operator will see HostAlreadyClaimed on next register and can intervene.
            _logger.LogError(ex, "Failed to read directory claim file at {Path} — treating as unclaimed", _filePath);
        }
    }

    private async Task WriteAtomicAsync(PersistedClaim claim, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var tmpPath = _filePath + ".tmp";

        await using (var stream = new FileStream(
            tmpPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true))
        {
            await JsonSerializer.SerializeAsync(stream, claim, JsonOptions, ct);
            await stream.FlushAsync(ct);
        }

        // 0600 on Unix — the file holds a secret. No-op on Windows.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                File.SetUnixFileMode(tmpPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set restrictive permissions on {Path}", tmpPath);
            }
        }

        File.Move(tmpPath, _filePath, overwrite: true);
    }

    private static string ResolveFilePath(IConfiguration configuration)
    {
        var configured = configuration["Server:DirectoryClaimPath"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        // Co-locate with the SQLite database so a single data-directory backup captures both.
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                var builder = new SqliteConnectionStringBuilder(connectionString);
                if (!string.IsNullOrWhiteSpace(builder.DataSource))
                {
                    var dir = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));
                    if (!string.IsNullOrWhiteSpace(dir))
                        return Path.Combine(dir, "directory-claim.json");
                }
            }
            catch
            {
                // Fall through to default
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "directory-claim.json");
    }

    private sealed record PersistedClaim(string? ClaimToken, Guid? ServerId);
}

public sealed record RegistrationStatus(
    bool IsRegistered,
    Guid? ServerId,
    DateTimeOffset? LastRegisteredAt,
    string? LastError,
    string[]? ConflictingHosts);
