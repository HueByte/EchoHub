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
            Guid.NewGuid(), content, sender, null, channel,
            MessageType.Text, null, null, DateTimeOffset.UtcNow, Embeds: embeds);
    }

    private static MessageDto CreateImageMessage(string asciiArt, string fileName = "image.png",
        string url = "https://example.com/image.png", string sender = "alice", string channel = "general")
    {
        return new MessageDto(
            Guid.NewGuid(), asciiArt, sender, null, channel,
            MessageType.Image, url, fileName, DateTimeOffset.UtcNow);
    }

    private static MessageDto CreateFileMessage(string fileName = "doc.pdf",
        string url = "https://example.com/doc.pdf", string sender = "alice", string channel = "general")
    {
        return new MessageDto(
            Guid.NewGuid(), "", sender, null, channel,
            MessageType.File, url, fileName, DateTimeOffset.UtcNow);
    }

    private static MessageDto CreateAudioMessage(string fileName = "song.mp3",
        string url = "https://example.com/song.mp3", string sender = "alice", string channel = "general")
    {
        return new MessageDto(
            Guid.NewGuid(), "", sender, null, channel,
            MessageType.Audio, url, fileName, DateTimeOffset.UtcNow);
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

        Assert.Contains(lines, l => l.Contains("[Image: photo.jpg]"));
        Assert.Contains(lines, l => l.Contains("Download: https://example.com/photo.jpg"));
    }

    [Fact]
    public void FormatMessage_ImageMessage_IncludesAsciiArt()
    {
        var msg = CreateImageMessage("line1\nline2");
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Contains(lines, l => l.Contains("line1"));
        Assert.Contains(lines, l => l.Contains("line2"));
    }

    [Fact]
    public void FormatMessage_ImageMessage_SkipsEmptyAsciiLines()
    {
        var msg = CreateImageMessage("line1\n\nline2");
        var lines = IrcMessageFormatter.FormatMessage(msg);

        // Empty lines should be skipped
        var asciiLines = lines.Where(l => !l.Contains("[Image:") && !l.Contains("Download:")).ToList();
        Assert.Equal(2, asciiLines.Count);
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

    // ── ColorTagsToAnsi ──────────────────────────────────────────────────

    [Fact]
    public void ColorTagsToAnsi_NoTags_ReturnsUnchanged()
    {
        Assert.Equal("Hello world", IrcMessageFormatter.ColorTagsToAnsi("Hello world"));
    }

    [Fact]
    public void ColorTagsToAnsi_ForegroundTag_ConvertsToAnsi()
    {
        var result = IrcMessageFormatter.ColorTagsToAnsi("{F:FF0000}Red text");
        Assert.Equal("\x1b[38;2;255;0;0mRed text", result);
    }

    [Fact]
    public void ColorTagsToAnsi_BackgroundTag_ConvertsToAnsi()
    {
        var result = IrcMessageFormatter.ColorTagsToAnsi("{B:00FF00}Green bg");
        Assert.Equal("\x1b[48;2;0;255;0mGreen bg", result);
    }

    [Fact]
    public void ColorTagsToAnsi_ResetTag_ConvertsToReset()
    {
        var result = IrcMessageFormatter.ColorTagsToAnsi("{F:FF0000}Red{X} Normal");
        Assert.Equal("\x1b[38;2;255;0;0mRed\x1b[0m Normal", result);
    }

    [Fact]
    public void ColorTagsToAnsi_MultipleTags_ConvertsAll()
    {
        var result = IrcMessageFormatter.ColorTagsToAnsi("{F:FF0000}Red {F:0000FF}Blue{X}");
        Assert.Contains("\x1b[38;2;255;0;0m", result);
        Assert.Contains("\x1b[38;2;0;0;255m", result);
        Assert.Contains("\x1b[0m", result);
    }

    [Fact]
    public void ColorTagsToAnsi_LowercaseHex_ConvertsCorrectly()
    {
        var result = IrcMessageFormatter.ColorTagsToAnsi("{F:ff8800}text");
        Assert.Equal("\x1b[38;2;255;136;0mtext", result);
    }

    [Fact]
    public void ColorTagsToAnsi_NoBraces_SkipsProcessing()
    {
        var text = "plain text without braces";
        Assert.Equal(text, IrcMessageFormatter.ColorTagsToAnsi(text));
    }

    [Fact]
    public void ColorTagsToAnsi_ExistingAnsiCodes_PreservesUnchanged()
    {
        var text = "\x1b[31mAlready colored\x1b[0m";
        Assert.Equal(text, IrcMessageFormatter.ColorTagsToAnsi(text));
    }
}
