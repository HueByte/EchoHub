using EchoHub.Client.Commands;
using EchoHub.Client.Config;
using EchoHub.Client.Services;
using EchoHub.Client.Themes;
using EchoHub.Client.UI;
using EchoHub.Core.Constants;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using Serilog;
using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace EchoHub.Client;

/// <summary>
/// Central orchestrator for the EchoHub TUI client.
/// Owns session state and wires UI events to service calls.
/// </summary>
public sealed class AppOrchestrator : IDisposable
{
    private readonly IApplication _app;
    private readonly MainWindow _mainWindow;
    private readonly CommandHandler _commandHandler;

    private EchoHubConnection? _connection;
    private ApiClient? _apiClient;
    private ClientConfig _config;
    private UserStatus _currentStatus = UserStatus.Online;
    private string? _currentStatusMessage;
    private string _currentUsername = string.Empty;
    private readonly HashSet<string> _joinedChannels = [];

    private bool IsConnected => _connection is not null && _connection.IsConnected;
    private bool IsAuthenticated => _apiClient is not null;

    public MainWindow MainWindow => _mainWindow;

    public AppOrchestrator(IApplication app, ClientConfig config)
    {
        _app = app;
        _config = config;
        _mainWindow = new MainWindow(app);
        _commandHandler = new CommandHandler();

        WireMainWindowEvents();
        WireCommandHandlerEvents();

        _mainWindow.UpdateStatusBar("Disconnected");
    }

    public void Dispose()
    {
        _connection?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _apiClient?.Dispose();
    }

    // ── Convenience Helpers ────────────────────────────────────────────────

    private void RunAsync(Func<Task> work, string errorPrefix, string? logContext = null)
    {
        AsyncRunner.Run(_app, work, _mainWindow.ShowError, errorPrefix, logContext);
    }

    private void InvokeUI(Action action) => _app.Invoke(action);

    // ── MainWindow Event Wiring ────────────────────────────────────────────

    private void WireMainWindowEvents()
    {
        _mainWindow.OnConnectRequested += HandleConnect;
        _mainWindow.OnDisconnectRequested += HandleDisconnect;
        _mainWindow.OnMessageSubmitted += HandleMessageSubmitted;
        _mainWindow.OnChannelSelected += HandleChannelSelected;
        _mainWindow.OnProfileRequested += HandleProfileRequested;
        _mainWindow.OnStatusRequested += HandleStatusRequested;
        _mainWindow.OnThemeSelected += HandleThemeSelected;
        _mainWindow.OnSavedServersRequested += HandleSavedServersRequested;
        _mainWindow.OnCreateChannelRequested += HandleCreateChannelRequested;
        _mainWindow.OnDeleteChannelRequested += HandleDeleteChannelRequested;
    }

    // ── Command Handler Wiring ─────────────────────────────────────────────

