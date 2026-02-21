using System.Text;
using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Irc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EchoHub.Tests.Irc;

public class IrcCommandHandlerTests
{
    private readonly IrcOptions _options = new() { ServerName = "testserver", Motd = null };
    private readonly FakeChatService _chatService = new();
    private readonly FakeEncryptionService _encryption = new();

    private IrcCommandHandler CreateHandler(IrcClientConnection conn) =>
        new(conn, _options, _chatService, _encryption, NullLogger.Instance);

    private async Task<List<string>> RunAndCapture(string[] inputLines,
        Action<IrcClientConnection>? setup = null)
    {
        var (conn, stream) = TestIrcConnectionFactory.Create(inputLines);
        setup?.Invoke(conn);

        var handler = CreateHandler(conn);
        await handler.RunAsync(CancellationToken.None);

        return stream.GetOutputLines();
    }

    private async Task<List<string>> RunAuthenticated(string[] inputLines,
        string nickname = "alice", Guid? userId = null,
        Action<IrcClientConnection>? setup = null)
    {
        var (conn, stream) = TestIrcConnectionFactory.CreateAuthenticated(nickname, userId, inputLines);
        setup?.Invoke(conn);

        var handler = CreateHandler(conn);
        await handler.RunAsync(CancellationToken.None);

        return stream.GetOutputLines();
    }

    // ── PING / PONG ──────────────────────────────────────────────────────

    [Fact]
    public async Task Ping_RespondsWithPong()
    {
        var lines = await RunAuthenticated(["PING :mytoken"]);

        Assert.Contains(lines, l => l.Contains("PONG") && l.Contains("mytoken"));
    }

    [Fact]
    public async Task Ping_NoToken_UsesServerName()
    {
        var lines = await RunAuthenticated(["PING"]);

        Assert.Contains(lines, l => l.Contains("PONG") && l.Contains("testserver"));
    }

    // ── Unregistered commands ────────────────────────────────────────────

    [Fact]
    public async Task UnregisteredUser_ChannelCommand_GetsNotRegisteredError()
    {
        var lines = await RunAndCapture(["JOIN #general"]);

        Assert.Contains(lines, l => l.Contains("451") && l.Contains("not registered"));
    }

    [Fact]
    public async Task UnregisteredUser_PrivmsgCommand_GetsNotRegisteredError()
    {
        var lines = await RunAndCapture(["PRIVMSG #general :hello"]);

        Assert.Contains(lines, l => l.Contains("451"));
    }

    // ── PASS / NICK / USER registration ──────────────────────────────────

    [Fact]
    public async Task PassNickUser_ValidCredentials_Registers()
    {
        var userId = Guid.NewGuid();
        _chatService.AuthResult = (userId, "alice");

        var lines = await RunAndCapture([
            "PASS secret123",
            "NICK alice",
            "USER alice 0 * :Alice Smith"
        ]);

        // Should get welcome burst (001)
        Assert.Contains(lines, l => l.Contains("001") && l.Contains("Welcome"));
        Assert.Contains("alice", _chatService.ConnectedUsers);
    }

    [Fact]
    public async Task NickUser_NoPassword_GetsPasswordError()
    {
        var lines = await RunAndCapture([
            "NICK alice",
            "USER alice 0 * :Alice Smith"
        ]);

        Assert.Contains(lines, l => l.Contains("464") && l.Contains("Password required"));
    }

    [Fact]
    public async Task PassNickUser_WrongPassword_GetsAuthError()
    {
        _chatService.AuthResult = null;

        var lines = await RunAndCapture([
            "PASS wrongpassword",
            "NICK alice",
            "USER alice 0 * :Alice Smith"
        ]);

        Assert.Contains(lines, l => l.Contains("464") && l.Contains("incorrect"));
    }

    [Fact]
    public async Task Nick_InvalidNickname_GetsError()
    {
        var lines = await RunAndCapture(["NICK a"]); // too short

        Assert.Contains(lines, l => l.Contains("432") && l.Contains("Erroneous nickname"));
    }

