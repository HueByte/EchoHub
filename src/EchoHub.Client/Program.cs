using EchoHub.Client.Commands;
using EchoHub.Client.Config;
using EchoHub.Client.Services;
using EchoHub.Client.Themes;
using EchoHub.Client.UI;
using EchoHub.Core.Constants;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using Microsoft.Extensions.Configuration;
using Serilog;
using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace EchoHub.Client;

public static class Program
{
    private static IApplication _app = null!;
    private static EchoHubConnection? _connection;
    private static ApiClient? _apiClient;
    private static MainWindow? _mainWindow;
    private static CommandHandler? _commandHandler;
    private static ClientConfig _config = new();
    private static UserStatus _currentStatus = UserStatus.Online;
    private static string? _currentStatusMessage;
    private static string _currentUsername = string.Empty;

    public static void Main()
    {
        // Configure Serilog from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        Log.Information("EchoHub client starting");

        try
        {
            // Load configuration
            _config = ConfigManager.Load();
            Log.Information("Configuration loaded, active theme: {Theme}", _config.ActiveTheme);

            _app = Application.Create().Init();

            // Apply our custom color scheme
            var theme = Themes.ThemeManager.GetTheme(_config.ActiveTheme);
            Themes.ThemeManager.ApplyTheme(theme);

            _mainWindow = new MainWindow(_app);
            _commandHandler = new CommandHandler();

            // Wire MainWindow events
            _mainWindow.OnConnectRequested += HandleConnect;
            _mainWindow.OnDisconnectRequested += HandleDisconnect;
            _mainWindow.OnMessageSubmitted += HandleMessageSubmitted;
            _mainWindow.OnChannelSelected += HandleChannelSelected;
            _mainWindow.OnProfileRequested += HandleProfileRequested;
            _mainWindow.OnStatusRequested += HandleStatusRequested;
            _mainWindow.OnThemeSelected += HandleThemeSelected;
            _mainWindow.OnSavedServersRequested += HandleSavedServersRequested;

            // Wire CommandHandler events
            WireCommandHandlerEvents();

            _mainWindow.UpdateStatusBar("Disconnected");

            _app.Run(_mainWindow);
            _app.Dispose();

            // Cleanup
            _connection?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _apiClient?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "EchoHub client crashed");
            throw;
        }
        finally
        {
            Log.Information("EchoHub client shutting down");
            Log.CloseAndFlush();
        }
    }

    // ── Command Handler Wiring ─────────────────────────────────────────────

    private static void WireCommandHandlerEvents()
    {
        _commandHandler!.OnSetStatus += async (status, message) =>
        {
            if (_connection is null || !_connection.IsConnected)
                return;

            await _connection.UpdateStatusAsync(status, message);
            _currentStatus = status;
            _currentStatusMessage = message;
        };

        _commandHandler.OnSetNick += async (displayName) =>
        {
            if (_apiClient is null)
                return;

            await _apiClient.UpdateProfileAsync(new UpdateProfileRequest(DisplayName: displayName));
            _app.Invoke(() =>
            {
                _mainWindow!.SetCurrentUser(displayName);
                _mainWindow.UpdateStatusBar("Connected");
            });
        };

        _commandHandler.OnSetColor += async (color) =>
        {
            if (_apiClient is null)
                return;

            await _apiClient.UpdateProfileAsync(new UpdateProfileRequest(NicknameColor: color));
        };

        _commandHandler.OnSetTheme += (name) =>
        {
            _app.Invoke(() => HandleThemeSelected(name));
            return Task.CompletedTask;
        };

        _commandHandler.OnSendFile += async (target) =>
        {
            if (_apiClient is null || _connection is null || !_connection.IsConnected)
                return;

            var channel = _mainWindow!.CurrentChannel;
            if (string.IsNullOrEmpty(channel))
                return;

            try
            {
                if (Uri.TryCreate(target, UriKind.Absolute, out var uri)
                    && (uri.Scheme == "http" || uri.Scheme == "https"))
                {
                    // Send URL to server — server handles downloading and conversion
                    await _apiClient.SendUrlAsync(channel, target);
                }
                else
                {
                    // Local file — upload to server
                    await using var stream = File.OpenRead(target);
                    var fileName = Path.GetFileName(target);
                    await _apiClient.UploadFileAsync(channel, stream, fileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "File send failed for {Target}", target);
                _app.Invoke(() =>
                    _mainWindow!.ShowError($"Send failed: {ex.Message}"));
            }
        };

        _commandHandler.OnOpenProfile += () =>
        {
            _app.Invoke(HandleProfileRequested);
            return Task.CompletedTask;
        };

        _commandHandler.OnOpenServers += () =>
        {
            _app.Invoke(HandleSavedServersRequested);
            return Task.CompletedTask;
        };

        _commandHandler.OnJoinChannel += async (channelName) =>
        {
            if (_connection is null || !_connection.IsConnected)
                return;

            try
            {
                var history = await _connection.JoinChannelAsync(channelName);
                _app.Invoke(() =>
                {
                    _mainWindow!.SwitchToChannel(channelName);
                    if (history.Count > 0)
                        _mainWindow.LoadHistory(channelName, history);
                });
            }
            catch (Exception ex)
            {
                _app.Invoke(() =>
                    _mainWindow!.ShowError($"Failed to join channel: {ex.Message}"));
            }
        };

        _commandHandler.OnLeaveChannel += async () =>
        {
            if (_connection is null || !_connection.IsConnected)
                return;

            var channel = _mainWindow!.CurrentChannel;
            if (string.IsNullOrEmpty(channel))
                return;

            try
            {
                await _connection.LeaveChannelAsync(channel);
                _app.Invoke(() =>
                    _mainWindow!.AddSystemMessage(channel, $"You left #{channel}"));
            }
            catch (Exception ex)
            {
                _app.Invoke(() =>
                    _mainWindow!.ShowError($"Failed to leave channel: {ex.Message}"));
            }
        };

        _commandHandler.OnSetTopic += async (topic) =>
        {
            if (_apiClient is null)
                return;

            var channel = _mainWindow!.CurrentChannel;
            if (string.IsNullOrEmpty(channel))
                return;

            try
            {
                await _apiClient.UpdateChannelTopicAsync(channel, topic);
                _app.Invoke(() =>
                    _mainWindow!.AddSystemMessage(channel, $"Topic set to: {topic}"));
            }
            catch (Exception ex)
            {
                _app.Invoke(() =>
                    _mainWindow!.ShowError($"Failed to set topic: {ex.Message}"));
            }
        };

        _commandHandler.OnListUsers += async () =>
        {
            if (_connection is null || !_connection.IsConnected)
                return;

            var channel = _mainWindow!.CurrentChannel;
            if (string.IsNullOrEmpty(channel))
                return;

            try
            {
                var users = await _connection.GetOnlineUsersAsync(channel);
                _app.Invoke(() =>
                {
                    _mainWindow!.AddSystemMessage(channel, $"Online users in #{channel}:");
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
                _app.Invoke(() =>
                    _mainWindow!.ShowError($"Failed to list users: {ex.Message}"));
            }
        };

        _commandHandler.OnQuit += () =>
        {
            _app.Invoke(() => _app.RequestStop());
            return Task.CompletedTask;
        };

        // OnHelp is handled by CommandHandler returning help text — no additional wiring needed.
    }

    // ── MainWindow Event Handlers ──────────────────────────────────────────

    private static void HandleConnect()
    {
        var result = ConnectDialog.Show(_app, _config.SavedServers);
        if (result is null)
            return;

        Log.Information("Connecting to {Url} as {User} (register={IsRegister})",
            result.ServerUrl, result.Username, result.IsRegister);

        Task.Run(async () =>
        {
            try
            {
                _apiClient?.Dispose();
                _apiClient = new ApiClient(result.ServerUrl);

                _app.Invoke(() =>
                    _mainWindow!.UpdateStatusBar("Authenticating..."));

                LoginResponse loginResponse;
                if (result.IsRegister)
                {
                    loginResponse = await _apiClient.RegisterAsync(result.Username, result.Password);
                }
                else
                {
                    loginResponse = await _apiClient.LoginAsync(result.Username, result.Password);
                }

                _currentUsername = loginResponse.Username;

                _app.Invoke(() =>
                {
                    _mainWindow!.SetCurrentUser(loginResponse.DisplayName ?? loginResponse.Username);
                    _mainWindow.UpdateStatusBar("Authenticated, connecting...");
                });

                // Dispose previous connection if any
                if (_connection is not null)
                {
                    await _connection.DisposeAsync();
                }

                _connection = new EchoHubConnection(result.ServerUrl, _apiClient);
                WireConnectionEvents(_connection);

                await _connection.ConnectAsync();

                // Load channels
                var channels = await _apiClient.GetChannelsAsync();
                _app.Invoke(() =>
                {
                    _mainWindow!.SetChannels(channels);
                    _mainWindow.UpdateStatusBar("Connected");
                });

                // Join the default channel
                await _connection.JoinChannelAsync(HubConstants.DefaultChannel);

                _app.Invoke(() =>
                    _mainWindow!.SwitchToChannel(HubConstants.DefaultChannel));

                // Load history for default channel
                try
                {
                    var history = await _connection.GetHistoryAsync(HubConstants.DefaultChannel);
                    _app.Invoke(() =>
                    {
                        _mainWindow!.LoadHistory(HubConstants.DefaultChannel, history);
                        _mainWindow.FocusInput();
                    });
                }
                catch
                {
                    // History might not be available, that is okay
                }

                // Save server to config on successful connection
                var savedServer = new SavedServer
                {
                    Name = new Uri(result.ServerUrl).Host,
                    Url = result.ServerUrl,
                    Username = result.Username,
                    Token = _apiClient.Token,
                    LastConnected = DateTimeOffset.Now
                };
                ConfigManager.SaveServer(savedServer);
                _config = ConfigManager.Load();
                Log.Information("Connected successfully to {Url}", result.ServerUrl);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Connection failed to {Url}", result.ServerUrl);
                _app.Invoke(() =>
                {
                    _mainWindow!.ShowError($"Connection failed: {ex.Message}");
                    _mainWindow.UpdateStatusBar("Disconnected");
                });
            }
        });
    }

    private static void HandleDisconnect()
    {
        Log.Information("Disconnecting from server");
        Task.Run(async () =>
        {
            try
            {
                if (_connection is not null)
                {
                    await _connection.DisconnectAsync();
                    await _connection.DisposeAsync();
                    _connection = null;
                }

                _apiClient?.Dispose();
                _apiClient = null;

                _app.Invoke(() =>
                {
                    _mainWindow!.ClearAll();
                    _mainWindow.UpdateStatusBar("Disconnected");
                });
            }
            catch (Exception ex)
            {
                _app.Invoke(() =>
                    _mainWindow!.ShowError($"Disconnect error: {ex.Message}"));
            }
        });
    }

    private static void HandleMessageSubmitted(string channelName, string content)
    {
        if (_connection is null || !_connection.IsConnected)
        {
            _mainWindow!.ShowError("Not connected to a server.");
            return;
        }

        // Check if this is a command
        if (_commandHandler!.IsCommand(content))
        {
            Task.Run(async () =>
            {
                try
                {
                    var result = await _commandHandler.HandleAsync(content);
                    if (result.Message is not null)
                    {
                        _app.Invoke(() =>
                        {
                            if (result.IsError)
                                _mainWindow!.ShowError(result.Message);
                            else
                                _mainWindow!.AddSystemMessage(channelName, result.Message);
                        });
                    }
                }
                catch (Exception ex)
                {
                    _app.Invoke(() =>
                        _mainWindow!.ShowError($"Command failed: {ex.Message}"));
                }
            });
            return;
        }

        // Regular message
        Task.Run(async () =>
        {
            try
            {
                await _connection.SendMessageAsync(channelName, content);
            }
            catch (Exception ex)
            {
                _app.Invoke(() =>
                    _mainWindow!.ShowError($"Send failed: {ex.Message}"));
            }
        });
    }

    private static void HandleChannelSelected(string channelName)
    {
        if (_connection is null || !_connection.IsConnected)
            return;

        Task.Run(async () =>
        {
            try
            {
                await _connection.JoinChannelAsync(channelName);

                // Load history if the channel has no messages cached yet
                try
                {
                    var history = await _connection.GetHistoryAsync(channelName);
                    _app.Invoke(() =>
                        _mainWindow!.LoadHistory(channelName, history));
                }
                catch
                {
                    // History might not be available
                }
            }
            catch (Exception ex)
            {
                _app.Invoke(() =>
                    _mainWindow!.ShowError($"Failed to join channel: {ex.Message}"));
            }
        });
    }

    private static void HandleProfileRequested()
    {
        Task.Run(async () =>
        {
            UserProfileDto? profile = null;
            try
            {
                if (_apiClient is not null && !string.IsNullOrEmpty(_currentUsername))
                {
                    profile = await _apiClient.GetUserProfileAsync(_currentUsername);
                }
            }
            catch
            {
                // Profile may not be available; continue with null
            }

            _app.Invoke(() =>
            {
                var action = UserPanelDialog.Show(_app,
                    profile,
                    _config.SavedServers,
                    _currentStatus,
                    _currentStatusMessage);

                switch (action)
                {
                    case UserPanelAction.EditProfile:
                        HandleEditProfile(profile);
                        break;
                    case UserPanelAction.SetStatus:
                        HandleStatusRequested();
                        break;
                    case UserPanelAction.Close:
                    default:
                        break;
                }
            });
        });
    }

    private static void HandleEditProfile(UserProfileDto? currentProfile)
    {
        var editResult = ProfileEditDialog.Show(_app,
            currentProfile?.DisplayName,
            currentProfile?.Bio,
            currentProfile?.NicknameColor);

        if (editResult is null)
            return;

        Task.Run(async () =>
        {
            try
            {
                if (_apiClient is not null)
                {
                    await _apiClient.UpdateProfileAsync(new UpdateProfileRequest(
                        editResult.DisplayName,
                        editResult.Bio,
                        editResult.NicknameColor));

                    if (editResult.DisplayName is not null)
                    {
                        _app.Invoke(() =>
                        {
                            _mainWindow!.SetCurrentUser(editResult.DisplayName);
                            _mainWindow.UpdateStatusBar("Connected");
                        });
                    }

                    // Update local config preset
                    _config.DefaultPreset = new AccountPreset
                    {
                        DisplayName = editResult.DisplayName,
                        Bio = editResult.Bio,
                        NicknameColor = editResult.NicknameColor
                    };
                    ConfigManager.Save(_config);
                }
            }
            catch (Exception ex)
            {
                _app.Invoke(() =>
                    _mainWindow!.ShowError($"Profile update failed: {ex.Message}"));
            }
        });
    }

    private static void HandleStatusRequested()
    {
        var result = StatusDialog.Show(_app, _currentStatus, _currentStatusMessage);
        if (result is null)
            return;

        _currentStatus = result.Status;
        _currentStatusMessage = result.StatusMessage;

        if (_connection is not null && _connection.IsConnected)
        {
            Task.Run(async () =>
            {
                try
                {
                    await _connection.UpdateStatusAsync(result.Status, result.StatusMessage);
                }
                catch (Exception ex)
                {
                    _app.Invoke(() =>
                        _mainWindow!.ShowError($"Status update failed: {ex.Message}"));
                }
            });
        }
    }

    private static void HandleThemeSelected(string themeName)
    {
        Log.Information("Theme selected: {Theme}", themeName);

        var theme = Themes.ThemeManager.GetTheme(themeName);
        Themes.ThemeManager.ApplyTheme(theme);

        _config.ActiveTheme = themeName;
        ConfigManager.Save(_config);

        // Defer UI refresh so the menu finishes processing its click first
        _app.Invoke(() =>
        {
            _mainWindow!.ApplyColorSchemes();
            _mainWindow.SetNeedsDraw();
            Log.Debug("Theme applied and UI refreshed");
        });
    }

    private static void HandleSavedServersRequested()
    {
        if (_config.SavedServers.Count == 0)
        {
            MessageBox.Query(_app, "Saved Servers", "No saved servers yet.\nConnect to a server to save it automatically.", "OK");
            return;
        }

        var serverLines = _config.SavedServers
            .Select(s => $"{s.Name} ({s.Url}) - {s.Username ?? "?"} - {s.LastConnected:yyyy-MM-dd}")
            .ToList();

        var message = string.Join("\n", serverLines);
        MessageBox.Query(_app, "Saved Servers", message, "OK");
    }

    // ── Connection Event Wiring ────────────────────────────────────────────

    private static void WireConnectionEvents(EchoHubConnection connection)
    {
        connection.OnMessageReceived += message =>
        {
            _app.Invoke(() =>
                _mainWindow!.AddMessage(message));
        };

        connection.OnUserJoined += (channelName, username) =>
        {
            _app.Invoke(() =>
                _mainWindow!.AddSystemMessage(channelName, $"{username} joined the channel"));
        };

        connection.OnUserLeft += (channelName, username) =>
        {
            _app.Invoke(() =>
                _mainWindow!.AddSystemMessage(channelName, $"{username} left the channel"));
        };

        connection.OnUserStatusChanged += presence =>
        {
            _app.Invoke(() =>
            {
                var displayName = presence.DisplayName ?? presence.Username;
                var statusText = presence.Status.ToString();
                if (!string.IsNullOrWhiteSpace(presence.StatusMessage))
                    statusText += $" - {presence.StatusMessage}";

                // Show status change in all active channels
                foreach (var channelName in _mainWindow!.GetChannelNames())
                {
                    _mainWindow.AddStatusMessage(channelName, displayName, statusText);
                }
            });
        };

        connection.OnError += errorMessage =>
        {
            _app.Invoke(() =>
                _mainWindow!.ShowError(errorMessage));
        };

        connection.OnConnectionStateChanged += status =>
        {
            _app.Invoke(() =>
                _mainWindow!.UpdateStatusBar(status));
        };
    }
}
