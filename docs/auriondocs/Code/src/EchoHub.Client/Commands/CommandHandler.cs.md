# CommandHandler.cs

> **Source:** `src/EchoHub.Client/Commands/CommandHandler.cs`

## Contents

- [CommandHandler](#commandhandler)
  - [HandleAsciiSize](#handleasciisize)
  - [HandleAsync](#handleasync)
  - [HandleAvatar](#handleavatar)
  - [HandleBan](#handleban)
  - [HandleBanner](#handlebanner)
  - [HandleClear](#handleclear)
  - [HandleColor](#handlecolor)
  - [HandleDeleteAccount](#handledeleteaccount)
  - [HandleExport](#handleexport)
  - [HandleHelp](#handlehelp)
  - [HandleInvite](#handleinvite)
  - [HandleKick](#handlekick)
  - [HandleLeave](#handleleave)
  - [HandleMe](#handleme)
  - [HandleMeta](#handlemeta)
  - [HandleMute](#handlemute)
  - [HandleNick](#handlenick)
  - [HandlePasswd](#handlepasswd)
  - [HandleProfile](#handleprofile)
  - [HandleQuit](#handlequit)
  - [HandleRole](#handlerole)
  - [HandleSend](#handlesend)
  - [HandleServers](#handleservers)
  - [HandleStatus](#handlestatus)
  - [HandleTestSound](#handletestsound)
  - [HandleTheme](#handletheme)
  - [HandleTopic](#handletopic)
  - [HandleUnban](#handleunban)
  - [HandleUnmute](#handleunmute)
  - [HandleUsers](#handleusers)
  - [IsCommand](#iscommand)
  - [IsValidHex](#isvalidhex)
  - [StripQuotes](#stripquotes)
  - [StatusUsage](#statususage)
- [CommandResult](#commandresult)
- [HandleDownloadPath](#handledownloadpath)
- [HandleJoin](#handlejoin)
- [HandleNuke](#handlenuke)
- [ParsePathAndSizeFlag](#parsepathandsizeflag)

---

## CommandHandler
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** class

```csharp
public class CommandHandler
```


Parses user-entered slash commands and turns them into asynchronous events the rest of the client can handle. Use this when you want a single place to interpret textual commands (for example from a chat input box) and decouple parsing from the actual side effects; call IsCommand to quickly test whether an input should be sent to the handler.

## Remarks
This class is an input-to-event bridge: it does not itself implement the side effects of commands but exposes one event per supported command (OnSetStatus, OnSendAction, OnCreateInvite, OnExportData, etc.). Handlers subscribe to these events to implement the application behavior. The source-level comments capture important parsing decisions made here — for example, status semantics (null status means keep the current status; null message means keep current message; empty message means clear it) and parsing details such as respecting quoted paths and an optional size flag when sending files. Those parsing responsibilities and the use of events keep command parsing isolated from platform-specific orchestration and UI code.

## Example
```csharp
// subscribe to a couple of command events and pass a slash-command string to the handler
var commands = new CommandHandler();
commands.OnSetStatus += async (status, message) =>
{
    // apply status change (status may be null to mean "keep current")
    await Task.CompletedTask;
};
commands.OnSendAction += async action =>
{
    // present an emote/action in the UI
    await Task.CompletedTask;
};

if (commands.IsCommand("/me waves"))
{
    var result = await commands.HandleAsync("/me waves");
    // inspect result or propagate it to the UI
}
```

## Notes
- The IsCommand check is a simple StartsWith('/'); a bare "/" will be treated as a command by that predicate.
- Events are plain C# events and may be null if nobody has subscribed; callers should expect that raising a command could be a no-op unless subscribers are attached.
- Status handling has specific semantics encoded in comments: the orchestrator resolves null/empty values against session state (null = keep, empty string = clear).

---

### HandleAsciiSize
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleAsciiSize(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


HandleAsciiSize is a private asynchronous helper that processes the ASCII size command. If a consumer is attached via OnSetAsciiSize, it is invoked with the input string trimmed by the caller. A lack of argument follows the convention of opening the size picker, while a non-empty argument such as s, m, l, small, medium, or large is forwarded to the delegate to apply the chosen size. The method always returns a successful CommandResult to indicate the command was handled.

## Remarks
By funneling ASCII-size logic through OnSetAsciiSize, this symbol decouples the command parsing from the actual size-management behavior (UI, persistence, or runtime configuration). The method remains flexible: if no delegate is supplied, the command is effectively a no-op but still reports success, allowing the caller to treat the input as handled. This pattern simplifies testing and enables swapping the size-setting behavior without changing the command-handler code.

## Notes
- Potential NullReferenceException if OnSetAsciiSize is non-null and 'args' is null because args.Trim() is called without a null-check.
- Returning CommandResult(true) means callers should not rely on this method to signal whether a size was actually changed.

---

### HandleAsync
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
public async Task<CommandResult> HandleAsync(string input)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `input` | `string` | — |

**Returns:** `Task<CommandResult>`


HandleAsync is the central dispatcher for user-entered commands in the EchoHub client; it validates the input, parses the command and its arguments, and asynchronously delegates to the appropriate command-specific handler, returning a CommandResult that reflects success or failure. If the input doesn't look like a command it returns a failure immediately, and unknown commands produce a user-friendly error.

## Remarks
This abstraction separates parsing from individual command implementations, normalizing input (lowercasing the command) and trimming whitespace so each handler can focus on its own logic. It uses the leading slash convention (input[1..]) and a switch expression to map to handlers, making it straightforward to add new commands by extending the switch.

## Notes
- The command is case-insensitive due to ToLowerInvariant.
- Unknown commands yield a friendly error that points to /help; non-command inputs yield a simple failure.
- Command behavior is delegated to a family of HandleX methods, keeping parsing concerns isolated from command logic.

---

### HandleAvatar
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleAvatar(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


This private async method handles the /avatar command: it validates the provided argument, trims surrounding quotes, and, if a handler is attached via OnSetAvatar, invokes it with the cleaned target. It returns a CommandResult indicating either a usage error when the argument is missing or a success message once the update completes.

## Remarks
This method isolates command parsing from the avatar update mechanism. By exposing OnSetAvatar as a delegate, the app can supply concrete update behavior (e.g., uploading to a server or updating local state) without coupling the command logic to the implementation. The user-facing feedback is produced after the asynchronous update completes, ensuring the message reflects the outcome of the operation.

## Notes
- If OnSetAvatar is null, the method completes without performing any update, but still returns a success message ('Uploading avatar...').
- Exceptions thrown by OnSetAvatar are not caught within this method; callers should handle faulted tasks if the delegate fails.

---

### HandleBan
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleBan(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


Handles the /ban command by validating input, parsing a required username and an optional reason, and invoking the OnBanUser callback when provided. If no arguments are given, it returns a usage message; otherwise it awaits the ban handler and responds with a banning message for the target user.

## Remarks
HandleBan acts as a small bridge between the command-processing surface and the domain action of banning a user. It encapsulates input parsing and the optional ban callback behind a single method, enabling testability and decoupling from the actual ban implementation. Its behavior is explicitly contingent on the presence of OnBanUser: with it set, the ban logic runs; without it, the method still returns a result, but no action is performed.

## Notes
- The ban action only occurs if OnBanUser is assigned; otherwise, the command completes with a 'Banning ...' message but no side-effect.
- The reason parameter is optional; if omitted, reason is passed as null to OnBanUser, which should handle it accordingly.
- Input parsing uses a single split with a maximum of two parts and trims whitespace, so arguments like "user" and "user  reason" are handled predictably.


---

### HandleBanner
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleBanner(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


Processes the /banner command by validating the provided text, trimming it, and optionally dispatching it to a banner sender. If the input is null, empty, or whitespace, it returns an error CommandResult containing a usage message. If a banner handler is registered via OnSendBanner, it awaits the handler with the trimmed text; finally it returns a successful CommandResult.

## Remarks
Centralizes banner command handling and isolates input validation from the actual rendering logic. By exposing OnSendBanner as a delegate, the system can plug in different banner delivery strategies without changing the command code. Trimming before dispatch ensures consistent formatting and prevents trailing or leading whitespace from affecting the banner.

## Example
```csharp
// Example: valid input
var result = await HandleBanner("Hello!");
// If a subscriber is attached to OnSendBanner, it's invoked with "Hello!" and a success result is returned.

// Example: invalid input (whitespace-only)
var error = await HandleBanner("   ");
```

## Notes
- If OnSendBanner is null, the method completes successfully without attempting to send a banner.
- Leading/trailing whitespace is trimmed before dispatch; inner whitespace is preserved.

---

### HandleClear
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleClear()
```

**Returns:** `Task<CommandResult>`


Describes the internal helper method HandleClear, which clears any attachments staged for a command by invoking the OnClearAttachments delegate if it's configured, awaits its completion, and then returns a CommandResult signaling success with the message "Cleared staged attachments." Because it is private, it is intended to be used internally by the command handling flow rather than as a public API.

## Remarks
The method encapsulates the cleanup step of the command execution, ensuring a single point of behavior for clearing attachments and reporting results. It reduces duplication by handling the conditional delegate invocation and the standardized success result in one place.

## Notes
- If OnClearAttachments is null, the method performs a no-op for the cleanup but still reports success.
- Exceptions raised by OnClearAttachments propagate to the caller; the method does not catch them.

---

### HandleColor
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleColor(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


Handles the /color command by validating and normalizing the input hex color and, when valid, applying it through an optional callback. It accepts a string of arguments, ensures a color was provided, prepends a leading '#' if missing, enforces the #RRGGBB format, and calls OnSetColor(color) if a handler is attached before returning a CommandResult with a user-friendly message.

## Remarks
By decoupling the color application from the parsing logic via OnSetColor, this method remains focused and testable. It normalizes input to a canonical #RRGGBB form and provides clear feedback for missing, malformed, or valid colors, integrating smoothly with the command-handling flow and the CommandResult feedback.

## Notes
- If OnSetColor throws, the exception will propagate to the caller since there is no internal try/catch.
- If OnSetColor is null, no color is actually applied, though a confirmation message is still returned.

---

### HandleDeleteAccount
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleDeleteAccount()
```

**Returns:** `Task<CommandResult>`


HandleDeleteAccount is a private asynchronous helper that orchestrates the delete-account action by optionally invoking a deletion callback and then returning a CommandResult indicating success. If an OnDeleteAccount delegate is supplied, it is awaited; if not, the method completes immediately and signals success.

## Remarks
By relying on the OnDeleteAccount hook, this method decouples the command handling from the actual deletion logic, enabling different delete implementations to be plugged in without changing the caller. It participates in a command-pattern flow where a successful outcome is reported via CommandResult once the delegate (if any) completes. Note that exceptions thrown by the deletion delegate will propagate to the caller; this method does not translate errors into a failure result itself.

## Notes
- Be mindful that if OnDeleteAccount is null, the method returns a successful CommandResult without performing any deletion.
- There is no internal error-handling; exceptions thrown by OnDeleteAccount bubble up to the caller.

---

### HandleExport
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleExport()
```

**Returns:** `Task<CommandResult>`


HandleExport is a private asynchronous helper in CommandHandler that conditionally triggers an export operation and then signals success to the caller. When OnExportData is assigned, the method awaits OnExportData, allowing export logic to be injected without forcing callers to perform the export themselves; if OnExportData is null, the method completes immediately. The resulting CommandResult(true) communicates that the command finished successfully regardless of whether an export was performed, effectively making the export step optional and pluggable within the command handling workflow.

## Remarks
Why this abstraction exists: It decouples the export behavior from the command flow, enabling tests and consumers to provide their own export logic via a delegate. It centralizes the export invocation behind a simple private method, which reduces repetition and makes the command-handling path easier to reason about. The null check ensures no-op behavior when no export is configured, while still preserving asynchronous semantics.

## Notes
- If OnExportData throws, the exception propagates to the caller; HandleExport itself does not catch exceptions.
- Because the method is private, it can only be invoked within CommandHandler, ensuring export behavior is controlled and testable.

---

### HandleHelp
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleHelp()
```

**Returns:** `Task<CommandResult>`


Handles internal help for the client command interface. When invoked, it optionally triggers the OnHelp hook if assigned, then returns a successful CommandResult containing a large multi-line help text that lists available commands, their usage, and usage tips. This method is used by the command-handling flow to present a consistent help experience to users.

## Remarks
By centralizing the help text in this method, the UI can display a single source of truth for command guidance. The OnHelp hook provides a minimal extension point for runtime customization or side effects without duplicating the static content. Because the method is private, it's intended to be invoked by the command-handling flow rather than consumed as part of the public API.

## Notes
- The returned CommandResult is constructed with a success flag and the help message; the signature implies CommandResult(bool, string).
- The help content is embedded as a raw string literal; editing the literal updates every user-visible line of help.
- If OnHelp throws, the exception will propagate as part of the async flow since there is no internal try/catch.
- The content is not localized here; there is no localization mechanism evident in this snippet.

---

### HandleInvite
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleInvite(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


Parses and handles invite-related commands issued to the command handler. It recognizes three forms: /invite list to enumerate invites, /invite revoke <code> to invalidate a specific invite, and /invite [maxUses] [expiresHours] to create a new invite with optional usage limits and expiration. It validates inputs, returns a CommandResult that signals success or error (with a usage message when inputs are invalid), and delegates the actual work to OnListInvites, OnRevokeInvite, or OnCreateInvite when those callbacks are provided.

## Remarks
By centralizing the invite command parsing, this private method insulates the higher-level command flow from the details of argument interpretation and validation. It coordinates with the domain actions via the OnListInvites, OnRevokeInvite, and OnCreateInvite callbacks, enabling a clean separation between parsing logic and invite-management behavior. The returned CommandResult ensures callers observe a uniform success/error contract.

## Notes
- It relies on StringSplitOptions and StringComparison for command parsing and case-insensitive subcommand matching, and it returns helpful usage messages when inputs are malformed.

---

### HandleKick
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleKick(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


Handles the /kick command by validating input, extracting the target username and an optional reason, and delegating the actual kick action to a callback if one is registered. If no arguments are supplied, it immediately returns a CommandResult indicating the proper usage. When arguments exist, it splits them into username and an optional reason (up to two parts) and, if OnKickUser is not null, awaits the callback with the username and reason. It then returns a success CommandResult with the message 'Kicking {username}...'.

## Remarks
Integrates input validation and a pluggable kick action via OnKickUser, acting as a thin mediator between the command parser and the actual moderation logic. It centralizes consistent user feedback and prevents empty or malformed kick commands from proceeding.

## Notes
- If OnKickUser throws, this method does not catch the exception; the exception propagates to the caller.
- When args are valid but OnKickUser is null, the method still returns a success CommandResult reflecting the intent ("Kicking {username}..."). No action is performed in that case within this method.
- The argument parsing uses StringSplitOptions.TrimEntries and splits at most once, honoring the first space as the separator between username and optional reason.

---

### HandleLeave
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleLeave()
```

**Returns:** `Task<CommandResult>`


HandleLeave is an asynchronous helper used by the command-handling flow to perform a channel leave action when provided. If a consumer assigns OnLeaveChannel, it will be awaited before the method completes; if not, the method completes immediately. In either case, it returns a CommandResult representing success.

## Remarks

By delegating the actual leave operation to the OnLeaveChannel delegate, this symbol decouples the command-handling logic from the concrete leave behavior, enabling easier testing and composition. It acts as a thin wrapper around the optional leave action, ensuring a consistent CommandResult is produced for the rest of the pipeline.

## Notes

- If OnLeaveChannel is provided and it throws an exception, that exception propagates to the caller since there is no local error handling.
- The return value is always new CommandResult(true), so success is signaled regardless of whether a leave action existed or completed.
- This method is private; external components should not rely on it directly.

---

### HandleMe
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleMe(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


HandleMe handles the /me command by validating the provided action and, when valid, dispatching it to an OnSendAction callback. If the argument is missing or whitespace, it returns an error-style CommandResult with a usage message; if OnSendAction is registered, it awaits the callback with the trimmed text and then returns a successful CommandResult.

## Remarks
By delegating the actual broadcast of the action to OnSendAction, this method remains focused on input validation and orchestration. The private scope indicates it is part of the internal command-processing pipeline, invoked by the higher-level handler when parsing a /me invocation.

## Notes
- Exceptions raised by OnSendAction are not caught within this method; callers should rely on upstream exception handling.

---

### HandleMeta
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleMeta()
```

**Returns:** `Task<CommandResult>`


HandleMeta is a tiny asynchronous helper in the command handling workflow. If an OnRoomInfo callback has been registered, it awaits that callback to allow any optional room-information logic to run, and then returns a CommandResult indicating success.

Use it when you want an optional, side-effect-free hook for room-info refresh during meta command handling without forcing callers to implement the null-check and await themselves; if OnRoomInfo is not provided, the method completes immediately with a successful result.

## Remarks
HandleMeta acts as an abstraction that coordinates optional room-state enrichment with the command pipeline. By encapsulating the OnRoomInfo invocation, it decouples room-information concerns from the rest of the command logic, making it easy to inject or mock in tests. It guarantees a consistent success signal via CommandResult, even if OnRoomInfo performs no work.

## Notes
- If OnRoomInfo throws, the exception will bubble up to the caller; there is no internal error handling here.
- The returned CommandResult(true) does not reflect the outcome of OnRoomInfo; use OnRoomInfo itself to signal failures if needed.
- Because OnRoomInfo is awaited, any long-running operation inside it will increase the total latency of this method.

---

### HandleMute
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleMute(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


Parses and handles the /mute command by extracting a username and an optional duration from the input, then delegates the actual mute action to an optional OnMuteUser handler and returns a CommandResult indicating the operation is in progress.

## Remarks
This method serves as a thin, command-UI-oriented wrapper around a mute action. It defers the concrete muting logic to OnMuteUser, enabling testability and alternate mute implementations without changing the command parsing. It also demonstrates defensive parsing: it requires a non-empty arg string, splits into at most two parts (username and an optional duration), and only proceeds to invoke the handler if one is provided. The user-facing feedback is consistently formatted as a "Muting {username}..." message to keep the command responsive while the mute operation completes asynchronously.

## Notes
- If OnMuteUser is null, no mute action is performed beyond returning the Muting message.
- If OnMuteUser throws an exception, HandleMute does not catch it; the exception will propagate to the caller.
- Only the first two space-delimited segments are considered: the first is the username, the second (if present) is parsed as an integer duration in minutes; if parsing fails, duration is treated as null (indefinite mute).


---

### HandleNick
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleNick(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


HandleNick processes the /nick command by validating the supplied display name and applying it when possible. If the argument is missing or whitespace, it returns a CommandResult indicating the proper usage. If an OnSetNick callback is provided, it awaits that callback with the trimmed nickname, and finally returns a success CommandResult showing the updated display name.

## Remarks
HandleNick acts as a focused, testable wrapper around nickname changes, separating input validation from persistence. By accepting an optional OnSetNick delegate, it delegates the actual update to the surrounding system while keeping the command-handling logic simple and easy to reason about.

## Notes
- If OnSetNick is null, the method reports success but does not persist the nickname; ensure a listener is wired if persistence is required.
- If OnSetNick throws, the exception bubbles to the caller since there is no internal error-handling around the delegate.
- The input is trimmed before both passing to OnSetNick and composing the final message, ensuring consistent display without leading or trailing spaces.

---

### HandlePasswd
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandlePasswd(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


Parses and handles the /passwd command by validating its arguments and, when possible, delegating the actual password change to an optional callback. It expects two tokens: the old passphrase and the new passphrase, enforces a minimum length for the new passphrase, and returns a CommandResult indicating success or error.

## Remarks
This method acts as a lightweight command-scaffold: it performs input validation and defers the real password change to OnChangeRoomPassword if provided. By isolating argument parsing from the change logic, it keeps command-handling concerns separate from the actual credential update, enabling test doubles or alternate implementations of the change process. The final outcome is indicated via CommandResult; errors produce usage information or length violations, while a successful path triggers the change callback and returns a non-error result.

## Notes
- Input is split on a single space; passphrases containing spaces won't be accepted.
- If OnChangeRoomPassword is null, the method completes without invoking a change, yet still returns a success result.
- New passphrase must be at least 3 characters; there is no verification of the old passphrase here.

---

### HandleProfile
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleProfile(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


HandleProfile is a private asynchronous command handler that triggers a profile view by delegating to an optional OnOpenProfile callback. It normalizes the incoming argument into a username: if the provided args are null, empty, or whitespace, it uses null; otherwise it trims surrounding whitespace. If a listener is attached, it awaits the callback with the computed username, allowing the consumer to decide how to present the profile. Finally, it returns a successful CommandResult, indicating the command was processed at the wrapper level.

## Remarks
By design this method decouples the command-handling surface from the actual navigation logic. It passes null when no explicit username is supplied, enabling the consumer to interpret that as a request for the current user's profile or a default view. The method is asynchronous only because it awaits the OnOpenProfile callback; the surrounding code can rely on the Task-based pattern without assuming internal navigation details. Note that exceptions raised by OnOpenProfile propagate to the caller, since there is no internal error handling here.

## Notes
- If OnOpenProfile is null, the method completes trivially after returning a success result.
- Exceptions from OnOpenProfile bubble up to the caller; this method does not swallow errors.
- Input normalization ensures that whitespace-only arguments are treated as no username.

---

### HandleQuit
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleQuit()
```

**Returns:** `Task<CommandResult>`


HandleQuit is a private asynchronous method that executes the quit sequence by invoking OnQuit (if provided) and then returning a CommandResult indicating success. It provides a centralized quit path for the command-handling flow, enabling an optional host-defined cleanup step to run before signaling completion to the caller.

## Remarks

By centralizing quit semantics in HandleQuit, the class separates the mechanics of terminating a session from the rest of command processing. It wires an optional asynchronous hook (OnQuit) that external code can supply to perform cleanup or notifications during quit, without forcing callers to know about the hook's existence. Note that exceptions raised by OnQuit will propagate to the caller; this method does not swallow errors, preserving fail-fast semantics for quit-related failures. The method always returns a successful CommandResult when OnQuit completes, or immediately if no OnQuit is provided.

## Notes

- If OnQuit is null, the method returns a CommandResult that signals success immediately after the null check.
- Exceptions from OnQuit propagate to the caller and are not swallowed here.
- This method is private; external code cannot call it directly and must go through the public command-handling path.

---

### HandleRole
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleRole(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


Parses and validates a /role command string, requiring a username and a target role (admin, mod, or member). If valid, it optionally invokes OnAssignRole to apply the change and returns a CommandResult that communicates the outcome or usage errors.

## Dependencies
- CommandResult
- StringSplitOptions

## Dependency APIs (verified signatures)

The REAL, parser-verified API surface of this symbol's collaborators:

- record `CommandResult` (`src/EchoHub.Client/Commands/CommandHandler.cs`)

## Remarks
HandleRole centralizes command parsing for role assignment. It isolates input validation (presence, tokenization, allowed roles) from the actual mutation performed by OnAssignRole, enabling easier testing and decoupling of concerns. By normalizing the role to lowercase, it accepts case-insensitive input while enforcing a strict set of roles. The method always returns a CommandResult, conveying either an error message with usage guidance or a confirmation that the role is being set.

## Notes
- The arguments are split with a maximum of two parts, so the first token is the username and the second is the role; any extra tokens are ignored.
- Role normalization uses ToLowerInvariant to enable case-insensitive input while restricting to the allowed values: 'admin', 'mod', or 'member'.
- OnAssignRole is optional; if provided, it's awaited to perform the actual assignment; if not, the method still returns a confirmation message.

---

### HandleSend
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleSend(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


HandleSend implements the /send command handler. It validates the provided target (a file path or URL), extracts an optional size flag, and triggers the actual sending via the OnSendFile callback when available. It distinguishes between HTTP/HTTPS URLs and local files, returning a user-facing CommandResult that reflects either the ongoing action (Sending or Uploading) or any encountered error (usage, missing target, or missing file).

## Remarks
HandleSend isolates user interaction from the sending mechanism by coordinating argument parsing and the send callback. It calls OnSendFile(target, size) when provided and then formats a helpful status message that includes the resolved file name (defaulting to 'image' when the URL lacks a file name). This method remains robust against missing files and invalid targets, giving clear guidance to the user while deferring the actual transmission to the registered callback. It also supports reading an optional size flag from the end or start of the argument string while honoring quoted paths.

## Notes
- If OnSendFile is null, HandleSend still returns a status message (e.g., "Sending:..." or "Uploading:...") without performing a callback.
- For HTTP/HTTPS URLs, the file name is derived from the URL's path; if absent, a sensible default of "image" is used.
- It relies on an external ParsePathAndSizeFlag(args) helper; behavior depends on that implementation, so edge cases around quoting and flag placement should be considered.
- When targeting a local file, the method checks File.Exists and returns a "File not found: ..." error if absent.

---

### HandleServers
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleServers()
```

**Returns:** `Task<CommandResult>`


HandleServers centralizes the action of opening servers by invoking an optional host-provided callback and returning a success result. Developers reach for this helper when they want the command sequence to trigger server-opening behavior that may be supplied externally via OnOpenServers, rather than implementing the logic inline.

## Remarks

It decouples the behavior from the command flow by using a nullable delegate, making the logic easier to test and replace in different hosting environments. If OnOpenServers is null, the method still returns a successful CommandResult, providing a safe no-op path. Note that there is no try-catch around the await, so exceptions raised by OnOpenServers will propagate to the caller rather than being swallowed here.

## Notes

- Exceptions from OnOpenServers propagate to the caller (no internal suppression).
- The returned CommandResult(true) is independent of the success of OnOpenServers.
- This method is private and intended for internal command flow; external code should not rely on it directly.

---

### HandleStatus
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleStatus(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


Parses and handles the /status subcommands by interpreting either a status keyword or a status message, and then invokes an optional OnSetStatus callback to apply the change. It validates input, supports a message form (msg/message) to set or clear a status message, and returns a CommandResult that communicates success or a structured error (with usage guidance) when appropriate.

## Remarks
This method centralizes the parsing logic for status-related subcommands and delegates the actual state mutation to a downstream callback via OnSetStatus, enabling clean separation between command parsing and state management. The OnSetStatus delegate is optional; if it is not provided, the method still returns a descriptive result but performs no external mutation. A strict policy is enforced: if a valid status keyword is supplied, no additional text is allowed; the special form msg/message may include a trailing message to set, otherwise the message is cleared.

## Example
```csharp
// Example: set online status
var r1 = await HandleStatus("online");

// Example: set a status message
var r2 = await HandleStatus("msg System maintenance at 22:00");

// Example: clear the status message
var r3 = await HandleStatus("msg");

// Example: unknown status yields an error
var r4 = await HandleStatus("busy");
```

## Notes
- The status argument is treated case-insensitively due to ToLowerInvariant, so "Online" and "online" behave the same.
- If a known status is supplied with additional text (e.g., "online extra"), the method returns an error including StatusUsage.
- Even when OnSetStatus is null, the method returns a meaningful CommandResult message and avoids side effects; hook up OnSetStatus to enact real status changes when available.


---

### HandleTestSound
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleTestSound()
```

**Returns:** `Task<CommandResult>`


HandleTestSound is a private async helper that, when an OnTestSound callback is provided, awaits that delegate to play a test notification sound, and then returns a CommandResult signaling success with the message 'Playing notification sound...'.

## Remarks
Serves as a thin abstraction that decouples the act of playing a test sound from the command result flow. By wrapping an optional callback in a single, awaitable operation, it keeps the surrounding command-handling code concise and testable, while allowing the actual sound playback logic to be supplied (or mocked) at runtime.

## Notes
- If OnTestSound throws, the exception propagates to the caller because HandleTestSound doesn't catch it.
- The returned CommandResult is always constructed after OnTestSound completes (when present), with a hard-coded success value; the result does not reflect any potential failure inside the OnTestSound callback.

---

### HandleTheme
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleTheme(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


HandleTheme processes the /theme command. If the user supplies no name, it returns a usage message guiding the caller to provide a theme name from the User menu's theme list (e.g. Default, Transparent, TransparentLight, Hacker). If a name is provided, it forwards the trimmed name to the OnSetTheme callback (if it is set) and returns a confirmation indicating the theme was switched.

## Remarks
By delegating to OnSetTheme, this method keeps command parsing separate from the actual theme application. It also ensures a consistent user feedback surface through CommandResult, regardless of how themes are implemented elsewhere in the codebase.

## Notes
- Validation of the theme name itself is not performed here; it is expected to be validated by OnSetTheme or the surrounding UI layer.
- If OnSetTheme is null, the method still returns a success message, which means the theme change may not be applied.
- Whitespace-only input is rejected with the usage message due to the IsNullOrWhiteSpace check.

---

### HandleTopic
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleTopic(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


Processes a topic-setting command by validating that text is provided, optionally forwarding the trimmed topic to a consumer via OnSetTopic, and returning a CommandResult that indicates success or instructs the user on proper usage. If no text is supplied, it immediately returns an error result with a usage hint; otherwise it applies the topic (when a listener is present) and responds with a confirmation message reflecting the new topic.

## Remarks

- By accepting an optional OnSetTopic callback, this method decouples the command parsing from the actual topic application. It trims input to standardize the topic value and uses a single, consistent success message. The async nature allows the topic application to perform I/O-bound work without blocking the caller.

## Notes

- If OnSetTopic throws, the resulting Task will fault; callers may wish to handle exceptions when the topic application performs I/O or other operations.
- The method is private and intended to be used by the surrounding command-handling workflow; external callers should interact with higher-level command APIs rather than this helper directly.

---

### HandleUnban
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleUnban(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


Handles the /unban command by validating the provided username and triggering the unban workflow if a handler is wired up. If the caller passes an empty or whitespace-only string, it returns an error CommandResult with a usage hint. When a username is supplied, it trims whitespace, calls the OnUnbanUser delegate if it exists, and then returns a success result with a live message indicating the unban operation is underway.

## Remarks

By delegating the actual unban action to OnUnbanUser, this method remains agnostic of how bans are enforced (in-memory, persisted, or communicated to another service). This separation concerns input validation, user feedback, and orchestration, while leaving the business logic to the registered handler. The approach also makes it straightforward to unit-test the command flow by supplying a mock OnUnbanUser and asserting that it was invoked with the trimmed username.

## Notes

- If OnUnbanUser throws, the exception will propagate to the caller since there is no try/catch here.
- The error path uses IsError: true to signal invalid usage; a non-empty username will yield a non-error CommandResult with the final message.

---

### HandleUnmute
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleUnmute(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


Handles the /unmute command by validating its argument, optionally triggering a delegated action, and returning a user-facing result. If no username is provided, it returns an error CommandResult with usage instructions. When a username is supplied, it trims whitespace, invokes the OnUnmuteUser callback if present, and finally returns a success CommandResult indicating that the unmute process has started for that user.

## Remarks
Encapsulates the unmute flow behind a private helper, decoupling the UI command parsing from the actual unmute operation. By deferring to an optional OnUnmuteUser delegate, it enables testability and flexible wiring of the unmute logic. The method also normalizes input by trimming the username before use.

## Notes
- The input username is trimmed before passing to the callback and before including in the final status message, preventing issues caused by surrounding whitespace.
- If OnUnmuteUser is null, the method returns a success result without performing any side effects, allowing the UI to display a consistent message even when no backend action is wired.
- The method is private; it is intended to be invoked through the command-handling pipeline rather than called directly from external code to preserve encapsulation.

---

### HandleUsers
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleUsers()
```

**Returns:** `Task<CommandResult>`


Handles the 'list users' command by optionally invoking an OnListUsers callback and then returning a successful CommandResult. If a consumer provides OnListUsers, that callback is awaited; otherwise the method completes by returning a CommandResult indicating success. This wrapping method keeps the command dispatch path consistent while delegating the actual listing logic to the injected delegate.

## Remarks
A thin wrapper in the command handling pipeline. It delegates the actual listing to OnListUsers via a delegate and then yields a consistent CommandResult(true). This separation allows tests to inject different listing behaviors without altering the caller, and keeps the path for listing users uniform.

## Notes
- OnListUsers exceptions propagate to the caller; this method does not catch errors.
- No cancellation token is observed or supported here.

---

### IsCommand
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
public bool IsCommand(string input) => input.StartsWith('/')
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `input` | `string` | — |

**Returns:** `bool`


IsCommand is a tiny predicate that determines whether an input string should be handled as a command by checking if the first character is '/'. Use it at the entry point of the command-processing path to decide whether to route the input to the command parser rather than treating it as ordinary text, thereby keeping command-detection logic in one place.

## Remarks
IsCommand encapsulates the slash-prefix convention used to invoke commands, providing a single, testable contract for command detection. This keeps command routing decoupled from unrelated input handling and makes future changes—such as supporting a different prefix or multiple prefixes—easier to implement. It also clarifies intent at call sites by replacing ad-hoc prefix checks with a well-named predicate.

## Notes
- Null input may cause a NullReferenceException since input.StartsWith is called directly on input.
- Strings with leading whitespace won't be treated as commands unless trimmed; consider input = input.TrimStart() or adjust logic.

---

### IsValidHex
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private static bool IsValidHex(string s) =>
        s.All(c => char.IsAsciiHexDigit(c))
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `s` | `string` | — |

**Returns:** `bool`


This private helper returns true if every character in the input string is an ASCII hexadecimal digit (0–9, A–F, a–f); otherwise it returns false. It is intended for internal use within the command handler to validate hex-like input before parsing or converting it to binary data.

## Remarks
IsValidHex centralizes hex-character validation, promoting consistent input checks across the command handling code. By leveraging the built-in IsAsciiHexDigit predicate, it stays robust against locale issues and clearly expresses the intent of the check. As a private static member, it remains an internal utility rather than part of the public API, simplifying maintenance and testing within the containing class.

## Notes
- Null input will throw a NullReferenceException when evaluated, since the method dereferences the input string. If you need null-tolerant behavior, guard the parameter before calling this helper.
- An empty string is considered valid by this implementation because All over an empty sequence returns true. If empty input should be rejected, add an explicit check before invoking this method.

---

### StripQuotes
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private static string StripQuotes(string s)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `s` | `string` | — |

**Returns:** `string`


Removes surrounding quotes from a string when the value is wrapped in matching single or double quotes; otherwise it returns the input unchanged. Use this helper to normalize command arguments that may be quoted, instead of duplicating substring logic or handling quotes at every parse path.

## Remarks
Private static helper inside the command handling path, StripQuotes encapsulates a small, focused normalization concern. It prevents scattering the same quote-stripping logic across multiple call sites and makes the intended behavior (remove only matching outer quotes) explicit. The method uses C# range and index syntax (s[1..^1], s[^1]) for a compact implementation.

## Example
```csharp
string input = "\"hello\"";
string output = StripQuotes(input); // output == "hello"
```

## Notes
- It only strips when both ends are the same quote character.
- It does not interpret escape sequences or nested quotes; quotes inside remain.
- Because it is private, external consumers cannot call it; to reuse externally, expose a public wrapper or move to a shared utility.
- Requires C# 8+ for index and range syntax (s[^1], s[1..^1]).

---

### StatusUsage
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** field

```csharp
private const string StatusUsage =
        "Usage: /status <online|away|dnd|invisible> or /status msg <text> (empty text clears it)"
```


StatusUsage is a private constant string that holds the canonical usage text for the /status command used by the command handler. It defines two forms of input: either selecting a predefined status (online, away, dnd, invisible) or providing a custom message with /status msg <text>. An empty text clears the current status. Centralizing this string avoids duplicating literals and helps keep the command’s user-facing guidance consistent across the codebase.

## Remarks
By keeping the usage text in a private field, the implementation encapsulates help and validation concerns within CommandHandler.cs. This makes it easy to adjust the wording or supported syntax in one place. If localization or broader reuse is needed later, this constant should be replaced with a resource or exposed via a helper so external components can reference it consistently.

## Notes
- If you extend supported presets, update both the hard-coded usage and the parsing logic; otherwise the command may reject valid inputs or mislead users.
- Because it's private, external code/tests can't reference it directly; consider exposing a read-only accessor or moving to a resource to improve testability.

---

## CommandResult
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** record

```csharp
public record CommandResult(bool Handled, string? Message = null, bool IsError = false)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Handled` | `bool` | — |
| [`Message`](../../EchoHub.Core/Models/Message.cs.md) | `string?` | `null` |
| `IsError` | `bool` | `false` |


Represents the outcome of handling a command as a compact, immutable value. It indicates whether the command was processed (Handled), carries an optional diagnostic or user-facing message (Message), and signals whether the outcome is an error (IsError). Use this as the return type from a command handler to convey success or failure without relying on exceptions, and to expose contextual details to the caller.

## Remarks
CommandResult acts as a lightweight contract between command invokers and handlers. By being a C# 9 record, it benefits from value-based equality and immutability, enabling safe sharing and straightforward comparisons in tests or across layers. The IsError flag clarifies how callers should react, while a non-null Message can provide actionable context in logs or UI.

## Example
```csharp
// Successful handling with no extra message
var success = new CommandResult(true);

// Failed handling with diagnostic
var failure = new CommandResult(false, "Unknown command", true);
```

## Notes
- Message may be null; always guard before displaying to users.
- Records support with-expressions, so you can derive a near-identical result with a different Message or IsError without reconstructing all fields.

---

## HandleDownloadPath
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleDownloadPath(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


Handles the download path configuration by consuming an optional argument. If the trimmed argument is empty, it signals the OnSetDownloadPath callback to open the native folder picker; if a value is provided, that value is used as the path to set directly. If OnSetDownloadPath is not wired, the call is a no-op and the method still returns a successful CommandResult.

## Remarks

Serves as a thin adapter between the command processing layer and the UI/persistence logic that applies the download path. It centralizes the branching logic: either prompt the user for a path via the native picker or apply a provided path, without forcing the caller to know which path was chosen. This keeps command handling simple while delegating the actual path application to a separate, testable component.

## Notes

- If OnSetDownloadPath is null, the method completes without changing the path; callers should ensure the callback is assigned before invocation.
- Whitespace-only arguments are treated as empty after trimming, which triggers the native folder picker behavior.
- The method does not perform path validation; downstream logic or the callback is responsible for validating the path.

---

## HandleJoin
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleJoin(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `args` | `string` | — |

**Returns:** `Task<CommandResult>`


HandleJoin is a private asynchronous command handler that processes the /join command by validating input, extracting a channel and an optional password, and delegating the actual join operation to a callback if one is registered. It normalizes the channel name by removing a leading '#', and returns a usage error when no arguments are supplied; otherwise it invokes the join hook and returns a success result.

## Remarks
HandleJoin serves as the bridge between user input and the join logic by decoupling command parsing from the actual join implementation. It relies on the OnJoinChannel callback to perform the real join work, enabling testability and flexibility by swapping in different join strategies. The normalization step (stripping a leading '#') supports common user conventions for channel identifiers.

## Notes
- The password extraction line in the snippet appears garbled (a redaction artifact). Ensure a properly declared password variable is assigned from parts[1] when present.
- Accessing parts[1] without checking parts.Length could lead to an IndexOutOfRangeException if the user supplies only a channel name.
- If OnJoinChannel is null, the method will return a successful CommandResult even though no join occurred; consider whether a different outcome is desired when no handler is attached.

## Dependencies
- CommandResult
- StringSplitOptions

## Dependency APIs
- CommandResult (record) — src/EchoHub.Client/Commands/CommandHandler.cs
- StringSplitOptions (enum) — System/StringSplitOptions (used in the Split call)

## Symbol To Document
- Name: HandleJoin
- Kind: method
- File: src/EchoHub.Client/Commands/CommandHandler.cs
- Language: csharp
- ID: 7f13a65c-5457-40c2-b652-518b98329639

---

## HandleNuke
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleNuke()
```

**Returns:** `Task<CommandResult>`


HandleNuke is a concise asynchronous command handler that, when invoked, optionally invokes the OnNukeChannel callback to perform the nuking operation and then returns a CommandResult indicating the channel history is being nuked.

## Remarks
This method acts as a thin wrapper around nuking logic, decoupling the command invocation from the actual nuking work by delegating to OnNukeChannel. It always returns a CommandResult, enabling a uniform response path whether or not a callback is attached.

## Source Code
```csharp
private async Task<CommandResult> HandleNuke()
{
    if (OnNukeChannel is not null)
        await OnNukeChannel();
    return new CommandResult(true, "Nuking channel history...");
}
```

## Dependencies
- CommandResult

## Dependency APIs (verified signatures)
- record `CommandResult` (`src/EchoHub.Client/Commands/CommandHandler.cs`)

## Symbol To Document
- Name: `HandleNuke`
- Kind: `method`
- File: `src/EchoHub.Client/Commands/CommandHandler.cs`
- Language: `csharp`
- ID: ecfce33b-9722-42e3-ab86-a70abb710ed8

---

## ParsePathAndSizeFlag
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private static (string Path, string? Size) ParsePathAndSizeFlag(string args)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Path` | `string` | — |
| `Size` | `string?` | — |


Parses a single argument string to extract a file path (which may be quoted) and an optional size flag (-s, -m, or -l). The flag can be placed either before or after the path, provided there is a separating space. The method returns a tuple (Path, Size) where Path is the cleaned file path and Size is null or the single-letter flag ('s', 'm', or 'l'). The Path is produced by stripping surrounding quotes via StripQuotes.

## Remarks
Centralizes argument parsing for command-line handling. It supports both -flag path and path -flag formats, selecting the flag based on the first matching pattern and ensuring unambiguous separation by a space. The path is normalized by removing surrounding quotes, reducing downstream quoting concerns and making the return value straightforward to consume.

## Notes
- If both start and end forms are present, the end form wins because the end-check runs first and the start form is only considered if no end flag was found.
- A space is required to delimit the flag from the path (either before or after); without the space, the flag is not recognized.
- This is an internal helper (private) intended for use within the command handling logic; external callers cannot rely on it directly.

---