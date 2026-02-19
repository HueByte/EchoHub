using EchoHub.Core.Models;

namespace EchoHub.Client.Commands;

public record CommandResult(bool Handled, string? Message = null, bool IsError = false);

public class CommandHandler
{
    public event Func<UserStatus, string?, Task>? OnSetStatus;
    public event Func<string, Task>? OnSetNick;
    public event Func<string, Task>? OnSetColor;
    public event Func<string, Task>? OnSetTheme;
    public event Func<string, string?, Task>? OnSendFile;
    public event Func<string?, Task>? OnOpenProfile;
    public event Func<Task>? OnOpenServers;
    public event Func<string, Task>? OnJoinChannel;
    public event Func<Task>? OnLeaveChannel;
    public event Func<string, Task>? OnSetTopic;
    public event Func<Task>? OnListUsers;
    public event Func<string, Task>? OnSetAvatar;
    public event Func<string, string?, Task>? OnKickUser;
    public event Func<string, string?, Task>? OnBanUser;
    public event Func<string, Task>? OnUnbanUser;
    public event Func<string, int?, Task>? OnMuteUser;
    public event Func<string, Task>? OnUnmuteUser;
    public event Func<string, string, Task>? OnAssignRole;
    public event Func<Task>? OnNukeChannel;
    public event Func<Task>? OnQuit;
    public event Func<Task>? OnHelp;

    public bool IsCommand(string input) => input.StartsWith('/');

    public async Task<CommandResult> HandleAsync(string input)
    {
        if (!IsCommand(input))
            return new CommandResult(false);

        var parts = input[1..].Split(' ', 2, StringSplitOptions.TrimEntries);
        var command = parts[0].ToLowerInvariant();
        var args = parts.Length > 1 ? parts[1] : string.Empty;

        return command switch
        {
            "status" => await HandleStatus(args),
            "nick" => await HandleNick(args),
            "color" => await HandleColor(args),
            "theme" => await HandleTheme(args),
            "send" => await HandleSend(args),
            "profile" => await HandleProfile(args),
            "avatar" => await HandleAvatar(args),
            "servers" => await HandleServers(),
            "join" => await HandleJoin(args),
            "leave" => await HandleLeave(),
            "topic" => await HandleTopic(args),
            "users" => await HandleUsers(),
            "kick" => await HandleKick(args),
            "ban" => await HandleBan(args),
            "unban" => await HandleUnban(args),
            "mute" => await HandleMute(args),
            "unmute" => await HandleUnmute(args),
            "role" => await HandleRole(args),
            "nuke" => await HandleNuke(),
            "quit" or "exit" => await HandleQuit(),
            "help" or "?" => await HandleHelp(),
            _ => new CommandResult(true, $"Unknown command: /{command}. Type /help for available commands.", IsError: true),
        };
    }

    private async Task<CommandResult> HandleStatus(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new CommandResult(true, "Usage: /status <online|away|dnd|invisible> or /status <message>", IsError: true);

        var statusArg = args.ToLowerInvariant().Trim();
        UserStatus? status = statusArg switch
        {
            "online" => UserStatus.Online,
            "away" => UserStatus.Away,
            "dnd" or "donotdisturb" => UserStatus.DoNotDisturb,
            "invisible" => UserStatus.Invisible,
            _ => null,
        };

        if (status.HasValue)
        {
            if (OnSetStatus is not null)
                await OnSetStatus(status.Value, null);
            return new CommandResult(true, $"Status set to {status.Value}");
        }

        // Treat as custom status message (keep current status)
        if (OnSetStatus is not null)
            await OnSetStatus(UserStatus.Online, args);
        return new CommandResult(true, $"Status message set: {args}");
    }

    private async Task<CommandResult> HandleNick(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new CommandResult(true, "Usage: /nick <display name>", IsError: true);

        if (OnSetNick is not null)
            await OnSetNick(args.Trim());
        return new CommandResult(true, $"Display name set to: {args.Trim()}");
    }

    private async Task<CommandResult> HandleColor(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new CommandResult(true, "Usage: /color <hex> (e.g., /color #FF5733)", IsError: true);

        var color = args.Trim();
        if (!color.StartsWith('#'))
            color = "#" + color;

        if (color.Length != 7 || !IsValidHex(color[1..]))
            return new CommandResult(true, "Invalid color. Use hex format: #RRGGBB", IsError: true);

        if (OnSetColor is not null)
            await OnSetColor(color);
        return new CommandResult(true, $"Nickname color set to: {color}");
    }

