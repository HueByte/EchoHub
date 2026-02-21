using System.Text;
using EchoHub.Core.Constants;
using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using Microsoft.Extensions.Logging;

namespace EchoHub.Server.Irc;

public sealed class IrcCommandHandler
{
    private readonly IrcClientConnection _conn;
    private readonly IrcOptions _options;
    private readonly IChatService _chatService;
    private readonly IMessageEncryptionService _encryption;
    private readonly ILogger _logger;

    private string ServerName => _options.ServerName;

    public IrcCommandHandler(
        IrcClientConnection conn,
        IrcOptions options,
        IChatService chatService,
        IMessageEncryptionService encryption,
        ILogger logger)
    {
        _conn = conn;
        _options = options;
        _chatService = chatService;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var line = await _conn.ReadLineAsync(ct);
            if (line is null) break;

            line = line.TrimEnd('\r', '\n');
            if (string.IsNullOrWhiteSpace(line)) continue;

            var msg = IrcMessage.Parse(line);

            try
            {
                await HandleCommandAsync(msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling IRC command {Command} for {Nick}",
                    msg.Command, _conn.Nickname ?? "unregistered");
            }
        }
    }

    private Task HandleCommandAsync(IrcMessage msg)
    {
        var command = msg.Command.ToUpperInvariant();

        return command switch
        {
            // Pre-registration
            "CAP" => HandleCapAsync(msg),
            "AUTHENTICATE" => HandleAuthenticateAsync(msg),
            "PASS" => HandlePassAsync(msg),
            "NICK" => HandleNickAsync(msg),
            "USER" => HandleUserAsync(msg),

            // Post-registration
            "PING" => HandlePingAsync(msg),
            "PONG" => Task.CompletedTask,
            "JOIN" => HandleJoinAsync(msg),
            "PART" => HandlePartAsync(msg),
            "PRIVMSG" => HandlePrivmsgAsync(msg),
            "QUIT" => HandleQuitAsync(msg),
            "NAMES" => HandleNamesAsync(msg),
            "TOPIC" => HandleTopicAsync(msg),
            "WHO" => HandleWhoAsync(msg),
            "WHOIS" => HandleWhoisAsync(msg),
            "AWAY" => HandleAwayAsync(msg),
            "LIST" => HandleListAsync(msg),
            "MODE" => HandleModeAsync(msg),
            "MOTD" => SendMotdAsync(),
            "USERHOST" or "LUSERS" => Task.CompletedTask,

            _ => _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_UNKNOWNCOMMAND,
                $"{command} :Unknown command"),
        };
    }

    // ── Authentication ──────────────────────────────────────────────────────

    private async Task HandleCapAsync(IrcMessage msg)
    {
        if (msg.Parameters.Count < 1) return;

        switch (msg.Parameters[0].ToUpperInvariant())
        {
            case "LS":
                await _conn.SendAsync($":{ServerName} CAP * LS :sasl");
                _conn.CapNegotiating = true;
                break;

            case "REQ":
                if (msg.Parameters.Count >= 2 &&
                    msg.Parameters[1].Trim().Equals("sasl", StringComparison.OrdinalIgnoreCase))
                {
                    await _conn.SendAsync($":{ServerName} CAP * ACK :sasl");
                    _conn.IsSasl = true;
                }
                else
                {
                    var requested = msg.Parameters.ElementAtOrDefault(1) ?? "";
                    await _conn.SendAsync($":{ServerName} CAP * NAK :{requested}");
                }
                break;

            case "END":
                _conn.CapNegotiating = false;
                if (_conn.Nickname is not null && _conn.Username is not null && !_conn.IsRegistered)
                    await TryCompleteRegistrationAsync();
                break;
        }
    }

    private async Task HandleAuthenticateAsync(IrcMessage msg)
    {
        if (msg.Parameters.Count < 1) return;

        if (msg.Parameters[0].Equals("PLAIN", StringComparison.OrdinalIgnoreCase))
        {
            await _conn.SendAsync("AUTHENTICATE +");
            return;
        }

        try
        {
            var decoded = Convert.FromBase64String(msg.Parameters[0]);
            var text = Encoding.UTF8.GetString(decoded);
            var parts = text.Split('\0');

            if (parts.Length < 3)
            {
                await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_SASLFAIL,
                    ":SASL authentication failed (malformed payload)");
                return;
            }

            var username = (parts[1].Length > 0 ? parts[1] : parts[0]).ToLowerInvariant();
            var password = parts[2];

            var result = await _chatService.AuthenticateUserAsync(username, password);

            if (result is null)
            {
                await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_SASLFAIL,
                    ":SASL authentication failed");
                return;
            }

            _conn.Nickname = result.Value.Username;
            _conn.UserId = result.Value.UserId;
            _conn.IsAuthenticated = true;

            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_LOGGEDIN,
                $"{_conn.Hostmask} {username} :You are now logged in as {username}");
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_SASLSUCCESS,
                ":SASL authentication successful");
        }
        catch
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_SASLFAIL,
                ":SASL authentication failed");
        }
    }

    private Task HandlePassAsync(IrcMessage msg)
    {
        if (_conn.IsRegistered)
            return _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_ALREADYREGISTERED,
                ":You may not reregister");

        if (msg.Parameters.Count >= 1)
            _conn.Password = msg.Parameters[0];

        return Task.CompletedTask;
    }

    private async Task HandleNickAsync(IrcMessage msg)
    {
        if (msg.Parameters.Count < 1)
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_NONICKNAMEGIVEN,
                ":No nickname given");
            return;
        }

        var nick = msg.Parameters[0];

        if (!ValidationConstants.UsernameRegex().IsMatch(nick))
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_ERRONEUSNICKNAME,
                $"{nick} :Erroneous nickname (must be 3-50 chars: a-z, 0-9, _, -)");
            return;
        }

        _conn.Nickname = nick.ToLowerInvariant();

        if (!_conn.IsRegistered && _conn.Username is not null)
            await TryCompleteRegistrationAsync();
    }

    private async Task HandleUserAsync(IrcMessage msg)
    {
        if (_conn.IsRegistered)
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_ALREADYREGISTERED,
                ":You may not reregister");
            return;
        }

        if (msg.Parameters.Count < 4)
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_NEEDMOREPARAMS,
                "USER :Not enough parameters");
            return;
        }

        _conn.Username = msg.Parameters[0];
        _conn.RealName = msg.Parameters[3];

        if (_conn.Nickname is not null)
            await TryCompleteRegistrationAsync();
    }

    private async Task TryCompleteRegistrationAsync()
    {
        if (_conn.CapNegotiating || _conn.IsRegistered) return;

        // SASL already authenticated
        if (_conn.IsAuthenticated && _conn.UserId is not null)
        {
            _conn.IsRegistered = true;
            await _chatService.UserConnectedAsync(_conn.ConnectionId, _conn.UserId.Value, _conn.Nickname!);
            await SendWelcomeBurstAsync();
            return;
        }

        // PASS-based authentication
        if (string.IsNullOrEmpty(_conn.Password))
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_PASSWDMISMATCH,
                ":Password required. Use PASS command or SASL PLAIN.");
            await _conn.SendAsync("ERROR :Authentication failed - no password provided");
            return;
        }

        var result = await _chatService.AuthenticateUserAsync(_conn.Nickname!, _conn.Password);

        if (result is null)
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_PASSWDMISMATCH,
                ":Password incorrect or account not found. Register via the EchoHub client first.");
            await _conn.SendAsync("ERROR :Authentication failed");
            return;
        }

        _conn.UserId = result.Value.UserId;
        _conn.Nickname = result.Value.Username;
        _conn.IsAuthenticated = true;
        _conn.IsRegistered = true;

        await _chatService.UserConnectedAsync(_conn.ConnectionId, result.Value.UserId, result.Value.Username);
        await SendWelcomeBurstAsync();
    }

    // ── Welcome / MOTD ──────────────────────────────────────────────────────

    private async Task SendWelcomeBurstAsync()
    {
        var nick = _conn.Nickname!;

        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_WELCOME,
            $":Welcome to the EchoHub IRC Gateway, {nick}!");
        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_YOURHOST,
            $":Your host is {ServerName}, running EchoHub IRC Gateway");
        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_CREATED,
            $":This server was created {DateTimeOffset.UtcNow:yyyy-MM-dd}");
        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_MYINFO,
            $"{ServerName} EchoHub-IRC o o");
        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_ISUPPORT,
            "CHANTYPES=# NICKLEN=50 CHANNELLEN=100 :are supported by this server");

        await SendMotdAsync();
    }

    private async Task SendMotdAsync()
    {
        if (string.IsNullOrWhiteSpace(_options.Motd))
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_NOMOTD,
                ":MOTD File is missing");
            return;
        }

        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_MOTDSTART,
            $":- {ServerName} Message of the day - ");

        foreach (var line in _options.Motd.Split('\n'))
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_MOTD,
                $":- {line.TrimEnd('\r')}");
        }

        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_ENDOFMOTD,
            ":End of MOTD command");
    }

    // ── Channel Operations ──────────────────────────────────────────────────

    private async Task HandleJoinAsync(IrcMessage msg)
    {
        if (!await RequireRegisteredAsync()) return;

        if (msg.Parameters.Count < 1)
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_NEEDMOREPARAMS,
                "JOIN :Not enough parameters");
            return;
        }

        var channels = msg.Parameters[0].Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawChannel in channels)
        {
            var channelName = IrcToEchoHubChannel(rawChannel);
            if (channelName is null)
            {
                await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_NOSUCHCHANNEL,
                    $"{rawChannel} :Invalid channel name");
                continue;
            }

            var (history, error) = await _chatService.JoinChannelAsync(
                _conn.ConnectionId, _conn.UserId!.Value, _conn.Nickname!, channelName);

            if (error is not null)
            {
                await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_NOSUCHCHANNEL,
                    $"#{channelName} :{error}");
                continue;
            }

            _conn.JoinChannel(channelName);

            // Confirm JOIN to the client
            await _conn.SendAsync($":{_conn.Hostmask} JOIN #{channelName}");

            // Send topic
            await SendChannelTopicAsync(channelName);

            // Send NAMES list
            await SendNamesReplyAsync(channelName);

            // Replay history (decrypt — history is encrypted for SignalR transport)
            foreach (var m in history)
            {
                var decrypted = m with { Content = _encryption.Decrypt(m.Content) };
                var lines = IrcMessageFormatter.FormatMessage(decrypted);
                foreach (var line in lines)
                    await _conn.SendAsync(line);
            }
        }
    }

    private async Task HandlePartAsync(IrcMessage msg)
    {
        if (!await RequireRegisteredAsync()) return;
        if (msg.Parameters.Count < 1) return;

        var channels = msg.Parameters[0].Split(',', StringSplitOptions.RemoveEmptyEntries);
        var partMessage = msg.Parameters.Count > 1 ? msg.Parameters[1] : null;

        foreach (var rawChannel in channels)
        {
            var channelName = IrcToEchoHubChannel(rawChannel);
            if (channelName is null) continue;

            await _chatService.LeaveChannelAsync(_conn.ConnectionId, _conn.Nickname!, channelName);
            _conn.LeaveChannel(channelName);

            await _conn.SendAsync($":{_conn.Hostmask} PART #{channelName}" +
                (partMessage is not null ? $" :{partMessage}" : ""));
        }
    }

    private async Task HandlePrivmsgAsync(IrcMessage msg)
    {
        if (!await RequireRegisteredAsync()) return;

        if (msg.Parameters.Count < 2)
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_NEEDMOREPARAMS,
                "PRIVMSG :Not enough parameters");
            return;
        }

        var target = msg.Parameters[0];
        var content = msg.Parameters[1];

        if (!target.StartsWith('#'))
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_NOSUCHNICK,
                $"{target} :Private messages are not supported. Use channels.");
            return;
        }

        var channelName = IrcToEchoHubChannel(target);
        if (channelName is null) return;

        var error = await _chatService.SendMessageAsync(
            _conn.UserId!.Value, _conn.Nickname!, channelName, content);

        if (error is not null)
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_CANNOTSENDTOCHAN,
                $"#{channelName} :{error}");
        }
    }

    private async Task HandleQuitAsync(IrcMessage msg)
    {
        var quitMessage = msg.Parameters.Count > 0 ? msg.Parameters[0] : "Client quit";
        await _conn.SendAsync($"ERROR :Closing Link: {_conn.Nickname} ({quitMessage})");
    }

    // ── Query Commands ──────────────────────────────────────────────────────

    private async Task HandleNamesAsync(IrcMessage msg)
    {
        if (!await RequireRegisteredAsync()) return;
        if (msg.Parameters.Count < 1) return;

        var channelName = IrcToEchoHubChannel(msg.Parameters[0]);
        if (channelName is null) return;

        await SendNamesReplyAsync(channelName);
    }

    private async Task SendNamesReplyAsync(string channelName)
    {
        var users = await _chatService.GetOnlineUsersAsync(channelName);
        var nicks = string.Join(" ", users.Select(u => u.Username));

        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_NAMREPLY,
            $"= #{channelName} :{nicks}");
        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_ENDOFNAMES,
            $"#{channelName} :End of /NAMES list");
    }

    private async Task HandleTopicAsync(IrcMessage msg)
    {
        if (!await RequireRegisteredAsync()) return;
        if (msg.Parameters.Count < 1) return;

        var channelName = IrcToEchoHubChannel(msg.Parameters[0]);
        if (channelName is null) return;

        if (msg.Parameters.Count == 1)
        {
            await SendChannelTopicAsync(channelName);
        }
        else
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_CHANOPRIVSNEEDED,
                $"#{channelName} :Topic can only be changed by the channel creator via the API");
        }
    }

    private async Task SendChannelTopicAsync(string channelName)
    {
        var (topic, exists) = await _chatService.GetChannelTopicAsync(channelName);

        if (!exists) return;

        if (topic is not null)
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_TOPIC,
                $"#{channelName} :{topic}");
        }
        else
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_NOTOPIC,
                $"#{channelName} :No topic is set");
        }
    }

    private async Task HandleWhoAsync(IrcMessage msg)
    {
        if (!await RequireRegisteredAsync()) return;
        if (msg.Parameters.Count < 1) return;

        var channelName = IrcToEchoHubChannel(msg.Parameters[0]);
        if (channelName is null) return;

        var users = await _chatService.GetOnlineUsersAsync(channelName);

        foreach (var u in users)
        {
            var awayFlag = u.Status == UserStatus.Away ? "G" : "H";
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_WHOREPLY,
                $"#{channelName} {u.Username} echohub {ServerName} {u.Username} {awayFlag} :0 {u.DisplayName ?? u.Username}");
        }

        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_ENDOFWHO,
            $"#{channelName} :End of WHO list");
    }

    private async Task HandleWhoisAsync(IrcMessage msg)
    {
        if (!await RequireRegisteredAsync()) return;
        if (msg.Parameters.Count < 1) return;

        var nick = msg.Parameters[^1].ToLowerInvariant();
        var profile = await _chatService.GetUserProfileAsync(nick);

        if (profile is null)
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_NOSUCHNICK,
                $"{nick} :No such nick/channel");
            return;
        }

        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_WHOISUSER,
            $"{nick} {nick} echohub * :{profile.DisplayName ?? nick}");
        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_WHOISSERVER,
            $"{nick} {ServerName} :EchoHub IRC Gateway");

        var channels = await _chatService.GetChannelsForUserAsync(nick);
        if (channels.Count > 0)
        {
            var chanList = string.Join(" ", channels.Select(c => $"#{c}"));
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_WHOISCHANNELS,
                $"{nick} :{chanList}");
        }

        if (profile.Status == UserStatus.Away && profile.StatusMessage is not null)
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_AWAY,
                $"{nick} :{profile.StatusMessage}");
        }

        var idleSeconds = (long)(DateTimeOffset.UtcNow - profile.LastSeenAt).TotalSeconds;
        var signonUnix = profile.CreatedAt.ToUnixTimeSeconds();
        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_WHOISIDLE,
            $"{nick} {idleSeconds} {signonUnix} :seconds idle, signon time");

        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_ENDOFWHOIS,
            $"{nick} :End of WHOIS list");
    }

    private async Task HandleAwayAsync(IrcMessage msg)
    {
        if (!await RequireRegisteredAsync()) return;

        if (msg.Parameters.Count > 0 && !string.IsNullOrWhiteSpace(msg.Parameters[0]))
        {
            _conn.AwayMessage = msg.Parameters[0];
            await _chatService.UpdateStatusAsync(
                _conn.UserId!.Value, _conn.Nickname!, UserStatus.Away, _conn.AwayMessage);
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_NOWAWAY,
                ":You have been marked as being away");
        }
        else
        {
            _conn.AwayMessage = null;
            await _chatService.UpdateStatusAsync(
                _conn.UserId!.Value, _conn.Nickname!, UserStatus.Online, null);
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_UNAWAY,
                ":You are no longer marked as being away");
        }
    }

    private async Task HandleListAsync(IrcMessage msg)
    {
        if (!await RequireRegisteredAsync()) return;

        var channels = await _chatService.GetChannelListAsync();

        foreach (var ch in channels)
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_LIST,
                $"#{ch.Name} {ch.OnlineCount} :{ch.Topic ?? ""}");
        }

        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_LISTEND,
            ":End of LIST");
    }

    private async Task HandleModeAsync(IrcMessage msg)
    {
        if (!await RequireRegisteredAsync()) return;
        if (msg.Parameters.Count < 1) return;

        var target = msg.Parameters[0];

        if (target.StartsWith('#'))
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_CHANNELMODEIS,
                $"{target} +");
        }
        else
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_UMODEIS, "+");
        }
    }

    private async Task HandlePingAsync(IrcMessage msg)
    {
        var token = msg.Parameters.Count > 0 ? msg.Parameters[0] : ServerName;
        await _conn.SendAsync($":{ServerName} PONG {ServerName} :{token}");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<bool> RequireRegisteredAsync()
    {
        if (_conn.IsRegistered) return true;

        await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_NOTREGISTERED,
            ":You have not registered");
        return false;
    }

    private static string? IrcToEchoHubChannel(string ircChannel)
    {
        if (!ircChannel.StartsWith('#') || ircChannel.Length < 2)
            return null;

        var name = ircChannel[1..].ToLowerInvariant().Trim();
        return ValidationConstants.ChannelNameRegex().IsMatch(name) ? name : null;
    }
}
