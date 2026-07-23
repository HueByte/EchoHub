# CommandHandler.cs

> **Source:** `src/EchoHub.Client/Commands/CommandHandler.cs`

## Contents

- [CommandHandler](#commandhandler)
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
  - [HandleJoin](#handlejoin)
  - [HandleKick](#handlekick)
  - [HandleLeave](#handleleave)
  - [HandleMe](#handleme)
  - [HandleMeta](#handlemeta)
  - [HandleMute](#handlemute)
  - [HandleNick](#handlenick)
  - [HandleNuke](#handlenuke)
  - [HandlePasswd](#handlepasswd)
  - [HandleProfile](#handleprofile)
  - [HandleQuit](#handlequit)
  - [HandleRole](#handlerole)
  - [HandleSend](#handlesend)
  - [HandleStatus](#handlestatus)
  - [HandleTestSound](#handletestsound)
  - [HandleTheme](#handletheme)
  - [HandleTopic](#handletopic)
  - [HandleUnban](#handleunban)
  - [HandleUnmute](#handleunmute)
  - [HandleUsers](#handleusers)
  - [IsCommand](#iscommand)
  - [IsValidHex](#isvalidhex)
  - [ParsePathAndSizeFlag](#parsepathandsizeflag)
  - [StripQuotes](#stripquotes)
  - [StatusUsage](#statususage)
- [CommandResult](#commandresult)
- [HandleAsciiSize](#handleasciisize)
- [HandleDownloadPath](#handledownloadpath)
- [HandleServers](#handleservers)

---

## CommandHandler
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** class

```csharp
public class CommandHandler
```


A central, asynchronous command dispatcher for parsing slash-style input and routing it to consumer-provided handlers. Use `CommandHandler` when you need a UI or orchestrator to interpret text commands (strings that begin with `/`) and invoke application logic via event hooks rather than hard-wiring command execution into the input component. The handler exposes a large set of `On...` events (for example `OnSetStatus`, `OnSendAction`, `OnCreateInvite`) that consumers subscribe to; calling `HandleAsync` parses the input and raises the appropriate event, returning a `CommandResult` that describes the outcome.

## Remarks
`CommandHandler` separates command parsing from command execution by exposing each command as an event of type `Func<..., Task>`. This makes it simple for the UI layer or an orchestrator to register asynchronous handlers for only the commands it cares about and keeps the parsing logic isolated in one place. The event signatures use nullable and optional parameters (for example `UserStatus?` and `int?`) to represent semantic distinctions described in the source comments — notably the `OnSetStatus` contract where a `null` status means "keep the current status", a `null` message means "keep the current message", and an empty message string means "clear it".

## Example
```csharp
// Hook a handler and dispatch a command string
var handler = new CommandHandler();
handler.OnSetStatus += async (status, message) =>
{
    // apply status and message to session state
    await Task.CompletedTask;
};

// This input will be recognized as a command because it starts with '/'
if (handler.IsCommand("/status away"))
{
    var result = await handler.HandleAsync("/status away");
    // inspect 'result' (a CommandResult record) to determine success or error
}
```

## Notes
- `IsCommand` uses `input.StartsWith('/')`, so leading whitespace prevents recognition; callers should trim input if they expect tolerant detection.
- Many events may be `null` when no subscriber is attached; consumers should ensure they subscribe to the commands they intend to handle, and callers of `HandleAsync` should expect that some commands may have no effect if no handler is present.
- The `OnSetStatus` semantics are intentionally specific: `null` vs empty string for the message are distinct (keep vs clear). The handler's parsing enforces strictness for some forms (see `StatusUsage`), so inputs that look like natural language may be rejected as invalid commands rather than implicitly treated as a different command form.

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


HandleAsync validates that the input starts with a slash command, normalizes the command name to be case-insensitive, and dispatches to the appropriate per-command handler (for example `HandleStatus`, `HandleMe`, `HandleExport`). It returns a `CommandResult` representing the outcome of the invoked handler, performing the operation asynchronously by awaiting the selected method; unknown commands yield a user-facing error.

## Remarks
HandleAsync is the central dispatcher for slash-based commands in the client. It maps each known command to a dedicated `HandleX` method, enabling the command implementations to stay focused on their behavior. It relies on `IsCommand` to guard inputs and on splitting logic to extract a command and its optional arguments, keeping routing consistent across commands.

## Notes
- Adding new commands requires updating the command routing to point to a new `HandleX` method.
- Unknown commands are surfaced as a user-facing error rather than throwing exceptions.

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


It handles the `/avatar` command by validating the input, stripping surrounding quotes from the trimmed argument, and then optionally invoking an avatar setter via `OnSetAvatar` before returning a success message. If no argument is provided (or it's whitespace), it immediately returns a `CommandResult` with an error and the usage hint `Usage: /avatar <URL or filepath>`. When a non-empty target is supplied, it calls `OnSetAvatar` if available and finally responds with `Uploading avatar...`.

## Remarks
This symbol encapsulates a small, testable command pattern: input validation, normalization, optional delegation, and user feedback. The optional `OnSetAvatar` delegate allows the avatar update logic to live outside the command handler, enabling different hosting contexts to supply their own upload behavior without changing this method.

## Notes
- Awaiting `OnSetAvatar` means the command handling thread will asynchronously wait for the upload to complete; consider the caller's synchronization context and potential long-running operations.
- If `OnSetAvatar` is not provided, the method still returns a success message, so ensure a consumer wires up the handler to actually execute the avatar update.
- There is no additional validation on the URL or file path beyond non-emptiness and quotes stripping.

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


HandleBan is a private asynchronous command handler for the `/ban` command. It validates input (returning a usage error when `args` is empty or whitespace), parses the first token as `username` and the remainder as an optional `reason`, and, if an `OnBanUser` handler is supplied, awaits it before returning a `CommandResult` indicating the ban is being processed.

## Remarks
This symbol acts as a thin wrapper that decouples the command parsing from the actual ban logic by delegating to `OnBanUser` when present. It uses `StringSplitOptions.TrimEntries` to robustly split the input into `username` and `reason` without stray whitespace. If no `OnBanUser` handler is attached, the method still returns a progress message, which can be misleading; the real ban happens only when a consumer wires up the `OnBanUser` callback.

## Notes
- If `OnBanUser` is null, no ban action is performed; ensure a handler is attached before invoking this symbol.

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


Handles the `/banner` command by validating the provided text and broadcasting it to the client or other listeners via the `OnSendBanner` callback. If the input `args` is null, empty, or whitespace, it returns a `CommandResult` marked as an error with a usage hint: `Usage: /banner <text> (letters, digits, basic punctuation)`. If valid text is supplied, it trims the input and, if the `OnSendBanner` handler is attached, awaits its invocation with the trimmed text, then returns a successful `CommandResult`.

## Remarks
This method decouples the banner broadcast from the command-handling pipeline by emitting the banner text through a callback rather than performing the display itself. It centralizes validation for the `/banner` command and communicates outcomes via a `CommandResult`, which callers can inspect to present feedback to users. The `OnSendBanner` invocation is conditional, avoiding a null-reference when no subscriber is attached.

## Notes
- The banner text is trimmed before broadcasting to avoid leading or trailing spaces.
- If no `OnSendBanner` subscriber is attached, the command still reports success; the side effect is simply skipped.

---

### HandleClear
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleClear()
```

**Returns:** `Task<CommandResult>`


This private async method clears attachments staged for the current command by invoking the optional `OnClearAttachments` callback when provided, awaiting its completion, and then returning a `CommandResult` indicating success with the message "Cleared staged attachments." It is used internally by the command-handling flow to reset the staging state after a clear action.

## Remarks
By delegating the actual clearing action to the optional `OnClearAttachments` delegate, this method decouples the clear operation from the concrete storage or UI details, enabling swap-in of different clearing strategies in tests or configurations. It always returns a successful `CommandResult` when it completes, even if no delegate is provided (in that case nothing is cleared).

## Notes
- If `OnClearAttachments` is null, the method does not perform any action beyond returning the success result.
- If `OnClearAttachments` throws, the exception is propagated to the caller and not caught here.

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


Parses and normalizes the color argument from a command, validating that it is a valid hex color in the format `#RRGGBB`. It accepts either a plain hex string or one prefixed with `#`, automatically prepends a leading `#` if missing, and rejects empty input with a usage hint. When the color is valid, it optionally notifies the consumer through the `OnSetColor` callback and returns a `CommandResult` that confirms the nickname color has been set to the normalized value.

## Remarks
This method concentrates the color customization flow for the command surface. By deferring the final color application to the optional `OnSetColor` callback, it stays decoupled from the downstream state management and remains easy to unit-test. It relies on the `IsValidHex` helper to validate the color digits and provides explicit user feedback for both invalid input and success.

## Notes
- Because the method is `private`, tests typically exercise it through the public command handler path rather than calling it directly.
- If `OnSetColor` is not assigned, color changes are not propagated; ensure a handler is attached if you expect the color to take effect.

---

### HandleDeleteAccount
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleDeleteAccount()
```

**Returns:** `Task<CommandResult>`


HandleDeleteAccount is a private async helper that, when invoked, awaits the optional `OnDeleteAccount` callback if it has been supplied, and then returns a `CommandResult` indicating success. It provides a simple, centralized way to trigger the deletion flow without exposing the internal delegation to external callers.

## Remarks
Being private, it keeps the deletion orchestration encapsulated within the command handler. It invokes the `OnDeleteAccount` callback only when it is non-null and awaits its completion; if no handler is provided, it simply proceeds. It always returns a successful `CommandResult` after that, so the actual deletion outcome is determined by the `OnDeleteAccount` implementation, not this wrapper.

---

### HandleExport
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleExport()
```

**Returns:** `Task<CommandResult>`


`HandleExport` is a private asynchronous wrapper that triggers the export flow by invoking the optional `OnExportData` delegate (if assigned) and then returns a successful `CommandResult`. Callers reach for it to initiate export logic through the handler without coupling to a concrete export implementation; if no export handler is attached, it completes as a no-op while still signaling success.

## Remarks
By deferring the actual work to `OnExportData`, this method isolates export concerns from the command handler and provides a clean extension point for tests and runtime customization. If `OnExportData` is null, the method simply returns success, making the export step optional. Any exceptions thrown by `OnExportData` will bubble up to the caller since there is no exception handling here.

## Notes
- This method is private, intended for internal orchestration within the class.
- The returned `CommandResult` is always created with `true` — it does not reflect the success or failure of the export delegate itself.
- If you rely on export results, consider handling exceptions at the call site or wiring `OnExportData` to signal failures.

---

### HandleHelp
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleHelp()
```

**Returns:** `Task<CommandResult>`


This private async method handles the help command by optionally invoking the `OnHelp` callback and returning a successful `CommandResult` that contains the multi-line help text enumerating the available commands. It uses a pre-formatted string literal to deliver the help content so users see a readable, aligned list of commands across categories (user, moderation, and utility).

## Remarks
By centralizing help content in `HandleHelp`, updates to the command list are made in one place, reducing drift between the dispatcher and the displayed help. The `OnHelp` delegate provides a hook for extending or customizing help behavior without altering the core command-dispatch logic.

## Notes
- Hard-coded help text means localization is not supported out of the box; consider external resources or injecting a localization service if multi-language support is required.

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


HandleInvite is the private command handler that processes the '/invite' input by parsing the argument string and routing to list, revoke, or create flows. It uses space-delimited tokens and delegates to `OnListInvites`, `OnRevokeInvite`, or `OnCreateInvite` when wired, returning a `CommandResult` that signals success or provides usage guidance.

## Remarks
HandleInvite centralizes the invite-management command surface in the client, decoupling the command parsing from the actual business logic behind listing, revoking, and creating invites. By validating inputs (e.g., ensuring revoke has a code; ensuring numeric values for maxUses and expiresHours) before invoking the callbacks, it minimizes error paths and provides consistent usage messages. Note that if the callbacks are not wired (null), the method completes with success but performs no action; wiring is required to affect state.

## Example
```csharp
// Demonstrative inputs (not direct calls to this private method in production)
var r1 = await HandleInvite("list");        // lists invites
var r2 = await HandleInvite("revoke CODE"); // revokes CODE
var r3 = await HandleInvite("5 24");         // creates an invite with 5 uses and 24h expiry
```

## Notes
- If `OnListInvites`, `OnRevokeInvite`, or `OnCreateInvite` are null, the method completes with success without performing any action.
- The input is split using `StringSplitOptions.RemoveEmptyEntries` and `StringSplitOptions.TrimEntries`, so extra spaces do not produce empty tokens.

---

### HandleJoin
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


Parses the input for a '/join' command, validates that a channel (and optionally a password) is supplied, and forwards the join request to a registered handler. It normalizes the channel name by removing a leading '#', and returns a CommandResult that signals success or a usage error.

## Remarks
This method serves as a small command-dispatch helper that decouples the parsing/validation of join requests from the actual join operation. By invoking the `OnJoinChannel` callback when present, it allows the channel-join logic to be injected or mocked, which is useful for testing and for swapping out how joins are performed without touching the parsing layer. It also supports both public and password-protected channels by treating an additional argument as the optional password.

## Example
```csharp
// Most common case: join a channel with a password
var result = await HandleJoin("#general mysecret");
```

## Notes
- The password extraction line in the provided source appears garbled (the token `[REDACTED:CONNECTION_STRING_PASSWORD]` and a comparison against `1`). This will not compile as written. Replace with a proper password extraction, for example:
  ```csharp
  var password = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : null;
  ```

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


HandleKick serves as the internal entry point for processing a `/kick` command. It validates that arguments are provided, splits them into a `username` and an optional `reason`, and, if present, awaits the `OnKickUser` callback before returning a `CommandResult` that signals the kick is in progress. It centralizes argument handling and user feedback for kicking a user.

## Remarks
Decoupling the command surface from the actual kick action via the `OnKickUser` callback enables testability and modularity. If no listener is attached, the method still returns a `CommandResult` indicating the kick is being processed, which preserves a consistent user experience even when the actual kick logic is not wired up. The parsing uses a two-part split to treat the first token as `username` and the rest as an optional `reason`, preserving spaces in the reason.

## Notes
- If `args` is null, empty, or whitespace, the method returns a usage error message.
- When `OnKickUser` is provided, it is awaited; otherwise the method completes with a proactive user feedback message without performing a kick action.

---

### HandleLeave
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleLeave()
```

**Returns:** `Task<CommandResult>`


Handles the leave action by optionally invoking the `OnLeaveChannel` callback and then returns a successful `CommandResult`.

Use this helper to centralize the leave flow so that the caller doesn't need to duplicate the optional callback invocation and always receives a confirmed success.

## Remarks
This method serves as a tiny orchestration point within the command-handling flow. It delegates the optional notification of leaving to `OnLeaveChannel` and always yields a positive `CommandResult`, keeping the leave path concise and consistent.

## Notes
- If the delegate `OnLeaveChannel` throws, the exception propagates to the caller since there is no internal exception handling here.

## Example
```csharp
// Inside the same class that defines HandleLeave
var result = await HandleLeave();
```


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


HandleMe processes the user-directed emote command (/me). It requires an action string; if the input is missing or whitespace, it returns a `CommandResult` containing a usage message and flags it as an error. When an `OnSendAction` handler is present, it is awaited with the input trimmed of whitespace, and the method then returns a successful `CommandResult`.

## Remarks
This method decouples the parsing and validation of the `/me` command from the actual sending of the action by routing via the `OnSendAction` callback. This makes the behavior easily testable and allows the application to swap in different emission strategies without changing the command-handling code.

## Notes
- If `OnSendAction` is null, no action is emitted even when a non-empty argument is supplied; the method still returns a success result.

---

### HandleMeta
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleMeta()
```

**Returns:** `Task<CommandResult>`


Handles the internal 'Meta' operation by optionally invoking the `OnRoomInfo` delegate if it is assigned and awaiting its completion, then returning a successful `CommandResult` (`new CommandResult(true)`). This method is invoked as part of the command-handling flow when meta-information about the room may be produced via the `OnRoomInfo` hook.

## Remarks
This symbol acts as an internal bridge between command handling and an optional hook. By performing a null-check before calling `OnRoomInfo` and standardizing the success response as a `CommandResult`, it keeps the meta-processing path concise and testable. If `OnRoomInfo` throws, the exception propagates to the caller since there is no try-catch here.

## Notes
- The method is private and can only be invoked by its containing type; external callers should go through the public command-handling path.

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


HandleMute processes the '/mute' command by parsing the supplied arguments. It expects a username and an optional duration in minutes. If arguments are empty or whitespace, it returns a usage error through `CommandResult` with a usage string. If a username is provided (and an optional duration can be parsed), it invokes the `OnMuteUser` delegate (if assigned) to perform the mute, and then returns a `CommandResult` indicating that the user is being muted.

## Remarks

Separation of concerns: this method is purely about parsing and delegating; the actual mute implementation is injected via `OnMuteUser`, enabling testability and replacement of mute logic. The input is split into at most two parts; the first is treated as the username and the second, if present and numeric, as the duration in minutes. This means usernames containing spaces aren't supported by this simple parser, and non-numeric durations are ignored (interpreted as null).

## Notes

- If `args` is empty or whitespace, a usage error is returned.
- If `OnMuteUser` is not assigned, the method will return a muting message without performing any action.
- The mute duration is optional; when omitted, duration stays null and the actual duration must be handled by the mute handler.


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


HandleNick processes the `/nick` command by validating the supplied display name and updating the display name when a handler is attached. If the argument is missing or whitespace, it returns an error `CommandResult` with the usage hint; if a name is provided, it trims it, optionally invokes the asynchronous `OnSetNick` callback to apply the change, and then returns a success `CommandResult` confirming the new display name.

## Remarks
By encapsulating nickname changes behind an optional `OnSetNick` delegate, this method decouples command parsing from nickname persistence and provides a uniform `CommandResult` contract to signal outcomes (whether guidance is needed or a nickname was updated). It relies on input sanitation (trimmed values) and adheres to the command-driven UX pattern used by other commands in the client.

## Notes
- If `OnSetNick` is null, the nickname change is not persisted, though the method still returns a success `CommandResult` with the confirmation message.
- The input is trimmed before both the callback and the confirmation, ensuring consistent storage and user feedback regardless of user formatting.


---

### HandleNuke
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleNuke()
```

**Returns:** `Task<CommandResult>`


HandleNuke asynchronously triggers the nuking workflow by invoking the optional `OnNukeChannel` callback and then returns a successful `CommandResult` with the message "Nuking channel history...". It is used by the command handling flow to initiate nuking through a configurable hook rather than containing the nuking logic itself.

## Remarks
This symbol decouples the command-processing path from the actual nuking implementation by delegating to `OnNukeChannel` when provided. If the delegate is not assigned, the method still completes with a success result, ensuring the caller receives a consistent response even in the absence of a nuking hook.

## Notes
- If `OnNukeChannel` is null, the nuking action does not run; the method returns `new CommandResult(true, "Nuking channel history...")`.

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


HandlePasswd parses the argument string for the `/passwd` command, expecting exactly two tokens: the old passphrase and the new passphrase. It uses `StringSplitOptions.RemoveEmptyEntries` and `StringSplitOptions.TrimEntries` to split, validates the token count and the new passphrase length, and returns an error `CommandResult` with usage guidance or a length message if validation fails; if a handler is attached via `OnChangeRoomPassword`, it is invoked asynchronously with the old and new passphrases, and a success `CommandResult` is returned.

## Remarks
Conceptually, this symbol acts as a thin, command-level gatekeeper that delegates the actual password change to a pluggable collaborator. It centralizes basic input validation for the `/passwd` command and ensures the operation is asynchronous by awaiting the handler. The real change occurs only if an `OnChangeRoomPassword` handler is wired; otherwise the method returns success without modifying any state.

## Notes
- The method is `private`; callers outside the class cannot invoke it directly and tests must exercise the command pipeline.
- If no `OnChangeRoomPassword` handler is attached, the call will report success without applying any change.
- It delegates actual password-changing logic to the `OnChangeRoomPassword` handler; the method itself performs only light input validation.

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


HandleProfile processes the profile command by deriving a sanitized username from the input `args`, then, if an `OnOpenProfile` handler is registered, it awaits that callback with the `username`. It always returns a successful `CommandResult`, independent of whether a profile navigation was actually performed.

## Remarks
HandleProfile serves as a small mediator between the command parser and the profile navigation logic. By normalizing the input and guarding the callback against absence, it decouples command handling from the actual navigation implementation represented by `OnOpenProfile`. The method returns a `CommandResult(true)` to indicate the command was processed, while the navigation is carried out asynchronously when available.

## Notes
- If `args` is null or whitespace, `username` becomes `null`, and the call passes `null` to `OnOpenProfile`—this is a contract decision left to the handler.

---

### HandleQuit
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleQuit()
```

**Returns:** `Task<CommandResult>`


`HandleQuit` is an asynchronous method that coordinates the quit workflow for the command-handling path. It conditionally invokes the `OnQuit` callback if one is supplied, awaiting its completion, and then returns a successful `CommandResult` by constructing `new CommandResult(true)`.

## Remarks

This method serves as a lightweight quit coordinator: it delegates the actual quit work to an optional `OnQuit` handler and exposes a consistent success signal to its callers. By centralizing this logic, the command pipeline can trigger quit behavior without duplicating event invocation logic elsewhere, while still allowing consumers to subscribe to `OnQuit` to perform custom shutdown steps.

## Notes

- If `OnQuit` throws, the exception propagates out of `HandleQuit` because there is no try/catch inside this method.

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


HandleRole is a private asynchronous command handler that processes the arguments of a "/role" command, validates the username and the requested role, and triggers the role-assignment workflow if a listener is provided. It validates that two arguments are present (a username and a role from the set admin, mod, or member) and returns clear, user-facing messages for both usage and invalid input. If a valid role is provided and an assignment callback is supplied via `OnAssignRole`, the method awaits that callback before returning a success message.

Because the method encapsulates argument parsing, validation, and user feedback, callers gain a consistent command-handling surface for role changes without duplicating error handling logic. It delegates the actual assignment to `OnAssignRole` to keep the command-layer concerns isolated from business logic, enabling test doubles or alternate implementations to be plugged in without altering the parsing behavior.

## Remarks
HandleRole centralizes the /role command’s input handling: it ensures you always receive a well-formed pair of `username` and `role`, normalizes the role to lower-case, and only proceeds when the role is one of the allowed values. The actual grant or revocation action is delegated to the `OnAssignRole` callback, preserving separation of concerns between command parsing and role-management logic. If `OnAssignRole` is not provided, the method still validates input and returns a final message indicating the target role would be set, which can be useful in dry-run scenarios or for UI testing.

## Notes
- Uses `Split` with a maximum of two parts and `StringSplitOptions.TrimEntries`, so extra tokens beyond the username and role are rejected as an invalid role (e.g., "/role user admin extra" becomes an invalid role).
- Normalizes the role with `ToLowerInvariant()` and accepts only `admin`, `mod`, or `member`; any other value yields a user-facing usage note about valid roles.
- If `OnAssignRole` is provided, the method awaits it before reporting success; if not, it returns a success message without performing any assignment.


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


HandleSend is the private asynchronous handler for the `/send` command. It validates input, supports both `http`/`https` URLs and local file paths, extracts an optional size flag, and triggers the `OnSendFile` callback with the resolved target and size, returning a `CommandResult` that informs the user of the action (either sending a URL or uploading a file).

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


HandleStatus processes a user-issued status command. It splits the incoming `args` into at most two parts using `StringSplitOptions.TrimEntries` and normalizes the first token with `ToLowerInvariant`. It supports `msg`/`message` to set or clear the status message via the `OnSetStatus` callback; otherwise it recognizes `online`, `away`, `dnd`/`donotdisturb`, and `invisible` as [`UserStatus`](../../EchoHub.Core/Models/UserStatus.cs.md) values. Unknown tokens or malformed input yield a `CommandResult` error that includes the usage string.

## Remarks
HandleStatus centralizes parsing and validation of status-related commands and delegates actual state updates to the `OnSetStatus` callback, keeping the parser as a pure command translator. This separation helps ensure consistent behavior across the UI and core models while allowing the rest of the system to react to status changes. It also encodes results via `CommandResult`, making success and error messages explicit to callers.

## Notes
- Unknown status tokens yield an error and show the usage hint.
- Passing `msg`/`message` with no trailing text clears the status message; providing text sets the message.
- If `OnSetStatus` is `null`, the method returns a success `CommandResult` but no external state is updated.


---

### HandleTestSound
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleTestSound()
```

**Returns:** `Task<CommandResult>`


Handles the test sound command by optionally invoking the `OnTestSound` callback if it is provided, then returns a `CommandResult` indicating success with the message `Playing notification sound...`. The method is `async` to accommodate the potential asynchronous callback invocation.

## Remarks
This method serves as a small adapter around an optional delegate: it defers the actual sound-playing work to `OnTestSound` when supplied, and it guarantees a consistent command result is returned to the caller. By centralizing the return value to a `CommandResult` with a success flag, it keeps the command-handling flow uniform across potential test sound implementations. It also isolates the scheduling of the sound-playing action from the rest of the command handling, making the behavior easy to mock in tests.

## Notes
- If `OnTestSound` throws, this method will propagate the exception since there is no try/catch around it; callers should handle failures at a higher level.

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


Handles the `/theme` command by validating the provided theme name, optionally dispatching the change via the `OnSetTheme` callback, and returning a structured `CommandResult` that reflects the outcome. When a non-empty argument is supplied, it trims the value, awaits `OnSetTheme` if a handler is registered, and returns a success message: `Theme switched to: <name>`. If the argument is missing or whitespace, it returns an error `CommandResult` with usage guidance containing the supported usage: `Usage: /theme <name> — pick one from the User menu's theme list (e.g. Default, Transparent, TransparentLight, Hacker)`. 

## Remarks
The `HandleTheme` method decouples command handling from the actual theme application by exposing an optional `OnSetTheme` callback. This allows hosting environments to plug in their own theme behavior without the command logic needing to know how themes are stored or applied. By funneling user feedback through the `CommandResult`, callers receive a consistent success/failure signal and message formatting.

## Notes
- Be aware that exceptions thrown by `OnSetTheme` propagate to the caller since there is no internal try/catch around the await. If you want robust error handling, consider wrapping the callback invocation or validating themes within the delegate itself.
- The method trims input before processing, so leading/trailing whitespace is ignored when applying the theme.

## Dependencies
- CommandResult

## Dependency APIs (verified signatures)
The REAL, parser-verified API surface of this symbol's collaborators:

- record `CommandResult` (`src/EchoHub.Client/Commands/CommandHandler.cs`)

## Symbol To Document
- Name: `HandleTheme`
- Kind: method
- File: `src/EchoHub.Client/Commands/CommandHandler.cs`
- Language: `csharp`
- ID: 92d79819-5755-4464-a323-9101267cd9ab


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


Handles the `/topic` command by validating the input and optionally delegating to the `OnSetTopic` callback. If the `args` parameter is null or whitespace, it returns a `CommandResult` containing a usage message and marks the result as an error. Otherwise, it trims the input, invokes `OnSetTopic` if provided, and returns a successful `CommandResult` with the message `Topic set to: {trimmed}`.

## Remarks
This method serves as a small, focused command-dispatch wrapper: it performs input validation, keeps the topic persistence/update logic decoupled via the `OnSetTopic` hook, and consistently reports the outcome back to the caller. By making the topic update an optional hook, the surrounding system can decide how to apply or broadcast the new topic without changing the command-handling flow. The trimming of the topic text ensures user input is normalized before persistence and confirmation.

## Notes
- If `OnSetTopic` throws, the exception will propagate to the caller since there is no local exception handling here.
- The method short-circuits on empty or whitespace-only `args`, returning a usage-style error message instead of attempting to set a topic.
- It is a private member, indicating it's intended for internal command processing rather than public API consumption.

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


The `HandleUnban` method processes the `/unban` command by validating the input and delegating the unban action to an optional callback. If the input is missing or whitespace, it returns a `CommandResult` with an error message and usage instructions via `string.IsNullOrWhiteSpace`. If a username is provided, it trims it with `args.Trim()` and, if an `OnUnbanUser` handler is assigned, awaits it. It then returns a success `CommandResult` indicating the unbanning of the specified user.

## Remarks
This method centralizes the unban command's validation and delegation, keeping the command handling logic decoupled from the actual unban implementation. It relies on a potential consumer exposed via `OnUnbanUser` to perform the unban, allowing the hosting context to supply the concrete behavior.

## Notes
- If `OnUnbanUser` is assigned and throws, the exception propagates to the caller because there is no internal catch block.
- The final user-facing message uses the trimmed username via `args.Trim()` to avoid leading/trailing spaces in the display.

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


````markdown
"""Description"""

`HandleUnmute` is a private async method that processes the `/unmute` command for a given username. It first validates the input; if the argument is null or whitespace it returns a `CommandResult` marked as an error with the usage hint. If a username is provided, it trims whitespace and, if the `OnUnmuteUser` callback is wired, awaits its execution with the username. It then returns a success `CommandResult` with a human-friendly message indicating that the unmute action is underway. This method encapsulates small command-handling logic and centralizes user feedback, while delegating the actual unmute operation to the consumer via `OnUnmuteUser`.

"""

## Remarks

By funneling the unmute action through a delegate (`OnUnmuteUser`), the symbol decouples command parsing from domain logic, enabling tests and replacements of the unmute behavior without changing the command handler. The method always produces a `CommandResult`, ensuring the caller can render consistent feedback regardless of whether the unmute handler is attached. The input normalization with `args.Trim()` prevents issues from extra whitespace. If no handler is attached, the call is effectively a no-op besides returning the success message, which is a deliberate design choice to keep the user experience consistent.

## Example

```csharp
// Typical usage within the command handler class
OnUnmuteUser += async username => {
    // Actual unmute logic would run here
    await Task.CompletedTask;
};

var result = await HandleUnmute("Alice");
// result.IsError == false and result.Message contains "Unmuting Alice..." (subject to implementation)
```

## Notes
- The error path returns a `CommandResult` with `IsError` set (e.g., to indicate invalid usage).
- The actual unmute action is delegated to `OnUnmuteUser`; if this delegate is null, the call does not perform unmuting but still returns a success message.
- Whitespace in the input is trimmed before any processing to normalize the username.
````


---

### HandleUsers
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleUsers()
```

**Returns:** `Task<CommandResult>`


Invokes the optional `OnListUsers` callback to perform the user listing and always yields a successful `CommandResult` (`new CommandResult(true)`). If a consumer supplies `OnListUsers`, the method awaits `OnListUsers()`; otherwise it completes without performing any listing.

## Remarks
By delegating the actual listing work to the `OnListUsers` delegate, this method acts as a thin adapter that preserves a consistent command result while allowing the listing behavior to be swapped or mocked in tests. It decouples the command flow from the UI or data access concerns and ensures a predictable outcome regardless of whether a listing handler is provided.

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


IsCommand determines whether the provided input should be treated as a command by checking if it starts with the '/' prefix. This predicate is used to route input to the command-handling path rather than treating it as plain text.

## Remarks
This tiny predicate encapsulates the command-prefix rule, allowing future changes (for example, supporting additional prefixes or configurable behavior) to be implemented in one place. It clarifies the separation of concerns: any code that needs to decide between command parsing and regular message processing should rely on this method's boolean result rather than duplicating the prefix check. `IsCommand` acts as a single source of truth for command-detection logic.

## Notes
- Passing a null `input` will throw a `NullReferenceException` when evaluating `StartsWith`; ensure callers perform null checks or the parameter is made non-nullable in line with your project's nullability rules.

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


IsValidHex is a private static predicate that returns true when every character in the input string `s` is an ASCII hexadecimal digit, i.e., one of `0`–`9`, `A`–`F`, or `a`–`f`. Implemented as a single expression using `s.All(c => char.IsAsciiHexDigit(c))`, it serves as a compact guard to validate hex-like inputs before parsing or further processing. Developers reach for this helper when a quick, inline check is needed to enforce that an input contains only ASCII hex digits, rather than performing a manual loop or pattern match.

## Remarks
Because it's private and static, it encapsulates the rule inside the command handler's implementation, preventing duplication and keeping the public API cleaner. It relies on `char.IsAsciiHexDigit`, which ensures strict ASCII-only digits rather than any Unicode hex digit, and therefore may reject strings that conceptually represent hex values but include non-ASCII characters. The function is pure and side-effect free; its result depends solely on the input string.

## Notes
- Null input will throw a `NullReferenceException` when the expression `s.All(...)` is evaluated; ensure `s` is non-null before calling `IsValidHex`, or guard the call site accordingly.

---

### ParsePathAndSizeFlag
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


Parses a single argument string to extract a file path (which may be quoted) and an optional size specifier. The size flag can be written as -s, -m, or -l either before or after the path. It returns a tuple (Path, Size) where Path is the path with surrounding quotes removed, and Size is null if no flag was found or 's', 'm', or 'l' if a flag is present. This encapsulates a tiny, CLI-like parsing rule so the rest of the command handling can rely on a normalized pair rather than slicing and quoting logic itself.

## Remarks
This helper centralizes input normalization for a small command-line style syntax used by the command handler. It trims whitespace, supports quoted paths with spaces, and detects the flag in either position (start or end). When both forms could apply, the trailing-flag form takes precedence; after recognizing a flag, the path is trimmed accordingly and quotes are stripped via `StripQuotes` so callers always receive a clean path string. The returned `Size` is a single-letter value ("s", "m", or "l") or `null` when no flag is present, enabling callers to branch on the presence of a size specifier without re-parsing the string.

## Example
```csharp
// Trailing flag
var (path1, size1) = ParsePathAndSizeFlag("C:\\Files\\report.txt -m");
// path1 == "C:\\Files\\report.txt", size1 == "m"

// Leading flag with quoted path
var (path2, size2) = ParsePathAndSizeFlag("-s \"My Documents\\data.csv\"");
// path2 == "My Documents\\data.csv", size2 == "s"
```

## Notes
- The size value is a single-letter string: `"s"`, `"m"`, or `"l"`, or `null` if no flag is found. Never assume a full word; the implementation maps only these short forms.
- The path is always passed through `StripQuotes`, so callers work with an unquoted path even if the input used quotes.
- The parser only recognizes flags at the defined positions (start or end) and requires a space separating the flag from the path segment in its respective form. Inputs that don’t match these patterns will yield a `Size` of `null` and a trimmed path without quotes.


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


StripQuotes removes matching surrounding quotes from a string. If the input begins and ends with the same quote character, either a double quote or a single quote, it returns the inner content (`s[1..^1]`). Otherwise, it returns the original string unchanged (`s`). This private helper is typically used in command or argument parsing to normalize values by removing wrapping quotes before further processing. Note that the method assumes a non-null input; passing a null input will throw a `NullReferenceException`.

## Remarks
This tiny utility encapsulates a common formatting concern: stripping a wrapping quote from a value. It centralizes the logic so command and argument parsing can rely on a single, consistent normalization step instead of duplicating code at multiple sites. Because it is private and static, it's intended as a local helper within the class that owns it rather than a general-purpose API.

## Example
```csharp
string a = "\"hello\"";
string r1 = StripQuotes(a); // hello

string b = "'world'";
string r2 = StripQuotes(b); // world

string c = "\"mismatch'";
string r3 = StripQuotes(c); // "mismatch'

string d = "plain";
string r4 = StripQuotes(d); // plain
```

## Notes
- Null inputs are not handled; a null argument will throw `NullReferenceException`.
- Surrounding quotes are stripped only when both ends are the same quote type (both double quotes or both single quotes); otherwise the string is returned unchanged.
- It is private to the class and not intended for external use; if reuse is required, consider extracting a public helper.

---

### StatusUsage
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** field

```csharp
private const string StatusUsage =
        "Usage: /status <online|away|dnd|invisible> or /status msg <text> (empty text clears it)"
```


StatusUsage is the canonical usage text for the `/status` command. It defines the accepted inputs: `/status online`, `/status away`, `/status dnd`, and `/status invisible`, as well as the variant `/status msg <text>` for setting a textual status, where an empty `<text>` clears the status. This constant is used by the command handler to present consistent usage information and to validate user input without duplicating strings elsewhere.

## Remarks
`StatusUsage` encapsulates the exact syntax users must follow for the `/status` command, acting as a single source of truth that the command handler relies on when parsing input and presenting help. Keeping it as a private constant prevents duplication and mismatches between parsing logic and user-facing messages, and it simplifies future localization or extension of the command.

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


Represents the outcome of processing a command in the command handling flow. Use `CommandResult` to convey whether the command was handled, optional detail text in [`Message`](../../EchoHub.Core/Models/Message.cs.md), and whether the result represents an error via `IsError`. As a `record`, it benefits from value-based equality and immutability, allowing straightforward comparisons and safe sharing of command outcomes.

---

## HandleAsciiSize
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


Handles an ASCII size command by delegating to the `OnSetAsciiSize` callback when present and always returning a successful `CommandResult`. It trims the incoming `args` and forwards it to the registered handler; the inline comment indicates that no-argument invocations should open the size picker, while providing an argument (such as `s`, `m`, `l` or `small`, `medium`, `large`) sets the size.

## Remarks
This method acts as a thin command adapter that decouples input parsing from the actual size-changing logic. By invoking the optional `OnSetAsciiSize` callback, it allows UI or domain logic to implement the size selection behavior while keeping the command handling surface minimal and testable. If no handler is registered (`OnSetAsciiSize` is null), the call is effectively a no-op aside from returning a success result.

## Notes
- Potential null-argument risk: if `args` is null, `args.Trim()` will throw a `NullReferenceException`. Ensure callers provide a non-null string or guard against null before trimming.


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


HandleDownloadPath asynchronously forwards a download path instruction to the registered host via the `OnSetDownloadPath` callback when available, passing the trimmed `args`. This design allows either supplying a path directly (via `args`) or prompting the host to present its native folder picker, while always returning a successful `CommandResult`.

## Remarks
This method serves as a thin adapter between the command system and the host's path-picking experience. It centralizes the decision to either apply a provided path or delegate to a UI prompt, keeping the command handler decoupled from platform specifics.

## Notes
- If `OnSetDownloadPath` is `null`, the method returns `true` without any path being set; callers should not assume a path was applied in this case.

---

## HandleServers
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<CommandResult> HandleServers()
```

**Returns:** `Task<CommandResult>`


HandleServers is a small asynchronous helper that conditionally delegates to the `OnOpenServers` hook and then returns a successful `CommandResult`. If a consumer has attached an `OnOpenServers` handler, the method awaits it; otherwise it completes immediately. This pattern keeps the command flow decoupled from the concrete implementation of opening servers while centralizing the control flow in this private handler.

## Remarks
Because `HandleServers` forwards to an optional delegate, its value is to provide a single, testable point for the open-servers workflow. It decouples the command invocation from the actual opening logic, enabling substitution of the behavior via `OnOpenServers` without changing call sites.

## Notes
- The method will return `new CommandResult(true)` after the delegate completes, so success is reported if the delegate completes without throwing.
- If `OnOpenServers` throws, the exception propagates to the caller; there is no internal exception handling in this method.
- If `OnOpenServers` is null, the method completes immediately with a successful `CommandResult`.


---