    private async Task<CommandResult> HandleTheme(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new CommandResult(true, "Usage: /theme <name> (Default, Dark, Light, Hacker, Solarized)", IsError: true);

        if (OnSetTheme is not null)
            await OnSetTheme(args.Trim());
        return new CommandResult(true, $"Theme switched to: {args.Trim()}");
    }

    private async Task<CommandResult> HandleSend(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new CommandResult(true, "Usage: /send <filepath or URL> [-s|-m|-l]", IsError: true);

        // Extract optional size flag from end or start, respecting quoted paths
        var (target, size) = ParsePathAndSizeFlag(args);

        if (string.IsNullOrWhiteSpace(target))
            return new CommandResult(true, "Usage: /send <filepath or URL> [-s|-m|-l]", IsError: true);

        if (Uri.TryCreate(target, UriKind.Absolute, out var uri)
            && (uri.Scheme == "http" || uri.Scheme == "https"))
        {
            if (OnSendFile is not null)
                await OnSendFile(target, size);
            var fileName = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "image";
            return new CommandResult(true, $"Sending: {fileName}...");
        }

        if (!File.Exists(target))
            return new CommandResult(true, $"File not found: {target}", IsError: true);

        if (OnSendFile is not null)
            await OnSendFile(target, size);
        return new CommandResult(true, $"Uploading: {Path.GetFileName(target)}...");
    }

    private async Task<CommandResult> HandleProfile(string args)
    {
        var username = string.IsNullOrWhiteSpace(args) ? null : args.Trim();
        if (OnOpenProfile is not null)
            await OnOpenProfile(username);
        return new CommandResult(true);
    }

    private async Task<CommandResult> HandleAvatar(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new CommandResult(true, "Usage: /avatar <URL or filepath>", IsError: true);

        var target = StripQuotes(args.Trim());

        if (OnSetAvatar is not null)
            await OnSetAvatar(target);
        return new CommandResult(true, "Uploading avatar...");
    }

    private async Task<CommandResult> HandleServers()
    {
        if (OnOpenServers is not null)
            await OnOpenServers();
        return new CommandResult(true);
    }

    private async Task<CommandResult> HandleJoin(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new CommandResult(true, "Usage: /join <channel>", IsError: true);

        var channel = args.Trim().TrimStart('#');
        if (OnJoinChannel is not null)
            await OnJoinChannel(channel);
        return new CommandResult(true);
    }

    private async Task<CommandResult> HandleLeave()
    {
        if (OnLeaveChannel is not null)
            await OnLeaveChannel();
        return new CommandResult(true);
    }

    private async Task<CommandResult> HandleTopic(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new CommandResult(true, "Usage: /topic <text>", IsError: true);

        if (OnSetTopic is not null)
            await OnSetTopic(args.Trim());
        return new CommandResult(true, $"Topic set to: {args.Trim()}");
    }

    private async Task<CommandResult> HandleUsers()
    {
        if (OnListUsers is not null)
            await OnListUsers();
        return new CommandResult(true);
    }

    private async Task<CommandResult> HandleQuit()
    {
        if (OnQuit is not null)
            await OnQuit();
        return new CommandResult(true);
    }

    private async Task<CommandResult> HandleKick(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new CommandResult(true, "Usage: /kick <username> [reason]", IsError: true);

        var parts = args.Split(' ', 2, StringSplitOptions.TrimEntries);
        var username = parts[0];
        var reason = parts.Length > 1 ? parts[1] : null;

        if (OnKickUser is not null)
            await OnKickUser(username, reason);
        return new CommandResult(true, $"Kicking {username}...");
    }

    private async Task<CommandResult> HandleBan(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new CommandResult(true, "Usage: /ban <username> [reason]", IsError: true);

        var parts = args.Split(' ', 2, StringSplitOptions.TrimEntries);
        var username = parts[0];
        var reason = parts.Length > 1 ? parts[1] : null;

        if (OnBanUser is not null)
            await OnBanUser(username, reason);
        return new CommandResult(true, $"Banning {username}...");
    }

    private async Task<CommandResult> HandleUnban(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new CommandResult(true, "Usage: /unban <username>", IsError: true);

        if (OnUnbanUser is not null)
            await OnUnbanUser(args.Trim());
        return new CommandResult(true, $"Unbanning {args.Trim()}...");
    }

