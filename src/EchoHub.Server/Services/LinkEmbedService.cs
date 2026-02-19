using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using EchoHub.Core.Constants;
using EchoHub.Core.DTOs;
using Microsoft.Extensions.Logging;

namespace EchoHub.Server.Services;

public partial class LinkEmbedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ImageToAsciiService _asciiService;
    private readonly ILogger<LinkEmbedService> _logger;

    public LinkEmbedService(
        IHttpClientFactory httpClientFactory,
        ImageToAsciiService asciiService,
        ILogger<LinkEmbedService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _asciiService = asciiService;
        _logger = logger;
    }

    /// <summary>
    /// Detect the first URL in message content and attempt to fetch OG embed data.
    /// Returns null if no URL found, fetch fails, or no useful OG data.
    /// Never throws — all errors are caught internally.
    /// </summary>
    public async Task<EmbedDto?> TryGetEmbedAsync(string content)
    {
        try
        {
            var url = ExtractFirstUrl(content);
            if (url is null)
                return null;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            if (uri.Scheme is not ("http" or "https"))
                return null;

            if (IsPrivateHost(uri))
                return null;

            using var cts = new CancellationTokenSource(
                TimeSpan.FromSeconds(HubConstants.EmbedFetchTimeoutSeconds));

            var client = _httpClientFactory.CreateClient("OgFetch");

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await client.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, cts.Token);

            if (!response.IsSuccessStatusCode)
                return null;

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType is null || !contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
                return null;

            var html = await ReadLimitedAsync(response, HubConstants.EmbedMaxHtmlBytes, cts.Token);
            if (string.IsNullOrWhiteSpace(html))
                return null;

            var ogTags = ParseOgTags(html);

            // Try og:title, fallback to <title> tag
            var title = ogTags.GetValueOrDefault("title");
            if (string.IsNullOrWhiteSpace(title))
            {
                var titleMatch = TitleTagRegex().Match(html);
                if (titleMatch.Success)
                    title = WebUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim());
            }

            // If no title at all, nothing useful to show
            if (string.IsNullOrWhiteSpace(title))
                return null;

            var siteName = ogTags.GetValueOrDefault("site_name");
            var description = ogTags.GetValueOrDefault("description");

            // Truncate description
            if (description is not null && description.Length > HubConstants.EmbedMaxDescriptionLength)
                description = description[..(HubConstants.EmbedMaxDescriptionLength - 3)] + "...";

            // HTML decode text fields
            title = WebUtility.HtmlDecode(title);
            siteName = siteName is not null ? WebUtility.HtmlDecode(siteName) : null;
            description = description is not null ? WebUtility.HtmlDecode(description) : null;

            // Attempt to fetch OG image thumbnail
            string? imageAscii = null;
            var imageUrl = ogTags.GetValueOrDefault("image");
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                imageAscii = await FetchImageThumbnailAsync(imageUrl, uri, cts.Token);
            }

            return new EmbedDto(siteName, title, description, imageAscii, url);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Embed fetch timed out for message content");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch embed");
            return null;
        }
    }

    private static string? ExtractFirstUrl(string content)
    {
        var match = UrlRegex().Match(content);
        if (!match.Success)
            return null;

        var url = match.Value;

        // Strip trailing punctuation that's likely not part of the URL
        url = url.TrimEnd('.', ',', '!', '?', ')', ']', ';', ':');

        return url;
    }

    private static bool IsPrivateHost(Uri uri)
    {
        if (uri.IsLoopback)
            return true;

        if (IPAddress.TryParse(uri.Host, out var ip))
        {
            var bytes = ip.GetAddressBytes();
            if (bytes.Length == 4)
            {
                if (bytes[0] == 10) return true;
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                if (bytes[0] == 192 && bytes[1] == 168) return true;
                if (bytes[0] == 127) return true;
                if (bytes[0] == 0) return true;
            }
        }

        // Also check hostname-based loopback
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static Dictionary<string, string> ParseOgTags(string html)
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Match: <meta property="og:key" content="value" />
        foreach (Match match in OgTagRegex().Matches(html))
        {
            var key = match.Groups[1].Value;
            var value = match.Groups[2].Value;
            tags.TryAdd(key, value);
        }

        // Match reversed order: <meta content="value" property="og:key" />
        foreach (Match match in OgTagReversedRegex().Matches(html))
        {
            var value = match.Groups[1].Value;
            var key = match.Groups[2].Value;
            tags.TryAdd(key, value);
        }

        return tags;
    }

    private async Task<string?> FetchImageThumbnailAsync(string imageUrl, Uri pageUri, CancellationToken ct)
    {
        try
        {
            // Resolve relative image URLs against the page URI
            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var imageUri))
            {
                if (!Uri.TryCreate(pageUri, imageUrl, out imageUri))
                    return null;
            }

            if (imageUri.Scheme is not ("http" or "https"))
                return null;

            if (IsPrivateHost(imageUri))
                return null;

            var client = _httpClientFactory.CreateClient("OgFetch");
            using var response = await client.GetAsync(imageUri, ct);

            if (!response.IsSuccessStatusCode)
                return null;

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType is null || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(ct);

            // Buffer into a MemoryStream for validation + conversion
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, ct);

            if (memoryStream.Length == 0 || memoryStream.Length > HubConstants.MaxFileSizeBytes)
                return null;

            memoryStream.Position = 0;

            if (!FileValidationHelper.IsValidImage(memoryStream))
                return null;

            memoryStream.Position = 0;

            return _asciiService.ConvertToAscii(memoryStream,
                HubConstants.EmbedThumbnailWidth,
                HubConstants.EmbedThumbnailHeight);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch OG image thumbnail from {Url}", imageUrl);
            return null;
        }
    }

    private static async Task<string> ReadLimitedAsync(HttpResponseMessage response, int maxBytes, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var buffer = new byte[maxBytes];
        var totalRead = 0;

        while (totalRead < maxBytes)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead, maxBytes - totalRead), ct);
            if (read == 0) break;
            totalRead += read;
        }

        // Try to detect encoding from Content-Type, default to UTF-8
        var charset = response.Content.Headers.ContentType?.CharSet;
        var encoding = charset is not null
            ? Encoding.GetEncoding(charset)
            : Encoding.UTF8;

        return encoding.GetString(buffer, 0, totalRead);
    }

    [GeneratedRegex(@"https?://[^\s<>""')\]]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"<meta\s+[^>]*?property\s*=\s*[""']og:(\w+)[""'][^>]*?content\s*=\s*[""']([^""']*)[""'][^>]*/?>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex OgTagRegex();

    [GeneratedRegex(@"<meta\s+[^>]*?content\s*=\s*[""']([^""']*)[""'][^>]*?property\s*=\s*[""']og:(\w+)[""'][^>]*/?>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex OgTagReversedRegex();

    [GeneratedRegex(@"<title[^>]*>([^<]+)</title>", RegexOptions.IgnoreCase)]
    private static partial Regex TitleTagRegex();
}
