using Serilog;

namespace EchoHub.Client.Services;

/// <summary>
/// Shared avatar upload logic — resolves a file path or URL to a stream
/// and uploads it via ApiClient.
/// </summary>
internal static class AvatarHelper
{
    /// <summary>
    /// Upload an avatar from a local file path or HTTP(S) URL.
    /// Returns the ASCII art response from the server.
    /// </summary>
    public static async Task<string?> UploadAsync(ApiClient apiClient, string target)
    {
        Stream stream;
        string fileName;

        if (Uri.TryCreate(target, UriKind.Absolute, out var uri)
            && (uri.Scheme == "http" || uri.Scheme == "https"))
        {
            using var http = new HttpClient();
            var bytes = await http.GetByteArrayAsync(uri);
            stream = new MemoryStream(bytes);
            fileName = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrWhiteSpace(fileName) || !fileName.Contains('.'))
                fileName = "avatar.png";
        }
        else
        {
            if (!File.Exists(target))
                throw new FileNotFoundException($"File not found: {target}");

            stream = File.OpenRead(target);
            fileName = Path.GetFileName(target);
        }

        await using (stream)
        {
            return await apiClient.UploadAvatarAsync(stream, fileName);
        }
    }
}
