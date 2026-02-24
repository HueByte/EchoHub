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
    private readonly ILogger<LinkEmbedService> _logger;

    public LinkEmbedService(
        IHttpClientFactory httpClientFactory,
        ILogger<LinkEmbedService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Detect all URLs in message content and attempt to fetch OG embed data for each.
    /// Returns null if no URLs found or all fetches fail.
    /// Never throws — all errors are caught internally.
    /// </summary>
    public async Task<List<EmbedDto>?> TryGetEmbedsAsync(string content)
    {
        var urls = ExtractUrls(content);
        if (urls.Count == 0)
            return null;

        var embeds = new List<EmbedDto>();

        foreach (var url in urls)
        {
            try
            {
                var embed = await FetchEmbedForUrlAsync(url);
                if (embed is not null)
                    embeds.Add(embed);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to fetch embed for {Url}", url);
            }
        }

        return embeds.Count > 0 ? embeds : null;
    }

    private async Task<EmbedDto?> FetchEmbedForUrlAsync(string url)
    {
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

        // Truncate very long descriptions but keep a generous limit
        if (description is not null && description.Length > HubConstants.EmbedMaxDescriptionLength)
            description = description[..(HubConstants.EmbedMaxDescriptionLength - 3)] + "...";

        // HTML decode text fields
        title = WebUtility.HtmlDecode(title);
        siteName = siteName is not null ? WebUtility.HtmlDecode(siteName) : null;
        description = description is not null ? WebUtility.HtmlDecode(description) : null;

        // Extract theme-color meta tag for embed border color
        var themeColor = ParseThemeColor(html);

        return new EmbedDto(siteName, title, description, null, url, themeColor);
    }

    private static string? ParseThemeColor(string html)
    {
        // ThemeColorRegex: group 3 = color value
        var match = ThemeColorRegex().Match(html);
        var color = match.Success ? match.Groups[3].Value.Trim() : null;

        if (color is null)
        {
            // ThemeColorReversedRegex: group 2 = color value
            match = ThemeColorReversedRegex().Match(html);
            color = match.Success ? match.Groups[2].Value.Trim() : null;
        }

        if (color is null)
            return null;

        if (color.Length == 4 && color[0] == '#'
            && IsHexDigit(color[1]) && IsHexDigit(color[2]) && IsHexDigit(color[3]))
        {
            // Expand #RGB to #RRGGBB
            return $"#{color[1]}{color[1]}{color[2]}{color[2]}{color[3]}{color[3]}";
        }

        if (color.Length == 7 && color[0] == '#'
            && color[1..].All(IsHexDigit))
        {
            return color;
        }

        return null;
    }

    private static bool IsHexDigit(char c) =>
        c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');

    private static List<string> ExtractUrls(string content)
    {
        var urls = new List<string>();

        foreach (Match match in UrlRegex().Matches(content))
        {
            var url = match.Value.TrimEnd('.', ',', '!', '?', ')', ']', ';', ':');
            if (!urls.Contains(url))
                urls.Add(url);

            if (urls.Count >= HubConstants.EmbedMaxUrlsPerMessage)
                break;
        }

        return urls;
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
        // Groups: 1=prop quote, 2=key, 3=content quote, 4=value
        foreach (Match match in OgTagRegex().Matches(html))
        {
            var key = match.Groups[2].Value;
            var value = match.Groups[4].Value;
            tags.TryAdd(key, value);
        }

        // Match reversed order: <meta content="value" property="og:key" />
        // Groups: 1=content quote, 2=value, 3=prop quote, 4=key
        foreach (Match match in OgTagReversedRegex().Matches(html))
        {
            var value = match.Groups[2].Value;
            var key = match.Groups[4].Value;
            tags.TryAdd(key, value);
        }

        return tags;
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

    [GeneratedRegex(@"https?://[^\s<>""')\]]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"<meta\s+[^>]*?property\s*=\s*([""'])og:(\w+)\1[^>]*?content\s*=\s*([""'])(.*?)\3[^>]*/?>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex OgTagRegex();

    [GeneratedRegex(@"<meta\s+[^>]*?content\s*=\s*([""'])(.*?)\1[^>]*?property\s*=\s*([""'])og:(\w+)\3[^>]*/?>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex OgTagReversedRegex();

    [GeneratedRegex(@"<title[^>]*>([^<]+)</title>", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TitleTagRegex();

    // <meta name="theme-color" content="#hex">
    [GeneratedRegex(@"<meta\s+[^>]*?name\s*=\s*([""'])theme-color\1[^>]*?content\s*=\s*([""'])(.*?)\2[^>]*/?>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex ThemeColorRegex();

    // <meta content="#hex" name="theme-color">
    [GeneratedRegex(@"<meta\s+[^>]*?content\s*=\s*([""'])(.*?)\1[^>]*?name\s*=\s*([""'])theme-color\3[^>]*/?>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex ThemeColorReversedRegex();
}
