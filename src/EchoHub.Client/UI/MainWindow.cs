using System.Collections.ObjectModel;
using EchoHub.Client.Themes;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI;

/// <summary>
/// Main Terminal.Gui window for the EchoHub chat client.
/// </summary>
public sealed class MainWindow : Runnable
{
    private readonly IApplication _app;
    private readonly ListView _channelList;
    private readonly ListView _messageList;
    private readonly TextView _inputField;
    private readonly FrameView _chatFrame;
    private readonly Label _statusLabel;
    private readonly Label _topicLabel;
    private MenuBar _menuBar;

    // Cached Key constants — compare via .KeyCode to avoid Key.Equals (which also checks Handled)
    private static readonly Key EnterKey = Key.Enter;
    private static readonly Key AltEnterKey = Key.Enter.WithAlt;
    private static readonly Key CtrlCKey = Key.C.WithCtrl;
    private static readonly Key TabKey = Key.Tab;

    // Available slash commands for Tab autocomplete
    private static readonly string[] SlashCommands =
    [
        "/status", "/nick", "/color", "/theme", "/send",
        "/profile", "/servers", "/join", "/leave", "/topic",
        "/users", "/quit", "/help"
    ];

    private readonly List<string> _channelNames = [];
    private readonly Dictionary<string, List<ChatLine>> _channelMessages = [];
    private readonly Dictionary<string, int> _channelUnread = [];
    private readonly Dictionary<string, string?> _channelTopics = [];
    private string _currentChannel = string.Empty;
    private string _currentUser = string.Empty;
    private int _lastChatWidth;

    /// <summary>
    /// Fired when the user selects a channel. Parameter is the channel name.
    /// </summary>
    public event Action<string>? OnChannelSelected;

    /// <summary>
    /// Fired when the user presses Enter in the input field. Parameters: channel name, message content.
    /// </summary>
    public event Action<string, string>? OnMessageSubmitted;

    /// <summary>
    /// Fired when the user requests to connect via the menu.
    /// </summary>
    public event Action? OnConnectRequested;

    /// <summary>
    /// Fired when the user requests to disconnect via the menu.
    /// </summary>
    public event Action? OnDisconnectRequested;

    /// <summary>
    /// Fired when the user requests to open their profile panel.
    /// </summary>
    public event Action? OnProfileRequested;

    /// <summary>
    /// Fired when the user requests to set their status.
    /// </summary>
    public event Action? OnStatusRequested;

    /// <summary>
    /// Fired when the user selects a theme from the menu. Parameter is the theme name.
    /// </summary>
    public event Action<string>? OnThemeSelected;

    /// <summary>
    /// Fired when the user requests to view saved servers.
    /// </summary>
    public event Action? OnSavedServersRequested;

    /// <summary>
    /// Fired when the user requests to create a new channel.
    /// </summary>
    public event Action? OnCreateChannelRequested;

    public MainWindow(IApplication app)
    {
        _app = app;
        Arrangement = ViewArrangement.Fixed;

        // Menu bar at the top
        _menuBar = BuildMenuBar();
        Add(_menuBar);

        // Left panel - channels
        var channelsFrame = new FrameView
        {
            Title = "Channels",
            X = 0,
            Y = 1, // below menu bar
            Width = 25,
            Height = Dim.Fill(1) // leave room for status bar
        };

        _channelList = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        _channelList.SetSource(new ObservableCollection<string>(_channelNames));
        _channelList.ValueChanged += OnChannelListSelectionChanged;
        channelsFrame.Add(_channelList);
        Add(channelsFrame);

        // Topic bar — sits above the chat frame in the right column
        _topicLabel = new Label
        {
            Text = "",
            X = 25,
            Y = 1,
            Width = Dim.Fill(),
            Height = 1,
            Visible = false
        };
        Add(_topicLabel);

        // Center panel - messages
        _chatFrame = new FrameView
        {
            Title = "Chat",
            X = 25,
            Y = 1, // below menu bar (shifts to 2 when topic is visible)
            Width = Dim.Fill(),
            Height = Dim.Fill(6) // leave room for input area and status bar
        };

        _messageList = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        _messageList.Source = new ChatListSource();
        _chatFrame.Add(_messageList);
        Add(_chatFrame);

        // Bottom input area
        var inputFrame = new FrameView
        {
            Title = "Message (Enter=send, Tab=autocomplete)",
            X = 25,
            Y = Pos.Bottom(_chatFrame),
            Width = Dim.Fill(),
            Height = 5
        };

        _inputField = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            WordWrap = true
        };
        _inputField.KeyDown += OnInputKeyDown;
        inputFrame.Add(_inputField);
        Add(inputFrame);

