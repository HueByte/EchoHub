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
    private readonly IUserService _userService;
    private readonly IChannelService _channelService;
    private readonly IMessageEncryptionService _encryption;
    private readonly ILogger _logger;

    private string ServerName => _options.ServerName;

    private static readonly Dictionary<string, string?> ServerCaps = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sasl"] = null,
        ["server-time"] = null,
        ["message-tags"] = null,
        ["echo-message"] = null,
        ["batch"] = null,
        ["draft/multiline"] = "max-bytes=40000,max-lines=10",
    };

    public IrcCommandHandler(
        IrcClientConnection conn,
        IrcOptions options,
        IChatService chatService,
        IUserService userService,
        IChannelService channelService,
        IMessageEncryptionService encryption,
        ILogger logger)
    {
        _conn = conn;
        _options = options;
        _chatService = chatService;
        _userService = userService;
        _channelService = channelService;
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

            _logger.LogDebug("IRC < {Id}: {Line}", _conn.ConnectionId, line);

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
            "NOTICE" => Task.CompletedTask,
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

            // IRCv3 multiline batch
            "BATCH" => HandleBatchAsync(msg),

            _ => _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_UNKNOWNCOMMAND,
                $"{command} :Unknown command"),
        };
    }

    // ── IRCv3 CAP Negotiation ─────────────────────────────────────────────

    private static readonly string[] CapList302 = [.. ServerCaps.Select(kvp =>
        kvp.Value is not null ? $"{kvp.Key}={kvp.Value}" : kvp.Key)];

    private static readonly string[] CapListLegacy = [.. ServerCaps.Keys];

    private async Task HandleCapAsync(IrcMessage msg)
    {
        if (msg.Parameters.Count < 1)
        {
            await SendInvalidCapCmdAsync("CAP requires a subcommand");
            return;
        }

        var subcommand = msg.Parameters[0].ToUpperInvariant();

        // Build the nick placeholder for server responses (use * while unregistered)
        var nick = _conn.Nickname ?? "*";

        switch (subcommand)
        {
            case "LS":
                await HandleCapLsAsync(msg, nick);
                break;

            case "LIST":
                await HandleCapListAsync(nick);
                break;

            case "REQ":
                await HandleCapReqAsync(msg, nick);
                break;

            case "END":
                _conn.CapNegotiating = false;
                if (_conn.Nickname is not null && _conn.Username is not null && !_conn.IsRegistered)
                    await TryCompleteRegistrationAsync();
                break;

            default:
                await SendInvalidCapCmdAsync($"Unknown subcommand {subcommand}");
                break;
        }
    }

    private async Task HandleCapLsAsync(IrcMessage msg, string nick)
    {
        // Parse optional version argument
        var version = 0;
        if (msg.Parameters.Count >= 2 && int.TryParse(msg.Parameters[1], out var v))
            version = v;

        // Store the highest version seen (clients cannot downgrade)
        if (version > _conn.CapVersion)
            _conn.CapVersion = version;

        // Decide capability list format based on negotiated version
        var caps = version >= 302 ? CapList302 : CapListLegacy;

        // Suspend registration during CAP negotiation
        _conn.CapNegotiating = true;

        // Multiline CAP LS 302 response
        if (version >= 302 && caps.Length > 0)
        {
            // If the total fits in one line, send it as a single reply
            var singleLine = string.Join(" ", caps);
            if (singleLine.Length < 400)
            {
                await _conn.SendAsync($":{ServerName} CAP {nick} LS :{singleLine}");
            }
            else
            {
                // Split across multiple lines; all but the last get '*' as a marker
                var lines = SplitCapList(caps, 400);
                for (var i = 0; i < lines.Count; i++)
                {
                    var marker = i < lines.Count - 1 ? "*" : "";
                    await _conn.SendAsync($":{ServerName} CAP {nick} LS {marker}:{lines[i]}");
                }
            }
        }
        else if (caps.Length > 0)
        {
            await _conn.SendAsync($":{ServerName} CAP {nick} LS :{string.Join(" ", caps)}");
        }
        else
        {
            await _conn.SendAsync($":{ServerName} CAP {nick} LS :");
        }
    }

    private async Task HandleCapListAsync(string nick)
    {
        var enabled = _conn.EnabledCaps.ToArray();
        if (enabled.Length > 0)
        {
            await _conn.SendAsync($":{ServerName} CAP {nick} LIST :{string.Join(" ", enabled)}");
        }
        else
        {
            await _conn.SendAsync($":{ServerName} CAP {nick} LIST :");
        }
    }

    private async Task HandleCapReqAsync(IrcMessage msg, string nick)
    {
        if (msg.Parameters.Count < 2 || string.IsNullOrWhiteSpace(msg.Parameters[1]))
        {
            await _conn.SendAsync($":{ServerName} CAP {nick} NAK :");
            return;
        }

        var requested = msg.Parameters[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Validate all caps are known before modifying anything (all-or-nothing)
        var ackList = new List<string>();
        var valid = true;

        foreach (var item in requested)
        {
            var cap = item;
            if (cap.StartsWith('-'))
                cap = cap[1..];

            // cap-notify is implicitly enabled for CAP LS 302; accept it silently
            if (cap.Equals("cap-notify", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!ServerCaps.ContainsKey(cap))
            {
                valid = false;
                break;
            }
        }

        if (!valid)
        {
            await _conn.SendAsync($":{ServerName} CAP {nick} NAK :{msg.Parameters[1]}");
            return;
        }

        foreach (var item in requested)
        {
            if (item.StartsWith('-'))
            {
                _conn.DisableCap(item[1..]);
                ackList.Add(item);
            }
            else
            {
                _conn.EnableCap(item);
                ackList.Add(item);
            }
        }

        if (ackList.Count > 0)
        {
            await _conn.SendAsync($":{ServerName} CAP {nick} ACK :{string.Join(" ", ackList)}");
        }
        else
        {
            // No actual caps to ack (e.g. cap-notify only) — send empty ACK
            await _conn.SendAsync($":{ServerName} CAP {nick} ACK :");
        }
    }

    private async Task SendInvalidCapCmdAsync(string message)
    {
        var nick = _conn.Nickname ?? "*";
        await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_INVALIDCAPCMD, nick, $"{message}");
    }

    private static List<string> SplitCapList(string[] caps, int maxLen)
    {
        var lines = new List<string>();
        var current = new List<string>();
        var currentLen = 0;

        foreach (var cap in caps)
        {
            var capLen = cap.Length + (current.Count > 0 ? 1 : 0); // +1 for leading space
            if (currentLen + capLen > maxLen && current.Count > 0)
            {
                lines.Add(string.Join(" ", current));
                current.Clear();
                currentLen = 0;
                capLen = cap.Length;
            }
            current.Add(cap);
            currentLen += capLen;
        }

        if (current.Count > 0)
            lines.Add(string.Join(" ", current));

        return lines.Count > 0 ? lines : [""];
    }

    // ── IRCv3 Multiline Batch ─────────────────────────────────────────────

    private async Task HandleBatchAsync(IrcMessage msg)
    {
        if (!await RequireRegisteredAsync()) return;
        if (msg.Parameters.Count < 1) return;

        var reference = msg.Parameters[0];

        if (reference.StartsWith('-'))
        {
            // BATCH -ref → end of batch
            var batch = _conn.PendingMultilineBatch;
            if (batch is null || batch.ReferenceTag != reference[1..])
                return;

            _conn.PendingMultilineBatch = null;
            await FlushMultilineBatchAsync(batch);
        }
        else
        {
            // BATCH +ref type [target]
            if (msg.Parameters.Count < 2) return;
            var type = msg.Parameters[1];

            if (!type.Equals("draft/multiline", StringComparison.OrdinalIgnoreCase))
                return;

            if (msg.Parameters.Count < 3) return;
            var target = msg.Parameters[2];

            var channelName = IrcToEchoHubChannel(target);
            if (channelName is null) return;

            var batchCtx = new MultilineBatchContext(reference[1..], channelName);

            // Capture +reply tag from the BATCH start line for reply handling
            if (msg.Tags.TryGetValue("+reply", out var replyStr) &&
                Guid.TryParse(replyStr, out var replyId))
            {
                batchCtx.ReplyToMessageId = replyId;
            }

            _conn.PendingMultilineBatch = batchCtx;
        }
    }

    private async Task FlushMultilineBatchAsync(MultilineBatchContext batch)
    {
        if (batch.Lines.Count == 0) return;

        // Validate: no blank lines with concat tag, no entirely blank messages
        var allBlank = true;
        for (var i = 0; i < batch.Lines.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(batch.Lines[i]))
            {
                allBlank = false;
                break;
            }
        }

        if (allBlank) return;

        // Per spec: lines joined by \n by default; draft/multiline-concat lines
        // are directly concatenated (already handled during collection).
        var content = string.Join("\n", batch.Lines);

        var error = await _chatService.SendMessageAsync(
            _conn.UserId!.Value, _conn.Nickname!, batch.Target, content, _conn.ConnectionId, batch.ReplyToMessageId);

        if (error is not null)
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_CANNOTSENDTOCHAN,
                $"#{batch.Target} :{error}");
        }
    }

    // ── Authentication ──────────────────────────────────────────────────────

    private async Task HandleAuthenticateAsync(IrcMessage msg)
    {
        if (msg.Parameters.Count < 1) return;

        if (msg.Parameters[0].Equals("PLAIN", StringComparison.OrdinalIgnoreCase))
        {
            await _conn.SendAsync("AUTHENTICATE +");
            return;
        }

        // AUTHENTICATE * = client aborts SASL
        if (msg.Parameters[0] == "*")
        {
            _conn.IsSasl = false;
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_SASLFAIL,
                ":SASL authentication aborted");
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

            _logger.LogDebug("SASL PLAIN auth attempt for user '{Username}' (connection {Id})",
                username, _conn.ConnectionId);

            var result = await _userService.AuthenticateUserAsync(username, password);

            // Auth failed — try registering a new account
            if (!result.IsSuccess)
                result = await _userService.RegisterUserAsync(username, password);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("SASL auth/register failed for user '{Username}': {Error} (connection {Id})",
                    username, result.ErrorMessage, _conn.ConnectionId);
                await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_SASLFAIL,
                    $":SASL authentication failed — {result.ErrorMessage}");
                return;
            }

            _conn.Nickname = result.User!.Username;
            _conn.UserId = result.User!.Id;
            _conn.IsAuthenticated = true;

            _logger.LogInformation("SASL auth succeeded for user '{Username}' (connection {Id})",
                username, _conn.ConnectionId);

            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_LOGGEDIN,
                $"{_conn.Hostmask} {username} :You are now logged in as {username}");
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_SASLSUCCESS,
                ":SASL authentication successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SASL auth exception for connection {Id}", _conn.ConnectionId);
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
        _logger.LogDebug("TryCompleteRegistration: CapNeg={Cap}, Registered={Reg}, Authenticated={Auth}, Nick={Nick}, User={User}, Id={Id}",
            _conn.CapNegotiating, _conn.IsRegistered, _conn.IsAuthenticated,
            _conn.Nickname, _conn.Username, _conn.ConnectionId);

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

        var result = await _userService.AuthenticateUserAsync(_conn.Nickname!, _conn.Password);

        // Auth failed — try registering a new account
        if (!result.IsSuccess)
            result = await _userService.RegisterUserAsync(_conn.Nickname!, _conn.Password);

        if (!result.IsSuccess)
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_PASSWDMISMATCH,
                $":{result.ErrorMessage}");
            await _conn.SendAsync("ERROR :Authentication failed");
            return;
        }

        _conn.UserId = result.User!.Id;
        _conn.Nickname = result.User!.Username;
        _conn.IsAuthenticated = true;
        _conn.IsRegistered = true;

        await _chatService.UserConnectedAsync(_conn.ConnectionId, result.User!.Id, result.User!.Username);
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

        var isupportTokens = new List<string>
        {
            "CHANTYPES=#",
            "CHANMODES=b,k,,,",
            "NICKLEN=50",
            "CHANNELLEN=100",
            "CLIENTTAGDENY=*,-reply",
        };

        // If the client has message-tags, advertise CLIENTTAGDENY
        if (_conn.HasCap("message-tags"))
        {
            isupportTokens.Add("CLIENTTAGDENY=*,-reply");
        }

        isupportTokens.Add(":are supported by this server");

        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_ISUPPORT,
            string.Join(" ", isupportTokens));

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

        // RFC 1459: optional second parameter carries comma-separated channel keys,
        // paired with channels by position (JOIN #a,#b key1,key2).
        var keys = msg.Parameters.Count > 1
            ? msg.Parameters[1].Split(',')
            : [];

        for (var i = 0; i < channels.Length; i++)
        {
            var rawChannel = channels[i];
            var key = i < keys.Length && !string.IsNullOrEmpty(keys[i]) ? keys[i] : null;

            var channelName = IrcToEchoHubChannel(rawChannel);
            if (channelName is null)
            {
                await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_NOSUCHCHANNEL,
                    $"{rawChannel} :Invalid channel name");
                continue;
            }

            // End-to-end encrypted channels can't be read over IRC (the gateway would
            // have to hold the room key server-side, defeating the privacy guarantee).
            var crypto = await _channelService.GetChannelCryptoAsync(channelName);
            if (crypto?.IsEncrypted == true)
            {
                await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_BADCHANNELKEY,
                    $"#{channelName} :Cannot join channel — end-to-end encrypted, use the EchoHub client");
                continue;
            }

            // System channels (e.g. the live log room) stream over SignalR only — the IRC
            // gateway never carries their content, so block joins outright.
            var channelInfo = await _channelService.GetChannelByNameAsync(channelName);
            if (channelInfo?.IsSystem == true)
            {
                await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_NOSUCHCHANNEL,
                    $"#{channelName} :Cannot join channel — server-managed, use the EchoHub client");
                continue;
            }

            var (history, error, passwordRequired) = await _chatService.JoinChannelAsync(
                _conn.ConnectionId, _conn.UserId!.Value, _conn.Nickname!, channelName, key);

            if (error is not null)
            {
                if (passwordRequired)
                {
                    await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_BADCHANNELKEY,
                        $"#{channelName} :Cannot join channel (+k) — {error}");
                }
                else
                {
                    await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_NOSUCHCHANNEL,
                        $"#{channelName} :{error}");
                }
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
                var decrypted = m with
                {
                    Content = _encryption.Decrypt(m.Content),
                    ReplyTo = m.ReplyTo is { } reply ? reply with { Content = _encryption.Decrypt(reply.Content) } : null,
                };
                var lines = IrcMessageFormatter.FormatMessage(decrypted, _options.PublicBaseUrl);
                foreach (var line in lines)
                    await _conn.SendAsync(DecorateLineForConnection(line, m));
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

        // Check if we're inside a multiline batch
        var batch = _conn.PendingMultilineBatch;
        if (batch is not null)
        {
            if (batch.Target != channelName)
                return;

            // Collect this line. If the message has the draft/multiline-concat tag,
            // it appends directly without a newline separator.
            var isConcat = msg.Tags.ContainsKey("draft/multiline-concat");
            if (isConcat)
            {
                if (string.IsNullOrEmpty(content))
                    return; // blank concat lines not allowed
                batch.UsesConcat = true;
                // Append to the last line (or start a new one)
                if (batch.Lines.Count > 0)
                    batch.Lines[^1] += content;
                else
                    batch.Lines.Add(content);
            }
            else
            {
                batch.Lines.Add(content);
            }
            return;
        }

        // Parse +reply tag for reply-to support from IRC clients
        Guid? replyTo = null;
        if (msg.Tags.TryGetValue("+reply", out var replyStr) &&
            Guid.TryParse(replyStr, out var replyId))
        {
            replyTo = replyId;
        }

        var error = await _chatService.SendMessageAsync(
            _conn.UserId!.Value, _conn.Nickname!, channelName, content, _conn.ConnectionId, replyTo);

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
            var topic = msg.Parameters[1];
            var result = await _channelService.UpdateTopicAsync(
                _conn.UserId!.Value, channelName, string.IsNullOrWhiteSpace(topic) ? null : topic);

            if (!result.IsSuccess)
            {
                var numeric = result.Error == ChannelError.NotFound
                    ? IrcNumericReply.ERR_NOSUCHCHANNEL
                    : IrcNumericReply.ERR_CHANOPRIVSNEEDED;
                await _conn.SendNumericAsync(ServerName, numeric, $"#{channelName} :{result.ErrorMessage}");
                return;
            }

            // Notify SignalR clients and echo the change back to the IRC client
            await _chatService.BroadcastChannelUpdatedAsync(result.Channel!, channelName);
            await _conn.SendAsync($":{_conn.Hostmask} TOPIC #{channelName} :{result.Channel!.Topic ?? ""}");
        }
    }

    private async Task SendChannelTopicAsync(string channelName)
    {
        var (topic, exists) = await _channelService.GetChannelTopicAsync(channelName);

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
        var profile = await _userService.GetUserProfileAsync(nick);

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

        var channels = await _channelService.GetChannelListAsync();

        // Private channels are hidden from discovery, matching the SignalR client's channel list
        foreach (var ch in channels.Where(c => c.IsPublic))
        {
            var lockHint = ch.IsProtected ? "[+k] " : "";
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_LIST,
                $"#{ch.Name} {ch.OnlineCount} :{lockHint}{ch.Topic ?? ""}");
        }

        await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_LISTEND,
            ":End of LIST");
    }

    private async Task HandleModeAsync(IrcMessage msg)
    {
        if (!await RequireRegisteredAsync()) return;
        if (msg.Parameters.Count < 1) return;

        var target = msg.Parameters[0];

        if (!target.StartsWith('#'))
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_UMODEIS, "+");
            return;
        }

        var channelName = IrcToEchoHubChannel(target);
        if (channelName is null)
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_NOSUCHCHANNEL,
                $"{target} :No such channel");
            return;
        }

        // Query: MODE #channel
        if (msg.Parameters.Count == 1)
        {
            var channel = await _channelService.GetChannelByNameAsync(channelName);
            if (channel is null)
            {
                await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_NOSUCHCHANNEL,
                    $"#{channelName} :No such channel");
                return;
            }

            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_CHANNELMODEIS,
                $"#{channelName} {(channel.IsProtected ? "+k" : "+")}");
            return;
        }

        var modes = msg.Parameters[1];

        // Clients commonly probe the ban list on join — reply with an empty list
        if (modes is "b" or "+b")
        {
            await _conn.SendNumericAsync(ServerName, IrcNumericReply.RPL_ENDOFBANLIST,
                $"#{channelName} :End of channel ban list");
            return;
        }

        switch (modes)
        {
            case "+k":
                if (msg.Parameters.Count < 3)
                {
                    await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_NEEDMOREPARAMS,
                        "MODE :Not enough parameters");
                    return;
                }

                var key = msg.Parameters[2];
                var setResult = await _channelService.SetChannelPasswordAsync(_conn.UserId!.Value, channelName, key);
                if (!setResult.IsSuccess)
                {
                    await SendModeErrorAsync(channelName, setResult);
                    return;
                }

                await _conn.SendAsync($":{_conn.Hostmask} MODE #{channelName} +k {key}");
                return;

            case "-k":
                var clearResult = await _channelService.SetChannelPasswordAsync(_conn.UserId!.Value, channelName, null);
                if (!clearResult.IsSuccess)
                {
                    await SendModeErrorAsync(channelName, clearResult);
                    return;
                }

                await _conn.SendAsync($":{_conn.Hostmask} MODE #{channelName} -k *");
                return;

            default:
                await _conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_UNKNOWNMODE,
                    $"{modes} :is unknown mode char to me for #{channelName}");
                return;
        }
    }

    private async Task SendModeErrorAsync(string channelName, ChannelOperationResult result)
    {
        var numeric = result.Error switch
        {
            ChannelError.NotFound => IrcNumericReply.ERR_NOSUCHCHANNEL,
            ChannelError.Forbidden => IrcNumericReply.ERR_CHANOPRIVSNEEDED,
            _ => IrcNumericReply.ERR_KEYSET,
        };
        await _conn.SendNumericAsync(ServerName, numeric, $"#{channelName} :{result.ErrorMessage}");
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

    private string DecorateLineForConnection(string line, MessageDto message)
    {
        var tags = new List<(string Key, string? Value)>();

        if (_conn.HasCap("server-time"))
            tags.Add(("time", message.SentAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));

        if (_conn.HasCap("message-tags"))
        {
            tags.Add(("msgid", message.Id.ToString("D")));
            if (message.ReplyTo is not null)
                tags.Add(("+reply", message.ReplyTo.MessageId.ToString("D")));
        }

        if (tags.Count == 0)
            return line;

        return IrcMessage.BuildTagPrefix([.. tags]) + line;
    }
}
