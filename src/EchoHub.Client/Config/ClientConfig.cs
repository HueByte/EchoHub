namespace EchoHub.Client.Config;

public class ClientConfig
{
    public List<SavedServer> SavedServers { get; set; } = [];
    public AccountPreset DefaultPreset { get; set; } = new();
    public string ActiveTheme { get; set; } = "Default";
    public NotificationConfig Notifications { get; set; } = new();

    /// <summary>
    /// Folder where downloaded attachments and saved images are written. When null, the
    /// OS Downloads folder is used. Set via the native folder picker or <c>/downloadpath</c>.
    /// </summary>
    public string? DownloadPath { get; set; }

    /// <summary>
    /// ASCII-art rendering size for images you attach: "s" (40×40), "m" (80×80), or "l" (120×120).
    /// Applies to copy-paste/drag-drop attachments, which have no per-file size flag.
    /// </summary>
    public string DefaultAsciiSize { get; set; } = "m";
}

public class NotificationConfig
{
    public bool Enabled { get; set; } = true;
    public byte Volume { get; set; } = 30;
    public string? SoundFile { get; set; }
}

public class SavedServer
{
    public required string Name { get; set; }
    public required string Url { get; set; }
    public string? Username { get; set; }
    public string? RefreshToken { get; set; }
    public bool RememberMe { get; set; }
    public DateTimeOffset LastConnected { get; set; }

    /// <summary>
    /// Cached room content keys for end-to-end encrypted channels on this server,
    /// keyed by channel name (base64). Like RefreshToken, these live only on the
    /// user's machine — the server never sees them.
    /// </summary>
    public Dictionary<string, string> ChannelKeys { get; set; } = [];
}

public class AccountPreset
{
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? NicknameColor { get; set; }
}
