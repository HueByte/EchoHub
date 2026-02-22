using EchoHub.Client.Commands;
using EchoHub.Client.Config;
using EchoHub.Client.Services;
using EchoHub.Client.Themes;
using EchoHub.Client.UI;
using EchoHub.Client.UI.Chat;
using EchoHub.Client.UI.Dialogs;
using EchoHub.Core.Constants;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using Serilog;
using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace EchoHub.Client;

/// <summary>
/// Central orchestrator for the EchoHub TUI client.
/// Wires UI events to service calls and connection events to UI updates.
/// </summary>
public sealed class AppOrchestrator : IDisposable
{
    private readonly IApplication _app;
    private readonly MainWindow _mainWindow;
    private readonly ChatMessageManager _messageManager;
    private readonly CommandHandler _commandHandler;
    private readonly NotificationSoundService _notificationSound;
    private readonly AudioPlaybackService _audioPlayback = new();
    private readonly UpdateChecker _updateService;
    private readonly ConnectionManager _conn = new();

    private ClientConfig _config;
    private readonly UserSession _session = new();

    public MainWindow MainWindow => _mainWindow;

    public AppOrchestrator(IApplication app, ClientConfig config)
    {
        _app = app;
        _config = config;
        _messageManager = new ChatMessageManager();
        _mainWindow = new MainWindow(app, _messageManager);
        _commandHandler = new CommandHandler();
        _notificationSound = new NotificationSoundService(config.Notifications);
        _updateService = new UpdateChecker(app);

        WireMainWindowEvents();
        WireCommandHandlerEvents();
        WireConnectionManagerEvents();

        _updateService.Start();

        _mainWindow.UpdateStatusBar("Disconnected");
    }

    public void Dispose()
    {
        _conn.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _updateService.Dispose();
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
        _mainWindow.OnLogoutRequested += HandleLogout;
        _mainWindow.OnMessageSubmitted += HandleMessageSubmitted;
        _mainWindow.OnChannelSelected += HandleChannelSelected;
        _mainWindow.OnProfileRequested += HandleProfileRequested;
        _mainWindow.OnStatusRequested += HandleStatusRequested;
        _mainWindow.OnThemeSelected += HandleThemeSelected;
        _mainWindow.OnSavedServersRequested += HandleSavedServersRequested;
        _mainWindow.OnCreateChannelRequested += HandleCreateChannelRequested;
        _mainWindow.OnDeleteChannelRequested += HandleDeleteChannelRequested;
        _mainWindow.OnAudioPlayRequested += HandleAudioPlayRequested;
        _mainWindow.OnFileDownloadRequested += HandleFileDownloadRequested;
    }

    // ── Command Handler Wiring ─────────────────────────────────────────────

    private void WireCommandHandlerEvents()
    {
        _commandHandler.OnSetStatus += HandleCmdSetStatus;
        _commandHandler.OnSetNick += HandleCmdSetNick;
        _commandHandler.OnSetColor += HandleCmdSetColor;
        _commandHandler.OnSetTheme += HandleCmdSetTheme;
        _commandHandler.OnSendFile += HandleCmdSendFile;
        _commandHandler.OnSetAvatar += HandleCmdSetAvatar;
        _commandHandler.OnOpenProfile += HandleCmdOpenProfile;
        _commandHandler.OnOpenServers += HandleCmdOpenServers;
        _commandHandler.OnJoinChannel += HandleCmdJoinChannel;
        _commandHandler.OnLeaveChannel += HandleCmdLeaveChannel;
        _commandHandler.OnSetTopic += HandleCmdSetTopic;
        _commandHandler.OnListUsers += HandleCmdListUsers;
        _commandHandler.OnKickUser += HandleCmdKickUser;
        _commandHandler.OnBanUser += HandleCmdBanUser;
        _commandHandler.OnUnbanUser += HandleCmdUnbanUser;
        _commandHandler.OnMuteUser += HandleCmdMuteUser;
        _commandHandler.OnUnmuteUser += HandleCmdUnmuteUser;
        _commandHandler.OnAssignRole += HandleCmdAssignRole;
        _commandHandler.OnNukeChannel += HandleCmdNukeChannel;
        _commandHandler.OnTestSound += HandleCmdTestSound;
        _commandHandler.OnQuit += HandleCmdQuit;
    }

