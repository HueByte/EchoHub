using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Irc;
using Xunit;

namespace EchoHub.Tests;

public class IrcMessageFormatterTests
{
    private static MessageDto CreateMessage(
        string content = "hello",
        string sender = "alice",
        string channel = "general",
        List<AttachmentDto>? attachments = null,
        List<EmbedDto>? embeds = null,
        ReplyRefDto? replyTo = null) => new(
        Id: Guid.NewGuid(),
        Content: content,
        SenderUsername: sender,
        SenderNicknameColor: null,
        ChannelName: channel,
        SentAt: DateTimeOffset.UtcNow,
        Attachments: attachments,
        Embeds: embeds,
        ReplyTo: replyTo);

    // ── CTCP ACTION (/me) ───────────────────────────────────

    [Fact]
    public void FormatMessage_ActionContent_EmitsCtcpAction()
    {
        var msg = CreateMessage(content: "\u0001ACTION waves at everyone\u0001");
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Single(lines);
        Assert.Contains("PRIVMSG #general :\u0001ACTION waves at everyone\u0001", lines[0]);
    }

    [Fact]
    public void FormatMessage_LongActionContent_EachChunkIsWellFormedCtcp()
    {
        var longText = string.Join(' ', Enumerable.Repeat("wordyword", 80));
        var msg = CreateMessage(content: "\u0001ACTION " + longText + "\u0001");
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.True(lines.Count > 1);
        foreach (var line in lines)
        {
            var payload = line[(line.IndexOf(" :", StringComparison.Ordinal) + 2)..];
            Assert.StartsWith("\u0001ACTION ", payload);
            Assert.EndsWith("\u0001", payload);
        }
    }

    // ── Replies ───────────────────────────────────────────

    [Fact]
    public void FormatMessage_Reply_PrefixesQuoteConvention()
    {
        var reply = new ReplyRefDto(Guid.NewGuid(), "bob", "the original text");
        var msg = CreateMessage(content: "I agree", replyTo: reply);
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Single(lines);
        Assert.Contains("PRIVMSG #general :> bob: the original text | I agree", lines[0]);
    }

    [Fact]
    public void FormatMessage_ReplyToLongMessage_SnippetTruncated()
    {
        var reply = new ReplyRefDto(Guid.NewGuid(), "bob", new string('x', 300));
        var msg = CreateMessage(content: "ok", replyTo: reply);
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Single(lines);
        Assert.Contains("… | ok", lines[0]);
        Assert.DoesNotContain(new string('x', 100), lines[0]);
    }

    [Fact]
    public void FormatMessage_ReplyToEncryptedContent_ShowsPlaceholder()
    {
        var reply = new ReplyRefDto(Guid.NewGuid(), "bob", "$RC1$AAAA$BBBB$CCCC");
        var msg = CreateMessage(content: "ok", replyTo: reply);
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Contains("> bob: [encrypted] | ok", lines[0]);
    }

    [Fact]
    public void FormatMessage_ReplyToAction_SnippetRendersAsAction()
    {
        var reply = new ReplyRefDto(Guid.NewGuid(), "bob", "\u0001ACTION waves\u0001");
        var msg = CreateMessage(content: "nice wave", replyTo: reply);
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Contains("> bob: * bob waves | nice wave", lines[0]);
    }

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
        Assert.Contains("GitHub", lines[1]);
        Assert.Contains("Repo Title", lines[1]);
    }

    [Fact]
    public void FormatMessage_ImageAttachment_IncludesImageTagAndDownloadUrl()
    {
        var msg = CreateMessage(
            content: "",
            attachments: [new AttachmentDto(AttachmentKind.Image, "/api/files/abc", "photo.png", 0, "{F:FF0000}█{X}")]);
        var lines = IrcMessageFormatter.FormatMessage(msg);

        // Images are a single link line — the ASCII preview is never sent to IRC clients
        Assert.Single(lines);
        Assert.Contains("[Image: photo.png]", lines[0]);
        Assert.Contains("/api/files/abc", lines[0]);
    }

    [Fact]
    public void FormatMessage_WithPublicBaseUrl_EmitsAbsoluteAttachmentLinks()
    {
        var msg = CreateMessage(
            content: "",
            attachments: [new AttachmentDto(AttachmentKind.Image, "/api/files/abc", "photo.png", 0, null)]);
        var lines = IrcMessageFormatter.FormatMessage(msg, "https://chat.example.com/");

        Assert.Single(lines);
        Assert.Contains("https://chat.example.com/api/files/abc", lines[0]);
    }

    [Fact]
    public void FormatMessage_FileAttachment_IncludesFileTag()
    {
        var msg = CreateMessage(
            content: "",
            attachments: [new AttachmentDto(AttachmentKind.File, "/api/files/xyz", "report.pdf", 0)]);
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Single(lines);
        Assert.Contains("[File: report.pdf]", lines[0]);
        Assert.Contains("/api/files/xyz", lines[0]);
    }

    [Fact]
    public void FormatMessage_AudioAttachment_IncludesMusicNoteAndAudioTag()
    {
        var msg = CreateMessage(
            content: "",
            attachments: [new AttachmentDto(AttachmentKind.Audio, "/api/files/def", "song.mp3", 0)]);
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Single(lines);
        Assert.Contains("♪", lines[0]);
        Assert.Contains("[Audio: song.mp3]", lines[0]);
        Assert.Contains("/api/files/def", lines[0]);
    }

    [Fact]
    public void FormatMessage_CaptionWithAttachment_RendersBoth()
    {
        var msg = CreateMessage(
            content: "check this photo",
            attachments: [new AttachmentDto(AttachmentKind.Image, "/api/files/p", "pic.png", 0, null)]);
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Contains(lines, l => l.Contains("check this photo"));
        Assert.Contains(lines, l => l.Contains("[Image: pic.png]"));
    }

    [Fact]
    public void FormatMessage_MultipleAttachments_RendersEach()
    {
        var msg = CreateMessage(
            content: "",
            attachments:
            [
                new AttachmentDto(AttachmentKind.Image, "/api/files/1", "a.png", 0, null),
                new AttachmentDto(AttachmentKind.Audio, "/api/files/2", "b.mp3", 0),
                new AttachmentDto(AttachmentKind.File, "/api/files/3", "c.pdf", 0),
            ]);
        var lines = IrcMessageFormatter.FormatMessage(msg);

        Assert.Contains(lines, l => l.Contains("[Image: a.png]"));
        Assert.Contains(lines, l => l.Contains("[Audio: b.mp3]"));
        Assert.Contains(lines, l => l.Contains("[File: c.pdf]"));
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

        Assert.Single(result);
        Assert.Equal(longWord, result[0]);
    }
}
