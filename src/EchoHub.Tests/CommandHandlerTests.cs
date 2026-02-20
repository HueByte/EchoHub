using EchoHub.Client.Commands;
using EchoHub.Core.Models;
using Xunit;

namespace EchoHub.Tests;

public class CommandHandlerTests
{
    private CommandHandler CreateHandler() => new();

    // ── IsCommand ─────────────────────────────────────────────────────

    [Fact]
    public void IsCommand_StartsWithSlash_ReturnsTrue()
    {
        var handler = CreateHandler();
        Assert.True(handler.IsCommand("/help"));
    }

    [Fact]
    public void IsCommand_NoSlash_ReturnsFalse()
    {
        var handler = CreateHandler();
        Assert.False(handler.IsCommand("hello"));
    }

    [Fact]
    public void IsCommand_EmptyString_ReturnsFalse()
    {
        var handler = CreateHandler();
        Assert.False(handler.IsCommand(""));
    }

    // ── HandleAsync — not a command ───────────────────────────────────

    [Fact]
    public async Task HandleAsync_NotCommand_ReturnsFalse()
    {
        var handler = CreateHandler();
        var result = await handler.HandleAsync("hello world");

        Assert.False(result.Handled);
    }

    // ── HandleAsync — unknown command ─────────────────────────────────

    [Fact]
    public async Task HandleAsync_UnknownCommand_ReturnsError()
    {
        var handler = CreateHandler();
        var result = await handler.HandleAsync("/doesnotexist");

        Assert.True(result.Handled);
        Assert.True(result.IsError);
        Assert.Contains("Unknown command", result.Message);
    }

    // ── /status ───────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_StatusOnline_SetsOnlineStatus()
    {
        var handler = CreateHandler();
        UserStatus? capturedStatus = null;
        handler.OnSetStatus += (status, msg) => { capturedStatus = status; return Task.CompletedTask; };

        var result = await handler.HandleAsync("/status online");

        Assert.True(result.Handled);
        Assert.False(result.IsError);
        Assert.Equal(UserStatus.Online, capturedStatus);
    }

    [Fact]
    public async Task HandleAsync_StatusAway_SetsAwayStatus()
    {
        var handler = CreateHandler();
        UserStatus? capturedStatus = null;
        handler.OnSetStatus += (status, msg) => { capturedStatus = status; return Task.CompletedTask; };

        await handler.HandleAsync("/status away");
        Assert.Equal(UserStatus.Away, capturedStatus);
    }

    [Fact]
    public async Task HandleAsync_StatusCustomMessage_SetsStatusMessage()
    {
        var handler = CreateHandler();
        string? capturedMessage = null;
        handler.OnSetStatus += (status, msg) => { capturedMessage = msg; return Task.CompletedTask; };

        var result = await handler.HandleAsync("/status brb lunch");

        Assert.True(result.Handled);
        Assert.Contains("brb lunch", result.Message);
        Assert.Equal("brb lunch", capturedMessage);
    }

    [Fact]
    public async Task HandleAsync_StatusNoArgs_ReturnsError()
    {
        var handler = CreateHandler();
        var result = await handler.HandleAsync("/status");

        Assert.True(result.IsError);
        Assert.Contains("Usage", result.Message);
    }

    // ── /nick ─────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Nick_SetsDisplayName()
    {
        var handler = CreateHandler();
        string? capturedNick = null;
        handler.OnSetNick += nick => { capturedNick = nick; return Task.CompletedTask; };

        var result = await handler.HandleAsync("/nick Bob Smith");

        Assert.True(result.Handled);
        Assert.Equal("Bob Smith", capturedNick);
    }

    [Fact]
    public async Task HandleAsync_Nick_EmptyArgs_ReturnsError()
    {
        var handler = CreateHandler();
        var result = await handler.HandleAsync("/nick");

        Assert.True(result.IsError);
        Assert.Contains("Usage", result.Message);
    }

    // ── /color ────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Color_ValidHex_Succeeds()
    {
        var handler = CreateHandler();
        string? capturedColor = null;
        handler.OnSetColor += color => { capturedColor = color; return Task.CompletedTask; };

        var result = await handler.HandleAsync("/color #FF5733");

        Assert.True(result.Handled);
        Assert.False(result.IsError);
        Assert.Equal("#FF5733", capturedColor);
    }

