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
  - [_app](#_app)
  - [_audioPlayback](#_audioplayback)
  - [_channelUsers](#_channelusers)
  - [_channelUsersLock](#_channeluserslock)
  - [_channelsLoadingMore](#_channelsloadingmore)
  - [_commandHandler](#_commandhandler)
  - [_config](#_config)
  - [_conn](#_conn)
  - [_declinedUnlocks](#_declinedunlocks)
  - [_mainWindow](#_mainwindow)
  - [_messageManager](#_messagemanager)
  - [_notificationSound](#_notificationsound)
  - [_pendingReply](#_pendingreply)
  - [_session](#_session)
  - [_stagedAttachments](#_stagedattachments)
  - [_tempPastedFiles](#_temppastedfiles)
  - [_updateService](#_updateservice)

---

## AppOrchestrator
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** class

```csharp
public sealed class AppOrchestrator : IDisposable
```


A central orchestration component for the EchoHub TUI client that wires UI events to service calls and connection events back to UI updates. Use `AppOrchestrator` when bootstrapping the TUI application: it holds long-lived services and state (connection, session, message manager, update checker, audio services) and coordinates interactions between `MainWindow`, the `CommandHandler`, and backend APIs.

## Remarks
`AppOrchestrator` is the single place that composes UI and platform services for the TUI. It owns the `MainWindow` instance (exposed via the `MainWindow` property), the live `UserSession`, a `ConnectionManager` (`_conn`) and supporting services such as `ChatMessageManager`, `NotificationSoundService`, `AudioPlaybackService` and `UpdateChecker`. Transient UI-to-service workflows (for example, command handling and send flows) are implemented as private command handler methods; background work is scheduled through the private `RunAsync` helper and UI work is marshalled with `InvokeUI` (which forwards to `_app.Invoke`).

The class also maintains several pieces of in-memory state used to present and manage channel UI: a case-insensitive map of channel user lists (`_channelUsers` constructed with `StringComparer.OrdinalIgnoreCase`), a lock (`_channelUsersLock`) protecting that map, a set of channels currently loading more messages (`_channelsLoadingMore`), staged attachments (`_stagedAttachments`), temporary pasted-file paths (`_tempPastedFiles`) and a set of E2E-unlock prompts the user declined (`_declinedUnlocks`). A pending reply target is tracked in `_pendingReply` so the next outgoing text in the target channel is sent as a reply. The `PendingUpdate` property surfaces the `UpdateChecker`'s pending restart action and must be run by the host after the `Terminal.Gui` main loop has exited (console restored) to avoid conflicting with the TUI loop.

## Notes
- `Dispose` must run cleanly when the app is shutting down; the class comments indicate read positions should be captured while still connected before tearing down resources. Ensure `Dispose` is called during shutdown so services and temporary artifacts are cleaned up.
- UI updates must be performed on the UI thread via `InvokeUI` (`_app.Invoke`). Background tasks should use the internal `RunAsync` helper rather than updating UI state directly to avoid threading issues.
- Access to `_channelUsers` is guarded by `_channelUsersLock`; callers inside `AppOrchestrator` should respect that locking discipline to avoid data races.
- Channel keys are compared case-insensitively (`StringComparer.OrdinalIgnoreCase`); treat channel identifiers accordingly when interacting with the orchestrator.
- Temporary PNGs produced for clipboard-image pastes are tracked in `_tempPastedFiles` and are expected to be deleted once the message is sent or staging is cleared—consumers should ensure these files are removed to avoid polluting the system temp directory.

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
| `config` | `ClientConfig` | — |


Constructs an `AppOrchestrator` by injecting an `IApplication` and a `ClientConfig`, then creates its core collaborators (`ChatMessageManager`, `MainWindow` (with `app` and the message manager), `CommandHandler`, `NotificationSoundService`, `UpdateChecker`). It then wires the UI and command/connection events via the `WireMainWindowEvents`, `WireCommandHandlerEvents`, and `WireConnectionManagerEvents` helpers, starts the update checker, and initializes the UI to a disconnected state with `_mainWindow.UpdateStatusBar("Disconnected")`.

---

### MainWindow
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** property

```csharp
public MainWindow MainWindow => _mainWindow
```


This read-only, expression-bodied property named `MainWindow` forwards the private backing field `_mainWindow` by returning it as the `MainWindow` instance. It provides a typed, centralized entry point to the application's main window for other components, while preserving encapsulation of the internal field.

---

### PendingUpdate
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** property

```csharp
public Func<Task>? PendingUpdate => _updateService.PendingUpdate
```


PendingUpdate is an optional delegate of type `Func<Task>?` that the host can invoke to perform the update after the user confirms it. It forwards to the internal `_updateService.PendingUpdate`, ensuring the host runs the updater after the `Terminal.Gui` main loop exits (console restored) so the in-place restart doesn't fight the TUI.

## Remarks
This forwarding role isolates the UI orchestration from the updater logic, enabling a clean separation of concerns. It encodes a safe restart point by triggering the update once the UI has yielded control back to the console, avoiding contention with the TUI.

## Example
```csharp
if (PendingUpdate is not null)
{
    await PendingUpdate();
}
```

## Notes
- Always await the `Task` returned by `PendingUpdate` to ensure the update completes before proceeding.
- Since `PendingUpdate` may be null when no update is pending, guard the call with a null check.


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


Updates the ASCII size by assigning the provided `flag` to `_config.DefaultAsciiSize`, saves the updated configuration with `ConfigManager.Save`, refreshes the staging tray via `RefreshStagingTray`, and, if there is an active channel (`_mainWindow.CurrentChannel`), posts a system message through `_messageManager.AddSystemMessage` stating the new size (rendered by `AsciiSizeLabel(flag)`).

Developers reach for this helper when changing the ASCII size to ensure config, UI, and user notification are consistently updated in one place.

## Remarks
This method encapsulates the cross-cutting side effects of updating the ASCII size: updating `_config` in-memory, persisting with `ConfigManager.Save`, refreshing the UI via `RefreshStagingTray`, and notifying the current channel with `_messageManager.AddSystemMessage`. By centralizing these steps, it keeps callers focused on higher-level behavior and ensures state and feedback stay aligned.

## Notes
- If `_mainWindow.CurrentChannel` is null or empty, no system message will be posted.
- The message text uses `AsciiSizeLabel(flag)` to render the user-friendly description.

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


Maps a size flag to a human-friendly ASCII size label. Implemented as a private static method `AsciiSizeLabel(string flag)` that uses a switch expression on `flag` to return `Small (40x40)` for `s`, `Large (120x120)` for `l`, and `Medium (80x80)` for any other value.

## Remarks
This helper centralizes the mapping from size flags to display labels, ensuring consistent wording across the UI and preventing duplication of literal strings. Being private and static, it is intended for internal use within its containing class, and its concise switch expression cleanly encodes the small set of known cases while gracefully handling unknown flags via the default case.

## Notes
- The default branch returns `Medium (80x80)` for unknown flags; if new flags are introduced, consider updating this mapping to reflect a specific label.
- As a private, side-effect-free helper, it is simple to unit-test by asserting expected outputs for given inputs.

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


Reads a staged file into an `OutgoingAttachment`. For encrypted channels the blob is AES-GCM encrypted, its kind is declared, and the image ASCII preview is rendered locally (at the given `size`) and room-encrypted — so the server never sees the file or image contents.

## Remarks
This function centralizes the packing of a staged file for transmission, handling both encrypted and unencrypted channels. By declaring a content kind (`image`, `audio`, or `file`) and, when appropriate, producing a local ASCII preview of images, it enables the server to apply suitable handling without exposing raw content. The preview is encrypted alongside the payload, ensuring that even lightweight representations remain protected on the wire.

## Notes
- Memory usage: it reads the entire file into memory to perform encryption; for very large files this can be memory-intensive. Consider streaming or chunking if needed.
- The ASCII preview is only produced for image files; non-image files skip the preview, but the full payload is still encrypted when a `roomKey` is supplied.

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


Best-effort cleanup helper `CleanupPastedTempFiles` that removes temporary files created during paste operations for pasted images, along with their per-paste directories. It iterates the provided file paths, deletes each with `File.Delete`, and then attempts to delete the containing directory via `Path.GetDirectoryName` and `Directory.Delete` when available. Any failures are caught and logged at debug level by `Log.Debug`, ensuring cleanup does not propagate exceptions to callers.

## Remarks
This method exists as a focused cleanup primitive for ephemeral artifacts created during image pasting. It centralizes the pattern of deleting a file and its containing directory, with non-fatal error handling: failures are logged but do not disrupt the caller's flow. As a private helper, it keeps paste-related cleanup encapsulated and reusable across the orchestrator.

## Notes
- Directory.Delete is non-recursive; it will only remove an empty directory, so if other files exist, the directory may remain.
- Exceptions are swallowed; callers should not rely on this method for guaranteed cleanup.

---

### ClearPendingReply
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void ClearPendingReply()
```

**Returns:** `void`


Resets the internal pending reply state and clears the UI’s current reply target. It does so by setting `_pendingReply` to `null` and by invoking `_mainWindow.SetReplyingTo(null)` to remove any visual cue of whom the reply is directed to.

## Remarks
This helper centralizes the cleanup of reply-related state in the orchestrator, ensuring both the internal field `_pendingReply` and the UI controller `_mainWindow` stay in sync when a reply is canceled or completed. By consolidating these two related updates, it prevents the UI from showing a stale reply target and reduces the risk of divergent state between the model and the view.

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


Invalidates the saved refresh token for a specific server identified by its URL. It loads the current configuration with `ConfigManager.Load()`, finds the corresponding entry in `config.SavedServers` by comparing `Url` against the provided `serverUrl` using a case-insensitive match, clears its `RefreshToken` by setting it to `null`, persists the updated configuration via `ConfigManager.Save(config)`, and refreshes the in-memory `_config` reference.

## Remarks
This private helper centralizes token invalidation so in-memory state and persisted configuration stay in sync for a single server, without affecting other servers or entries. It relies on a first-match lookup (`FirstOrDefault`), so if there are duplicates for the same `Url` only the first one will have its token cleared.

## Notes
- If no server matches the provided `serverUrl`, nothing is changed.
- Only `RefreshToken` is cleared; all other server properties remain intact.

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


DedupPath takes a target directory and file name and returns a unique path by appending ' (n)' before the extension until the resulting path does not exist. It derives the base name and extension via `Path.GetFileNameWithoutExtension` and `Path.GetExtension`, and builds successive candidates with `Path.Combine`, stopping when `File.Exists` reports no existing file. It is a private static helper intended for internal use whenever saving a file should not overwrite an existing file.

## Remarks
This single-purpose helper centralizes the common pattern of deduplicating file names in a directory, reducing duplication and ensuring a consistent naming rule across the class. It relies on filesystem state and standard `Path` utilities rather than creating any files itself, so callers must still perform the actual write. Because this routine checks existence prior to use, there is a potential race condition if another process creates the file after the check.

## Example
```csharp
// Example: request a unique path for a new log file
var uniquePath = DedupPath(@"C:\Logs", "log.txt");
```

## Notes
- It does not create the file; it only computes a path that avoids colliding with existing ones.
- It is not inherently thread-safe: concurrent invocations may compute the same candidate path before either writes the file.
- If the directory does not exist, the method will still return a path, but writing will fail until the directory is created.

---

### Dispose
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
public void Dispose()
```

**Returns:** `void`


Disposes the client orchestrator resources in a deterministic teardown sequence: it first persists the last reads, then synchronously disposes the underlying connection, and finally disposes the update service. It calls `PersistLastReads()` to capture any in-flight read positions before teardown, then synchronously disposes the underlying connection with `_conn.DisposeAsync().AsTask().GetAwaiter().GetResult()`, and finally releases `_updateService` via `_updateService.Dispose()`.

## Remarks
This disposal path centralizes resource cleanup and enforces a predictable shutdown order: last reads are saved before the connection is torn down, and the update service is released after the connection has been disposed. By wrapping the asynchronous disposal in a synchronous call, callers are shielded from async lifetimes, but this can block the calling thread and may introduce deadlock risk if invoked on a synchronization context (for example, a UI thread or certain server contexts).

## Notes
- The call to `DisposeAsync` is awaited synchronously via `GetAwaiter().GetResult()`, which can block the current thread and potentially cause deadlocks in contexts with a captured synchronization context.
- Ensure `Dispose` is invoked when no further asynchronous work involving `_conn` or `_updateService` remains to avoid extended blocking during shutdown.

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


Downloads an attachment to a temporary file, decrypting it locally when the current channel is end-to-end encrypted (the server stores those blobs as ciphertext). It fetches the blob via `_conn.Api!.DownloadFileToTempAsync(attachmentUrl, fileName)`, and if a non-empty `channel` is available and `_conn.RoomKeys.TryGetKey(channel, out var roomKey)` succeeds, it reads the downloaded bytes, decrypts them with `RoomCrypto.DecryptBytes(blob, roomKey)`, and writes the result back to the file. If decryption fails for any reason, it logs a warning and returns the path to the file as downloaded (the raw bytes may be preserved). The method returns the path to the temporary file.

## Remarks
Provides a single, centralized mechanism for obtaining attachments that may be encrypted, so callers don't need to handle per-channel key retrieval or decryption logic. It relies on the `RoomKeys` property exposed by the `ConnectionManager` to fetch a per-channel room key and on `RoomCrypto` to perform the actual decryption; this aligns with the application's end-to-end encryption model and keeps sensitive crypto code in one place.

If there is no available channel key or decryption cannot be performed, the method gracefully falls back to returning the downloaded file without throwing, ensuring the download flow remains robust.

## Notes
- Decryption errors are swallowed; only a warning is emitted.
- The returned path may refer to an encrypted file if decryption did not occur.

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


Ensures the end-to-end encryption sending path is guarded by verifying that the target channel is unlocked before any message leaves the client. If the channel is not encrypted or a room key is already cached, it returns true to allow sending. If encryption is active but the key is missing, it prompts to unlock by awaiting `UnlockTrackedChannelAsync(channelName)` and returns true on a successful unlock. If unlocking fails, it surfaces an error to the user via `_mainWindow.ShowError` and returns false, preventing the message from being sent. This guard satisfies the policy that plaintext should never be transmitted in an encrypted channel without the corresponding room key, and it offers the unlock prompt immediately when the user attempts to talk (even after an earlier decline).

## Remarks
This method centralizes the gating logic for encrypted channels, reducing duplication and ensuring consistent UX across the send paths. It relies on the `RoomKeys` contract to determine encryption state and key presence, and on the UI prompt path to surface unlock actions to the user. Returning a boolean enables callers to cleanly short-circuit the actual send operation when protection is required.

## Notes
- Always await the result of this method before attempting to send; the return value determines whether the send should proceed.

---

### FetchAndUpdateOnlineUsers
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void FetchAndUpdateOnlineUsers()
```

**Returns:** `void`


FetchAndUpdateOnlineUsers is a private helper that refreshes the online-user list for the current channel in the background. When there is a valid channel and the connection is active, it kicks off a `Task.Run` to call `_conn.GetOnlineUsersAsync(channel)`, caches the result into `_channelUsers[channel]` under `_channelUsersLock`, and updates the UI via `InvokeUI(() => _mainWindow.UpdateOnlineUsers(users))`. If an exception occurs, the failure is logged with `Log.Debug`.

---

### GetDownloadDir
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private string GetDownloadDir()
```

**Returns:** `string`


Resolves the folder where downloads are written by preferring the configured `ClientConfig.DownloadPath`; if that is not set, it defaults to the `Downloads` folder under the current user's profile. It ensures the directory exists, and returns the resulting path; if directory creation fails for any reason, it logs a warning and falls back to the system temporary directory (`Path.GetTempPath()`).

## Remarks
This method centralizes download-path resolution, so callers do not duplicate environment checks or error handling. It provides resilience against misconfigurations or permission issues by gracefully degrading to a temporary directory and logging the underlying problem for troubleshooting. Encapsulating this logic behind `GetDownloadDir` improves testability and keeps platform-specific path logic out of the call sites.

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


Handles an audio-play request by first validating authentication through `_conn.IsAuthenticated`; if not authenticated, it returns early. When authenticated, it runs an asynchronous workflow via `RunAsync` that downloads the attachment using `DownloadAttachmentAsync(attachmentUrl, fileName)` to obtain the temporary path `tempPath`, and then opens the audio player with `AudioPlayerDialog.Show(_app, _audioPlayback, tempPath, fileName)`.

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


When a message requests joining the channel named `channelName`, `HandleChannelJoinFromMessage` first checks `_conn.IsConnected`. If connected, it marshals a UI update via `InvokeUI` to ensure the channel is present in the UI list and to switch to that channel (`_mainWindow.EnsureChannelInList` and `_mainWindow.SwitchToChannel`). It then calls `HandleChannelSelected(channelName)` to perform any further selection handling.

## Remarks
By design, this method acts as a bridge between message-driven join events and the UI flow. It centralizes the sequence of validating the connection, updating the UI to reflect the new channel, and proceeding with selection logic while ensuring the UI changes occur on the main thread.

## Notes
- The `HandleChannelSelected(channelName)` call executes after the UI marshal and may run on the caller's thread; ensure any UI-affecting logic in that method is thread-safe or marshals back to the UI thread if needed.
- If `_conn.IsConnected` is false, the method returns immediately with no UI updates.

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


Orchestrates the UI and data flow when a user selects a `Channel`, coordinating `_conn` state, `_pendingReply`, and read-position persistence, and it exits early if `_conn.IsConnected` is false.

If the channel is tracked, it may prompt for a password with `JoinChannelWithPasswordPromptAsync` and, on cancellation, untracks the channel and switches to the `HubConstants.DefaultChannel`; on success it calls `UpdateServerConfig` to remove the channel from `LeftChannels` (case-insensitive), then loads history via `_conn.GetHistoryAsync` and updates the UI with `_messageManager.LoadHistory` before refreshing online users; if the channel isn't tracked but requires unlocking, it calls `UnlockTrackedChannelAsync`.

## Remarks
Centralizes channel-switch logic within the app, ensuring consistent UX and alignment between the UI state and the server-side channel state. It encapsulates edge cases such as clearing pending replies (`ClearPendingReply`), persisting reads (`PersistLastReads`), handling password prompts (`JoinChannelWithPasswordPromptAsync`) and unlocks (`UnlockTrackedChannelAsync`), and loading history in a non-blocking way to keep the UI responsive. It coordinates with `_conn`, `_mainWindow`, and `_messageManager` to transition channels smoothly and refresh the online user list after each switch.

## Notes
- History fetch failures are swallowed to preserve responsiveness during channel switches.
- Rapid consecutive channel selections may race UI updates and history loading; consider synchronization to avoid glitches.

---

### HandleCheckForUpdatesRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleCheckForUpdatesRequested()
```

**Returns:** `void`


HandleCheckForUpdatesRequested handles a request to check for updates. It triggers an asynchronous update check by delegating to the update service's `CheckNowAsync` method (accessed through `_updateService`) via the helper `RunAsync`, supplying the user-facing error message 'Failed to check for updates'. By not awaiting the operation, this method keeps the caller responsive while centralizing error handling and feedback through `RunAsync`.

## Remarks
This method acts as a thin orchestration layer that funnels a 'check now' action through a centralized asynchronous runner (`RunAsync`). This design avoids duplicating error handling logic and ensures a consistent user-facing failure message when update checks fail. It also decouples the UI/command trigger from the concrete update-check implementation by routing through `_updateService`.

## Notes
- The call is fire-and-forget: the method returns before the update check completes. If subsequent logic depends on the update result, coordinate through `RunAsync` or other completion signaling.

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


Processes the `HandleCmdAssignRole` command to assign a role to a given `username`. It gates the operation on `_conn.IsAuthenticated`, maps the input `roleStr` to a `ServerRole` (`"admin"` → `ServerRole.Admin`, `"mod"` → `ServerRole.Mod`, otherwise `ServerRole.Member`), and then calls `_conn.Api!.AssignRoleAsync(username, role)` to persist the change.

## Remarks
By centralizing authentication gating and role-string translation in this single method, the code ensures consistent access control for role changes and prevents disparate command handlers from issuing arbitrary role updates. It keeps the orchestration logic close to the command surface while delegating the actual update to the API client (`AssignRoleAsync`). The use of a switch expression makes the mapping explicit and easy to extend if new roles are added.

## Notes
- The code uses a null-forgiving operator on `_conn.Api` (`Api!`); if the API client isn't initialized after authentication, a null reference exception could occur. Ensure API client initialization happens before this handler runs.

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


HandleCmdBanUser is an asynchronous command handler that, when invoked, bans a user by calling the API client, but only if the current connection is authenticated. If `_conn.IsAuthenticated` is false, the method returns immediately and performs no network request. It accepts a `string` username and an optional `string?` reason, and forwards them to `_conn.Api!.BanUserAsync(username, reason)`.

## Remarks
Conceptually, this method encapsulates a moderation action behind the orchestrator, ensuring bans occur only within an authenticated session and through the single `BanUserAsync` pathway. The early return on authentication status prevents unnecessary API calls and aligns with the principle of guarding privileged operations behind authentication checks. The use of the null-forgiving operator on `_conn.Api` expresses an assumption that, once authenticated, the API client is available; if that assumption is violated, a `NullReferenceException` could occur.

## Notes
- This method does not catch exceptions from `BanUserAsync`; callers should handle potential failures from the API.
- Because the method is `private`, it is intended for internal orchestration flows and should not be called from outside the class; ensure the surrounding call sites preserve the authentication precondition.

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


Re-derives the join credential for the active channel from the `oldPassphrase` and `newPassphrase` and re-wraps the cached room content key using the new derived key, so the channel is protected by the new passphrase while the history remains readable. It guards the operation with checks for authentication and connectivity, ensures a valid channel is selected and that the channel is end-to-end encrypted (having a non-null `EncryptionSalt`), derives the old and new credentials, and invokes `RekeyChannelAsync` with a `RekeyChannelRequest`, finally posting a system message about the change.

## Remarks
This function centralizes the rotation of channel encryption material, coordinating the local cryptographic state (`RoomKeys`, `RoomCrypto`) with the remote Rekey flow via `RekeyChannelAsync`. By re-wrapping the existing room key instead of re-encrypting history, it minimizes disruption while enforcing the new passphrase for future access.

## Notes
- If the channel isn't end-to-end encrypted or the `EncryptionSalt` is missing, the operation aborts with a user-visible error.
- If `_conn.RoomKeys` does not contain a key for the channel, the user is prompted to unlock the channel first (rejoin it with its passphrase) before retrying.
- Exceptions are surfaced to the user as `Passphrase change failed: {ex.Message}`.

---

### HandleCmdClearAttachments
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdClearAttachments()
```

**Returns:** `Task`


Private `Task` method `HandleCmdClearAttachments` clears all attachments that have been staged for the current operation, typically invoked when the user clears the staging area. It first identifies attachments in `_stagedAttachments` that also exist in `_tempPastedFiles`, removes those from `_tempPastedFiles`, cleans up the corresponding temporary files via `CleanupPastedTempFiles`, clears the `_stagedAttachments` collection, and finally refreshes the staging UI by calling `InvokeUI(RefreshStagingTray)`, then returns `Task.CompletedTask`.

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


HandleCmdCreateInvite coordinates the creation of a channel invite by validating that the user is authenticated via `_conn.IsAuthenticated` and that a current channel exists via `_mainWindow.CurrentChannel`, exiting early otherwise. It uses `RunAsync` to call `_conn.Api.CreateInviteAsync(maxUses, expiresHours)` and, if successful, posts a system message containing the invite code, usage limit, optional expiry, and a revoke hint via `_messageManager.AddSystemMessage`.

## Remarks
`HandleCmdCreateInvite` acts as the orchestrator for the invite-creation workflow, bridging the authentication state, UI channel context, and the invite-creation API. By performing the network call inside `RunAsync`, it prevents UI blocking, and it conditionally formats and displays expiry information only when `Invite.ExpiresAt` is provided, embedding the expiry in the final system message along with a clear revoke command.

## Notes
- It accesses `_conn.Api` with a null-forgiving operator; if `_conn.Api` ends up null even when `_conn.IsAuthenticated` is true, this could throw a runtime exception.

---

### HandleCmdDeleteAccount
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdDeleteAccount()
```

**Returns:** `Task`


HandleCmdDeleteAccount orchestrates the UI-driven account-deletion workflow. It first checks that the client is authenticated and returns a completed task if not; it then uses `InvokeUI` to display a confirmation dialog (title `Delete Account`) that warns this action permanently deletes `'{_session.Username}'` on this server, including the profile, sessions, and uploaded files, and aborts if the user does not confirm. If the user confirms, it prompts for the password via `PromptPassword`; a non-null password proceeds to persist pending reads, then runs an asynchronous sequence that calls `_conn.Api!.DeleteMyAccountAsync(password)`, clears the saved token for `_conn.Api!.BaseUrl` with `ClearSavedToken`, and performs cleanup with `_conn.CleanupAsync()`. Upon completion, it returns to the UI thread to clear the main window, update the status bar to `Disconnected`, and display an account-deleted confirmation with `MessageBox.Query`.

## Remarks
This symbol acts as a cohesive coordinator for a dangerous, user-triggered operation. By wrapping authentication checking, user confirmation, password validation, server deletion, and local state cleanup in a single flow, it ensures consistent UI feedback and a clean disconnected state only after the server reports success (and it marshals UI updates back to the main thread).

## Notes
- The code accesses the API client via `_conn.Api!`, i.e. it force-unclaims a non-null API instance; if `_conn.Api` is unexpectedly null, this will throw.
- The operation performs network I/O and UI updates; the error context passed to `RunAsync` is `"Account deletion failed"` with the qualifier `"DeleteAccount"`.


---

### HandleCmdExportData
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdExportData()
```

**Returns:** `Task`


HandleCmdExportData coordinates the data-export workflow for the current user: if the user is authenticated (via `_conn.IsAuthenticated`), it asynchronously requests a data export from the API (`_conn.Api!.ExportMyDataAsync()`), saves the resulting JSON to a timestamped file named `echohub-export-{_session.Username}-{DateTime.Now:yyyyMMdd-HHmmss}.json` in the download directory (using `GetDownloadDir()` and `DedupPath(...)`), and posts a system message to the current channel with the file path and a ciphertext notice. The operation runs off the UI thread via `RunAsync` and reports failures with the error label `Export failed`.

## Remarks
This private method encapsulates authentication gating, network I/O, disk I/O, and UI notification into a single orchestrator, keeping the higher-level command logic clean. It ensures the potentially long-running export executes in the background, while the user is informed about the result through a channel message, including a ciphertext note clarifying data sensitivity.

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


Handles the user command to join a channel by ensuring the client is connected (`_conn.IsConnected`), optionally prompting for a password via `JoinChannelWithPasswordPromptAsync`, and then updating server state and the UI to reflect the join. On success it removes the channel from the left-channel exclusion by updating `LeftChannels` (using `StringComparison.OrdinalIgnoreCase`), ensures the channel is in the UI list, switches to the channel, and loads history if available; on failure it reports an error via the main window.

## Remarks
Coordinates asynchronous operations across networking, server state, and the UI, ensuring that a successful join results in a visible channel with history loaded when available. The explicit removal from `LeftChannels` guarantees that a join overrides any prior exclusion and keeps the UI state consistent.

## Notes
- Exceptions raised inside `InvokeUI` may not be caught by this method's `catch` block since they occur asynchronously on the UI thread.

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


Handles the 'kick user' command by first verifying that the connection is authenticated and, if so, delegating the action to the remote API via `KickUserAsync`, passing the target `username` and the optional `reason`. If the client is not authenticated, the method returns early and performs no remote action. This private asynchronous method relies on `_conn.Api` being non-null when authenticated (as indicated by the null-forgiving operator).

## Remarks
This symbol serves as the orchestration boundary between command handling and the backend action. It encapsulates the authentication gate and the API call, making behavior consistent for kick operations. Because the method is asynchronous and returns a `Task`, exceptions from `KickUserAsync` propagate to the caller awaiting this method.

## Notes
- Silent path when not authenticated: the method returns without any notification or logging, which may require the caller to enforce authentication at a higher level.
- The code uses `_conn.Api!` to dereference the API client; if `_conn.Api` is null even when authenticated, a `NullReferenceException` could occur at runtime. Ensure the API client is initialized alongside authentication.

---

### HandleCmdLeaveChannel
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdLeaveChannel()
```

**Returns:** `Task`


Leaves the channel currently selected in the UI by validating the connection and channel state, and, if allowed, calls `_conn.LeaveChannelAsync(channel)`. It then updates the server's `LeftChannels` to prevent auto-join on reconnect and posts a system message noting the leave. If an error occurs, a user-facing error is shown.

## Remarks
This method enforces a minimal, defensive flow: it refuses to leave the default channel, and it deduplicates entries in `LeftChannels` using a case-insensitive comparison, so leaving the same channel twice won't clutter configuration. UI feedback is marshaled to the main thread via `InvokeUI`, ensuring the user sees updates without threading issues. The approach keeps leave behavior isolated from other channel-management paths, reducing the chance of unintended re-joins after disconnection.

## Notes
- Early returns on preconditions (not connected or no channel selected) are intentional quiet-paths; callers should drive user feedback as needed.
- Returning and reporting errors exposes `ex.Message` to the UI; consider guarding or sanitizing in production if exposing internal details is undesirable.

---

### HandleCmdListInvites
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdListInvites()
```

**Returns:** `Task`


Lists invite codes for the currently selected channel when the user is authenticated. If authenticated and a channel is present, it fetches invites via `_conn.Api.GetInvitesAsync()` and posts the results as a system message to the channel; otherwise it returns immediately.

## Remarks
This private handler centralizes the invites-listing behavior for the channel command, including gating on authentication and channel context, asynchronous data retrieval, and UI notification. It formats each invite with its code, usage, and creator, and derives a textual state per invite (active, expired, or used up) based on `ExpiresAt`, `UseCount`, and `MaxUses`.

## Notes
- Potential NullReference: `_conn.Api` is accessed with a null-forgiving operator (`!`); ensure an API client is available after authentication.
- UI/text considerations: the resulting message could be long if many invites exist; consider UI constraints or paging if needed.



---

### HandleCmdListUsers
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdListUsers()
```

**Returns:** `Task`


HandleCmdListUsers is a private asynchronous method that fetches the online users for the currently selected channel and displays them as system messages in the UI. It is invoked when the user wants to refresh or view the current channel’s presence, and it guards against performing work when the connection is not established or no channel is selected. The method retrieves the user list via `_conn.GetOnlineUsersAsync(channel)` and updates the UI on the main thread using `InvokeUI`, formatting each entry as the user’s display name (falling back to `Username`) and their textual `Status`, with an optional status message appended when present. If the fetch fails, it surfaces an error through `_mainWindow.ShowError` to provide immediate feedback to the user.

## Remarks
This symbol encapsulates the end-to-end flow for presenting channel presence: it coordinates the network call, localizes the results into a human-friendly list, and marshals updates to the UI thread. By centralizing the formatting logic (name fallback, status text construction, and conditional status message), it ensures a consistent presentation of user presence across channels. It relies on the `Status` value representation to render each user’s state and uses system messages to render the output within the active channel context.

## Notes
- Assumes `user.Status` is non-null since `user.Status.ToString()` is invoked directly; a null-status could throw at runtime.
- The method exits early if `_conn.IsConnected` is false or if the `channel` string is null or empty, preventing unnecessary work or errors.
- Updates to the UI are performed via `InvokeUI` to ensure thread affinity for UI operations.


---

### HandleCmdMeta
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdMeta()
```

**Returns:** `Task`


HandleCmdMeta fetches and displays the current channel's metadata when the client is connected and a channel is selected. It calls `_conn.Api.GetChannelMetaAsync(channel)` to obtain a `meta` object and, if present, updates the UI with a structured room summary (topic, room ID, created time, message count, unique users, Est. size, and protection status); if the meta is null, it reports an error, and any exceptions are surfaced as a failure to fetch room info.

## Remarks
This symbol acts as a UI-facing orchestrator that decouples network data retrieval from presentation. It validates connection and channel state, relies on `_messageManager` to emit system messages, and uses `ChatMessageManager.FormatFileSize` to render a friendly size. UI updates are marshalled with `InvokeUI` to ensure they run on the UI thread, avoiding cross-thread issues.

## Notes
- The method exits early when `_conn.IsConnected` is false or `_conn.Api` is null, which can yield a silent no-op unless the caller ensures connectivity.
- The room topic line is emitted only if `meta.Topic` is non-empty; otherwise, the topic line is omitted, keeping the display concise.


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


Handles the mute-user command as a small, focused operation within the command-handling layer. `HandleCmdMuteUser` validates that the connection is authenticated and, only if it is, forwards the mute request to the backend via `await _conn.Api!.MuteUserAsync(username, duration)`, passing along the required `username` and the optional `duration` parameter. If the client is not authenticated, the method returns without invoking the API. The use of the null-forgiving operator `!` on `_conn.Api` communicates the expectation that the API reference is non-null once authentication is established. This method serves as a thin wrapper that encapsulates authentication gating and the actual mutation call for muting a user, centralizing this concern in one place and keeping command-handling logic focused and consistent.

## Remarks
This method provides a small abstraction that couples an authentication gate with a single backend action, isolating command interpretation from the network call. By funneling muting through this single point, higher-level command handlers avoid duplicating authentication checks or direct API invocation patterns, improving maintainability and reducing the surface area for mistakes when muting users. It also makes it easier to swap or mock the backend mutation (`MuteUserAsync`) without touching command parsing code.

## Notes
- Be mindful of potential timing-related edge cases: the guard `_conn.IsAuthenticated` is checked before awaiting; if authentication state changes between the check and the `MuteUserAsync` call, the eventual action may diverge from the guard's intent.
- The code uses `_conn.Api!` with the null-forgiving operator, implying an invariant that `Api` will be non-null after authentication. If that invariant is violated, a runtime `NullReferenceException` may occur during the mute call.


---

### HandleCmdNukeChannel
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdNukeChannel()
```

**Returns:** `Task`


HandleCmdNukeChannel is a private asynchronous command handler that nukes the currently selected channel by delegating to the backend API, but only when the user is authenticated and a channel is selected. It acts as a UI-to-backend bridge, encapsulating the preconditions and the destructive operation behind a single, reusable command.

## Remarks
HandleCmdNukeChannel coordinates UI state and server actions, reading the active channel from `_mainWindow.CurrentChannel` and performing the API call via `_conn.Api!.NukeChannelAsync(channel)`. By centralizing the checks for authentication and channel presence, callers can trigger this operation without duplicating guard logic. The method uses the null-forgiving operator on `Api` to assume the API client is initialized after authentication; if that assumption ever fails, a `NullReferenceException` could occur.

## Notes
- No user confirmation or result handling is performed here; callers should ensure the user intends to perform the destructive action or higher-level UI handles confirmation.
- Exceptions from `NukeChannelAsync` are not caught here and will bubble up to the caller.
- The `Api!` usage means a potential null reference if the API client isn't initialized despite authentication.

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


An internal helper that, given an optional `string?` username, schedules a UI action to view the corresponding profile by invoking `HandleViewProfile(username)` on the UI thread via `InvokeUI`, and then completes synchronously with `Task.CompletedTask`. It is a non-blocking bridge between command handling and UI navigation.

## Remarks
InvokeUI ensures the delegate runs on the UI thread, which is required for UI interactions. `HandleCmdOpenProfile` serves as a thin command-to-UI bridge, translating a request to open a profile into a UI update without performing navigation itself, keeping concerns separated.

## Notes
- Returns immediately with a `Task.CompletedTask`; callers should not rely on the returned task to reflect the completion of the UI navigation.

---

### HandleCmdOpenServers
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdOpenServers()
```

**Returns:** `Task`


Responds to the 'open servers' command by routing control to the UI layer. It forwards execution to `HandleSavedServersRequested` via `InvokeUI` and then returns `Task.CompletedTask`, providing a Task-based contract while delegating the actual work to the UI flow.

## Remarks
This method acts as a thin adapter in the command-handling path: it decouples the command initiation from the details of how the UI presents saved servers. By returning `Task.CompletedTask` immediately, it preserves an asynchronous signature without performing asynchronous work itself, which simplifies orchestration and makes the command plumbing easier to test, while the real work is performed by `HandleSavedServersRequested` in the UI layer.

## Notes
- Private method: `HandleCmdOpenServers` is declared as `private`; callers outside the class cannot invoke it directly, so tests should exercise the public API that triggers this path.

---

### HandleCmdQuit
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdQuit()
```

**Returns:** `Task`


HandleCmdQuit is a private command handler that, when invoked, marshals a shutdown request to the UI thread by calling `_app.RequestStop()` through `InvokeUI`, and immediately completes with `Task.CompletedTask`. This pattern lets command-processing logic trigger application termination without blocking, while ensuring the actual stop logic runs on the UI thread.

## Remarks
This symbol acts as a small adapter at the UI boundary: it marshals the quit request to the UI thread via `InvokeUI`, ensuring `RequestStop()` runs in the correct context. By returning `Task.CompletedTask`, it keeps the command-handling path asynchronous without blocking for the stop operation.

## Notes
- The returned `Task` is already completed; callers should not rely on it to reflect the actual duration of shutdown.

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


Revoke an invite by its `code` only when the user is authenticated and a channel is currently active. If either condition is not met, it returns `Task.CompletedTask` immediately. When both conditions hold, it runs the revoke in a background task by calling `_conn.Api!.RevokeInviteAsync(code)` and, on success, posts a system message to the current channel via `_messageManager.AddSystemMessage` stating the invite was revoked, displaying the code in upper-case via `code.ToUpperInvariant()`.

## Remarks
: Acts as a small orchestrator that coordinates the authentication state (`_conn.IsAuthenticated`), the UI channel context (`_mainWindow.CurrentChannel`), and the remote API call (`_conn.Api!.RevokeInviteAsync`). By executing the API call through `RunAsync` and then updating the UI with `_messageManager.AddSystemMessage` (using the message `Invite {code.ToUpperInvariant()} revoked.`), it preserves UI responsiveness and provides consistent feedback for invite revocation commands.

## Notes
- Potential null-reference risk: `_conn.Api` is accessed with the null-forgiving operator `!`; if API is null despite being authenticated, this will throw. Ensure `_conn.Api` is initialized after authentication.

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


HandleCmdSendAction takes a text input and sends it as a CTCP ACTION to the currently selected channel, but only when the client is connected and a channel is chosen. It routes the content through the normal send path (including room encryption) by first ensuring the room is unlocked, then dispatching `_conn.SendMessageAsync(channel, MessageConventions.FormatAction(text))` inside a `RunAsync` task that reports a 'Send failed' error on failure.

## Remarks
Acts as a small coordination layer that centralizes preconditions for action sending (connected state and channel validity) and reuses the existing asynchronous send pipeline. By funneling CTCP ACTION messages through the same encryption-aware path, it ensures consistent treatment with regular messages.

## Notes
- The method returns `Task.CompletedTask` early when not connected or channel is empty; no exception is thrown in those cases.

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


The `HandleCmdSendBanner` method coordinates turning user input into an ASCII banner and delivering it to the currently selected channel. It returns immediately with `Task.CompletedTask` when the client is not connected or no channel is selected; otherwise it renders the text via `AsciiBannerService.Render` and, if rendering succeeds, sends the banner to the channel after confirming the room is unlocked with `EnsureRoomUnlockedForSendAsync`. If rendering fails, it surfaces a UI error through `InvokeUI` that explains the allowed characters and the maximum input length defined by `AsciiBannerService.MaxInputLength`.

## Remarks
Orchestration role: coordinates input, rendering, and delivery; delegates rendering to `AsciiBannerService` and network transport to `_conn`, while using `RunAsync` to avoid blocking the UI. This keeps concerns separated and ensures a clear failure path with UI feedback when rendering fails.

## Notes
- If `AsciiBannerService.Render` returns null, the user is notified with a UI error; this enforces supported input during rendering.
- If `EnsureRoomUnlockedForSendAsync` returns false, the send is aborted and no message is sent.

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


HandleCmdSendFile orchestrates the logic for sending a file or URL in the current channel. It validates authentication and channel presence, then branches between URL-based sends and local-file staging. URL sends are dispatched via `_conn.Api!.SendUrlAsync` when the channel is not encrypted; if the channel has no cached keys or is encrypted, the code shows an error instructing the user to download the file and send it instead. Local files are added to `_stagedAttachments` for the next Enter-send, subject to the per-message limit defined by `HubConstants.MaxAttachmentsPerMessage`; if the limit is reached, the UI shows an error. If a size hint is provided, `NormalizeAsciiSize(size)` is used to update `_config.DefaultAsciiSize`. In all cases, the code may invoke UI updates via `InvokeUI` and then returns a `Task` (often `Task.CompletedTask`) after scheduling any asynchronous work with `RunAsync`.

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


Handles the command to configure the ASCII-art rendering size for attached images: if a valid size is provided via `NormalizeAsciiSize(args)`, it applies that size immediately on the UI thread by invoking `ApplyAsciiSize` through `InvokeUI`. If not, it shows a size picker via `MessageBox.Query` offering Small/Medium/Large, maps the selected option to the codes `s`, `m`, or `l`, and applies the chosen size with `ApplyAsciiSize`; the resulting selection is persisted as a user preference for subsequent attachments.

## Remarks
This method centralizes the ASCII size selection flow, bridging command handling with UI interaction and persisting the preference. It marshals UI updates via `InvokeUI` and relies on `_app` as the owner context for the `MessageBox.Query` prompt, ensuring consistent behavior whether the size is provided directly or chosen interactively.

## Example
```csharp
// Directly set to medium via shorthand
await HandleCmdSetAsciiSize("m");

// Open the interactive size picker
await HandleCmdSetAsciiSize("");
```

## Notes
- Canceling the interactive picker results in no change.
- The method always completes a Task promptly; actual application of the size occurs on the UI thread via `InvokeUI` when required.

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


The command handler `HandleCmdSetAvatar` encapsulates the flow for updating a user avatar in response to the corresponding command. It first guards the operation behind an authentication check and only proceeds when the current connection is authenticated. On a valid path, it performs an asynchronous upload via `AvatarHelper.UploadAsync`, passing the API client from the connection and the provided `target` path. If the upload succeeds and there is a non-empty `CurrentChannel` on the main window, it marshals a UI update to the main thread to display the system message `Avatar updated.` through the `_messageManager`. If any exception occurs during the upload, the method logs the error with the target context using `Log.Error` and surfaces a user-facing error through the main window by invoking `ShowError` with the exception message. This design isolates the command handling, backend interaction, and UI feedback, promoting responsive behavior and centralized error reporting.

## Remarks
The symbol coordinates several collaborators to perform a single user-facing action: `_conn` provides authentication state and access to the API client, `AvatarHelper` handles the actual upload, `_mainWindow` supplies context for UI and error presentation, and `_messageManager` publishes the system message to the current channel. The operation is asynchronous to avoid blocking the UI thread, and any UI updates are marshaled via `InvokeUI` to maintain thread safety.

## Notes
- The method returns early if `_conn.IsAuthenticated` is false, ensuring the command does not attempt an upload without a valid session.
- Exceptions during upload are caught broadly, logged with contextual information, and a user-visible error is shown. Be mindful of exposing sensitive details from `ex.Message` in production; consider wrapping or localizing error strings if needed.

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


HandleCmdSetColor is a private asynchronous command handler that applies a user-specified color to the user's nickname by persisting it through the API when the user issues a color-change command. It guards against unauthenticated invocations by returning early if `_conn.IsAuthenticated` is false; when authenticated, it calls `_conn.Api!.UpdateProfileAsync` with a new `UpdateProfileRequest` that sets `NicknameColor` to the provided `color`.

## Remarks
This method acts as a small boundary between the command-parsing layer and the profile service. By gating on authentication and delegating to `UpdateProfileAsync`, it centralizes the persistence of nickname color changes and keeps command handling decoupled from the details of the API surface.

## Notes
- No local validation of `color` is performed; invalid values may be rejected by the API.
- Assumes `_conn.IsAuthenticated` implies `_conn.Api` is non-null; the `Api!` usage suppresses potential null-reference warnings, which could be problematic if the assumption ever fails.
- Exceptions from `UpdateProfileAsync` propagate to the caller; no internal error handling is performed here.

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


Handles the download path configuration command by either accepting a path argument to set the folder directly, or, when no argument is supplied, attempting to open the OS-native folder picker. If the picker is unavailable (for example in headless environments), it informs the user how to set the path manually. The method coordinates with the app configuration, computes the current directory via `GetDownloadDir()`, and applies changes through `SetDownloadPath`. All user-facing updates are dispatched on the UI thread via `InvokeUI`, and outcomes from the native picker drive the corresponding messages.

## Remarks
Encapsulates the cross-cutting concern of configuring the download location behind a single command handler. It separates OS-specific interaction (`NativeFolderPicker`) from the app logic, centralizing how the path is stored (`_config`), derived (`GetDownloadDir()`), and presented to the user via `_messageManager` and `_mainWindow`. This design keeps the command surface stable while adapting to environments with or without a native picker.

## Example
```csharp
// Directly set the download path
await HandleCmdSetDownloadPath(@"D:\Downloads");

// Or invoke the native picker (no argument)
await HandleCmdSetDownloadPath(string.Empty);
```

## Notes
- The actual path change when using the picker occurs asynchronously inside `RunAsync`; the method returns immediately while the picker is shown.
- If the user cancels or the native picker is unavailable, the user receives a system message indicating that the download folder remains unchanged or that a path must be provided.

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


Private async method `HandleCmdSetNick` that updates the authenticated user's display name on the server and refreshes the UI to reflect the change. It returns early if not authenticated (`_conn.IsAuthenticated`). When authenticated, it awaits `_conn.Api!UpdateProfileAsync(new UpdateProfileRequest(DisplayName: displayName))` and then dispatches UI updates through `InvokeUI` to execute `_mainWindow.SetCurrentUser(displayName)` and `_mainWindow.UpdateStatusBar("Connected")`.

## Remarks
This method encapsulates the end-to-end flow for nickname changes by coordinating a server profile update and the corresponding UI refresh. It guards the operation behind authentication and uses `InvokeUI` to ensure UI updates run on the UI thread, keeping the server state and client state synchronized.

## Notes
- The code relies on a non-null assertion `_conn.Api!`; if authentication succeeds but `Api` is null, a NullReferenceException may occur.
- Exceptions from `UpdateProfileAsync` propagate to the caller; there is no internal error handling within this method.

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


Handles the client-side processing of the user status command by updating both the server-side status and the local session state. When connected, it resolves the new status as either the provided `status` or retains the current `_session.Status`, and resolves the new message as either the provided `message` or retains the current `_session.StatusMessage`; an empty `message` clears it. It then calls `_conn.UpdateStatusAsync(newStatus, newMessage)` and applies those changes to `_session.Status` and `_session.StatusMessage`.

## Remarks
By treating `null` status as 'keep current' and an empty string for `message` as 'clear', this method supports convenient command usage like `/status away` or `/status msg brb` without duplicating logic elsewhere. It also ensures the server state and the local session stay in sync in a single operation, preventing drift between UI intent and remote state.

## Notes
- No action is performed when the client is not connected (`_conn.IsConnected` is false).


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


Thin command handler that forwards a theme change to the UI by marshaling the provided `name` to the UI thread via `InvokeUI`, where `HandleThemeSelected(name)` executes. It then returns a completed `Task`, making it suitable for async command pipelines without awaiting the UI work.

## Remarks
This method acts as a lightweight adapter between command processing and UI logic, ensuring the actual theme application runs on the UI thread while callers receive an immediately completed `Task`. The heavy lifting happens in `HandleThemeSelected`; `InvokeUI` guarantees thread affinity for that operation. Because the returned `Task` is completed synchronously, any exceptions raised during the UI invocation occur on the UI thread and are not propagated to the caller.

## Dependencies
- `Task`

## Dependency APIs
- `System.Threading.Tasks.Task` — Represents an asynchronous operation; the method uses `Task.CompletedTask` to return a completed task.

## Notes
- The method does not propagate exceptions from the UI callback to the caller; consider UI-dispatch error handling if you need observable failures.


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


HandleCmdSetTopic(string topic) asynchronously handles the command to change a channel's topic by first ensuring the user is authenticated via `_conn.IsAuthenticated` and that a current channel exists via `_mainWindow.CurrentChannel`, returning early if either check fails. It then calls `_conn.Api!.UpdateChannelTopicAsync(channel, topic)` to persist the change and, on success, uses `InvokeUI` to update the channel topic via `_mainWindow.SetChannelTopic(channel, topic)` and emit a system message with `_messageManager.AddSystemMessage(channel, $"Topic set to: {topic}")`; if an error occurs it uses `InvokeUI` to show an error via `_mainWindow.ShowError($"Failed to set topic: {ex.Message}")`.

## Remarks
This method serves as the bridge between command input, the API, and the UI for topic updates. It centralizes the flow: guard checks, remote update, then UI refresh, so callers need only invoke this method to apply topic changes; UI is refreshed and a system message is emitted on success, while errors are surfaced to the user.

## Notes
- Silent exit when not authenticated may confuse users; consider surfacing a message earlier.
- Topic validation is not performed inside the method; upstream code should ensure topics meet length/character constraints.
- The code uses the null-forgiving operator `_conn.Api!` which assumes a non-null API after authentication; if `_conn.Api` is unexpectedly null, an exception will occur.

---

### HandleCmdTestSound
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdTestSound()
```

**Returns:** `Task`


HandleCmdTestSound is a private asynchronous command handler that delegates the actual sound playback to the `_notificationSound` subsystem by calling `PlayTestAsync()`. It is invoked in response to a command to emit a test notification sound, keeping the orchestration logic separate from the playback implementation.

## Remarks

This method acts as a thin wrapper around the notification subsystem, routing the 'test sound' command to playback without duplicating logic in the orchestrator. By delegating to the `_notificationSound`'s `PlayTestAsync()` method, it keeps the orchestration layer decoupled from the concrete playback implementation, enabling easy swapping or mocking of the provider in tests. It serves as a focused entry point for diagnostic feedback that is isolated from primary command handling flow.

## Notes

- This method does not catch exceptions; exceptions from `PlayTestAsync` propagate to the caller.

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


This private method handles the unban-user command by issuing the API call to unban a user, but only when the client is authenticated. If `_conn.IsAuthenticated` is false, it returns early; otherwise it calls `_conn.Api!.UnbanUserAsync(username)` to perform the unban.

## Remarks
Represents a thin orchestration boundary between the UI/command layer and the API layer. It assumes `_conn.Api` is non-null when authenticated (hence the null-forgiving operator) and relies on the backend to enforce actual permissions and validation.

## Notes
- Potential NullReferenceException if `_conn.Api` is null after authentication due to the `!` operator.
- No internal error handling; exceptions from `UnbanUserAsync` propagate to the caller, so higher layers should handle them as needed.

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


This private command handler enforces an authentication guard before mutating user state and then forwards the unmute request to the API. It checks `_conn.IsAuthenticated` and, when true, awaits `_conn.Api!.UnmuteUserAsync(username)`; otherwise it returns immediately. It acts as the bridge between command handling in the `AppOrchestrator` and the remote user-management API.

## Remarks
This symbol's role is to encapsulate the small orchestration step required for user moderation commands: verify authorization then perform the API call. It helps keep command parsing separate from the network layer, making the flow easier to test and reason about.

## Notes
- The null-forgiving operator on `_conn.Api` means a null API instance could throw at runtime if authentication is true but initialization failed; ensure `_conn.Api` is non-null whenever `_conn.IsAuthenticated` is true.

---

### HandleConnect
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleConnect()
```

**Returns:** `void`


HandleConnect orchestrates the user-initiated connection workflow: if a session is already connected, it prompts to disconnect; otherwise it shows the connect dialog and performs an asynchronous connect. On success it updates the UI with the current user and channels, switches to the default channel, loads per-channel histories using saved last-read markers to seed unread counts, focuses the input, and refreshes online users. If a saved session expires, it clears the token and shows a session-expired dialog; if the server version differs from the client, it prompts to continue or disconnect, and finally it saves the server configuration.

## Remarks
This method acts as the connection lifecycle orchestrator, coordinating the connection service with UI initialization (user, channels, histories) and per-server state persistence (last-read markers and saved servers) to bootstrap a server session.

---

### HandleCreateChannelRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleCreateChannelRequested()
```

**Returns:** `void`


HandleCreateChannelRequested coordinates the user-initiated flow to create a new channel: it verifies authentication and connectivity, shows the `CreateChannelDialog`, and, if the user confirms, starts an asynchronous sequence to create and join the channel. For password-protected channels, it derives the join credential locally (via `RoomCrypto`), wraps the room key, and stores it in `RoomKeys`, ensuring the passphrase never leaves the client, after which history is loaded and the UI is updated.

## Remarks
This method acts as a high-level orchestrator between the UI, remote API, and local key management. By keeping the end-to-end encryption setup on the client and centralizing channel creation logic here, it reduces surface area for password handling elsewhere and ensures key material is managed in a single, auditable flow.

## Notes
- The code assumes `_conn.Api` is non-null (uses the null-forgiving operator); if `Api` is null, a runtime exception will be thrown.
- Password validation enforces `ValidationConstants.MinChannelPasswordLength`; if the password is too short, the operation aborts with a user-visible error.
- If `CreateChannelDialog` returns null, the operation is canceled gracefully.

---

### HandleDeleteChannelRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleDeleteChannelRequested()
```

**Returns:** `void`


This private method `HandleDeleteChannelRequested` handles the user-initiated flow for deleting a channel. It first validates the current connection and authentication, ensures a channel is selected and that it is not the default channel, and surfaces appropriate errors when any check fails. If the user confirms deletion, it asynchronously calls the server API to delete the channel, unregisters the channel locally, and updates the UI to switch to the default channel while posting a system message that the channel has been deleted.

## Remarks
By centralizing this sequence in `HandleDeleteChannelRequested`, the UI layer coordinates prompts, server calls, and state updates in one cohesive flow, ensuring consistent feedback to the user and a single source of truth for channel deletion. It relies on `MessageBox.Query` for confirmation and `RunAsync` to perform the deletion without blocking the UI, with UI updates marshalled via `InvokeUI`.

## Notes
- Be aware that `_conn.Api` is accessed with a null-forgiving operator `!`; if the API client is null at runtime, this will throw.
- The deletion flow includes a UI dispatch via `InvokeUI` to ensure updates occur on the UI thread.

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


Handles a user-initiated request to delete a message by its `Guid` ID, short-circuiting when the client is not authenticated. It delegates the deletion to the server via `_conn.Api!.DeleteMessageAsync(messageId)` inside `RunAsync`, relying on the server-enforced hierarchy and broadcast of deletions; the client updates its local list when the event arrives.

## Remarks
It acts as a minimal, authority-checking conduit that funnels user-initiated deletions through the central API. By delegating to the server for permission checks and relying on a broadcast to synchronize local state, it decouples client-side logic from server-side policy and keeps the UI in sync with server state.

## Notes
- This method returns early if the client is not authenticated, so callers must ensure authentication before invocation.

---

### HandleDisconnect
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleDisconnect()
```

**Returns:** `void`


HandleDisconnect orchestrates the client’s graceful shutdown when disconnecting from the server. It logs the disconnect intent via `Log.Information("Disconnecting from server")`, clears the current channel-user state under `_channelUsersLock` with `_channelUsers.Clear()`, resets `_pendingReply`, and clears the reply UI through `InvokeUI(() => _mainWindow.SetReplyingTo(null))`, then persists the last reads with `PersistLastReads()` and launches an asynchronous cleanup using `RunAsync` with the error caption `Disconnect error` and operation name `Disconnect`. The background task awaits `_conn.CleanupAsync()` and, upon completion, updates the UI to a disconnected state by calling `_mainWindow.ClearAll()` and `UpdateStatusBar("Disconnected")`. This centralizes the disconnect workflow so a caller can initiate a clean shutdown without managing the sequence, while ensuring resources are released and the UI reflects the final state.

## Remarks
This single abstraction coordinates the teardown of the client connection across channel-state, persistence, and the UI. It protects shared state with `_channelUsersLock` and uses `InvokeUI` to marshal UI updates, ensuring the disconnection sequence remains thread-safe. The asynchronous cleanup via `RunAsync` keeps the UI responsive while releasing the network resources held by `_conn`.

## Notes
- The final UI refresh occurs after `_conn.CleanupAsync()` completes; during cleanup the UI may still reflect a connected state.
- UI updates are marshaled via `InvokeUI` to the UI thread, ensuring that `_mainWindow.ClearAll()` and `UpdateStatusBar("Disconnected")` execute safely on the correct thread.

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


Handles the profile-edit workflow by presenting a dialog with the current profile values and, upon confirmation, updating the server, UI, and local configuration when the user is authenticated. It shows `ProfileEditDialog` pre-populated from `currentProfile`, and if the user confirms (i.e., `editResult` is not null), it runs an asynchronous sequence that updates the profile via `UpdateProfileRequest`, refreshes the UI with the new display name, optionally uploads a new avatar through `AvatarHelper.UploadAsync`, and applies notification settings before persisting the changes as the default preset.

## Remarks
This method centralizes the profile-edit lifecycle, coordinating server state, user interface updates, and local configuration. It performs I/O-bound work asynchronously to keep the UI responsive and isolates avatar upload errors so they do not block the rest of the update; errors are logged and surfaced to the user without aborting the overall profile refresh.

## Notes
- Cancelling the dialog (i.e., `editResult` is null) results in no changes.
- Avatar upload failures are logged and shown to the user, but do not prevent the rest of the profile update from applying.
- The final `AccountPreset` is derived from the edited values and saved via `ConfigManager.Save`; any null fields in the result will be stored as null in the preset.

## Symbol To Document
- Name: `HandleEditProfile`
- Kind: `method`
- File: `src/EchoHub.Client/AppOrchestrator.cs`
- Language: `csharp`
- ID: `44c28ba8-51ca-4c05-88c3-63a6940e49b4`

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


Handles requests to download a file attachment. If `_conn.IsAuthenticated` is false, it exits immediately. It schedules an asynchronous operation via `RunAsync` that informs the UI about the download start by calling `_messageManager.AddSystemMessage` on the current channel (`_mainWindow.CurrentChannel`), downloads the attachment with `DownloadAttachmentAsync` into a temporary path (`tempPath`), then moves the file to a deduplicated destination produced by `DedupPath(GetDownloadDir(), fileName)`, and posts a UI message noting the final location (`destination`). If the extension of `fileName` is in `SafeOpenExtensions` (checked via `Path.GetExtension(fileName)`), it attempts to launch the file with the system default application using a `ProcessStartInfo` with `UseShellExecute = true` and `Process.Start`; any exception is caught and logged with `Log.Warning`.

## Remarks
Bundling the download flow inside this method centralizes user-facing download behavior, UI signaling, and error handling. It cleanly gates the operation behind authentication and encapsulates the file opening logic for known safe extensions, reducing duplication elsewhere in the codebase.

## Notes
- Operations run inside the `RunAsync` call are asynchronous and may complete after the method returns; failures during download are surfaced via UI messages or logging, and failures during opening are captured by `Log.Warning`.

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


Stages a batch of local files (from multi-file paste or drag-and-drop) as attachments for the next message, performing the operation on the UI thread to keep the staging list in sync with user actions. It exits early if the connection is not authenticated or not connected, then computes the remaining attachment slots as `HubConstants.MaxAttachmentsPerMessage - _stagedAttachments.Count` and enqueues up to that many files from `files` with `files.Take(Math.Max(0, slotsLeft))`. If more files are provided than can fit, it shows an error through `_mainWindow.ShowError` and finally refreshes the staging tray with `RefreshStagingTray()`.

## Remarks
This function encapsulates the UI-bound attachment staging, guarding against overflows by enforcing the per-message limit at the moment files are staged. It also communicates issues to the user when files exceed the available slots, via `_mainWindow.ShowError`, ensuring the UI remains consistent with the underlying attachment list.

## Notes
- When `_stagedAttachments` already holds the maximum number of attachments, `slotsLeft` is 0 and no new files are enqueued; if `files` contains any items, an error is shown.

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


Handles the user action to open an image attachment (`HandleImageOpenRequested`). It first requires authentication and exits early if not authenticated. It then branches on whether the current channel is an E2E-encrypted room by consulting `RoomKeys`: in non-encrypted rooms it opens the `attachmentUrl` in the system browser; in E2E rooms it either delegates to the save path for non-image extensions or downloads, decrypts, and opens the image from a temporary file.

## Remarks
Encapsulates the dual-viewing strategy for images: standard channels surface images through the host browser, while E2E rooms guard content by decrypting and opening locally. It ties into the channel state via `RoomKeys` and relies on the image-extension filter `ImageOpenExtensions` to decide between decryption-and-open vs. save-and-open, keeping the behavior centralized for a consistent UX and security model. This centralization reduces duplication and ensures consistent handling of image attachments across both encrypted and non-encrypted rooms.

## Notes
- Be aware that this method uses `System.Diagnostics.Process.Start` with `UseShellExecute = true`; on some platforms or restricted environments this can fail or behave differently, and such failures are surfaced as logs and user messages.

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


Handles an image pasted from the clipboard as raw PNG data. It only proceeds when the connection is authenticated and connected (`_conn.IsAuthenticated`, `_conn.IsConnected`), enforces the per-message attachment limit (`HubConstants.MaxAttachmentsPerMessage`), and, if allowed, writes the PNG into a unique temporary folder under the system temp path and saves it as `image.png` so multiple pasted images can coexist with a consistent display name, then tracks the path in `_tempPastedFiles` and `_stagedAttachments` and refreshes the staging UI. The temporary artifact is deleted after the message is sent.

## Remarks
Per-paste temporary folders prevent file-name collisions across multiple pasted images and preserve the browser-like display semantics by keeping the artifact named `image.png`. This approach integrates pasted content into the existing staging and encryption pipeline, leveraging the same attachment collections (`_stagedAttachments`, `_tempPastedFiles`) and UI feedback flow via `RefreshStagingTray`, so pasted images receive the same handling and cleanup guarantees as regular files.


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


It handles an image-save request by guarding with an authentication check on `_conn.IsAuthenticated` and, when authenticated, performing the download-and-persist sequence on a background thread via `RunAsync`. It informs the user of progress using `InvokeUI`, downloads the attachment to a temporary path with `DownloadAttachmentAsync`, and then moves it to a deduplicated destination determined by `GetDownloadDir` and `DedupPath`, using `File.Move`, before notifying the user of the final saved location.

## Remarks
This method encapsulates the end-to-end save flow behind a single, high-level action. It keeps the UI responsive by running the heavy IO inside `RunAsync` and uses `InvokeUI` to emit user-facing progress messages. The authentication guard and the deduplicated destination logic (via `DedupPath` and `GetDownloadDir`) centralize access control and collision handling, so callers needn't implement these concerns themselves.

## Notes
- Silent no-op when not authenticated: the method returns immediately with no UI feedback. Consider surfacing a login prompt or error to clarify why saving failed.

---

### HandleLoadMoreRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleLoadMoreRequested()
```

**Returns:** `void`


Loads additional chat history for the currently active channel when the user requests more messages, but only if `_conn.IsConnected` and `_mainWindow.CurrentChannel` are non-empty. It guards against concurrent loads per channel with `_channelsLoadingMore.Add(channel)`, computes `offset` from `_messageManager.GetMessages(channel)?.Count ?? 0`, then fetches history via `_conn.GetHistoryAsync(channel, HubConstants.DefaultHistoryCount, offset)` inside `RunAsync` and updates the UI with `_messageManager.PrependHistory` on the UI thread via `InvokeUI`, finally clearing the loading flag in the `finally` block; any failure surfaces as the RunAsync error message ``Failed to load more messages``.

---

### HandleLogout
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleLogout()
```

**Returns:** `void`


The `HandleLogout` method orchestrates the full sign-out flow: it logs the logout event via `Log.Information`, persists the last-read state via `PersistLastReads()`, and then delegates the asynchronous logout sequence to `RunAsync` with the error-contexts `Logout error` and `Logout`. Inside that asynchronous block it reads `_conn.Api?.BaseUrl` into `baseUrl`, awaits `_conn.LogoutAsync()`, conditionally clears the saved token by calling `ClearSavedToken(baseUrl)` when `baseUrl` is not null, awaits `_conn.CleanupAsync()`, and uses `InvokeUI` to clear the main window and set the status bar to `Disconnected` by calling `_mainWindow.ClearAll()` and `_mainWindow.UpdateStatusBar("Disconnected")`.

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


HandleMessageSubmitted is the central entry point for processing a user-submitted message in a channel. It first verifies the connection, aborting with a UI error if not connected, then branches based on the intent: commands are dispatched to the `_commandHandler`, staged attachments trigger a combined message with attachments via `SendStagedMessage`, and plain text messages may be posted with an optional `replyTo` target. The method orchestrates the async send and ensures UI updates occur on the appropriate thread, reporting failures through the UI and system messages when needed.

## Remarks
This method acts as the central coordinator between the network layer (`_conn`), the command processor (`_commandHandler`), and the UI/messaging state (`_mainWindow`, `_messageManager`, `_stagedAttachments`, `_pendingReply`). It hides the branching logic behind a single submission point, ensuring commands, attachments, and plain messages are handled correctly and that UI feedback happens on the UI thread. By centralizing these concerns, it reduces duplicated send logic in callers and keeps the user experience consistent when errors occur or when a pending reply is resolved after a send.

## Notes
- Staged attachments take precedence over plain text sending: when `_stagedAttachments.Count > 0`, `SendStagedMessage` is invoked and the rest of the method is skipped; the content is used as the caption for the attachments.
- Pending replies are channel-scoped; only a pending reply for the same `channelName` will be used as the `replyTo` target.

---

### HandleProfileRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleProfileRequested()
```

**Returns:** `void`


Handles the profile-request action by delegating to `HandleViewProfile` with a null argument, thereby reusing the existing profile-view flow to show the current or default profile. It serves as a small adapter between the external trigger and the central view logic, ensuring consistent behavior without duplicating code.

## Remarks
By routing through `HandleViewProfile(null)`, this method separates the surface action name from the actual view logic, acting as an integration point that keeps the path to display the profile consistent. It also makes it easy to add additional triggers in the future by mapping them to the same underlying view method, without duplicating the view call.

## Notes
- This wrapper relies on `HandleViewProfile` treating a `null` argument as the default or current profile. If that contract changes, `HandleProfileRequested` would need updating and any tests depending on the default behavior may fail.

---

### HandleReplyCancelRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleReplyCancelRequested() => ClearPendingReply()
```

**Returns:** `void`


Handles a cancellation request for an in-flight reply by delegating to `ClearPendingReply()` to reset the reply state. This private wrapper expresses the intent of the cancellation path and avoids duplicating the clearing logic at each cancellation site.

## Remarks
Because this is a private, forwarding helper, its value is expressive and structural: it names the cancellation path and centralizes the action of clearing the pending reply. It integrates with the orchestrator's event-driven flow by providing a dedicated handler for the `ReplyCancelRequested` event without exposing the clearing logic to external callers.

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


HandleReplyRequested is an internal UI helper that prepares the current channel context for replying to a specific message. It first reads the active channel from `_mainWindow.CurrentChannel` and returns early if no channel is selected, ensuring replies are not anchored to a non-existent context. When a channel exists, the method records the target as `_pendingReply = (channel, messageId)` so the rest of the UI knows what to reply to, truncates the provided `snippet` to a 40-character preview (appending a single '…' if needed), and instructs the UI to show the reply prompt via `_mainWindow.SetReplyingTo($"{sender}: {snippet}")`.

---

### HandleRollbackRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleRollbackRequested()
```

**Returns:** `void`


HandleRollbackRequested coordinates the rollback UX: it checks for an existing backup via `UpdateBackupService.BackupExists()` and, if none is found, reports an error through `MessageBox.ErrorQuery` and returns. If a backup exists, it fetches the backup info with `UpdateBackupService.GetBackupInfo()` and prompts the user to confirm restoration to version `info?.Version ?? "unknown"` using `MessageBox.Query`; on confirmation it invokes `UpdateBackupService.RestoreBackup()`, which will terminate the app via `Environment.Exit(0)` upon success, while any exception is logged with `Log.Error` and surfaced to the user via another error dialog.

## Remarks
This method serves as the UI orchestration layer for rollback, decoupling the user prompts and error handling from the underlying backup mechanics implemented in `UpdateBackupService`. It centralizes the flow: validate existence, confirm with the user, perform the restore, and surface failures to the user while logging for diagnostics. The explicit restart behavior via `Environment.Exit(0)` is a consequence of the restoration path invoked by `RestoreBackup`.

## Notes
- The app will terminate after a successful restore because `Environment.Exit(0)` is invoked by the restoration path.
- If the backup info lacks a version, the prompt gracefully shows `unknown` due to `info?.Version ?? "unknown"`.
- If no backup exists, the method ends early after showing an error, preventing a restore attempt.

---

### HandleSavedServersRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleSavedServersRequested()
```

**Returns:** `void`


The private helper `HandleSavedServersRequested` presents the user with the current list of saved servers by reading `_config.SavedServers` and displaying them in a dialog. If no servers are saved, it informs the user with a message prompting them to connect to a server so it can be saved automatically; otherwise it formats one line per server as `Name` (`Url`) - `Username` ?? `?` - `LastConnected`:yyyy-MM-dd and appends ` [session saved]` when a `RefreshToken` is present, then shows the compiled list via `MessageBox.Query`.

## Remarks
This method centralizes the UI presentation of saved servers, reducing duplication and ensuring consistent formatting (including the date format and the session indicator) across the app. It relies on the internal configuration store and the dialog API, acting as a thin presenter that translates the saved-server model into a user-friendly surface.

## Notes
- If the number of saved servers grows large, the resulting dialog could become unwieldy; consider paging or a dedicated view to enhance usability.

---

### HandleSearchRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleSearchRequested()
```

**Returns:** `void`


HandleSearchRequested centralizes routing of user-driven search results. It prompts with a search dialog listing channel names via `SearchDialog.Show(_app, _mainWindow.GetChannelNames())`; if the user cancels (result is `null`), it returns. If the user selects a channel (`SearchResultType.Channel`), it switches to that channel with `_mainWindow.SwitchToChannel(result.Key)` and then handles the channel selection via `HandleChannelSelected(result.Key)`. If an action is chosen (`SearchResultType.Action`), it dispatches to the appropriate handler by the `result.Key` string: `connect`, `disconnect`, `logout`, `profile`, `status`, `create-channel`, `delete-channel`, `servers`, `toggle-users`, `updates`, or `quit` (the last one triggers `_app.RequestStop()`).

---

### HandleStatusRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleStatusRequested()
```

**Returns:** `void`


HandleStatusRequested prompts the user for a new status by calling `StatusDialog.Show(_app, _session.Status, _session.StatusMessage)` and, if the user provides input, updates the local session (`_session.Status`, `_session.StatusMessage`) and, when connected, propagates the change to the server by issuing `_conn.UpdateStatusAsync(result.Status, result.StatusMessage)` inside a `RunAsync` call (with the failure hint `"Status update failed"`). If the dialog result is null, the method returns immediately without side effects.

## Remarks
This method serves as the orchestration point between the UI layer and the networking layer for a user-initiated status change. It encapsulates the flow by first updating local session state and then attempting remote synchronization only when `_conn.IsConnected` is true, keeping behavior predictable in online/offline scenarios. By dispatching the remote update via `RunAsync`, the UI remains responsive and avoids blocking while the potentially long-running network operation completes.

## Notes
- If the application is offline (i.e., `_conn.IsConnected` is false), the status update is applied locally but not persisted remotely until a connection is re-established.
- The remote update is kicked off without awaiting its completion, so errors are handled by the underlying `UpdateStatusAsync` path and surfaced via the provided failure message in the asynchronous workflow.

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


Handles the user’s theme selection by logging the choice, resolving the corresponding theme via `ThemeManager.GetTheme`, applying it with `ThemeManager.ApplyTheme`, persisting the selection to the configuration via `_config.ActiveTheme` and `ConfigManager.Save`, and finally refreshing the UI through `InvokeUI` to re-apply color schemes and redraw the main window.

## Remarks
This method acts as the central coordinator for applying a new UI theme. It coordinates `ThemeManager` to resolve and apply the theme, `ConfigManager` to persist the selection, and the main window to refresh visuals via `ApplyColorSchemes` and `SetNeedsDraw`. This separation keeps theme changes atomic and ensures the UI is updated promptly after a change and that the choice persists across restarts.

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


HandleViewProfile coordinates loading and displaying a user profile for either the current user or a specified user. It executes the retrieval on a background task, uses the authenticated API to obtain the profile, handles failures by reporting to the UI, and finally presents an appropriate dialog: an editable own-profile view or a read-only profile view for others; actions from the own-profile dialog (edit or set status) route back to dedicated handlers.

## Remarks
This method centralizes the profile-view flow, decoupling UI presentation from data-fetching and error handling. It distinguishes between viewing the own profile and another user by comparing the provided `username` against `_session.Username` with `StringComparison.OrdinalIgnoreCase`, then marshals UI updates back to the main thread via `InvokeUI` to avoid blocking. By delegating edit and status actions to `HandleEditProfile` and `HandleStatusRequested`, it keeps concerns separated and makes the editing workflow explicit only when the user is viewing their own profile.

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


InvokeUI is a private helper that forwards a given `Action` to the underlying application's `Invoke` method by calling `_app.Invoke(action)`. This centralizes the UI invocation path, so callers marshal UI work through a single, stable hook rather than interacting with `_app` directly.

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


Joins a channel, prompting for a password when the server requires one and re-prompting on a wrong password. For end-to-end encrypted channels the typed passphrase never leaves the client; instead a PBKDF2-derived auth key is sent and the room content key is unwrapped locally, with the method returning the channel history or null if the prompt is cancelled.

## Remarks

Conceptually, this method centralizes the user-interaction and key-management flow required to join password-protected or encrypted channels. It begins by optionally querying crypto metadata to determine whether a channel is encrypted, then derives local keys from the provided password when a salt is available, and uses the derived auth key to perform the join without exposing the passphrase. If a server-supplied `WrappedRoomKey` is received, the envelope can be stored and the content key unwrapped locally; on success the history is retrieved so the caller can decrypt messages with the new key. When the server requires a password, the UI is invoked to show a password dialog and the loop continues until a non-null password yields a result or the user cancels.

## Notes

- The re-prompt loop continues until a non-null password is supplied and the join succeeds, or the user cancels (resulting in null).
- In end-to-end encrypted channels, the actual passphrase never leaves the client; only the derived auth key is transmitted to the server, and the room content key is unwrapped locally.
- Crypto metadata retrieval is best-effort: if it fails, the method logs and proceeds, potentially affecting how encryption state is reflected.

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


Determines whether the UI should present an unlock prompt for an end-to-end encrypted channel. It returns true when the channel is encrypted, there is no cached room key for that channel, and the user has not declined the unlock prompt during the current session. Use this check to gate the unlock UX when opening an encrypted channel.

## Remarks
Centralizes the decision to prompt for a channel unlock, tying together the channel state accessed via `_conn.RoomKeys` and the per-session declines tracked in `_declinedUnlocks` under a lock. This separation avoids scattering the same conditional logic across call sites, ensuring a consistent user experience for encrypted channels while supporting per-session opt-outs.

## Notes
- Access to `_declinedUnlocks` is guarded by a lock, but make sure any updates to that collection are performed under the same lock to keep the internal state consistent.
- The method is private and intended for internal orchestration; external callers should not rely on it directly.

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


Normalizes an optional input size string to a canonical single-letter code by trimming whitespace, converting to lowercase invariant with `ToLowerInvariant`, and mapping common words to `s`, `m`, or `l`. Use this private helper when you need a consistent size token for downstream logic and you want to accept both short forms (`s`, `m`, `l`) and full words (`small`, `medium`, `large`) without duplicating normalization logic at call sites. If the input is unrecognized, it returns `null` to signal that no valid size was provided.

## Remarks
Centralizes size normalization within `AppOrchestrator`, preventing divergent handling of size inputs across call sites and making it easier to adjust supported synonyms in one place. The `null` return signals an absent or unsupported value, allowing callers to apply defaults or validation as appropriate.

---

### PersistLastReads
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void PersistLastReads()
```

**Returns:** `void`


Persists the in-memory map of last-read message IDs to the current server's configuration, so unread/mention state can be reconstructed on the next connect. It reads the mapping from `_messageManager.LastReadIds` and, if non-empty, updates the server configuration via `UpdateServerConfig`, persisting each `(channel, id)` pair by assigning `server.LastReadMessages[channel] = id.ToString()`.

## Remarks
By centralizing this durability logic, `PersistLastReads` isolates read-state persistence from other server operations and ensures the state survives reconnects. It relies on the `server.LastReadMessages` dictionary as the durable store, with the in-memory map driving its updates through `UpdateServerConfig`. Since it early-returns when there are no entries in `_messageManager.LastReadIds`, callers should be aware that no persistence occurs in that case.

## Notes
- The IDs are stored as strings using `ToString()`; ensure downstream restoration uses that representation.
- The method short-circuits on an empty map, so no writes occur unless there is data.

---

### PromptPassword
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private string? PromptPassword(string prompt)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `prompt` | `string` | — |

**Returns:** `string?`


This method displays a modal password prompt using the `terminal GUI` and returns the entered password or null if the user cancels or leaves it empty. It constructs a `Dialog` titled 'Confirm Password' containing a masked `TextField` (`Secret = true`) and two buttons (`Confirm` and `Cancel`), runs the dialog via `_app.Run`, and finally returns the input, or null if there is no input.

## Remarks
This helper centralizes the password-prompt UX, keeping UI concerns isolated from authentication logic and enabling reuse wherever a password is needed. It relies on a modal dialog pattern (using `Dialog`, `TextField`, and `Button` instances) and on `_app.Run`/`_app.RequestStop()` to suspend the rest of the application until the user responds, returning null for cancellation or empty input.

## Notes
- Empty input is treated as null due to `string.IsNullOrEmpty(result)`; if you need to distinguish between cancel and empty, adjust the logic.

---

### RefreshStagingTray
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void RefreshStagingTray()
```

**Returns:** `void`


`RefreshStagingTray` refreshes the staging tray to reflect the current set of staged attachments by collecting file names from `_stagedAttachments` via `Path.GetFileName` and updating the UI through `_mainWindow.SetStagedAttachments` with the resulting `names` (a `List<string>`) and the size label produced by `AsciiSizeLabel(_config.DefaultAsciiSize)`.

Being private, it is intended for internal reuse to keep the UI in sync when the staged attachments or the configured ASCII size changes.

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


RunAsync is a convenience wrapper that executes an asynchronous unit of work, passed as a `Func<Task>`, by delegating to `AsyncRunner.Run` within the application's context. It wires the error reporting to `_mainWindow.ShowError` and forwards the `errorPrefix` and optional `logContext` so all async operations launched from the orchestrator share a consistent error-handling and logging path.

## Remarks
By centralizing the call to `AsyncRunner.Run` and the UI error handler, this method reduces boilerplate and ensures consistent behavior for asynchronous operations across the component. It lets code that needs to run background work opt into standardized error display without duplicating wiring. The design ties the execution context to `_app` and `_mainWindow`, preserving a single place where errors are surfaced to the user.

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
| `result` | `ConnectDialogResult` | — |

**Returns:** `void`


SaveServerToConfig updates the per-server entry in the configuration after a successful connection, preserving existing per-server state (cached room keys, left channels, last-read markers) by updating the entry in place and only creating a new one if none matches `result.ServerUrl`.

It loads the config with `ConfigManager.Load()`, locates the server using a case-insensitive comparison of `s.Url` to `result.ServerUrl` (via `FirstOrDefault`), creates a new `SavedServer` with `Name` from `new Uri(result.ServerUrl).Host` and `Url` set to `result.ServerUrl` when needed, updates `Username`, `RefreshToken` (via `_conn.Api!.RefreshToken` if `result.RememberMe` is true), `RememberMe`, and `LastConnected` (`DateTimeOffset.Now`), then saves with `ConfigManager.Save(config)`, updates `_config`, and logs a success message with `Log.Information`.

## Remarks
This method centralizes the post-connection persistence of per-server data, ensuring existing per-server state survives across connections while recording credentials and last connection time. By updating the entry in place, it avoids discarding associated per-server data that would be lost if the entry were replaced.

## Notes
- The line `server.RefreshToken = result.RememberMe ? _conn.Api!.RefreshToken : null` relies on `_conn.Api` being non-null when `RememberMe` is true; if `_conn.Api` is null at this point, a `NullReferenceException` may occur.

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


Sends a single message to the specified `channel` containing the given `content` as the caption and all currently staged files as attachments, then clears the staging tray. In encrypted channels each file is room-encrypted client-side (blob + ASCII preview) before upload and the caption is encrypted as well; the method first ensures the room is unlocked for sending via `EnsureRoomUnlockedForSendAsync`, builds `OutgoingAttachment`s for every staged path (using the room key when available), and uploads with `SendMessageWithAttachmentsAsync` while cleaning up temporary pasted files in a `finally` block.

## Remarks
This symbol acts as a transactional coordinator for staged content: it coordinates end-to-end encryption, attachment preparation, and dispatch in a single user action. By taking a snapshot of `_stagedAttachments`, clearing the tray, and refreshing the UI, it ensures the user sees an immediate staging state change while the network operation proceeds. It centralizes lifecycle management for clipboard-pasted files via `_tempPastedFiles`, reducing the risk of orphaned temp files.

## Notes
- It clears `_stagedAttachments` before the upload completes; if the upload fails, the attachments are not recoverable from the staging area.
- Encryption is conditional: attachments are encrypted when a room key exists and the caption content is non-empty; otherwise, the payload is sent unencrypted.

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


SetDownloadPath is a helper invoked when a user selects or changes the download folder; it first ensures the target directory exists by calling `Directory.CreateDirectory(path)`, and on failure uses `InvokeUI` to show an error via `_mainWindow.ShowError` with a message like `Can't use that folder: {ex.Message}`. On success, it updates `_config.DownloadPath`, persists the change with `ConfigManager.Save(_config)`, and uses `InvokeUI` to post a system message via `_messageManager.AddSystemMessage(_mainWindow.CurrentChannel, `$"Download folder set to: {path}")`.

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
| `outcome` | `JoinOutcome` | — |

**Returns:** `Task<List<MessageDto>?>`


Submitted narrative documentation for `UnlockRoomKeyAsync`. The description explains that it orchestrates a user-driven unlock of an encrypted channel's room key when there is no cached key, by prompting for a passphrase, deriving keys via `RoomCrypto.DeriveKeys`, and attempting to unwrap with `_conn.RoomKeys.TryStoreFromEnvelope`, returning channel history via `_conn.GetHistoryAsync(channelName)` on success, or the existing history if the user cancels or the unlock is abandoned. The Remarks section notes its role in centralizing the unlock UX and handling declined-unlock state to avoid nagging on reselection.

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


UnlockTrackedChannelAsync is a private asynchronous helper that unlocks the end-to-end encrypted history for a channel that is already hub-joined. It follows the auto-join/reconnect flow to obtain the key envelope by calling `_conn.JoinChannelAsync(channelName, null)`; if the resulting `WrappedRoomKey` is null, the channel is not an E2E channel and the method returns false. It then calls `UnlockRoomKeyAsync(channelName, outcome)` to unwrap the envelope and retrieve the history. If `_conn.RoomKeys.HasKey(channelName)` is false, the unlock did not complete (cancellation or unwrap failure), and the method returns false. If a non-null `history` is produced, it schedules a UI update via `InvokeUI(() => _messageManager.LoadHistory(channelName, history))`. The method returns true when unlocking succeeds. Any exception is caught, logged with `Log.Warning`, and results in a false return.

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


Mutates the current server's entry in the saved configuration and persists the change. It is a no-op if there is no authenticated context (the current connection lacks a base URL) or if the server isn't present in the saved list. The method locates the target `SavedServer` by matching `_conn.Api?.BaseUrl` against each `SavedServer.Url` using a case-insensitive comparison, applies the mutation via the supplied `Action<SavedServer>`, saves the updated `config` with `ConfigManager.Save`, and updates the in-memory `_config` reference.

## Remarks
By centralizing updates to the active server config, this helper ensures consistency between the runtime connection URL and the persisted server state. It encapsulates the mutation-and-persist pattern so callers don't need to repeat load/save logic and can rely on `_config` reflecting the latest changes after the call.

## Notes
- URL matching uses `StringComparison.OrdinalIgnoreCase`; ensure both the connection base URL and the saved server URLs are normalized to avoid a missed match.
- This method is private; usage is confined to the containing class, so callers must provide mutation logic that remains within the class's invariants.

---

### WireCommandHandlerEvents
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void WireCommandHandlerEvents()
```

**Returns:** `void`


WireCommandHandlerEvents subscribes the command handler's event stream to the orchestrator's command handlers by attaching a suite of `HandleCmd...` methods to the corresponding `On...` events on `_commandHandler`. This ensures that when the command surface raises events such as `OnSetStatus`, `OnJoinChannel`, or `OnKickUser`, the matching handler is invoked to apply UI and state updates.

## Remarks
This method centralizes the wiring of all command events into their handlers, making initialization explicit and easier to audit. It acts as the bridge between the `_commandHandler` event emissions and the orchestrator's response logic. If a new event is added, extending here keeps the lifecycle and subscriptions consistent.

## Notes
- Ensure `_commandHandler` is non-null before calling this method; a null reference would throw during event subscription.

---

### WireConnectionManagerEvents
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void WireConnectionManagerEvents()
```

**Returns:** `void`


Wires up the `WireConnectionManagerEvents` method to connect the ConnectionManager’s real-time events to the UI and in-memory presence state. When a message arrives, it marshals the update to the UI via `InvokeUI`, and triggers a notification by calling `PlayAsync` when the current user is mentioned (via `message.Content.Contains(..., StringComparison.OrdinalIgnoreCase)`). It also updates the per-channel presence cache under `_channelUsersLock` for join/leave events and refreshes the online user list for the current channel, or fetches updates when needed, while keeping UI updates synchronized with the main window.

## Remarks
Centralizes the event-handling choreography between the connection layer and the UI/presence model, so all real-time reactions are maintained in one place. It coordinates thread-safety around the shared channel-user state and ensures the main window’s online user display remains consistent with the cached state and current channel context. This wiring enables responsive feedback for users entering, leaving, or changing presence across channels without requiring callers to manage cross-component updates.

## Dependencies
- `UserPresenceDto`
- `Content`
- `StringComparison`
- `Username`
- `Status`
- `UserStatus`


---

### WireMainWindowEvents
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void WireMainWindowEvents()
```

**Returns:** `void`


WireMainWindowEvents subscribes the main window's events to the orchestrator's handlers by wiring each `OnXRequested` event to its corresponding `HandleXRequested` method. This centralizes the UI-to-logic wiring so user actions such as `OnConnectRequested`, `OnDisconnectRequested`, `OnLogoutRequested`, `OnMessageSubmitted`, `OnFilesStaged`, `OnImagePasted`, `OnChannelSelected`, `OnProfileRequested`, `OnStatusRequested`, `OnThemeSelected`, `OnSavedServersRequested`, `OnCreateChannelRequested`, `OnDeleteChannelRequested`, `OnAudioPlayRequested`, `OnFileDownloadRequested`, `OnImageSaveRequested`, `OnImageOpenRequested`, `OnDeleteMessageRequested`, `OnCheckForUpdatesRequested`, `OnRollbackRequested`, `OnUserProfileRequested`, `OnChannelJoinRequested`, `OnSearchRequested`, `OnLoadMoreRequested`, `OnReplyRequested`, and `OnReplyCancelRequested` are routed to their respective `Handle*` methods. This method is typically invoked during startup to establish the runtime event routing between the UI and the application.

---

### ImageOpenExtensions
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private static readonly HashSet<string> ImageOpenExtensions = new(StringComparer.OrdinalIgnoreCase)
```


ImageOpenExtensions defines the set of file extensions that the `[`open`]` action will hand to the OS image viewer for E2E rooms. It is a private static readonly `HashSet<string>` initialized with `.png`, `.jpg`, `.jpeg`, `.gif`, `.webp`, and `.bmp` using `StringComparer.OrdinalIgnoreCase` to ensure case-insensitive matching.

## Remarks

This centralized set ensures consistent behavior for which files are opened via the OS image viewer, isolated from the actual open logic. Being private, only this class uses it, so updates are self-contained. The `HashSet<string>` data structure provides fast lookups, and the `StringComparer.OrdinalIgnoreCase` comparer guarantees extension checks ignore case across file names.

## Notes

- Ensure the leading dot is present in each extension; omitting the dot will break matching.
- External components should not rely on this private member directly; use the public open action APIs instead.

---

### SafeOpenExtensions
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private static readonly HashSet<string> SafeOpenExtensions = new(StringComparer.OrdinalIgnoreCase)
```


Defines the set of file extensions safe to open with the system default application. This static readonly `HashSet<string>` named `SafeOpenExtensions` is constructed with `StringComparer.OrdinalIgnoreCase`, ensuring case-insensitive matching, and is consulted to decide whether to launch a file via `UseShellExecute` or to download it instead.

## Remarks

Using a dedicated `HashSet` provides fast, constant-time membership checks and centralizes the open-policy decision in one place. The `OrdinalIgnoreCase` comparer guarantees consistent behavior for extensions regardless of case, helping avoid subtle bugs when a user or API supplies `.MP4` vs `.mp4`. Because the field is private and readonly, it serves as a single source of truth for the allowed-to-open policy within this class.

## Notes

- The `HashSet` instance is mutable; although the field is `readonly`, the collection can be modified internally. Avoid mutating it unless there is a deliberate reason to extend the safe-open list.

---

### _app
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private readonly IApplication _app
```


Stores a private, readonly reference to an `IApplication` implementation that the `AppOrchestrator` uses to access application-wide services. It is assigned during construction and never reassigned, so consumers should rely on this field for injected access to the broader application layer rather than creating or swapping the dependency.

---

### _audioPlayback
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private readonly AudioPlaybackService _audioPlayback = new()
```


This private readonly field `_audioPlayback` holds the dedicated `AudioPlaybackService` instance used by the `AppOrchestrator` to perform audio playback tasks. It is initialized inline with `new()` and kept private to ensure a single, immutable playback handler per `AppOrchestrator` instance.

## Remarks
This field centralizes audio playback responsibilities within the `AppOrchestrator`, ensuring a single, consistent playback service is used per instance. By owning a private, readonly `AudioPlaybackService` instance, it prevents multiple playback engines from existing per `AppOrchestrator` instance and keeps playback coordination contained within the orchestrator.

---

### _channelUsers
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private readonly Dictionary<string, List<UserPresenceDto>> _channelUsers = new(StringComparer.OrdinalIgnoreCase)
```


Represents an in-memory registry of user presence per channel. It maps channel names to the list of `UserPresenceDto` instances for users connected to that channel, using `StringComparer.OrdinalIgnoreCase` so channel keys are treated case-insensitively. The field is `private readonly`, ensuring the dictionary instance remains the same after construction while allowing its contents to evolve as users join or leave channels.

## Remarks
Keyed by channel name, this single dictionary centralizes presence state for the orchestrator and avoids scattering per-channel state across multiple fields. The case-insensitive key handling prevents duplicates caused by capitalization differences, while the internal lists enable efficient per-channel presence updates and enumerations.

## Notes
- The `readonly` modifier applies to the dictionary reference, but its contents are mutable; external code may mutate the lists and the dictionary, so caller code should coordinate mutations when accessed from multiple threads.
- Mutations to `_channelUsers` and its inner `List<UserPresenceDto>` are not inherently thread-safe; consider external synchronization if updates occur concurrently from multiple threads.

---

### _channelUsersLock
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private readonly Lock _channelUsersLock = new()
```


This private, readonly field `_channelUsersLock` is the synchronization primitive used to guard access to the channel users collection within `EchoHub.Client.AppOrchestrator`. When concurrently accessed by multiple threads, code should synchronize on this object (for example, with `lock(_channelUsersLock) { ... }`) to ensure consistent, atomic updates and reads of the channel user state.

## Remarks

By centralizing locking on a dedicated `_channelUsersLock`, the class avoids exposing its synchronization mechanism and reduces the risk of deadlocks caused by external code locking on shared data. It represents the canonical ownership boundary for the channel users' in-memory state, coordinating access among all operations that query or mutate that state.

## Notes

- Do not lock on external objects or on the channel users collection itself when those are publicly accessible; always use `_channelUsersLock` as the guard to maintain a consistent locking discipline.

---

### _channelsLoadingMore
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private readonly HashSet<string> _channelsLoadingMore = new(StringComparer.OrdinalIgnoreCase)
```


The `_channelsLoadingMore` field is a private, readonly `HashSet<string>` used to track channel identifiers for which a 'load more' operation is currently underway. It is constructed with a `StringComparer.OrdinalIgnoreCase` so channel IDs are treated case-insensitively, ensuring a single entry per channel regardless of casing and enabling fast checks to avoid initiating duplicate loads.

## Remarks
Conceptually, this set serves as a lightweight in-memory coordination mechanism within `AppOrchestrator` to prevent overlapping load operations per channel. The use of `StringComparer.OrdinalIgnoreCase` ensures consistent identity for channel identifiers, avoiding duplicates caused by casing variations. Because the field is private and readonly, the class retains full ownership of its lifecycle and mutation is controlled through its own methods rather than external code.

## Notes
- Access to the collection is not inherently thread-safe; if mutations may occur from multiple threads, synchronize access or use a concurrent approach.
- Ensure that each added channel is removed from the set when its load operation completes to avoid stale state or leaking tracking data.
- Do not expose `_channelsLoadingMore` publicly; it is an internal implementation detail of the class.

---

### _commandHandler
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private readonly CommandHandler _commandHandler
```


The `_commandHandler` field holds a private, readonly reference to a `CommandHandler` used by the `AppOrchestrator` to process and route command-related work. It centralizes command-processing behavior away from the orchestrator's coordination logic, ensuring a single, consistent pathway for handling commands within the client. Because the field is readonly, the chosen command-handling implementation remains fixed after construction, promoting predictable behavior and easier testing.

## Remarks
By delegating to a `CommandHandler`, the `AppOrchestrator` remains focused on coordinating higher-level flows rather than implementing command logic. The private, readonly field ensures the collaboration is established once and remains stable, improving testability by allowing the handler to be mocked or substituted at construction time without affecting orchestration behavior.

## Notes
- The field is private and readonly; it cannot be reassigned after construction. If runtime swapping of the command handler is required, consider exposing it via a constructor parameter or a controlled accessor.

---

### _config
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private ClientConfig _config
```


It stores the runtime configuration for the EchoHub client orchestration. As a private field of `AppOrchestrator`, it centralizes the `ClientConfig` used to configure and create client components, ensuring consistent behavior across the orchestrator without exposing config externally. A developer would rely on it indirectly when the class initializes or updates its clients, rather than constructing or passing a `ClientConfig` at every call site.

## Remarks
By holding `ClientConfig` privately, the class retains control over when and how configuration changes are applied. It serves as a compact internal dependency that initialization logic can reference to align client creation with current settings, without leaking configuration details to consumers.

## Notes
- Access to `_config` can lead to a `NullReferenceException` if it is not initialized before use; ensure it is assigned in the constructor or prior to any access.
- If the field is updated at runtime, consider thread-safety implications and coordinate updates with any in-flight operations that read from the field.

---

### _conn
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private readonly ConnectionManager _conn = new()
```


It stores the orchestrator's private `ConnectionManager` instance named `_conn`, serving as the internal gateway for all connection-related operations within `AppOrchestrator`. The field is `readonly` and eagerly initialized with `new()`, ensuring a single, immutable reference to the `ConnectionManager` that isn't exposed publicly.

## Remarks
By encapsulating the connection logic behind a private, `readonly` field `_conn`, this symbol prevents accidental reconfiguration or duplication of the connection path in `AppOrchestrator`. It acts as the canonical reference that other members within the class rely on to initiate and route communication against the underlying channel, lending cohesion to the orchestration workflow.

## Notes
- If `ConnectionManager` implements `IDisposable`, ensure disposing it as part of the owning class lifecycle to avoid resource leaks.
- The `readonly` field guarantees the reference won't be reassigned, but internal state must still be thread-safe if accessed from multiple threads.

---

### _declinedUnlocks
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private readonly HashSet<string> _declinedUnlocks = new(StringComparer.OrdinalIgnoreCase)
```


This private, readonly `HashSet<string>` `_declinedUnlocks` records E2E channel identifiers for which the user declined the unlock prompt, preventing repeated nagging when a channel is reselected. It uses `StringComparer.OrdinalIgnoreCase` so prompts are matched without regard to case. The set is cleared on connect/reconnect; an explicit `/join` or a send attempt re-offers the prompt.

## Remarks
This field serves as a lightweight, in-memory cache of declined prompts that separates user-facing prompt logic from channel-selection flow. By using a `HashSet<string>` with ordinal ignore-case comparison, lookups are fast and robust to case variation, while the explicit clear-on-reconnect lifecycle ensures prompts can be re-offered when the context changes.

## Notes
- `HashSet<string>` is not inherently thread-safe; guard accesses if this field is mutated from multiple threads.

---

### _mainWindow
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private readonly MainWindow _mainWindow
```


This private readonly field, `_mainWindow`, holds a reference to the application's main window, enabling the `AppOrchestrator` to coordinate UI interactions with the `MainWindow`. Being `readonly`, the reference is established during construction and remains constant for the lifetime of the orchestrator. Since the field is private, only the `AppOrchestrator` can access it, keeping the UI coupling encapsulated.

## Remarks
Having a private, readonly link to the `MainWindow` keeps UI coordination centralized within the `AppOrchestrator`, reducing indirection and ensuring a stable window handle is available for UI-bound operations. The `readonly` modifier communicates intent: once created, the reference should not be reassigned, clarifying ownership of the UI coupling.

## Notes
- Initialization: the field must be assigned in the constructor; after that, it cannot be reassigned due to `readonly`.

---

### _messageManager
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private readonly ChatMessageManager _messageManager
```


The field `_messageManager` stores the instance of `ChatMessageManager` that `AppOrchestrator` uses to coordinate chat message processing. It is `private` and `readonly`, meaning it is assigned during construction and not reassigned thereafter, ensuring all message-related operations within the orchestrator consistently delegate to the same manager instance.

## Remarks
Design-wise, this private readonly field centralizes messaging logic inside the `AppOrchestrator`, reducing duplication by funneling all message handling through a single `ChatMessageManager` instance. Making the reference immutable signals a stable dependency lifecycle: the orchestrator is bound to that particular `ChatMessageManager` for its entire lifetime, aiding reasoning about state and side effects in the chat flow.

---

### _notificationSound
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private readonly NotificationSoundService _notificationSound
```


It stores a reference to the `NotificationSoundService` used by the `AppOrchestrator` to trigger notification sounds in response to events. Being `private readonly`, its value is assigned once during construction and cannot be reassigned, which enforces a stable dependency for the orchestration logic.

## Remarks
This field encapsulates the sound-playing concern, decoupling the orchestration logic from the concrete sound behavior and enabling easier testing or swapping of the sound implementation via dependency injection. Keeping it private ensures that only the orchestrator's behavior directly controls when sounds are played, avoiding leaks of implementation detail.

---

### _pendingReply
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private (string Channel, Guid MessageId)? _pendingReply
```


This private field stores the target for the next reply in a channel conversation, indicating that the upcoming message should be posted as a reply to a specific message. It is a nullable value tuple named `_pendingReply` with two named elements: `(string Channel, Guid MessageId)?`, where `Channel` identifies the destination channel and `MessageId` is the ID of the message to which the reply should attach. When no reply is pending, the field is `null`.

## Remarks
This field centralizes reply-threading state within the orchestrator, avoiding scattered state and clarifying that the next outbound message should attach to a specific message in a channel (via the `Channel` and `MessageId` pair). Because the field is nullable, code can check for a pending state and clear it after the reply is dispatched.

---

### _session
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private readonly UserSession _session = new()
```


private readonly field `_session` stores the per-instance `UserSession` used by the orchestration logic to track the current user's session data. It is initialized with a new `UserSession` at construction and is reused for all session-related operations within the instance, preventing repeated allocations and preserving a single session state for the lifetime of the object.

## Remarks
Because `_session` is private and readonly, the instance maintains a single, non-reassignable reference to the `UserSession` for its lifetime. The contained `UserSession` can still mutate its internal state; this field simply guarantees that the same session object is used across all methods of the class.

## Notes
- The `UserSession` instance referenced by `_session` is mutable; changes made through `_session` persist across method calls. If a fresh session is needed for a particular operation, reset its state explicitly rather than attempting to reassign the field (which is impossible due to `readonly`).

---

### _stagedAttachments
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private readonly List<string> _stagedAttachments = []
```


This private field stores the in-memory list of attachment identifiers that have been staged for processing by the `AppOrchestrator`. It acts as the staging area used to accumulate the attachment identifiers stored in `_stagedAttachments` for inclusion in a batch or payload, with updates performed against the `List<string>` rather than replacing the collection. The `readonly` modifier ensures the underlying `List<string>` reference cannot be reassigned after construction, providing a stable staging container for the object's lifetime.

## Remarks
By keeping the field private and `readonly`, the class enforces encapsulation of its attachment staging state and minimizes the risk of external or accidental replacement detaching attachments from the workflow. The single shared list also simplifies downstream processing by giving other methods within the class a single source of truth for what attachments are pending.

## Notes
- If this field is mutated from multiple threads, `List<string>` is not thread-safe; synchronize access or use a thread-safe collection to avoid data races.

---

### _tempPastedFiles
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private readonly HashSet<string> _tempPastedFiles = []
```


This private field `HashSet<string>` named `_tempPastedFiles` tracks the file paths of temporary PNGs created during clipboard paste operations. The files are deleted once their message is sent or the staging tray is cleared, preventing buildup in the temp directory. The field is marked `readonly`, so while the reference to the `HashSet<string>` cannot be reassigned, the contents of the set can be updated as images are pasted and later removed.

## Remarks
This field acts as a small lifecycle registry for ephemeral clipboard-derived images. By centralizing the paths, the orchestrator can coordinate timely deletion when messages are dispatched or the staging area resets, helping maintain a clean temp surface and predictable resource usage.

---

### _updateService
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private readonly UpdateChecker _updateService
```


The `_updateService` field is a private readonly reference to the `UpdateChecker` used by the `AppOrchestrator` to perform update checks for the EchoHub client. It is assigned during construction and never reassigned, guaranteeing a stable and consistent update-checking pathway for the orchestrator's lifecycle.

## Remarks
Restraining the field to private and readonly ensures a single `UpdateChecker` instance is used by the `AppOrchestrator` for the duration of its life, avoiding mid-flight swaps that could complicate update coordination. It positions `UpdateChecker` as a dedicated collaborator responsible for update-awareness, centralizing network calls and state related to application updates.

---