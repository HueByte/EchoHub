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


StatusDialog is a terminal-based UI component that presents a compact dialog for updating the current user's status and an optional status message. Its Show method renders the dialog initialized with the provided current status and message, and returns a StatusDialogResult when the user saves, or null if the user cancels.

The dialog consists of a title 'Set Status', a status option selector pre-populated with the current status, a text field for the status message, and Save/Cancel actions. On Save, the selected status is captured (defaulting to Online if nothing is selected) and the message is trimmed; an empty message becomes null. The method returns a new StatusDialogResult with those values and stops the application loop via app.RequestStop(); Cancel returns null and stops the loop.

Callers use the returned result to apply the updated status and message; otherwise, no changes are made.

## Remarks
StatusDialog encapsulates the presentation logic for updating user status, isolating UI concerns from business logic. It is a small, reusable piece that orchestrates Terminal.Gui controls (Dialog, Label, OptionSelector, TextField, Button) and relies on IApplication to drive the modal flow. The use of a default Online and trimming of the message ensures sane behavior even when fields are left blank.

## Example
```csharp
var result = StatusDialog.Show(app, currentStatus, currentMessage);
if (result != null)
{
    // Apply updates to the user's status and message
    currentStatus = result.Status;
    currentMessage = result.Message;
}
```

## Notes
- A null result indicates the user cancelled the dialog; callers should guard against applying changes in this case.
- If the user leaves the Message field blank or whitespace, the message is stored as null.
- The Save action is wired as the default action (IsDefault = true), and both Save and Cancel terminate the modal interaction by invoking app.RequestStop().

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


StatusDialogResult is a minimal, immutable data carrier returned when the status dialog completes. It groups the chosen user status (Status) with an optional message (StatusMessage) into a single value that downstream logic can consume without inspecting the dialog UI directly. As a C# record, it benefits from value-based equality and straightforward deconstruction.

## Remarks
StatusDialogResult encapsulates the outcome of a UI interaction into a single semantic unit that can be passed through the application flow or stored for auditing. It separates presentation concerns from business logic: callers reason about the user's status and optional message rather than UI details. The nullable StatusMessage signals that extra context is optional; consumer code should handle the absence gracefully, typically by pattern matching on Status and checking for a non-null message. The record type also supports structural equality, making tests and comparisons concise.

---