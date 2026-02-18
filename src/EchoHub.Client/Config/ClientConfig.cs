namespace EchoHub.Client.Config;

public class ClientConfig
{
    public List<SavedServer> SavedServers { get; set; } = [];
    public AccountPreset DefaultPreset { get; set; } = new();
    public string ActiveTheme { get; set; } = "Default";
}

public class SavedServer
{
    public required string Name { get; set; }
    public required string Url { get; set; }
    public string? Username { get; set; }
    public string? Token { get; set; }
    public DateTimeOffset LastConnected { get; set; }
}

public class AccountPreset
{
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? NicknameColor { get; set; }
}