    [Fact]
    public async Task HandleAsync_Color_WithoutHash_AddsHash()
    {
        var handler = CreateHandler();
        string? capturedColor = null;
        handler.OnSetColor += color => { capturedColor = color; return Task.CompletedTask; };

        await handler.HandleAsync("/color FF5733");
        Assert.Equal("#FF5733", capturedColor);
    }

    [Fact]
    public async Task HandleAsync_Color_InvalidHex_ReturnsError()
    {
        var handler = CreateHandler();
        var result = await handler.HandleAsync("/color #ZZZZZZ");

        Assert.True(result.IsError);
        Assert.Contains("Invalid color", result.Message);
    }

    [Fact]
    public async Task HandleAsync_Color_TooShort_ReturnsError()
    {
        var handler = CreateHandler();
        var result = await handler.HandleAsync("/color #FFF");

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task HandleAsync_Color_NoArgs_ReturnsError()
    {
        var handler = CreateHandler();
        var result = await handler.HandleAsync("/color");

        Assert.True(result.IsError);
    }

    // ── /join ─────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Join_StripsHashPrefix()
    {
        var handler = CreateHandler();
        string? capturedChannel = null;
        handler.OnJoinChannel += ch => { capturedChannel = ch; return Task.CompletedTask; };

        await handler.HandleAsync("/join #random");
        Assert.Equal("random", capturedChannel);
    }

    [Fact]
    public async Task HandleAsync_Join_NoHash_PassedDirectly()
    {
        var handler = CreateHandler();
        string? capturedChannel = null;
        handler.OnJoinChannel += ch => { capturedChannel = ch; return Task.CompletedTask; };

        await handler.HandleAsync("/join random");
        Assert.Equal("random", capturedChannel);
    }

    [Fact]
    public async Task HandleAsync_Join_NoArgs_ReturnsError()
    {
        var handler = CreateHandler();
        var result = await handler.HandleAsync("/join");

        Assert.True(result.IsError);
    }

    // ── /kick ─────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Kick_WithReason_ParsesUsernameAndReason()
    {
        var handler = CreateHandler();
        string? capturedUser = null;
        string? capturedReason = null;
        handler.OnKickUser += (user, reason) =>
        {
            capturedUser = user;
            capturedReason = reason;
            return Task.CompletedTask;
        };

        await handler.HandleAsync("/kick baduser being rude");
        Assert.Equal("baduser", capturedUser);
        Assert.Equal("being rude", capturedReason);
    }

    [Fact]
    public async Task HandleAsync_Kick_WithoutReason_NullReason()
    {
        var handler = CreateHandler();
        string? capturedReason = "initial";
        handler.OnKickUser += (user, reason) =>
        {
            capturedReason = reason;
            return Task.CompletedTask;
        };

        await handler.HandleAsync("/kick baduser");
        Assert.Null(capturedReason);
    }

    [Fact]
    public async Task HandleAsync_Kick_NoArgs_ReturnsError()
    {
        var handler = CreateHandler();
        var result = await handler.HandleAsync("/kick");
        Assert.True(result.IsError);
    }

    // ── /mute ─────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Mute_WithDuration_ParsesDuration()
    {
        var handler = CreateHandler();
        int? capturedDuration = null;
        handler.OnMuteUser += (user, duration) =>
        {
            capturedDuration = duration;
            return Task.CompletedTask;
        };

        await handler.HandleAsync("/mute alice 30");
        Assert.Equal(30, capturedDuration);
    }

    [Fact]
    public async Task HandleAsync_Mute_WithoutDuration_NullDuration()
    {
        var handler = CreateHandler();
        int? capturedDuration = -1;
        handler.OnMuteUser += (user, duration) =>
        {
            capturedDuration = duration;
            return Task.CompletedTask;
        };

        await handler.HandleAsync("/mute alice");
        Assert.Null(capturedDuration);
    }

    // ── /role ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("admin")]
    [InlineData("mod")]
    [InlineData("member")]
    public async Task HandleAsync_Role_ValidRole_Succeeds(string role)
    {
        var handler = CreateHandler();
        string? capturedRole = null;
        handler.OnAssignRole += (user, r) => { capturedRole = r; return Task.CompletedTask; };

        var result = await handler.HandleAsync($"/role alice {role}");

        Assert.True(result.Handled);
        Assert.False(result.IsError);
        Assert.Equal(role, capturedRole);
    }

    [Fact]
    public async Task HandleAsync_Role_InvalidRole_ReturnsError()
    {
        var handler = CreateHandler();
        var result = await handler.HandleAsync("/role alice superadmin");

        Assert.True(result.IsError);
        Assert.Contains("Invalid role", result.Message);
    }

    [Fact]
    public async Task HandleAsync_Role_MissingRole_ReturnsError()
    {
        var handler = CreateHandler();
        var result = await handler.HandleAsync("/role alice");

        Assert.True(result.IsError);
    }

    // ── /send ─────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Send_NoArgs_ReturnsUsageError()
    {
        var handler = CreateHandler();
        var result = await handler.HandleAsync("/send");

        Assert.True(result.IsError);
        Assert.Contains("Usage", result.Message);
    }

    [Fact]
    public async Task HandleAsync_Send_UrlInput_RecognizedAsUrl()
    {
        var handler = CreateHandler();
        string? capturedTarget = null;
        handler.OnSendFile += (target, size) => { capturedTarget = target; return Task.CompletedTask; };

        var result = await handler.HandleAsync("/send https://example.com/image.png");

        Assert.True(result.Handled);
        Assert.False(result.IsError);
        Assert.Equal("https://example.com/image.png", capturedTarget);
    }

    [Fact]
    public async Task HandleAsync_Send_UrlWithSizeFlag_ExtractsSizeCorrectly()
    {
        var handler = CreateHandler();
        string? capturedSize = null;
        handler.OnSendFile += (target, size) => { capturedSize = size; return Task.CompletedTask; };

        await handler.HandleAsync("/send https://example.com/photo.jpg -s");
        Assert.Equal("s", capturedSize);
    }

    // ── /help ─────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Help_ReturnsHelpText()
    {
        var handler = CreateHandler();
        var result = await handler.HandleAsync("/help");

        Assert.True(result.Handled);
        Assert.Contains("Available commands", result.Message);
    }

    [Fact]
    public async Task HandleAsync_QuestionMark_ReturnsHelp()
    {
        var handler = CreateHandler();
        var result = await handler.HandleAsync("/?");

        Assert.True(result.Handled);
        Assert.Contains("Available commands", result.Message);
    }

    // ── /ban ──────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Ban_WithReason_ParsesUsernameAndReason()
    {
        var handler = CreateHandler();
        string? capturedUser = null;
        string? capturedReason = null;
        handler.OnBanUser += (user, reason) =>
        {
            capturedUser = user;
            capturedReason = reason;
            return Task.CompletedTask;
        };

        await handler.HandleAsync("/ban troll spamming links");
        Assert.Equal("troll", capturedUser);
        Assert.Equal("spamming links", capturedReason);
    }

    // ── /topic ────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Topic_SetsTopic()
    {
        var handler = CreateHandler();
        string? capturedTopic = null;
        handler.OnSetTopic += topic => { capturedTopic = topic; return Task.CompletedTask; };

        var result = await handler.HandleAsync("/topic Welcome to our channel!");

        Assert.True(result.Handled);
        Assert.Equal("Welcome to our channel!", capturedTopic);
    }

    [Fact]
    public async Task HandleAsync_Topic_NoArgs_ReturnsError()
    {
        var handler = CreateHandler();
        var result = await handler.HandleAsync("/topic");
        Assert.True(result.IsError);
    }

    // ── /quit and /exit ───────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Quit_Handled()
    {
        var handler = CreateHandler();
        var quitCalled = false;
        handler.OnQuit += () => { quitCalled = true; return Task.CompletedTask; };

        var result = await handler.HandleAsync("/quit");
        Assert.True(result.Handled);
        Assert.True(quitCalled);
    }

    [Fact]
    public async Task HandleAsync_Exit_Handled()
    {
        var handler = CreateHandler();
        var quitCalled = false;
        handler.OnQuit += () => { quitCalled = true; return Task.CompletedTask; };

        var result = await handler.HandleAsync("/exit");
        Assert.True(result.Handled);
        Assert.True(quitCalled);
    }
}
