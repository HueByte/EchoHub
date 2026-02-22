using System.Reflection;
using EchoHub.Server.Services;
using Xunit;

namespace EchoHub.Tests;

public class LinkEmbedServiceTests
{
    private static readonly MethodInfo ExtractUrlsMethod = typeof(LinkEmbedService)
        .GetMethod("ExtractUrls", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo IsPrivateHostMethod = typeof(LinkEmbedService)
        .GetMethod("IsPrivateHost", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo ParseOgTagsMethod = typeof(LinkEmbedService)
        .GetMethod("ParseOgTags", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static List<string> ExtractUrls(string content) =>
        (List<string>)ExtractUrlsMethod.Invoke(null, [content])!;

    private static bool IsPrivateHost(Uri uri) =>
        (bool)IsPrivateHostMethod.Invoke(null, [uri])!;

    private static Dictionary<string, string> ParseOgTags(string html) =>
        (Dictionary<string, string>)ParseOgTagsMethod.Invoke(null, [html])!;

    // ── ExtractUrls ───────────────────────────────────────────────────

    [Fact]
    public void ExtractUrls_SingleUrl_ReturnsIt()
    {
        var urls = ExtractUrls("Check this: https://example.com");
        Assert.Single(urls);
        Assert.Equal("https://example.com", urls[0]);
    }

    [Fact]
    public void ExtractUrls_MultipleUrls_ReturnsAll()
    {
        var urls = ExtractUrls("See https://a.com and https://b.com");
        Assert.Equal(2, urls.Count);
        Assert.Contains("https://a.com", urls);
        Assert.Contains("https://b.com", urls);
    }

    [Fact]
    public void ExtractUrls_UrlWithTrailingPunctuation_Trimmed()
    {
        var urls = ExtractUrls("Visit https://example.com.");
        Assert.Single(urls);
        Assert.Equal("https://example.com", urls[0]);
    }

    [Fact]
    public void ExtractUrls_NoUrls_ReturnsEmpty()
    {
        var urls = ExtractUrls("No links here");
        Assert.Empty(urls);
    }

    [Fact]
    public void ExtractUrls_MaxUrlsLimit_Respected()
    {
        // EmbedMaxUrlsPerMessage = 3
        var text = "https://a.com https://b.com https://c.com https://d.com https://e.com";
        var urls = ExtractUrls(text);
        Assert.Equal(3, urls.Count);
    }

    [Fact]
    public void ExtractUrls_DuplicateUrls_Deduped()
    {
        var urls = ExtractUrls("https://example.com and https://example.com again");
        Assert.Single(urls);
    }

    [Fact]
    public void ExtractUrls_HttpUrl_Extracted()
    {
        var urls = ExtractUrls("http://example.com");
        Assert.Single(urls);
        Assert.StartsWith("http://", urls[0]);
    }

    // ── IsPrivateHost ─────────────────────────────────────────────────

    [Fact]
    public void IsPrivateHost_Localhost_ReturnsTrue()
    {
        Assert.True(IsPrivateHost(new Uri("http://localhost/test")));
    }

    [Fact]
    public void IsPrivateHost_LoopbackIP_ReturnsTrue()
    {
        Assert.True(IsPrivateHost(new Uri("http://127.0.0.1/test")));
    }

    [Fact]
    public void IsPrivateHost_10Network_ReturnsTrue()
    {
        Assert.True(IsPrivateHost(new Uri("http://10.0.0.1/test")));
    }

    [Fact]
    public void IsPrivateHost_172_16Network_ReturnsTrue()
    {
        Assert.True(IsPrivateHost(new Uri("http://172.16.0.1/test")));
    }

    [Fact]
    public void IsPrivateHost_192_168Network_ReturnsTrue()
    {
        Assert.True(IsPrivateHost(new Uri("http://192.168.1.1/test")));
    }

    [Fact]
    public void IsPrivateHost_PublicIP_ReturnsFalse()
    {
        Assert.False(IsPrivateHost(new Uri("http://8.8.8.8/test")));
    }

    [Fact]
    public void IsPrivateHost_PublicDomain_ReturnsFalse()
    {
        Assert.False(IsPrivateHost(new Uri("https://example.com/test")));
    }

    [Fact]
    public void IsPrivateHost_ZeroIP_ReturnsTrue()
    {
        Assert.True(IsPrivateHost(new Uri("http://0.0.0.0/test")));
    }

    // ── ParseOgTags ───────────────────────────────────────────────────

    [Fact]
    public void ParseOgTags_StandardOgTags_ParsedCorrectly()
    {
        var html = """
            <html><head>
            <meta property="og:title" content="Test Title" />
            <meta property="og:description" content="A description" />
            <meta property="og:site_name" content="TestSite" />
            </head></html>
            """;
        var tags = ParseOgTags(html);

        Assert.Equal("Test Title", tags["title"]);
        Assert.Equal("A description", tags["description"]);
        Assert.Equal("TestSite", tags["site_name"]);
    }

    [Fact]
    public void ParseOgTags_ReversedOrder_ParsedCorrectly()
    {
        var html = """<meta content="Reversed Title" property="og:title" />""";
        var tags = ParseOgTags(html);

        Assert.Equal("Reversed Title", tags["title"]);
    }

    [Fact]
    public void ParseOgTags_NoOgTags_ReturnsEmptyDictionary()
    {
        var html = "<html><head><title>Page</title></head></html>";
        var tags = ParseOgTags(html);

        Assert.Empty(tags);
    }

    [Fact]
    public void ParseOgTags_SingleQuotes_ParsedCorrectly()
    {
        var html = """<meta property='og:title' content='Single Quoted' />""";
        var tags = ParseOgTags(html);

        Assert.Equal("Single Quoted", tags["title"]);
    }

    [Fact]
    public void ParseOgTags_DuplicateKeys_FirstWins()
    {
        var html = """
            <meta property="og:title" content="First" />
            <meta property="og:title" content="Second" />
            """;
        var tags = ParseOgTags(html);

        Assert.Equal("First", tags["title"]);
    }
}
