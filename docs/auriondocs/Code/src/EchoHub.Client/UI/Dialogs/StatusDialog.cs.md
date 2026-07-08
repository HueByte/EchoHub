# StatusDialog.cs

> **Source:** `src/EchoHub.Client/UI/Dialogs/StatusDialog.cs`

## Contents

- [StatusDialog](#statusdialog)
- [StatusDialogResult](#statusdialogresult)

---

## StatusDialog
> **File:** `src/EchoHub.Client/UI/Dialogs/StatusDialog.cs`  
> **Kind:** class

```csharp
public sealed class StatusDialog
```


A Terminal.Gui dialog for setting the user's status and status message.

The StatusDialog class exposes a single entry point, Show, that presents a modal UI for selecting a user status and optionally entering a message. Call this method from a UI workflow whenever you need to collect or update the current status and its accompanying text. It returns a StatusDialogResult with the chosen status and message when the user saves, or null if the user cancels. The dialog pre-populates the status selector with currentStatus and the message field with currentMessage. Before returning, it trims the message; if the result is an empty or whitespace-only string, the message is stored as null. This encapsulation keeps the UI concerns isolated from business logic and ensures a consistent, keyboard-friendly layout for status updates across the application.

## Remarks

Wrapping the status-update UI in StatusDialog provides a consistent, reusable UX and a single source of truth for how status information is collected. It hides Terminal.Gui component wiring (dialog, labels, input fields, and buttons) behind a simple Show method, so callers don’t need to manage layout or lifecycle details. The defaulting behavior (falling back to Online when no status is provided) and the cancel path are centralized, reducing edge-case handling scattered through the UI code.

## Example

```csharp
var result = StatusDialog.Show(app, currentStatus, currentMessage);
if (result != null)
{
    // Persist result.Status and result.Message as needed
    var updatedStatus = result.Status;
    var updatedMessage = result.Message; // may be null
}
```

## Notes

- If the user enters only whitespace for the message, the value is normalized to null.
- The dialog defaults to UserStatus.Online when no status is selected at save time.
- The method returns null to indicate cancellation; callers should handle this as a no-op.


---

## StatusDialogResult
> **File:** `src/EchoHub.Client/UI/Dialogs/StatusDialog.cs`  
> **Kind:** record

```csharp
public record StatusDialogResult(UserStatus Status, string? StatusMessage)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Status` | [`UserStatus`](../../../EchoHub.Core/Models/UserStatus.cs.md) | — |
| `StatusMessage` | `string?` | — |


StatusDialogResult is the immutable value object returned by the status dialog in the EchoHub client UI. It carries two pieces of information: the user-selected status (Status, of type UserStatus) and an optional message (StatusMessage) that provides additional context. Implemented as a C# positional-record, it supports value-based equality and convenient deconstruction, enabling callers to treat the dialog outcome as a single, self-describing result.

## Remarks
This abstraction reduces API surface by bundling the two related pieces of data into one return type. The record's immutability ensures that once produced by the dialog, the result's contents cannot be changed, preventing accidental mutations as the result propagates through the code. Deconstruction makes it easy to read the two components separately (Status and StatusMessage) without needing separate properties in the caller.

## Example
```csharp
StatusDialogResult result = new StatusDialogResult(default(UserStatus), null);
var (status, message) = result;
// Use status and optional message as needed
```

## Notes
- StatusMessage is nullable; check for null before using it.
- As a record, StatusDialogResult provides value-based equality; two instances with identical Status and StatusMessage compare equal.
- The deconstruction syntax is a convenient way to extract both components in a single, readable statement.

---