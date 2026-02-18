using System.Text.Json;

namespace EchoHub.Client.Config;

public static class ConfigManager
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".echohub");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static ClientConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return new ClientConfig();

            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<ClientConfig>(json, JsonOptions) ?? new ClientConfig();
        }
        catch
        {
            return new ClientConfig();
        }
    }

    public static void Save(ClientConfig config)
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(ConfigPath, json);
        }
        catch
        {
            // Silently fail — config save is best-effort
        }
    }

    public static void SaveServer(SavedServer server)
    {
        var config = Load();
        var existing = config.SavedServers.FindIndex(s =>
            string.Equals(s.Url, server.Url, StringComparison.OrdinalIgnoreCase));

        if (existing >= 0)
            config.SavedServers[existing] = server;
        else
            config.SavedServers.Add(server);

        Save(config);
    }

    public static void RemoveServer(string url)
    {
        var config = Load();
        config.SavedServers.RemoveAll(s =>
            string.Equals(s.Url, url, StringComparison.OrdinalIgnoreCase));

        Save(config);
    }
}
