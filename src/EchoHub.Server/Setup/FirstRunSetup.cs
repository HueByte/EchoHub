using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EchoHub.Server.Setup;

public static class FirstRunSetup
{
    public static void EnsureAppSettings()
    {
        var contentRoot = Directory.GetCurrentDirectory();
        var settingsPath = Path.Combine(contentRoot, "appsettings.json");
        var examplePath = Path.Combine(contentRoot, "appsettings.example.json");

        if (!File.Exists(settingsPath) && File.Exists(examplePath))
        {
            File.Copy(examplePath, settingsPath);
            Console.WriteLine("Created appsettings.json from example config.");
        }

        if (!File.Exists(settingsPath))
            return;

        EnsureJwtSecret(settingsPath);
    }

    private static void EnsureJwtSecret(string settingsPath)
    {
        var json = File.ReadAllText(settingsPath);
        var root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        if (root is null)
            return;

        var currentSecret = root["Jwt"]?["Secret"]?.GetValue<string>();

        if (!string.IsNullOrEmpty(currentSecret) && !currentSecret.StartsWith("CHANGE_ME"))
            return;

        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

        root["Jwt"] ??= new JsonObject();
        root["Jwt"]!["Secret"] = secret;

        var writeOptions = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(settingsPath, root.ToJsonString(writeOptions));
        Console.WriteLine("Generated new JWT secret in appsettings.json.");
    }
}
