# AppOrchestrator.cs

> **Source:** `src/EchoHub.Client/AppOrchestrator.cs`

## Contents

- [AppOrchestrator](#apporchestrator)
- [AppOrchestrator (constructor)](#apporchestrator-constructor)
- [MainWindow](#mainwindow)
- [ClearSavedToken](#clearsavedtoken)
- [Dispose](#dispose)
- [FetchAndUpdateOnlineUsers](#fetchandupdateonlineusers)
- [HandleAudioPlayRequested](#handleaudioplayrequested)
- [HandleChannelJoinFromMessage](#handlechanneljoinfrommessage)
- [HandleChannelSelected](#handlechannelselected)
- [HandleCheckForUpdatesRequested](#handlecheckforupdatesrequested)
- [HandleCmdAssignRole](#handlecmdassignrole)
- [HandleCmdBanUser](#handlecmdbanuser)
- [HandleCmdJoinChannel](#handlecmdjoinchannel)
- [HandleCmdKickUser](#handlecmdkickuser)
- [HandleCmdLeaveChannel](#handlecmdleavechannel)
- [HandleCmdListUsers](#handlecmdlistusers)
- [HandleCmdMuteUser](#handlecmdmuteuser)
- [HandleCmdNukeChannel](#handlecmdnukechannel)
- [HandleCmdOpenProfile](#handlecmdopenprofile)
- [HandleCmdOpenServers](#handlecmdopenservers)
- [HandleCmdQuit](#handlecmdquit)
- [HandleCmdSendFile](#handlecmdsendfile)
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
- [HandleDeleteChannelRequested](#handledeletechannelrequested)
- [HandleDisconnect](#handledisconnect)
- [HandleEditProfile](#handleeditprofile)
- [HandleFileDownloadRequested](#handlefiledownloadrequested)
- [HandleLoadMoreRequested](#handleloadmorerequested)
- [HandleLogout](#handlelogout)
- [HandleMessageSubmitted](#handlemessagesubmitted)
- [HandleProfileRequested](#handleprofilerequested)
- [HandleRollbackRequested](#handlerollbackrequested)
- [HandleSavedServersRequested](#handlesavedserversrequested)
- [HandleSearchRequested](#handlesearchrequested)
- [HandleStatusRequested](#handlestatusrequested)
- [HandleThemeSelected](#handlethemeselected)
- [HandleViewProfile](#handleviewprofile)
- [InvokeUI](#invokeui)
- [RunAsync](#runasync)
- [SaveServerToConfig](#saveservertoconfig)
- [WireCommandHandlerEvents](#wirecommandhandlerevents)
- [WireConnectionManagerEvents](#wireconnectionmanagerevents)
- [WireMainWindowEvents](#wiremainwindowevents)
- [SafeOpenExtensions](#safeopenextensions)

---

## AppOrchestrator
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** class

```csharp
public sealed class AppOrchestrator : IDisposable
```


Coordinates the TUI client's UI, command handling and connection lifecycle. AppOrchestrator wires MainWindow and connection events to the underlying services (chat/message manager, connection manager, audio and notification services), exposes the main window instance to callers, and provides helpers for running asynchronous work and marshaling updates back to the UI thread.

## Remarks
This class is the single integration point that keeps UI components and background services in sync for the EchoHub TUI. It maintains an in-memory, case-insensitive cache of per-channel user presence lists and a set of channels currently loading more users, and it centralizes asynchronous execution and error handling via RunAsync while ensuring UI updates happen through InvokeUI. It also implements IDisposable to own and release long-lived resources such as the connection manager and playback/notification services.

## Notes
- UI updates must be performed via InvokeUI to ensure they run on the application's UI thread.
- Use RunAsync when invoking potentially-failing or long-running asynchronous work so the orchestrator can handle errors and logging consistently.
- Channel user lists are stored with a case-insensitive key (StringComparer.OrdinalIgnoreCase); callers should treat channel names accordingly.

---

## AppOrchestrator (constructor)
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


Bootstraps the EchoHub client by composing its core services and UI elements. On construction, it stores the application and configuration, creates the ChatMessageManager and MainWindow, initializes the CommandHandler, NotificationSoundService, and UpdateChecker, wires up all essential event handlers, kicks off the background update check, and sets the initial UI status to Disconnected.

## Remarks
AppOrchestrator serves as the composition root for the client, coordinating the lifecycles of its major collaborators and the flow of events between the UI and backend services. Centralizing startup logic in this constructor makes the sequence predictable, testable, and resilient to partial failures where the UI remains in a known Disconnected state until a connection is established.

## Notes
- The update check starts in the background during construction and does not block startup.
- Fresh instances of the key collaborators (ChatMessageManager, MainWindow, CommandHandler, NotificationSoundService, UpdateChecker) are created here, which defines their lifetimes for the client session.
- The constructor triggers immediate background work (UpdateChecker.Start); ensure downstream consumers are thread-safe and prepared for asynchronous progress reporting via UpdateChecker hooks.

---

## MainWindow
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** property

```csharp
public MainWindow MainWindow => _mainWindow
```


This read-only property returns the private _mainWindow field, exposing the current MainWindow instance to external callers without permitting reassignment. Use this property when a component outside the orchestrator needs to interact with the main window, rather than accessing private fields or duplicating retrieval logic.

## Remarks
By encapsulating the reference behind a property, the design preserves encapsulation while offering a stable access point. It communicates ownership and lifecycle: the orchestrator creates and manages the MainWindow, and callers should rely on this accessor rather than attempting to replace or recreate the window.

## Notes
- UI thread affinity: ensure calls into MainWindow happen on the UI thread to avoid cross-thread exceptions.
- Coupling: exposing the concrete MainWindow type increases coupling; consider introducing an abstraction if you plan to substitute or test the window more easily.

---

## ClearSavedToken
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


Clears the saved refresh token for the server identified by the provided URL. It loads the application configuration, searches the SavedServers collection for a server whose Url matches serverUrl (case-insensitive), and if a matching entry exists, sets its RefreshToken to null, persists the updated configuration, and updates the in-memory _config reference to reflect the change.

## Remarks
This helper centralizes the credential-clearing logic so callers don't manipulate the config directly. It targets only the specified server and preserves the server entry itself, enabling a clean logout or token invalidation workflow without removing server metadata. By persisting the change and updating the in-memory copy, subsequent operations see a consistently invalidated token state.

## Notes
- No-op when no matching server is found.
- Only clears the RefreshToken; the server entry remains in the configuration.
- Not inherently thread-safe; concurrent invocations may race on config load/save.

---

## Dispose
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
public void Dispose()
```

**Returns:** `void`


Dispose synchronously releases the resources held by the AppOrchestrator: it synchronously completes the asynchronous disposal of the underlying connection and then disposes the update service. Use this method when you’re done with the orchestrator to ensure the connection is closed and update-related resources are cleaned up (typically via a using block or when implementing IDisposable).

## Remarks
Dispose bridges asynchronous resource lifetime with the synchronous IDisposable contract. The disposal order matters: the connection is disposed first, then the update service, ensuring no dependent resource outlives its provider. Because it blocks on an async operation using GetAwaiter().GetResult(), callers in a context that can't pump continuations (such as a UI thread) risk deadlocks; prefer async disposal (DisposeAsync) if available in async paths. This method assumes its internal fields are non-null and initialized before use.

## Notes
- Deadlock risk due to blocking on async disposal in certain synchronization contexts.
- No null-checks; potential NullReferenceException if _conn or _updateService are null.
- Partial disposal on failure; if disposing the first resource throws, the second is not disposed.

---

## FetchAndUpdateOnlineUsers
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void FetchAndUpdateOnlineUsers()
```

**Returns:** `void`


FetchAndUpdateOnlineUsers is a private helper that refreshes the online user list for the channel currently active on the main window. It returns early if there is no channel or the connection is not established; otherwise, it launches a background task to retrieve the latest online users, cache them under the active channel, and update the UI to reflect the changes.

## Remarks
FetchAndUpdateOnlineUsers centralizes the network I/O needed to populate the online-user view for the active channel and keeps the UI responsive by performing work on a background thread. It protects the shared channel-user cache with a lock during the update and funnels any transient failures into a debug log rather than propagating errors to callers. This pattern provides a safe, isolated update path that can be invoked without blocking the UI.

## Notes
- Fire-and-forget behavior: the operation is kicked off with Task.Run and is not awaited by the caller; exceptions are swallowed into a debug log rather than thrown to the caller.
- Channel capture nuance: the channel value used for the fetch is captured at invocation time; if the user navigates to a different channel before completion, the fetched results will be applied to the originally captured channel.


---

## HandleAudioPlayRequested
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


Handles the user action to play an audio attachment: it first checks authentication, downloads the attachment to a temporary file, and then opens the audio playback dialog with the downloaded file.

## Remarks
Centralizes the end-to-end playback flow for attachments, separating concerns among authentication, file retrieval, and UI presentation. The RunAsync wrapper runs the download and dialog-launch asynchronously, preserving UI responsiveness and providing a clear failure message if the operation cannot complete.

## Example
```csharp
// Typical usage to trigger playback for an attachment
HandleAudioPlayRequested("https://example.com/file.mp3", "file.mp3");
```

## Notes
- If the user is not authenticated, this method returns early and performs no UI updates.
- The code dereferences _conn.Api with a null-forgiving operator (Api!). If the API client is not initialized, a NullReferenceException will be thrown at runtime.

---

## HandleChannelJoinFromMessage
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


Documentation submitted for symbol HandleChannelJoinFromMessage. The narrative covers the purpose, when to use, and important considerations for null input and behavior when disconnected.

---

## HandleChannelSelected
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


HandleChannelSelected is the channel-switch workflow invoked when a user selects a channel. It first exits early if the client is not connected, ensuring no work is attempted without an active connection. When connected, it offloads the work to a background flow (RunAsync) to avoid blocking the UI: if the channel should be tracked, it joins the channel, then attempts to fetch the channel history and render it in the UI via LoadHistory on the UI thread. History retrieval is optional and any failure to obtain history is swallowed to keep the channel switch responsive. Finally, it refreshes the list of online users.

## Remarks
This method centralizes the channel-activation sequence used by the UI: validate connection, ensure channel participation, surface historical context when available, and refresh presence. By performing I/O-heavy operations asynchronously and updating the UI through a dedicated invocation path, it keeps the user experience smooth while maintaining a consistent channel context across components (_conn, _messageManager, and the UI layer).

## Notes
- History retrieval failures are swallowed without user-visible error messaging, which can hide intermittent availability issues.
- There is no explicit validation of channelName (e.g., null or empty checks) within this method; callers should ensure a valid channel identifier is supplied.


---

## HandleCheckForUpdatesRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleCheckForUpdatesRequested()
```

**Returns:** `void`


Triggers an update check in response to a request, delegating the work to the UpdateService via an asynchronous wrapper. It passes UpdateService.CheckNowAsync to RunAsync along with the error message "Failed to check for updates" so that failures are reported consistently without blocking the UI.

## Remarks
This is a private orchestration helper: it decouples the UI/event layer from the concrete update workflow by funneling the operation through RunAsync. The pattern centralizes asynchronous execution and error reporting, ensuring a consistent user experience when an update check fails and reducing boilerplate across call sites that initiate update checks.

## Notes
- If CheckNowAsync changes its signature or requires parameters, this delegate wiring may need adjustment to remain compatible with RunAsync.


---

## HandleCmdAssignRole
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


This is a private asynchronous command handler that processes a request to assign a role to a user. It guards execution behind an authenticated connection, maps a textual role argument to a ServerRole enum, and then invokes the authenticated API to perform the assignment.

HandleCmdAssignRole first checks that the connection is authenticated; if not, it exits without performing any action. It then translates the incoming role string using a concise switch expression: 'admin' becomes ServerRole.Admin, 'mod' becomes ServerRole.Mod, and any other input defaults to ServerRole.Member. Finally, it awaits the API call to AssignRoleAsync on the authenticated connection's API to apply the role to the specified user.

## Remarks
Centralizes the role-assignment logic behind a single command handler, keeping command parsing distinct from the underlying API call and ensuring consistent role semantics across the client. The default to Member for unrecognized inputs offers a forgiving behavior, but it can mask input errors—explicit validation may be desirable if strict input handling is required. The use of the null-forgiving operator on Api reflects an assumption that the API is non-null once authenticated; however, reaching this point with a null Api would trigger a runtime exception, so callers should ensure authentication flow guarantees Api availability prior to invocation.

## Notes
- Early exit if not authenticated; no side effects occur.
- Unrecognized role strings default to Member; consider explicit validation to avoid silent permission assignments.
- No error handling around AssignRoleAsync within this method; exceptions will bubble up to the caller. 

---

## HandleCmdBanUser
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


Bans a user by delegating to the underlying API, but only after confirming the current connection is authenticated. If not authenticated, the command exits without performing any action. When authenticated, it awaits BanUserAsync on the API with the specified username and optional reason.

## Remarks
By gating the ban operation behind an authentication check, this method enforces that only authenticated sessions can issue bans. It also isolates the orchestration logic from the API surface, making it easier to test and reason about the ban workflow.

## Notes
- No exception handling is performed inside the method; exceptions from BanUserAsync propagate to the caller.
- Uses the null-forgiving operator on Api (Api!), so ensure that Api is non-null when IsAuthenticated is true to avoid a NullReferenceException.

---

## HandleCmdJoinChannel
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdJoinChannel(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `Task`


Handles the user command to join a chat channel by coordinating the network join with UI state updates. It first checks that the client is connected, then awaits the channel join and, on success, ensures the channel appears in the UI, switches to it, and loads any returned history.

## Remarks
This method acts as the bridge between the networking layer and the UI, ensuring channel state and history are reconciled before presenting the channel to the user. By marshaling UI updates onto the UI thread via InvokeUI, it maintains thread-safety and keeps the user experience responsive during asynchronous operations. The approach centralizes the join-channel workflow so callers do not need to duplicate the sequence of network join, UI list maintenance, channel switching, and history loading.

## Notes
- If the connection is not established, the method returns immediately with no user feedback, which can be surprising from a UX perspective. Consider surfacing a disconnected state prior to invoking this handler.
- The catch block surfaces a generic error message containing ex.Message; for production code you may want more user-friendly messaging and proper logging for diagnostics.
- History loading occurs only when history.Count > 0, avoiding unnecessary UI work if no history is returned.

---

## HandleCmdKickUser
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


It handles a user-kick command by issuing a server-side kick for the specified username, but only when the current connection is authenticated. It guards against unauthenticated usage by returning early if the connection is not authenticated; when authenticated, it awaits the KickUserAsync call on the server API, forwarding both the username and the optional reason.

## Remarks
By encapsulating the authentication check and the remote invocation, this helper centralizes the command-handling contract for kicking users. It separates the command-handling flow from the server-enforcement details, relying on the surrounding connection object to determine eligibility and on the server API to perform the action.

## Notes
- If the connection is not authenticated, the method returns without performing any action, which means callers may need to provide user feedback about why the command had no effect.
- The use of the null-forgiving operator on the API reference (_conn.Api!) assumes Api is non-null when IsAuthenticated is true; if Api is unexpectedly null, a runtime NullReferenceException could occur. Ensure proper initialization and invariant maintenance between authentication state and API availability.

---

## HandleCmdLeaveChannel
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdLeaveChannel()
```

**Returns:** `Task`


Handles the user action to leave the currently selected chat channel. It validates the connection and the target channel, prevents leaving the default channel, and then performs the leave operation asynchronously, reporting success or failure back to the UI.

## Remarks
At runtime this method coordinates the orchestration between the connection layer (_conn) and the UI layer (_mainWindow, _messageManager). It enforces a business rule that the default channel cannot be left (so attempts show an error) and marshals UI updates onto the main thread via InvokeUI. The asynchronous call to LeaveChannelAsync ensures the UI remains responsive even if network I/O is slow or failing.

## Notes
- Returns early when not connected or when there is no current channel, avoiding any network activity.
- Prevents leaving the DefaultChannel by showing a UI error message instead of performing the operation.
- Catches all exceptions from LeaveChannelAsync and surfaces a user-friendly error via the main window.

---

## HandleCmdListUsers
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdListUsers()
```

**Returns:** `Task`


Handles the command to list online users for the currently selected channel. It immediately returns if there is no active connection or channel, then fetches the list of online users asynchronously via GetOnlineUsersAsync and updates the UI by marshaling the updates onto the UI thread with InvokeUI. It prints a header that reads "Online users in #<channel>:" and then renders one line per user showing a DisplayName (or Username as fallback) and their status, including an optional StatusMessage. If an error occurs during retrieval, it surfaces an error message in the main window.

## Remarks
This method acts as a focused orchestrator for the "list users" flow: it validates preconditions, retrieves data, and renders the results in the UI in a single cohesive operation. It relies on the Status property of each user (and optional StatusMessage) to produce a readable, human-facing line for each participant. By performing UI updates through InvokeUI, it ensures thread-safety and keeps UI concerns isolated from the networking logic.

## Notes
- UI updates are marshaled to the UI thread via InvokeUI; omitting this could lead to cross-thread exceptions.
- DisplayName is used when available; if both DisplayName and Username are null, the rendered name may be null, so data integrity for user objects is important.
- Exceptions are surfaced to the user with ex.Message through ShowError; for production scenarios, consider additional logging or more user-friendly messaging.

---

## HandleCmdMuteUser
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


Handles the mute-user command by validating authentication and delegating to the server via the API. If the connection is not authenticated, it returns immediately; otherwise it calls MuteUserAsync with the target username and an optional duration, awaiting the operation.

## Remarks
Centralizes the mute operation behind the command handler, enforcing authentication at the boundary between the command pipeline and network calls. It maintains a thin wrapper over the API, relying on the API client to perform the actual mutation and on the surrounding infrastructure to surface results. It also presumes that Api is non-null whenever IsAuthenticated is true; if that invariant is violated, a runtime exception may occur.

## Notes
- Api may be null even after IsAuthenticated; the code uses a null-forgiving operator, which will throw at runtime if Api is null.
- Exceptions from MuteUserAsync are not caught here; callers should handle moderation failures.
- When not authenticated, the method returns without signaling to the caller; this path is a silent no-op.

---

## HandleCmdNukeChannel
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdNukeChannel()
```

**Returns:** `Task`


This private async method handles the "nuke channel" command by validating the current context and delegating the operation to the backend API. It only proceeds when the user is authenticated and a channel is selected, at which point it calls NukeChannelAsync with that channel.

## Remarks
It serves as a thin orchestration boundary that enforces security and context guards before issuing a destructive action, bridging the UI state with the server API. Centralizing these checks here keeps the command surface consistent and makes the NukeChannel operation easier to test in isolation. If your UX requires explicit confirmation, it should be added at a higher level since this method trusts preconditions but does not itself prompt.

## Notes
- Potential runtime null reference: Api is accessed with null-forgiving operator; ensure Api is initialized when IsAuthenticated is true.
- No internal exception handling: any exception from NukeChannelAsync will propagate to the caller; consider wrapping with error handling if you need user-friendly feedback.

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


This private helper schedules the opening of a user's profile by marshaling the call to the UI thread. It wraps a call to HandleViewProfile(username) inside InvokeUI and returns a completed Task immediately. This pattern lets command logic expose an async signature while performing the actual UI work synchronously on the UI thread.

## Remarks

This abstraction isolates cross-thread invocation from the caller, enabling clean command handlers that don't need to know about UI threading details. It also preserves asynchronous API shape by returning a Task, though the Task completes before the profile view is rendered.

## Example

```csharp
// Example usage: trigger opening Alice's profile without awaiting completion.
var _ = HandleCmdOpenProfile("alice");
```

## Notes

- The Task is completed immediately; exceptions thrown during UI invocation occur on the UI thread and won't be observable via awaiting this Task.
- The username parameter is nullable; ensure HandleViewProfile can handle nulls gracefully.
- If you need to wait for the profile view to finish rendering, this method alone won't provide that guarantee; consider a callback or event mechanism from the UI layer.

---

## HandleCmdOpenServers
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdOpenServers()
```

**Returns:** `Task`


HandleCmdOpenServers is a private command-handler that delegates the UI action to open or display the saved servers list by invoking the HandleSavedServersRequested callback through a UI-marshalling helper, then immediately completes by returning Task.CompletedTask. It signals command handling completion right away while the actual UI work is carried out asynchronously on the UI thread via the provided callback.

## Remarks
This method acts as a thin bridge between the command layer and the UI layer. By routing the work through a centralized InvokeUI helper, it keeps thread-affinity concerns confined to the UI subsystem and preserves responsiveness in the command path. The implementation also decouples the command name from the specifics of how the saved-servers UI is presented, allowing the UI presentation to change (via HandleSavedServersRequested) without altering the command method.

## Notes
- The method returns a completed Task and does not await the UI operation, so callers should not rely on this method to signal the completion of the UI action.
- Any exceptions arising from the UI callback (HandleSavedServersRequested) occur on the UI thread and are not surfaced through the returned Task; handle such errors within the UI callback or its downstream code.
- The symbol is private to its containing class; external code cannot call it directly.


---

## HandleCmdQuit
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private Task HandleCmdQuit()
```

**Returns:** `Task`


Handles the quit command by marshaling a stop request to the UI thread; the stop is performed by invoking _app.RequestStop() on the UI thread, while this method itself returns a completed Task immediately, allowing the caller to continue without waiting for the shutdown to finish.

## Remarks
This symbol centralizes termination behavior within the AppOrchestrator and preserves the UI-thread affinity required for stopping the application. By delegating to the UI thread, it avoids performing shutdown logic on the caller's thread and keeps lifecycle concerns localized to the orchestrator.

## Notes
- The returned Task is completed immediately; it does not represent shutdown completion.
- Multiple quit invocations may enqueue multiple stop requests; rely on the underlying stop mechanism to handle duplicates or race conditions.
- If the UI thread cannot be invoked (e.g., during a shutdown sequence), the stop request might not be posted.

---

## HandleCmdSendFile
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdSendFile(string target, string? size)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `target` | `string` | — |
| `size` | `string?` | — |

**Returns:** `Task`


Handles the command to send a file by routing either a remote URL or a local file to the current channel. It first validates that the user is authenticated, the connection is active, and a channel is selected. If the target resolves to an absolute HTTP/HTTPS URL, it delegates to the API to send the URL; otherwise it opens the target as a file, streams its contents, and uploads the file along with its name. All exceptions are caught, logged with the target context, and surfaced to the user via the UI.

## Remarks
This method centralizes two related file-sending workflows behind a single command path: URL-based sending and local-file uploading. It relies on the connection layer's API contract (_conn.Api) and the current channel context to perform the operation, and it centralizes error handling to avoid propagating exceptions to the user interface.

## Notes
- The operation is a best-effort guard: it returns early if authentication or connectivity prerequisites are not met, or if no channel is selected, avoiding unnecessary work.
- Only absolute HTTP/HTTPS URLs are treated as remote targets; other schemes fall back to local-file handling.
- When sending a local file, the file is opened as a stream and disposed via await using, ensuring proper resource management.
- Exceptions are logged with the target context and a user-facing error is shown using ex.Message; consider sanitizing error details for end-user exposure in production.

---

## HandleCmdSetAvatar
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


Handles the Set Avatar command by uploading the specified avatar for the authenticated user. It exits early if the connection is not authenticated. When authenticated, it delegates to AvatarHelper.UploadAsync using the current API client. On success, if a channel is active, it posts a system message "Avatar updated." to that channel. If an exception occurs, it logs the error and surfaces a user-visible error message.

## Remarks
Serves as a small orchestration boundary that coordinates authentication, API interaction, and UI feedback for avatar changes. By centralizing the upload call, success notification, and error handling in one place, it reduces duplication across command handlers and keeps the command flow consistent with other user actions.

## Notes
- The code assumes _conn.IsAuthenticated implies a non-null _conn.Api; using the null-forgiving operator means a NullReferenceException could occur if Api is unexpectedly null.
- Exceptions are surfaced to the user via a UI error message including the exception message; consider safer messaging in production.
- If _mainWindow.CurrentChannel is null or empty, the success system message is not posted; the avatar upload still succeeds on the server side.

---

## HandleCmdSetColor
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


Updates the current user's nickname color as requested by the set-color command, but only when the client is authenticated. It constructs an UpdateProfileRequest with NicknameColor set to the provided color and invokes UpdateProfileAsync via the API client.

## Remarks
Encapsulates the color-change side effect of the command by first enforcing authentication, then persisting the chosen color to the backend. This keeps command handling focused on input routing while delegating persistence to the API layer. It relies on the API client being non-null after authentication and treats API failures as exceptions that flow back to the caller.

## Notes
- No client-side validation of the color format is performed here; invalid values may produce an API error.
- If IsAuthenticated is false, the method returns silently with no user-facing feedback from this method.
- The Api property is accessed with a null-forgiving operator; ensure authentication guarantees Api is initialized to avoid runtime NullReferenceException.

---

## HandleCmdSetNick
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


Handles setting the local nickname by persisting the new display name to the server and then updating the UI to reflect the change. If the connection is not authenticated, the command is ignored; otherwise it updates the profile via UpdateProfileAsync and, on success, updates the UI with the new display name and marks the status as Connected.

## Remarks
As a small coordination helper, this method isolates the cross-cutting concerns of authentication, server-side state update, and UI refresh. The API call updates the server-side profile, while the UI updates ensure the nickname is visible immediately. The InvokeUI call ensures UI changes happen on the correct thread.

## Notes
- The code uses the null-forgiving operator on _conn.Api; ensure Api is non-null when IsAuthenticated is true, or else a null-reference exception may occur.
- There is no local error handling around UpdateProfileAsync; failures will bubble to the caller. Consider wrapping in try/catch and surfacing a user-facing error or rollback in the UI.

---

## HandleCmdSetStatus
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdSetStatus(UserStatus status, string? message)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `status` | [`UserStatus`](../EchoHub.Core/Models/UserStatus.cs.md) | — |
| `message` | `string?` | — |

**Returns:** `Task`


Implements the internal command that sets the user's status. If the client is connected, it forwards the new status and optional message to the server via UpdateStatusAsync and then mirrors those values in the local session; if not connected, it returns immediately without making any changes. This method is a small, private helper used by command handlers to propagate status changes consistently across server and client state.

## Remarks
This abstraction centralizes the status-change path for command-driven state updates. It ensures that once a status is updated remotely, the local session model reflects the same values, preventing drift between UI/state and server state. The private access modifier communicates that status changes are an internal concern of the orchestrator's command-handling flow rather than a broad public API.

## Notes
- No error handling within the method; exceptions from UpdateStatusAsync will propagate to the caller.
- It short-circuits when the connection is not established, so callers should either ensure connectivity or tolerate a no-op.
- It mutates both _conn (remote state) and _session (local state); callers should be mindful of potential re-entrancy concerns if invoking concurrently.

---

## HandleCmdSetTheme
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


Marshals the request to set a theme to the UI thread by invoking HandleThemeSelected(name) via InvokeUI. It takes the theme name and delegates the actual theme application to the UI thread, then returns Task.CompletedTask to satisfy an asynchronous caller.

## Remarks
UI thread affinity is preserved by this wrapper; this keeps command handling agnostic of threading concerns while ensuring UI updates occur on the correct thread. It also separates the orchestration layer from the theme-application logic, making the command path simpler and more testable.

## Notes
- The returned Task is completed immediately; the actual theme change runs asynchronously on the UI thread.
- Exceptions thrown by HandleThemeSelected on the UI thread will not be observed via the returned Task; ensure appropriate UI error handling.

---

## HandleCmdSetTopic
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


Handles the user-issued command to set the topic of the currently selected channel. It first verifies the user is authenticated and a channel is selected, then calls the backend API to update the topic and updates the UI to reflect the change, including a system message that confirms the new topic. If the update fails, it surfaces an error message to the user.

## Remarks
Encapsulates the orchestration between authentication state, backend API calls, and UI updates. It performs the API call and subsequent UI changes on the UI thread via InvokeUI, ensuring a responsive and consistent user experience when commands succeed or fail. As a private command handler, it is intended to be invoked by the command-processing path rather than called directly by external code.

## Notes
- Silent no-op when not authenticated or when no channel is selected (no user-visible feedback in these paths).
- Relies on _conn.Api being non-null; uses the null-forgiving operator (_conn.Api!). If Api is null, a NullReferenceException could occur.
- Exceptions from UpdateChannelTopicAsync are caught and reported to the user via an error message; the displayed text comes from ex.Message.

---

## HandleCmdTestSound
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private async Task HandleCmdTestSound()
```

**Returns:** `Task`


Handles the test sound command by calling the notification sound service to play a test sound and awaiting its completion. It delegates to _notificationSound.PlayTestAsync(), acting as a small command handler within AppOrchestrator that enables verification of in-app notification audio without duplicating playback logic.

## Remarks
Acts as a thin wrapper around the notification sound service to handle a 'test sound' command. It keeps command handling separate from the playback implementation, enabling swap of the sound provider without touching call sites.

## Notes
- No exception handling is present in this method; any exceptions from PlayTestAsync bubble up to the caller, so callers should handle errors as appropriate.

---

## HandleCmdUnbanUser
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


Handles the unban-user command by invoking the API to unban the specified username, but only when the client is authenticated; otherwise it returns immediately.

## Remarks
By centralizing the unban operation behind an authentication check, this method prevents unauthenticated code paths from triggering server-side changes. It delegates the actual unban to the API client (_conn.Api), keeping AppOrchestrator focused on command routing and lifecycle concerns. The use of Api! signals the expectation that Api is non-null after successful authentication; if Api is null, a runtime exception will occur. Exceptions from UnbanUserAsync propagate to the caller, allowing higher-level error handling to present user-friendly feedback.

## Notes
- Early return occurs when the connection is not authenticated, ensuring no unban is attempted.
- The code assumes _conn.Api is non-null when authenticated; if Api is unexpectedly null, a NullReferenceException may be thrown at runtime.
- No internal error handling is performed here; any exceptions from UnbanUserAsync bubble up to the caller for handling.

---

## HandleCmdUnmuteUser
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


This private asynchronous method handles the unmute-user command within the client orchestrator. It first checks whether the current connection is authenticated; if not, it returns without performing any action. When authenticated, it calls the backend API to unmute the specified username by invoking UnmuteUserAsync on the Api object.

## Remarks
This method serves as a gatekeeper between command handling and the backend service, ensuring that unmute actions occur only in authenticated sessions. It relies on the Api being non-null at the moment of invocation (as hinted by the null-forgiving operator) and does not perform local validation of the username. Errors thrown by the API call will propagate to the caller, since there is no internal error handling here.

## Notes
- No local try-catch is present; exceptions from UnmuteUserAsync will bubble up to the caller.
- The Api instance is accessed via a null-forgiving operator; ensure _conn.Api is initialized whenever IsAuthenticated is true.

---

## HandleConnect
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleConnect()
```

**Returns:** `void`


Orchestrates the user-initiated connection workflow for the client. When invoked (typically in response to the user selecting Connect), it will gracefully disconnect the current session if the user confirms, present the connect dialog to collect server and user details, and then perform the asynchronous connect operation. Upon success it updates the UI with the logged-in user, loads channels, switches to the default channel, loads history when available, focuses the input, and refreshes the online user list; it also persists the chosen server to configuration. If a saved session token has expired, it clears the token, informs the user, and resets the UI to a disconnected state so the user can re-authenticate.

## Remarks
HandleConnect is the UI-facing orchestrator of the connect flow. It hides the complexity of token management, channel setup, and history loading behind a single event handler, coordinating between the dialog, the asynchronous connection, and the MainWindow updates. By centralizing this logic, it ensures a consistent post-connect state across first-time logins and reconnections, and it acts as the boundary where user-driven connection decisions translate into concrete UI and session changes.

## Notes
- Cancelling the ConnectDialog (dialogResult is null) results in no action.
- If a saved refresh token exists but has expired or been revoked, the code logs a warning, clears the saved token, shows a "Session Expired" prompt, and disconnects to require re-authentication.
- All UI updates (status bar, current user, channels, focus, history load, online users) are marshaled to the UI thread via InvokeUI, ensuring thread safety during the asynchronous connect operation.
- After a successful connect, the chosen server is persisted to configuration via SaveServerToConfig.


---

## HandleCreateChannelRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleCreateChannelRequested()
```

**Returns:** `void`


Handles the user-initiated request to create a new channel. It first verifies that the client is authenticated and connected, showing a non-blocking error if not; then it prompts the user for channel details via CreateChannelDialog and, if the user provides input, creates the channel on the server, joins it to fetch history, and updates the UI to reflect the new channel, its topic, and any loaded history.

---

## HandleDeleteChannelRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleDeleteChannelRequested()
```

**Returns:** `void`


HandleDeleteChannelRequested orchestrates the client-side deletion flow for a channel requested by the user. It enforces authentication and connectivity, blocks deletion of the default channel, prompts for confirmation, then calls the server API to delete the channel, unregisters it locally, and updates the UI to reflect the deletion and switch back to the default channel with a system message.

## Remarks
The method acts as a coordinator between server operations, application state, and the user interface. It consolidates precondition checks (authenticated and connected, a channel selected, and the channel not being the default) and a user confirmation dialog, ensuring deletions are intentional and consistent. By performing the API call and local state updates inside RunAsync with UI updates marshaled via InvokeUI, it keeps the user experience responsive while maintaining thread-safety.

## Notes
- If _conn.Api is unexpectedly null after authentication, the null-forgiving usage (_conn.Api!) would cause a runtime exception when calling DeleteChannelAsync.
- Deleting the default channel is explicitly blocked to preserve a sane UI state and prevent orphaned references.
- The actual UI updates (removing the channel, switching to the default channel, and posting a system message) are performed on the UI thread via InvokeUI to avoid cross-thread access issues.

---

## HandleDisconnect
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleDisconnect()
```

**Returns:** `void`


HandleDisconnect encapsulates the client-side disconnect sequence. It logs the intent to disconnect, clears the in-memory channel state under a dedicated lock to avoid race conditions, and then schedules asynchronous cleanup followed by UI updates to reflect the disconnected state.

The method starts by emitting an informational log entry, then acquires _channelUsersLock to clear the _channelUsers collection in a thread-safe manner. It proceeds to RunAsync, performing an asynchronous cleanup via _conn.CleanupAsync(). Upon completion, it marshals UI work back onto the UI thread to reset the main window and set the status to "Disconnected". The RunAsync invocation is labeled with "Disconnect error" and "Disconnect", indicating the operation's context for error reporting and diagnostics.

This routine centralizes the disconnection flow so callers can trigger a full teardown without duplicating cleanup steps, ensuring both connection resources and UI state are consistently reset.


---

## HandleEditProfile
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


HandleEditProfile orchestrates the in-app profile-editing flow. It presents a ProfileEditDialog pre-populated with the current profile values and the current notification settings; if the user submits changes, it runs a background task that updates the server profile, refreshes the UI when the display name changes, optionally uploads a new avatar, applies notification preference changes, and persists the new default profile to disk.

## Remarks
Centralizes the profile-editing sequence across UI, API communication, avatar handling, and configuration persistence. It guards server calls with an authentication check and marshals UI updates onto the UI thread. Avatar uploads are best-effort: failures are logged and surfaced to the user, but do not short-circuit the rest of the update flow.

## Notes
- Avatar upload errors are caught and shown, allowing other updates to proceed.
- DisplayName changes trigger a UI refresh and status update; null values are handled gracefully to support partial updates.
- Config persistence happens unconditionally after all updates by calling ConfigManager.Save(_config).


---

## HandleFileDownloadRequested
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


Handles a user-initiated request to download an attachment by streaming it to a temporary file and, if the file type is deemed safe, opening it with the system default application. If the user is not authenticated, the method exits early; otherwise it provides progress messages and manages post-download behavior and error handling.

## Remarks
Encapsulates the end-to-end file download UX: it updates the chat channel with a 'Downloading {fileName}...' message, downloads to a temp path, and either launches the file or reports the location to the user. The logic for auto-opening relies on SafeOpenExtensions to distinguish benign from potentially risky files, delegating to the OS via Process.Start with UseShellExecute. This centralization reduces duplication and ensures consistent user feedback and error handling across file-download scenarios in AppOrchestrator.

## Notes
- Exits early when not authenticated; nothing happens if _conn.IsAuthenticated is false. 
- Api is accessed with the null-forgiving operator; a non-null Api is expected after authentication, so misconfigurations could lead to an exception if Api is unexpectedly null. 
- Auto-opening of safe extensions uses the system shell; this may fail in restricted environments or due to user permissions; failures are logged and the downloaded path is shown to the user.

---

## HandleLoadMoreRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleLoadMoreRequested()
```

**Returns:** `void`


Loads more chat history for the currently selected channel by performing an asynchronous server request when the user requests more messages; it validates the connection and the selected channel, prevents concurrent loads per channel, computes the offset from the currently loaded messages, fetches the next page using that offset, and prepends the retrieved history to the channel's UI history.

## Remarks
The per-channel loading guard (_channelsLoadingMore) ensures only one in-flight history fetch runs for a channel at a time, avoiding race conditions and duplicate UI updates. The method delegates the actual data fetch to the connection layer and updates the UI on the main thread via InvokeUI, keeping network and UI concerns loosely coupled. The offset-based paging relies on GetMessages(channel) to reflect the current history state and HubConstants.DefaultHistoryCount to define the page size.

## Notes
- Returns early if the connection isn't established or no channel is selected, or if a load is already in progress for the channel.
- Always clears the loading flag in a finally block, even if the server call fails.
- Uses the error message "Failed to load more messages" for the RunAsync wrapper to surface to the user.

## Dependencies
- HubConstants

## Dependency APIs (verified signatures)

- class [`HubConstants`](../EchoHub.Core/Constants/HubConstants.cs.md) (`src/EchoHub.Core/Constants/HubConstants.cs`)
  - field `string ChatHubPath`
  - field `string DefaultChannel`
  - field `int DefaultHistoryCount`
  - field `int MaxMessageLength`
  - field `int MaxImageSizeBytes`
  - field `int MaxAudioFileSizeBytes`
  - field `int MaxFileSizeBytes`
  - field `int MaxAvatarSizeBytes`
  - field `int MaxMessageNewlines`
  - field `int MaxConsecutiveNewlines`
  - field `int AsciiArtWidth`
  - field `int AsciiArtHeight`
  - …and 5 more member(s)

---

## HandleLogout
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleLogout()
```

**Returns:** `void`


The HandleLogout method encapsulates the end-to-end logout workflow: it logs a notice that the client is logging out, then runs an asynchronous sequence that performs server logout, conditionally clears any saved token based on the presence of a base URL, performs cleanup, and finally updates the UI to reflect a disconnected state. This method is the explicit hook developers use to trigger a full logout without blocking the UI, delegating the orchestration to a background task while ensuring the UI is refreshed afterward.

## Remarks
Consolidates the logout lifecycle in a private helper so network interaction, token management, and UI state transitions stay cohesive and maintainable. It coordinates with the connection object to terminate the server session, clears local credentials when a base URL is known, and ensures the application presents a consistent disconnected state by clearing the main window and updating the status bar on the UI thread. The pattern of using RunAsync for the operation and InvokeUI for UI updates helps keep the UI responsive and minimizes race conditions between network activity and presentation.

## Notes
- ClearSavedToken(baseUrl) only executes if baseUrl is non-null; tokens tied to other contexts may remain untouched.
- UI state updates are performed on the UI thread via InvokeUI, which prevents cross-thread access issues when clearing UI components and updating the status.


---

## HandleMessageSubmitted
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


Processes a submitted chat message for a channel, ensuring the client is connected and either handling a command or sending the message to the server asynchronously. It surfaces error or status messages to the UI accordingly.

## Remarks
Centralizes message submission logic, coordinating connectivity state, command handling, and UI feedback. It marshals UI updates to the main thread via InvokeUI and runs potentially long operations asynchronously to keep the UI responsive.

## Notes
- This method assumes _conn and _commandHandler are non-null; a null reference would throw before the connectivity check.
- When content is identified as a command, the method delegates to the command handler and, if a non-null Message is returned, either shows an error (if IsError is true) or adds a system message to the channel.
- For non-command content, the message is sent asynchronously via _conn.SendMessageAsync, with a RunAsync wrapper that surfaces a "Send failed" error if the operation fails.

---

## HandleProfileRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleProfileRequested()
```

**Returns:** `void`


Handles the ProfileRequested event by delegating to the shared profile-view path with a null target. This private helper translates the request into a call to HandleViewProfile(null), ensuring the default/current profile view is shown without duplicating view logic.

## Remarks
By routing the default profile through a single method, the class keeps profile viewing behavior consistent and easier to modify. The private nature signals it is an internal implementation detail rather than part of the public API, used only by the orchestration code when the profile is requested without a specific target.

---

## HandleRollbackRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleRollbackRequested()
```

**Returns:** `void`


HandleRollbackRequested orchestrates the rollback flow for a post-update restore. It validates the existence of a backup, prompts the user to confirm restoration to the backed-up version (the application will restart), and, on confirmation, delegates the restore to UpdateBackupService, which terminates the process; any error is logged and shown to the user.

## Remarks
This method centralizes the human-in-the-loop rollback path by coordinating backup validation, user confirmation, and the restoration step. It relies on UpdateBackupService to perform the actual restoration and to terminate the application, emphasizing that a rollback is a destructive, all-or-nothing operation that should be invoked from a carefully guarded UI flow. Exceptions are caught and surfaced to the user while being logged for diagnostics.

## Notes
- If no backup exists, the user is shown a "No Backup" message and the operation returns early.
- The confirmation message displays the version from backup info; if info is null, the version is shown as "unknown".
- If the user cancels the confirmation, the method returns without performing a restore.
- RestoreBackup is expected to terminate the application (via Environment.Exit(0)); callers should not rely on code paths following this call.

---

## HandleSavedServersRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleSavedServersRequested()
```

**Returns:** `void`


This private UI handler reads the application's saved servers from configuration and presents them to the user in a dialog. If there are no saved servers, it informs the user with a brief message; otherwise it formats each saved server into a concise line showing its name, URL, username (or a '?' placeholder if missing), the last connected date (yyyy-MM-dd), and an indicator when a refresh token is present ([session saved]). It then displays the assembled lines in a single message box.

## Remarks
This symbol encapsulates the presentation logic for saved servers, centralizing how stored data becomes human-readable text. It isolates UI formatting from the storage structure, so changes to the SavedServers collection or to how sessions are marked do not require changes elsewhere in the UI flow. It relies on MessageBox to render the information, ensuring a consistent user experience when users inspect their saved servers.

## Notes
- Username may be null; the code substitutes a '?' to keep the line readable.
- A non-empty RefreshToken triggers the [session saved] tag, indicating a persisted session for that server.
- The method is private; it is intended to be invoked by user actions within AppOrchestrator and is not part of the public API.

---

## HandleSearchRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleSearchRequested()
```

**Returns:** `void`


HandleSearchRequested processes the user's selection from the search dialog by dispatching it to either a channel switch or an action handler. Use this method as the single point of interpretation for search results, instead of duplicating routing logic elsewhere in the UI.

## Remarks
By centralizing the routing of SearchDialog results, this method decouples the UI trigger (SearchDialog) from the concrete behaviors (SwitchToChannel, HandleConnect, HandleProfileRequested, etc.). It coordinates with the App and MainWindow to perform navigation, server-related operations, and lifecycle actions (such as quitting or toggling panels) in a consistent manner. The action-key mapping is explicit; adding new actions requires updating this switch block and the corresponding handler methods listed in dependencies.

## Notes
- Unrecognized action keys are ignored because there is no default branch for the Action case. Extend the mapping if new actions are introduced.
- The "quit" branch triggers _app.RequestStop(), which can terminate the application outside of normal shutdown flows.
- The method guards against a null result from the search dialog, ensuring no exceptions occur if the user cancels or closes the dialog.


---

## HandleStatusRequested
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void HandleStatusRequested()
```

**Returns:** `void`


Prompts the user to update the current status by displaying a StatusDialog populated with the current status and message, then applies the chosen values to the in-memory session and, if a connection exists, persists the change to the server asynchronously.

## Remarks
HandleStatusRequested acts as the orchestration point between the UI dialog (StatusDialog) and the client's session and connection layers. It first aligns local state with the user's selection, then conditionally triggers a remote update only when a connection is available. This separation of concerns makes the flow easier to test and resilient to offline scenarios, since UI logic and network persistence are decoupled.

## Notes
- If the dialog result is null, the method returns early with no state change.
- If connected, the status update is performed asynchronously and may fail; the provided message 'Status update failed' helps surface the failure.

---

## HandleThemeSelected
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


When a user selects a theme in the UI, HandleThemeSelected logs the selection, retrieves the corresponding Theme via ThemeManager.GetTheme, and applies it with ThemeManager.ApplyTheme. It then stores the chosen theme name in the client configuration and persists it with ConfigManager.Save, before invoking the UI to refresh and render the new color scheme.

## Remarks

HandleThemeSelected acts as the glue between the theming subsystem, configuration persistence, and the UI. It ensures the new theme is applied immediately and that the choice is saved for future sessions, by updating _config.ActiveTheme and persisting it via ConfigManager.Save. The UI is refreshed through InvokeUI by applying color schemes and triggering a redraw.

## Notes

- No explicit error handling in this snippet; exceptions from ThemeManager.GetTheme, ThemeManager.ApplyTheme, or ConfigManager.Save could propagate to the caller.
- Saves are performed before the UI refresh; if saving fails, the in-memory UI state may reflect a theme change without a persisted setting.

---

## HandleViewProfile
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


Computes whether the requested profile is the current user, loads the UserProfileDto asynchronously when authenticated, and then presents the appropriate profile UI. It serves as the orchestrator that bridges authentication, API access, and UI dialogs for viewing profiles, handling both own-profile edits/status updates and viewing others' profiles.

## Remarks
This function centralizes the profile-view flow, delegating rendering to ProfileViewDialog and reacting to user actions via ProfileAction. By performing the data fetch on a background thread and marshaling UI updates back to the main thread, it keeps the UI responsive while preserving a single, consistent path for profile presentation. It also encapsulates error handling so that load failures surface a user-visible error rather than crashing the application.

## Notes
- When not authenticated or when the target username is empty, the fetch is skipped and the resulting profile may be null; the UI must handle a null profile gracefully.
- API call failures are caught and reported to the user via a UI error message instead of propagating exceptions.
- This is a private method; its call sites are confined to the orchestrator's flow and should not be invoked directly from external components.

---

## InvokeUI
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


Marshals the supplied Action to the application's UI thread by delegating to _app.Invoke. This private helper centralizes thread-affinity concerns for UI updates invoked from non-UI threads, so callers don't have to repeat the underlying invocation pattern.

## Remarks
By abstracting the invocation through this method, the class can change how UI work is dispatched without touching every call site. It also clarifies intent: the Action is intended to run on the UI thread, not in background work. If future changes swap _app.Invoke for BeginInvoke or a different dispatcher, the impact is isolated here. This method plays the role of a small adapter between the orchestration logic and the UI-thread marshaling mechanism, keeping concerns separated.

## Notes
- Be mindful that if this invocation originates from the UI thread, the behavior depends on _app.Invoke's implementation; reentrancy and timing caveats apply depending on the dispatcher semantics.
- Ensure _app is initialized before use to avoid null-reference errors.

---

## RunAsync
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


A private helper that runs an asynchronous unit of work through AsyncRunner, wiring in the application context and UI error reporting. It forwards the provided work delegate, along with the current application instance and the main window's error display callback, to AsyncRunner.Run, along with the supplied errorPrefix and optional logContext. This ensures consistent error presentation and logging for background operations initiated by AppOrchestrator without duplicating boilerplate.

## Remarks
By abstracting to RunAsync, AppOrchestrator centralizes how asynchronous work is executed and surfaced to users. It decouples the orchestration logic from the details of AsyncRunner, enabling changes to error handling or UI integration to be made in one place. The method ties together two collaborators: _app and _mainWindow.ShowError, ensuring that failures are reported in a uniform manner across background tasks.

## Example
```csharp
// Example usage within AppOrchestrator
this.RunAsync(async () =>
{
    // simulate some async work
    await Task.Delay(250);
    // real work would await other async calls here
}, "Background operation failed");
```

## Notes
- This helper is private to AppOrchestrator; external callers cannot invoke it directly. If you need to initiate similar work from outside, expose a public wrapper.
- If you provide a non-null logContext, it will help diagnose the operation in logs; otherwise the context may be omitted. The errorPrefix remains the primary cue in error messages to identify the source of the failure.

---

## SaveServerToConfig
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


Persists the server connection information obtained from the connect dialog into the application configuration. It builds a SavedServer entry from the dialog result, deriving a friendly Name from the host portion of the server URL, and stores the URL, username, and, if RememberMe is selected, the refresh token. It also stamps LastConnected with the current time, then saves the server via ConfigManager and reloads the in-memory configuration to reflect the change. A log entry records the successful connection to the specified URL.

## Remarks
This method is the bridge between the UI flow (ConnectDialogResult) and the persistent server list used by the client. By deriving the display Name from the URL host, it avoids requiring the caller to compute a human-friendly label. Saving the refresh token only when RememberMe is true helps respect user intent while ensuring the app can re-authenticate on subsequent connections. Re-loading the configuration ensures the in-memory representation stays in sync with what was persisted.

## Notes
- URL parsing will throw on invalid server URLs; ensure result.ServerUrl is a valid absolute URL before calling.
- RememberMe true relies on a non-null _conn.Api to provide a RefreshToken; if it is null, accessing RefreshToken may cause an error.

---

## WireCommandHandlerEvents
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void WireCommandHandlerEvents()
```

**Returns:** `void`


Wires up the command-handling surface by subscribing the orchestrator’s local handlers to each event exposed by the command handler. It connects OnSetStatus to HandleCmdSetStatus, OnSetNick to HandleCmdSetNick, OnSetColor to HandleCmdSetColor, OnSetTheme to HandleCmdSetTheme, OnSendFile to HandleCmdSendFile, OnSetAvatar to HandleCmdSetAvatar, OnOpenProfile to HandleCmdOpenProfile, OnOpenServers to HandleCmdOpenServers, OnJoinChannel to HandleCmdJoinChannel, OnLeaveChannel to HandleCmdLeaveChannel, OnSetTopic to HandleCmdSetTopic, OnListUsers to HandleCmdListUsers, OnKickUser to HandleCmdKickUser, OnBanUser to HandleCmdBanUser, OnUnbanUser to HandleCmdUnbanUser, OnMuteUser to HandleCmdMuteUser, OnUnmuteUser to HandleCmdUnmuteUser, OnAssignRole to HandleCmdAssignRole, OnNukeChannel to HandleCmdNukeChannel, OnTestSound to HandleCmdTestSound, OnQuit to HandleCmdQuit.

---

## WireConnectionManagerEvents
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void WireConnectionManagerEvents()
```

**Returns:** `void`


WireConnectionManagerEvents wires the connection manager's live events to the UI and the in-memory presence state. It subscribes to MessageReceived to render incoming messages and play a notification when addressed to the current user; on UserJoined/UserLeft it updates the per-channel user list under a lock and refreshes the online users view for the active channel; and on UserStatusChanged it propagates presence changes to all channels and synchronizes the cached presence across channel lists. This method centralizes real-time wiring so that chat activity and presence are consistently reflected in the UI.

## Remarks
By confining event wiring to this method, the class decouples event handling from UI logic and ensures a single, consistent path for updating the main window and message manager. UI thread marshaling via InvokeUI is used intentionally to keep UI updates responsive, while guarding shared state with a lock around _channelUsers prevents race conditions when multiple events fire concurrently. The creation of a local snapshot for joined/left updates minimizes the time spent holding the lock and avoids unnecessary UI churn.

## Notes
- The notification behavior depends on detecting the current user’s username within message content (string containment with OrdinalIgnoreCase); consider adjusting the detection if message formats change.
- Presence updates are synchronized across all channels, with per-channel lists updated under a lock and then reflected in the UI or refreshed globally as needed.
- If the current channel is not the one receiving a join/leave event, the code defers to a FetchAndUpdateOnlineUsers call to refresh the view for the active channel when appropriate.

---

## WireMainWindowEvents
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** method

```csharp
private void WireMainWindowEvents()
```

**Returns:** `void`


Wires the main window's events to the orchestrator by subscribing each OnXxx event to a corresponding HandleXxx method. This initialization-time wiring ensures that user actions emitted by the UI (such as connect/disconnect requests, message submissions, channel selections, profile requests, status queries, theme changes, and other interactions) are dispatched to the appropriate handling logic.

## Remarks

Centralizes the binding of UI events to application logic, making the flow from user action to handler explicit and maintainable. By naming the event-to-handler mappings directly in this method, it becomes easier to see which user interactions are supported and how they are handled, without scattering subscriptions elsewhere in the class.

## Notes

- Ensure WireMainWindowEvents is called after _mainWindow is created to avoid null references.
- If invoked more than once, subscriptions will accumulate and handlers will run multiple times; consider guarding with a one-time initialization pattern or unsubscribing on disposal.

---

## SafeOpenExtensions
> **File:** `src/EchoHub.Client/AppOrchestrator.cs`  
> **Kind:** field

```csharp
private static readonly HashSet<string> SafeOpenExtensions = new(StringComparer.OrdinalIgnoreCase)
```


SafeOpenExtensions is a private static readonly `HashSet<string>` that enumerates the file extensions considered safe to open with the system default application. Any file extension not in this set should be downloaded rather than opened automatically via UseShellExecute. The set uses StringComparer.OrdinalIgnoreCase to ensure case-insensitive matching across platforms and user input; it lists common video formats (".mp4", ".webm", ".mkv", ".avi", ".mov") and document types (".pdf", ".txt", ".csv", ".json", ".xml").

## Remarks
This abstraction centralizes the open-file policy for the UI and file-downloading logic, preventing scattered string comparisons throughout the codebase. It guarantees O(1) lookups via a HashSet, which is appropriate for a policy check performed per file. The use of StringComparer.OrdinalIgnoreCase ensures consistent behavior regardless of the file extension casing or the platform's filesystem case sensitivity. The comment indicates that only the safe set should be opened via the system default, while all other extensions are intentionally downloaded rather than auto-opened, reducing the risk of inadvertently executing arbitrary content.

## Notes
- Ensure Path.GetExtension is used to feed into SafeOpenExtensions.Contains; mismatches can occur if the extension extraction is inconsistent.
- If you need to extend the policy, update this collection; the static readonly semantics help ensure the policy remains immutable at runtime.

---