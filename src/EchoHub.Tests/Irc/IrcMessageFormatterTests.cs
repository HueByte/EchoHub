using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Irc;
using Xunit;

namespace EchoHub.Tests.Irc;

public class IrcMessageFormatterTests
{
    private static MessageDto CreateTextMessage(string content, string sender = "alice",
        string channel = "general", List<EmbedDto>? embeds = null)
    {
        return new MessageDto(
            Guid.NewGuid(), content, sender, null, channel, DateTimeOffset.UtcNow, Embeds: embeds);
    }

    private static MessageDto CreateImageMessage(string asciiArt, string fileName = "image.png",
        string url = "https://example.com/image.png", string sender = "alice", string channel = "general")
    {
        return new MessageDto(
            Guid.NewGuid(), "", sender, null, channel, DateTimeOffset.UtcNow,
            [new AttachmentDto(AttachmentKind.Image, url, fileName, 0, asciiArt)]);
    }

    private static MessageDto CreateFileMessage(string fileName = "doc.pdf",
        string url = "https://example.com/doc.pdf", string sender = "alice", string channel = "general")
    {
        return new MessageDto(
            Guid.NewGuid(), "", sender, null, channel, DateTimeOffset.UtcNow,
            [new AttachmentDto(AttachmentKind.File, url, fileName, 0)]);
    }

    private static MessageDto CreateAudioMessage(string fileName = "song.mp3",
        string url = "https://example.com/song.mp3", string sender = "alice", string channel = "general")
    {
        return new MessageDto(
            Guid.NewGuid(), "", sender, null, channel, DateTimeOffset.UtcNow,
            [new AttachmentDto(AttachmentKind.Audio, url, fileName, 0)]);
    }

    // ── FormatMessage ────────────────────────────────────────────────────

    [Fact]
    public void FormatMessage_TextMessage_FormatsAsPrivmsg()
    {
        var msg = CreateTextMessage("Hello world");
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Single(lines);
        Assert.Equal(":alice!alice@echohub PRIVMSG #general :Hello world", lines[0]);
    }

    [Fact]
    public void FormatMessage_TextMessage_IncludesChannelHash()
    {
        var msg = CreateTextMessage("test", channel: "random");
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Contains("#random", lines[0]);
    }

    [Fact]
    public void FormatMessage_TextWithEmbeds_AppendsEmbedLines()
    {
        var embeds = new List<EmbedDto>
        {
            new("Example Site", "Page Title", "A description of the page", null, "https://example.com")
        };
        var msg = CreateTextMessage("Check this: https://example.com", embeds: embeds);
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.True(lines.Count >= 2);
        Assert.Contains("Check this: https://example.com", lines[0]);
        // Embed header
        Assert.Contains("Example Site", lines[1]);
        Assert.Contains("Page Title", lines[1]);
    }

    [Fact]
    public void FormatMessage_EmbedWithDescription_IncludesDescription()
    {
        var embeds = new List<EmbedDto>
        {
            new("Site", "Title", "This is a description", null, "https://example.com")
        };
        var msg = CreateTextMessage("url", embeds: embeds);
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.True(lines.Count >= 3);
        Assert.Contains("This is a description", lines[2]);
    }

    [Fact]
    public void FormatMessage_EmbedWithLongDescription_Truncates()
    {
        var longDesc = new string('x', 300);
        var embeds = new List<EmbedDto>
        {
            new("Site", "Title", longDesc, null, "https://example.com")
        };
        var msg = CreateTextMessage("url", embeds: embeds);
        var lines = IrcMessageFormatter.FormatMessage(msg);

        var descLine = lines.First(l => l.Contains("xxx"));
        Assert.Contains("...", descLine);
        // Should be truncated to ~200 chars
        var descContent = descLine[(descLine.LastIndexOf(':') + 2)..]; // after ":│ "
        Assert.True(descContent.Length <= 210);
    }

    [Fact]
    public void FormatMessage_ImageMessage_IncludesFileNameAndUrl()
    {
        var msg = CreateImageMessage("##\n##", "photo.jpg", "https://example.com/photo.jpg");
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Contains(lines, l => l.Contains("[Image: photo.jpg]") && l.Contains("https://example.com/photo.jpg"));
    }

    [Fact]
    public void FormatMessage_ImageMessage_NeverEmitsAsciiArt()
    {
        // Images are shared as plain links (the common IRC practice) — never color art,
        // regardless of what the preview contains.
        var msg = CreateImageMessage("{F:FF0000}█{X}\nline2");
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Single(lines);
        Assert.Contains("[Image: image.png]", lines[0]);
        Assert.DoesNotContain(lines, l => l.Contains("line2"));
    }

    [Fact]
    public void FormatMessage_RelativeUrl_JoinedWithPublicBaseUrl()
    {
        var msg = CreateImageMessage("art", "photo.jpg", "/api/files/abc");
        var lines = IrcMessageFormatter.FormatMessage(msg, "https://chat.example.com");

        Assert.Single(lines);
        Assert.Contains("https://chat.example.com/api/files/abc", lines[0]);
    }

    [Fact]
    public void FormatMessage_AbsoluteUrl_NotRewrittenByPublicBaseUrl()
    {
        var msg = CreateImageMessage("art", "photo.jpg", "https://cdn.example.com/photo.jpg");
        var lines = IrcMessageFormatter.FormatMessage(msg, "https://chat.example.com");

        Assert.Contains("https://cdn.example.com/photo.jpg", lines[0]);
        Assert.DoesNotContain("https://chat.example.com", lines[0]);
    }