    // ── Command Handlers ──────────────────────────────────────────────────

    private async Task HandleCmdSetStatus(UserStatus status, string? message)
    {
        if (!_conn.IsConnected) return;

        await _conn.UpdateStatusAsync(status, message);
        _session.Status = status;
        _session.StatusMessage = message;
    }

    private async Task HandleCmdSetNick(string displayName)
    {
        if (!_conn.IsAuthenticated) return;

        await _conn.Api!.UpdateProfileAsync(new UpdateProfileRequest(DisplayName: displayName));
        InvokeUI(() =>
        {
            _mainWindow.SetCurrentUser(displayName);
            _mainWindow.UpdateStatusBar("Connected");
        });
    }

    private async Task HandleCmdSetColor(string color)
    {
        if (!_conn.IsAuthenticated) return;
        await _conn.Api!.UpdateProfileAsync(new UpdateProfileRequest(NicknameColor: color));
    }

    private Task HandleCmdSetTheme(string name)
    {
        InvokeUI(() => HandleThemeSelected(name));
        return Task.CompletedTask;
    }

    private async Task HandleCmdSendFile(string target, string? size)
    {
        if (!_conn.IsAuthenticated || !_conn.IsConnected) return;

        var channel = _mainWindow.CurrentChannel;
        if (string.IsNullOrEmpty(channel)) return;

        try
        {
            if (Uri.TryCreate(target, UriKind.Absolute, out var uri)
                && (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                await _conn.Api!.SendUrlAsync(channel, target, size);
            }
            else
            {
                await using var stream = File.OpenRead(target);
                var fileName = Path.GetFileName(target);
                await _conn.Api!.UploadFileAsync(channel, stream, fileName, size);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "File send failed for {Target}", target);
            InvokeUI(() => _mainWindow.ShowError($"Send failed: {ex.Message}"));
        }
    }

    private async Task HandleCmdSetAvatar(string target)
    {
        if (!_conn.IsAuthenticated) return;

        try
        {
            await AvatarHelper.UploadAsync(_conn.Api!, target);
            var channel = _mainWindow.CurrentChannel;
            if (!string.IsNullOrEmpty(channel))
                InvokeUI(() => _messageManager.AddSystemMessage(channel, "Avatar updated."));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Avatar upload failed for {Target}", target);
            InvokeUI(() => _mainWindow.ShowError($"Avatar upload failed: {ex.Message}"));
        }
    }

    private Task HandleCmdOpenProfile(string? username)
    {
        InvokeUI(() => HandleViewProfile(username));
        return Task.CompletedTask;
    }

    private Task HandleCmdOpenServers()
    {
        InvokeUI(HandleSavedServersRequested);
        return Task.CompletedTask;
    }

    private async Task HandleCmdJoinChannel(string channelName)
    {
        if (!_conn.IsConnected) return;

        try
        {
            var history = await _conn.JoinChannelAsync(channelName);
            InvokeUI(() =>
            {
                _mainWindow.EnsureChannelInList(channelName);
                _mainWindow.SwitchToChannel(channelName);
                if (history.Count > 0)
                    _messageManager.LoadHistory(channelName, history);
            });
        }
        catch (Exception ex)
        {
            InvokeUI(() => _mainWindow.ShowError($"Failed to join channel: {ex.Message}"));
        }
    }

    private async Task HandleCmdLeaveChannel()
    {
        if (!_conn.IsConnected) return;

        var channel = _mainWindow.CurrentChannel;
        if (string.IsNullOrEmpty(channel)) return;

        if (channel == HubConstants.DefaultChannel)
        {
            InvokeUI(() => _mainWindow.ShowError($"You cannot leave the #{HubConstants.DefaultChannel} channel."));
            return;
        }

        try
        {
            await _conn.LeaveChannelAsync(channel);
            InvokeUI(() => _messageManager.AddSystemMessage(channel, $"You left #{channel}"));
        }
        catch (Exception ex)
        {
            InvokeUI(() => _mainWindow.ShowError($"Failed to leave channel: {ex.Message}"));
        }
    }

    private async Task HandleCmdSetTopic(string topic)
    {
        if (!_conn.IsAuthenticated) return;

        var channel = _mainWindow.CurrentChannel;
        if (string.IsNullOrEmpty(channel)) return;

        try
        {
            await _conn.Api!.UpdateChannelTopicAsync(channel, topic);
            InvokeUI(() =>
            {
                _mainWindow.SetChannelTopic(channel, topic);
                _messageManager.AddSystemMessage(channel, $"Topic set to: {topic}");
            });
        }
        catch (Exception ex)
        {
            InvokeUI(() => _mainWindow.ShowError($"Failed to set topic: {ex.Message}"));
        }
    }

    private async Task HandleCmdListUsers()
    {
        if (!_conn.IsConnected) return;

        var channel = _mainWindow.CurrentChannel;
        if (string.IsNullOrEmpty(channel)) return;

        try
        {
            var users = await _conn.GetOnlineUsersAsync(channel);
            InvokeUI(() =>
            {
                _messageManager.AddSystemMessage(channel, $"Online users in #{channel}:");
                foreach (var user in users)
                {
                    var displayName = user.DisplayName ?? user.Username;
                    var statusText = user.Status.ToString();
                    if (!string.IsNullOrWhiteSpace(user.StatusMessage))
                        statusText += $" - {user.StatusMessage}";
                    _messageManager.AddSystemMessage(channel, $"  {displayName} ({statusText})");
                }
            });
        }
        catch (Exception ex)
        {
            InvokeUI(() => _mainWindow.ShowError($"Failed to list users: {ex.Message}"));
        }
    }

    private async Task HandleCmdKickUser(string username, string? reason)
    {
        if (!_conn.IsAuthenticated) return;
        await _conn.Api!.KickUserAsync(username, reason);
    }

    private async Task HandleCmdBanUser(string username, string? reason)
    {
        if (!_conn.IsAuthenticated) return;
        await _conn.Api!.BanUserAsync(username, reason);
    }

    private async Task HandleCmdUnbanUser(string username)
    {
        if (!_conn.IsAuthenticated) return;
        await _conn.Api!.UnbanUserAsync(username);
    }

    private async Task HandleCmdMuteUser(string username, int? duration)
    {
        if (!_conn.IsAuthenticated) return;
        await _conn.Api!.MuteUserAsync(username, duration);
    }

    private async Task HandleCmdUnmuteUser(string username)
    {
        if (!_conn.IsAuthenticated) return;
        await _conn.Api!.UnmuteUserAsync(username);
    }

    private async Task HandleCmdAssignRole(string username, string roleStr)
    {
        if (!_conn.IsAuthenticated) return;
        var role = roleStr switch
        {
            "admin" => ServerRole.Admin,
            "mod" => ServerRole.Mod,
            _ => ServerRole.Member,
        };
        await _conn.Api!.AssignRoleAsync(username, role);
    }

    private async Task HandleCmdNukeChannel()
    {
        if (!_conn.IsAuthenticated) return;
        var channel = _mainWindow.CurrentChannel;
        if (string.IsNullOrEmpty(channel)) return;
        await _conn.Api!.NukeChannelAsync(channel);
    }

    private async Task HandleCmdTestSound()
    {
        await _notificationSound.PlayTestAsync();
    }

    private Task HandleCmdQuit()
    {
        InvokeUI(() => _app.RequestStop());
        return Task.CompletedTask;
    }

    // ── ConnectionManager Event Wiring ─────────────────────────────────────

    private void WireConnectionManagerEvents()
    {
        _conn.MessageReceived += message =>
        {
            InvokeUI(() => _messageManager.AddMessage(message));

            if (!string.IsNullOrEmpty(_session.Username)
                && message.Content.Contains($"@{_session.Username}", StringComparison.OrdinalIgnoreCase))
            {
                _ = _notificationSound.PlayAsync();
            }
        };

        _conn.UserJoined += (channelName, username) =>
        {
            InvokeUI(() => _messageManager.AddSystemMessage(channelName, $"{username} joined the channel"));
            if (channelName == _mainWindow.CurrentChannel)
                FetchAndUpdateOnlineUsers();
        };

        _conn.UserLeft += (channelName, username) =>
        {
            InvokeUI(() => _messageManager.AddSystemMessage(channelName, $"{username} left the channel"));
            if (channelName == _mainWindow.CurrentChannel)
                FetchAndUpdateOnlineUsers();
        };

        _conn.UserStatusChanged += presence =>
        {
            InvokeUI(() =>
            {
                var displayName = presence.DisplayName ?? presence.Username;
                var statusText = presence.Status.ToString();
                if (!string.IsNullOrWhiteSpace(presence.StatusMessage))
                    statusText += $" - {presence.StatusMessage}";

                foreach (var channelName in _mainWindow.GetChannelNames())
                    _messageManager.AddStatusMessage(channelName, displayName, statusText);
            });
            FetchAndUpdateOnlineUsers();
        };

        _conn.UserKicked += (channelName, username, reason) =>
        {
            var reasonText = reason is not null ? $" ({reason})" : "";
            InvokeUI(() => _messageManager.AddSystemMessage(channelName, $"{username} was kicked{reasonText}"));
        };

        _conn.UserBanned += (username, reason) =>
        {
            var reasonText = reason is not null ? $" ({reason})" : "";
            InvokeUI(() =>
            {
                if (!username.Equals(_session.Username, StringComparison.OrdinalIgnoreCase))
                {
                    var channel = _mainWindow.CurrentChannel;
                    if (!string.IsNullOrEmpty(channel))
                        _messageManager.AddSystemMessage(channel, $"{username} was banned{reasonText}");
                }
            });
        };

        _conn.ForceDisconnected += reason =>
        {
            InvokeUI(() =>
            {
                _mainWindow.ShowError(reason);
                HandleDisconnect();
            });
        };

        _conn.MessageDeleted += (channelName, messageId) =>
            InvokeUI(() => _messageManager.RemoveMessage(channelName, messageId));

        _conn.ChannelNuked += channelName =>
        {
            InvokeUI(() =>
            {
                _messageManager.ClearChannelMessages(channelName);
                _messageManager.AddSystemMessage(channelName, "Channel history has been cleared by a moderator.");
            });
        };

        _conn.ChannelUpdated += channel =>
        {
            InvokeUI(() =>
            {
                if (channel.IsPublic)
                    _mainWindow.EnsureChannelInList(channel.Name, channel.IsPublic);
                _mainWindow.SetChannelTopic(channel.Name, channel.Topic);
            });
        };

        _conn.Error += errorMessage =>
            InvokeUI(() => _mainWindow.ShowError(errorMessage));

        _conn.ConnectionStatusChanged += status =>
            InvokeUI(() => _mainWindow.UpdateStatusBar(status));

        _conn.Reconnected += () =>
        {
            RunAsync(
                async () => await _conn.RejoinChannelsAsync(),
                "Failed to rejoin channels after reconnect");
        };
    }

    // ── MainWindow Event Handlers ──────────────────────────────────────────

    private void HandleConnect()
    {
        if (_conn.IsConnected)
        {
            var confirm = MessageBox.Query(_app, "Already Connected",
                "You are already connected to a server.\nDisconnect and connect to a new one?", "Yes", "Cancel");

            if (confirm != 0) return;

            HandleDisconnect();
        }

        var dialogResult = ConnectDialog.Show(_app, _config.SavedServers);
        if (dialogResult is null) return;

        Log.Information("Connecting to {Url} as {User} (register={IsRegister})",
            dialogResult.ServerUrl, dialogResult.Username, dialogResult.IsRegister);

        RunAsync(async () =>
        {
            ConnectResult result;
            try
            {
                result = await _conn.ConnectAsync(dialogResult,
                    status => InvokeUI(() => _mainWindow.UpdateStatusBar(status)));
            }
            catch (Exception ex) when (dialogResult.SavedRefreshToken is not null)
            {
                Log.Warning(ex, "Saved session expired or revoked");
                ClearSavedToken(dialogResult.ServerUrl);
                InvokeUI(() =>
                {
                    _mainWindow.UpdateStatusBar("Disconnected");
                    MessageBox.ErrorQuery(_app, "Session Expired",
                        "Your saved session has expired or was revoked.\nPlease log in with your password.", "OK");
                });
                return;
            }

            _session.Username = result.Login.Username;

            InvokeUI(() =>
            {
                _mainWindow.SetCurrentUser(result.Login.DisplayName ?? result.Login.Username);
                _mainWindow.SetChannels(result.Channels);
                _mainWindow.SwitchToChannel(HubConstants.DefaultChannel);
                if (result.DefaultHistory.Count > 0)
                    _messageManager.LoadHistory(HubConstants.DefaultChannel, result.DefaultHistory);
                _mainWindow.FocusInput();
                FetchAndUpdateOnlineUsers();
            });
            SaveServerToConfig(dialogResult);
        }, "Connection failed", "Connect");
    }

    private void HandleDisconnect()
    {
        Log.Information("Disconnecting from server");

        RunAsync(async () =>
        {
            await _conn.CleanupAsync();
            InvokeUI(() =>
            {
                _mainWindow.ClearAll();
                _mainWindow.UpdateStatusBar("Disconnected");
            });
        }, "Disconnect error", "Disconnect");
    }

    private void HandleLogout()
    {
        Log.Information("Logging out from server");

        RunAsync(async () =>
        {
            var baseUrl = _conn.Api?.BaseUrl;
            await _conn.LogoutAsync();

            if (baseUrl is not null)
                ClearSavedToken(baseUrl);

            await _conn.CleanupAsync();
            InvokeUI(() =>
            {
                _mainWindow.ClearAll();
                _mainWindow.UpdateStatusBar("Disconnected");
            });
        }, "Logout error", "Logout");
    }

    private void HandleMessageSubmitted(string channelName, string content)
    {
        if (!_conn.IsConnected)
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
                            _messageManager.AddSystemMessage(channelName, result.Message);
                    });
                }
            }, "Command failed");
            return;
        }

        RunAsync(
            async () => await _conn.SendMessageAsync(channelName, content),
            "Send failed");
    }

    private void HandleChannelSelected(string channelName)
    {
        if (!_conn.IsConnected) return;

        RunAsync(async () =>
        {
            if (_conn.TrackChannel(channelName))
                await _conn.JoinChannelAsync(channelName);

            try
            {
                var history = await _conn.GetHistoryAsync(channelName);
                InvokeUI(() => _messageManager.LoadHistory(channelName, history));
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
        var isOwnProfile = string.IsNullOrWhiteSpace(username)
            || username.Equals(_session.Username, StringComparison.OrdinalIgnoreCase);

        Task.Run(async () =>
        {
            UserProfileDto? profile = null;
            try
            {
                if (_conn.IsAuthenticated)
                {
                    var target = isOwnProfile ? _session.Username : username!;
                    if (!string.IsNullOrEmpty(target))
                        profile = await _conn.Api!.GetUserProfileAsync(target);
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
                        _session.Status,
                        _session.StatusMessage);

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
            currentProfile?.NicknameColor,
            _config.Notifications.Enabled,
            _config.Notifications.Volume);

        if (editResult is null) return;

        RunAsync(async () =>
        {
            if (!_conn.IsAuthenticated) return;

            await _conn.Api!.UpdateProfileAsync(new UpdateProfileRequest(
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
                    await AvatarHelper.UploadAsync(_conn.Api!, editResult.AvatarPath);
                    var channel = _mainWindow.CurrentChannel;
                    if (!string.IsNullOrEmpty(channel))
                        InvokeUI(() => _messageManager.AddSystemMessage(channel, "Avatar updated."));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Avatar upload failed for {Target}", editResult.AvatarPath);
                    InvokeUI(() => _mainWindow.ShowError($"Avatar upload failed: {ex.Message}"));
                }
            }

            if (editResult.NotificationSoundEnabled.HasValue)
            {
                _config.Notifications.Enabled = editResult.NotificationSoundEnabled.Value;
                _notificationSound.SetEnabled(editResult.NotificationSoundEnabled.Value);
            }

            if (editResult.NotificationVolume.HasValue)
            {
                _config.Notifications.Volume = editResult.NotificationVolume.Value;
                _notificationSound.SetVolume(editResult.NotificationVolume.Value);
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
        var result = StatusDialog.Show(_app, _session.Status, _session.StatusMessage);
        if (result is null) return;

        _session.Status = result.Status;
        _session.StatusMessage = result.StatusMessage;

        if (_conn.IsConnected)
        {
            RunAsync(
                async () => await _conn.UpdateStatusAsync(result.Status, result.StatusMessage),
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
            .Select(s =>
            {
                var session = !string.IsNullOrEmpty(s.RefreshToken) ? " [session saved]" : "";
                return $"{s.Name} ({s.Url}) - {s.Username ?? "?"} - {s.LastConnected:yyyy-MM-dd}{session}";
            })
            .ToList();

        MessageBox.Query(_app, "Saved Servers", string.Join("\n", serverLines), "OK");
    }

    private void HandleCreateChannelRequested()
    {
        if (!_conn.IsAuthenticated || !_conn.IsConnected)
        {
            _mainWindow.ShowError("Not connected to a server.");
            return;
        }

        var result = CreateChannelDialog.Show(_app);
        if (result is null) return;

        RunAsync(async () =>
        {
            var channel = await _conn.Api!.CreateChannelAsync(result.Name, result.Topic, result.IsPublic);
            if (channel is null) return;

            var history = await _conn.JoinChannelAsync(channel.Name);

            InvokeUI(() =>
            {
                _mainWindow.EnsureChannelInList(channel.Name);
                _mainWindow.SetChannelTopic(channel.Name, channel.Topic);
                _mainWindow.SwitchToChannel(channel.Name);
                if (history.Count > 0)
                    _messageManager.LoadHistory(channel.Name, history);
            });
        }, "Failed to create channel");
    }

    private void HandleDeleteChannelRequested()
    {
        if (!_conn.IsAuthenticated || !_conn.IsConnected)
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
            await _conn.Api!.DeleteChannelAsync(channel);
            _conn.UntrackChannel(channel);

            InvokeUI(() =>
            {
                _mainWindow.RemoveChannel(channel);
                _mainWindow.SwitchToChannel(HubConstants.DefaultChannel);
                _messageManager.AddSystemMessage(HubConstants.DefaultChannel, $"Channel #{channel} has been deleted.");
            });
        }, "Failed to delete channel");
    }

    private void HandleAudioPlayRequested(string attachmentUrl, string fileName)
    {
        if (!_conn.IsAuthenticated) return;

        RunAsync(async () =>
        {
            InvokeUI(() => _messageManager.AddSystemMessage(_mainWindow.CurrentChannel, $"Downloading {fileName}..."));
            var tempPath = await _conn.Api!.DownloadFileToTempAsync(attachmentUrl, fileName);
            InvokeUI(() => AudioPlayerDialog.Show(_app, _audioPlayback, tempPath, fileName));
        }, "Failed to play audio");
    }

    private void HandleFileDownloadRequested(string attachmentUrl, string fileName)
    {
        if (!_conn.IsAuthenticated) return;

        RunAsync(async () =>
        {
            InvokeUI(() => _messageManager.AddSystemMessage(_mainWindow.CurrentChannel, $"Downloading {fileName}..."));
            var tempPath = await _conn.Api!.DownloadFileToTempAsync(attachmentUrl, fileName);

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(tempPath) { UseShellExecute = true };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to open file with default app: {Path}", tempPath);
                InvokeUI(() => _messageManager.AddSystemMessage(_mainWindow.CurrentChannel, $"Downloaded to: {tempPath}"));
            }
        }, "Failed to download file");
    }

    // ── Private Helpers ────────────────────────────────────────────────────

    private void FetchAndUpdateOnlineUsers()
    {
        var channel = _mainWindow.CurrentChannel;
        if (string.IsNullOrEmpty(channel) || !_conn.IsConnected) return;

        Task.Run(async () =>
        {
            try
            {
                var users = await _conn.GetOnlineUsersAsync(channel);
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
            RefreshToken = result.RememberMe ? _conn.Api!.RefreshToken : null,
            RememberMe = result.RememberMe,
            LastConnected = DateTimeOffset.Now
        };
        ConfigManager.SaveServer(savedServer);
        _config = ConfigManager.Load();
        Log.Information("Connected successfully to {Url}", result.ServerUrl);
    }

    private void ClearSavedToken(string serverUrl)
    {
        var config = ConfigManager.Load();
        var server = config.SavedServers.FirstOrDefault(s =>
            string.Equals(s.Url, serverUrl, StringComparison.OrdinalIgnoreCase));
        if (server is not null)
        {
            server.RefreshToken = null;
            ConfigManager.Save(config);
            _config = config;
        }
    }
}
