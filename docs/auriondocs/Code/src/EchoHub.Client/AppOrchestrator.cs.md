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
  - [Dispose](#dispose)
  - [EnsureRoomUnlockedForSendAsync](#ensureroomunlockedforsendasync)
  - [HandleChannelJoinFromMessage](#handlechanneljoinfrommessage)
  - [HandleChannelSelected](#handlechannelselected)
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
  - [HandleCmdOpenServers](#handlecmdopenservers)
  - [HandleCmdQuit](#handlecmdquit)
  - [HandleCmdRevokeInvite](#handlecmdrevokeinvite)
  - [HandleCmdSendAction](#handlecmdsendaction)
  - [HandleCmdSendBanner](#handlecmdsendbanner)
  - [HandleCmdSendFile](#handlecmdsendfile)
  - [HandleCmdSetAsciiSize](#handlecmdsetasciisize)
  - [HandleCmdSetAvatar](#handlecmdsetavatar)
  - [HandleCmdSetColor](#handlecmdsetcolor)
  - [HandleCmdSetNick](#handlecmdsetnick)
  - [HandleCmdSetStatus](#handlecmdsetstatus)
  - [HandleCmdSetTheme](#handlecmdsettheme)
  - [HandleCmdSetTopic](#handlecmdsettopic)
  - [HandleCmdTestSound](#handlecmdtestsound)
  - [HandleCmdUnbanUser](#handlecmdunbanuser)
  - [HandleCmdUnmuteUser](#handlecmdunmuteuser)
  - [HandleConnect](#handleconnect)
  - [HandleCreateChannelRequested](#handlecreatechannelrequested)
  - [HandleDeleteMessageRequested](#handledeletemessagerequested)
  - [HandleDisconnect](#handledisconnect)
  - [HandleEditProfile](#handleeditprofile)
  - [HandleFilesStaged](#handlefilesstaged)
  - [HandleImagePasted](#handleimagepasted)
  - [HandleLoadMoreRequested](#handleloadmorerequested)
  - [HandleLogout](#handlelogout)
  - [HandleMessageSubmitted](#handlemessagesubmitted)
  - [HandleProfileRequested](#handleprofilerequested)
  - [HandleReplyCancelRequested](#handlereplycancelrequested)
  - [HandleReplyRequested](#handlereplyrequested)
  - [HandleSavedServersRequested](#handlesavedserversrequested)
  - [HandleStatusRequested](#handlestatusrequested)
  - [HandleThemeSelected](#handlethemeselected)
  - [HandleViewProfile](#handleviewprofile)
  - [InvokeUI](#invokeui)
  - [JoinChannelWithPasswordPromptAsync](#joinchannelwithpasswordpromptasync)
  - [NeedsUnlockPrompt](#needsunlockprompt)
  - [NormalizeAsciiSize](#normalizeasciisize)
  - [RunAsync](#runasync)
  - [SendStagedMessage](#sendstagedmessage)
  - [UnlockTrackedChannelAsync](#unlocktrackedchannelasync)
  - [WireCommandHandlerEvents](#wirecommandhandlerevents)
  - [WireConnectionManagerEvents](#wireconnectionmanagerevents)
  - [WireMainWindowEvents](#wiremainwindowevents)
- [HandleCmdOpenProfile](#handlecmdopenprofile)
- [HandleSearchRequested](#handlesearchrequested)
- [PromptPassword](#promptpassword)
- [RefreshStagingTray](#refreshstagingtray)
- [UnlockRoomKeyAsync](#unlockroomkeyasync)

---

## AppOrchestrator
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** class

```csharp
public sealed class AppOrchestrator : IDisposable
```


Central coordinator for the EchoHub terminal UI client: it wires MainWindow UI events and command input to the underlying services (connection manager, message manager, audio/update services, etc.), maintains ephemeral UI-facing state (staged attachments, per-channel user lists, temporary pasted files, declined E2E unlocks, pending reply targets), and exposes a small host-facing surface (MainWindow and PendingUpdate). Reach for AppOrchestrator when you need a single place to orchestrate cross-cutting app behavior rather than wiring UI components and services together manually.

## Remarks
AppOrchestrator exists to centralize responsibilities that span UI, networking and local client state so the rest of the codebase can remain focused: UI widgets raise events and the orchestrator translates those into service calls, and connection/service events are translated back into UI updates. It owns short-lived caches (case-insensitive channel user lists), staging buffers (attachments and temporary pasted files), and user-interaction policies (for example, remembering which E2E unlock prompts were declined so the user isn't repeatedly nagged). It also exposes PendingUpdate so the host can perform an in-place restart safely after the TUI's main loop exits.

## Notes
- PendingUpdate must be executed by the host only after the Terminal.Gui main loop and TUI have exited (console restored); running the updater while the TUI is still active can conflict with terminal state and in-place restart behavior. 
- Temporary PNG files created for clipboard-image pastes are tracked and removed when their message is sent or when the staging tray is cleared—do not assume pasted screenshots persist on disk indefinitely. 
- The channel user cache is case-insensitive (StringComparer.OrdinalIgnoreCase) and is guarded by an internal lock; callers should not bypass the orchestrator to modify that state directly.



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


The AppOrchestrator constructor initializes the client by capturing the application context and configuration, creating core UI and service collaborators, wiring their inter-component events, and starting the update checker. It then initializes the main window with a disconnected state, readying the app for user interaction.

## Remarks
As the composition root for the EchoHub client, AppOrchestrator centralizes the creation and wiring of the UI and service layer. It ensures that all collaborators exist before the app becomes interactive and that the event pipelines are in place so user actions, commands, and connection state changes flow through a single, predictable lifecycle. The initial 'Disconnected' status communicates to users that the app is not yet connected, and will transition as connections and updates occur.

## Notes
- The constructor immediately starts the update checker via Start(); this can trigger asynchronous network activity during startup.
- The initialization sequence assumes non-null app and config; null values will throw during assignment.
- Be mindful of multiple AppOrchestrator instances: event subscriptions are established in the constructor and may accumulate if the object is created more than once.

---

### MainWindow
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** property

```csharp
public MainWindow MainWindow => _mainWindow
```


Exposes the application's main window as a read-only property. It returns the private _mainWindow field, allowing callers to access the current MainWindow instance without exposing a setter or the backing field directly. Use this property when UI or orchestration code needs to interact with the main window in a controlled way, e.g., to trigger window-bound actions or dialogs, while preserving encapsulation.

## Remarks
This property serves as an abstraction boundary between the app orchestrator and the UI layer. It decouples consumers from the concrete storage of the main window and provides a stable access point that can be replaced or mocked in tests without changing call sites. It also communicates ownership: the orchestrator owns and manages the MainWindow reference. If initialization order guarantees the value, callers can rely on its presence; otherwise, guard against nulls.

## Notes
- Access before initialization may yield null; ensure initialization before first use.
- If the main window can be swapped at runtime (for testing or multi-window scenarios), consider adding a controlled mechanism to rebind the reference or introduce an interface the rest of the code depends on.
- This property is a simple passthrough of the backing field; for testability, consider introducing an abstraction (e.g., IMainWindow) if you need to mock interactions with the window.

---

### PendingUpdate
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** property

```csharp
public Func<Task>? PendingUpdate => _updateService.PendingUpdate
```


PendingUpdate is a nullable `Func<Task>` that becomes non-null once the user confirms an in-app update. The host should invoke this function after the Terminal.Gui main loop exits and the console has been restored, allowing the updater to restart in-place without fighting the TUI. This property simply proxies the update action from the update service, decoupling the UI lifecycle from the restart logic.

## Remarks
PendingUpdate exists to separate the moment of user confirmation from the actual restart work. It ensures the update runs after the UI has been torn down, avoiding conflicts with the Terminal.Gui lifecycle. By delegating to the update service's PendingUpdate, the hosting code remains agnostic of the specifics of how updates are performed.

## Example
```csharp
// After the Terminal.Gui loop completes and the UI is torn down
var pending = appOrchestrator.PendingUpdate;
if (pending != null)
{
    await pending.Invoke();
}
```

## Notes
- PendingUpdate may be null; guard against null before invoking.
- Because it returns a Task, await the invocation to ensure the update process completes (as appropriate for your app lifecycle).
- Call this only after the UI teardown to avoid interfering with the TUI.

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


Applies a user-selected ASCII size by updating the in-memory configuration, persisting it to disk, and refreshing the staging UI. If a current channel is active, it also broadcasts a system message to inform users that the image ASCII size has been updated, formatting the value via AsciiSizeLabel(flag).

## Remarks
This method centralizes the side effects of changing the ASCII size: it updates the config, persists the change, updates the staging tray, and notifies the channel. It relies on AsciiSizeLabel to render a user-friendly label and on ConfigManager.Save to persist settings; callers should be aware exceptions from these operations propagate to the caller.

## Notes
- Assumes _config is non-null; a null _config would cause a NullReferenceException.
- No input validation: the flag string is stored directly and passed to AsciiSizeLabel; ensure downstream code handles invalid values.
- Synchronous IO: ConfigManager.Save(_config) may block if disk IO is slow; consider invoking from a background task if called from UI event handlers.

## Dependencies
- ConfigManager

## Dependency APIs (verified signatures)

- class [`ConfigManager`](Config/ConfigManager.cs.md) (`src/EchoHub.Client/Config/ConfigManager.cs`)
  - field `string ConfigDir`
  - field `string ConfigPath`
  - property `string ConfigDirectory`
  - field `Lock FileLock`
  - field `JsonSerializerOptions JsonOptions`
  - `ClientConfig Load()`
  - `void Save(ClientConfig config)`
  - `void SaveServer(SavedServer server)`
  - `void RemoveServer(string url)`

## Symbol To Document
- Name: `ApplyAsciiSize`
- Kind: `method`
- File: `src/EchoHub.Client/AppOrchestrator.cs`
- Language: `csharp`
- ID: `db251379-6f70-4217-ba11-e574fbf08e9d`

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


Converts a single-character size flag into a human-readable ASCII size label used by the UI. This private helper centralizes the mapping so callers don't duplicate string literals across the codebase. For flag \"s\" it yields \"Small (40x40)\", for flag \"l\" it yields \"Large (120x120)\", and for any other value it yields \"Medium (80x80)\".

## Remarks
By keeping the label logic in one private, static method, the rest of the codebase benefits from a single source of truth for size labels. The switch expression makes the mapping straightforward to extend if new flags are introduced. Its private scope keeps coupling low and makes intent explicit within its containing type.

## Example
```csharp
// Example usage within the same class
string small = AsciiSizeLabel("s"); // "Small (40x40)"
string defaultLabel = AsciiSizeLabel("x"); // "Medium (80x80)"
```

## Notes
- Access scope: AsciiSizeLabel is private to its declaring type, so it cannot be called from outside the class. If external callers need the same mapping, expose a public wrapper or move the method to a shared utility.

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


Reads a staged file into an OutgoingAttachment. If a roomKey is provided, the method reads the file into memory, determines whether it is a valid image, and for images generates a local ASCII preview using ImageToAsciiService at the requested size; the preview text is then encrypted with roomKey. The file bytes are encrypted with RoomCrypto and encapsulated in the attachment; the attachment also records a declaredKind of image, audio, or file, and includes the (encrypted) preview when available. If no roomKey is supplied, the method returns an OutgoingAttachment that streams the file directly with its name, skipping encryption and preview generation. This design enables end-to-end protection in encrypted channels while keeping behaviors for non-encrypted channels simple.

## Remarks
This method acts as the orchestration point for turning a local file into the transportable OutgoingAttachment used by the client’s outbound pipeline. It encapsulates the conditional encryption and, for images, an on-device ASCII preview to surface a visual cue without exposing the raw image. By delegating validation, ASCII rendering, and crypto to dedicated helpers, it keeps the code focused on assembling the attachment rather than the details of how each piece works.

## Notes
- If roomKey is null, the file is streamed directly without encryption or a preview.
- Large files will be fully loaded into memory for encryption; consider streaming or chunking if this is a concern.
- The preview is only produced for valid images; non-image files skip the preview.

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


Best-effort cleanup of pasted-image temp files and their per-paste folders. The method iterates the provided file paths, deletes each file, and then attempts to remove the containing directory if present; any failures are caught and logged at Debug level, so cleanup does not disrupt the calling workflow.

## Remarks
Encapsulates the cleanup behavior so the paste pathway remains focused on its primary task. It uses a forgiving error-handling strategy: delete what you can, swallow failures, and report them only via debug logs. Because the directory deletion happens after the file removal, the directory will be removed only if it is empty, aligning with typical per-paste directory semantics.

## Notes
- Directory.Delete is non-recursive by default; the directory will be removed only if empty after file deletion.
- Exceptions are swallowed; failures are only logged at Debug level; callers can't rely on exceptions to signal cleanup success.

---

### ClearPendingReply
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void ClearPendingReply()
```

**Returns:** `void`


Clears the active pending-reply state by assigning null to the internal _pendingReply field and informing the UI that there is no current reply in progress by calling _mainWindow.SetReplyingTo(null). As a private helper, it's used within the orchestrator to finalize or abort the reply workflow and keep both the data and the user interface in sync.

## Remarks
Centralizes reply-cleanup logic to avoid duplicating state-management across multiple code paths. By updating both the internal _pendingReply and the main window's replying-to state in one place, it guarantees consistent behavior whenever the reply flow ends. This abstraction helps decouple the decision to clear a reply from the specific UI or flow that triggers it, making future changes to the cleanup process easier.

## Notes
- Must run on the UI thread due to SetReplyingTo updating the UI; calling from a background thread may cause cross-thread exceptions or UI glitches.

---

### Dispose
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
public void Dispose()
```

**Returns:** `void`


Disposes EchoHub client resources in a defined teardown order. On disposal, it persists the last reads, then synchronously disposes the underlying connection, and finally disposes the update service. Call this when the orchestrator is shutting down to ensure all resources are released and the connection is cleanly closed.

## Remarks
This Dispose implements a synchronous disposal flow that bridges an asynchronous disposal of the connection. The explicit GetAwaiter().GetResult() ensures the connection is fully closed before proceeding, but it can introduce deadlocks if called on a synchronization context that blocks. The order of operations — persist reads, close connection, then dispose updates — ensures state is captured before teardown and that dependent services are torn down only after the connection is terminated.

## Notes
- Blocking on asynchronous disposal can lead to deadlocks in certain synchronization contexts; prefer calling this from a non-UI thread or consider an asynchronous disposal pattern if possible.
- If an exception is thrown during PersistLastReads, DisposeAsync, or _updateService.Dispose, disposal may be interrupted and not all resources may be released.
- This method is not guarded for multiple invocations; repeated calls may encounter disposed resources.


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


This private guard ensures outbound messages to a channel are only sent when the room is accessible: if the channel is not end-to-end encrypted or already has a key, sending is allowed immediately. If the channel is encrypted but locked, it triggers the unlock flow and returns true only when unlocking succeeds; otherwise it reports an error and blocks the send.

## Remarks
By centralizing the gating logic here, the sending path consistently enforces encryption constraints and user interaction for unlocking. It bridges the connection manager's RoomKeys state, the unlocking prompt, and the UI feedback loop so callers don't have to duplicate this logic. Keeping this logic in this dedicated helper reduces the risk of inconsistent encryption checks scattered across the codebase.

## Notes
- Short-circuits and returns true if the channel doesn't require unlocking or already has a key; otherwise it may present an unlock prompt and potentially abort the send.
- If unlocking is required, it calls UnlockTrackedChannelAsync and may present a prompt to the user; a declined or failed unlock results in a false return and an error message.
- It relies on the RoomKeys state and uses UI feedback (via _mainWindow.ShowError) to communicate failures to the user; callers should respect the returned boolean and refrain from sending when false.

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


Responds to a request to join a channel coming from a message by first ensuring the connection is active; if not connected, it exits early. When connected, it marshals execution to the UI thread, guarantees the channel is present in the UI channel list, switches the UI to that channel, and then delegates to HandleChannelSelected to trigger further channel-activation handling.

## Remarks
Acts as the glue between the messaging flow and the UI state. By marshaling to the UI thread and centralizing the sequence of ensuring the channel exists and becomes active, it helps prevent cross-thread issues and keeps the UI in sync with inbound join requests. The subsequent call to HandleChannelSelected ensures that any additional, per-channel activation logic is applied in a single, well-defined step.

## Notes
- No input validation is performed on channelName here; downstream UI methods may interpret empty or invalid values in different ways. Validate as needed at call-sites if that matters for your scenario.
- The method is a no-op when not connected, so callers relying on its side effects must ensure connection lifecycle is managed appropriately.

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


HandleChannelSelected is invoked when a user selects a channel in the client UI. It first verifies the client is connected and returns immediately if it is not; it also clears any pending reply that does not belong to the newly selected channel and persists the last-read positions before switching context.

In the asynchronous workflow that follows, the method attempts to track the selected channel. If tracking succeeds, it prompts for a password as needed and, on a successful join, clears the left-channel exclusion for the joined channel. If the user cancels the password prompt, the channel is untracked and the UI switches back to the default channel. If tracking is not required but an unlock is needed, it triggers an unlock of the tracked channel.

After handling join/unlock, the method tries to retrieve the channel history and load it into the UI; failures to fetch history are ignored. Finally, it refreshes the list of online users.


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


Processes a user-issued command to assign a role to a specific user. It short-circuits when the current connection is not authenticated, preventing unauthorized role changes. It normalizes the incoming role string into a ServerRole enum (admin maps to Admin, mod to Mod, and any other value defaults to Member) and then calls the server API to apply that role to the given username.

## Remarks
Acts as a small wrapper around the underlying API to centralize access control and input normalization for role assignment commands. By guarding the operation behind the authentication check, it ensures only authenticated sessions can modify roles. The conservative default to Member for unknown role strings provides a safe fallback that avoids elevating privileges due to unrecognized input.

## Notes
- The operation does nothing if the client is not authenticated.
- Despite the authentication check, Api is accessed with a null-forgiving operator; if _conn.Api is null, this will throw at runtime. Ensure the API client is initialized alongside authentication.
- The role default to Member for unknown values can mask invalid input; if explicit validation is desired, this behavior may need to be revisited.

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


Handles the ban-user command by ensuring the client is authenticated and then delegating to the API to perform the ban. It accepts a target username and an optional reason; if the current connection is not authenticated, it returns without taking action. When authenticated, it invokes BanUserAsync on the API, passing along the username and reason.

## Remarks
This method acts as a focused command handler: it enforces authentication and delegates the ban operation behind a single, well-defined API boundary. By isolating the authentication gate here, the rest of the command processing remains decoupled from the specifics of how a ban is performed. Any failures from BanUserAsync propagate to the caller, enabling centralized error handling or user feedback at a higher level.

## Notes
- Silent return when not authenticated means callers may not observe a failure path for unauthenticated ban attempts; consider logging or surfacing a result if caller feedback is required.

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


Handles the user command to change the end-to-end encryption passphrase for the currently active channel. It validates that the client is authenticated and connected, that a channel is selected and encrypted, and then re-derives the join credentials for both the old and new passphrases. It then asks the backend to re-key the channel by wrapping the cached room key with the new derived key. History remains readable; the room key itself does not change, so previously encrypted content stays decryptable by existing history, while new members/devices must use the new passphrase.

## Remarks
This function coordinates client state (the current channel and the cached room key) with a server-side RekeyChannelAsync call to effect a passphrase transition. It guards against common failure modes (not authenticated, no channel selected, channel not end-to-end encrypted, missing room key) by showing targeted UI errors. The derived key material (old and new) and a fresh salt ensure that only someone with the new passphrase can wrap/unwrap the room key going forward.

## Notes
- If the channel is not end-to-end encrypted or lacks an encryption salt, the operation aborts with a user-facing error.
- The action requires that the room key for the channel is cached; otherwise the user is prompted to unlock/rejoin the channel.
- The change does not re-encrypt historical messages; it only re-wraps the room key under the new passphrase, so access for existing history remains intact while access for new participants depends on the new passphrase.

---

### HandleCmdClearAttachments
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdClearAttachments()
```

**Returns:** `Task`


Clears the currently staged attachments, removing any files that are also present in the temporary pasted set, cleans up their temporary copies, clears the staging collection, and refreshes the staging UI. Invoke this when the user requests to discard all attachments added during the current operation; it centralizes the cleanup logic to keep in-memory state and the UI in sync.

## Remarks

This method coordinates between in-memory state (_stagedAttachments), the temporary-file tracking (_tempPastedFiles), and the UI update path (InvokeUI(RefreshStagingTray)). By encapsulating the cleanup sequence, callers avoid duplicating disposal and UI-refresh logic, and it guarantees that temporary files created for pasted content are removed in tandem with the in-memory state.

## Example

```csharp
// Example usage within the same class
private async Task DemoClearAttachmentsUsage()
{
    await HandleCmdClearAttachments();
}
```

## Notes

- The method returns Task.CompletedTask, so from a caller's perspective it is effectively synchronous; awaiting it does not introduce real asynchronous work.
- If other code mutates _stagedAttachments or _tempPastedFiles concurrently, results may be inconsistent; this method assumes a single-threaded (UI) invocation context.
- If CleanupPastedTempFiles(temps) throws, the task will fault; callers should consider exception handling around the command invocation.

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


Creates a channel-scoped invite by calling the API with the optional maxUses and expiresHours and displays the resulting code in the current channel; it includes uses, an optional expiry, and a tip to revoke with a slash command. If the user is not authenticated or there is no current channel, it exits early and performs the work asynchronously to keep the UI responsive.

## Remarks
This method orchestrates authentication state, UI context, and remote API to provide a seamless command experience. It isolates the network call and UI update from the main thread using RunAsync, reducing UI latency and potential blocking, while presenting the user with a clear revoke path to manage the created invite.

## Notes
- Relies on _conn.Api being non-null after authentication; if that invariant is violated, the null-forgiving operator may lead to a NullReferenceException.
- The expiry timestamp is formatted in local time when available, via ToLocalTime with the pattern "yyyy-MM-dd HH:mm".
- The outer method returns Task.CompletedTask immediately; the actual invite creation and UI update run asynchronously, so callers should not expect any awaitable work from this method.


---

### HandleCmdDeleteAccount
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdDeleteAccount()
```

**Returns:** `Task`


Deletes the currently authenticated user’s account from the server as part of the user command workflow. It first ensures the client is authenticated, presents a destructive confirmation dialog explaining that the account will be permanently removed (including profile, sessions, and uploaded files, with messages remaining attributed to 'deleted-user'), and only proceeds if the user confirms. If confirmation is given, it prompts for the account password, then asynchronously performs the deletion via the server API, clears saved tokens for the server, and performs cleanup. On success, the UI is reset (main window cleared, status set to Disconnected) and the user is informed that the account and uploaded data have been deleted.

The operation is designed to run asynchronously to keep the UI responsive and to encapsulate the end-to-end flow of a high-risk action within a single command handler.

---

### HandleCmdExportData
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdExportData()
```

**Returns:** `Task`


Exports the authenticated user\'s data by calling the server API and writing the resulting JSON to a timestamped file in the local downloads directory. The file is named echohub-export-{username}-{yyyyMMdd-HHmmss}.json to avoid collisions. The operation runs asynchronously so the UI remains responsive. After the file is created, a system message is posted to the current channel (if any) indicating the destination path and clarifying that the content is ciphertext and the server never had plaintext.

## Remarks
The method centralizes the data-export workflow behind a command handler, encapsulating authentication gating, local persistence, and user feedback. It ensures the UI remains responsive by offloading work to an asynchronous runner and then dispatching a UI update once the export completes. This abstraction keeps the channel-communication logic separate from the networking and file-system concerns, making the export action reusable from the UI command surface.

## Notes
- The final user notification is sent only when a non-empty channel is present; otherwise, no in-channel message is posted.
- If authentication fails, the method completes without performing the export, avoiding unnecessary work or side effects.
- The exported payload is treated as ciphertext in the UI message, reflecting that the server-side data is not plaintext within the export artifact.

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


Handles the user command to join a channel by validating the connection, prompting for a password when required, removing any left-channel exclusion for the channel on success, updating the UI to include and switch to the channel, and loading previous history if available. It is invoked as part of the command execution flow when a user requests to join a channel, encapsulating the join logic and related UI updates in one place.

## Remarks
It centralizes the channel-join flow in AppOrchestrator, coordinating connection state, password prompting, server configuration, and UI navigation. By clearing the left-channel exclusion on join (RemoveAll with a case-insensitive match), it ensures a fresh entry for the channel. UI updates are dispatched to the main thread via InvokeUI, and history loading is triggered when available, keeping the user experience cohesive even when past sessions exist.

## Notes
- Skips work if there is no active connection; the method exits early when _conn.IsConnected is false.
- If the password prompt is canceled (history is null), the join is aborted without altering server state or UI.
- The cleanup of LeftChannels uses a case-insensitive match to ensure the channel name is removed regardless of casing.
- Exceptions are caught and shown to the user via the main window, which prevents the app from crashing but may hide internal details.

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


Handles the Kick User command by delegating to the underlying API to remove a user from the session. Before performing the kick, it checks that the connection is authenticated; if not, it exits without taking action. When authenticated, it calls KickUserAsync on the API, passing the target username and the optional reason. This method centralizes the guard logic and the actual kick call under a single handler, so higher-level command processing can rely on a single moderation action point rather than invoking the API directly.

## Remarks
Acts as a boundary between command parsing and server-side moderation actions. It ensures that only authenticated sessions can initiate a kick, preventing unauthorized disruptions. The use of the null-forgiving operator on Api implies the API instance is expected to be non-null once authentication is established; this contract should be preserved by the surrounding code.

## Notes
- Be aware that Api is accessed with the null-forgiving operator, so a non-null Api is required after authentication; otherwise a NullReferenceException may be thrown.
- Exceptions from KickUserAsync propagate to the caller; callers may need to catch and handle errors (e.g., log failures or surface user-visible errors).
- The reason parameter is optional; passing null means no reason will be recorded.

---

### HandleCmdLeaveChannel
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdLeaveChannel()
```

**Returns:** `Task`


Handles the user command to leave a channel. It exits early when not connected or when no channel is selected, blocks leaving the default channel, calls LeaveChannelAsync, records the leave in the server configuration's LeftChannels so auto-join won't pull you back in, and posts a system message confirming the departure. If something goes wrong, it reports the error to the UI.

## Remarks
Coordinates UI feedback, connection state, and user preferences. It separates the transient action of leaving a channel from the persistent config, ensuring the user's choice is remembered and doesn’t get undone by auto-join logic on reconnect. The UI thread marshalling via InvokeUI and the defensive checks guard against invalid states and repeated leaves.

## Notes
- If the user is not connected or no channel is selected, the method returns without side effects.
- Leaving the DefaultChannel is explicitly blocked to avoid breaking the core chat experience.
- LeftChannels is updated using a case-insensitive check (OrdinalIgnoreCase) to deduplicate and persist the user's choice across sessions.
- Any exception during LeaveChannelAsync is surfaced to the user via ShowError.

---

### HandleCmdListInvites
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdListInvites()
```

**Returns:** `Task`


After verifying authentication and a current channel, this method fetches invite codes from the API, formats each invite’s usage and state into a readable list, and posts the result as a system message to the active channel. If no invites exist, it guides the user on how to create one.

## Remarks

Serves as the UI-facing adapter for the invite-list feature by coordinating API access, formatting, and UI updates. Each invite is categorized as used up, expired, or active based on UseCount, MaxUses, and ExpiresAt: used up when UseCount >= MaxUses; expired when ExpiresAt has a value and is in the past; otherwise active. If ExpiresAt has a value, the expiration date is shown in local time using the format yyyy-MM-dd HH:mm. The multi-line message is posted to the channel via InvokeUI to ensure thread-safety, and failures are surfaced by RunAsync with the message Failed to list invites.

## Notes

- Early exits guard: returns Task.CompletedTask if not authenticated or there is no current channel, preventing API calls or UI updates.
- The method accesses _conn.Api using the null-forgiving operator, which assumes Api is non-null after authentication.
- Expiration handling relies on ExpiresAt being present to show an expiration timestamp and formats it using local time; if ExpiresAt is null, the invite is considered active unless UseCount/MaxUses dictate otherwise.

---

### HandleCmdListUsers
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdListUsers()
```

**Returns:** `Task`


HandleCmdListUsers retrieves and displays the list of online users for the currently selected channel when the client is connected. It requests the online user list from the underlying connection and then updates the UI to present each user with their display name (falling back to username) and their status, including any status message. If there is no active connection or no channel selected, the method exits without performing work; any error during retrieval or UI update is caught and reported to the user.

## Remarks
By centralizing the command handling for listing users, this method cleanly separates networking concerns from UI rendering. It uses InvokeUI to marshal updates to the main thread, ensuring thread-safety when modifying the UI. The rendered output displays a header indicating the channel, followed by one line per user that shows a display name and a textual status, including any optional status message when present.

## Notes
- Guards against no connection or empty channel by returning early, making the call effectively a no-op in those cases.
- All exceptions during retrieval or UI updates are caught and surfaced to the user via a dialog, preventing crashes.
- The rendering relies on Status.ToString(); ensure Status is non-null in your UserSession model or guard accordingly to avoid potential NullReferenceException.


---

### HandleCmdMeta
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdMeta()
```

**Returns:** `Task`


Fetches the current channel's metadata from the API and renders a concise room-info block in the chat UI. The method only runs when the client is connected and a channel is selected, and it gracefully handles missing metadata or API errors by reporting them to the user.

## Remarks
This symbol acts as the UI-facing bridge between the network layer and the chat view, encapsulating the logic to fetch channel metadata and translate raw values into human-friendly text. It centralizes formatting decisions (such as the estimated size display and the protection status label) and ensures all UI updates occur on the UI thread through InvokeUI, preventing cross-thread UI mutations. By using early returns for preconditions and clear error signaling, it keeps the UI responsive and predictable even when the channel information cannot be retrieved.

## Notes
- If meta.EstimatedSizeBytes <= 0, the size is displayed as "0 B"; otherwise the value is converted via FormatFileSize to a human-friendly string.
- When the API returns null for the channel metadata, the method surfaces a user-facing error {Channel #<channel>} not found to avoid throwing.
- All UI updates are dispatched to the UI thread via InvokeUI to maintain thread safety during cross-thread operations.


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


Handles the mute-user command by delegating to the API after ensuring the client is authenticated. If the client is not authenticated, it exits early without performing any action.

## Remarks
This method serves as a small glue layer between command-handling logic and the backend API. It enforces the precondition that only authenticated sessions can mute users and centralizes that guard in one place. The actual mute action is performed by the Api.MuteUserAsync call; the code uses the null-forgiving operator on Api, which implies Api must be non-null when IsAuthenticated is true.

## Notes
- Silent no-op when not authenticated means callers may not see feedback; consider surfacing an explicit denial to the user at the call site.
- The null-forgiving operator on Api implies Api must be initialized for authenticated sessions; a mismatch could throw NullReferenceException.
- There is no cancellation support in this path; the caller cannot cancel the operation here.

---

### HandleCmdNukeChannel
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdNukeChannel()
```

**Returns:** `Task`


This private async method performs a destructive operation on the currently selected channel by calling NukeChannelAsync on the authenticated API. It first ensures there is an authenticated connection and a non-empty channel; if either condition is not met, it returns early. Otherwise, it invokes the server-side NukeChannelAsync with the selected channel.

## Remarks
This method encapsulates the preconditions required to perform a channel-nuke, keeping the UI-orchestrator code free from direct API calls. It acts as a gatekeeper, ensuring that only an authenticated user with a selected channel can trigger the destructive server-side operation. The actual deletion is delegated to the API via NukeChannelAsync; thus, changes to the nuking behavior should be made at the API level.

## Example
```csharp
// Example usage from within the same class
await HandleCmdNukeChannel();
```

## Notes
- The method relies on _conn.IsAuthenticated and a non-empty current channel; if _conn.Api is null at this point, the null-forgiving access (_conn.Api!) could produce a NullReferenceException. Ensure the API surface is initialized after authentication.
- The operation is destructive; it intentionally performs a server-side action only when preconditions are satisfied, and it swallows precondition failures by returning early without throwing.
- This method does not guard against changes to authentication or channel state between the precondition checks and the API call, so consider synchronization if those state changes can occur concurrently.

---

### HandleCmdOpenServers
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdOpenServers()
```

**Returns:** `Task`


Dispatches a UI action to open the saved servers UI by invoking HandleSavedServersRequested on the UI thread, and returns a completed Task. It acts as a command-handling bridge that does not perform long-running work itself, delegating the actual UI work to the UI layer.

## Remarks
Helps keep command-handling code decoupled from UI-thread specifics. This abstraction lets the orchestrator trigger UI flows without assuming any UI state or thread affinity, and it centralizes the UI-dispatch pattern via InvokeUI.

## Notes
- The returned Task is completed immediately; the actual UI work runs asynchronously on the UI thread.
- Exceptions raised by HandleSavedServersRequested are not propagated through the returned Task and should be handled within the UI path.
- This method is private and intended for internal orchestration; callers should not rely on its Task representing the completion of the UI action.

---

### HandleCmdQuit
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdQuit()
```

**Returns:** `Task`


HandleCmdQuit is a private command handler that initiates application shutdown by marshaling a stop request to the UI thread and returning a completed Task. It is intended for quit commands where the actual stop must be executed within the UI context.

## Remarks
By funneling the stop through InvokeUI, this symbol ensures the shutdown sequence runs on the UI thread, preventing cross-thread access issues. It centralizes quit behavior in the orchestrator, keeping UI concerns isolated from the command-processing path. The method returns a completed Task to maintain an asynchronous signature, while the actual stopping occurs asynchronously on the UI thread via RequestStop. If the UI thread handling fails, the shutdown may surface as an exception in that context.

## Notes
- It does not await the stop; the caller should not assume the application is fully stopped after this method returns.
- Exceptions during UI marshaling or in the UI thread may propagate outside this method.
- Assumes _app is non-null and that RequestStop is safe to call on the UI thread.

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


HandleCmdRevokeInvite revokes an invite by code when the user issues the revoke command. It first ensures the client is authenticated and that a channel is currently selected; if either check fails, it completes without performing any action. When both conditions are met, it runs the revoke operation asynchronously against the API and, after revocation, inserts a system message into the active channel confirming that the invite has been revoked, using the code formatted in upper-case via ToUpperInvariant().

## Remarks
Encapsulates the command-handling concerns for invites by coordinating authentication state, channel context, API invocation, and UI feedback. The early returns prevent unnecessary API calls and potential exceptions in invalid contexts. Using RunAsync ensures the UI remains responsive and that failures surface with a clear error message ("Failed to revoke invite").

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


HandleCmdSendAction orchestrates sending a CTCP ACTION message to the currently selected channel. It guards against being disconnected or lacking a channel, and when invoked, schedules a background send that first unlocks the room for sending and then dispatches the formatted action payload through the established connection, preserving the encryption used for ordinary messages.

## Remarks
This method centralizes the CTCP ACTION sending logic: it validates the connection and channel state, formats the action text using the project’s action conventions, and routes the payload through the same encrypted send path used for regular messages. By gating the unlock step with EnsureRoomUnlockedForSendAsync, it respects per-channel locking and avoids sending actions to rooms that are not ready for sending. The CTCP ACTION content therefore benefits from the same encryption, routing, and error handling as normal messages.

## Notes
- The actual network send is performed asynchronously inside RunAsync; failures surface through the RunAsync error path with the message 'Send failed'.
- If EnsureRoomUnlockedForSendAsync(channel) returns false, no message is sent; the operation aborts gracefully.

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


Handles the user command to render and send an ASCII banner to the currently selected channel. It validates connectivity and a non-empty channel, renders the banner via AsciiBannerService.Render, and, if rendering succeeds, dispatches a background task to ensure the room is unlocked before sending the banner over the active connection. If rendering yields nothing, it surfaces a UI error detailing the allowed characters and the maximum input length sourced from AsciiBannerService.MaxInputLength.

## Remarks
Consolidates the end-to-end flow from user command to network transmission by coordinating the UI layer, the ASCII banner renderer, and the messaging connection. It delegates permission checks to EnsureRoomUnlockedForSendAsync and offloads the actual send to a background task via RunAsync, preserving UI responsiveness and isolating concerns among rendering, validation, and transport.

## Notes
- Early exits (not connected or no channel) return immediately without user-facing feedback.
- Rendering failures produce a user-facing error rather than throwing, tying the UX to the banner rendering service.
- The error message reflects AsciiBannerService.MaxInputLength, tying user feedback to the configured rendering limits.
- The banner is sent only after EnsureRoomUnlockedForSendAsync(channel) confirms the room is unlocked.

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


Handles the /send command to attach local files or transmit a URL to the current chat channel, validating authentication and a selected channel; URLs are sent immediately for non-encrypted channels while local files are staged for a single subsequent message up to the attachment limit, and any provided size flag updates the default ASCII size for future sends.

## Remarks

URLs are sent immediately when allowed by the channel's encryption state, bypassing the staging area; local files are queued in a staging collection and dispatched together with a caption when the user confirms. This separation ensures encrypted channels remain protected and enforces HubConstants.MaxAttachmentsPerMessage to prevent oversized messages; UI refresh via the staging tray keeps the user informed of what will be sent.

## Notes

- URL sends are blocked in encrypted channels; users see an error guiding them to download the file and send it instead.
- If the user is not authenticated, not connected, or no channel is selected, the method returns a completed task without user feedback.
- The ASCII size flag updates a global default for subsequent sends rather than applying to the current staged set.

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


Opens or sets the ASCII-art size for attached images. If a recognizable argument is supplied, the size is applied immediately; otherwise, a UI dialog prompts the user to choose Small, Medium, or Large, and the chosen size is persisted as a preference for future attachments.

## Remarks
This method centralizes the user experience for configuring ASCII art size by handling both direct argument parsing and an interactive picker. Parsing is delegated to NormalizeAsciiSize, and the actual update is performed by ApplyAsciiSize, with UI-affecting work marshaled through InvokeUI to ensure thread-safety. The two-path design supports quick scripted usage and an explicit, user-driven selection while keeping behavior consistent.

## Notes
- If the argument does not yield a non-null flag, the interactive picker is shown; pressing Cancel results in no change.
- The mapping from the picker result to the internal values is explicit (Small -> "s", Medium -> "m", Large -> "l").
- The method completes its Task synchronously from the caller's perspective; UI updates are executed asynchronously via InvokeUI.


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


Handles the Set Avatar command by updating the user's avatar when the client is authenticated. It uploads the specified target avatar via AvatarHelper.UploadAsync using the current API client, and on success posts a system message in the current channel to indicate that the avatar was updated. If an error occurs, the method logs the exception and shows a user-facing error dialog with the exception message.

## Remarks

This method acts as an orchestration boundary between authentication, server communication, and UI feedback. It delegates the actual upload to AvatarHelper.UploadAsync and uses InvokeUI to marshal UI updates onto the UI thread. Because the method ignores the UploadAsync result, its success is determined solely by the absence of an exception.

## Notes

- If not authenticated, the method returns early with no side effects.
- The error path surfaces ex.Message to the user, which may reveal internal details; consider masking sensitive information in production.

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


Handles a color-setting command by updating the current user's nickname color via the profile API, but only when the client connection is authenticated. If not authenticated it exits early without performing any update. When authenticated, it constructs an UpdateProfileRequest with NicknameColor set to the supplied color and invokes UpdateProfileAsync on the API client to persist the change.

## Remarks
Acts as a small command handler within AppOrchestrator that translates a user-issued color command into a persistence operation. It cleanly separates command validation (authentication check) from the transport to the backend profile service, delegating the actual update to the API layer.

## Notes
- The method relies on _conn.IsAuthenticated being accurate; if that state changes between the check and the API call, updates could occur inconsistently.
- It assumes _conn.Api is non-null after authentication (using the null-forgiving operator). If Api is unexpectedly null, a NullReferenceException may be thrown.
- There is no input validation on the color value; invalid colors may be rejected by the backend or cause API-side validation errors.
- Exceptions from UpdateProfileAsync propagate to the caller; callers should handle potential failures when updating the profile.

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


Handles nickname changes by persisting the new DisplayName to the server when the client is authenticated, and then updates the UI to reflect the new nickname and a connected status. If the client is not authenticated, it exits early without performing any network or UI changes.

## Remarks
Centralizes the nickname-change flow inside the AppOrchestrator, bridging the command, network call, and UI update. The UI updates are marshaled via InvokeUI to ensure thread affinity, and the Api reference is assumed non-null once IsAuthenticated holds, as evidenced by the null-forgiving operator.

## Notes
- The method uses _conn.Api! to pass UpdateProfileRequest to the API, relying on IsAuthenticated to guarantee Api is non-null; if that guarantee ever fails, a NullReferenceException could occur.
- No input validation of displayName is performed here; validation and sanitization should occur at higher layers or via server-side checks.

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


Handles the command to set the current user's status and optional message. It only executes when the client is connected; it determines the final status and message from the provided arguments, preserving existing values when an argument is omitted and clearing the message when an empty string is supplied. The method updates the remote server via UpdateStatusAsync and then synchronizes the local session state.

## Remarks
This abstraction centralizes how a user-initiated status change is applied across both server and client state. It ensures consistent behavior for preserving or clearing values and guarantees the UI and session reflect the server after a successful update. The operation is a no-op when the connection is unavailable, avoiding unintended network activity.

## Notes
- Early return when not connected means there is no network call or local state change in offline scenarios.
- Null status or null message preserves the current values; an empty message clears the status message.
- The server update is performed before mutating the local session; if UpdateStatusAsync throws, the session remains unchanged.

## Dependencies
- _conn
- _session

## Dependency APIs
- _conn.IsConnected: bool
- _conn.UpdateStatusAsync(UserStatus newStatus, string? newMessage): Task
- _session.Status: UserStatus
- _session.StatusMessage: string?

## Symbol To Document
- Name: HandleCmdSetStatus
- Kind: method
- File: src/EchoHub.Client/AppOrchestrator.cs
- Language: csharp
- ID: 2eb21054-d449-44d8-a265-4b55287688a4

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


Marshals a theme-change request to the UI thread via InvokeUI by invoking HandleThemeSelected(name). It does not perform the theme switch itself; instead it queues the work on the UI thread and returns a completed Task. This is useful when a command handler or non-UI code needs to trigger a theme change while preserving proper UI thread affinity.

## Remarks
HandleCmdSetTheme acts as a small bridge between command reception and UI state mutation. By decoupling the theme-name parameter from the actual UI update, it keeps non-UI layers free of UI-thread requirements and centralizes the invocation in InvokeUI. The pattern helps prevent cross-thread access issues while keeping a concise, test-friendly signature.

## Notes
- Awaiting the returned Task does not guarantee the theme has been applied; the UI action runs asynchronously on the UI thread after this call returns.
- Exceptions raised during HandleThemeSelected occur on the UI thread and are not surfaced through this Task; consider handling errors inside the UI action or via a global UI exception handler.

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


HandleCmdSetTopic is the internal handler for the user command that sets the topic of the current channel. It first guards against an unauthenticated user and a missing channel context; if either condition is not met, it exits without performing any update. When both checks pass, it updates the channel topic via the API, and on success it marshals back to the UI thread to refresh the display and emit a system message confirming the new topic. If an error occurs during the API call or UI update, it surfaces an error message to the user.

## Remarks
This method serves as the glue between the command processing layer, the network API, and the user interface. It encapsulates the end-to-end workflow for a topic update: validation of preconditions, server-side update, and immediate UI feedback, ensuring consistent state across the client and server while minimizing duplication of error handling across the command pipeline.

## Notes
- The API reference is accessed with a null-forgiving operator (_conn.Api!), so a null Api client can result in a NullReferenceException. Consider guarding Api’ availability or ensuring lifecycle management guarantees a non-null Api before invocation.
- If authentication is missing or there is no active channel, the method returns without user-facing feedback, which means callers should ensure appropriate UX messaging when these preconditions fail.
- Exceptions raised by the API call or by UI updates invoked in the success path are caught and surfaced as a user-visible error message; such exceptions are not propagated to the caller.

---

### HandleCmdTestSound
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdTestSound()
```

**Returns:** `Task`


When invoked, this private async handler triggers a test playback of the notification sound by delegating to the injected _notificationSound and awaiting PlayTestAsync. It serves as a small command-handler within AppOrchestrator to validate audio feedback without blocking the command processing flow.

## Remarks
Acts as a thin orchestration boundary, delegating to the notification sound service. This keeps the command-handling flow decoupled from the specifics of sound playback, enabling the sound provider to be swapped or mocked in tests. Being private ensures it's only called from within the orchestrator's command-processing logic, preserving encapsulation.

## Notes
- No cancellation token is passed to PlayTestAsync from this wrapper; if cancellation is required, consider extending the API or wiring cancellation from the caller.
- Exceptions from PlayTestAsync will bubble up to the caller; consider adding error handling if resilience is needed in the command pipeline.

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


Unbans a user by delegating to the client API after confirming the current connection is authenticated. If the connection is not authenticated, the method returns immediately without performing any API call. When authenticated, it invokes Api.UnbanUserAsync with the provided username and awaits its completion, ensuring the operation completes asynchronously without blocking the caller.

## Remarks
This method serves as a small command-handler primitive within the orchestration layer. It encapsulates the authentication check and the unban operation, so higher-level command wiring does not need to duplicate this guard. By awaiting the API call, it preserves responsiveness and enables proper exception propagation to the caller if the unban fails. The null-forgiving operator on Api expresses the expectation that the API client is prepared after a successful authentication handshake.

## Notes
- Exceptions from Api.UnbanUserAsync may propagate to the caller; callers should handle errors at a higher level.
- If invoked while not authenticated, the method performs no action and returns immediately.

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


Unmutes a user by delegating to the remote API after verifying that the current session is authenticated. If the client is not authenticated, the method returns immediately without issuing any API call.

## Remarks
This method is a small piece of the command-handling layer in AppOrchestrator. It couples the authentication check to the concrete unmute operation, ensuring that unmute requests are only sent to the API when a user is authenticated. By encapsulating this flow in one place, it keeps higher-level command logic focused on parsing input while the API interaction remains centralized and straightforward.

## Notes
- No error handling within this method; any exception thrown by UnmuteUserAsync will propagate to the caller.
- If _conn.Api is null even after authentication, the null-forgiving operator is used, which will throw at runtime if the API reference is missing.
- The method is private, so its behavior is only observable within the containing class (and tests) and is not part of the public API.

---

### HandleConnect
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleConnect()
```

**Returns:** `void`


HandleConnect coordinates the user-initiated connection workflow. It first checks whether a connection is already active and, if so, prompts the user to disconnect before proceeding; if the user confirms, it initiates a disconnect. Otherwise it shows a ConnectDialog to select a server and credentials, logs the attempt, and runs the asynchronous connection. It calls _conn.ConnectAsync and updates the status bar as the connection progresses. If a saved refresh token exists but the attempt fails due to an expired or revoked session, it logs a warning, clears the saved token, informs the user, and aborts the connection attempt. On success, it records the resulting login, clears any pending unlocks, loads the last-read markers for the target server from the saved config, and updates the UI: sets the current user, loads channels, switches to the default channel, loads per-channel histories (honoring lastRead where available), focuses the input, and fetches the list of online users. Finally it persists the selected server configuration.

## Remarks
HandleConnect serves as the central flow controller that binds the UI to the authentication/connection protocol and the post-connection UI state. It isolates network logic from the UI by performing the connect work inside RunAsync and marshaling UI updates via InvokeUI; it also coordinates reading and applying per-server state (channels, histories, last-read markers) so the user sees an up-to-date workspace after connecting.

## Notes
- It only handles a special exception path when a saved refresh token is present; other exceptions bubble to the RunAsync failure handler and surface a generic "Connection failed" message, so callers should be prepared for a generic error state.
- The last-read history seeding relies on the SavedServers list in the config; if the server URL isn't found, lastRead will be null and histories will start from the most recent history loaded from the server.

---

### HandleCreateChannelRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleCreateChannelRequested()
```

**Returns:** `void`


Handles the user-initiated request to create a channel. It validates that the client is authenticated and connected, prompts the user for channel details via a dialog, and, if the user proceeds, performs the creation workflow asynchronously. If a password is provided for the channel, it derives a per-channel join credential and locally wraps a new room key so that the channel content remains end-to-end encrypted (the passphrase never leaves the client). The workflow includes salt generation, key derivation, room key generation, base64-encoding of the salt, and wrapping the room key with the derived key, before sending the resulting wirePassword, saltB64, and wrappedKey to the server. On success, any generated room key is stored for the channel, the client joins the channel, and its history is loaded if present. UI state is updated to show the new channel, set its topic, and switch focus to it, followed by a refresh of online users. If the user cancels the dialog or the channel creation fails, the method exits without side effects.

The method orchestrates UI interaction, server communication, encryption preparation, and post-create UI updates in a single, cohesive flow, thereby providing a predictable and secure channel creation experience from a single entry point.


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


Deletes a message in response to a user-initiated delete request.

The method first guards against unauthenticated calls by returning early if the connection is not authenticated. When authenticated, it delegates the actual deletion to the server via the API and relies on the server to enforce the hierarchy rule (own message or Mod+ over a strictly lower role) and to broadcast the deletion to all clients. The local message list is updated only in response to that broadcast, not by mutating internal state directly within this method.

## Remarks

This function acts as a thin bridge between the UI action (delete request) and the network operation, deferring permission checks to the server to ensure consistent enforcement across clients. It also centralizes error handling through RunAsync, which surfaces a user-facing message ("Failed to delete message") if the server operation fails. By relying on the server broadcast to refresh local state, it avoids duplicating deletion logic on the client and keeps the UI in sync with server-side state changes.

## Example

```csharp
// Called when the user confirms deletion of a message with ID messageId
HandleDeleteMessageRequested(messageId);
```

## Notes
- The method is a no-op when the connection is not authenticated, avoiding any local or server interaction.
- The local message list is updated via server broadcasts rather than direct mutation inside this method, ensuring consistency with server-side state and other clients.

---

### HandleDisconnect
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleDisconnect()
```

**Returns:** `void`


Handles the disconnection sequence for the EchoHub client orchestrator. It is invoked when the connection to the server is lost or a deliberate disconnect is initiated. The method centralizes teardown steps to ensure a consistent, user-friendly shutdown: it logs the disconnect, clears per-channel user state under a dedicated lock, resets the pending reply to avoid processing stale data, updates the UI to reflect that no reply is expected, persists the last-read state, and then kicks off an asynchronous cleanup of the underlying connection. The UI is updated on the main thread to display a disconnected state after cleanup, preserving responsiveness during teardown.

## Remarks
This abstraction exists to provide a single, well-defined path for disconnect scenarios, reducing the risk of partial teardown and inconsistent UI state across call sites. By guarding channel-user state with a lock, it prevents race conditions during teardown, and by marshaling UI updates through InvokeUI, it maintains proper thread affinity between background work and the UI. Running the cleanup asynchronously ensures the user interface remains responsive while the connection is terminated and resources are released.

## Example
```csharp
// Example usage: internally invoked when the underlying connection reports a disconnect
HandleDisconnect();
```

## Notes
- Clearing _channelUsers is performed under _channelUsersLock to prevent races with other threads that may access the collection during disconnect.
- _pendingReply is set to null to avoid handling a stale or invalid reply after disconnect.
- UI updates are dispatched through InvokeUI to guarantee execution on the UI thread, even when called from a background context.
- The actual connection teardown happens asynchronously via RunAsync, so any resulting UI changes (e.g., the status bar showing "Disconnected") occur after cleanup completes.


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


Drives the user-initiated profile editing flow. It opens a ProfileEditDialog pre-populated with the current profile values, and if the user submits changes, it updates the server (when authenticated), refreshes the UI with the new display name, and optionally uploads a new avatar. It also applies any updated notification settings and persists the updated profile as the default preset in the local configuration.

It encapsulates the end-to-end sequence as an asynchronous operation: show the dialog, validate the result, perform server updates, and reflect changes in both the UI and local config. The operation is guarded by an authentication check and is executed via RunAsync to avoid blocking the UI thread, with careful UI updates performed on the main thread where appropriate.

## Remarks
Centralizes the profile-edit workflow in a single coordinator: it integrates the user dialog, the remote profile update, avatar handling, notification preference updates, and the persistence of a default profile preset. This orchestration ensures consistency across server state, UI presentation (display name and status), avatar state, and locally stored configuration.

## Notes
- The code uses _conn.Api! after verifying IsAuthenticated; if Api can be null in edge cases, this could throw at runtime. Consider guarding Api against nulls or enforcing a stronger invariant that IsAuthenticated implies a non-null Api.
- Avatar upload failures are isolated: an exception will be logged and a user-visible error shown, but the rest of the profile updates (name, bio, colors, notifications) still apply.


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


Stages a batch of local files (multi-file paste or drag-and-drop) as attachments for the next message. It executes synchronously on the UI thread to ensure the staged attachment list is updated before the message is sent, preventing races with the routing path. If the client is not authenticated or connected, it returns immediately. It adds only up to the remaining attachment slots, reports an error if more files were provided than can be staged, and then refreshes the staging tray.

## Remarks
This method centralizes the per-message attachment staging logic, tying the UI state (`_stagedAttachments`) to the maximum allowed attachments (`HubConstants.MaxAttachmentsPerMessage`). It guarantees a deterministic UI update by running on the UI thread and by refreshing the staging tray after updates, reducing the chance of inconsistent attachment state when users paste many files quickly.

## Notes
- The channel parameter is currently unused by this method.
- If files.Count exceeds the available slots, only the portion that fits is staged; the user is shown an error message and the extras are ignored.

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


Handles an image pasted from the clipboard by staging it as a temporary PNG in a per-paste folder so it flows through the same path-based staging and encryption pipeline as regular attachments. The temporary file is deleted once the message is sent.

If the client is not authenticated or connected, the method returns early without attempting to stage anything.

If the attachment limit for the current message has been reached, it shows an error and returns.

It uses a unique, per-paste folder under the system temporary path (EchoHub/pasted/{8-char}) to preserve the familiar image.png name while letting multiple pasted images coexist in a single message. It writes the PNG as image.png, tracks the path in the internal _tempPastedFiles and _stagedAttachments collections, and refreshes the staging tray UI.

If anything goes wrong, it logs the exception and informs the user via the main window.

## Remarks

This symbol acts as a glue between clipboard paste handling and the attachment pipeline, reducing ad-hoc clipboard writes and ensuring consistent processing downstream. By isolating each paste, it avoids file-name clashes and keeps UI behavior predictable (the image shows as image.png, even when multiple pastes exist). It relies on the existing authentication/connection state and the global attachment limit to maintain a robust UX.

## Notes

- No operation occurs if the user is not authenticated or not connected; the method returns quietly in that case.
- Temporary files are not cleaned up by this method; cleanup occurs later in the message lifecycle when the message is sent.
- Pasting images enforces HubConstants.MaxAttachmentsPerMessage; once reached, a user-visible error is shown and gating prevents further staging.


---

### HandleLoadMoreRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleLoadMoreRequested()
```

**Returns:** `void`


Loads the next page of chat history for the currently selected channel when the user requests more messages. It executes asynchronously and prepends the retrieved history to the channel's message list, typically in response to a 'Load more' action in the chat UI.

## Remarks

Separates concerns by consolidating the load-more behavior in this method, acting as the bridge between the connection layer, the currently selected channel, and the in-memory history store. It uses RunAsync to perform the network call off the UI thread and InvokeUI to apply the updated history safely to the UI. The per-channel guard (_channelsLoadingMore) prevents overlapping loads for the same channel, preserving responsiveness when users tap repeatedly.

## Notes

- Exits early if the client is not connected or no channel is selected, avoiding unnecessary work.
- The fetch uses HubConstants.DefaultHistoryCount as the batch size and computes the offset from the current message count; a finally block ensures the per-channel loading flag is cleared even if the fetch fails, and RunAsync surfaces a 'Failed to load more messages' error to the user.

---

### HandleLogout
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleLogout()
```

**Returns:** `void`


Orchestrates the end-to-end logout sequence for the client: it logs the logout, persists pending read state, and initiates an asynchronous server logout. It then conditionally clears the saved token based on the API base URL, performs cleanup, and updates the UI to show a disconnected state.

## Remarks
This method centralizes the logout workflow in AppOrchestrator, coordinating server communication, local state cleanup, and UI transitions. It delegates UI changes to InvokeUI to guarantee thread-safety and relies on RunAsync to provide contextual error handling with the 'Logout error' title.

## Notes
- ClearSavedToken(baseUrl) is invoked only when baseUrl is non-null, preventing token removal in contexts without a configured API.
- UI updates are scheduled on the main thread via InvokeUI to avoid cross-thread interaction issues.
- If LogoutAsync fails, RunAsync provides the contextual error information ('Logout error'); subsequent steps inside the lambda are contingent on successful logout.

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


Handles the submission of a user-typed message for a specific channel. It validates the connection and routes the content through command handling, staged attachments, or a plain text send, while updating the UI with errors or system messages as appropriate.

## Remarks

This method serves as the central orchestrator for message submission, coordinating the UI layer, command processor, and network transport. It enforces preconditions (an active connection and an unlocked room) and uses asynchronous execution to avoid blocking the UI thread, ensuring proper state transitions such as clearing a pending reply after a successful send.

## Notes

- Not connected: shows an error and aborts before performing any network activity.
- Command handling: commands are detected via IsCommand; when HandleAsync returns a result with a non-null Message, the UI is updated either with an error (if IsError) or a system message.
- Message sending paths: if there are staged attachments, a staged message is sent with the caption and attachments; otherwise, if there is a per-channel pending reply, the message is sent with a replyTo ID; in all sending paths, EnsureRoomUnlockedForSendAsync is awaited; after a pending-reply send, ClearPendingReply is invoked on the UI thread.

---

### HandleProfileRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleProfileRequested()
```

**Returns:** `void`


Handles the internal action when a profile is requested by the user. It delegates directly to HandleViewProfile(null), which triggers the shared profile-view workflow for the current user without requiring a specific user identifier.

## Remarks
By routing through this private method, the code preserves a clean separation between the act of requesting a profile and the details of how a profile is displayed. It keeps the event-name binding lightweight while centralizing the actual view logic in HandleViewProfile. If future requirements ever need to view a non-current profile, this wrapper can be adapted or extended to pass a concrete identifier without broadening the public API.

## Notes
- The wrapper relies on the null argument to signal 'current user' to the profile viewer; changing the contract of HandleViewProfile would require updating this method.
- Because the method is private, tests must exercise the public paths that lead to this method rather than invoking it directly.

---

### HandleReplyCancelRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleReplyCancelRequested() => ClearPendingReply()
```

**Returns:** `void`


This private method handles a cancel request for a pending reply by clearing any in-progress reply state. It delegates to ClearPendingReply to perform the reset, providing a semantically meaningful hook within the orchestrator's reply lifecycle.

## Remarks
By naming this hook, the code communicates intent: cancellation of a pending reply is a distinct moment in the flow, and all necessary cleanup should be centralized here. This makes future changes to the cancellation behavior easier to implement without touching multiple call sites.

## Notes
- This method is private; external callers should trigger cancellation through public channels that eventually surface this path.
- It is a synchronous wrapper around ClearPendingReply; no asynchronous operations are performed in this method.

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


Handles a user action to reply to a specific message. It first checks for an active channel from the main window; if none exists, it returns without altering any UI state. When a channel is present, it records the target message and channel as the pending reply, shortens the provided snippet to a concise preview (40 characters max, with an ellipsis added if truncation occurs), and updates the UI to show who is being replied to along with that preview.

## Remarks
Handles the internal UX state for message replies by coupling the selected message (messageId) with its channel and exposing a concise preview to the user. This keeps the reply context consistent across subsequent actions without requiring the higher-level UI to recompute the target context on every interaction. It also prevents initiating a reply when there is no active channel.

## Notes
- Early return when there is no current channel; the method exits without updating pending state or UI.
- Snippet truncated to 40 characters with a trailing ellipsis (the character '…') when longer, ensuring a compact in-UI preview.
- This method directly manipulates UI state via _mainWindow; callers should ensure execution on the appropriate UI thread to avoid cross-thread exceptions.

---

### HandleSavedServersRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleSavedServersRequested()
```

**Returns:** `void`


Shows the list of saved servers to the user by reading from the configuration and presenting it in a modal dialog. If there are no saved servers, it informs the user that none exist.

## Remarks
This method centralizes the presentation logic for saved servers, decoupling data retrieval from display concerns. It relies on the configuration's SavedServers collection and uses a straightforward LINQ projection to build human-readable lines, including a session indicator when a refresh token is present and the last connected date. The formatting rules (username fallback to ? and the optional [session saved] tag) are encapsulated here, making future changes localized to this UI path.

## Notes
- The function renders a modal dialog via MessageBox.Query, which blocks until the user dismisses it; callers should ensure this is invoked on the UI thread.
- The last connected date is formatted as yyyy-MM-dd, a culture-invariant representation; if LastConnected can be default or null, this may warrant data validation upstream.
- Username defaults to "?" when missing, and a server is labeled with "[session saved]" only if RefreshToken is non-empty, tying display state to authentication/session data.

---

### HandleStatusRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleStatusRequested()
```

**Returns:** `void`


Handles a user-initiated request to change the current status. It presents StatusDialog to gather a new status and optional message, updates the local session with the chosen values, and, if a connection is active, persists the change asynchronously via the connection.

## Remarks
This method is the bridge between the UI dialog and the rest of the application's state. It encapsulates the common pattern of mutating local session state plus a conditional remote update, so callers don't need to duplicate the sequence. If the user cancels the dialog (StatusDialog.Show returns null), no state is changed. The remote update runs asynchronously and uses a dedicated error message, allowing the UI to remain responsive and errors to be surfaced in a uniform way.

## Notes
- The local session state is updated regardless of connectivity; the remote update only runs when _conn.IsConnected.
- If the user cancels the dialog, indicated by a null result, the method returns without modifying state.
- This method is private; callers should trigger it through the UI flow rather than invoking it directly.

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


Orchestrates the end-to-end theme switch when the user selects a theme. It loads the chosen theme via ThemeManager.GetTheme, applies it with ThemeManager.ApplyTheme, persists the selection by saving the updated config through ConfigManager.Save, and refreshes the UI to reflect the change.

## Remarks
Centralizes the end-to-end theme-switch workflow for the app: the same sequence is used whenever a theme changes, ensuring runtime state, persisted configuration, and the visible UI stay in sync. It coordinates ThemeManager, ConfigManager, and the main window (via InvokeUI) to apply color schemes and trigger a UI redraw in a thread-safe manner. Keeping this logic in one private method reduces duplication and makes future theme-related behavior easier to evolve.

## Notes
- No exception handling is shown in this snippet; exceptions from GetTheme, ApplyTheme, or Save may propagate unless handled by the caller.
- Assumes _config and _mainWindow are initialized before invocation; otherwise a null reference may occur.

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


HandleViewProfile orchestrates the retrieval and presentation of a user’s profile. It determines whether the requested username represents the current user, fetches the profile asynchronously when authenticated, and routes the UI flow to either editing the own profile or viewing another user’s profile.

## Remarks
This method centralizes the profile-view UX flow, coordinating session state, authentication status, data access, and the dialog-driven UI. By dispatching work to a background task and marshaling UI updates back to the main thread, it keeps the caller responsive while ensuring consistent dialog behavior for own vs. other profiles. The outcome of ShowOwn drives subsequent actions (EditProfile or SetStatus) via the ProfileAction return value, keeping subsequent logic cohesive within this symbol.

## Notes
- The profile fetch only happens if authentication is present; target may be the current user or another user, and a non-empty target is required for the API call.
- All exceptions during data retrieval are caught and surfaced via ShowError on the UI thread; the flow gracefully aborts the view if loading fails.
- UI updates are performed on the UI thread using InvokeUI to avoid cross-thread issues.

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


InvokeUI is a private helper that forwards an Action to the application's UI thread by calling _app.Invoke(action). It centralizes UI-thread marshaling within AppOrchestrator so UI updates scheduled from background or orchestrator code go through a single path, ensuring consistent threading semantics.

## Remarks

Centers the UI-thread marshaling logic in a single place, reducing boilerplate and the risk of inconsistent marshalling across callers. Because the method is private, its usage is limited to internal orchestration and can be changed without impacting public APIs. The actual dispatch timing depends on the implementation of _app.Invoke; if non-blocking behavior is required, prefer a non-blocking path (such as a BeginInvoke variant) when available.

## Notes

- The underlying _app.Invoke dictates whether the call blocks; this method does not introduce a new asynchrony model by itself.
- Avoid long-running work inside the Action passed to InvokeUI to prevent UI thread stalling; offload heavy work to background threads.

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


Joins a channel, automatically handling password prompts when the server requires authentication. If the channel is end-to-end encrypted, the user’s passphrase never leaves the client; a PBKDF2-derived authentication key is used to unwrap the room key locally, and the method returns the channel history. If the user cancels the password prompt, it returns null.

## Remarks
This method centralizes the join-with-password flow, including encryption metadata handling, local key management, and UI coordination, so callers do not need to re-implement retry logic. It coordinates with the connection's room-key store to cache and unwrap encryption envelopes; when a fresh envelope is obtained, it unwraps it locally (using the derived key, if available) and fetches history so decryption uses the latest key. The password prompt is dispatched to the UI via InvokeUI and the result is awaited, ensuring a responsive user experience even on the UI thread.

## Notes
- Cancellation returns null; callers should treat this as a failed join.
- Wrong passwords trigger a re-prompt loop via ChannelPasswordRequiredException handling.
- If crypto metadata cannot be retrieved, the join proceeds with best-effort encryption state and logs the incident for diagnostics.

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


Determines whether the orchestrator should prompt the user to unlock the room key for a given end-to-end encrypted channel. It returns true only when the channel is encrypted, has no cached key, and the user has not declined the unlock prompt during this session. If the channel isn't encrypted or a key is already cached, no prompt is needed.

## Remarks

By centralizing this decision, the code avoids spuriously prompting for unlocks. It leverages the RoomKeys service to inspect encryption status and key caching, and it uses a per-session declined-set to remember user choices, preventing repeated prompts for the same channel within a session. This method acts as a guard that the UI can consult before triggering any unlock UI.

## Notes

- The check to read _declinedUnlocks is performed under a lock to ensure thread-safe access to the collection.

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


Normalizes common ASCII size descriptors into a canonical one-letter code used internally. It trims whitespace, lowercases the input, and maps 's'/'small' to 's', 'm'/'medium' to 'm', and 'l'/'large' to 'l'. Anything else (including null or unknown values) yields null, leaving it to the caller to decide how to proceed.

## Remarks
Centralizes normalization logic for size inputs, ensuring consistent downstream handling wherever a compact size code is required. Since it returns null for unknown inputs, callers must guard against nulls or provide a fallback. The method is pure (no side effects) and deterministic given its input.

## Notes
- Returns null for any value that isn't a recognized descriptor, including null or whitespace.
- Whitespace is trimmed and case-insensitive matching is applied, so variations like ' Small ' or 'S' are treated the same.

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


Consolidates the execution of an asynchronous operation within the application context by routing it through AsyncRunner.Run. It passes the current application context, the work to perform, and the UI-facing error handler, along with an error prefix and optional log context. Use RunAsync to ensure uniform error reporting and logging for asynchronous tasks without duplicating the wiring at every call site.

## Remarks
RunAsync is a tiny abstraction that centralizes cross-cutting concerns around asynchronous work: error handling, user feedback through the main window, and optional diagnostic logging. It decouples callers from the AsyncRunner wiring, so changes to error handling or the logging strategy can be made in one place without touching every invocation.

## Notes
- Private accessibility means it can only be used within the containing class; callers should use higher-level methods that eventually invoke this helper.
- logContext is optional; omit it if there is no additional logging context, but providing a descriptive value improves traceability in logs and diagnostics.

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


Sends one message with the given caption plus all staged files as attachments, then clears the staging tray. In encrypted channels each file is room-encrypted (blob + ASCII preview) client-side before upload; the caption is room-encrypted too.

## Remarks
This method serves as the orchestration point for sending staged content. It encapsulates the encryption decision (via RoomKeys and RoomCrypto), attachment construction (via OutgoingAttachment), and the lifecycle management of the staging tray and temporary pasted files. By coordinating several collaborators, it provides a single, reliable path to publish a caption and its attachments while preserving staging integrity and user experience even in edge cases (e.g., locked rooms or upload failures).

## Notes
- If the channel is locked, the send is blocked and the files remain staged for after the unlock. The operation exits early in this case.
- The content and attachments are encrypted only if a room key is available; otherwise they are sent in plaintext.
- Temporary pasted files are cleaned up in a finally block, and the staging tray is refreshed regardless of success or failure to prevent orphaned UI artifacts.


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


UnlockTrackedChannelAsync unlocks an end-to-end encrypted channel that is already joined as part of the hub. It re-joins the channel to obtain the WrappedRoomKey envelope, unwraps the key, and, if a key exists in the local store, loads decrypted history into the UI before signaling that the channel is unlocked. The method returns true on a successful unlock and false if the envelope is missing, the key cannot be unwrapped, or an error occurs during the flow.

## Remarks
This method centralizes the unlock sequence for channels the client is tracking, hiding the details of rejoining, envelope handling, and UI synchronization behind a single, reusable path. It relies on the connection's RoomKeys store to verify that a key has been unwrapped and on the UI dispatcher (InvokeUI) to surface decrypted history when available, ensuring the unlock outcome is reflected in the user interface in a thread-safe manner.

## Notes
- The function returns false if the channel does not present a WrappedRoomKey (i.e., not an E2E channel) or if the key cannot be established, providing a clear early-out behavior for non-E2E or cancelled unlocks.
- All exceptions are caught and logged with a warning, preventing an unhandled exception from propagating to callers.
- History is loaded into the UI only when a non-null history payload is produced; otherwise, unlocking still succeeds but there is nothing to display.

---

### WireCommandHandlerEvents
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void WireCommandHandlerEvents()
```

**Returns:** `void`


WireCommandHandlerEvents subscribes the AppOrchestrator to a broad suite of events exposed by the command handler. This centralized wiring ensures that whenever the command handler raises events such as OnSetStatus, OnJoinChannel, or OnExportData, the corresponding local handlers (HandleCmdSetStatus, HandleCmdJoinChannel, HandleCmdExportData, etc.) are invoked. Use this method during initialization to establish the one-to-one event-to-handler mappings instead of scattering subscriptions throughout startup code.

## Remarks

This method encapsulates the event-binding choreography between the command layer and the orchestrator. It provides a single, discoverable place where command-related concerns are wired, which helps keep the AppOrchestrator focused on reacting to high-level user commands rather than wiring subsystems. Because the wiring is performed in one place, tests can swap a mock _commandHandler or verify that specific events are connected to their expected handlers. If the set of command events changes, updating this method is the one authoritative location.

## Notes

- If _commandHandler is not initialized before calling this method, a NullReferenceException will occur when subscribing to events. Ensure proper initialization order during construction/startup.
- Renaming or removing events requires updating this wiring method to maintain coverage.

---

### WireConnectionManagerEvents
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void WireConnectionManagerEvents()
```

**Returns:** `void`


## Source Code
Wires up the ConnectionManager's event handlers to drive the client UI for messages, joins/leaves, and presence updates. It subscribes to MessageReceived to display incoming messages and to notify the user when they are mentioned; it handles UserJoined and UserLeft to keep the per-channel online user list in sync and to refresh the UI when the current channel is active; and it reacts to UserStatusChanged to propagate presence updates across all channels and to update the cached channel user lists. The updates are marshaled onto the UI thread via InvokeUI, and thread-safety around the shared _channelUsers collection is maintained with a lock to avoid race conditions when users join, leave, or change status.

## Remarks
This method centralizes all event wiring for the connection manager, isolating networking concerns from UI/presentation logic. It coordinates message display, system messages for joins/leaves, and presence updates in a single place, which simplifies reasoning about real-time behavior and makes testing easier. The design favors immediate UI feedback for the active channel while ensuring the in-memory presence cache stays consistent across channels.

## Notes
- The code uses a lock (_channelUsersLock) to guard updates to the _channelUsers dictionary; avoid performing long-running work inside the locked region to prevent UI thread blocking.
- Notification sounds depend on a non-empty session username and on the message content mentioning that username; if either is missing, the sound is not played.
- Presence updates are propagated to all channels for status visibility, and the code path optimizes updates for the current channel by taking a snapshot when applicable.

## Dependencies
- UserPresenceDto
- Content
- StringComparison
- Username
- Status
- UserStatus

## Dependency APIs (verified signatures)
- record [`UserPresenceDto`](../EchoHub.Core/DTOs/ProfileDtos.cs.md) (`src/EchoHub.Core/DTOs/ProfileDtos.cs`)
- property `Content` (`src/EchoHub.Core/Models/Message.cs`)
- property `Username` (`src/EchoHub.Client/Config/ClientConfig.cs`)
- property `Status` (`src/EchoHub.Client/Services/UserSession.cs`)
- enum [`UserStatus`](../EchoHub.Core/Models/UserStatus.cs.md) (`src/EchoHub.Core/Models/UserStatus.cs`)

## Symbol To Document
- Name: WireConnectionManagerEvents
- Kind: method
- File: src/EchoHub.Client/AppOrchestrator.cs
- Language: csharp
- ID: 8c768c24-e19f-4c25-b611-7ab4a805aa68

---

### WireMainWindowEvents
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void WireMainWindowEvents()
```

**Returns:** `void`


Wires the MainWindow's UI events to the orchestrator's handlers by subscribing to a broad set of On...Requested events exposed by _mainWindow. This centralized bootstrap method maps user interactions—such as connecting, disconnecting, logging out, submitting messages, staging files, pasting images, selecting channels, requesting profiles or status, changing themes, managing servers and channels, audio playback, file download/save, image operations, deleting messages, checking for updates, rolling back, viewing user profiles, joining channels from messages, searching, loading more results, and replying to messages—to their corresponding handler methods. Call this during initialization to ensure UI actions are routed into the application's business logic.

## Remarks
Centralizes event wiring and decouples the UI from concrete business logic by acting as the single source of truth for how UI actions map to handlers. It makes the orchestrator's responsibilities explicit and simplifies testing and future changes, since all UI-to-logic subscriptions are declared in one place.

## Notes
- Invoking WireMainWindowEvents more than once will attach duplicate handlers, causing each event to invoke handlers multiple times; ensure this is called once or guard against re-subscription.
- If _mainWindow is not initialized before calling this method, a NullReferenceException can be thrown during subscription; ensure proper initialization order.


---

## HandleCmdOpenProfile
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


This private command handler serves as the command-path bridge to open a user's profile: it dispatches a UI action to display the profile for the provided username and returns a completed Task. It doesn't navigate directly; InvokeUI marshals the call to the UI thread and delegates to HandleViewProfile for the actual rendering. The username parameter is nullable and is passed through to the underlying handler.

## Remarks
This separation of concerns keeps command execution lightweight while centralizing UI-thread marshaling. By delegating to HandleViewProfile, the actual profile rendering logic remains centralized, promoting consistent navigation behavior across commands that open profiles.

## Notes
- Returns immediately with Task.CompletedTask; the UI action runs asynchronously on the UI thread.
- The nullable username means callers should ensure compatibility with HandleViewProfile's expectations, or provide a fallback when null.

---

## HandleSearchRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleSearchRequested()
```

**Returns:** `void`


Handles the search-driven user flow by presenting a dialog of channel names and dispatching the selected result to navigation or action handlers. If a channel is chosen, it switches to that channel and triggers additional channel-selection logic; if an action is chosen, it delegates to the corresponding handler (connect, disconnect, logout, profile, status, create-channel, delete-channel, saved-servers, toggle-users, updates, or quit). When the dialog is canceled, the method exits without side effects.

## Remarks

By acting as a centralized dispatcher, this method separates the UI interaction (SearchDialog.Show) from the concrete consequences of each choice. It delegates work to the AppOrchestrator's specialized handlers, ensuring consistent behavior for search-driven commands and simplifying future extension of supported actions. If new search result types or actions are introduced, this entry point will need parallel updates to the switch branches and handlers to maintain correctness.

## Notes

- The method only handles Channel and Action result types; additional types will be ignored unless handled here.
- Action keys are plain strings; adding new actions requires updating both the inner switch and the corresponding handler methods.
- Null results are treated as cancellation and result in an early return.

---

## PromptPassword
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


Prompts the user for a password via a modal dialog and returns the entered value, or null if the user cancels or submits an empty value. Use this helper when you need a blocking, masked password input in a Terminal.Gui-based UI, rather than duplicating dialog scaffolding in multiple places.

## Remarks
This method encapsulates a small, reusable UI flow using Terminal.Gui components: a dialog with a label that shows the provided prompt, a masked text field (Secret = true), and Confirm/Cancel buttons. It returns the raw password string entered by the user, or null when the user cancels or omits input. The prompt text is injected into the label, allowing reuse with different messages without changing layout. The interaction relies on _app.Run(dialog) to present the modal and _app.RequestStop() to close it once the user makes a choice.

## Notes
- The password value is kept in memory as a string until the method returns; avoid logging or persisting it in plaintext.
- Whitespace-only input is not treated as empty by this implementation; if your validation requires trimming, perform it after receiving the result.
- Because this is a private helper, callers should provide an appropriate prompt message to convey the expected credential.


---

## RefreshStagingTray
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void RefreshStagingTray()
```

**Returns:** `void`


RefreshStagingTray updates the UI to reflect the current set of staged attachments by extracting their file names and passing them to the main window alongside a size label derived from the configuration. It is a UI helper invoked after modifications to the staging area to keep the display in sync with the underlying data.

## Remarks
Serves as a UI adapter between the staging model and the presenter, encapsulating how the staged attachments are presented to the user. By using Path.GetFileName, it shows only the file names, avoiding full paths in the interface, and delegates formatting of the size indicator to AsciiSizeLabel(_config.DefaultAsciiSize). This separation simplifies updating presentation details without altering the staging logic.

## Notes
- Null-reference risk if _mainWindow or _config is not initialized before this method runs.

---

## UnlockRoomKeyAsync
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


UnlockRoomKeyAsync drives the interactive unlock flow for an encrypted channel that lacks a cached room key (for example, on a new device). It prompts the user for a passphrase until the room key can be unwrapped and stored, at which point it loads and returns the channel history; if the user cancels, it records the decline and returns the existing history without unlocking.

## Remarks

This helper encapsulates the user interaction required to unlock messages for a specific channel, separating the UI-driven password prompt and key-derivation loop from the rest of the connection logic. It uses a per-channel decline cache to avoid nagging after a user declines to unlock, and it clears that cache on a successful unwrap to resume normal history loading.

## Notes

- Early exit: if outcome.EncryptionSalt or outcome.WrappedRoomKey are null, the method returns outcome.History immediately.
- Asynchronous prompt: the passphrase prompt is dispatched to the UI and awaited without blocking the caller; the prompt is wired via a TaskCompletionSource and ChannelPasswordDialog.Show.
- Unlock success vs cancel: on success, the derived key is stored and the method returns the channel history; on cancel, the channel is added to _declinedUnlocks and history is returned unchanged.

---