    [Fact]
    public void ToAbsoluteUrl_NoBaseUrl_ReturnsRelativeUnchanged()
    {
        Assert.Equal("/api/files/abc", IrcMessageFormatter.ToAbsoluteUrl("/api/files/abc", null));
        Assert.Equal("/api/files/abc", IrcMessageFormatter.ToAbsoluteUrl("/api/files/abc", "  "));
    }

    [Fact]
    public void ToAbsoluteUrl_TrailingSlashBase_JoinsWithoutDoubleSlash()
    {
        Assert.Equal("https://x.example/api/files/1",
            IrcMessageFormatter.ToAbsoluteUrl("/api/files/1", "https://x.example/"));
    }

    [Fact]
    public void FormatMessage_FileMessage_FormatsCorrectly()
    {
        var msg = CreateFileMessage("report.pdf", "https://example.com/report.pdf");
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Single(lines);
        Assert.Contains("[File: report.pdf]", lines[0]);
        Assert.Contains("https://example.com/report.pdf", lines[0]);
    }

    [Fact]
    public void FormatMessage_AudioMessage_FormatsWithMusicNote()
    {
        var msg = CreateAudioMessage("track.mp3", "https://example.com/track.mp3");
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Single(lines);
        Assert.Contains("\u266a", lines[0]); // ♪
        Assert.Contains("[Audio: track.mp3]", lines[0]);
        Assert.Contains("https://example.com/track.mp3", lines[0]);
    }

    // ── SplitMessage ─────────────────────────────────────────────────────

    [Fact]
    public void SplitMessage_ShortMessage_ReturnsSingleChunk()
    {
        var chunks = IrcMessageFormatter.SplitMessage("Hello", 400);
        Assert.Single(chunks);
        Assert.Equal("Hello", chunks[0]);
    }

    [Fact]
    public void SplitMessage_ExactlyAtLimit_ReturnsSingleChunk()
    {
        var msg = new string('a', 400);
        var chunks = IrcMessageFormatter.SplitMessage(msg, 400);
        Assert.Single(chunks);
    }

    [Fact]
    public void SplitMessage_LongMessage_SplitsAtWordBoundary()
    {
        // Create a message that's longer than 50 bytes
        var words = string.Join(" ", Enumerable.Repeat("hello", 20)); // 20 * 6 - 1 = 119 bytes
        var chunks = IrcMessageFormatter.SplitMessage(words, 50);

        Assert.True(chunks.Count > 1);
        // Each chunk should be roughly <= 50 bytes
        foreach (var chunk in chunks)
        {
            Assert.True(System.Text.Encoding.UTF8.GetByteCount(chunk) <= 55,
                $"Chunk too long: {chunk.Length} chars");
        }
        // Reassembled content should match original
        var reassembled = string.Join(" ", chunks);
        Assert.Equal(words, reassembled);
    }

    [Fact]
    public void SplitMessage_SingleLongWord_ForcedIntoOneChunk()
    {
        var longWord = new string('a', 500);
        var chunks = IrcMessageFormatter.SplitMessage(longWord, 400);
        // A single word can't be split at word boundaries, so it stays as one chunk
        Assert.Single(chunks);
        Assert.Equal(longWord, chunks[0]);
    }

    [Fact]
    public void SplitMessage_EmptyString_ReturnsSingleEmpty()
    {
        var chunks = IrcMessageFormatter.SplitMessage("", 400);
        Assert.Single(chunks);
        Assert.Equal("", chunks[0]);
    }

    [Fact]
    public void FormatMessage_UrlOnOwnLine_EmitsUrlAsItsOwnPrivmsg()
    {
        // Regression: multi-line content used to go out as ONE line with a raw \n
        // embedded. IRC clients drop everything after the newline as a malformed
        // frame, so a URL on its own line silently vanished while the embed lines
        // (separate, valid PRIVMSGs) still rendered.
        var embeds = new List<EmbedDto>
        {
            new("Example Site", "Page Title", "Description", null, "https://example.com")
        };
        var msg = CreateTextMessage("check this out\nhttps://example.com", embeds: embeds);
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Contains(lines, l => l.EndsWith("PRIVMSG #general :check this out"));
        Assert.Contains(lines, l => l.EndsWith("PRIVMSG #general :https://example.com"));
        Assert.All(lines, l => Assert.DoesNotContain('\n', l));
        Assert.All(lines, l => Assert.DoesNotContain('\r', l));
    }

    [Fact]
    public void SplitMessage_MultiLineContent_SplitsOnNewlines()
    {
        var chunks = IrcMessageFormatter.SplitMessage("line one\nhttps://example.com/page", 400);

        Assert.Equal(2, chunks.Count);
        Assert.Equal("line one", chunks[0]);
        Assert.Equal("https://example.com/page", chunks[1]);
    }

    [Fact]
    public void SplitMessage_UnicodeContent_CountsUtf8Bytes()
    {
        // Japanese text: each char is 3 bytes in UTF-8
        var text = string.Join(" ", Enumerable.Repeat("\u3042\u3044\u3046", 50));
        var chunks = IrcMessageFormatter.SplitMessage(text, 100);

        Assert.True(chunks.Count > 1);
        foreach (var chunk in chunks)
        {
            Assert.True(System.Text.Encoding.UTF8.GetByteCount(chunk) <= 110,
                $"Chunk too long in bytes: {System.Text.Encoding.UTF8.GetByteCount(chunk)}");
        }
    }

}