    private async Task<CommandResult> HandleMute(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new CommandResult(true, "Usage: /mute <username> [duration_minutes]", IsError: true);

        var parts = args.Split(' ', 2, StringSplitOptions.TrimEntries);
        var username = parts[0];
        int? duration = parts.Length > 1 && int.TryParse(parts[1], out var d) ? d : null;

        if (OnMuteUser is not null)
            await OnMuteUser(username, duration);
        return new CommandResult(true, $"Muting {username}...");
    }

    private async Task<CommandResult> HandleUnmute(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new CommandResult(true, "Usage: /unmute <username>", IsError: true);

        if (OnUnmuteUser is not null)
            await OnUnmuteUser(args.Trim());
        return new CommandResult(true, $"Unmuting {args.Trim()}...");
    }

    private async Task<CommandResult> HandleRole(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new CommandResult(true, "Usage: /role <username> <admin|mod|member>", IsError: true);

        var parts = args.Split(' ', 2, StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            return new CommandResult(true, "Usage: /role <username> <admin|mod|member>", IsError: true);

        var username = parts[0];
        var role = parts[1].ToLowerInvariant();

        if (role is not ("admin" or "mod" or "member"))
            return new CommandResult(true, "Invalid role. Use: admin, mod, or member", IsError: true);

        if (OnAssignRole is not null)
            await OnAssignRole(username, role);
        return new CommandResult(true, $"Setting {username} to {role}...");
    }

    private async Task<CommandResult> HandleNuke()
    {
        if (OnNukeChannel is not null)
            await OnNukeChannel();
        return new CommandResult(true, "Nuking channel history...");
    }

    private async Task<CommandResult> HandleHelp()
    {
        if (OnHelp is not null)
            await OnHelp();
        return new CommandResult(true, """
            Available commands:
              /status <online|away|dnd|invisible>  - Set your status
              /status <message>                    - Set status message
              /nick <name>                         - Set display name
              /color <#hex>                        - Set nickname color
              /theme <name>                        - Switch theme
              /send <filepath or URL> [-s|-m|-l]      - Send a file or image (size: small/medium/large)
              /avatar <URL or filepath>             - Set your avatar
              /profile [username]                   - View a profile
              /servers                             - Open saved servers
              /join <channel>                      - Join a channel
              /leave                               - Leave current channel
              /topic <text>                        - Set channel topic
              /users                               - List online users
            Moderation:
              /kick <user> [reason]                - Kick a user (Mod+)
              /ban <user> [reason]                 - Ban a user (Admin+)
              /unban <user>                        - Unban a user (Admin+)
              /mute <user> [minutes]               - Mute a user (Mod+)
              /unmute <user>                       - Unmute a user (Mod+)
              /role <user> <admin|mod|member>      - Assign role (Admin+)
              /nuke                                - Clear channel history (Mod+)
              /quit                                - Exit the app
            """);
    }

    /// <summary>
    /// Extract a file path (possibly quoted) and an optional size flag (-s, -m, -l).
    /// The flag can appear before or after the path.
    /// </summary>
    private static (string Path, string? Size) ParsePathAndSizeFlag(string args)
    {
        var trimmed = args.Trim();
        string? size = null;

        // Check for flag at the end: "path" -m  or  path -m
        if (trimmed.Length > 3)
        {
            var suffix = trimmed[^2..];
            if (suffix is "-s" or "-m" or "-l" && trimmed[^3] == ' ')
            {
                size = suffix[1..];
                trimmed = trimmed[..^3].TrimEnd();
            }
        }

        // Check for flag at the start: -m "path"  or  -m path
        if (size is null && trimmed.Length > 3)
        {
            var prefix = trimmed[..2];
            if (prefix is "-s" or "-m" or "-l" && trimmed[2] == ' ')
            {
                size = prefix[1..];
                trimmed = trimmed[3..].TrimStart();
            }
        }

        return (StripQuotes(trimmed), size);
    }

    /// <summary>
    /// Remove matching surrounding quotes (double or single) from a string.
    /// </summary>
    private static string StripQuotes(string s)
    {
        if (s.Length >= 2 &&
            ((s[0] == '"' && s[^1] == '"') || (s[0] == '\'' && s[^1] == '\'')))
            return s[1..^1];
        return s;
    }

    private static bool IsValidHex(string s) =>
        s.All(c => char.IsAsciiHexDigit(c));
}
