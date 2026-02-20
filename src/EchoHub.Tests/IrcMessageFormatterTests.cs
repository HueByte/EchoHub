using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Irc;
using Xunit;

namespace EchoHub.Tests;

public class IrcMessageFormatterTests
{
    private static MessageDto CreateMessage(
        MessageType type = MessageType.Text,
        string content = "hello",
        string sender = "alice",
        string channel = "general",
        string? attachmentUrl = null,
        string? attachmentFileName = null,
        List<EmbedDto>? embeds = null) => new(
        Id: Guid.NewGuid(),
        Content: content,
        SenderUsername: sender,
        SenderNicknameColor: null,
        ChannelName: channel,
        Type: type,
        AttachmentUrl: attachmentUrl,
        AttachmentFileName: attachmentFileName,
        SentAt: DateTimeOffset.UtcNow,
        Embeds: embeds);

    // ── FormatMessage ─────────────────────────────────────────────────

    [Fact]
    public void FormatMessage_TextMessage_FormatsAsPRIVMSG()
    {
        var msg = CreateMessage(content: "Hello world");
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Single(lines);
        Assert.Contains("PRIVMSG #general :Hello world", lines[0]);
        Assert.StartsWith(":alice!alice@echohub", lines[0]);
    }

    [Fact]
    public void FormatMessage_TextMessage_WithEmbeds_AppendsEmbedLines()
    {
        var embeds = new List<EmbedDto>
        {
            new("GitHub", "Repo Title", "A description", null, "https://github.com/test")
        };
        var msg = CreateMessage(content: "check this out https://github.com/test", embeds: embeds);
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.True(lines.Count >= 2);
        Assert.Contains("PRIVMSG #general :check this out", lines[0]);
        // Embed lines contain the Unicode pipe char and site/title
        Assert.Contains("GitHub", lines[1]);
        Assert.Contains("Repo Title", lines[1]);
    }

    [Fact]
    public void FormatMessage_ImageMessage_IncludesImageTagAndDownloadUrl()
    {
        var msg = CreateMessage(
            type: MessageType.Image,
            content: "{F:FF0000}\u2588{X}",
            attachmentUrl: "/api/files/abc",
            attachmentFileName: "photo.png");
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.True(lines.Count >= 2);
        Assert.Contains("[Image: photo.png]", lines[0]);
        Assert.Contains("Download: /api/files/abc", lines[1]);
    }

    [Fact]
    public void FormatMessage_FileMessage_IncludesFileTag()
    {
        var msg = CreateMessage(
            type: MessageType.File,
            content: "report.pdf",
            attachmentUrl: "/api/files/xyz",
            attachmentFileName: "report.pdf");
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Single(lines);
        Assert.Contains("[File: report.pdf]", lines[0]);
        Assert.Contains("/api/files/xyz", lines[0]);
    }

    [Fact]
    public void FormatMessage_AudioMessage_IncludesMusicNoteAndAudioTag()
    {
        var msg = CreateMessage(
            type: MessageType.Audio,
            content: "song.mp3",
            attachmentUrl: "/api/files/def",
            attachmentFileName: "song.mp3");
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Single(lines);
        Assert.Contains("\u266a", lines[0]); // ♪
        Assert.Contains("[Audio: song.mp3]", lines[0]);
        Assert.Contains("/api/files/def", lines[0]);
    }

    // ── ColorTagsToAnsi ───────────────────────────────────────────────

    [Fact]
    public void ColorTagsToAnsi_ForegroundTag_ConvertsToAnsiEscape()
    {
        var result = IrcMessageFormatter.ColorTagsToAnsi("{F:FF0000}text");
        Assert.Contains("\x1b[38;2;255;0;0m", result);
        Assert.Contains("text", result);
    }

    [Fact]
    public void ColorTagsToAnsi_BackgroundTag_ConvertsToAnsiEscape()
    {
        var result = IrcMessageFormatter.ColorTagsToAnsi("{B:00FF00}text");
        Assert.Contains("\x1b[48;2;0;255;0m", result);
    }

    [Fact]
    public void ColorTagsToAnsi_ResetTag_ConvertsToAnsiReset()
    {
        var result = IrcMessageFormatter.ColorTagsToAnsi("{X}");
        Assert.Equal("\x1b[0m", result);
    }

    [Fact]
    public void ColorTagsToAnsi_NoTags_ReturnsUnchanged()
    {
        var result = IrcMessageFormatter.ColorTagsToAnsi("plain text");
        Assert.Equal("plain text", result);
    }

    [Fact]
    public void ColorTagsToAnsi_MultipleTags_ConvertsAll()
    {
        var result = IrcMessageFormatter.ColorTagsToAnsi("{F:FF0000}red{F:0000FF}blue{X}");
        Assert.Contains("\x1b[38;2;255;0;0m", result);
        Assert.Contains("\x1b[38;2;0;0;255m", result);
        Assert.Contains("\x1b[0m", result);
        Assert.Contains("red", result);
        Assert.Contains("blue", result);
    }

    // ── SplitMessage ──────────────────────────────────────────────────

    [Fact]
    public void SplitMessage_ShortMessage_ReturnsSingleChunk()
    {
        var result = IrcMessageFormatter.SplitMessage("Hello", 400);
        Assert.Single(result);
        Assert.Equal("Hello", result[0]);
    }

    [Fact]
    public void SplitMessage_LongMessage_SplitsAtWordBoundary()
    {
        var words = string.Join(" ", Enumerable.Repeat("word", 200));
        var result = IrcMessageFormatter.SplitMessage(words, 50);

        Assert.True(result.Count > 1);
        foreach (var chunk in result)
            Assert.True(System.Text.Encoding.UTF8.GetByteCount(chunk) <= 50);
    }

    [Fact]
    public void SplitMessage_EmptyMessage_ReturnsSingleEmptyChunk()
    {
        var result = IrcMessageFormatter.SplitMessage("", 400);
        Assert.Single(result);
        Assert.Equal("", result[0]);
    }

    [Fact]
    public void SplitMessage_SingleLongWord_KeptAsOneChunk()
    {
        var longWord = new string('a', 500);
        var result = IrcMessageFormatter.SplitMessage(longWord, 400);

        // Single word can't be split at word boundary, so it stays as one chunk
        Assert.Single(result);
        Assert.Equal(longWord, result[0]);
    }
}
