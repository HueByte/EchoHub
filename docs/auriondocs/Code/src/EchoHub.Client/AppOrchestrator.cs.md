# AppOrchestrator.cs

> **Source:** `src/EchoHub.Client/AppOrchestrator.cs`

## Contents

- [AppOrchestrator](#apporchestrator)
  - [AppOrchestrator (constructor)](#apporchestrator-constructor)
  - [MainWindow](#mainwindow)
  - [PendingUpdate](#pendingupdate)
  - [ApplyAsciiSize](#applyasciisize)
  - [AsciiSizeLabel](#asciisizelabel)
  - [BuildOutgoingAttachmentAsync](#buildoutgoingattachmentasync)
  - [CleanupPastedTempFiles](#cleanuppastedtempfiles)
  - [ClearPendingReply](#clearpendingreply)
  - [ClearSavedToken](#clearsavedtoken)
  - [DedupPath](#deduppath)
  - [Dispose](#dispose)
  - [DownloadAttachmentAsync](#downloadattachmentasync)
  - [EnsureRoomUnlockedForSendAsync](#ensureroomunlockedforsendasync)
  - [FetchAndUpdateOnlineUsers](#fetchandupdateonlineusers)
  - [GetDownloadDir](#getdownloaddir)
  - [HandleAudioPlayRequested](#handleaudioplayrequested)
  - [HandleChannelJoinFromMessage](#handlechanneljoinfrommessage)
  - [HandleChannelSelected](#handlechannelselected)
  - [HandleCheckForUpdatesRequested](#handlecheckforupdatesrequested)
  - [HandleCmdAssignRole](#handlecmdassignrole)
  - [HandleCmdBanUser](#handlecmdbanuser)
  - [HandleCmdChangeRoomPassword](#handlecmdchangeroompassword)
  - [HandleCmdClearAttachments](#handlecmdclearattachments)
  - [HandleCmdCreateInvite](#handlecmdcreateinvite)
  - [HandleCmdDeleteAccount](#handlecmddeleteaccount)
  - [HandleCmdExportData](#handlecmdexportdata)
  - [HandleCmdJoinChannel](#handlecmdjoinchannel)
  - [HandleCmdKickUser](#handlecmdkickuser)
  - [HandleCmdLeaveChannel](#handlecmdleavechannel)
  - [HandleCmdListInvites](#handlecmdlistinvites)
  - [HandleCmdListUsers](#handlecmdlistusers)
  - [HandleCmdMeta](#handlecmdmeta)
  - [HandleCmdMuteUser](#handlecmdmuteuser)
  - [HandleCmdNukeChannel](#handlecmdnukechannel)
  - [HandleCmdOpenProfile](#handlecmdopenprofile)
  - [HandleCmdOpenServers](#handlecmdopenservers)
  - [HandleCmdQuit](#handlecmdquit)
  - [HandleCmdRevokeInvite](#handlecmdrevokeinvite)
  - [HandleCmdSendAction](#handlecmdsendaction)
  - [HandleCmdSendBanner](#handlecmdsendbanner)
  - [HandleCmdSendFile](#handlecmdsendfile)
  - [HandleCmdSetAsciiSize](#handlecmdsetasciisize)
  - [HandleCmdSetAvatar](#handlecmdsetavatar)
  - [HandleCmdSetColor](#handlecmdsetcolor)
  - [HandleCmdSetDownloadPath](#handlecmdsetdownloadpath)
  - [HandleCmdSetNick](#handlecmdsetnick)
  - [HandleCmdSetStatus](#handlecmdsetstatus)
  - [HandleCmdSetTheme](#handlecmdsettheme)
  - [HandleCmdSetTopic](#handlecmdsettopic)
  - [HandleCmdTestSound](#handlecmdtestsound)
  - [HandleCmdUnbanUser](#handlecmdunbanuser)
  - [HandleCmdUnmuteUser](#handlecmdunmuteuser)
  - [HandleConnect](#handleconnect)
  - [HandleCreateChannelRequested](#handlecreatechannelrequested)
  - [HandleDeleteChannelRequested](#handledeletechannelrequested)
  - [HandleDeleteMessageRequested](#handledeletemessagerequested)
  - [HandleDisconnect](#handledisconnect)
  - [HandleEditProfile](#handleeditprofile)
  - [HandleFileDownloadRequested](#handlefiledownloadrequested)
  - [HandleFilesStaged](#handlefilesstaged)
  - [HandleImageOpenRequested](#handleimageopenrequested)
  - [HandleImagePasted](#handleimagepasted)
  - [HandleImageSaveRequested](#handleimagesaverequested)
  - [HandleLoadMoreRequested](#handleloadmorerequested)
  - [HandleLogout](#handlelogout)
  - [HandleMessageSubmitted](#handlemessagesubmitted)
  - [HandleProfileRequested](#handleprofilerequested)
  - [HandleReplyCancelRequested](#handlereplycancelrequested)
  - [HandleReplyRequested](#handlereplyrequested)
  - [HandleRollbackRequested](#handlerollbackrequested)
  - [HandleSavedServersRequested](#handlesavedserversrequested)
  - [HandleSearchRequested](#handlesearchrequested)
  - [HandleStatusRequested](#handlestatusrequested)
  - [HandleThemeSelected](#handlethemeselected)
  - [HandleViewProfile](#handleviewprofile)
  - [InvokeUI](#invokeui)
  - [JoinChannelWithPasswordPromptAsync](#joinchannelwithpasswordpromptasync)
  - [NeedsUnlockPrompt](#needsunlockprompt)
  - [NormalizeAsciiSize](#normalizeasciisize)
  - [PersistLastReads](#persistlastreads)
  - [PromptPassword](#promptpassword)
  - [RefreshStagingTray](#refreshstagingtray)
  - [RunAsync](#runasync)
  - [SaveServerToConfig](#saveservertoconfig)
  - [SendStagedMessage](#sendstagedmessage)
  - [SetDownloadPath](#setdownloadpath)
  - [UnlockRoomKeyAsync](#unlockroomkeyasync)
  - [UnlockTrackedChannelAsync](#unlocktrackedchannelasync)
  - [UpdateServerConfig](#updateserverconfig)
  - [WireCommandHandlerEvents](#wirecommandhandlerevents)
  - [WireConnectionManagerEvents](#wireconnectionmanagerevents)
  - [WireMainWindowEvents](#wiremainwindowevents)
  - [ImageOpenExtensions](#imageopenextensions)
  - [SafeOpenExtensions](#safeopenextensions)

---

## AppOrchestrator
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** class

```csharp
public sealed class AppOrchestrator : IDisposable
```


Central coordinator that wires the Terminal UI to the client services and connection layer. Reach for `AppOrchestrator` when hosting the EchoHub TUI: it attaches `MainWindow` events to service calls, routes incoming connection events back into the UI, manages message staging/attachments, and exposes lifecycle hooks such as `PendingUpdate` and `Dispose` so the host can integrate cleanly with the application loop.

## Remarks
`AppOrchestrator` exists to decouple the UI surface (`MainWindow`) from the lower-level services ([`ConnectionManager`](Services/ConnectionManager.cs.md), `ChatMessageManager`, [`UpdateChecker`](Services/UpdateChecker.cs.md), [`NotificationSoundService`](Services/NotificationSoundService.cs.md), [`AudioPlaybackService`](Services/AudioPlaybackService.cs.md), etc.). It centralizes event wiring and background task scheduling (via the `RunAsync` helper) so UI code can remain thin and reactive while the orchestrator handles async operations, error reporting, and state such as `_channelUsers`, `_stagedAttachments`, and `_tempPastedFiles`. The class exposes `MainWindow` and `PendingUpdate` so the host can present the TUI and then perform post-loop actions (for example applying an in-place updater) after the terminal is restored.

## Notes
- Call `Dispose` when shutting down the TUI so the orchestrator can perform its teardown work (the source notes it should "capture read positions before tearing down").
- `PendingUpdate` is intended to be executed after the Terminal.Gui main loop exits (the host must run it once the terminal is restored); running an updater while the TUI is still active can conflict with the console state.
- The class tracks temporary pasted files in `_tempPastedFiles` and staged attachments in `_stagedAttachments`; these are cleaned when messages are sent or the staging tray is cleared — failing to clear them may leave temporary files in the system temp directory.
- Threading: use `InvokeUI` to marshal UI updates back onto the `IApplication` thread and prefer the orchestrator's `RunAsync` helper for fire-and-forget background work to avoid blocking the UI thread.

---

### AppOrchestrator (constructor)
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** constructor

```csharp
public AppOrchestrator(IApplication app, ClientConfig config)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `app` | `IApplication` | — |
| `config` | [`ClientConfig`](Config/ClientConfig.cs.md) | — |


Initializes the application orchestration by constructing the core UI and services from the provided `IApplication` and [`ClientConfig`](Config/ClientConfig.cs.md), creating `ChatMessageManager`, `MainWindow`, `CommandHandler`, [`NotificationSoundService`](Services/NotificationSoundService.cs.md), and [`UpdateChecker`](Services/UpdateChecker.cs.md), wiring their events, starting the update checker, and finally setting the initial status to Disconnected.

## Remarks
AppOrchestrator acts as the composition root for the client, ensuring that UI and operational subsystems are created with the correct dependencies and interconnected before the application becomes interactive. By wiring event handlers before starting background services, it centralises lifecycle management and guarantees predictable startup sequencing.

## Notes
- Startup work runs on the creating thread; long-running initialization (such as the work kicked off by `UpdateChecker.Start()`) may block startup in some hosting environments. Consider ensuring the constructor runs in a context that allows background work to proceed without delaying UI readiness.

---

### MainWindow
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** property

```csharp
public MainWindow MainWindow => _mainWindow
```


Read-only accessor `MainWindow` exposes the underlying `_mainWindow` instance to callers. It provides a convenient way to access the application's main window from the `AppOrchestrator` without exposing the backing field directly, keeping encapsulation intact while enabling coordinated UI interactions.

---

### PendingUpdate
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** property

```csharp
public Func<Task>? PendingUpdate => _updateService.PendingUpdate
```


PendingUpdate is a nullable `Func<Task>` that becomes set when the user confirms an update. The host should invoke this delegate after the `Terminal.Gui` main loop exits to perform the updater's in-place restart without fighting the TUI, since it simply forwards to `_updateService.PendingUpdate`.

## Remarks
PendingUpdate acts as a thin bridge between the UI decision and the update lifecycle. By exposing the updater hook via `_updateService.PendingUpdate`, the design keeps the UI layer decoupled from the restart mechanics while ensuring the host sequences console restoration prior to starting the update.

## Notes
- Check for null before invocation; the property is nullable and may be absent if no update is pending.

---

### ApplyAsciiSize
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void ApplyAsciiSize(string flag)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `flag` | `string` | — |

**Returns:** `void`


Updates the in-memory `DefaultAsciiSize` to the provided `flag`, saves the updated config with `ConfigManager.Save`, refreshes the staging tray via `RefreshStagingTray`, and, if a `CurrentChannel` exists, emits a system message announcing the new size using `AsciiSizeLabel(flag)`.

---

### AsciiSizeLabel
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private static string AsciiSizeLabel(string flag) => flag switch
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `flag` | `string` | — |

**Returns:** `string`


Converts a compact ASCII size flag into a human-readable label for display in the UI. Given the `flag` argument, it returns `Small (40x40)` for `s`, `Large (120x120)` for `l`, and `Medium (80x80)` for any other value. This centralizes the mapping so the rest of the ASCII rendering logic uses consistent strings rather than duplicating literals.

## Remarks

This small helper encapsulates the mapping between the size flag and its display label, making future changes to the labels centralized. Its private static scope signals it's an internal detail of the hosting class and should not be relied on from outside.

## Notes

- The switch is case-sensitive; values other than `s` or `l` result in `Medium (80x80)`.

---

### BuildOutgoingAttachmentAsync
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private static async Task<OutgoingAttachment> BuildOutgoingAttachmentAsync(string path, byte[]? roomKey, string size)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `path` | `string` | — |
| `roomKey` | `byte[]?` | — |
| `size` | `string` | — |

**Returns:** `Task<OutgoingAttachment>`


BuildOutgoingAttachmentAsync reads a file from disk and returns an [`OutgoingAttachment`](Services/OutgoingAttachment.cs.md) suitable for transmission. If a `roomKey` is provided, it encrypts the file bytes with that key, and for image files it also renders a local ASCII preview (at the size indicated by `size`) and encrypts the preview; the server never sees the original file or image contents. If `roomKey` is null, it returns a plain attachment backed by the raw file stream.

## Remarks
By centralizing the attachment preparation for encrypted channels, this method hides the intricacies of file type detection, client-side preview rendering, and encryption behind a single helper. It relies on [`RoomCrypto`](../EchoHub.Core/Security/RoomCrypto.cs.md) for encryption and on [`ImageToAsciiService`](../EchoHub.Core/Services/ImageToAsciiService.cs.md) to generate human-friendly previews, ensuring consistent behavior across callers when dealing with encrypted attachments.

## Notes
- When `roomKey` is null, the method returns a non-encrypted attachment without a preview.
- The ASCII preview is generated only for valid images; non-image files produce no `preview` (it remains null). The declared kind is set to `image` for images, `audio` for audio files, and `file` otherwise.


---

### CleanupPastedTempFiles
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private static void CleanupPastedTempFiles(IReadOnlyList<string> files)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `files` | `IReadOnlyList<string>` | — |

**Returns:** `void`


Best-effort cleanup of pasted-image temp files and their per-paste folders. For each path in the supplied `IReadOnlyList<string>`, it deletes the file with `File.Delete` and, if available, deletes the directory that contains the file (via `Path.GetDirectoryName` and `Directory.Delete`). Any exceptions are caught and logged with `Log.Debug` so cleanup does not propagate to the caller.

## Remarks
This helper favors safety and simplicity: it never throws from the cleanup loop; failures are swallowed to avoid impacting the user flow after a paste. It provides diagnostic visibility through debug logging, aiding investigation if artifacts persist after cleanup.

## Notes
- `Directory.Delete(dir)` will only delete an empty directory; if the per-paste folder still contains files, the delete will fail and be swallowed, potentially leaving artifacts.

---

### ClearPendingReply
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void ClearPendingReply()
```

**Returns:** `void`


Clears the currently pending reply by setting the private field `_pendingReply` to `null`. It also notifies the UI by calling `_mainWindow.SetReplyingTo(null)` to reflect that there is no active reply target.

## Remarks
By centralizing this two-step reset, the method guarantees that both the internal model (`_pendingReply`) and the UI state (`_mainWindow.SetReplyingTo(null)`) stay in sync when a reply is canceled or completed. It provides a single, discoverable place to revert to idle state, reducing the chance of stale state leaking into the user interface.

---

### ClearSavedToken
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void ClearSavedToken(string serverUrl)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `serverUrl` | `string` | — |

**Returns:** `void`


Clears the locally saved refresh token for a specific server by mutating the client configuration. It loads the current configuration via `ConfigManager.Load()`, searches the `SavedServers` collection for an entry whose `Url` matches the provided `serverUrl` using `StringComparison.OrdinalIgnoreCase`, and if found sets its `RefreshToken` to `null`. The updated config is then persisted with `ConfigManager.Save(config)` and the in-memory `_config` reference is refreshed to the latest object.

## Remarks
This private helper centralizes the token-clearing mutation to a single server URL, ensuring both the persisted configuration and the in-memory copy remain in sync. It encapsulates a security-sensitive operation (token removal) behind a tidy, reusable unit to avoid duplicating credential-clearing logic across call sites.

## Notes
- Only affects the server that matches `serverUrl`; if no matching entry exists, no action is taken.
- The URL comparison uses `StringComparison.OrdinalIgnoreCase` to tolerate casing differences in server URLs.

---

### DedupPath
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private static string DedupPath(string dir, string fileName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `dir` | `string` | — |
| `fileName` | `string` | — |

**Returns:** `string`


DedupPath is a private static helper that returns a non-colliding file path within `dir` for a given `fileName` by appending a numeric suffix in the form ` (n)` before the extension when the initial name already exists. It relies on `Path.GetFileNameWithoutExtension`, `Path.GetExtension`, `Path.Combine`, and `File.Exists` to probe candidate names—starting with the original, then variants that insert ` (1)`, ` (2)`, etc.—until a path that does not exist is found, so the caller can save without overwriting existing files.

---

### Dispose
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
public void Dispose()
```

**Returns:** `void`


Disposes the orchestrator's resources synchronously. When invoked, it first persists the last read positions via `PersistLastReads()`, then synchronously disposes the underlying connection by awaiting `_conn.DisposeAsync()` through `AsTask().GetAwaiter().GetResult()`, and finally disposes `_updateService`.

## Remarks
This ordering guarantees that in-flight read state is captured before tearing down the connection and that dependent resources are released in a safe sequence. Bridging the asynchronous disposal of the connection into the synchronous `Dispose` method via `AsTask().GetAwaiter().GetResult()` is a pragmatic pattern for the classic `IDisposable` contract, but it can introduce deadlock risk if called from certain synchronization contexts; callers should ensure an appropriate execution context (e.g., non-UI threads) when disposing.

## Notes
- Bridging `DisposeAsync()` with `GetAwaiter().GetResult()` can deadlock in some synchronization contexts; prefer disposing from a context without a synchronization trap or consider a fully asynchronous disposal pattern if needed.


---

### DownloadAttachmentAsync
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task<string> DownloadAttachmentAsync(string attachmentUrl, string fileName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `attachmentUrl` | `string` | — |
| `fileName` | `string` | — |

**Returns:** `Task<string>`


Downloads an attachment to a temporary file via `_conn.Api!.DownloadFileToTempAsync`, and, if the current channel has a room key available in `_conn.RoomKeys`, decrypts the file locally with `RoomCrypto.DecryptBytes` using that key. If no key is available or decryption fails, the temp file remains with the downloaded bytes and a warning is logged. The method returns the path to the temporary file for downstream usage.

---

### EnsureRoomUnlockedForSendAsync
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task<bool> EnsureRoomUnlockedForSendAsync(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `Task<bool>`


Ensure sending on an end-to-end encrypted channel is allowed only when the room is unlocked. It checks the encryption state via `_conn.RoomKeys.IsChannelEncrypted(channelName)` and `_conn.RoomKeys.HasKey(channelName)`, attempts to unlock with `UnlockTrackedChannelAsync(channelName)`, and surfaces an error via `InvokeUI(() => _mainWindow.ShowError(...))` if unlocking isn’t possible, returning false in that case. This guard centralizes the encryption-state handling so callers don’t leak plaintext or duplicate unlock logic when sending on protected channels.

---

### FetchAndUpdateOnlineUsers
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void FetchAndUpdateOnlineUsers()
```

**Returns:** `void`


FetchAndUpdateOnlineUsers is a private helper that fetches the online users for the current channel and refreshes the UI, but only when there is a non-empty channel and the connection is active (`_conn.IsConnected`). It runs on a background thread via `Task.Run`, calls `_conn.GetOnlineUsersAsync(channel)` to obtain the list, stores the result in the shared cache `_channelUsers` under the lock `_channelUsersLock`, and then marshals the update to the UI with `InvokeUI(() => _mainWindow.UpdateOnlineUsers(users))`; any exceptions are caught and logged with `Log.Debug`.

## Remarks
This private helper isolates data retrieval, cache synchronization, and UI refresh from the foreground path, keeping the UI responsive and ensuring thread-safety when updating the `_channelUsers` collection. It coordinates with `_mainWindow` and `_conn` collaborators to reflect current online users for the active channel.

---

### GetDownloadDir
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private string GetDownloadDir()
```

**Returns:** `string`


Private helper `GetDownloadDir()` determines where downloads should be written by preferring the configured `ClientConfig.DownloadPath`; if that is not set, it resolves to the OS Downloads folder derived from the current user's profile. It then ensures the directory exists by calling `Directory.CreateDirectory` and, if an exception occurs, logs a warning via `Log.Warning` and falls back to the system temp folder from `Path.GetTempPath()`.

## Remarks
Centralizes download-path resolution in one place, shielding callers from platform differences and misconfigurations. By performing the directory creation with `Directory.CreateDirectory` and handling failures with a graceful fallback and warning log via `Log.Warning`, this method provides a robust foundation for all download operations and keeps higher-level code focused on business logic.

---

### HandleAudioPlayRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleAudioPlayRequested(string attachmentUrl, string fileName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `attachmentUrl` | `string` | — |
| `fileName` | `string` | — |

**Returns:** `void`


Handles a request to play an audio attachment by validating authentication, downloading the file, and presenting playback UI. If authenticated, it runs an asynchronous operation that posts a system message to the current channel indicating download progress, downloads the attachment to a temporary path via `DownloadAttachmentAsync(attachmentUrl, fileName)`, and then marshals back to the UI thread to display the playback dialog with `AudioPlayerDialog.Show(_app, _audioPlayback, tempPath, fileName)`. If anything goes wrong, the operation is reported via the RunAsync error handler with the message `Failed to play audio`.

## Remarks
This method encapsulates the full user flow for playing an audio attachment, combining authentication guard, background download, and UI orchestration into a single, testable unit. By offloading IO to `RunAsync` and marshaling UI work with `InvokeUI`, it keeps the orchestrator responsive while the file downloads, and then hands off to `AudioPlayerDialog.Show` to present playback. This separation makes it easy to swap the underlying playback UI or the download mechanism without changing the caller.

## Notes
- Silent no-op when `_conn.IsAuthenticated` is false; there is no user-visible feedback in that case.

---

### HandleChannelJoinFromMessage
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleChannelJoinFromMessage(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `void`


HandleChannelJoinFromMessage processes a channel-join request derived from an incoming message. If the connection is active, it marshals the UI work to the main thread via `InvokeUI` to ensure the channel is present in the list (`_mainWindow.EnsureChannelInList(channelName)`) and switches to the channel (`_mainWindow.SwitchToChannel(channelName)`), then delegates to `HandleChannelSelected(channelName)` to perform any downstream selection logic.

## Remarks
This method acts as a small UI-facing bridge that keeps channel state in sync when a join event arrives from a message. It enforces UI-thread affinity by wrapping updates in `InvokeUI` and centralizes the ordering: connectivity check, UI update, then downstream selection logic via `HandleChannelSelected`.

## Notes
- Early return on `_conn.IsConnected` being false means join events are ignored until a live connection is established; callers should anticipate that messages about channel joins may be deferred.

---

### HandleChannelSelected
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleChannelSelected(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `void`


HandleChannelSelected coordinates the client’s response when a user selects a channel. If the connection is active, it clears any pending reply for a different channel, persists last-read positions, and then performs the join/unlock/history-refresh flow in the background, updating the UI and online user list.

## Remarks
`HandleChannelSelected` serves as the central orchestrator for channel switching, merging connection state, channel-tracking behavior, and UI updates. By centralizing this logic, it ensures that selecting a channel results in a consistent state transition: joining with a password when required, unlocking tracked channels when prompted, loading the channel history, and refreshing the list of online users. It also guards against cross-channel carryover of pending replies and read markers to avoid confusing user experiences.

## Notes
- History retrieval failures are swallowed (the `catch` block is empty), so history may be unavailable without crashing the UI.
- If a user cancels the password prompt for a join, the method untracks the channel and returns the user to the default channel via `HubConstants.DefaultChannel` (and the UI is switched accordingly).

---

### HandleCheckForUpdatesRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleCheckForUpdatesRequested()
```

**Returns:** `void`


It handles a request to check for updates by starting an asynchronous check via `_updateService.CheckNowAsync`, wrapped in the shared `RunAsync` error-handling helper with the failure message "Failed to check for updates". This method is invoked by the orchestrator in response to a user action or system event requesting an update check, ensuring the call runs asynchronously and failures are surfaced consistently.

## Remarks

This method encapsulates the common pattern of executing an asynchronous operation with centralized error handling. By delegating the actual work to `_updateService` and using the `RunAsync` helper for orchestration, the orchestrator remains decoupled from the details of the update mechanism while still providing a uniform failure experience to the user. It promotes consistent UX for update checks and keeps the orchestration logic tidy by avoiding direct await/try-catch boilerplate in the caller.

## Notes

- The method is private; it is not part of the public API.
- It does not return a value; it fires off the asynchronous operation and relies on `RunAsync` to handle completion and errors.

---

### HandleCmdAssignRole
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdAssignRole(string username, string roleStr)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |
| `roleStr` | `string` | — |

**Returns:** `Task`


Processes the client-side assign-role command by translating a textual role into the corresponding [`ServerRole`](../EchoHub.Core/Models/ServerRole.cs.md) and invoking the server API to apply it, but only when the connection is authenticated. It resolves `roleStr` via a switch: "admin" becomes `ServerRole.Admin`, "mod" becomes `ServerRole.Mod`, and all other inputs default to `ServerRole.Member`, before calling `_conn.Api!.AssignRoleAsync(username, role)`.

## Remarks
It exists as a small, internal command handler that enforces authentication and translates user input into a server mutation. It centralizes the role-mapping policy and delegates the actual mutation to the server, keeping the command-text parsing separate from server interaction.

## Notes
- Silent no-op when not authenticated may mask misuse; consider returning a status or throwing an exception to signal that authentication is required.
- Unrecognized role strings default to `ServerRole.Member` — this can hide input errors; consider explicit validation or user feedback.
- The method uses null-forgiving `_conn.Api!` — ensure `_conn.Api` is initialized post-authentication to avoid a `NullReferenceException`.

---

### HandleCmdBanUser
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdBanUser(string username, string? reason)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |
| `reason` | `string?` | — |

**Returns:** `Task`


`HandleCmdBanUser` is a private async command handler that translates a ban command into a backend action. It first ensures the current connection is authenticated (`_conn.IsAuthenticated`). If authenticated, it calls [`BanUserAsync`](Services/ApiClient.cs.md) on `_conn.Api` with the target `username` and optional `reason` to perform the ban.

## Remarks
This symbol serves as the orchestration layer between command input and moderation API calls. By encapsulating the authentication check and the API invocation, it keeps the higher-level command handling clean and focused on input parsing. It relies on the surrounding connection context (`_conn`) to route the request to the moderation API, making it easy to swap or mock the API in tests.

## Notes
- The call uses the null-forgiving operator on `_conn.Api` (as shown in `_conn.Api!.BanUserAsync(...)`); if `_conn.Api` can be null after authentication, this may throw at runtime.
- Exceptions from [`BanUserAsync`](Services/ApiClient.cs.md) will propagate to the caller of `HandleCmdBanUser`; ensure the returned `Task` is awaited to observe failures.

---

### HandleCmdChangeRoomPassword
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdChangeRoomPassword(string oldPassphrase, string newPassphrase)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `oldPassphrase` | `string` | — |
| `newPassphrase` | `string` | — |

**Returns:** `Task`


Changes the current encrypted channel's passphrase by re-deriving the join credential and re-wrapping the cached room content key under the new passphrase. History is never re-encrypted — the room key itself doesn't change.

This private method is the UI action invoked when a user requests to rotate the passphrase for the currently selected end-to-end encrypted channel. It derives the old and new join credentials, re-wraps the room key with the new derived key, and applies the update via the channel API, leaving historical content intact.

## Remarks
This logic centralizes the rekey flow in the client orchestrator, ensuring proper preconditions (authenticated and connected, with a current channel selected) and using the existing crypto surface ([`RoomCrypto`](../EchoHub.Core/Security/RoomCrypto.cs.md)) to derive keys and wrap the room key. The actual server-side mutation is performed through `RekeyChannelAsync` on the channel API, which applies the new passphrase consistently for future access while preserving existing history. It relies on `RoomKeys` to locate the channel key before proceeding.

## Notes
- It aborts early if the client is not authenticated or not connected, or if no channel is selected.
- It requires the channel to be end-to-end encrypted and to have a non-null encryption salt; otherwise it reports that the channel is not encrypted.
- If the channel key cannot be retrieved (e.g., the channel is not unlocked), it prompts the user to unlock the channel first and retry.

---

### HandleCmdClearAttachments
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdClearAttachments()
```

**Returns:** `Task`


Clears the currently staged attachments as part of handling the 'Clear Attachments' command, removing any matching temporary pasted files, performing cleanup, and resetting the in-memory staging state. It returns a completed `Task` to align with asynchronous call patterns while performing the work synchronously.

## Remarks
This method centralizes the cleanup sequence for the attachment staging area: it computes the intersection of `_stagedAttachments` with `_tempPastedFiles`, removes those temps from `_tempPastedFiles`, calls `CleanupPastedTempFiles(temps)`, clears `_stagedAttachments`, and refreshes the UI via `InvokeUI(RefreshStagingTray)`.
By coupling these steps in one place, the rest of the UI command handlers benefit from a consistent cleanup path, reducing risk of leaks or stale UI state. It also makes the intent explicit: clearing attachments is a single, atomic operation from the user's perspective.

## Notes
- This operation discards all staged attachments; any unsaved items will be removed and cannot be recovered.

---

### HandleCmdCreateInvite
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdCreateInvite(int? maxUses, int? expiresHours)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `maxUses` | `int?` | — |
| `expiresHours` | `int?` | — |

**Returns:** `Task`


HandleCmdCreateInvite is a private command handler that orchestrates the creation of a channel invite. When invoked, it first checks that the user is authenticated (`_conn.IsAuthenticated`) and that there is a current channel (`_mainWindow.CurrentChannel`); if both preconditions hold, it runs an asynchronous flow that calls the API (`_conn.Api!.CreateInviteAsync(maxUses, expiresHours)`) to obtain an invite, and upon success formats an optional expiry from `invite.ExpiresAt` and posts a system message to the channel with the invite code, allowed uses, expiry if present, and a reminder to revoke the invite via `/invite revoke {invite.Code}`.

## Remarks
HandleCmdCreateInvite encapsulates the end-to-end flow of invite generation in the UI, coordinating authentication state, channel context, API interaction, and user notification. It relies on the components `_conn`, `_mainWindow`, `_messageManager`, and the helper `RunAsync` to perform the work without blocking the UI thread, keeping the command-handling logic focused and testable.

## Notes
- If the API becomes unexpectedly null while authenticated, the null-forgiving usage `_conn.Api!` could throw; ensure API initialization is tightly coupled with the authentication state.

---

### HandleCmdDeleteAccount
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdDeleteAccount()
```

**Returns:** `Task`


HandleCmdDeleteAccount coordinates the user-initiated account-deletion workflow. It first ensures the client is authenticated via `_conn.IsAuthenticated` and aborts with `Task.CompletedTask` if not; it then presents a warning dialog explaining that deleting the account permanently removes the user on this server (including profile, sessions, and uploads) while leaving messages attributed to 'deleted-user'. If the user confirms and provides a `password`, the handler proceeds asynchronously: it calls `_conn.Api!.DeleteMyAccountAsync(password)`, clears the saved token for the current `baseUrl` with `ClearSavedToken(baseUrl)`, performs cleanup via `_conn.CleanupAsync()`, and updates the UI to reflect a disconnected state and to display an "Account Deleted" confirmation."

## Remarks
HandleCmdDeleteAccount centralizes the end-to-end destructive flow in a single, private handler, ensuring a consistent user experience from confirmation to disconnection. The operation is carried out asynchronously (via `RunAsync` and `InvokeUI` for UI thread marshalling), preserving UI responsiveness while performing server-side deletion and thorough cleanup of session state. This tight coupling of authentication check, user confirmation, credential handling, server API invocation, token invalidation, and UI refresh helps avoid partial/inconsistent states around a critical, irreversible action.

---

### HandleCmdExportData
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdExportData()
```

**Returns:** `Task`


HandleCmdExportData exports the authenticated user's data to a local file by guarding against unauthenticated invocations, then asynchronously fetching the data via [`ExportMyDataAsync`](Services/ApiClient.cs.md), writing it to a timestamped file named `echohub-export-{_session.Username}-{DateTime.Now:yyyyMMdd-HHmmss}.json` in the download directory after deduplication with `DedupPath(GetDownloadDir(), fileName)`, and finally posting a system message in the current channel with the file path and a ciphertext disclaimer.

---

### HandleCmdJoinChannel
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdJoinChannel(string channelName, string? password)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `password` | `string?` | — |

**Returns:** `Task`


HandleCmdJoinChannel is the central handler for a user-initiated join-channel action. It ensures the client is connected, invokes a password prompt via `JoinChannelWithPasswordPromptAsync`, and, if a non-null history is returned, clears any prior left-channel exclusion from `LeftChannels` before updating the UI to list and switch to the channel and loading its history.

## Remarks
This method coordinates cross-cutting concerns across connection state, server configuration, and the user interface. It encapsulates the complete join flow: validating connectivity, obtaining necessary credentials, mutating the server-side exclusion list so the channel can be rejoined, and driving UI navigation and history display. By handling errors centrally (showing a user-facing error via the main window), it provides a consistent UX and keeps the join logic isolated from higher-level command parsing.

## Notes
- If `history` is `null`, the operation is considered cancelled (e.g., the user dismissed the password prompt) and no side effects occur. 
- The removal from `LeftChannels` uses `StringComparison.OrdinalIgnoreCase`, making the operation robust to channel name casing.
- All UI updates are dispatched via `InvokeUI` to marshal work to the UI thread; exceptions during this path surface to the user through [`ShowError`](UI/MainWindow.cs.md).


---

### HandleCmdKickUser
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdKickUser(string username, string? reason)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |
| `reason` | `string?` | — |

**Returns:** `Task`


Kick a user command handling is performed by this private asynchronous method. It first checks the current authentication state via `_conn.IsAuthenticated` and exits early if the client is not authenticated; when authenticated, it delegates the kick operation to the remote API by calling `_conn.Api!.KickUserAsync(username, reason)` with the target `username` and an optional `reason`.

## Remarks
By encapsulating the command-handling path and delegating the actual removal to `_conn.Api!.KickUserAsync`, this method keeps the orchestrator focused on command flow while isolating API interaction behind a single channel. It enforces an authentication check before issuing the kick, preventing unauthorized attempts from progressing in the command pipeline. The private scope signals that this logic is internal to the orchestrator and should not be invoked directly from external callers.

## Notes
- The code uses the null-forgiving operator on `_conn.Api` to assume a non-null API client after authentication. If `_conn.Api` could be null in certain states, this could throw a `NullReferenceException` when calling [`KickUserAsync`](Services/ApiClient.cs.md).


---

### HandleCmdLeaveChannel
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdLeaveChannel()
```

**Returns:** `Task`


HandleCmdLeaveChannel is the asynchronous command handler that leaves the currently selected channel when the client is connected and the channel is not the default one (`HubConstants.DefaultChannel`). It delegates the actual leave to `_conn.LeaveChannelAsync(channel)`, and on success persists the departure by adding the channel to the server configuration’s `LeftChannels` (via `UpdateServerConfig`) using a case-insensitive check (`StringComparer.OrdinalIgnoreCase`) to avoid duplicates, then informs the user with a system message through `_messageManager.AddSystemMessage`. If an error occurs, it surfaces a failure message via `_mainWindow.ShowError`.

## Remarks
This method acts as the orchestrator between the connection layer, persistent configuration, and the UI. By recording the channel in `LeftChannels`, it preserves the user’s intent across restarts and prevents automatic re-entry during subsequent connections. UI updates are marshalled to the main thread with `InvokeUI`, ensuring thread-safety when the operation completes asynchronously.

## Notes
- `LeftChannels` updates use `StringComparer.OrdinalIgnoreCase` to avoid duplicates regardless of channel name casing.

---

### HandleCmdListInvites
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdListInvites()
```

**Returns:** `Task`


If the user is authenticated and a channel is selected, `HandleCmdListInvites` fetches invite codes with `_conn.Api!.GetInvitesAsync()` and builds a human-readable summary listing each invite's `Code` and `UseCount`/`MaxUses`, annotating state as `used up`, `expired`, `expires <date>`, or `active` depending on expiry and usage. The resulting text is then posted to the current channel as a system message via `InvokeUI(() => _messageManager.AddSystemMessage(channel, text))`.

## Remarks
This symbol acts as a presentation conduit for invites and keeps UI-thread interaction isolated from the data fetch by wrapping the operation in `RunAsync`. It enforces simple preconditions via `_conn.IsAuthenticated` and a non-empty `channel` to avoid unnecessary network calls. The per-invite state logic encodes the business rules for visibility: `used up` when `i.UseCount >= i.MaxUses`, `expired` when `i.ExpiresAt` exists and is in the past, `expires <date>` when a future expiry exists (formatted in local time as `yyyy-MM-dd HH:mm`), or `active` otherwise.

---

### HandleCmdListUsers
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdListUsers()
```

**Returns:** `Task`


HandleCmdListUsers fetches the online users for the currently selected channel when the client is connected and renders them as system messages in the channel UI. It guards against missing connection or channel, retrieves the users via `_conn.GetOnlineUsersAsync(channel)`, and uses `InvokeUI` to add a header line and a per-user entry that shows the user’s `DisplayName` (falling back to `Username`) and their `Status` (plus optional `StatusMessage`).

## Remarks
This method acts as a small bridge between the connection service and the UI layer. By performing an asynchronous fetch and then marshaling the results onto the UI thread, it isolates the command's intent—listing channel presence—from the details of how messages are rendered, promoting consistency across similar commands and making it easier to reason about threading and error handling in the UI-bound flow.

## Notes
- The status line for each user uses `Status` (converted to string) and conditionally appends `StatusMessage` when it is present, providing richer presence information without clutter when there is no message.
- The method gracefully handles scenarios where there is no active connection or no channel selected by returning early, avoiding unnecessary work or exceptions.
- All UI updates are performed through `InvokeUI` to ensure they execute on the UI thread, preserving thread-safety for `_messageManager` and the main window.


---

### HandleCmdMeta
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdMeta()
```

**Returns:** `Task`


Fetches and displays room metadata for the currently selected channel when a connection is active. It retrieves the channel name from `_mainWindow.CurrentChannel`, calls `_conn.Api.GetChannelMetaAsync(channel)` to obtain metadata, and, if found, renders a concise room-info panel including topic, room ID, creation time, message count, unique users, estimated size, and protection status; if not found or an error occurs, it reports an error to the user.

## Remarks
This method encapsulates the UI-driven flow for presenting server-provided channel metadata. It coordinates the network fetch, null-checks, and UI updates via `InvokeUI` to ensure the information is shown on the correct thread, and it formats the display values (for example, the estimated size using `ChatMessageManager.FormatFileSize` and the creation time via `CreatedAt.ToLocalTime()`).

## Notes
- The method guards its core path with `_conn.IsConnected` and a non-null `_conn.Api`, returning early otherwise.
- If `GetChannelMetaAsync` returns `null`, a user-facing error like "Channel #<channel> not found." is shown.
- Exceptions are caught and surfaced to the user through a UI error message, so callers should not rely on exceptions propagating from this method.


---

### HandleCmdMuteUser
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdMuteUser(string username, int? duration)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |
| `duration` | `int?` | — |

**Returns:** `Task`


Handles the command to mute a user by validating authentication and delegating to the server-side API to apply the mute for the optional duration. The operation short-circuits when the client is not authenticated and, when authenticated, forwards the `username` and `duration` to the server via `_conn.Api!.MuteUserAsync(username, duration)`.

## Remarks
This method is a focused wrapper around the server mutation that enforces a simple client-side security boundary: authentication must be established before muting. It coordinates between the orchestrator's connection state (`_conn`) and the server API (`_conn.Api`). If the invariant that `_conn.IsAuthenticated` implies a non-null `_conn.Api` is violated, this method can throw a `NullReferenceException`.

## Notes
- The operation short-circuits on unauthenticated state: if `!_conn.IsAuthenticated`, the method returns immediately without calling the server.
- It uses the null-forgiving operator on `_conn.Api` (`_conn.Api!`) which assumes the API is non-null after authentication; if this invariant is violated, a `NullReferenceException` may be thrown.
- No validation is performed on `username` or `duration` at this layer; callers or the server should enforce any required constraints.

---

### HandleCmdNukeChannel
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdNukeChannel()
```

**Returns:** `Task`


HandleCmdNukeChannel is a private asynchronous command handler that triggers a channel-nuke operation for the currently selected channel. It validates preconditions by ensuring the user is authenticated and a channel is selected, then delegates to `_conn.Api!.NukeChannelAsync(channel)` to perform the action; if preconditions are not met, it exits without issuing a request.

## Remarks
Centralizes precondition checks for a destructive action and encapsulates the invocation pattern for nuking a channel, aiding consistency across UI commands and simplifying testing. It reads the target channel from `_mainWindow.CurrentChannel` and relies on `_conn.IsAuthenticated` to gate the API invocation, delegating the actual work to the API client. This separation makes unit testing easier by isolating the orchestration logic from the network call.

## Notes
- Be aware that the call uses `_conn.Api!`, so if the API client is not initialized even when authenticated, a `NullReferenceException` can occur.
- Exceptions from [`NukeChannelAsync`](Services/ApiClient.cs.md) are not handled here and will bubble to the caller.

---

### HandleCmdOpenProfile
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdOpenProfile(string? username)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string?` | — |

**Returns:** `Task`


Acts as a compact command handler that opens the profile view for the given `username`. It forwards the request to the UI layer by scheduling `HandleViewProfile(username)` through `InvokeUI`, and returns `Task.CompletedTask` to satisfy its asynchronous contract.

## Remarks
This method acts as a UI-thread orchestration boundary: it guarantees that `HandleViewProfile` executes on the UI thread via `InvokeUI`, while keeping the command logic separate from view navigation. By returning a completed `Task`, it preserves an async-compatible signature without awaiting the UI action, which helps keep the caller's flow linear and testable.

## Notes
- The returned `Task` is completed immediately; the actual profile opening occurs on the UI thread and is not awaited by the caller.

---

### HandleCmdOpenServers
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdOpenServers()
```

**Returns:** `Task`


Handles the Open Servers command by dispatching the UI operation to show saved servers. It forwards to `HandleSavedServersRequested` via `InvokeUI` and returns `Task.CompletedTask`, making it a minimal adapter in the command pipeline that ensures the UI logic runs in the proper context.

## Remarks
It acts as a small bridge between the command-handling path and the UI flow, ensuring the actual UI logic runs on the appropriate UI thread via `InvokeUI`. Because it returns `Task.CompletedTask`, the method itself does not perform asynchronous work; any long-running processing must be offloaded inside `HandleSavedServersRequested` or its downstream actions.

## Notes
- Avoid UI-thread blocking: this wrapper returns immediately; long-running work should not run on the UI thread; if `HandleSavedServersRequested` starts long tasks, ensure they are offloaded appropriately.

---

### HandleCmdQuit
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdQuit()
```

**Returns:** `Task`


Handles the quit command by marshaling a stop request to the UI thread through `InvokeUI`, which executes `_app.RequestStop()`. It then returns `Task.CompletedTask`, allowing callers to continue asynchronously while the application shutdown proceeds on the UI thread.

## Remarks
This tiny wrapper ensures the quit action is performed on the UI thread, preventing cross-thread access issues when altering the application's stopping state. By returning a completed `Task`, it preserves an async-friendly signature without awaiting the (potentially long) shutdown sequence.

## Notes
- The returned `Task` completes immediately; the actual stop is performed on the UI thread via `_app.RequestStop()`, so awaiting this method does not wait for shutdown to finish.

---

### HandleCmdRevokeInvite
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdRevokeInvite(string code)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `code` | `string` | — |

**Returns:** `Task`


HandleCmdRevokeInvite processes the revoke invite command by validating authentication (`_conn.IsAuthenticated`) and the current channel (`_mainWindow.CurrentChannel`); if both conditions are met, it revokes the invite via `_conn.Api!.RevokeInviteAsync(code)` and posts a channel system message: `Invite {code.ToUpperInvariant()} revoked.`.

## Remarks
Conceptually, it serves as a bridge between user input, network action, and channel feedback. It uses `RunAsync` to perform the revoke without blocking the UI thread and `InvokeUI` to surface the confirmation via `_messageManager.AddSystemMessage` in the active channel. It relies on `_conn.Api` being non-null after authentication and on `_mainWindow.CurrentChannel` providing a valid destination; if those preconditions are missing, the method is a no-op.

## Notes
- Uses `_conn.Api!` (null-forgiving) to call [`RevokeInviteAsync`](Services/ApiClient.cs.md); if `Api` is null, this can throw.

---

### HandleCmdSendAction
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdSendAction(string text)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `text` | `string` | — |

**Returns:** `Task`


HandleCmdSendAction processes a CTCP ACTION command (the /me action) by routing it through the standard message path. It validates that there is an active `_conn` connection and a non-empty current channel, returning immediately otherwise; when both exist, it uses `RunAsync` to asynchronously ensure the room is unlocked via `EnsureRoomUnlockedForSendAsync` and then sends the formatted action with `_conn.SendMessageAsync(channel, MessageConventions.FormatAction(text))`, so CTCP content benefits from the same encryption and sending pipeline as regular messages.

## Remarks
This abstraction centralizes CTCP ACTION formatting via `MessageConventions.FormatAction` and guarantees that action content is subject to the same encryption and room-state checks as normal messages, preventing edge cases where an action could bypass the standard safeguards.

---

### HandleCmdSendBanner
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdSendBanner(string text)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `text` | `string` | — |

**Returns:** `Task`


Handles the banner sending workflow: it returns early if `_conn.IsConnected` is false or the current channel (obtained from `_mainWindow.CurrentChannel`) is null or empty; otherwise it renders the ASCII banner from the input text with `AsciiBannerService.Render(text)` and, if rendering yields `null`, shows an error via `InvokeUI` about allowed characters and the maximum input length (`AsciiBannerService.MaxInputLength`) before returning. When rendering succeeds, it schedules an asynchronous operation with `RunAsync` that first awaits `EnsureRoomUnlockedForSendAsync(channel)` and, if that succeeds, sends the banner with `_conn.SendMessageAsync(channel, banner)`.

## Remarks
This method acts as a focused orchestrator bridging UI feedback, banner rendering, and network dispatch. By delegating rendering to [`AsciiBannerService`](../EchoHub.Core/Services/AsciiBannerService.cs.md) and gating the actual send behind `EnsureRoomUnlockedForSendAsync`, it keeps concerns separated and avoids blocking the UI thread. It relies on `InvokeUI` to marshal error messages to the UI thread for a responsive user experience.

## Notes
- Early exits occur when `_conn.IsConnected` is false or the current channel is empty; the method completes with no exception. 
- If rendering fails, the user receives a UI error explaining the allowed characters and the maximum input length via `AsciiBannerService.MaxInputLength`. 
- The actual send is performed asynchronously within `RunAsync` and is labeled with the failure caption "Send failed" to aid troubleshooting.

---

### HandleCmdSendFile
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdSendFile(string target, string? size)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `target` | `string` | — |
| `size` | `string?` | — |

**Returns:** `Task`


HandleCmdSendFile orchestrates sending a target from the UI by validating preconditions, distinguishing between a URL and a local file, and routing the action accordingly. It first bails out early if the connection is not authenticated or not active, and if there is no current channel selected. If the target resolves to a HTTP(S) URL (detected via `Uri.TryCreate(target, UriKind.Absolute, out var uri)` with a scheme of `http` or `https`), the method attempts to send the URL through the API by invoking `_conn.Api!.SendUrlAsync(channel, target, size)` inside a background `RunAsync` task, but only when the current channel has no room keys and is not encrypted; otherwise it shows an error message stating that sending by URL isn\'t available in encrypted channels. For local files, it enforces a maximum per-message attachment limit using `HubConstants.MaxAttachmentsPerMessage`, and if the limit is reached it displays an error. The function also respects an optional ASCII size hint by evaluating `NormalizeAsciiSize(size)` and, if present, updating `_config.DefaultAsciiSize`. Finally, it stages the target by adding it to `_stagedAttachments` and triggers a UI refresh of the staging tray via `RefreshStagingTray`, so that pressing Enter sends all staged items as one message with the typed caption.

---

### HandleCmdSetAsciiSize
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdSetAsciiSize(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task`


Handles the command to configure the ASCII rendering size for attached images. If an explicit size is provided via the `args` parameter, it parses and applies it immediately with `NormalizeAsciiSize` and `ApplyAsciiSize`, marshaling the update to the UI thread via `InvokeUI`. If no argument is given, it prompts the user with a `MessageBox.Query` to pick between Small, Medium, or Large, and applies the chosen size (the selection is persisted as a preference for future attachments).

## Remarks
This method centralizes the ASCII size configuration for the image-attachment flow. It cleanly separates argument-based updates from the interactive prompt while ensuring all size changes run on the UI thread through `InvokeUI`. The persistent preference is applied to future attachments, providing a consistent rendering size across sessions.

## Notes
- The method returns `Task.CompletedTask` in both branches; awaiting it does not guarantee that the size change has completed, since the actual application happens asynchronously on the UI thread via `InvokeUI`.

---

### HandleCmdSetAvatar
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdSetAvatar(string target)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `target` | `string` | — |

**Returns:** `Task`


Handles the command to set the avatar by first short-circuiting when `_conn.IsAuthenticated` is false. When authenticated, it calls `AvatarHelper.UploadAsync(_conn.Api!, target)` to upload the avatar. If the upload succeeds and there is a current channel (`_mainWindow.CurrentChannel` is non-empty), it uses `InvokeUI` to add a system message `Avatar updated.` to that channel via `_messageManager.AddSystemMessage`. If an exception occurs during upload, it is logged with `Log.Error(ex, "Avatar upload failed for {Target}", target)` and a UI error is shown via `InvokeUI` calling `_mainWindow.ShowError(`Avatar upload failed: {ex.Message}`)`.

## Remarks
Remains focused on bridging authentication state, backend interaction, and user feedback. It delegates the actual upload to [`AvatarHelper`](Services/AvatarHelper.cs.md), while UI feedback is mediated through `InvokeUI` and `_messageManager` to keep the user informed of results. The method demonstrates a pattern of gating backend calls behind authentication and marshaling UI updates to the correct thread.

## Notes
- The call is a no-op if `_conn.IsAuthenticated` is false, ensuring unauthenticated commands cannot trigger avatar uploads.
- Exceptions are caught and surfaced to the user via a UI error message, while also being logged for diagnostics; consider safer messaging in production to avoid leaking internal details.
- The success notification is emitted only when there is an active channel (`_mainWindow.CurrentChannel` not empty); in a channelless context, the only effect is the backend upload.


---

### HandleCmdSetColor
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdSetColor(string color)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `color` | `string` | — |

**Returns:** `Task`


HandleCmdSetColor is a command handler that, when the client is authenticated, updates the current user's nickname color on the server. If the client is not authenticated, it returns early without issuing any API calls; when authenticated, it builds an [`UpdateProfileRequest`](../EchoHub.Core/DTOs/ProfileDtos.cs.md) with `NicknameColor: color` and awaits `_conn.Api!.UpdateProfileAsync(...)` to persist the change.

## Remarks
Acting as a small facade around the profile update API, this symbol centralizes the handling of a user-facing color-change command and the corresponding server update. It relies on `_conn.IsAuthenticated` to gate the operation and on `_conn.Api` being non-null after authentication (the null-forgiving operator signals this expectation). This keeps color customization logic isolated in one place, simplifying testing and future enhancements to command handling.

## Notes
- No local validation of `color` is performed here; invalid values may cause the API call to fail. Validate or constrain `color` prior to invocation if necessary to avoid server rejection.


---

### HandleCmdSetDownloadPath
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdSetDownloadPath(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task`


HandleCmdSetDownloadPath updates the application's download folder. If called with an argument, it applies that path directly; otherwise it opens the OS-native folder picker (when available) and updates the path based on the user's selection, or informs the user if the picker is unavailable or the operation is cancelled.

## Remarks
This private command handler centralizes the logic for configuring the download directory, supporting both direct path input and interactive picking. It initializes the target directory from `_config.DownloadPath` or `GetDownloadDir()`, invokes the native picker via `NativeFolderPicker.PickFolderAsync(current)`, and applies the outcome on the UI thread using `InvokeUI`. It treats three outcomes: `PickerOutcome.Chosen` (set path to `result.Path`), `PickerOutcome.Cancelled` (show "Download folder unchanged."), and `PickerOutcome.Unavailable` (show a hint that no native picker exists and offer manual setup).

## Notes
- Calling with a non-empty `args` bypasses the picker and completes after `SetDownloadPath(args.Trim())`.
- If the native folder picker is unavailable (headless environments or missing tooling), the user is guided to set the path directly using `/downloadpath <path>`.
- The method uses `RunAsync` to perform the picker operation asynchronously and marshals UI updates to the main thread via `InvokeUI`.

---

### HandleCmdSetNick
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdSetNick(string displayName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `displayName` | `string` | — |

**Returns:** `Task`


Handles the command to set the user’s nickname by updating the authenticated user’s profile through the client API and then refreshing the UI to reflect the new display name and the connected status. If the user is not authenticated, the method exits early without performing any update.

## Remarks
This method acts as a small bridge between authentication, the API client, and the UI, centralizing the nickname update behavior to keep command handling consistent. It relies on the convention that an authenticated session provides a non-null `Api` and uses `InvokeUI` to marshal UI updates onto the main thread.

## Notes
- Update operations may fail due to network or server errors; no internal retry logic is present here, so callers should handle exceptions as appropriate.
- The `DisplayName` is passed directly via [`UpdateProfileRequest`](../EchoHub.Core/DTOs/ProfileDtos.cs.md) without local validation; ensure inputs are validated by callers to avoid invalid nicknames.
- The code uses a null-forgiving `!` on `_conn.Api`, assuming it is non-null when `_conn.IsAuthenticated` is true; if this invariant is violated, a `NullReferenceException` could occur.

---

### HandleCmdSetStatus
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdSetStatus(UserStatus? status, string? message)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `status` | `UserStatus?` | — |
| `message` | `string?` | — |

**Returns:** `Task`


HandleCmdSetStatus applies a requested status and optional message for the current user. If the client is not connected, the method exits early and makes no changes. With the two parameters, a null `status` preserves the current session status, while a non-null `status` overrides it; for the `message` parameter, a null value keeps the existing message and an empty string clears it, enabling commands like `/status away` to change the status without erasing the message, or `/status msg brb` to set a new message while keeping the current status. It pushes the updated values to the server via `_conn.UpdateStatusAsync(newStatus, newMessage)` and then synchronizes the in-memory session by assigning `_session.Status` and `_session.StatusMessage`.

---

### HandleCmdSetTheme
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdSetTheme(string name)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `name` | `string` | — |

**Returns:** `Task`


`HandleCmdSetTheme` acts as a command handler that, given a theme name, schedules the actual theme application on the UI thread by invoking `HandleThemeSelected(name)` through `InvokeUI`. It then returns `Task.CompletedTask` to integrate with asynchronous command pipelines without awaiting the UI work.

## Remarks
By using `InvokeUI`, the method ensures that UI-affecting work runs on the UI thread, avoiding cross-thread access violations when applying a new theme. The method is a lightweight bridge between the command surface and the UI logic, returning a completed `Task` to keep async-call sites simple and non-blocking while the actual work is dispatched to the UI context.

---

### HandleCmdSetTopic
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdSetTopic(string topic)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `topic` | `string` | — |

**Returns:** `Task`


The `HandleCmdSetTopic` method processes the user command to set the topic of the currently selected channel. It first ensures the user is authenticated via `_conn.IsAuthenticated` and that a channel is selected (`_mainWindow.CurrentChannel` is not null or empty); if either check fails, it exits without making changes. When both checks pass, it calls the API via `_conn.Api!.UpdateChannelTopicAsync(channel, topic)` to apply the new topic, and, on success, updates the UI (`_mainWindow.SetChannelTopic`) and logs a system message through `_messageManager.AddSystemMessage`. If an exception occurs, it surfaces an error using `_mainWindow.ShowError`.

## Remarks
This method encapsulates a small, user-initiated operation that spans authentication, network, and UI concerns. It coordinates the backend update and the corresponding UI feedback, ensuring that the channel's topic appears consistent after a successful API call, or an error is surfaced otherwise. The use of `InvokeUI` ensures UI updates run on the main thread, preserving thread-safety while the operation executes asynchronously.

## Notes
- Silent early returns on unauthenticated state or missing channel can surprise users; consider surfacing feedback earlier in the flow.
- The null-forgiving operator on `_conn.Api` assumes the API client is non-null after authentication; if it isn't, a `NullReferenceException` may occur.

---

### HandleCmdTestSound
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdTestSound()
```

**Returns:** `Task`


Handles the `HandleCmdTestSound` command by asynchronously invoking the underlying `_notificationSound.PlayTestAsync()` to emit a test notification sound. This method is a targeted bridge that lets the orchestrator trigger audible verification of the notification path without exposing playback specifics to higher-level command logic. Use it during development or diagnostics when you need to confirm that notification sounds play correctly.

## Remarks
By encapsulating the playback call behind a private method, the code keeps command handling decoupled from the concrete sound implementation, easing testing and future substitutions of `_notificationSound`. This small abstraction centralizes the test-path behavior in one place, so changes to how test sounds are produced won't ripple through the command routing.

---

### HandleCmdUnbanUser
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdUnbanUser(string username)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |

**Returns:** `Task`


Handles the unban-user command by first checking `_conn.IsAuthenticated`; if not authenticated, it exits without making a request. When authenticated, it forwards the request to the server by awaiting `_conn.Api!.UnbanUserAsync(username)`.

## Remarks
This method encapsulates the small, command-level action of unbanning a user behind an authentication gate. Keeping the authentication check and the API call in one place reduces duplication across command handlers and makes the flow easier to modify (for example, to add logging or auditing around unban operations). The implementation relies on the invariant that a valid, non-null `_conn.Api` is available when `_conn.IsAuthenticated` is true; the null-forgiving operator expresses this contract but could surface a `NullReferenceException` if the invariant is violated.

## Notes
- Silent no-op when unauthenticated; callers should provide appropriate user feedback at a higher level if needed.
- The call uses the null-forgiving operator on `_conn.Api`, which assumes a non-null API client when authenticated; ensure the invariant is maintained to avoid `NullReferenceException`.


---

### HandleCmdUnmuteUser
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdUnmuteUser(string username)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |

**Returns:** `Task`


Unmutes a user by username by forwarding the request to the backend API after validating the connection is authenticated. If the client is not authenticated, it returns early and does not call the API. 

## Remarks
This small wrapper isolates the moderation action from the underlying API call, enforcing the authentication precondition and delegating the actual unmute to the API client. It helps keep higher-level command handling uniform by providing a single path for unmute operations. It relies on the API client being initialized when authentication is established, tying its correctness to the lifecycle that creates `_conn.Api`.

## Notes
- If `_conn.IsAuthenticated` is true but `_conn.Api` is null, the call will throw due to the null-forgiving operator used on `_conn.Api`.


---

### HandleConnect
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleConnect()
```

**Returns:** `void`


HandleConnect orchestrates the user-initiated connection flow: if already connected it prompts to disconnect via `MessageBox.Query`, then shows the connect dialog with `ConnectDialog.Show(_app, _config.SavedServers)` and starts an asynchronous connect via `_conn.ConnectAsync`, updating the UI status as it progresses. On success it updates the current user, populates channels, switches to the default channel (`HubConstants.DefaultChannel`), replays channel histories seeded from persisted last-read markers, focuses the input, fetches online users, and persists the server to configuration.

## Remarks
By centralizing the connect logic, this method coordinates cross-cutting concerns across `_conn`, `_mainWindow`, and [`ConfigManager`](Config/ConfigManager.cs.md) while preserving UI responsiveness through `RunAsync` and `InvokeUI`. It also handles a graceful recovery path when a saved refresh token is invalid: it logs a warning, clears the saved token, resets the UI status to "Disconnected", and prompts the user to reauthenticate.

---

### HandleCreateChannelRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleCreateChannelRequested()
```

**Returns:** `void`


`HandleCreateChannelRequested` orchestrates the end-to-end flow when a user requests to create a new channel: it validates authentication and connection, collects channel details via `CreateChannelDialog.Show(_app)`, and, if the user confirms, asynchronously creates the channel and updates the UI. If a password is provided, it enforces the minimum length, derives join credentials with [`RoomCrypto`](../EchoHub.Core/Security/RoomCrypto.cs.md), generates a new room key, and wraps the key for secure transmission, sending the password-derived data as `wirePassword`, `saltB64`, and `wrappedKey` to `CreateChannelAsync`. On success, it stores the room key (when present), joins the channel to fetch history, and refreshes the channel list and topic in the UI, finally refreshing online user information.

## Remarks
It acts as a focused orchestration layer that coordinates input collection ([`CreateChannelDialog`](UI/Dialogs/CreateChannelDialog.cs.md)), security setup ([`RoomCrypto`](../EchoHub.Core/Security/RoomCrypto.cs.md)), server interaction (`_conn.Api.CreateChannelAsync`), and UI state updates (`_mainWindow`, `_messageManager`). The encryption path is isolated to the password flow, ensuring that password-derived credentials and the wrapped room key are prepared locally before transmission. The method is a private responder to a user action and relies on `RunAsync` to surface failures with the message 'Failed to create channel'.

## Notes
- The work is scheduled via `RunAsync`, so the caller remains responsive; any failure surfaces through the provided error caption (e.g., 'Failed to create channel').

---

### HandleDeleteChannelRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleDeleteChannelRequested()
```

**Returns:** `void`


The `HandleDeleteChannelRequested` method coordinates the delete-channel flow, validating that the user is authenticated and connected and that a non-default channel is selected. If the user confirms, it deletes the channel via the API (`DeleteChannelAsync`), stops tracking the channel, switches the UI to the default channel (`HubConstants.DefaultChannel`), and posts a system message announcing the deletion. It also surfaces error messages if authentication/connection fails, no channel is selected, or the default channel is attempted to be deleted.

---

### HandleDeleteMessageRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleDeleteMessageRequested(Guid messageId)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `messageId` | `Guid` | — |

**Returns:** `void`


HandleDeleteMessageRequested handles a user-initiated request to delete a message identified by `messageId`. It prevents unauthenticated deletions by returning early if `_conn.IsAuthenticated` is false, and, when authenticated, asynchronously calls `_conn.Api!.DeleteMessageAsync(messageId)` via `RunAsync`, relying on server-side enforcement of the hierarchy rule and on the deletion broadcast to refresh the local list.

## Remarks
By design, this method is a thin orchestrator that encapsulates the authentication check and delegates the deletion to the server. It does not mutate the local message collection directly; instead, the client updates its UI when the server broadcasts the deletion.

## Notes
- No local persistence occurs inside this method; the local view updates in response to the server's deletion broadcast.
- `RunAsync` is supplied with the error message "Failed to delete message" to surface failures consistent with other API operations.

---

### HandleDisconnect
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleDisconnect()
```

**Returns:** `void`


HandleDisconnect is a private method that gracefully tears down the client’s connection to the server and resets the UI and internal state. It logs the impending disconnect with `Log.Information("Disconnecting from server")`, clears `_channelUsers` under `_channelUsersLock`, sets `_pendingReply` to null, clears the reply target in the UI with `_mainWindow.SetReplyingTo(null)`, and persists the last reads by calling `PersistLastReads()`. It then starts an asynchronous cleanup by calling `_conn.CleanupAsync()` inside `RunAsync` with the labels `"Disconnect error"` and `"Disconnect"`, and on completion clears the UI (`_mainWindow.ClearAll()`) and updates the status bar to `Disconnected` via `_mainWindow.UpdateStatusBar("Disconnected")`.

---

### HandleEditProfile
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleEditProfile(UserProfileDto? currentProfile)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `currentProfile` | `UserProfileDto?` | — |

**Returns:** `void`


Orchestrates the end-to-end profile-edit operation: displays the [`ProfileEditDialog`](UI/Dialogs/ProfileEditDialog.cs.md) to collect changes, pushes updates to the server via [`UpdateProfileRequest`](../EchoHub.Core/DTOs/ProfileDtos.cs.md) when authenticated, updates the UI with the new display name, optionally uploads an avatar, updates notification preferences, and saves the updated profile as the default preset using `ConfigManager.Save`.

## Remarks
HandleEditProfile acts as a coordinator that binds together the UI, API, avatar service, and configuration persistence. It ensures that updates are attempted only when `_conn.IsAuthenticated` is true, handles avatar upload failure gracefully by logging and showing an error, and keeps the user experience in sync by updating the main window and status bar when a display name is provided. When an avatar is uploaded, a system message is posted to the current channel to inform users, if one exists.

## Notes
- If the user is not authenticated, the operation aborts early and makes no changes.
- The UI update of the display name is conditional on `editResult.DisplayName` not being null; otherwise the display name remains unchanged.
- The configuration is saved via `ConfigManager.Save` regardless of avatar upload success, so avatar failures do not automatically roll back server or UI updates.

---

### HandleFileDownloadRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleFileDownloadRequested(string attachmentUrl, string fileName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `attachmentUrl` | `string` | — |
| `fileName` | `string` | — |

**Returns:** `void`


HandleFileDownloadRequested downloads an attachment from `attachmentUrl` with the given `fileName`. It only proceeds if `_conn.IsAuthenticated` is true; it downloads the file asynchronously, saves it to a deduplicated destination, notifies the UI about progress and the saved path, and, for extensions in `SafeOpenExtensions`, launches the file with the system default application, logging any failure to open.

## Remarks
This method encapsulates the end-to-end download-and-open pattern, shielding callers from the details of authentication checks, UI messaging, and filesystem operations behind a single, intention-revealing API. It collaborates with `_conn` for authentication, `_messageManager` and `_mainWindow` for user feedback, and `SafeOpenExtensions` to decide when to launch the file automatically, fitting into the app's orchestration layer that responds to file-download requests.

## Notes
- If the user is not authenticated, the operation is short-circuited (returns early) with no downloads.
- Automatic opening is gated by `SafeOpenExtensions`; only safe extensions are opened; failures to launch are logged via `Log.Warning` and do not throw.

---

### HandleFilesStaged
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleFilesStaged(string channel, IReadOnlyList<string> files)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channel` | `string` | — |
| `files` | `IReadOnlyList<string>` | — |

**Returns:** `void`


Stages a batch of local file paths as attachments for the next message. This runs on the UI thread to ensure the staging list cannot race with a subsequent send command, so a multi-file paste won’t slip past the limit. If the connection is not authenticated or not connected, the method exits early; otherwise it appends as many files as can fit within the per-message cap.

It computes the remaining slots as `HubConstants.MaxAttachmentsPerMessage - _stagedAttachments.Count` and adds up to that many files from `files` via `files.Take(Math.Max(0, slotsLeft))`. If more files are provided than there are slots, it shows an error message to the user: "You can attach at most {HubConstants.MaxAttachmentsPerMessage} files per message." Finally it calls `RefreshStagingTray()` to update the UI.

## Remarks
Keeping this logic on the UI thread ensures the staging operation is atomic with respect to user interactions, avoiding interleaving with the sending path. The method relies on `_stagedAttachments` to track current attachments and enforces the per-message cap via `HubConstants.MaxAttachmentsPerMessage`. It also depends on `_conn` to determine whether staging should proceed and on `_mainWindow` to surface errors, with `RefreshStagingTray()` updating the visual staging area.

---

### HandleImageOpenRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleImageOpenRequested(string attachmentUrl, string fileName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `attachmentUrl` | `string` | — |
| `fileName` | `string` | — |

**Returns:** `void`


Views an image attachment without saving it to the user's downloads. It requires authentication and branches on whether the current channel is encrypted: for unencrypted rooms it opens a full URL in the default browser (constructing the URL from `_conn.Api.BaseUrl` when needed) via `System.Diagnostics.Process.Start`; for encrypted rooms it only proceeds if the file extension is in `ImageOpenExtensions`, otherwise it delegates to the save flow, decrypts with `DownloadAttachmentAsync`, and opens the resulting temporary file with `Process.Start` after indicating progress via a system message.

---

### HandleImagePasted
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleImagePasted(string channel, byte[] png)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channel` | `string` | — |
| `png` | `byte[]` | — |

**Returns:** `void`


Stashes a PNG image pasted from the clipboard into the message workflow by writing the raw PNG data to a per-paste temporary folder and registering the file with the current message’s staging pipeline so it flows through the same path-based handling as regular attachments. It only runs when `_conn.IsAuthenticated` and `_conn.IsConnected` are true, enforces the per-message attachment cap via `HubConstants.MaxAttachmentsPerMessage`, and ensures the temporary file is deleted once the message is sent; on failure, the error is logged and the user is notified via `_mainWindow.ShowError`.

## Remarks
Centralizes pasted-image handling to keep the user experience consistent with other attachments and to reuse the existing staging/cleanup logic. It creates a unique per-paste folder under the system temp path and stores the image as `image.png` to preserve a familiar display name while allowing multiple pasted images to coexist in a single message.

## Notes
- Potential thread-safety trap: `_tempPastedFiles` and `_stagedAttachments` are mutated here without visible synchronization; ensure calls are serialized or synchronized when invoked from multiple threads.

---

### HandleImageSaveRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleImageSaveRequested(string attachmentUrl, string fileName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `attachmentUrl` | `string` | — |
| `fileName` | `string` | — |

**Returns:** `void`


HandleImageSaveRequested is a private method in `AppOrchestrator` that saves an image when a save is requested. It guards the operation with `_conn.IsAuthenticated`, then asynchronously downloads the attachment via `DownloadAttachmentAsync`, moves it to a deduplicated destination under the directory from `GetDownloadDir()` using `DedupPath` and `File.Move`, and reports progress to the UI with `InvokeUI` and `_messageManager.AddSystemMessage`. If authentication is not present, it exits early.

## Remarks
Combines an authentication guard (`_conn.IsAuthenticated`), asynchronous work (`RunAsync`), and UI feedback via `InvokeUI` and `_messageManager.AddSystemMessage` to keep the user informed while the image is downloaded and moved. It relies on `DownloadAttachmentAsync`, `GetDownloadDir`, `DedupPath`, and `File.Move` to perform I/O in a deduplicated, user-visible way. This arrangement keeps authentication, I/O, and presentation concerns clearly separated in the orchestration flow.

---

### HandleLoadMoreRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleLoadMoreRequested()
```

**Returns:** `void`


This private method handles the user-initiated request to load older messages for the currently selected channel. It returns early unless `_conn.IsConnected` and a non-empty `_mainWindow.CurrentChannel` are present, then uses `_channelsLoadingMore` to prevent concurrent loads for the same channel and computes the offset as `_messageManager.GetMessages(channel)?.Count ?? 0`. It runs a background task with `RunAsync` to fetch history via `_conn.GetHistoryAsync(channel, HubConstants.DefaultHistoryCount, offset)` and, on success, invokes `InvokeUI(() => _messageManager.PrependHistory(channel, history))`; finally, the channel is removed from `_channelsLoadingMore`, and any failure surfaces the message `Failed to load more messages`.

---

### HandleLogout
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleLogout()
```

**Returns:** `void`


Coordinates the client-side logout sequence when the user signs out: it logs the event, persists the last-read state, and kicks off an asynchronous flow that signs out from the server, clears any saved token tied to the API's [`BaseUrl`](Services/ApiClient.cs.md), performs cleanup, and updates the UI to reflect a disconnected state.

## Remarks
Acts as the centralized logout orchestrator within the app, encapsulating server communication, token management, and UI reset behind a single private method. By using `RunAsync`, it defers error handling to a consistent pathway and keeps UI threading concerns isolated to the `InvokeUI` call. It collaborates with `_conn` for logout/cleanup and with `_mainWindow` to reflect the disconnected state.

## Notes
- If `baseUrl` is null, `ClearSavedToken(baseUrl)` is not invoked, so a saved token may remain.
- The logout sequence runs asynchronously via `RunAsync`; the method returns immediately while the operations execute in the background.
- UI updates occur on the UI thread through `InvokeUI` to avoid cross-thread issues.

---

### HandleMessageSubmitted
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleMessageSubmitted(string channelName, string content)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `content` | `string` | — |

**Returns:** `void`


HandleMessageSubmitted coordinates the user’s message submission in a channel by performing connectivity checks, routing command input through the `_commandHandler`, handling staged attachments via `SendStagedMessage`, and computing a potential `replyTo` for threaded replies before sending asynchronously through `_conn.SendMessageAsync` after ensuring the room is unlocked. It also surfaces command results as either errors or system messages and clears any pending reply once the message is sent.

## Remarks
This function is the single entry point for all message submissions, enforcing connectivity, handling commands, attachments, and reply threading in one place to maintain consistent user experience and state transitions across the chat UI and network layer.

## Notes
- The method has several early returns; a caller cannot assume a message was sent after invoking it.
- The reply-to resolution is channel-sensitive and uses a case-insensitive comparison; mismatched channels will skip applying the pending reply.


---

### HandleProfileRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleProfileRequested()
```

**Returns:** `void`


This private method handles the 'Profile Requested' action by delegating to the shared profile-viewing workflow. It calls `HandleViewProfile` with a `null` argument to trigger the default/current profile display.

## Remarks
This forwarding method isolates the action-handling glue from the actual view logic. By funneling through `HandleViewProfile`, the orchestrator maintains a single path for presenting a profile, reducing duplication and centralizing validation or normalization of input when no specific target is provided. It remains a small wrapper around the real work, preserving a clean public surface while reusing the underlying view logic.

## Notes
- Relies on `HandleViewProfile` being able to handle a `null` input; if that contract changes, this method will need adjustment.

---

### HandleReplyCancelRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleReplyCancelRequested() => ClearPendingReply()
```

**Returns:** `void`


As a private handler, `HandleReplyCancelRequested` responds to a cancellation signal for the current reply by clearing any pending reply state. It simply delegates to `ClearPendingReply()` to perform the actual cleanup, ensuring the cancellation path does not duplicate the clearing logic elsewhere.

## Remarks
By providing a dedicated private handler, cancellation triggers can be wired to a specific action (e.g., user-initiated cancel or protocol signal) without scattering the cleanup details across callers. The method keeps the cleanup behavior centralized in `ClearPendingReply()` while exposing a clear semantic hook for cancellation paths.

---

### HandleReplyRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleReplyRequested(Guid messageId, string sender, string snippet)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `messageId` | `Guid` | — |
| `sender` | `string` | — |
| `snippet` | `string` | — |

**Returns:** `void`


HandleReplyRequested processes a user action to reply to a specific message by establishing the reply context for the current channel and updating the user interface to reflect that intent. If there is no active channel, the method exits early and makes no changes. When a channel exists, it records the target channel and message in `_pendingReply`, truncates the provided snippet to at most 40 characters (appending a Unicode ellipsis when truncated), and updates the main window with a header showing who is being replied to and the short snippet.

## Remarks
By centralizing the reply-flow logic, this method ensures a consistent user experience for replying to messages across the application. It creates a lightweight, UI-facing preview and a stored reply target that subsequent compose/send logic can use to route the reply to the correct channel and message. The operation ties the reply context to the active channel, ensuring the reply target remains meaningful within the current conversation.

## Example
```csharp
// Example: long snippet is truncated for the reply header
Guid messageId = Guid.NewGuid();
string sender = "Alice";
string longSnippet = "This is a long snippet that will be truncated to keep the reply header compact.";
HandleReplyRequested(messageId, sender, longSnippet);
```

## Notes
- If `CurrentChannel` is null or empty, the method returns immediately and does not modify state.
- Snippet truncation uses 40 characters and appends the Unicode ellipsis '…' when needed.
- The UI update calls `_mainWindow.SetReplyingTo` to reflect the reply context; ensure `_mainWindow` is initialized and that this runs on the UI thread to avoid cross-thread issues.

---

### HandleRollbackRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleRollbackRequested()
```

**Returns:** `void`


Orchestrates the rollback flow for an in-progress update by restoring from a previously created backup. It first validates the existence of a backup via `UpdateBackupService.BackupExists()`. If none exists, it shows an error dialog through `MessageBox.ErrorQuery` and returns. If a backup is present, it retrieves backup details with `UpdateBackupService.GetBackupInfo()` to display the target version in a confirmation prompt via `MessageBox.Query(_app, "Rollback Update", `$"Restore to version {info?.Version ?? "unknown"}?

The app will restart."`, "Restore", "Cancel")`. If the user confirms (the result is 0), it calls `UpdateBackupService.RestoreBackup()`, after which `Environment.Exit(0)` terminates the application. Any exception raised during restoration is caught, logged with `Log.Error`, and shown to the user via an error dialog containing the exception message.

---

### HandleSavedServersRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleSavedServersRequested()
```

**Returns:** `void`


This private UI helper reads the configured list of saved servers from `_config.SavedServers` and presents it to the user. If the list is empty, it informs the user via a message box that no saved servers exist yet. If there are saved servers, it formats each entry as: server name, URL, username (defaulting to `?` when missing), and the last connected date formatted as `yyyy-MM-dd`; if a refresh token is present, it appends ` [session saved]`. The resulting lines are displayed in a `MessageBox.Query` under the title `Saved Servers`.

## Remarks
By isolating the saved servers rendering here, the UI layer has a single place to manage how server metadata is presented. It relies on `_config.SavedServers` for data and on `MessageBox.Query` for presentation, which keeps the behavior consistent with other similarly presented lists in the app. If later the display format changes (e.g., additional metadata is shown or localization), only this method needs adjustment.

---

### HandleSearchRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleSearchRequested()
```

**Returns:** `void`


HandleSearchRequested is a private handler that processes the result of the search dialog by presenting channel names and routing the user to either channel navigation or an action handler. It calls `SearchDialog.Show(_app, _mainWindow.GetChannelNames())` and, if the result is `null`, returns; otherwise it branches on `result.Type` to either switch to a channel via `_mainWindow.SwitchToChannel(result.Key)` and call `HandleChannelSelected(result.Key)`, or dispatch to the appropriate action handler (e.g., `HandleConnect()`, `HandleDisconnect()`, `HandleLogout()`, `HandleProfileRequested()`, `HandleStatusRequested()`, `HandleCreateChannelRequested()`, `HandleDeleteChannelRequested()`, `HandleSavedServersRequested()`), or perform UI operations like toggling the users panel or quitting the app.


---

### HandleStatusRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleStatusRequested()
```

**Returns:** `void`


Handles a user-initiated request to change the current status by presenting a [`StatusDialog`](UI/Dialogs/StatusDialog.cs.md) to collect a new `Status` and optional `StatusMessage`. If the user cancels (the dialog returns `null`), the method returns early; otherwise it updates the in-memory `_session.Status` and `_session.StatusMessage` and, when a connection exists, asynchronously sends the update to the server via `_conn.UpdateStatusAsync(...)` inside `RunAsync` with a failure message.

## Remarks
`HandleStatusRequested` acts as a small orchestration unit between the UI and the persistence layer. It delegates the prompting to `StatusDialog.Show`, then applies the resulting values to the local session and conditionally persists them to the server, ensuring the UI remains responsive through `RunAsync`. The local session is updated immediately, so the in-memory representation reflects the user's choice even if the remote update is deferred due to lack of connectivity.

## Notes
- When offline (`_conn.IsConnected` is false), the remote update is skipped; the local session state changes still apply, which may require reconciliation once connectivity is restored.

---

### HandleThemeSelected
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleThemeSelected(string themeName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `themeName` | `string` | — |

**Returns:** `void`


Handles a user-initiated theme selection by resolving the requested theme with `ThemeManager.GetTheme(themeName)`, applying it through `ThemeManager.ApplyTheme(theme)`, persisting the chosen name to `_config.ActiveTheme` with `ConfigManager.Save(_config)`, and finally refreshing the UI via `InvokeUI` to re-apply color schemes and redraw the main window. This method serves as the central coordinator for the end-to-end theme-change workflow, ensuring the in-memory state, persisted configuration, and visual presentation stay in sync when a theme is selected.

## Remarks
This symbol acts as the end-to-end theme-change workflow, coordinating `ThemeManager.GetTheme(themeName)` and `ThemeManager.ApplyTheme(theme)`, persisting the choice to `_config.ActiveTheme` with `ConfigManager.Save(_config)`, and refreshing the UI through `InvokeUI` (calling `_mainWindow.ApplyColorSchemes()` and `_mainWindow.SetNeedsDraw()`). It centralizes theme semantics so UI controls can request a theme by name without implementing the propagation logic themselves, and it ensures the selection survives across sessions by persisting it.

---

### HandleViewProfile
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleViewProfile(string? username)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string?` | — |

**Returns:** `void`


HandleViewProfile orchestrates the flow to display a profile by determining whether the requested username matches the current session's user (own profile) or not, and then, if authenticated, loading the target profile asynchronously before rendering the UI. For own profiles, it delegates to `ProfileViewDialog.ShowOwn` and handles actions such as `ProfileAction.EditProfile` and `ProfileAction.SetStatus`; for other users, it uses `ProfileViewDialog.Show` to present a read-only view.

## Remarks
This method centralizes the profile viewing UX, minimizing duplication of own-vs-other logic across the codebase. By performing the profile fetch in the background and only switching to the UI thread when needed, it preserves UI responsiveness and error handling through a single, consistent path.

---

### InvokeUI
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void InvokeUI(Action action) => _app.Invoke(action)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `action` | `Action` | — |

**Returns:** `void`


Invokes the supplied `Action` on the UI thread by delegating to the underlying `_app.Invoke(action)` call. This private wrapper centralizes UI-thread marshaling within the `AppOrchestrator` so that all UI work flows through a single, consistent path rather than sprinkling `_app` calls across the class.

## Remarks

This private wrapper isolates UI-thread marshaling behind a small helper in `AppOrchestrator`. It makes the threading contract explicit and consistent: all code that must run on the UI thread goes through `_app.Invoke`, avoiding scattered direct `_app` usage. As a simple pass-through, it adds no additional synchronization or error handling beyond what `_app.Invoke` provides; callers should rely on that behavior.

## Notes

- If `_app` is null or uninitialized, this method will throw.
- Exceptions thrown by the provided `Action` propagate to the caller; this method does not swallow or transform them.

---

### JoinChannelWithPasswordPromptAsync
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task<List<MessageDto>?> JoinChannelWithPasswordPromptAsync(string channelName, string? password)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `password` | `string?` | — |

**Returns:** `Task<List<MessageDto>?>`


Joins a channel by name, prompting for a password when the server requires one and re-prompting on wrong password. For end-to-end encrypted channels the typed passphrase never leaves the client — a `PBKDF2`-derived auth key is sent to the server and the room content key is unwrapped locally. The method returns the channel history when the join succeeds, or null if the user cancels the prompt.

## Remarks
This method encapsulates the end-to-end join and key-management flow for encrypted channels. It first fetches optional crypto metadata and, if present, marks the channel encrypted. It then loops, deriving a key from the user's password when available and calling [`JoinChannelAsync`](../EchoHub.Server/Services/ChatService.cs.md) with the wire password. If a fresh `WrappedRoomKey` arrives, it attempts to persist it via `RoomKeys` and, on success, re-fetches history to decrypt content with the new key; if no local key is yet available, it triggers `UnlockRoomKeyAsync` to obtain one. The design keeps encryption material on the client and ensures history is decrypted with the current key state.

## Notes
- The join/prompt loop continues until a successful join yields a usable room key and history, or the user cancels by providing a null password.
- Key derivation is performed only when `crypto.IsEncrypted` is true and `crypto.EncryptionSalt` is non-null; otherwise `kek` remains null and envelope unwrap is skipped.
- UI interaction relies on `InvokeUI` and `ChannelPasswordDialog.Show`, so this path assumes a UI thread context.

---

### NeedsUnlockPrompt
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private bool NeedsUnlockPrompt(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `bool`


Determines whether the UI should prompt for unlocking the room key for a given channel. NeedsUnlockPrompt returns true only when the channel is end-to-end encrypted, its room key is not cached, and the user has not already declined the unlock prompt in this session. If the channel is not encrypted or a key is already cached, the method returns false; if encryption is present but the user has declined, it also returns false. The decision is guarded by a lock around the per-session declined-state to ensure thread-safe reads.

## Remarks
This method centralizes the decision logic for showing an unlock prompt, coordinating between the encryption state available from `RoomKeys` and the per-session user preference tracked in `_declinedUnlocks`. By returning a simple boolean, it prevents repeated prompting for the same channel within a session and encapsulates the necessary synchronization around the decline-tracking collection.

## Notes
- This function is a pure decision point: it does not perform any UI action itself, it merely indicates whether a prompt should be shown.
- It relies on the per-session `_declinedUnlocks` collection to respect a user’s prior decline; the actual population of that set happens outside this snippet.

---

### NormalizeAsciiSize
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private static string? NormalizeAsciiSize(string? size) => size?.Trim().ToLowerInvariant() switch
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `size` | `string?` | — |

**Returns:** `string?`


NormalizeAsciiSize is a private static helper method that standardizes a user-supplied size descriptor (parameter `size`). It trims whitespace with `Trim()`, converts to lowercase with `ToLowerInvariant()`, and maps common tokens to a canonical short form: `s` or `small` → `s`, `m` or `medium` → `m`, and `l` or `large` → `l`. If the input is `null` or does not match any known token, it returns `null` to indicate an unrecognized size.

## Remarks
By centralizing these token mappings in a single helper, the codebase avoids duplicating normalization logic at call sites and ensures consistent downstream handling. Since it returns `null` for unrecognized input, callers must handle this possibility explicitly rather than relying on exceptions.

## Notes
- Returns `null` for unrecognized inputs; callers must handle this possibility.
- Trims whitespace and ignores case via `Trim()` and `ToLowerInvariant()` to make matching resilient to user input.

---

### PersistLastReads
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void PersistLastReads()
```

**Returns:** `void`


Persists the in-memory last-read message ids to the current server's config entry so unread and mention state can be reconstructed on the next connect. It reads the mapping from `_messageManager.LastReadIds`, returns early if it is empty, and uses `UpdateServerConfig` to write each `(channel, id)` pair into `server.LastReadMessages[channel]` as `id.ToString()`.

## Remarks
This method encapsulates the persistence of per-channel read progress behind a single server-config mutation (`UpdateServerConfig`). It decouples in-memory tracking from durable storage, ensuring the last-read state survives restarts and reconnections. The mapping is stored as string values in `server.LastReadMessages`, derived from `id.ToString()`.

---

### PromptPassword
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
}

    private string? PromptPassword(string prompt)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `prompt` | `string` | — |

**Returns:** `}

    private string?`


Displays a modal password-prompt dialog and returns the entered password when the user confirms; it returns `null` if the user cancels or leaves the field empty. The UI is constructed with a `Dialog` titled "Confirm Password" containing a `Label` for the prompt, a secret `TextField` for password input, and two `Button`s: "Confirm" (default) and "Cancel". When the user accepts, the handler assigns the field's `Text` to `result` and stops the dialog loop; when cancelling, it assigns `null` to `result` and stops. The dialog is shown by `_app.Run(dialog)`, and the final return is either the password or `null` if no input was provided.

---

### RefreshStagingTray
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void RefreshStagingTray()
```

**Returns:** `void`


Refreshes the staging tray with the current staged files and ASCII size. It collects file names from `_stagedAttachments` via `Path.GetFileName` and updates the UI by calling `_mainWindow.SetStagedAttachments`, supplying the resulting `names` list and the ASCII size label produced by `AsciiSizeLabel(_config.DefaultAsciiSize)`.

## Remarks
Private helper that centralizes the UI refresh pattern for the staging area. It translates the raw attachments into the user-visible names and size indicator, ensuring a consistent presentation whenever the staged set changes.

## Notes
- The method uses `OfType<string>()` to ignore non-string entries in `_stagedAttachments` before extracting file names.
- The ASCII size displayed derives from `_config.DefaultAsciiSize` via `AsciiSizeLabel`, so changing the config affects the label globally.

---

### RunAsync
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void RunAsync(Func<Task> work, string errorPrefix, string? logContext = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `work` | `Func<Task>` | — |
| `errorPrefix` | `string` | — |
| `logContext` | `string?` | `null` |

**Returns:** `void`


A private helper that delegates the execution of an asynchronous unit of work to `AsyncRunner.Run`, binding it to the current application context and a centralized error presentation path. Callers supply a `Func<Task>` representing the work, an `errorPrefix` for user-facing errors, and an optional `logContext` for additional trace information; the method forwards these to `AsyncRunner.Run` along with `_app` and `_mainWindow.ShowError`.

## Remarks
Consolidates cross-cutting concerns: error handling and user feedback for async operations initiated by the orchestrator. By funneling all such work through this method, the codebase avoids duplicating boilerplate at every call site and ensures consistent error presentation via ` _mainWindow.ShowError` by passing it to `AsyncRunner.Run`.


---

### SaveServerToConfig
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void SaveServerToConfig(ConnectDialogResult result)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `result` | [`ConnectDialogResult`](UI/Dialogs/ConnectDialog.cs.md) | — |

**Returns:** `void`


On a successful connection to a server, this method updates the per-server entry in the client config in place (never replacing it) so that cached room keys, left channels, and last-read markers survive across connections. It looks up the server by URL (case-insensitive) within the `config.SavedServers` collection and, if no entry exists, creates a new [`SavedServer`](Config/ClientConfig.cs.md) named after the URL host and adds it to the collection; it then updates `Username`, `RefreshToken` (taken from `_conn.Api!.RefreshToken` when `RememberMe` is true, otherwise `null`), `RememberMe`, and `LastConnected`, persists the updated [`ClientConfig`](Config/ClientConfig.cs.md) via `ConfigManager.Save(config)`, and updates the in-memory `_config` while logging the successful connection with the URL.

---

### SendStagedMessage
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void SendStagedMessage(string channel, string content)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channel` | `string` | — |
| `content` | `string` | — |

**Returns:** `void`


Sends one message with the given caption plus all staged files as attachments, then clears the staging tray. In encrypted channels each file is room-encrypted (blob + ASCII preview) client-side before upload; the caption is encrypted too when a room key is available and the content is non-empty.

The operation runs asynchronously inside a `RunAsync` wrapper, taking a snapshot of the current staged attachments, clearing `_stagedAttachments`, and refreshing the staging tray UI via `RefreshStagingTray`.

The method builds a list of [`OutgoingAttachment`](Services/OutgoingAttachment.cs.md)s by calling `BuildOutgoingAttachmentAsync` for each staged path (using a room key if one is present). If a room key is available and `content` is not empty, the caption is encrypted with `RoomCrypto.EncryptText` before sending; otherwise the plain `content` is sent. The final send is performed through `_conn.Api!.SendMessageWithAttachmentsAsync(channel, wireContent, outgoing, size)` with `size` taken from `_config.DefaultAsciiSize`.

A `finally` block ensures that temporary pasted files are cleaned up via `CleanupPastedTempFiles(tempFiles)` so there are no leftovers regardless of success or failure.


---

### SetDownloadPath
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void SetDownloadPath(string path)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `path` | `string` | — |

**Returns:** `void`


Sets the application's download folder to the provided `path`. It first ensures the directory exists by calling `Directory.CreateDirectory(path)`; if that throws, it marshals a UI error through the main window and returns. On success, it updates `_config.DownloadPath`, saves the configuration with `ConfigManager.Save(_config)`, and posts a system message via `_messageManager.AddSystemMessage` to `_mainWindow.CurrentChannel` indicating the new download folder. All UI feedback is marshaled through `InvokeUI` to run on the UI thread.

---

### UnlockRoomKeyAsync
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task<List<MessageDto>?> UnlockRoomKeyAsync(string channelName, JoinOutcome outcome)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `outcome` | [`JoinOutcome`](Services/EchoHubConnection.cs.md) | — |

**Returns:** `Task<List<MessageDto>?>`


UnlockRoomKeyAsync is the encrypted-channel recovery workflow used when there is no cached room key for a channel (for example on a new device). It first checks the provided [`JoinOutcome`](Services/EchoHubConnection.cs.md) for `EncryptionSalt` and `WrappedRoomKey`; if either is missing, it returns the existing history. If both are present, it prompts the user with `ChannelPasswordDialog.Show` to enter a passphrase, derives a candidate key with `RoomCrypto.DeriveKeys(passphrase, salt)`, and attempts to store the derived key via `_conn.RoomKeys.TryStoreFromEnvelope(channelName, outcome.WrappedRoomKey, derived.KeyEncryptionKey)`. On success, it clears any decline flag for the channel and fetches the updated history using `_conn.GetHistoryAsync(channelName)`. If the passphrase is incorrect, it repeats the prompt; if the user cancels, it records the decline for that channel and returns the existing history.

---

### UnlockTrackedChannelAsync
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task<bool> UnlockTrackedChannelAsync(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `Task<bool>`


UnlockTrackedChannelAsync orchestrates the unlock sequence for a channel that is already hub-joined. It reuses the join flow to fetch a wrapped room key envelope by calling `_conn.JoinChannelAsync(channelName, null)`. If the envelope is absent (the `WrappedRoomKey` is null), the channel is not an E2E channel and the method returns `false`. It then delegates to `UnlockRoomKeyAsync(channelName, outcome)` to unwrap the key (which prompts for the passphrase). If no key is retained in `_conn.RoomKeys` for the channel, the flow is considered cancelled or unwrapped, and the method returns `false`. If a `history` payload is produced, it is applied to the UI via `InvokeUI(() => _messageManager.LoadHistory(channelName, history))`. The method returns `true` when the unlock succeeds; any exception is caught, logged with a warning, and results in `false`.

## Remarks
UnlockTrackedChannelAsync centralizes the unlock sequence for an E2E channel into a single, testable flow that spans network joining, key envelope handling, and UI history restoration. It defers to the hub-provided envelope to determine applicability, uses `UnlockRoomKeyAsync` for unwrapping (and passphrase prompting), and only then surfaces the decrypted history via [`LoadHistory`](UI/Chat/ChatMessageManager.cs.md). This encapsulation keeps concerns separated: the rest of the app can request an unlock without wiring together [`JoinChannelAsync`](../EchoHub.Server/Services/ChatService.cs.md), envelope checks, and UI updates.

## Notes
- Exceptions are caught and cause the method to return `false`; debugging may require examining logs produced by `Log.Warning`.
- The method returns `false` for multiple distinct failure modes (not an E2E envelope, cancellation/unwrapping failure, or an unexpected exception); callers should handle a generic failure outcome gracefully.

---

### UpdateServerConfig
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void UpdateServerConfig(Action<SavedServer> mutate)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `mutate` | `Action<SavedServer>` | — |

**Returns:** `void`


UpdateServerConfig mutates the configuration entry for the server associated with the current API base URL and persists the result. It loads the existing configuration with `ConfigManager.Load()`, locates the matching [`SavedServer`](Config/ClientConfig.cs.md) in `config.SavedServers` by comparing the server’s `Url` to the API base URL using `StringComparison.OrdinalIgnoreCase`, applies the mutation via the `mutate` action, saves the updated configuration with `ConfigManager.Save`, and updates `_config` to reflect the in-memory state. If the URL cannot be determined or no matching server exists, the method returns without changes.

## Remarks
UpdateServerConfig acts as a small centralization point for modifying the active server's settings, ensuring that mutations are consistently applied and immediately persisted. It encapsulates the guard logic (null URL and missing server) behind a simple contract and keeps the in-memory `_config` synchronized with the persisted config.

---

### WireCommandHandlerEvents
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void WireCommandHandlerEvents()
```

**Returns:** `void`


Subscribes the command handler's events to their corresponding handlers within the orchestrator. It wires each `OnX` event exposed by `_commandHandler` to a concrete `HandleCmdX` method (for example `OnSetStatus` → `HandleCmdSetStatus`, `OnJoinChannel` → `HandleCmdJoinChannel`). This central wiring typically runs during initialization to ensure that incoming commands trigger the appropriate response logic.

## Remarks
By centralizing all event subscriptions in `WireCommandHandlerEvents`, the class gains a single, discoverable place to manage the command-event surface, reducing drift where a handler might be forgotten. The pattern keeps wiring concerns separate from the handlers themselves, which simplifies testing and future extension (adding new commands simply introduces a new `OnX`-`HandleCmdX` pair).

---

### WireConnectionManagerEvents
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void WireConnectionManagerEvents()
```

**Returns:** `void`


Wires the connection manager's events to drive UI updates and internal state in response to real-time chat activity. It subscribes to `MessageReceived`, `UserJoined`, `UserLeft`, and `UserStatusChanged`, updating the message feed, per-channel presence caches, and online user lists, while triggering mention notifications when relevant. All UI updates are marshalled via `InvokeUI`, and the shared cache is protected with `_channelUsersLock` to ensure thread-safety during joins, leaves, and status changes.

## Remarks
By centralizing the wiring of `MessageReceived`, `UserJoined`, `UserLeft`, and `UserStatusChanged`, this method keeps the UI and the per-channel presence cache in sync with real-time activity, delegating display concerns to the UI while mutating a `List<UserPresenceDto>` under `_channelUsersLock`. This approach minimizes UI churn by refreshing the current channel's online list when needed and broadcasting presence changes across all channel views.

## Notes
- All presence mutations occur inside a `lock (_channelUsersLock)` block to guard against concurrent updates from multiple events.
- UI updates are dispatched via `InvokeUI` to ensure thread affinity for UI components like `_messageManager` and `_mainWindow`.
- `UserStatusChanged` propagates a textual status to all channels via `_messageManager.AddStatusMessage` and synchronizes per-channel lists, with special handling for `UserStatus.Invisible`.

---

### WireMainWindowEvents
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void WireMainWindowEvents()
```

**Returns:** `void`


Subscribes the `_mainWindow` events to their corresponding handlers (for example, `_mainWindow.OnConnectRequested` to `HandleConnect`, `_mainWindow.OnMessageSubmitted` to `HandleMessageSubmitted`, `_mainWindow.OnChannelSelected` to `HandleChannelSelected`, and so on). This centralizes the UI-to-logic wiring that drives the application's event-driven behavior. Call this during initialization to bootstrap the UI event flow in a single place rather than scattering subscriptions across the codebase.

## Remarks
By collecting all event subscriptions here, the method provides a single locus for the startup wiring and makes it easier to see which UI actions trigger which handlers. It also decouples the `_mainWindow` from concrete behavior; the handlers can be replaced or mocked for testing without changing event hookup sites. If this method runs multiple times, handlers would be added repeatedly; ensure it's invoked once or guard against re-subscription.

## Notes
- Calling this method more than once will attach duplicate event handlers to `_mainWindow`, causing handlers to fire multiple times for a single UI action. Consider guarding with a flag or detach before re-wiring in diagnostic scenarios.

---

### ImageOpenExtensions
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private static readonly HashSet<string> ImageOpenExtensions = new(StringComparer.OrdinalIgnoreCase)
```


ImageOpenExtensions is a private static readonly `HashSet<string>` that lists the image file extensions the [open] action may hand to the OS image viewer in E2E rooms. It is constructed with `StringComparer.OrdinalIgnoreCase` to perform case-insensitive lookups, whitelisting extensions such as `.png`, `.jpg`, `.jpeg`, `.gif`, `.webp`, and `.bmp`.

---

### SafeOpenExtensions
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private static readonly HashSet<string> SafeOpenExtensions = new(StringComparer.OrdinalIgnoreCase)
```


SafeOpenExtensions is a private static readonly `HashSet<string>` listing the file extensions the app is allowed to open with the system's default application via the shell. For anything else, the code downloads rather than auto-opening, enforcing a safe-open policy; the set uses `StringComparer.OrdinalIgnoreCase` to treat extensions case-insensitively (e.g., `.MP4` and `.mp4` are equivalent).

## Remarks
Centralizes the open-via-shell policy for file handling by enumerating extensions that may be opened with the system default application; any file with an extension not present in `SafeOpenExtensions` is downloaded instead and not opened automatically. The policy uses `StringComparer.OrdinalIgnoreCase` to ensure case-insensitive matching, so `.MP4` and `.mp4` are treated equally.

## Notes
- Underlying collection mutability: although the field is `static readonly`, the `HashSet<string>` contents can be changed at runtime; treat this as a fixed policy only if you guarantee no mutation after initialization.

---