        // Status bar at the very bottom
        _statusLabel = new Label
        {
            Text = "Disconnected",
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1
        };
        _statusLabel.SetScheme(SchemeManager.GetScheme("Menu"));
        Add(_statusLabel);

        // Apply our custom color schemes to all views
        ApplyColorSchemes();

        // Re-wrap messages when the chat area is resized
        _messageList.ViewportChanged += (_, _) => OnChatViewportChanged();

        // Window-level key handling for Ctrl+C (quit)
        KeyDown += OnWindowKeyDown;
    }

    /// <summary>
    /// Applies the currently registered color schemes to all views.
    /// Call after theme changes to refresh colors.
    /// </summary>
    public void ApplyColorSchemes()
    {
        var baseScheme = SchemeManager.GetScheme("Base");
        var menuScheme = SchemeManager.GetScheme("Menu");

        if (baseScheme is not null)
        {
            this.SetScheme(baseScheme);

            // Propagate to all child views that should use the base scheme
            foreach (var sub in SubViews)
            {
                if (sub != _menuBar && sub != _statusLabel && sub != _topicLabel)
                    sub.SetScheme(baseScheme);
            }
        }

        if (menuScheme is not null)
        {
            _menuBar.SetScheme(menuScheme);
            _statusLabel.SetScheme(menuScheme);
            _topicLabel.SetScheme(menuScheme);
        }
    }

    /// <summary>
    /// Builds the menu bar with File, Server, User menus and a theme submenu.
    /// </summary>
    private MenuBar BuildMenuBar()
    {
        // Build theme menu items and prepend them with a separator header
        var themeItems = new List<MenuItem>();
        foreach (var t in Themes.ThemeManager.GetAvailableThemes())
        {
            var name = t.Name;
            themeItems.Add(new MenuItem(name, "", () => OnThemeSelected?.Invoke(name), Key.Empty));
        }

        // Combine user items with theme items, separated by a line
        var allUserItems = new List<View>
        {
            new MenuItem("_My Profile", "Open your profile panel", () => OnProfileRequested?.Invoke(), Key.Empty),
            new MenuItem("Set _Status...", "Set your status", () => OnStatusRequested?.Invoke(), Key.Empty),
            new Line()
        };
        allUserItems.AddRange(themeItems);

        var menuBar = new MenuBar(
        [
            new MenuBarItem("_File",
            [
                new MenuItem("_Quit", "Quit EchoHub", () => _app.RequestStop(), Key.Empty)
            ]),
            new MenuBarItem("_Server", new View[]
            {
                new MenuItem("_Connect...", "Connect to a server", () => OnConnectRequested?.Invoke(), Key.Empty),
                new MenuItem("_Disconnect", "Disconnect from server", () => OnDisconnectRequested?.Invoke(), Key.Empty),
                new Line(),
                new MenuItem("New C_hannel...", "Create a new channel", () => OnCreateChannelRequested?.Invoke(), Key.Empty),
                new MenuItem("_Saved Servers...", "View saved servers", () => OnSavedServersRequested?.Invoke(), Key.Empty)
            }),
            new MenuBarItem("_User", allUserItems)
        ]);
        menuBar.X = 0;
        menuBar.Y = 0;
        menuBar.Width = Dim.Fill();

        // Workaround: Make CommandView (title text area) mouse-transparent on each MenuBarItem.
        // Without this, clicks on the text hit the CommandView sub-view whose Source propagates
        // as a plain View — MenuBar.OnAccepting checks "sourceView is MenuBarItem" which fails.
        // Making CommandView transparent lets clicks pass through to the MenuBarItem itself.
        foreach (var mbi in menuBar.SubViews.OfType<MenuBarItem>())
        {
            mbi.CommandView.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;
        }

        return menuBar;
    }

    /// <summary>
    /// Rebuilds and replaces the menu bar (e.g., after theme list changes).
    /// </summary>
    public void RefreshMenuBar()
    {
        Remove(_menuBar);
        _menuBar = BuildMenuBar();
        Add(_menuBar);
        ApplyColorSchemes();
        SetNeedsDraw();
    }

    private void OnChannelListSelectionChanged(object? sender, ValueChangedEventArgs<int?> e)
    {
        var index = e.NewValue;
        if (index.HasValue && index.Value >= 0 && index.Value < _channelNames.Count)
        {
            var channelName = _channelNames[index.Value];
            if (channelName != _currentChannel)
            {
                SwitchToChannel(channelName);
                OnChannelSelected?.Invoke(channelName);
            }
        }
    }

    private void OnInputKeyDown(object? sender, Key e)
    {
        if (e.KeyCode == TabKey.KeyCode)
        {
            TryAutocompleteCommand();
            e.Handled = true;
        }
        else if (e.KeyCode == AltEnterKey.KeyCode)
        {
            _inputField.InsertText("\n");
            e.Handled = true;
        }
        else if (e.KeyCode == EnterKey.KeyCode)
        {
            var text = _inputField.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(_currentChannel))
            {
                OnMessageSubmitted?.Invoke(_currentChannel, text);
                _inputField.Text = string.Empty;
            }
            e.Handled = true;
        }
        else if (e.KeyCode == CtrlCKey.KeyCode)
        {
            _app.RequestStop();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Tab-complete slash commands in the input field.
    /// </summary>
    private void TryAutocompleteCommand()
    {
        var text = _inputField.Text ?? string.Empty;
        if (!text.StartsWith('/') || text.Contains(' '))
            return;

        var matches = SlashCommands
            .Where(c => c.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 1)
        {
            _inputField.Text = matches[0] + " ";
        }
        else if (matches.Count > 1)
        {
            // Complete to longest common prefix
            var prefix = matches[0];
            foreach (var m in matches.Skip(1))
            {
                var len = 0;
                while (len < prefix.Length && len < m.Length
                    && char.ToLowerInvariant(prefix[len]) == char.ToLowerInvariant(m[len]))
                    len++;
                prefix = prefix[..len];
            }
            if (prefix.Length > text.Length)
                _inputField.Text = prefix;
        }
    }

    private void OnChatViewportChanged()
    {
        var newWidth = _messageList.Viewport.Width;
        if (newWidth > 0 && newWidth != _lastChatWidth)
        {
            _lastChatWidth = newWidth;
            RefreshMessages();
        }
    }

    private void OnWindowKeyDown(object? sender, Key e)
    {
        // Ctrl+C quits from anywhere
        if (e.KeyCode == CtrlCKey.KeyCode)
        {
            _app.RequestStop();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Add a message to the specified channel's message list and refresh if it is the current channel.
    /// Tracks unread count for non-active channels.
    /// </summary>
    public void AddMessage(MessageDto message)
    {
        var lines = FormatMessage(message);
        if (!_channelMessages.TryGetValue(message.ChannelName, out var messages))
        {
            messages = [];
            _channelMessages[message.ChannelName] = messages;
        }

        foreach (var line in lines)
        {
            messages.Add(line);
        }

        if (message.ChannelName == _currentChannel)
        {
            RefreshMessages();
        }
        else
        {
            // Increment unread count for non-active channels
            _channelUnread.TryGetValue(message.ChannelName, out var count);
            _channelUnread[message.ChannelName] = count + 1;
            RefreshChannelList();
        }
    }

    /// <summary>
    /// Add a system/informational message to a channel with colored styling.
    /// </summary>
    public void AddSystemMessage(string channelName, string text)
    {
        var time = DateTimeOffset.Now.ToString("HH:mm");
        var segments = new List<ChatSegment>
        {
            new($"[{time}] ", ChatColors.TimestampAttr),
            new($"** {text}", ChatColors.SystemAttr)
        };

        if (!_channelMessages.TryGetValue(channelName, out var messages))
        {
            messages = [];
            _channelMessages[channelName] = messages;
        }
        messages.Add(new ChatLine(segments));

        if (channelName == _currentChannel)
        {
            RefreshMessages();
        }
    }

    /// <summary>
    /// Add a status change message to a channel with colored styling.
    /// </summary>
    public void AddStatusMessage(string channelName, string username, string status)
    {
        var time = DateTimeOffset.Now.ToString("HH:mm");
        var segments = new List<ChatSegment>
        {
            new($"[{time}] ", ChatColors.TimestampAttr),
            new($"** {username} is now {status}", ChatColors.SystemAttr)
        };

        if (!_channelMessages.TryGetValue(channelName, out var messages))
        {
            messages = [];
            _channelMessages[channelName] = messages;
        }
        messages.Add(new ChatLine(segments));

        if (channelName == _currentChannel)
        {
            RefreshMessages();
        }
    }

    /// <summary>
    /// Set the list of available channels, storing topics, and refresh the channel list view.
    /// </summary>
    public void SetChannels(List<ChannelDto> channels)
    {
        _channelNames.Clear();
        _channelTopics.Clear();
        foreach (var ch in channels)
        {
            _channelNames.Add(ch.Name);
            _channelTopics[ch.Name] = ch.Topic;
            if (!_channelMessages.ContainsKey(ch.Name))
                _channelMessages[ch.Name] = [];
        }
        RefreshChannelList();
    }

    /// <summary>
    /// Update the topic for a specific channel.
    /// </summary>
    public void SetChannelTopic(string channelName, string? topic)
    {
        _channelTopics[channelName] = topic;
        if (channelName == _currentChannel)
            UpdateTopicBar();
    }

    /// <summary>
    /// Show an error message to the user.
    /// </summary>
    public void ShowError(string message)
    {
        MessageBox.ErrorQuery(_app, "Error", message, "OK");
    }

    /// <summary>
    /// Update the connection status displayed in the status bar.
    /// </summary>
    public void UpdateStatusBar(string status)
    {
        var userPart = string.IsNullOrEmpty(_currentUser) ? "" : $" | User: {_currentUser}";
        var channelPart = string.IsNullOrEmpty(_currentChannel) ? "" : $" | #{_currentChannel}";
        _statusLabel.Text = $" {status}{userPart}{channelPart}";
    }

    /// <summary>
    /// Set the current user name for display in the status bar.
    /// </summary>
    public void SetCurrentUser(string username)
    {
        _currentUser = username;
    }

    /// <summary>
    /// Get the current channel name.
    /// </summary>
    public string CurrentChannel => _currentChannel;

    /// <summary>
    /// Get all channel names that have message buffers (for broadcasting status changes).
    /// </summary>
    public IReadOnlyList<string> GetChannelNames() => _channelNames.AsReadOnly();

    /// <summary>
    /// Switch the chat view to the given channel, resetting its unread count and updating the topic bar.
    /// </summary>
    public void SwitchToChannel(string channelName)
    {
        _currentChannel = channelName;
        _chatFrame.Title = $"#{channelName}";

        // Reset unread count for this channel
        _channelUnread[channelName] = 0;
        RefreshChannelList();

        RefreshMessages();
        UpdateTopicBar();

        // Update channel list selection
        var idx = _channelNames.IndexOf(channelName);
        if (idx >= 0)
            _channelList.SelectedItem = idx;
    }

    /// <summary>
    /// Load historical messages into a channel, replacing any existing messages.
    /// </summary>
    public void LoadHistory(string channelName, List<MessageDto> messages)
    {
        var formatted = messages.SelectMany(FormatMessage).ToList();
        _channelMessages[channelName] = formatted;

        if (channelName == _currentChannel)
        {
            RefreshMessages();
        }
    }


    /// <summary>
    /// Clear all messages and channels (used on disconnect).
    /// </summary>
    public void ClearAll()
    {
        _channelNames.Clear();
        _channelMessages.Clear();
        _channelUnread.Clear();
        _channelTopics.Clear();
        _currentChannel = string.Empty;
        _currentUser = string.Empty;
        _channelList.SetSource(new ObservableCollection<string>(_channelNames));
        _chatFrame.Title = "Chat";
        _topicLabel.Visible = false;
        _chatFrame.Y = 1;
        RefreshMessages();
    }

    /// <summary>
    /// Focus the input field for typing.
    /// </summary>
    public void FocusInput()
    {
        _inputField.SetFocus();
    }

    private void RefreshMessages()
    {
        if (_channelMessages.TryGetValue(_currentChannel, out var messages))
        {
            var width = _messageList.Viewport.Width;
            var source = new ChatListSource();

            if (width > 0)
            {
                foreach (var line in messages)
                    source.AddRange(line.Wrap(width));
            }
            else
            {
                source.AddRange(messages);
            }

            _messageList.Source = source;
            if (source.Count > 0)
                _messageList.SelectedItem = source.Count - 1;
        }
        else
        {
            _messageList.Source = new ChatListSource();
        }
    }

    /// <summary>
    /// Refresh the channel list view, showing unread counts next to channel names.
    /// </summary>
    private void RefreshChannelList()
    {
        var displayNames = _channelNames.Select(name =>
        {
            _channelUnread.TryGetValue(name, out var unread);
            return unread > 0 ? $"#{name} ({unread})" : $"#{name}";
        }).ToList();

        _channelList.SetSource(new ObservableCollection<string>(displayNames));

        // Restore selection to current channel
        var idx = _channelNames.IndexOf(_currentChannel);
        if (idx >= 0)
            _channelList.SelectedItem = idx;
    }

    /// <summary>
    /// Show or hide the topic bar based on the current channel's topic.
    /// </summary>
    private void UpdateTopicBar()
    {
        _channelTopics.TryGetValue(_currentChannel, out var topic);
        if (!string.IsNullOrWhiteSpace(topic))
        {
            _topicLabel.Text = $" Topic: {topic}";
            _topicLabel.Visible = true;
            _chatFrame.Y = 2;
        }
        else
        {
            _topicLabel.Visible = false;
            _chatFrame.Y = 1;
        }
    }

    /// <summary>
    /// Format a message DTO into one or more display lines based on its MessageType.
    /// Timestamps are dimmed and sender names are colored.
    /// </summary>
    private static List<ChatLine> FormatMessage(MessageDto message)
    {
        var time = message.SentAt.ToLocalTime().ToString("HH:mm");
        var senderName = message.SenderUsername + ":";
        var senderColor = ColorHelper.ParseHexColor(message.SenderNicknameColor);

        var lines = new List<ChatLine>();

        switch (message.Type)
        {
            case MessageType.Image:
                lines.Add(BuildChatLine(time, senderName, senderColor, " [Image]"));
                // Content IS the ASCII art — add each line as a separate list item
                if (!string.IsNullOrWhiteSpace(message.Content))
                {
                    foreach (var artLine in message.Content.Split('\n'))
                    {
                        // Parse ANSI color codes from colored ASCII art
                        var trimmed = artLine.TrimEnd('\r');
                        if (trimmed.Contains('\x1b'))
                            lines.Add(ChatLine.FromAnsi("       " + trimmed));
                        else
                            lines.Add(new ChatLine($"       {trimmed}"));
                    }
                }
                break;

            case MessageType.File:
                var fileName = message.AttachmentFileName ?? "unknown";
                var fileContent = !string.IsNullOrWhiteSpace(message.Content) ? $" {message.Content}" : "";
                lines.Add(BuildChatLine(time, senderName, senderColor, $" [File: {fileName}]{fileContent}"));
                break;

            case MessageType.Text:
            default:
                var contentLines = message.Content.Split('\n');
                var firstLine = contentLines[0].TrimEnd('\r');
                lines.Add(BuildChatLine(time, senderName, senderColor, $" {firstLine}"));
                // Continuation lines indented to align with first line's content
                var indent = new string(' ', $"[{time}] {senderName} ".Length);
                for (int i = 1; i < contentLines.Length; i++)
                {
                    lines.Add(new ChatLine($"{indent}{contentLines[i].TrimEnd('\r')}"));
                }
                break;
        }

        return lines;
    }

    /// <summary>
    /// Build a chat line with a dimmed timestamp and optionally colored sender name.
    /// </summary>
    private static ChatLine BuildChatLine(string time, string senderName, Attribute? senderColor, string suffix)
    {
        var segments = new List<ChatSegment>
        {
            new($"[{time}] ", ChatColors.TimestampAttr),
            new(senderName, senderColor),
            new(suffix, null)
        };
        return new ChatLine(segments);
    }
}
