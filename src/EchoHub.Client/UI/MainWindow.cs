using System.Text.RegularExpressions;
using EchoHub.Client.Themes;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.Text;
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
    private readonly FrameView _inputFrame;
    private readonly Label _statusLabel;
    private readonly Label _topicLabel;
    private MenuBar _menuBar;

    // Online users panel
    private readonly FrameView _usersFrame;
    private readonly ListView _usersList;
    private readonly UserListSource _usersListSource;
    private bool _usersPanelVisible = true;
    private const int UsersPanelWidth = 22;
    private static readonly Key F2Key = Key.F2;

    internal static readonly string AppVersion =
        typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "?";

    // Cached Key constants — compare via .KeyCode to avoid Key.Equals (which also checks Handled)
    private static readonly Key EnterKey = Key.Enter;
    private static readonly Key NewlineKey = Key.N.WithCtrl;
    private static readonly Key AltQKey = Key.Q.WithAlt;
    private static readonly Key TabKey = Key.Tab;

    // Available slash commands for Tab autocomplete
    private static readonly string[] SlashCommands =
    [
        "/status", "/nick", "/color", "/theme", "/send",
        "/avatar", "/profile", "/servers", "/join", "/leave",
        "/topic", "/users", "/kick", "/ban", "/unban",
        "/mute", "/unmute", "/role", "/nuke", "/test-sound", "/quit", "/help"
    ];

    private readonly List<string> _channelNames = [];
    private readonly Dictionary<string, List<ChatLine>> _channelMessages = [];
    private readonly Dictionary<string, int> _channelUnread = [];
    private readonly Dictionary<string, string?> _channelTopics = [];
    private readonly Dictionary<string, bool> _channelPublic = [];
    private readonly ChannelListSource _channelListSource;
    private string _currentChannel = string.Empty;
    private string _currentUser = string.Empty;
    private string _connectionStatus = "Disconnected";
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
    /// Fired when the user requests to logout (disconnect + revoke session).
    /// </summary>
    public event Action? OnLogoutRequested;

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

    /// <summary>
    /// Fired when the user requests to delete the current channel.
    /// </summary>
    public event Action? OnDeleteChannelRequested;

    /// <summary>
    /// Fired when the user activates (Enter/click) an audio message. Parameters: attachmentUrl, fileName.
    /// </summary>
    public event Action<string, string>? OnAudioPlayRequested;

    /// <summary>
    /// Fired when the user activates (Enter/click) a file message. Parameters: attachmentUrl, fileName.
    /// </summary>
    public event Action<string, string>? OnFileDownloadRequested;

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
            Width = 22,
            Height = Dim.Fill(1) // leave room for status bar
        };

        _channelList = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        _channelListSource = new ChannelListSource();
        _channelList.Source = _channelListSource;
        _channelList.ValueChanged += OnChannelListSelectionChanged;
        channelsFrame.Add(_channelList);
        Add(channelsFrame);

        // Topic bar — sits above the chat frame in the right column
        _topicLabel = new Label
        {
            Text = "",
            X = 22,
            Y = 1,
            Width = Dim.Fill(UsersPanelWidth),
            Height = 1,
            Visible = false
        };
        Add(_topicLabel);

        // Center panel - messages
        _chatFrame = new FrameView
        {
            Title = "Chat",
            X = 22,
            Y = 1, // below menu bar (shifts to 2 when topic is visible)
            Width = Dim.Fill(UsersPanelWidth),
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
        _messageList.Accepting += OnMessageListAccepting;
        _chatFrame.Add(_messageList);
        Add(_chatFrame);

        // Bottom input area
        _inputFrame = new FrameView
        {
            Title = "Message \u2502 Enter=send \u2502 Ctrl+N=newline \u2502 Tab=complete",
            X = 22,
            Y = Pos.Bottom(_chatFrame),
            Width = Dim.Fill(UsersPanelWidth),
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
        _inputField.ContentsChanged += OnInputContentsChanged;
        _inputFrame.Add(_inputField);
        Add(_inputFrame);

        // Right panel - online users
        _usersFrame = new FrameView
        {
            Title = "Users",
            X = Pos.AnchorEnd(UsersPanelWidth),
            Y = 1,
            Width = UsersPanelWidth,
            Height = Dim.Fill(1)
        };

        _usersList = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        _usersListSource = new UserListSource();
        _usersList.Source = _usersListSource;
        _usersFrame.Add(_usersList);
        Add(_usersFrame);

        // Status bar at the very bottom — custom drawing for colored connection state
        _statusLabel = new Label
        {
            Text = "",
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1
        };
        _statusLabel.SetScheme(SchemeManager.GetScheme("Menu"));
        _statusLabel.DrawingContent += OnStatusBarDrawContent;
        Add(_statusLabel);

        // Apply our custom color schemes to all views
        ApplyColorSchemes();

        // Re-wrap messages when the chat area is resized
        // Subscribe to both ListView and FrameView viewport changes for reliable resize detection
        _messageList.ViewportChanged += (_, _) => OnChatViewportChanged();
        _chatFrame.ViewportChanged += (_, _) => OnChatViewportChanged();

        // Window-level key handling for Alt+Q (quit), F2 (toggle users panel)
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
                new MenuItem("_Logout", "Logout and clear session", () => OnLogoutRequested?.Invoke(), Key.Empty),
                new Line(),
                new MenuItem("New C_hannel...", "Create a new channel", () => OnCreateChannelRequested?.Invoke(), Key.Empty),
                new MenuItem("_Delete Channel", "Delete the current channel", () => OnDeleteChannelRequested?.Invoke(), Key.Empty),
                new Line(),
                new MenuItem("_Saved Servers...", "View saved servers", () => OnSavedServersRequested?.Invoke(), Key.Empty),
                new Line(),
                new MenuItem("Toggle _Users Panel", "Toggle online users (F2)", () => ToggleUsersPanel(), Key.Empty)
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

    private void OnMessageListAccepting(object? sender, CommandEventArgs e)
    {
        if (_messageList.Source is not ChatListSource source)
            return;

        var index = _messageList.SelectedItem;
        if (!index.HasValue || index.Value < 0 || index.Value >= source.Count)
            return;

        var line = source.GetLine(index.Value);
        if (line?.AttachmentUrl is null || line.AttachmentFileName is null)
            return;

        if (line.Type == MessageType.Audio)
        {
            OnAudioPlayRequested?.Invoke(line.AttachmentUrl, line.AttachmentFileName);
            e.Handled = true;
        }
        else if (line.Type == MessageType.File)
        {
            OnFileDownloadRequested?.Invoke(line.AttachmentUrl, line.AttachmentFileName);
            e.Handled = true;
        }
    }

    private void OnInputKeyDown(object? sender, Key e)
    {
        if (e.KeyCode == TabKey.KeyCode)
        {
            TryAutocompleteCommand();
            e.Handled = true;
        }
        else if (e.KeyCode == NewlineKey.KeyCode)
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
        else if (e.KeyCode == AltQKey.KeyCode)
        {
            _app.RequestStop();
            e.Handled = true;
        }
    }

    private bool _suppressEmojiReplace;

    private void OnInputContentsChanged(object? sender, ContentsChangedEventArgs e)
    {
        if (_suppressEmojiReplace)
            return;

        var text = _inputField.Text;
        if (string.IsNullOrEmpty(text))
            return;

        var replaced = EmojiHelper.ReplaceEmoji(text);
        if (replaced == text)
            return;

        // Calculate where cursor should land after replacement
        var lengthDelta = replaced.Length - text.Length;
        var newCol = Math.Max(0, _inputField.CurrentColumn + lengthDelta);

        _suppressEmojiReplace = true;
        _inputField.Text = replaced;
        _inputField.InsertionPoint = new System.Drawing.Point(newCol, _inputField.CurrentRow);
        _suppressEmojiReplace = false;
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
        if (e.KeyCode == AltQKey.KeyCode)
        {
            _app.RequestStop();
            e.Handled = true;
        }
        else if (e.KeyCode == F2Key.KeyCode)
        {
            ToggleUsersPanel();
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
        if (!_channelMessages.TryGetValue(channelName, out var messages))
        {
            messages = [];
            _channelMessages[channelName] = messages;
        }

        var time = DateTimeOffset.Now.ToString("HH:mm");
        var textLines = text.Split('\n');

        // First line gets timestamp prefix
        messages.Add(new ChatLine(
        [
            new($"[{time}] ", ChatColors.TimestampAttr),
            new($"** {textLines[0].TrimEnd('\r')}", ChatColors.SystemAttr)
        ]));

        // Continuation lines are indented to align
        var indent = new string(' ', $"[{time}] ** ".Length);
        for (int i = 1; i < textLines.Length; i++)
        {
            var line = textLines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;
            messages.Add(new ChatLine(
            [
                new($"{indent}{line}", ChatColors.SystemAttr)
            ]));
        }

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
    /// Remove all lines associated with a specific message ID.
    /// </summary>
    public void RemoveMessage(string channelName, Guid messageId)
    {
        if (_channelMessages.TryGetValue(channelName, out var messages))
        {
            messages.RemoveAll(l => l.MessageId == messageId);
            if (channelName == _currentChannel)
                RefreshMessages();
        }
    }

    /// <summary>
    /// Clear all messages from a specific channel.
    /// </summary>
    public void ClearChannelMessages(string channelName)
    {
        if (_channelMessages.TryGetValue(channelName, out var messages))
        {
            messages.Clear();
            if (channelName == _currentChannel)
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
        _channelPublic.Clear();
        foreach (var ch in channels)
        {
            _channelNames.Add(ch.Name);
            _channelTopics[ch.Name] = ch.Topic;
            _channelPublic[ch.Name] = ch.IsPublic;
            if (!_channelMessages.ContainsKey(ch.Name))
                _channelMessages[ch.Name] = [];
        }
        RefreshChannelList();
    }

    /// <summary>
    /// Ensure a channel exists in the left panel list (used for private channels joined via /join).
    /// </summary>
    public void EnsureChannelInList(string channelName, bool? isPublic = null)
    {
        if (isPublic.HasValue)
            _channelPublic[channelName] = isPublic.Value;

        if (_channelNames.Contains(channelName))
            return;

        _channelNames.Add(channelName);
        if (!_channelMessages.ContainsKey(channelName))
            _channelMessages[channelName] = [];
        RefreshChannelList();
    }

    /// <summary>
    /// Remove a channel from the left panel list.
    /// </summary>
    public void RemoveChannel(string channelName)
    {
        _channelNames.Remove(channelName);
        _channelTopics.Remove(channelName);
        _channelPublic.Remove(channelName);
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
        _connectionStatus = status;
        _statusLabel.SetNeedsDraw();
    }

    private static readonly Attribute StatusConnectedAttr = new(new Color(0, 200, 0), Color.Transparent);
    private static readonly Attribute StatusDisconnectedAttr = new(new Color(220, 50, 50), Color.Transparent);
    private static readonly Attribute StatusTransitionalAttr = new(new Color(220, 180, 0), Color.Transparent);
    private static readonly Attribute StatusBrandAttr = new(new Color(218, 165, 32), Color.Transparent);

    private void OnStatusBarDrawContent(object? sender, DrawEventArgs e)
    {
        var menuScheme = SchemeManager.GetScheme("Menu");
        var normalAttr = menuScheme?.Normal ?? _statusLabel.GetAttributeForRole(VisualRole.Normal);
        var width = _statusLabel.Viewport.Width;
        if (width <= 0) return;

        // Resolve transparent background for colored segments
        var bg = normalAttr.Background;
        Attribute Resolve(Attribute a) => a.Background == Color.Transparent ? a with { Background = bg } : a;

        int col = 0;

        void Write(string text, Attribute attr)
        {
            _statusLabel.SetAttribute(Resolve(attr));
            foreach (var g in GraphemeHelper.GetGraphemes(text))
            {
                var cols = Math.Max(g.GetColumns(), 1);
                if (col + cols > width) return;
                _statusLabel.Move(col, 0);
                _statusLabel.AddStr(g);
                col += cols;
            }
        }

        // EchoHub branding
        Write(" EchoHub", Resolve(StatusBrandAttr));
        Write($" \u2502 v{AppVersion} \u2502 ", normalAttr);

        // Connection state with color
        var statusAttr = _connectionStatus switch
        {
            "Connected" => StatusConnectedAttr,
            "Disconnected" => StatusDisconnectedAttr,
            _ => StatusTransitionalAttr // Connecting, Reconnecting, Authenticating, etc.
        };
        Write(_connectionStatus, Resolve(statusAttr));

        // User
        if (!string.IsNullOrEmpty(_currentUser))
            Write($" \u2502 User: {_currentUser}", normalAttr);

        // Channel + type
        if (!string.IsNullOrEmpty(_currentChannel))
        {
            _channelPublic.TryGetValue(_currentChannel, out var isPublic);
            var typeSuffix = isPublic ? "public" : "private";
            Write($" \u2502 #{_currentChannel} - {typeSuffix}", normalAttr);
        }

        // Fill remaining space
        _statusLabel.SetAttribute(normalAttr);
        while (col < width)
        {
            _statusLabel.Move(col, 0);
            _statusLabel.AddStr(" ");
            col++;
        }

        e.Cancel = true;
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
        _statusLabel.SetNeedsDraw();

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
        _channelPublic.Clear();
        _currentChannel = string.Empty;
        _currentUser = string.Empty;
        _channelListSource.Update([], [], string.Empty);
        _channelList.Source = _channelListSource;
        _chatFrame.Title = "Chat";
        _topicLabel.Visible = false;
        _chatFrame.Y = 1;
        _usersListSource.Update([]);
        _usersList.Source = _usersListSource;
        _usersFrame.Title = "Users";
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

            // Update cached width when viewport reports a valid value;
            // fall back to last known width if viewport hasn't been laid out yet.
            if (width > 0)
                _lastChatWidth = width;
            else
                width = _lastChatWidth;

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
        _channelListSource.Update(_channelNames, _channelUnread, _currentChannel);
        _channelList.Source = _channelListSource;

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
    /// Adjusts widths of chat, topic, and input frames based on users panel visibility.
    /// </summary>
    private void UpdateLayout()
    {
        var rightMargin = _usersPanelVisible ? UsersPanelWidth : 0;
        _chatFrame.Width = Dim.Fill(rightMargin);
        _topicLabel.Width = Dim.Fill(rightMargin);
        _inputFrame.Width = Dim.Fill(rightMargin);
        _usersFrame.Visible = _usersPanelVisible;
        SetNeedsDraw();
    }

    /// <summary>
    /// Toggle the online users panel visibility (F2).
    /// </summary>
    public void ToggleUsersPanel()
    {
        _usersPanelVisible = !_usersPanelVisible;
        UpdateLayout();
    }

    /// <summary>
    /// Update the online users list display.
    /// </summary>
    public void UpdateOnlineUsers(List<UserPresenceDto> users)
    {
        var displayItems = users.Select(u =>
        {
            var statusIcon = u.Status switch
            {
                UserStatus.Online => "\u25cf", // ●
                UserStatus.Away => "\u25cb",   // ○
                UserStatus.DoNotDisturb => "\u25d0", // ◐
                UserStatus.Invisible => "\u25cc",    // ◌
                _ => " "
            };
            var name = u.DisplayName ?? u.Username;
            var roleTag = u.Role switch
            {
                ServerRole.Owner => "\u2605", // ★
                ServerRole.Admin => "\u2666", // ♦
                ServerRole.Mod => "\u2740",   // ❀
                _ => ""
            };
            var text = $"{statusIcon} {roleTag}{name}";
            var nameColor = ColorHelper.ParseHexColor(u.NicknameColor);
            return (text, nameColor);
        }).ToList();

        _usersListSource.Update(displayItems);
        _usersList.Source = _usersListSource;
        _usersFrame.Title = $"Users ({users.Count})";
    }

    /// <summary>
    /// Format a message DTO into one or more display lines based on its MessageType.
    /// Timestamps are dimmed and sender names are colored.
    /// </summary>
    private List<ChatLine> FormatMessage(MessageDto message)
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
                        // Parse color tags from colored ASCII art
                        var trimmed = artLine.TrimEnd('\r');
                        if (ChatLine.HasColorTags(trimmed))
                            lines.Add(ChatLine.FromColoredText("       " + trimmed));
                        else
                            lines.Add(new ChatLine($"       {trimmed}"));
                    }
                }
                break;

            case MessageType.Audio:
                var audioName = message.AttachmentFileName ?? "unknown";
                var audioSize = FormatFileSize(message.AttachmentFileSize);
                var audioLine = BuildChatLineColored(time, senderName, senderColor,
                    $" \u266a [Audio: {audioName}] [{audioSize}]", ChatColors.AudioAttr);
                audioLine.AttachmentUrl = message.AttachmentUrl;
                audioLine.AttachmentFileName = audioName;
                audioLine.Type = MessageType.Audio;
                lines.Add(audioLine);
                break;

            case MessageType.File:
                var fileName = message.AttachmentFileName ?? "unknown";
                var fileSize = FormatFileSize(message.AttachmentFileSize);
                var fileLine = BuildChatLineColored(time, senderName, senderColor,
                    $" [File: {fileName}] [{fileSize}]", ChatColors.FileAttr);
                fileLine.AttachmentUrl = message.AttachmentUrl;
                fileLine.AttachmentFileName = fileName;
                fileLine.Type = MessageType.File;
                lines.Add(fileLine);
                break;

            case MessageType.Text:
            default:
                var displayContent = EmojiHelper.ReplaceEmoji(message.Content);
                var contentLines = displayContent.Split('\n');
                var firstLine = contentLines[0].TrimEnd('\r');
                lines.Add(BuildChatLineWithMentions(time, senderName, senderColor, $" {firstLine}"));
                // Continuation lines indented to align with first line's content
                var indent = new string(' ', $"[{time}] {senderName} ".Length);
                for (int i = 1; i < contentLines.Length; i++)
                {
                    var contText = $"{indent}{contentLines[i].TrimEnd('\r')}";
                    lines.Add(new ChatLine(ChatColors.SplitMentions(contText)));
                }

                // Render link embeds if present
                if (message.Embeds is { Count: > 0 })
                {
                    var chatWidth = _lastChatWidth > 0 ? _lastChatWidth : 80;
                    foreach (var embed in message.Embeds)
                        lines.AddRange(FormatEmbed(embed, indent, chatWidth));
                }
                break;
        }

        // Tag all lines with the message ID for deletion support
        foreach (var line in lines)
            line.MessageId = message.Id;

        // Check for @mention of current user
        if (!string.IsNullOrEmpty(_currentUser) && message.Type == MessageType.Text)
        {
            var pattern = $@"@{Regex.Escape(_currentUser)}\b";
            if (Regex.IsMatch(message.Content, pattern, RegexOptions.IgnoreCase))
            {
                foreach (var line in lines)
                    line.IsMention = true;
            }
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

    /// <summary>
    /// Build a chat line with a colored suffix (used for audio/file indicators).
    /// </summary>
    private static ChatLine BuildChatLineColored(string time, string senderName, Attribute? senderColor, string suffix, Attribute suffixColor)
    {
        var segments = new List<ChatSegment>
        {
            new($"[{time}] ", ChatColors.TimestampAttr),
            new(senderName, senderColor),
            new(suffix, suffixColor)
        };
        return new ChatLine(segments);
    }

    /// <summary>
    /// Build a chat line with @mention highlighting in the suffix text.
    /// </summary>
    private static ChatLine BuildChatLineWithMentions(string time, string senderName, Attribute? senderColor, string suffix)
    {
        var segments = new List<ChatSegment>
        {
            new($"[{time}] ", ChatColors.TimestampAttr),
            new(senderName, senderColor),
        };
        segments.AddRange(ChatColors.SplitMentions(suffix));
        return new ChatLine(segments);
    }

    /// <summary>
    /// Format a link embed as indented chat lines with a left border bar,
    /// text at full width, and optional icon below (preview image).
    /// Each line is pre-wrapped to fit chatWidth so ChatLine.Wrap won't break layout.
    /// </summary>
    private static List<ChatLine> FormatEmbed(EmbedDto embed, string indent, int chatWidth)
    {
        var lines = new List<ChatLine>();
        const string border = "\u258f "; // ▏ + space
        const int borderCols = 2;
        int indentCols = indent.GetColumns();
        int textWidth = chatWidth - indentCols - borderCols;
        if (textWidth < 20) textWidth = 20;

        // Helper: create a bordered text line
        void AddTextLine(string text, Attribute? color)
        {
            lines.Add(new ChatLine(
            [
                new ChatSegment(indent, null),
                new ChatSegment(border, ChatColors.EmbedBorderAttr),
                new ChatSegment(text, color)
            ]));
        }

        // Site name
        if (!string.IsNullOrWhiteSpace(embed.SiteName))
            AddTextLine(embed.SiteName, ChatColors.EmbedBorderAttr);

        // Title
        if (!string.IsNullOrWhiteSpace(embed.Title))
        {
            foreach (var wrapped in WordWrap(embed.Title, textWidth))
                AddTextLine(wrapped, ChatColors.EmbedTitleAttr);
        }

        // Description (word-wrapped at full available width)
        if (!string.IsNullOrWhiteSpace(embed.Description))
        {
            foreach (var wrapped in WordWrap(embed.Description, textWidth))
                AddTextLine(wrapped, ChatColors.EmbedDescAttr);
        }

        return lines;
    }

    /// <summary>
    /// Simple word-wrap: splits text into lines that fit within maxCols display columns.
    /// </summary>
    private static List<string> WordWrap(string text, int maxCols)
    {
        if (maxCols <= 0)
            return [text];

        var result = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = "";

        foreach (var word in words)
        {
            var candidate = currentLine.Length == 0 ? word : currentLine + " " + word;
            if (candidate.GetColumns() <= maxCols)
            {
                currentLine = candidate;
            }
            else
            {
                if (currentLine.Length > 0)
                    result.Add(currentLine);
                // If a single word exceeds maxCols, just add it as-is
                currentLine = word;
            }
        }

        if (currentLine.Length > 0)
            result.Add(currentLine);

        return result;
    }

    private static string FormatFileSize(long? bytes)
    {
        if (bytes is null or 0)
            return "?";

        return bytes.Value switch
        {
            < 1024 => $"{bytes.Value} B",
            < 1024 * 1024 => $"{bytes.Value / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes.Value / (1024.0 * 1024.0):F1} MB",
            _ => $"{bytes.Value / (1024.0 * 1024.0 * 1024.0):F1} GB"
        };
    }
}
