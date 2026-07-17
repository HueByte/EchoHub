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
using EchoHub.Core.Security;
using EchoHub.Core.Services;
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
    private readonly Dictionary<string, List<UserPresenceDto>> _channelUsers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _channelUsersLock = new();
    private readonly HashSet<string> _channelsLoadingMore = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _stagedAttachments = [];

    // Temp PNGs created for clipboard-image pastes; deleted once their message is sent
    // (or the staging tray is cleared) so pasted screenshots don't pile up in %TEMP%.
    private readonly HashSet<string> _tempPastedFiles = [];

    // E2E channels whose unlock prompt the user cancelled — don't nag on every reselect.
    // Cleared on connect/reconnect; an explicit /join or a send attempt re-offers the prompt.
    private readonly HashSet<string> _declinedUnlocks = new(StringComparer.OrdinalIgnoreCase);

    private ClientConfig _config;
    private readonly UserSession _session = new();

    public MainWindow MainWindow => _mainWindow;

    /// <summary>
    /// Set when the user confirms an update. The host must run this after the Terminal.Gui main
    /// loop exits (console restored), so the updater's in-place restart doesn't fight the TUI.
    /// </summary>
    public Func<Task>? PendingUpdate => _updateService.PendingUpdate;

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
        // Quit while still connected — capture read positions before tearing down
        PersistLastReads();
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
        _mainWindow.OnFilesStaged += HandleFilesStaged;
        _mainWindow.OnImagePasted += HandleImagePasted;
        _mainWindow.OnChannelSelected += HandleChannelSelected;
        _mainWindow.OnProfileRequested += HandleProfileRequested;
        _mainWindow.OnStatusRequested += HandleStatusRequested;
        _mainWindow.OnThemeSelected += HandleThemeSelected;
        _mainWindow.OnSavedServersRequested += HandleSavedServersRequested;
        _mainWindow.OnCreateChannelRequested += HandleCreateChannelRequested;
        _mainWindow.OnDeleteChannelRequested += HandleDeleteChannelRequested;
        _mainWindow.OnAudioPlayRequested += HandleAudioPlayRequested;
        _mainWindow.OnFileDownloadRequested += HandleFileDownloadRequested;
        _mainWindow.OnImageSaveRequested += HandleImageSaveRequested;
        _mainWindow.OnImageOpenRequested += HandleImageOpenRequested;
        _mainWindow.OnDeleteMessageRequested += HandleDeleteMessageRequested;
        _mainWindow.OnCheckForUpdatesRequested += HandleCheckForUpdatesRequested;
        _mainWindow.OnRollbackRequested += HandleRollbackRequested;
        _mainWindow.OnUserProfileRequested += HandleViewProfile;
        _mainWindow.OnChannelJoinRequested += HandleChannelJoinFromMessage;
        _mainWindow.OnSearchRequested += HandleSearchRequested;
        _mainWindow.OnLoadMoreRequested += HandleLoadMoreRequested;
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
        _commandHandler.OnChangeRoomPassword += HandleCmdChangeRoomPassword;
        _commandHandler.OnClearAttachments += HandleCmdClearAttachments;
        _commandHandler.OnSetAsciiSize += HandleCmdSetAsciiSize;
        _commandHandler.OnSetDownloadPath += HandleCmdSetDownloadPath;
        _commandHandler.OnLeaveChannel += HandleCmdLeaveChannel;
        _commandHandler.OnSetTopic += HandleCmdSetTopic;
        _commandHandler.OnListUsers += HandleCmdListUsers;
        _commandHandler.OnRoomInfo += HandleCmdMeta;
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

    private Task HandleCmdSendFile(string target, string? size)
    {
        if (!_conn.IsAuthenticated || !_conn.IsConnected) return Task.CompletedTask;

        var channel = _mainWindow.CurrentChannel;
        if (string.IsNullOrEmpty(channel)) return Task.CompletedTask;

        // A URL image is sent immediately as its own message (it can't be staged/encrypted).
        if (Uri.TryCreate(target, UriKind.Absolute, out var uri)
            && (uri.Scheme == "http" || uri.Scheme == "https"))
        {
            // Also blocks locked E2E channels (no cached key) — a URL send would be plaintext
            if (_conn.RoomKeys.HasKey(channel) || _conn.RoomKeys.IsChannelEncrypted(channel))
            {
                InvokeUI(() => _mainWindow.ShowError(
                    "Sending by URL isn't available in encrypted channels — download the file and /send it instead."));
                return Task.CompletedTask;
            }

            RunAsync(async () => await _conn.Api!.SendUrlAsync(channel, target, size), "Send failed");
            return Task.CompletedTask;
        }

        // Local files are staged; the next Enter sends them with the typed caption as one message.
        if (_stagedAttachments.Count >= HubConstants.MaxAttachmentsPerMessage)
        {
            InvokeUI(() => _mainWindow.ShowError($"You can attach at most {HubConstants.MaxAttachmentsPerMessage} files per message."));
            return Task.CompletedTask;
        }

        // An explicit "-s/-m/-l" on /send also sets the message's ASCII size.
        if (NormalizeAsciiSize(size) is { } flag)
            _config.DefaultAsciiSize = flag;

        _stagedAttachments.Add(target);
        InvokeUI(RefreshStagingTray);
        return Task.CompletedTask;
    }

    private Task HandleCmdClearAttachments()
    {
        var temps = _stagedAttachments.Where(_tempPastedFiles.Contains).ToList();
        _tempPastedFiles.ExceptWith(temps);
        CleanupPastedTempFiles(temps);

        _stagedAttachments.Clear();
        InvokeUI(RefreshStagingTray);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stages a batch of local files (multi-file paste or drag-and-drop) as attachments of the
    /// next message. Runs synchronously on the UI thread — unlike routing each file through a
    /// fire-and-forget /send command, a 10-file paste can't race the staging list.
    /// </summary>
    private void HandleFilesStaged(string channel, IReadOnlyList<string> files)
    {
        if (!_conn.IsAuthenticated || !_conn.IsConnected)
            return;

        var slotsLeft = HubConstants.MaxAttachmentsPerMessage - _stagedAttachments.Count;
        _stagedAttachments.AddRange(files.Take(Math.Max(0, slotsLeft)));

        if (files.Count > slotsLeft)
            _mainWindow.ShowError($"You can attach at most {HubConstants.MaxAttachmentsPerMessage} files per message.");

        RefreshStagingTray();
    }

    /// <summary>
    /// Stages an image pasted as raw clipboard data (copied from a browser, a screenshot tool,
    /// or an image editor). The PNG is written to a per-paste temp folder so it flows through
    /// the same path-based staging/encryption pipeline as regular files, and the temp file is
    /// deleted once the message is sent.
    /// </summary>
    private void HandleImagePasted(string channel, byte[] png)
    {
        if (!_conn.IsAuthenticated || !_conn.IsConnected)
            return;

        if (_stagedAttachments.Count >= HubConstants.MaxAttachmentsPerMessage)
        {
            _mainWindow.ShowError($"You can attach at most {HubConstants.MaxAttachmentsPerMessage} files per message.");
            return;
        }

        try
        {
            // A unique folder per paste keeps the Discord-style "image.png" display name
            // while letting several pasted images coexist in one message.
            var dir = Path.Combine(Path.GetTempPath(), "EchoHub", "pasted", Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "image.png");
            File.WriteAllBytes(path, png);

            _tempPastedFiles.Add(path);
            _stagedAttachments.Add(path);
            RefreshStagingTray();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Staging a pasted clipboard image failed");
            _mainWindow.ShowError($"Pasting image failed: {ex.Message}");
        }
    }

    /// <summary>Best-effort removal of pasted-image temp files and their per-paste folders.</summary>
    private static void CleanupPastedTempFiles(IReadOnlyList<string> files)
    {
        foreach (var file in files)
        {
            try
            {
                File.Delete(file);
                if (Path.GetDirectoryName(file) is { } dir)
                    Directory.Delete(dir);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Could not delete pasted-image temp file {File}", file);
            }
        }
    }

    /// <summary>
    /// Opens the ASCII-art size picker (no argument) or sets it directly from "s"/"m"/"l" (or
    /// small/medium/large). The choice is a persistent preference applied to attached images.
    /// </summary>
    private Task HandleCmdSetAsciiSize(string args)
    {
        var flag = NormalizeAsciiSize(args);
        if (flag is not null)
        {
            InvokeUI(() => ApplyAsciiSize(flag));
            return Task.CompletedTask;
        }

        InvokeUI(() =>
        {
            var choice = MessageBox.Query(_app, "ASCII Art Size",
                "Size of the ASCII rendering for images you attach:\n\n"
                + "  Small    40 x 40    (compact)\n"
                + "  Medium   80 x 80    (default)\n"
                + "  Large    120 x 120  (detailed)",
                "Small", "Medium", "Large", "Cancel");

            var picked = choice switch { 0 => "s", 1 => "m", 2 => "l", _ => null };
            if (picked is not null)
                ApplyAsciiSize(picked);
        });
        return Task.CompletedTask;
    }

    private void ApplyAsciiSize(string flag)
    {
        _config.DefaultAsciiSize = flag;
        ConfigManager.Save(_config);
        RefreshStagingTray();

        var channel = _mainWindow.CurrentChannel;
        if (!string.IsNullOrEmpty(channel))
            _messageManager.AddSystemMessage(channel, $"Image ASCII size set to {AsciiSizeLabel(flag)}.");
    }

    /// <summary>Refreshes the staging tray with the current staged files and ASCII size.</summary>
    private void RefreshStagingTray()
    {
        var names = _stagedAttachments.Select(Path.GetFileName).OfType<string>().ToList();
        _mainWindow.SetStagedAttachments(names, AsciiSizeLabel(_config.DefaultAsciiSize));
    }

    private static string? NormalizeAsciiSize(string? size) => size?.Trim().ToLowerInvariant() switch
    {
        "s" or "small" => "s",
        "m" or "medium" => "m",
        "l" or "large" => "l",
        _ => null,
    };

    private static string AsciiSizeLabel(string flag) => flag switch
    {
        "s" => "Small (40x40)",
        "l" => "Large (120x120)",
        _ => "Medium (80x80)",
    };

    /// <summary>
    /// Sends one message with the given caption plus all staged files as attachments, then
    /// clears the staging tray. In encrypted channels each file is room-encrypted (blob +
    /// ASCII preview) client-side before upload; the caption is room-encrypted too.
    /// </summary>
    private void SendStagedMessage(string channel, string content)
    {
        var size = _config.DefaultAsciiSize;

        RunAsync(async () =>
        {
            // Locked E2E channel: block the send and keep the files staged for after the unlock
            if (!await EnsureRoomUnlockedForSendAsync(channel))
                return;

            var staged = _stagedAttachments.ToList();
            _stagedAttachments.Clear();
            InvokeUI(RefreshStagingTray);

            // Pasted clipboard images live in temp files; once this send owns them they are
            // deleted whether the upload succeeds or fails (the tray is already cleared).
            var tempFiles = staged.Where(_tempPastedFiles.Contains).ToList();
            _tempPastedFiles.ExceptWith(tempFiles);

            try
            {
                var hasRoomKey = _conn.RoomKeys.TryGetKey(channel, out var roomKey);

                var outgoing = new List<OutgoingAttachment>();
                foreach (var path in staged)
                    outgoing.Add(await BuildOutgoingAttachmentAsync(path, hasRoomKey ? roomKey : null, size));

                var wireContent = hasRoomKey && !string.IsNullOrEmpty(content)
                    ? RoomCrypto.EncryptText(content, roomKey)
                    : content;

                await _conn.Api!.SendMessageWithAttachmentsAsync(channel, wireContent, outgoing, size);
            }
            finally
            {
                CleanupPastedTempFiles(tempFiles);
            }
        }, "Send failed");
    }

    /// <summary>
    /// Reads a staged file into an <see cref="OutgoingAttachment"/>. For encrypted channels the
    /// blob is AES-GCM encrypted, its kind is declared, and the image ASCII preview is rendered
    /// locally (at <paramref name="size"/>) and room-encrypted — so the server never sees the
    /// file or image contents.
    /// </summary>
    private static async Task<OutgoingAttachment> BuildOutgoingAttachmentAsync(string path, byte[]? roomKey, string size)
    {
        var fileName = Path.GetFileName(path);

        if (roomKey is null)
            return new OutgoingAttachment(File.OpenRead(path), fileName);

        var bytes = await File.ReadAllBytesAsync(path);
        string declaredKind;
        string? preview = null;

        using (var ms = new MemoryStream(bytes))
        {
            if (FileValidationHelper.IsValidImage(ms))
            {
                declaredKind = "image";
                var (w, h) = ImageToAsciiService.GetDimensions(size);
                ms.Position = 0;
                preview = RoomCrypto.EncryptText(new ImageToAsciiService().ConvertToAscii(ms, w, h), roomKey);
            }
            else
            {
                declaredKind = FileValidationHelper.IsAudioFile(fileName) ? "audio" : "file";
            }
        }

        var encryptedBlob = RoomCrypto.EncryptBytes(bytes, roomKey);
        return new OutgoingAttachment(new MemoryStream(encryptedBlob), fileName, declaredKind, preview);
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

    private async Task HandleCmdJoinChannel(string channelName, string? password)
    {
        if (!_conn.IsConnected) return;

        try
        {
            var history = await JoinChannelWithPasswordPromptAsync(channelName, password);
            if (history is null) return; // user cancelled the password prompt

            // A deliberate join cancels any earlier /leave exclusion
            UpdateServerConfig(server =>
                server.LeftChannels.RemoveAll(c => c.Equals(channelName, StringComparison.OrdinalIgnoreCase)));

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

    /// <summary>
    /// Joins a channel, prompting for a password when the server requires one and
    /// re-prompting on a wrong password. For end-to-end encrypted channels the typed
    /// passphrase never goes to the server — a PBKDF2-derived auth key is sent instead,
    /// and the room content key is unwrapped locally. Returns the channel history,
    /// or null if the user cancelled the prompt.
    /// </summary>
    private async Task<List<MessageDto>?> JoinChannelWithPasswordPromptAsync(string channelName, string? password)
    {
        ChannelCryptoDto? crypto = null;
        try
        {
            crypto = await _conn.Api!.GetChannelCryptoAsync(channelName);
            if (crypto is not null)
                _conn.RoomKeys.MarkChannelEncrypted(channelName, crypto.IsEncrypted);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Crypto metadata unavailable for {Channel}", channelName);
        }

        while (true)
        {
            byte[]? kek = null;
            var wirePassword = password;
            if (password is not null && crypto is { IsEncrypted: true, EncryptionSalt: not null })
            {
                var derived = RoomCrypto.DeriveKeys(password, Convert.FromBase64String(crypto.EncryptionSalt));
                wirePassword = derived.AuthKeyHex;
                kek = derived.KeyEncryptionKey;
            }

            try
            {
                var outcome = await _conn.JoinChannelAsync(channelName, wirePassword);

                if (outcome.WrappedRoomKey is not null)
                {
                    // A typed passphrase always wins over the cache: unwrap the fresh envelope
                    // and overwrite any stale key (e.g. the channel was deleted and recreated
                    // under the same name — the old key would encrypt for nobody).
                    if (kek is not null && _conn.RoomKeys.TryStoreFromEnvelope(channelName, outcome.WrappedRoomKey, kek))
                    {
                        lock (_declinedUnlocks) _declinedUnlocks.Remove(channelName);
                        // Re-fetch so history decrypts with the now-available room key
                        return await _conn.GetHistoryAsync(channelName);
                    }

                    if (!_conn.RoomKeys.HasKey(channelName))
                        return await UnlockRoomKeyAsync(channelName, outcome);
                }

                return outcome.History;
            }
            catch (ChannelPasswordRequiredException ex)
            {
                var prompt = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
                var message = password is not null ? ex.Message : null;
                InvokeUI(() => prompt.SetResult(ChannelPasswordDialog.Show(_app, channelName, message)));

                password = await prompt.Task;
                if (password is null) return null;
            }
        }
    }

    /// <summary>
    /// Member of an encrypted channel without a cached room key (e.g. a new device):
    /// prompt for the passphrase until the room key unwraps or the user gives up.
    /// </summary>
    private async Task<List<MessageDto>?> UnlockRoomKeyAsync(string channelName, JoinOutcome outcome)
    {
        if (outcome.EncryptionSalt is null || outcome.WrappedRoomKey is null)
            return outcome.History;

        var salt = Convert.FromBase64String(outcome.EncryptionSalt);
        var message = "Enter the passphrase to unlock messages.";

        while (true)
        {
            var prompt = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var promptMessage = message;
            InvokeUI(() => prompt.SetResult(ChannelPasswordDialog.Show(_app, channelName, promptMessage)));

            var passphrase = await prompt.Task;
            if (passphrase is null)
            {
                // Stays locked; placeholders render instead of content. Remember the decline
                // so reselecting the channel doesn't nag every time.
                lock (_declinedUnlocks) _declinedUnlocks.Add(channelName);
                return outcome.History;
            }

            var derived = RoomCrypto.DeriveKeys(passphrase, salt);
            if (_conn.RoomKeys.TryStoreFromEnvelope(channelName, outcome.WrappedRoomKey, derived.KeyEncryptionKey))
            {
                lock (_declinedUnlocks) _declinedUnlocks.Remove(channelName);
                return await _conn.GetHistoryAsync(channelName);
            }

            message = "Wrong passphrase — try again.";
        }
    }

    /// <summary>
    /// True when a channel is end-to-end encrypted, its room key isn't cached, and the
    /// user hasn't already declined the unlock prompt this session.
    /// </summary>
    private bool NeedsUnlockPrompt(string channelName)
    {
        if (!_conn.RoomKeys.IsChannelEncrypted(channelName) || _conn.RoomKeys.HasKey(channelName))
            return false;

        lock (_declinedUnlocks) return !_declinedUnlocks.Contains(channelName);
    }

    /// <summary>
    /// Unlock flow for a channel that is already hub-joined (auto-join/reconnect discard
    /// the key envelope): rejoin — members pass the gate without a password and the join
    /// result carries the envelope — then run the passphrase prompt. On success the
    /// decrypted history replaces the locked placeholders. Returns true when unlocked.
    /// </summary>
    private async Task<bool> UnlockTrackedChannelAsync(string channelName)
    {
        try
        {
            var outcome = await _conn.JoinChannelAsync(channelName, null);
            if (outcome.WrappedRoomKey is null)
                return false; // not an E2E channel after all

            var history = await UnlockRoomKeyAsync(channelName, outcome);
            if (!_conn.RoomKeys.HasKey(channelName))
                return false; // cancelled or never unwrapped

            if (history is not null)
                InvokeUI(() => _messageManager.LoadHistory(channelName, history));
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Unlock flow failed for #{Channel}", channelName);
            return false;
        }
    }

    /// <summary>
    /// Send guard for end-to-end encrypted channels: without the cached room key nothing
    /// may leave the client (it would be plaintext in a room others read as encrypted).
    /// Offers the unlock prompt right away — even after an earlier decline, since the user
    /// is actively trying to talk here. Returns true when sending is safe.
    /// </summary>
    private async Task<bool> EnsureRoomUnlockedForSendAsync(string channelName)
    {
        if (!_conn.RoomKeys.IsChannelEncrypted(channelName) || _conn.RoomKeys.HasKey(channelName))
            return true;

        if (await UnlockTrackedChannelAsync(channelName))
            return true;

        InvokeUI(() => _mainWindow.ShowError(
            $"#{channelName} is end-to-end encrypted and locked — nothing was sent. Enter its passphrase to unlock it first."));
        return false;
    }

    /// <summary>
    /// Changes the current encrypted channel's passphrase: re-derives the join credential
    /// and re-wraps the cached room content key under the new passphrase. History is
    /// never re-encrypted — the room key itself doesn't change.
    /// </summary>
    private async Task HandleCmdChangeRoomPassword(string oldPassphrase, string newPassphrase)
    {
        if (!_conn.IsAuthenticated || !_conn.IsConnected) return;

        var channel = _mainWindow.CurrentChannel;
        if (string.IsNullOrEmpty(channel)) return;

        try
        {
            var crypto = await _conn.Api!.GetChannelCryptoAsync(channel);
            if (crypto is not { IsEncrypted: true } || crypto.EncryptionSalt is null)
            {
                InvokeUI(() => _mainWindow.ShowError($"#{channel} is not an end-to-end encrypted channel."));
                return;
            }

            if (!_conn.RoomKeys.TryGetKey(channel, out var roomKey))
            {
                InvokeUI(() => _mainWindow.ShowError("Unlock this channel first (rejoin it with its passphrase), then retry."));
                return;
            }

            var oldDerived = RoomCrypto.DeriveKeys(oldPassphrase, Convert.FromBase64String(crypto.EncryptionSalt));
            var newSalt = RoomCrypto.GenerateSalt();
            var newDerived = RoomCrypto.DeriveKeys(newPassphrase, newSalt);

            await _conn.Api!.RekeyChannelAsync(channel, new RekeyChannelRequest(
                oldDerived.AuthKeyHex,
                newDerived.AuthKeyHex,
                Convert.ToBase64String(newSalt),
                RoomCrypto.WrapRoomKey(roomKey, newDerived.KeyEncryptionKey)));

            InvokeUI(() => _messageManager.AddSystemMessage(channel,
                "Passphrase changed. History stays readable; new members and new devices need the new passphrase."));
        }
        catch (Exception ex)
        {
            InvokeUI(() => _mainWindow.ShowError($"Passphrase change failed: {ex.Message}"));
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

            // Remember the leave so the connect-time auto-join doesn't pull us back in
            UpdateServerConfig(server =>
            {
                if (!server.LeftChannels.Contains(channel, StringComparer.OrdinalIgnoreCase))
                    server.LeftChannels.Add(channel);
            });

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

    private async Task HandleCmdMeta()
    {
        if (!_conn.IsConnected || _conn.Api is null) return;

        var channel = _mainWindow.CurrentChannel;
        if (string.IsNullOrEmpty(channel)) return;

        try
        {
            var meta = await _conn.Api.GetChannelMetaAsync(channel);
            if (meta is null)
            {
                InvokeUI(() => _mainWindow.ShowError($"Channel #{channel} not found."));
                return;
            }

            var size = meta.EstimatedSizeBytes <= 0 ? "0 B" : ChatMessageManager.FormatFileSize(meta.EstimatedSizeBytes);
            var protection = meta.IsEncrypted ? "end-to-end encrypted"
                : meta.IsProtected ? "password-protected"
                : "open";

            InvokeUI(() =>
            {
                _messageManager.AddSystemMessage(channel, $"Room info for #{meta.Name}:");
                if (!string.IsNullOrWhiteSpace(meta.Topic))
                    _messageManager.AddSystemMessage(channel, $"  Topic         {meta.Topic}");
                _messageManager.AddSystemMessage(channel, $"  Room ID       {meta.Id}");
                _messageManager.AddSystemMessage(channel, $"  Created       {meta.CreatedAt.ToLocalTime():g}");
                _messageManager.AddSystemMessage(channel, $"  Messages      {meta.MessageCount}");
                _messageManager.AddSystemMessage(channel, $"  Unique users  {meta.UniqueUserCount}");
                _messageManager.AddSystemMessage(channel, $"  Est. size     {size}");
                _messageManager.AddSystemMessage(channel, $"  Protection    {protection}");
            });
        }
        catch (Exception ex)
        {
            InvokeUI(() => _mainWindow.ShowError($"Failed to fetch room info: {ex.Message}"));
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

        _conn.UserJoined += (channelName, username, presence) =>
        {
            InvokeUI(() => _messageManager.AddSystemMessage(channelName, $"{username} joined the channel"));

            List<UserPresenceDto>? snapshot = null;
            lock (_channelUsersLock)
            {
                if (presence is not null && _channelUsers.TryGetValue(channelName, out var users))
                {
                    if (!users.Any(u => u.Username.Equals(presence.Username, StringComparison.OrdinalIgnoreCase)))
                        users.Add(presence);

                    if (channelName.Equals(_mainWindow.CurrentChannel, StringComparison.OrdinalIgnoreCase))
                        snapshot = [.. users];
                }
            }

            if (snapshot is not null)
                InvokeUI(() => _mainWindow.UpdateOnlineUsers(snapshot));
            else if (channelName.Equals(_mainWindow.CurrentChannel, StringComparison.OrdinalIgnoreCase))
                FetchAndUpdateOnlineUsers();
        };

        _conn.UserLeft += (channelName, username) =>
        {
            InvokeUI(() => _messageManager.AddSystemMessage(channelName, $"{username} left the channel"));

            List<UserPresenceDto>? snapshot = null;
            lock (_channelUsersLock)
            {
                if (_channelUsers.TryGetValue(channelName, out var users))
                {
                    users.RemoveAll(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                    if (channelName.Equals(_mainWindow.CurrentChannel, StringComparison.OrdinalIgnoreCase))
                        snapshot = [.. users];
                }
            }

            if (snapshot is not null)
                InvokeUI(() => _mainWindow.UpdateOnlineUsers(snapshot));
            else if (channelName.Equals(_mainWindow.CurrentChannel, StringComparison.OrdinalIgnoreCase))
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

            // Update presence in all cached channel lists
            List<UserPresenceDto>? snapshot = null;
            lock (_channelUsersLock)
            {
                foreach (var (channel, users) in _channelUsers)
                {
                    var idx = users.FindIndex(u => u.Username.Equals(presence.Username, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0)
                    {
                        if (presence.Status == UserStatus.Invisible)
                            users.RemoveAt(idx);
                        else
                            users[idx] = presence;
                    }
                    else if (presence.Status != UserStatus.Invisible)
                    {
                        // User came back from invisible — re-add them
                        users.Add(presence);
                    }
                }

                var currentChannel = _mainWindow.CurrentChannel;
                if (!string.IsNullOrEmpty(currentChannel) && _channelUsers.TryGetValue(currentChannel, out var currentUsers))
                    snapshot = [.. currentUsers];
            }

            if (snapshot is not null)
                InvokeUI(() => _mainWindow.UpdateOnlineUsers(snapshot));
        };

        _conn.UserKicked += (channelName, username, reason) =>
        {
            var reasonText = reason is not null ? $" ({reason})" : "";
            InvokeUI(() => _messageManager.AddSystemMessage(channelName, $"{username} was kicked{reasonText}"));

            List<UserPresenceDto>? snapshot = null;
            lock (_channelUsersLock)
            {
                if (_channelUsers.TryGetValue(channelName, out var users))
                {
                    users.RemoveAll(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                    if (channelName.Equals(_mainWindow.CurrentChannel, StringComparison.OrdinalIgnoreCase))
                        snapshot = [.. users];
                }
            }

            if (snapshot is not null)
                InvokeUI(() => _mainWindow.UpdateOnlineUsers(snapshot));
        };

        _conn.UserBanned += (username, reason) =>
        {
            var reasonText = reason is not null ? $" ({reason})" : "";

            List<UserPresenceDto>? snapshot = null;
            lock (_channelUsersLock)
            {
                // Remove banned user from all cached channel lists
                foreach (var (channel, users) in _channelUsers)
                {
                    users.RemoveAll(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                }

                var currentChannel = _mainWindow.CurrentChannel;
                if (!string.IsNullOrEmpty(currentChannel) && _channelUsers.TryGetValue(currentChannel, out var currentUsers))
                    snapshot = [.. currentUsers];
            }

            InvokeUI(() =>
            {
                if (!username.Equals(_session.Username, StringComparison.OrdinalIgnoreCase))
                {
                    var channel = _mainWindow.CurrentChannel;
                    if (!string.IsNullOrEmpty(channel))
                        _messageManager.AddSystemMessage(channel, $"{username} was banned{reasonText}");

                    if (snapshot is not null)
                        _mainWindow.UpdateOnlineUsers(snapshot);
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

        _conn.ChannelDeleted += channelName =>
        {
            InvokeUI(() =>
            {
                _conn.UntrackChannel(channelName);
                _mainWindow.RemoveChannel(channelName);
            });
        };

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
                    _mainWindow.EnsureChannelInList(channel.Name, channel.IsPublic, channel.IsProtected);
                _mainWindow.SetChannelTopic(channel.Name, channel.Topic);
            });
        };

        _conn.Error += errorMessage =>
            InvokeUI(() => _mainWindow.ShowError(errorMessage));

        _conn.ConnectionStatusChanged += status =>
            InvokeUI(() => _mainWindow.UpdateStatusBar(status));

        _conn.Reconnected += () =>
        {
            lock (_channelUsersLock) _channelUsers.Clear();
            lock (_declinedUnlocks) _declinedUnlocks.Clear();
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
            lock (_declinedUnlocks) _declinedUnlocks.Clear();

            // Persisted last-read markers for this server — used to seed unread counts,
            // mention highlights, and "new messages" markers from the fetched histories.
            var lastReads = ConfigManager.Load().SavedServers
                .FirstOrDefault(s => string.Equals(s.Url, dialogResult.ServerUrl, StringComparison.OrdinalIgnoreCase))
                ?.LastReadMessages ?? [];

            InvokeUI(() =>
            {
                _mainWindow.SetCurrentUser(result.Login.DisplayName ?? result.Login.Username);
                _mainWindow.SetChannels(result.Channels);
                _mainWindow.SwitchToChannel(HubConstants.DefaultChannel);

                foreach (var (channel, history) in result.Histories)
                {
                    if (history.Count == 0)
                        continue;

                    Guid? lastRead = lastReads.TryGetValue(channel, out var idText)
                        && Guid.TryParse(idText, out var id) ? id : null;
                    _messageManager.LoadHistory(channel, history, lastRead);
                }

                _mainWindow.FocusInput();
                FetchAndUpdateOnlineUsers();
            });
            SaveServerToConfig(dialogResult);
        }, "Connection failed", "Connect");
    }

    private void HandleDisconnect()
    {
        Log.Information("Disconnecting from server");
        lock (_channelUsersLock) _channelUsers.Clear();
        PersistLastReads();

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
        PersistLastReads();

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

        // Staged files → one message with the typed caption plus those attachments.
        if (_stagedAttachments.Count > 0)
        {
            SendStagedMessage(channelName, content);
            return;
        }

        RunAsync(async () =>
        {
            if (!await EnsureRoomUnlockedForSendAsync(channelName))
                return;
            await _conn.SendMessageAsync(channelName, content);
        }, "Send failed");
    }

    private void HandleDeleteMessageRequested(Guid messageId)
    {
        if (!_conn.IsAuthenticated) return;

        // The server enforces the hierarchy rule (own message, or Mod+ over a strictly
        // lower role) and broadcasts the deletion; the local list updates on that event.
        RunAsync(async () => await _conn.Api!.DeleteMessageAsync(messageId),
            "Failed to delete message");
    }

    private void HandleChannelSelected(string channelName)
    {
        if (!_conn.IsConnected) return;

        // Checkpoint read positions — the previous channel was just marked read
        PersistLastReads();

        RunAsync(async () =>
        {
            if (_conn.TrackChannel(channelName))
            {
                var joined = await JoinChannelWithPasswordPromptAsync(channelName, null);
                if (joined is null)
                {
                    // User cancelled the password prompt — back to the default channel
                    _conn.UntrackChannel(channelName);
                    InvokeUI(() => _mainWindow.SwitchToChannel(HubConstants.DefaultChannel));
                    return;
                }

                // A deliberate join cancels any earlier /leave exclusion
                UpdateServerConfig(server =>
                    server.LeftChannels.RemoveAll(c => c.Equals(channelName, StringComparison.OrdinalIgnoreCase)));
            }
            else if (NeedsUnlockPrompt(channelName))
            {
                // Auto-join/reconnect already hub-joined this E2E channel but discarded the
                // key envelope — selecting it is the user's cue to unlock it.
                await UnlockTrackedChannelAsync(channelName);
            }

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

    private void HandleLoadMoreRequested()
    {
        if (!_conn.IsConnected) return;

        var channel = _mainWindow.CurrentChannel;
        if (string.IsNullOrEmpty(channel)) return;

        if (!_channelsLoadingMore.Add(channel)) return;

        var offset = _messageManager.GetMessages(channel)?.Count ?? 0;

        RunAsync(async () =>
        {
            try
            {
                var history = await _conn.GetHistoryAsync(channel, HubConstants.DefaultHistoryCount, offset);
                InvokeUI(() => _messageManager.PrependHistory(channel, history));
            }
            finally
            {
                _channelsLoadingMore.Remove(channel);
            }
        }, "Failed to load more messages");
    }

    private void HandleChannelJoinFromMessage(string channelName)
    {
        if (!_conn.IsConnected) return;

        InvokeUI(() =>
        {
            _mainWindow.EnsureChannelInList(channelName);
            _mainWindow.SwitchToChannel(channelName);
        });

        HandleChannelSelected(channelName);
    }


    private void HandleSearchRequested()
    {
        var result = SearchDialog.Show(_app, _mainWindow.GetChannelNames());
        if (result is null) return;

        switch (result.Type)
        {
            case SearchResultType.Channel:
                _mainWindow.SwitchToChannel(result.Key);
                HandleChannelSelected(result.Key);
                break;

            case SearchResultType.Action:
                switch (result.Key)
                {
                    case "connect": HandleConnect(); break;
                    case "disconnect": HandleDisconnect(); break;
                    case "logout": HandleLogout(); break;
                    case "profile": HandleProfileRequested(); break;
                    case "status": HandleStatusRequested(); break;
                    case "create-channel": HandleCreateChannelRequested(); break;
                    case "delete-channel": HandleDeleteChannelRequested(); break;
                    case "servers": HandleSavedServersRequested(); break;
                    case "toggle-users": _mainWindow.ToggleUsersPanel(); break;
                    case "updates": HandleCheckForUpdatesRequested(); break;
                    case "quit": _app.RequestStop(); break;
                }
                break;
        }
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
            // Password rooms are end-to-end encrypted: derive the join credential and
            // wrap a fresh room content key locally — the passphrase never leaves here.
            string? wirePassword = null, saltB64 = null, wrappedKey = null;
            byte[]? roomKey = null;
            if (result.Password is not null)
            {
                if (result.Password.Length < ValidationConstants.MinChannelPasswordLength)
                {
                    InvokeUI(() => _mainWindow.ShowError(
                        $"Channel password must be at least {ValidationConstants.MinChannelPasswordLength} characters."));
                    return;
                }

                var salt = RoomCrypto.GenerateSalt();
                var derived = RoomCrypto.DeriveKeys(result.Password, salt);
                roomKey = RoomCrypto.GenerateRoomKey();
                wirePassword = derived.AuthKeyHex;
                saltB64 = Convert.ToBase64String(salt);
                wrappedKey = RoomCrypto.WrapRoomKey(roomKey, derived.KeyEncryptionKey);
            }

            var channel = await _conn.Api!.CreateChannelAsync(
                result.Name, result.Topic, result.IsPublic, wirePassword, saltB64, wrappedKey);
            if (channel is null) return;

            if (roomKey is not null)
                _conn.RoomKeys.StoreKey(channel.Name, roomKey);

            var history = (await _conn.JoinChannelAsync(channel.Name)).History;

            InvokeUI(() =>
            {
                _mainWindow.EnsureChannelInList(channel.Name, channel.IsPublic, channel.IsProtected);
                _mainWindow.SetChannelTopic(channel.Name, channel.Topic);
                _mainWindow.SwitchToChannel(channel.Name);
                if (history.Count > 0)
                    _messageManager.LoadHistory(channel.Name, history);
            });

            FetchAndUpdateOnlineUsers();
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
            var tempPath = await DownloadAttachmentAsync(attachmentUrl, fileName);
            InvokeUI(() => AudioPlayerDialog.Show(_app, _audioPlayback, tempPath, fileName));
        }, "Failed to play audio");
    }

    /// <summary>
    /// Downloads an attachment to a temp file, decrypting it locally when the current
    /// channel is end-to-end encrypted (the server stores those blobs as ciphertext).
    /// </summary>
    private async Task<string> DownloadAttachmentAsync(string attachmentUrl, string fileName)
    {
        var tempPath = await _conn.Api!.DownloadFileToTempAsync(attachmentUrl, fileName);

        var channel = _mainWindow.CurrentChannel;
        if (!string.IsNullOrEmpty(channel) && _conn.RoomKeys.TryGetKey(channel, out var roomKey))
        {
            try
            {
                var blob = await File.ReadAllBytesAsync(tempPath);
                await File.WriteAllBytesAsync(tempPath, RoomCrypto.DecryptBytes(blob, roomKey));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Attachment {File} did not decrypt with the room key — keeping raw bytes", fileName);
            }
        }

        return tempPath;
    }

    /// <summary>File extensions the "[open]" action will hand to the OS image viewer for E2E rooms.</summary>
    private static readonly HashSet<string> ImageOpenExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp",
    };

    /// <summary>
    /// Views an image without saving it to the user's downloads. Plain channels open the
    /// file's web URL in the default browser (the server serves files by capability URL, so
    /// no auth token is needed). E2E-encrypted channels would render as ciphertext in a
    /// browser, so the blob is downloaded, decrypted locally, and opened from a temp file.
    /// </summary>
    private void HandleImageOpenRequested(string attachmentUrl, string fileName)
    {
        if (!_conn.IsAuthenticated) return;

        var channel = _mainWindow.CurrentChannel;
        var isEncryptedRoom = !string.IsNullOrEmpty(channel) && _conn.RoomKeys.TryGetKey(channel, out _);

        if (!isEncryptedRoom)
        {
            var webUrl = attachmentUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || attachmentUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? attachmentUrl
                : $"{_conn.Api!.BaseUrl}/{attachmentUrl.TrimStart('/')}";

            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(webUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to open image URL in browser: {Url}", webUrl);
                InvokeUI(() => _messageManager.AddSystemMessage(channel, $"Couldn't open a browser — image URL: {webUrl}"));
            }
            return;
        }

        // In E2E rooms the attachment kind is sender-declared, so only hand real image
        // extensions to the OS viewer; anything else goes through the save path instead.
        if (!ImageOpenExtensions.Contains(Path.GetExtension(fileName)))
        {
            HandleImageSaveRequested(attachmentUrl, fileName);
            return;
        }

        RunAsync(async () =>
        {
            InvokeUI(() => _messageManager.AddSystemMessage(channel, $"Decrypting {fileName}..."));
            var tempPath = await DownloadAttachmentAsync(attachmentUrl, fileName);
            var psi = new System.Diagnostics.ProcessStartInfo(tempPath) { UseShellExecute = true };
            System.Diagnostics.Process.Start(psi);
        }, "Failed to open image");
    }

    private void HandleImageSaveRequested(string attachmentUrl, string fileName)
    {
        if (!_conn.IsAuthenticated) return;

        RunAsync(async () =>
        {
            InvokeUI(() => _messageManager.AddSystemMessage(_mainWindow.CurrentChannel, $"Downloading {fileName}..."));
            var tempPath = await DownloadAttachmentAsync(attachmentUrl, fileName);

            var destination = DedupPath(GetDownloadDir(), fileName);
            File.Move(tempPath, destination);
            InvokeUI(() => _messageManager.AddSystemMessage(_mainWindow.CurrentChannel, $"Image saved to: {destination}"));
        }, "Failed to save image");
    }

    /// <summary>
    /// Resolves the folder downloads are written to: the user's configured
    /// <see cref="ClientConfig.DownloadPath"/> if set, otherwise the OS Downloads folder.
    /// Falls back to the temp folder if neither can be created.
    /// </summary>
    private string GetDownloadDir()
    {
        var dir = _config.DownloadPath;
        if (string.IsNullOrWhiteSpace(dir))
            dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        try
        {
            Directory.CreateDirectory(dir);
            return dir;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Download folder {Dir} is not usable; falling back to temp", dir);
            return Path.GetTempPath();
        }
    }

    /// <summary>Appends " (n)" before the extension until the path doesn't collide with an existing file.</summary>
    private static string DedupPath(string dir, string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var dest = Path.Combine(dir, fileName);
        for (var i = 1; File.Exists(dest); i++)
            dest = Path.Combine(dir, $"{stem} ({i}){ext}");
        return dest;
    }

    /// <summary>
    /// File extensions considered safe to open with the system default application.
    /// Everything else is downloaded only — never auto-opened via UseShellExecute.
    /// </summary>
    private static readonly HashSet<string> SafeOpenExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm", ".mkv", ".avi", ".mov",  // video
        ".pdf", ".txt", ".csv", ".json", ".xml",   // documents
    };

    private void HandleFileDownloadRequested(string attachmentUrl, string fileName)
    {
        if (!_conn.IsAuthenticated) return;

        RunAsync(async () =>
        {
            InvokeUI(() => _messageManager.AddSystemMessage(_mainWindow.CurrentChannel, $"Downloading {fileName}..."));
            var tempPath = await DownloadAttachmentAsync(attachmentUrl, fileName);

            var destination = DedupPath(GetDownloadDir(), fileName);
            File.Move(tempPath, destination);
            InvokeUI(() => _messageManager.AddSystemMessage(_mainWindow.CurrentChannel, $"Saved to: {destination}"));

            if (SafeOpenExtensions.Contains(Path.GetExtension(fileName)))
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo(destination) { UseShellExecute = true };
                    System.Diagnostics.Process.Start(psi);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to open file with default app: {Path}", destination);
                }
            }
        }, "Failed to download file");
    }

    /// <summary>
    /// Sets the download folder. With no argument, opens the OS-native folder picker; if that
    /// isn't available (headless, missing tool), tells the user to pass a path instead. With an
    /// argument, sets that path directly (the fallback for machines with no native picker).
    /// </summary>
    private Task HandleCmdSetDownloadPath(string args)
    {
        var current = _config.DownloadPath ?? GetDownloadDir();

        if (!string.IsNullOrWhiteSpace(args))
        {
            SetDownloadPath(args.Trim());
            return Task.CompletedTask;
        }

        RunAsync(async () =>
        {
            var result = await NativeFolderPicker.PickFolderAsync(current);
            InvokeUI(() =>
            {
                switch (result.Outcome)
                {
                    case PickerOutcome.Chosen when result.Path is not null:
                        SetDownloadPath(result.Path);
                        break;
                    case PickerOutcome.Cancelled:
                        _messageManager.AddSystemMessage(_mainWindow.CurrentChannel, "Download folder unchanged.");
                        break;
                    case PickerOutcome.Unavailable:
                        _messageManager.AddSystemMessage(_mainWindow.CurrentChannel,
                            $"No native folder picker here. Current download folder: {current}\nSet one with: /downloadpath <path>");
                        break;
                }
            });
        }, "Failed to open folder picker");

        return Task.CompletedTask;
    }

    private void SetDownloadPath(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
        }
        catch (Exception ex)
        {
            InvokeUI(() => _mainWindow.ShowError($"Can't use that folder: {ex.Message}"));
            return;
        }

        _config.DownloadPath = path;
        ConfigManager.Save(_config);
        InvokeUI(() => _messageManager.AddSystemMessage(_mainWindow.CurrentChannel, $"Download folder set to: {path}"));
    }

    private void HandleCheckForUpdatesRequested()
    {
        RunAsync(_updateService.CheckNowAsync, "Failed to check for updates");
    }

    private void HandleRollbackRequested()
    {
        if (!UpdateBackupService.BackupExists())
        {
            MessageBox.ErrorQuery(_app, "No Backup", "No backup is available to restore.", "OK");
            return;
        }

        var info = UpdateBackupService.GetBackupInfo();
        var confirm = MessageBox.Query(_app, "Rollback Update",
            $"Restore to version {info?.Version ?? "unknown"}?\n\nThe app will restart.", "Restore", "Cancel");

        if (confirm != 0) return;

        try
        {
            UpdateBackupService.RestoreBackup();
            // RestoreBackup calls Environment.Exit(0)
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Rollback failed");
            MessageBox.ErrorQuery(_app, "Rollback Failed", $"Could not restore: {ex.Message}", "OK");
        }
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
                lock (_channelUsersLock) _channelUsers[channel] = users;
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
        // Update the existing entry in place (never replace it) — the per-server entry also
        // carries cached room keys, left channels, and last-read markers that must survive.
        var config = ConfigManager.Load();
        var server = config.SavedServers.FirstOrDefault(s =>
            string.Equals(s.Url, result.ServerUrl, StringComparison.OrdinalIgnoreCase));

        if (server is null)
        {
            server = new SavedServer { Name = new Uri(result.ServerUrl).Host, Url = result.ServerUrl };
            config.SavedServers.Add(server);
        }

        server.Username = result.Username;
        server.RefreshToken = result.RememberMe ? _conn.Api!.RefreshToken : null;
        server.RememberMe = result.RememberMe;
        server.LastConnected = DateTimeOffset.Now;

        ConfigManager.Save(config);
        _config = config;
        Log.Information("Connected successfully to {Url}", result.ServerUrl);
    }

    /// <summary>
    /// Mutates the current server's config entry and persists it. No-op when not
    /// authenticated or the server isn't saved.
    /// </summary>
    private void UpdateServerConfig(Action<SavedServer> mutate)
    {
        var url = _conn.Api?.BaseUrl;
        if (url is null) return;

        var config = ConfigManager.Load();
        var server = config.SavedServers.FirstOrDefault(s =>
            string.Equals(s.Url, url, StringComparison.OrdinalIgnoreCase));
        if (server is null) return;

        mutate(server);
        ConfigManager.Save(config);
        _config = config;
    }

    /// <summary>
    /// Persists the in-memory last-read message ids to the current server's config entry,
    /// so unread/mention state can be reconstructed on the next connect.
    /// </summary>
    private void PersistLastReads()
    {
        var lastReads = _messageManager.LastReadIds;
        if (lastReads.Count == 0) return;

        UpdateServerConfig(server =>
        {
            foreach (var (channel, id) in lastReads)
                server.LastReadMessages[channel] = id.ToString();
        });
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
