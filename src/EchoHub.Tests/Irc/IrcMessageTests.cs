using EchoHub.Server.Irc;
using Xunit;

namespace EchoHub.Tests.Irc;

public class IrcMessageTests
{
    [Fact]
    public void Parse_SimpleCommand_ExtractsCommand()
    {
        var msg = IrcMessage.Parse("PING");
        Assert.Equal("PING", msg.Command);
        Assert.Null(msg.Prefix);
        Assert.Empty(msg.Parameters);
    }

    [Fact]
    public void Parse_CommandWithOneParam_ExtractsParam()
    {
        var msg = IrcMessage.Parse("NICK alice");
        Assert.Equal("NICK", msg.Command);
        Assert.Single(msg.Parameters);
        Assert.Equal("alice", msg.Parameters[0]);
    }

    [Fact]
    public void Parse_CommandWithTrailing_ExtractsTrailingAsLastParam()
    {
        var msg = IrcMessage.Parse("PRIVMSG #general :Hello world!");
        Assert.Equal("PRIVMSG", msg.Command);
        Assert.Equal(2, msg.Parameters.Count);
        Assert.Equal("#general", msg.Parameters[0]);
        Assert.Equal("Hello world!", msg.Parameters[1]);
        Assert.Equal("Hello world!", msg.Trailing);
    }

    [Fact]
    public void Parse_MessageWithPrefix_ExtractsPrefix()
    {
        var msg = IrcMessage.Parse(":alice!alice@echohub PRIVMSG #general :hi");
        Assert.Equal("alice!alice@echohub", msg.Prefix);
        Assert.Equal("PRIVMSG", msg.Command);
        Assert.Equal(2, msg.Parameters.Count);
        Assert.Equal("#general", msg.Parameters[0]);
        Assert.Equal("hi", msg.Parameters[1]);
    }

    [Fact]
    public void Parse_MultipleParams_ExtractsAll()
    {
        var msg = IrcMessage.Parse("USER alice 0 * :Alice Smith");
        Assert.Equal("USER", msg.Command);
        Assert.Equal(4, msg.Parameters.Count);
        Assert.Equal("alice", msg.Parameters[0]);
        Assert.Equal("0", msg.Parameters[1]);
        Assert.Equal("*", msg.Parameters[2]);
        Assert.Equal("Alice Smith", msg.Parameters[3]);
    }

    [Fact]
    public void Parse_PingWithToken_ExtractsToken()
    {
        var msg = IrcMessage.Parse("PING :server.example.com");
        Assert.Equal("PING", msg.Command);
        Assert.Single(msg.Parameters);
        Assert.Equal("server.example.com", msg.Parameters[0]);
    }

    [Fact]
    public void Parse_CapLs_ParsesSubcommand()
    {
        var msg = IrcMessage.Parse("CAP LS 302");
        Assert.Equal("CAP", msg.Command);
        Assert.Equal(2, msg.Parameters.Count);
        Assert.Equal("LS", msg.Parameters[0]);
        Assert.Equal("302", msg.Parameters[1]);
    }

    [Fact]
    public void Parse_CapReqWithTrailing_ParsesSasl()
    {
        var msg = IrcMessage.Parse("CAP REQ :sasl");
        Assert.Equal("CAP", msg.Command);
        Assert.Equal(2, msg.Parameters.Count);
        Assert.Equal("REQ", msg.Parameters[0]);
        Assert.Equal("sasl", msg.Parameters[1]);
    }

    [Fact]
    public void Parse_JoinMultipleChannels_ExtractsCsv()
    {
        var msg = IrcMessage.Parse("JOIN #general,#random");
        Assert.Equal("JOIN", msg.Command);
        Assert.Single(msg.Parameters);
        Assert.Equal("#general,#random", msg.Parameters[0]);
    }

    [Fact]
    public void Parse_PartWithReason_ExtractsReason()
    {
        var msg = IrcMessage.Parse("PART #general :Leaving for now");
        Assert.Equal("PART", msg.Command);
        Assert.Equal(2, msg.Parameters.Count);
        Assert.Equal("#general", msg.Parameters[0]);
        Assert.Equal("Leaving for now", msg.Parameters[1]);
    }

