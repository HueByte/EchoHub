using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace EchoHub.Client.Services;

public static class UpdateChecker
{
    private static readonly Uri ReleaseUrl =
        new("https://api.github.com/repos/HueByte/EchoHub/releases/latest");

    /// <summary>
    /// Checks GitHub for a newer release. Returns the new version string if one exists, or null.
    /// Never throws — all errors are silently swallowed.
    /// </summary>
    public static async Task<string?> CheckForUpdateAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("EchoHub-Client");

            var release = await http.GetFromJsonAsync<GitHubRelease>(ReleaseUrl);
            if (release?.TagName is null)
                return null;

            var tag = release.TagName.TrimStart('v', 'V');
            if (!Version.TryParse(tag, out var latest))
                return null;

            var currentStr = typeof(UpdateChecker).Assembly.GetName().Version?.ToString(3);
            if (currentStr is null || !Version.TryParse(currentStr, out var current))
                return null;

            return latest > current ? tag : null;
        }
        catch
        {
            return null;
        }
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }
    }
}
