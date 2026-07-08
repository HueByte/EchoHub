# CommandHandler.cs

> **Source:** `src/EchoHub.Client/Commands/CommandHandler.cs`

*Figure: How CommandHandler works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
flowchart TB
Start["Input string received"]
Start --> Check["CommandHandler.IsCommand(input)?"]
Check --|"false"| NotCmd["Return CommandResult(false)"]
Check --|"true"| Parse["Strip '/' and split into command and args"]
Parse --> Switch["Switch on lowercase command"]
Switch --|"status"| Status["HandleStatus: validate args → map to UserStatus (Online/Away/Dnd/Invisible) → invoke CommandHandler.OnSetStatus(UserStatus, message) → return CommandResult"]
Switch --|"theme"| ThemeNode["HandleTheme: parse arg → map to Theme → invoke CommandHandler.OnSetTheme(Theme) → return CommandResult"]
Switch --|"nick"| NickNode["HandleNick: invoke CommandHandler.OnSetNick(nick) → return CommandResult"]
Switch --|"other handlers"| Other["Invoke corresponding CommandHandler event (send, kick, ban, join, leave, etc.) → return CommandResult"]
Switch --|"unknown"| Unknown["Return CommandResult(true, 'Unknown command: /{command}. Type /help for available commands.', IsError: true)"]
Status --> End["CommandResult returned"]
ThemeNode --> End
NickNode --> End
Other --> End
Unknown --> End
```

## Contents

- [CommandHandler](#commandhandler)
- [CommandResult](#commandresult)

---

## CommandHandler
> **File:** `src/EchoHub.Client/Commands/CommandHandler.cs`  
> **Kind:** class

```csharp
public class CommandHandler
```


Parses user-typed slash commands (strings starting with '/') and routes them to well-known handlers by raising async events. Use CommandHandler when you want a small, centralized command parser that decouples text input parsing from the actual command implementations (subscribers implement the behavior by attaching to events).

## Remarks
This is a thin, event-driven command router: it recognizes a fixed set of commands (status, nick, color, theme, send, profile, avatar, servers, join, leave, topic, users, kick, ban, unban, mute, unmute, role, nuke, test-sound, quit/exit, help/?) and delegates the work to subscribers via Task-returning Func events. Handlers parse the command name and a single argument string (split on the first space), perform basic argument validation (see /status usage), then invoke the corresponding event. The class intentionally keeps parsing and invocation logic separate from command implementations so callers can attach their own async handlers.

## Example
```csharp
var handler = new CommandHandler();

// Subscribe to a couple of commands
handler.OnSetNick += async nick =>
{
    Console.WriteLine($"Set nick to: {nick}");
    await Task.CompletedTask;
};

handler.OnSetStatus += async (status, message) =>
{
    Console.WriteLine($"Status: {status}, message: {message}");
    await Task.CompletedTask;
};

// Invoke the parser
await handler.HandleAsync("/nick Alice");
await handler.HandleAsync("/status away");
```

## Notes
- The input must start with '/' — IsCommand only checks input.StartsWith('/').
- Command parsing is case-insensitive; the command token is converted with ToLowerInvariant().
- Only the first space separates command and arguments: the parser splits into at most two parts (command and the remainder as a single args string).
- Events are nullable; handlers check for null before invoking but if no subscriber is attached, no action occurs (the method typically still returns a success result or usage/error message depending on the command).
- Subscriber exceptions are not caught by the CommandHandler — subscribers run asynchronously and are awaited, so exceptions will propagate to the caller of HandleAsync.
- Some commands return structured usage or error messages (for example, /status returns a usage string when arguments are missing or invalid); unknown commands produce a CommandResult flagged as an error.

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


CommandResult is an immutable data carrier that communicates the outcome of a command-handling operation. It indicates whether the command was handled, carries an optional message suitable for UI or logging, and flags whether the outcome represents an error. Use it as the return type from a command handler to convey a unified result that can drive orchestration, user feedback, and error reporting.

## Remarks
CommandResult acts as a standardized contract between a handler and its caller. By grouping Handled, Message, and IsError into a single value, it reduces the need for multiple out-parameters and makes command-processing pipelines easier to compose and test. The record nature provides value-based equality and built-in immutability, which helps avoid accidental state changes and simplifies reasoning about results.

## Notes
- Message is nullable; guard against null before using it in UI or logs.
- A false Handled value typically signals that another handler or fallback path should be tried; orchestrators should decide how to react rather than assuming global success.
- Leverage deconstruction if you want to bind the individual fields succinctly: var (handled, message, isError) = result;

---