    [Fact]
    public void Parse_EmptyTrailing_ExtractsEmptyString()
    {
        var msg = IrcMessage.Parse("PRIVMSG #general :");
        Assert.Equal("PRIVMSG", msg.Command);
        Assert.Equal(2, msg.Parameters.Count);
        Assert.Equal("#general", msg.Parameters[0]);
        Assert.Equal("", msg.Parameters[1]);
    }

    [Fact]
    public void Parse_TrailingWithColons_PreservesColons()
    {
        var msg = IrcMessage.Parse("PRIVMSG #general :time is 12:30:00");
        Assert.Equal("time is 12:30:00", msg.Parameters[1]);
    }

    [Fact]
    public void Parse_CrLfTrimmed()
    {
        var msg = IrcMessage.Parse("PING\r\n");
        Assert.Equal("PING", msg.Command);
        Assert.Empty(msg.Parameters);
    }

    [Fact]
    public void Parse_ExtraSpaces_Handled()
    {
        var msg = IrcMessage.Parse("NICK   alice");
        Assert.Equal("NICK", msg.Command);
        Assert.Single(msg.Parameters);
        Assert.Equal("alice", msg.Parameters[0]);
    }

    [Fact]
    public void Parse_Authenticate_Base64Payload()
    {
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("\0alice\0secret"));
        var msg = IrcMessage.Parse($"AUTHENTICATE {payload}");
        Assert.Equal("AUTHENTICATE", msg.Command);
        Assert.Single(msg.Parameters);
        Assert.Equal(payload, msg.Parameters[0]);
    }

    [Fact]
    public void Parse_PassCommand_ExtractsPassword()
    {
        var msg = IrcMessage.Parse("PASS mysecretpassword");
        Assert.Equal("PASS", msg.Command);
        Assert.Single(msg.Parameters);
        Assert.Equal("mysecretpassword", msg.Parameters[0]);
    }

    [Fact]
    public void Parse_QuitWithMessage_ExtractsMessage()
    {
        var msg = IrcMessage.Parse("QUIT :Goodbye!");
        Assert.Equal("QUIT", msg.Command);
        Assert.Single(msg.Parameters);
        Assert.Equal("Goodbye!", msg.Parameters[0]);
    }

    [Fact]
    public void Trailing_NoParams_ReturnsNull()
    {
        var msg = IrcMessage.Parse("PING");
        Assert.Null(msg.Trailing);
    }

    [Fact]
    public void Trailing_WithParams_ReturnsLastParam()
    {
        var msg = IrcMessage.Parse("MODE #channel +o alice");
        Assert.Equal("alice", msg.Trailing);
    }

    // ── IRCv3 Message Tags ──────────────────────────────────────────────

    [Fact]
    public void Parse_WithTags_ExtractsTags()
    {
        var msg = IrcMessage.Parse("@time=2024-01-01T12:00:00.000Z;msgid=abc PRIVMSG #channel :Hello");

        Assert.Equal(2, msg.Tags.Count);
        Assert.Equal("2024-01-01T12:00:00.000Z", msg.Tags["time"]);
        Assert.Equal("abc", msg.Tags["msgid"]);
        Assert.Equal("PRIVMSG", msg.Command);
        Assert.Equal("Hello", msg.Parameters[1]);
    }

    [Fact]
    public void Parse_WithClientOnlyTags_ExtractsPlusPrefix()
    {
        var msg = IrcMessage.Parse("@+reply=abc123;+example.com/tag=val PRIVMSG #channel :Hello");

        Assert.Equal(2, msg.Tags.Count);
        Assert.Equal("abc123", msg.Tags["+reply"]);
        Assert.Equal("val", msg.Tags["+example.com/tag"]);
    }

    [Fact]
    public void Parse_WithTagsAndPrefix_ExtractsBoth()
    {
        var msg = IrcMessage.Parse("@time=2024-01-01T12:00:00.000Z :alice!user@host PRIVMSG #channel :Hello");

        Assert.Single(msg.Tags);
        Assert.Equal("alice!user@host", msg.Prefix);
        Assert.Equal("PRIVMSG", msg.Command);
        Assert.Equal("Hello", msg.Parameters[1]);
    }

    [Fact]
    public void Parse_TagWithEscapedValue_Unescapes()
    {
        var msg = IrcMessage.Parse("@key=hello\\sworld\\:! PRIVMSG #channel :Hi");

        Assert.Single(msg.Tags);
        Assert.Equal("hello world;!", msg.Tags["key"]);
    }

    [Fact]
    public void Parse_TagWithNoValue_StoresNull()
    {
        var msg = IrcMessage.Parse("@empty;key=val PRIVMSG #channel :Hi");

        Assert.Equal(2, msg.Tags.Count);
        Assert.Null(msg.Tags["empty"]);
        Assert.Equal("val", msg.Tags["key"]);
    }

    [Fact]
    public void Parse_BatchTag_ParsesMultilineContext()
    {
        var msg = IrcMessage.Parse("@batch=abc123 PRIVMSG #channel :line content");

        Assert.Single(msg.Tags);
        Assert.Equal("abc123", msg.Tags["batch"]);
        Assert.Equal("PRIVMSG", msg.Command);
        Assert.Equal("line content", msg.Parameters[1]);
    }

    // ── Tag Escaping ─────────────────────────────────────────────────────

    [Fact]
    public void TagEscape_HandlesSpecialChars()
    {
        Assert.Equal("hello\\sworld", IrcMessage.TagEscape("hello world"));
        Assert.Equal("a\\:b", IrcMessage.TagEscape("a;b"));
        Assert.Equal("a\\\\b", IrcMessage.TagEscape("a\\b"));
        Assert.Equal("a\\rb\\n", IrcMessage.TagEscape("a\rb\n"));

        // Regular chars pass through unchanged
        Assert.Equal("plain-text_123", IrcMessage.TagEscape("plain-text_123"));
    }

    [Fact]
    public void TagUnescape_HandlesEscapeSequences()
    {
        Assert.Equal("hello world", IrcMessage.TagUnescape("hello\\sworld"));
        Assert.Equal("a;b", IrcMessage.TagUnescape("a\\:b"));
        Assert.Equal("a\\b", IrcMessage.TagUnescape("a\\\\b"));
        Assert.Equal("a\rb\n", IrcMessage.TagUnescape("a\\rb\\n"));
    }

    // ── BuildTagPrefix ───────────────────────────────────────────────────

    [Fact]
    public void BuildTagPrefix_NoTags_ReturnsEmpty()
    {
        Assert.Equal("", IrcMessage.BuildTagPrefix());
    }

    [Fact]
    public void BuildTagPrefix_SingleTag_FormatsCorrectly()
    {
        var result = IrcMessage.BuildTagPrefix(("time", "2024-01-01T12:00:00.000Z"));
        Assert.Equal("@time=2024-01-01T12:00:00.000Z ", result);
    }

    [Fact]
    public void BuildTagPrefix_MultipleTags_SemicolonSeparated()
    {
        var result = IrcMessage.BuildTagPrefix(("time", "2024-01-01T12:00:00.000Z"), ("msgid", "abc123"));
        Assert.Equal("@time=2024-01-01T12:00:00.000Z;msgid=abc123 ", result);
    }

    [Fact]
    public void BuildTagPrefix_TagWithNullValue_OmitsEquals()
    {
        var result = IrcMessage.BuildTagPrefix(("tag-only", null));
        Assert.Equal("@tag-only ", result);
    }

    [Fact]
    public void BuildTagPrefix_ClientOnlyTag_IncludesPlusPrefix()
    {
        var result = IrcMessage.BuildTagPrefix(("+reply", "msg-123"));
        Assert.Equal("@+reply=msg-123 ", result);
    }
}