    [Fact]
    public async Task Nick_NoParam_GetsNoNicknameError()
    {
        var lines = await RunAndCapture(["NICK"]);

        Assert.Contains(lines, l => l.Contains("431") && l.Contains("No nickname given"));
    }

    [Fact]
    public async Task User_AlreadyRegistered_GetsError()
    {
        var lines = await RunAuthenticated([
            "USER alice 0 * :Alice"
        ]);

        Assert.Contains(lines, l => l.Contains("462") && l.Contains("reregister"));
    }

    [Fact]
    public async Task Pass_AlreadyRegistered_GetsError()
    {
        var lines = await RunAuthenticated([
            "PASS newpassword"
        ]);

        Assert.Contains(lines, l => l.Contains("462") && l.Contains("reregister"));
    }

    // ── CAP / SASL ──────────────────────────────────────────────────────

    [Fact]
    public async Task CapLs_AdvertisesSasl()
    {
        var lines = await RunAndCapture(["CAP LS"]);

        Assert.Contains(lines, l => l.Contains("CAP") && l.Contains("sasl"));
    }

    [Fact]
    public async Task CapReqSasl_Acknowledged()
    {
        var lines = await RunAndCapture(["CAP REQ :sasl"]);

        Assert.Contains(lines, l => l.Contains("ACK") && l.Contains("sasl"));
    }

    [Fact]
    public async Task CapReqUnknown_GetsNak()
    {
        var lines = await RunAndCapture(["CAP REQ :multi-prefix"]);

        Assert.Contains(lines, l => l.Contains("NAK"));
    }

    [Fact]
    public async Task SaslPlain_ValidCredentials_Authenticates()
    {
        var userId = Guid.NewGuid();
        _chatService.AuthResult = (userId, "alice");

        var saslPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes("\0alice\0password123"));

        var lines = await RunAndCapture([
            "CAP LS",
            "CAP REQ :sasl",
            $"AUTHENTICATE PLAIN",
            $"AUTHENTICATE {saslPayload}",
            "NICK alice",
            "USER alice 0 * :Alice",
            "CAP END"
        ]);