    private void WireCommandHandlerEvents()
    {
        _commandHandler.OnSetStatus += async (status, message) =>
        {
            if (!IsConnected) return;

            await _connection!.UpdateStatusAsync(status, message);
            _currentStatus = status;
            _currentStatusMessage = message;
        };

        _commandHandler.OnSetNick += async (displayName) =>
        {
            if (!IsAuthenticated) return;

            await _apiClient!.UpdateProfileAsync(new UpdateProfileRequest(DisplayName: displayName));
            InvokeUI(() =>
            {
                _mainWindow.SetCurrentUser(displayName);
                _mainWindow.UpdateStatusBar("Connected");
            });
        };

        _commandHandler.OnSetColor += async (color) =>
        {
            if (!IsAuthenticated) return;

            await _apiClient!.UpdateProfileAsync(new UpdateProfileRequest(NicknameColor: color));
        };

        _commandHandler.OnSetTheme += (name) =>
        {
            InvokeUI(() => HandleThemeSelected(name));
            return Task.CompletedTask;
        };

        _commandHandler.OnSendFile += async (target) =>
        {
            if (!IsAuthenticated || !IsConnected) return;

            var channel = _mainWindow.CurrentChannel;
            if (string.IsNullOrEmpty(channel)) return;

            try
            {
                if (Uri.TryCreate(target, UriKind.Absolute, out var uri)
                    && (uri.Scheme == "http" || uri.Scheme == "https"))
                {
                    await _apiClient!.SendUrlAsync(channel, target);
                }
                else
                {
                    await using var stream = File.OpenRead(target);
                    var fileName = Path.GetFileName(target);
                    await _apiClient!.UploadFileAsync(channel, stream, fileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "File send failed for {Target}", target);
                InvokeUI(() => _mainWindow.ShowError($"Send failed: {ex.Message}"));
            }
        };

        _commandHandler.OnSetAvatar += async (target) =>
        {
            if (!IsAuthenticated) return;

            try
            {
                Stream stream;
                string fileName;

                if (Uri.TryCreate(target, UriKind.Absolute, out var uri)
                    && (uri.Scheme == "http" || uri.Scheme == "https"))
                {
                    using var http = new HttpClient();
                    var bytes = await http.GetByteArrayAsync(uri);
                    stream = new MemoryStream(bytes);
                    fileName = Path.GetFileName(uri.LocalPath);
                    if (string.IsNullOrWhiteSpace(fileName) || !fileName.Contains('.'))
                        fileName = "avatar.png";
                }
                else
                {
                    if (!File.Exists(target))
                    {
                        InvokeUI(() => _mainWindow.ShowError($"File not found: {target}"));
                        return;
                    }
                    stream = File.OpenRead(target);
                    fileName = Path.GetFileName(target);
                }

                await using (stream)
                {
                    var ascii = await _apiClient!.UploadAvatarAsync(stream, fileName);
                    var channel = _mainWindow.CurrentChannel;
                    if (!string.IsNullOrEmpty(channel))
                        InvokeUI(() => _mainWindow.AddSystemMessage(channel, "Avatar updated."));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Avatar upload failed for {Target}", target);
                InvokeUI(() => _mainWindow.ShowError($"Avatar upload failed: {ex.Message}"));
            }
        };

        _commandHandler.OnOpenProfile += (username) =>
        {
            InvokeUI(() => HandleViewProfile(username));
            return Task.CompletedTask;
        };

        _commandHandler.OnOpenServers += () =>
        {
            InvokeUI(HandleSavedServersRequested);
            return Task.CompletedTask;
        };

        _commandHandler.OnJoinChannel += async (channelName) =>
        {
            if (!IsConnected) return;

            try
            {
                _joinedChannels.Add(channelName);
                var history = await _connection!.JoinChannelAsync(channelName);
                InvokeUI(() =>
                {
                    _mainWindow.SwitchToChannel(channelName);
                    if (history.Count > 0)
                        _mainWindow.LoadHistory(channelName, history);
                });
            }
            catch (Exception ex)
            {
                InvokeUI(() => _mainWindow.ShowError($"Failed to join channel: {ex.Message}"));
            }
        };

        _commandHandler.OnLeaveChannel += async () =>
        {
            if (!IsConnected) return;

            var channel = _mainWindow.CurrentChannel;
            if (string.IsNullOrEmpty(channel)) return;

            try
            {
                await _connection!.LeaveChannelAsync(channel);
                _joinedChannels.Remove(channel);
                InvokeUI(() => _mainWindow.AddSystemMessage(channel, $"You left #{channel}"));
            }
            catch (Exception ex)
            {
                InvokeUI(() => _mainWindow.ShowError($"Failed to leave channel: {ex.Message}"));
            }
        };

        _commandHandler.OnSetTopic += async (topic) =>
        {
            if (!IsAuthenticated) return;

            var channel = _mainWindow.CurrentChannel;
            if (string.IsNullOrEmpty(channel)) return;

            try
            {
                await _apiClient!.UpdateChannelTopicAsync(channel, topic);
                InvokeUI(() =>
                {
                    _mainWindow.SetChannelTopic(channel, topic);
                    _mainWindow.AddSystemMessage(channel, $"Topic set to: {topic}");
                });
            }
            catch (Exception ex)
            {
                InvokeUI(() => _mainWindow.ShowError($"Failed to set topic: {ex.Message}"));
            }
        };

        _commandHandler.OnListUsers += async () =>
        {
            if (!IsConnected) return;

            var channel = _mainWindow.CurrentChannel;
            if (string.IsNullOrEmpty(channel)) return;

            try
            {
                var users = await _connection!.GetOnlineUsersAsync(channel);
                InvokeUI(() =>
                {
                    _mainWindow.AddSystemMessage(channel, $"Online users in #{channel}:");
                    foreach (var user in users)
                    {
                        var displayName = user.DisplayName ?? user.Username;
                        var statusText = user.Status.ToString();
                        if (!string.IsNullOrWhiteSpace(user.StatusMessage))
                            statusText += $" - {user.StatusMessage}";
                        _mainWindow.AddSystemMessage(channel, $"  {displayName} ({statusText})");
                    }
                });
            }
            catch (Exception ex)
            {
                InvokeUI(() => _mainWindow.ShowError($"Failed to list users: {ex.Message}"));
            }
        };

        _commandHandler.OnKickUser += async (username, reason) =>
        {
            if (!IsAuthenticated) return;
            await _apiClient!.KickUserAsync(username, reason);
        };

        _commandHandler.OnBanUser += async (username, reason) =>
        {
            if (!IsAuthenticated) return;
            await _apiClient!.BanUserAsync(username, reason);
        };

        _commandHandler.OnUnbanUser += async (username) =>
        {
            if (!IsAuthenticated) return;
            await _apiClient!.UnbanUserAsync(username);
        };

        _commandHandler.OnMuteUser += async (username, duration) =>
        {
            if (!IsAuthenticated) return;
            await _apiClient!.MuteUserAsync(username, duration);
        };

        _commandHandler.OnUnmuteUser += async (username) =>
        {
            if (!IsAuthenticated) return;
            await _apiClient!.UnmuteUserAsync(username);
        };

        _commandHandler.OnAssignRole += async (username, roleStr) =>
        {
            if (!IsAuthenticated) return;
            var role = roleStr switch
            {
                "admin" => ServerRole.Admin,
                "mod" => ServerRole.Mod,
                _ => ServerRole.Member,
            };
            await _apiClient!.AssignRoleAsync(username, role);
        };

        _commandHandler.OnNukeChannel += async () =>
        {
            if (!IsAuthenticated) return;
            var channel = _mainWindow.CurrentChannel;
            if (string.IsNullOrEmpty(channel)) return;
            await _apiClient!.NukeChannelAsync(channel);
        };

        _commandHandler.OnQuit += () =>
        {
            InvokeUI(() => _app.RequestStop());
            return Task.CompletedTask;
        };
    }

    // ── MainWindow Event Handlers ──────────────────────────────────────────

    private void HandleConnect()
    {
        var result = ConnectDialog.Show(_app, _config.SavedServers);
        if (result is null) return;

        Log.Information("Connecting to {Url} as {User} (register={IsRegister})",
            result.ServerUrl, result.Username, result.IsRegister);

        RunAsync(async () =>
        {
            _apiClient?.Dispose();
            _apiClient = new ApiClient(result.ServerUrl);

            InvokeUI(() => _mainWindow.UpdateStatusBar("Authenticating..."));

            var loginResponse = result.IsRegister
                ? await _apiClient.RegisterAsync(result.Username, result.Password)
                : await _apiClient.LoginAsync(result.Username, result.Password);

            _currentUsername = loginResponse.Username;

            InvokeUI(() =>
            {
                _mainWindow.SetCurrentUser(loginResponse.DisplayName ?? loginResponse.Username);
                _mainWindow.UpdateStatusBar("Authenticated, connecting...");
            });

            if (_connection is not null)
                await _connection.DisposeAsync();

            _connection = new EchoHubConnection(result.ServerUrl, _apiClient);
            WireConnectionEvents(_connection);
            await _connection.ConnectAsync();

            var channels = await _apiClient.GetChannelsAsync();
            InvokeUI(() =>
            {
                _mainWindow.SetChannels(channels);
                _mainWindow.UpdateStatusBar("Connected");
            });

            _joinedChannels.Clear();
            _joinedChannels.Add(HubConstants.DefaultChannel);
            await _connection.JoinChannelAsync(HubConstants.DefaultChannel);
            InvokeUI(() => _mainWindow.SwitchToChannel(HubConstants.DefaultChannel));

            try
            {
                var history = await _connection.GetHistoryAsync(HubConstants.DefaultChannel);
                InvokeUI(() =>
                {
                    _mainWindow.LoadHistory(HubConstants.DefaultChannel, history);
                    _mainWindow.FocusInput();
                });
            }
            catch
            {
                // History might not be available
            }

            FetchAndUpdateOnlineUsers();
            SaveServerToConfig(result);

            // Check for newer version in the background
            _ = Task.Run(async () =>
            {
                var newVersion = await UpdateChecker.CheckForUpdateAsync();
                if (newVersion is not null)
                {
                    InvokeUI(() => _mainWindow.AddSystemMessage(
                        HubConstants.DefaultChannel,
                        $"A new version of EchoHub is available: v{newVersion} (current: v{MainWindow.AppVersion}). Visit https://github.com/HueByte/EchoHub/releases"));
                }
            });
        }, "Connection failed", "Connect");
    }

    private void HandleDisconnect()
    {
        Log.Information("Disconnecting from server");

        RunAsync(async () =>
        {
            if (_connection is not null)
            {
                await _connection.DisconnectAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }

            _apiClient?.Dispose();
            _apiClient = null;
            _joinedChannels.Clear();

            InvokeUI(() =>
            {
                _mainWindow.ClearAll();
                _mainWindow.UpdateStatusBar("Disconnected");
            });
        }, "Disconnect error", "Disconnect");
    }

    private void HandleMessageSubmitted(string channelName, string content)
    {
        if (!IsConnected)
        {
            _mainWindow.ShowError("Not connected to a server.");
            return;
        }

        if (_commandHandler.IsCommand(content))
        {
            RunAsync(async () =>
            {
                var result = await _commandHandler.HandleAsync(content);
                if (result.Message is not null)
                {
                    InvokeUI(() =>
                    {
                        if (result.IsError)
                            _mainWindow.ShowError(result.Message);
                        else
                            _mainWindow.AddSystemMessage(channelName, result.Message);
                    });
                }
            }, "Command failed");
            return;
        }

        RunAsync(
            async () => await _connection!.SendMessageAsync(channelName, content),
            "Send failed");
    }

    private void HandleChannelSelected(string channelName)
    {
        if (!IsConnected) return;

        RunAsync(async () =>
        {
            if (_joinedChannels.Add(channelName))
                await _connection!.JoinChannelAsync(channelName);

            try
            {
                var history = await _connection!.GetHistoryAsync(channelName);
                InvokeUI(() => _mainWindow.LoadHistory(channelName, history));
            }
            catch
            {
                // History might not be available
            }

            FetchAndUpdateOnlineUsers();
        }, "Failed to join channel");
    }

    private void HandleProfileRequested()
    {
        HandleViewProfile(null);
    }

    private void HandleViewProfile(string? username)
    {
        // If no username or it's our own, show the full user panel
        var isOwnProfile = string.IsNullOrWhiteSpace(username)
            || username.Equals(_currentUsername, StringComparison.OrdinalIgnoreCase);

        Task.Run(async () =>
        {
            UserProfileDto? profile = null;
            try
            {
                if (IsAuthenticated)
                {
                    var target = isOwnProfile ? _currentUsername : username!;
                    if (!string.IsNullOrEmpty(target))
                        profile = await _apiClient!.GetUserProfileAsync(target);
                }
            }
            catch (Exception ex)
            {
                InvokeUI(() => _mainWindow.ShowError($"Failed to load profile: {ex.Message}"));
                return;
            }

            InvokeUI(() =>
            {
                if (isOwnProfile)
                {
                    var action = ProfileViewDialog.ShowOwn(_app,
                        profile,
                        _currentStatus,
                        _currentStatusMessage);

                    switch (action)
                    {
                        case ProfileAction.EditProfile:
                            HandleEditProfile(profile);
                            break;
                        case ProfileAction.SetStatus:
                            HandleStatusRequested();
                            break;
                    }
                }
                else
                {
                    ProfileViewDialog.Show(_app, profile);
                }
            });
        });
    }

    private void HandleEditProfile(UserProfileDto? currentProfile)
    {
        var editResult = ProfileEditDialog.Show(_app,
            currentProfile?.DisplayName,
            currentProfile?.Bio,
            currentProfile?.NicknameColor);

        if (editResult is null) return;

        RunAsync(async () =>
        {
            if (!IsAuthenticated) return;

            await _apiClient!.UpdateProfileAsync(new UpdateProfileRequest(
                editResult.DisplayName,
                editResult.Bio,
                editResult.NicknameColor));

            if (editResult.DisplayName is not null)
            {
                InvokeUI(() =>
                {
                    _mainWindow.SetCurrentUser(editResult.DisplayName);
                    _mainWindow.UpdateStatusBar("Connected");
                });
            }

            // Upload avatar if specified
            if (editResult.AvatarPath is not null)
            {
                try
                {
                    Stream stream;
                    string fileName;

                    if (Uri.TryCreate(editResult.AvatarPath, UriKind.Absolute, out var uri)
                        && (uri.Scheme == "http" || uri.Scheme == "https"))
                    {
                        using var http = new HttpClient();
                        var bytes = await http.GetByteArrayAsync(uri);
                        stream = new MemoryStream(bytes);
                        fileName = Path.GetFileName(uri.LocalPath);
                        if (string.IsNullOrWhiteSpace(fileName) || !fileName.Contains('.'))
                            fileName = "avatar.png";
                    }
                    else
                    {
                        stream = File.OpenRead(editResult.AvatarPath);
                        fileName = Path.GetFileName(editResult.AvatarPath);
                    }

                    await using (stream)
                    {
                        await _apiClient!.UploadAvatarAsync(stream, fileName);
                        var channel = _mainWindow.CurrentChannel;
                        if (!string.IsNullOrEmpty(channel))
                            InvokeUI(() => _mainWindow.AddSystemMessage(channel, "Avatar updated."));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Avatar upload failed for {Target}", editResult.AvatarPath);
                    InvokeUI(() => _mainWindow.ShowError($"Avatar upload failed: {ex.Message}"));
                }
            }

            _config.DefaultPreset = new AccountPreset
            {
                DisplayName = editResult.DisplayName,
                Bio = editResult.Bio,
                NicknameColor = editResult.NicknameColor
            };
            ConfigManager.Save(_config);
        }, "Profile update failed");
    }

    private void HandleStatusRequested()
    {
        var result = StatusDialog.Show(_app, _currentStatus, _currentStatusMessage);
        if (result is null) return;

        _currentStatus = result.Status;
        _currentStatusMessage = result.StatusMessage;

        if (IsConnected)
        {
            RunAsync(
                async () => await _connection!.UpdateStatusAsync(result.Status, result.StatusMessage),
                "Status update failed");
        }
    }

    private void HandleThemeSelected(string themeName)
    {
        Log.Information("Theme selected: {Theme}", themeName);

        var theme = ThemeManager.GetTheme(themeName);
        ThemeManager.ApplyTheme(theme);

        _config.ActiveTheme = themeName;
        ConfigManager.Save(_config);

        InvokeUI(() =>
        {
            _mainWindow.ApplyColorSchemes();
            _mainWindow.SetNeedsDraw();
            Log.Debug("Theme applied and UI refreshed");
        });
    }

    private void HandleSavedServersRequested()
    {
        if (_config.SavedServers.Count == 0)
        {
            MessageBox.Query(_app, "Saved Servers",
                "No saved servers yet.\nConnect to a server to save it automatically.", "OK");
            return;
        }

        var serverLines = _config.SavedServers
            .Select(s => $"{s.Name} ({s.Url}) - {s.Username ?? "?"} - {s.LastConnected:yyyy-MM-dd}")
            .ToList();

        MessageBox.Query(_app, "Saved Servers", string.Join("\n", serverLines), "OK");
    }

    private void HandleCreateChannelRequested()
    {
        if (!IsAuthenticated || !IsConnected)
        {
            _mainWindow.ShowError("Not connected to a server.");
            return;
        }

        var result = CreateChannelDialog.Show(_app);
        if (result is null) return;

        RunAsync(async () =>
        {
            var channel = await _apiClient!.CreateChannelAsync(result.Name, result.Topic);
            if (channel is null) return;

            _joinedChannels.Add(channel.Name);
            var history = await _connection!.JoinChannelAsync(channel.Name);

            // Refresh the channel list
            var channels = await _apiClient.GetChannelsAsync();
            InvokeUI(() =>
            {
                _mainWindow.SetChannels(channels);
                _mainWindow.SwitchToChannel(channel.Name);
                if (history.Count > 0)
                    _mainWindow.LoadHistory(channel.Name, history);
            });
        }, "Failed to create channel");
    }

    private void HandleDeleteChannelRequested()
    {
        if (!IsAuthenticated || !IsConnected)
        {
            _mainWindow.ShowError("Not connected to a server.");
            return;
        }

        var channel = _mainWindow.CurrentChannel;
        if (string.IsNullOrEmpty(channel))
        {
            _mainWindow.ShowError("No channel selected.");
            return;
        }

        if (channel == HubConstants.DefaultChannel)
        {
            _mainWindow.ShowError($"The #{HubConstants.DefaultChannel} channel cannot be deleted.");
            return;
        }

        var confirm = MessageBox.Query(_app, "Delete Channel",
            $"Are you sure you want to delete #{channel}?\nThis will remove all messages permanently.", "Delete", "Cancel");

        if (confirm != 0) return;

        RunAsync(async () =>
        {
            await _apiClient!.DeleteChannelAsync(channel);
            _joinedChannels.Remove(channel);

            var channels = await _apiClient.GetChannelsAsync();
            InvokeUI(() =>
            {
                _mainWindow.SetChannels(channels);
                _mainWindow.SwitchToChannel(HubConstants.DefaultChannel);
                _mainWindow.AddSystemMessage(HubConstants.DefaultChannel, $"Channel #{channel} has been deleted.");
            });
        }, "Failed to delete channel");
    }

    // ── Connection Event Wiring ────────────────────────────────────────────

    private void WireConnectionEvents(EchoHubConnection connection)
    {
        connection.OnMessageReceived += message =>
            InvokeUI(() => _mainWindow.AddMessage(message));

        connection.OnUserJoined += (channelName, username) =>
        {
            InvokeUI(() => _mainWindow.AddSystemMessage(channelName, $"{username} joined the channel"));
            if (channelName == _mainWindow.CurrentChannel)
                FetchAndUpdateOnlineUsers();
        };

        connection.OnUserLeft += (channelName, username) =>
        {
            InvokeUI(() => _mainWindow.AddSystemMessage(channelName, $"{username} left the channel"));
            if (channelName == _mainWindow.CurrentChannel)
                FetchAndUpdateOnlineUsers();
        };

        connection.OnUserStatusChanged += presence =>
        {
            InvokeUI(() =>
            {
                var displayName = presence.DisplayName ?? presence.Username;
                var statusText = presence.Status.ToString();
                if (!string.IsNullOrWhiteSpace(presence.StatusMessage))
                    statusText += $" - {presence.StatusMessage}";

                foreach (var channelName in _mainWindow.GetChannelNames())
                    _mainWindow.AddStatusMessage(channelName, displayName, statusText);
            });
            FetchAndUpdateOnlineUsers();
        };

        connection.OnUserKicked += (channelName, username, reason) =>
        {
            var reasonText = reason is not null ? $" ({reason})" : "";
            InvokeUI(() =>
            {
                _mainWindow.AddSystemMessage(channelName, $"{username} was kicked{reasonText}");
                if (username.Equals(_currentUsername, StringComparison.OrdinalIgnoreCase))
                {
                    _mainWindow.AddSystemMessage(channelName, "You were kicked from this channel.");
                }
            });
        };

        connection.OnUserBanned += (username, reason) =>
        {
            InvokeUI(() =>
            {
                if (username.Equals(_currentUsername, StringComparison.OrdinalIgnoreCase))
                {
                    _mainWindow.ShowError("You have been banned from this server.");
                    HandleDisconnect();
                }
            });
        };

        connection.OnMessageDeleted += (channelName, messageId) =>
        {
            InvokeUI(() =>
            {
                _mainWindow.RemoveMessage(channelName, messageId);
            });
        };

        connection.OnChannelNuked += channelName =>
        {
            InvokeUI(() =>
            {
                _mainWindow.ClearChannelMessages(channelName);
                _mainWindow.AddSystemMessage(channelName, "Channel history has been cleared by a moderator.");
            });
        };

        connection.OnError += errorMessage =>
            InvokeUI(() => _mainWindow.ShowError(errorMessage));

        connection.OnConnectionStateChanged += status =>
            InvokeUI(() => _mainWindow.UpdateStatusBar(status));

        connection.OnReconnected += () =>
        {
            // Server-side state is lost on reconnect — rejoin all channels
            var channels = _joinedChannels.ToList();
            if (channels.Count == 0) return;

            _joinedChannels.Clear();

            RunAsync(async () =>
            {
                foreach (var channel in channels)
                {
                    _joinedChannels.Add(channel);
                    await _connection!.JoinChannelAsync(channel);
                }

                Log.Information("Rejoined {Count} channel(s) after reconnect", channels.Count);
            }, "Failed to rejoin channels after reconnect");
        };
    }

    // ── Private Helpers ────────────────────────────────────────────────────

    private void FetchAndUpdateOnlineUsers()
    {
        var channel = _mainWindow.CurrentChannel;
        if (string.IsNullOrEmpty(channel) || !IsConnected) return;

        Task.Run(async () =>
        {
            try
            {
                var users = await _connection!.GetOnlineUsersAsync(channel);
                InvokeUI(() => _mainWindow.UpdateOnlineUsers(users));
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to fetch online users for {Channel}", channel);
            }
        });
    }

    private void SaveServerToConfig(ConnectDialogResult result)
    {
        var savedServer = new SavedServer
        {
            Name = new Uri(result.ServerUrl).Host,
            Url = result.ServerUrl,
            Username = result.Username,
            Token = _apiClient!.Token,
            LastConnected = DateTimeOffset.Now
        };
        ConfigManager.SaveServer(savedServer);
        _config = ConfigManager.Load();
        Log.Information("Connected successfully to {Url}", result.ServerUrl);
    }
}
