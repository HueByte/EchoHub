using EchoHub.Core.Models;

namespace EchoHub.Client.Commands;

public record CommandResult(bool Handled, string? Message = null, bool IsError = false);

public class CommandHandler
{
    public event Func<UserStatus, string?, Task>? OnSetStatus;
    public event Func<string, Task>? OnSetNick;
    public event Func<string, Task>? OnSetColor;
    public event Func<string, Task>? OnSetTheme;
    public event Func<string, Task>? OnSendFile;
    public event Func<string?, Task>? OnOpenProfile;
    public event Func<Task>? OnOpenServers;
    public event Func<string, Task>? OnJoinChannel;
    public event Func<Task>? OnLeaveChannel;
    public event Func<string, Task>? OnSetTopic;
    public event Func<Task>? OnListUsers;
    public event Func<string, Task>? OnSetAvatar;
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
            return new CommandResult(true, "Usage: /send <filepath or URL>", IsError: true);

        var target = args.Trim().Trim('"');

        if (Uri.TryCreate(target, UriKind.Absolute, out var uri)
            && (uri.Scheme == "http" || uri.Scheme == "https"))
        {
            if (OnSendFile is not null)
                await OnSendFile(target);
            var fileName = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "image";
            return new CommandResult(true, $"Sending: {fileName}...");
        }

        if (!File.Exists(target))
            return new CommandResult(true, $"File not found: {target}", IsError: true);

        if (OnSendFile is not null)
            await OnSendFile(target);
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

        var target = args.Trim().Trim('"');

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
              /send <filepath or URL>               - Send a file or image
              /avatar <URL or filepath>             - Set your avatar
              /profile [username]                   - View a profile (yours if no name given)
              /servers                             - Open saved servers
              /join <channel>                      - Join a channel
              /leave                               - Leave current channel
              /topic <text>                        - Set channel topic
              /users                               - List online users
              /quit                                - Exit the app
            """);
    }

    private static bool IsValidHex(string s) =>
        s.All(c => char.IsAsciiHexDigit(c));
}