        Assert.Contains(lines, l => l.Contains("903") && l.Contains("SASL authentication successful"));
        Assert.Contains(lines, l => l.Contains("001") && l.Contains("Welcome"));
    }

    [Fact]
    public async Task SaslPlain_InvalidCredentials_GetsError()
    {
        _chatService.AuthResult = null;

        var saslPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes("\0alice\0wrongpwd"));

        var lines = await RunAndCapture([
            "CAP LS",
            "CAP REQ :sasl",
            "AUTHENTICATE PLAIN",
            $"AUTHENTICATE {saslPayload}",
        ]);

        Assert.Contains(lines, l => l.Contains("904") && l.Contains("SASL authentication failed"));
    }

    [Fact]
    public async Task SaslPlain_MalformedPayload_GetsError()
    {
        var saslPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes("malformed"));

        var lines = await RunAndCapture([
            "CAP REQ :sasl",
            "AUTHENTICATE PLAIN",
            $"AUTHENTICATE {saslPayload}",
        ]);

        Assert.Contains(lines, l => l.Contains("904"));
    }

    // ── JOIN ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Join_ValidChannel_ConfirmsJoin()
    {
        _chatService.TopicResult = ("Welcome!", true);

        var lines = await RunAuthenticated(["JOIN #general"]);

        Assert.Contains(lines, l => l.Contains("JOIN #general"));
        Assert.Single(_chatService.JoinedChannels);
        Assert.Equal("general", _chatService.JoinedChannels[0].Channel);
    }

    [Fact]
    public async Task Join_SendsTopic()
    {
        _chatService.TopicResult = ("Welcome to general!", true);

        var lines = await RunAuthenticated(["JOIN #general"]);

        Assert.Contains(lines, l => l.Contains("332") && l.Contains("Welcome to general!"));
    }

    [Fact]
    public async Task Join_NoTopic_SendsNoTopicReply()
    {
        _chatService.TopicResult = (null, true);

        var lines = await RunAuthenticated(["JOIN #general"]);

        Assert.Contains(lines, l => l.Contains("331") && l.Contains("No topic is set"));
    }

    [Fact]
    public async Task Join_SendsNamesReply()
    {
        _chatService.OnlineUsersToReturn =
        [
            new("alice", null, null, UserStatus.Online, null, ServerRole.Member),
            new("bob", null, null, UserStatus.Online, null, ServerRole.Member),
        ];

        var lines = await RunAuthenticated(["JOIN #general"]);

        Assert.Contains(lines, l => l.Contains("353") && l.Contains("alice") && l.Contains("bob"));
        Assert.Contains(lines, l => l.Contains("366") && l.Contains("End of /NAMES"));
    }

    [Fact]
    public async Task Join_DecryptsHistoryForIrc()
    {
        // Simulate encrypted history (as ChatService returns it)
        var encryptedContent = _encryption.Encrypt("Hello from history!");
        _chatService.HistoryToReturn =
        [
            new(Guid.NewGuid(), encryptedContent, "bob", null, "general",
                MessageType.Text, null, null, DateTimeOffset.UtcNow)
        ];

        var lines = await RunAuthenticated(["JOIN #general"]);

        // Should contain the DECRYPTED text, not the encrypted version
        Assert.Contains(lines, l => l.Contains("Hello from history!"));
        Assert.DoesNotContain(lines, l => l.Contains("$ENC$Hello from history!"));
    }

    [Fact]
    public async Task Join_NonexistentChannel_GetsError()
    {
        _chatService.JoinError = "Channel 'nope' does not exist.";

        var lines = await RunAuthenticated(["JOIN #nope"]);

        Assert.Contains(lines, l => l.Contains("403") && l.Contains("does not exist"));
    }

    [Fact]
    public async Task Join_InvalidChannelName_GetsError()
    {
        var lines = await RunAuthenticated(["JOIN invalid"]);

        Assert.Contains(lines, l => l.Contains("403") && l.Contains("Invalid channel name"));
    }

    [Fact]
    public async Task Join_MultipleChannels_JoinsAll()
    {
        var lines = await RunAuthenticated(["JOIN #general,#random"]);

        Assert.Equal(2, _chatService.JoinedChannels.Count);
        Assert.Contains(_chatService.JoinedChannels, j => j.Channel == "general");
        Assert.Contains(_chatService.JoinedChannels, j => j.Channel == "random");
    }

    [Fact]
    public async Task Join_NoParams_GetsNeedMoreParamsError()
    {
        var lines = await RunAuthenticated(["JOIN"]);

        Assert.Contains(lines, l => l.Contains("461") && l.Contains("Not enough parameters"));
    }

    // ── PART ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Part_ValidChannel_ConfirmsPart()
    {
        var lines = await RunAuthenticated(["PART #general"]);

        Assert.Contains(lines, l => l.Contains("PART #general"));
        Assert.Single(_chatService.LeftChannels);
        Assert.Equal("general", _chatService.LeftChannels[0].Channel);
    }

    [Fact]
    public async Task Part_WithReason_IncludesReason()
    {
        var lines = await RunAuthenticated(["PART #general :Leaving for now"]);

        Assert.Contains(lines, l => l.Contains("PART #general") && l.Contains("Leaving for now"));
    }

    // ── PRIVMSG ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Privmsg_ChannelMessage_SendsViaService()
    {
        var lines = await RunAuthenticated(["PRIVMSG #general :Hello everyone!"]);

        Assert.Single(_chatService.SentMessages);
        Assert.Equal("general", _chatService.SentMessages[0].Channel);
        Assert.Equal("Hello everyone!", _chatService.SentMessages[0].Content);
    }

    [Fact]
    public async Task Privmsg_PrivateMessage_GetsError()
    {
        var lines = await RunAuthenticated(["PRIVMSG bob :Hey bob"]);

        Assert.Contains(lines, l => l.Contains("401") && l.Contains("Private messages are not supported"));
        Assert.Empty(_chatService.SentMessages);
    }

    [Fact]
    public async Task Privmsg_ServiceError_ReturnsError()
    {
        _chatService.SendMessageError = "You are muted.";

        var lines = await RunAuthenticated(["PRIVMSG #general :Hello"]);

        Assert.Contains(lines, l => l.Contains("404") && l.Contains("muted"));
    }

    [Fact]
    public async Task Privmsg_NoParams_GetsNeedMoreParamsError()
    {
        var lines = await RunAuthenticated(["PRIVMSG"]);

        Assert.Contains(lines, l => l.Contains("461") && l.Contains("Not enough parameters"));
    }

    // ── QUIT ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Quit_WithMessage_SendsClosingLink()
    {
        var lines = await RunAuthenticated(["QUIT :Goodbye!"]);

        Assert.Contains(lines, l => l.Contains("ERROR") && l.Contains("Goodbye!"));
    }

    [Fact]
    public async Task Quit_NoMessage_UsesDefault()
    {
        var lines = await RunAuthenticated(["QUIT"]);

        Assert.Contains(lines, l => l.Contains("ERROR") && l.Contains("Client quit"));
    }

    // ── NAMES ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Names_ReturnsUserList()
    {
        _chatService.OnlineUsersToReturn =
        [
            new("alice", null, null, UserStatus.Online, null, ServerRole.Member),
            new("bob", "Bob", null, UserStatus.Away, null, ServerRole.Mod),
        ];

        var lines = await RunAuthenticated(["NAMES #general"]);

        Assert.Contains(lines, l => l.Contains("353") && l.Contains("alice") && l.Contains("bob"));
        Assert.Contains(lines, l => l.Contains("366"));
    }

    // ── TOPIC ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Topic_Query_ReturnsTopic()
    {
        _chatService.TopicResult = ("Chat about everything", true);

        var lines = await RunAuthenticated(["TOPIC #general"]);

        Assert.Contains(lines, l => l.Contains("332") && l.Contains("Chat about everything"));
    }

    [Fact]
    public async Task Topic_SetAttempt_GetsPermissionDenied()
    {
        var lines = await RunAuthenticated(["TOPIC #general :New topic"]);

        Assert.Contains(lines, l => l.Contains("482") && l.Contains("channel creator"));
    }

    // ── WHO ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Who_ReturnsUserListWithAwayFlags()
    {
        _chatService.OnlineUsersToReturn =
        [
            new("alice", "Alice", null, UserStatus.Online, null, ServerRole.Member),
            new("bob", "Bob", null, UserStatus.Away, "brb", ServerRole.Member),
        ];

        var lines = await RunAuthenticated(["WHO #general"]);

        Assert.Contains(lines, l => l.Contains("352") && l.Contains("alice") && l.Contains("H")); // Here
        Assert.Contains(lines, l => l.Contains("352") && l.Contains("bob") && l.Contains("G")); // Gone
        Assert.Contains(lines, l => l.Contains("315") && l.Contains("End of WHO"));
    }

    // ── WHOIS ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Whois_ExistingUser_ReturnsInfo()
    {
        _chatService.ProfileToReturn = new UserProfileDto(
            Guid.NewGuid(), "bob", "Bob S.", "Hello!", null, null,
            UserStatus.Online, null, ServerRole.Member,
            DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow);
        _chatService.ChannelsForUserToReturn = ["general", "random"];

        var lines = await RunAuthenticated(["WHOIS bob"]);

        Assert.Contains(lines, l => l.Contains("311") && l.Contains("bob") && l.Contains("Bob S."));
        Assert.Contains(lines, l => l.Contains("312") && l.Contains("testserver"));
        Assert.Contains(lines, l => l.Contains("319") && l.Contains("#general") && l.Contains("#random"));
        Assert.Contains(lines, l => l.Contains("317")); // idle
        Assert.Contains(lines, l => l.Contains("318") && l.Contains("End of WHOIS"));
    }

    [Fact]
    public async Task Whois_NonexistentUser_GetsNoSuchNickError()
    {
        _chatService.ProfileToReturn = null;

        var lines = await RunAuthenticated(["WHOIS ghost"]);

        Assert.Contains(lines, l => l.Contains("401") && l.Contains("No such nick"));
    }

    [Fact]
    public async Task Whois_AwayUser_ShowsAwayMessage()
    {
        _chatService.ProfileToReturn = new UserProfileDto(
            Guid.NewGuid(), "bob", null, null, null, null,
            UserStatus.Away, "Gone fishing", ServerRole.Member,
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);
        _chatService.ChannelsForUserToReturn = [];

        var lines = await RunAuthenticated(["WHOIS bob"]);

        Assert.Contains(lines, l => l.Contains("301") && l.Contains("Gone fishing"));
    }

    // ── AWAY ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Away_WithMessage_SetsAway()
    {
        var lines = await RunAuthenticated(["AWAY :Be right back"]);

        Assert.Contains(lines, l => l.Contains("306") && l.Contains("marked as being away"));
        Assert.Single(_chatService.StatusUpdates);
        Assert.Equal(UserStatus.Away, _chatService.StatusUpdates[0].Status);
    }

    [Fact]
    public async Task Away_NoMessage_ClearsAway()
    {
        var lines = await RunAuthenticated(["AWAY"]);

        Assert.Contains(lines, l => l.Contains("305") && l.Contains("no longer marked"));
        Assert.Single(_chatService.StatusUpdates);
        Assert.Equal(UserStatus.Online, _chatService.StatusUpdates[0].Status);
    }

    // ── LIST ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_ReturnsChannels()
    {
        _chatService.ChannelListToReturn =
        [
            new("general", "General chat", 5),
            new("random", null, 2),
        ];

        var lines = await RunAuthenticated(["LIST"]);

        Assert.Contains(lines, l => l.Contains("322") && l.Contains("#general") && l.Contains("General chat"));
        Assert.Contains(lines, l => l.Contains("322") && l.Contains("#random"));
        Assert.Contains(lines, l => l.Contains("323") && l.Contains("End of LIST"));
    }

    // ── MODE ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Mode_Channel_ReturnsChannelModes()
    {
        var lines = await RunAuthenticated(["MODE #general"]);

        Assert.Contains(lines, l => l.Contains("324") && l.Contains("#general"));
    }

    [Fact]
    public async Task Mode_User_ReturnsUserModes()
    {
        var lines = await RunAuthenticated(["MODE alice"]);

        Assert.Contains(lines, l => l.Contains("221"));
    }

    // ── MOTD ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Motd_NoMotdConfigured_GetsNoMotdError()
    {
        var lines = await RunAuthenticated(["MOTD"]);

        Assert.Contains(lines, l => l.Contains("422") && l.Contains("MOTD File is missing"));
    }

    [Fact]
    public async Task Motd_WithMotd_DisplaysMotd()
    {
        _options.Motd = "Welcome to EchoHub!\nEnjoy your stay.";

        var lines = await RunAuthenticated(["MOTD"]);

        Assert.Contains(lines, l => l.Contains("375")); // MOTDSTART
        Assert.Contains(lines, l => l.Contains("372") && l.Contains("Welcome to EchoHub!"));
        Assert.Contains(lines, l => l.Contains("372") && l.Contains("Enjoy your stay."));
        Assert.Contains(lines, l => l.Contains("376")); // ENDOFMOTD
    }

    // ── Unknown command ──────────────────────────────────────────────────

    [Fact]
    public async Task UnknownCommand_GetsError()
    {
        var lines = await RunAuthenticated(["FOOBAR"]);

        Assert.Contains(lines, l => l.Contains("421") && l.Contains("FOOBAR") && l.Contains("Unknown command"));
    }

    // ── Channel name conversion ──────────────────────────────────────────

    [Fact]
    public async Task Join_ChannelNameNormalized_ToLowerCase()
    {
        var lines = await RunAuthenticated(["JOIN #General"]);

        Assert.Single(_chatService.JoinedChannels);
        Assert.Equal("general", _chatService.JoinedChannels[0].Channel);
    }
}
