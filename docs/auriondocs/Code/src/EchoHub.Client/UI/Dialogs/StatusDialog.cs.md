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


StatusDialog is a Terminal.Gui-based dialog that enables a user to set their [`UserStatus`](../../../EchoHub.Core/Models/UserStatus.cs.md) and an optional status message. The static `Show` method displays the dialog within an `IApplication`, initializes the controls from `currentStatus` and `currentMessage`, and returns a `StatusDialogResult` when the user saves, or `null` if the dialog is cancelled.

## Remarks
StatusDialog serves as a focused UI primitive that isolates status-edit behavior from the rest of the application. By wiring `OptionSelector<UserStatus>` and a `TextField` to a lightweight `StatusDialogResult`, it provides a predictable, reusable pattern for collecting user input and converting it to a simple value object. This keeps the UI code cohesive while allowing the caller to handle the result without managing Terminal.Gui lifecycle details. The dialog is deliberately minimal and self-contained, relying on the provided `IApplication` to control its lifecycle.

## Notes
- The `message` field is trimmed and, if empty or whitespace, stored as `null`.
- Cancelling returns `null` and no `StatusDialogResult` is produced.
- When saving, if the selected status is `null`, it defaults to `UserStatus.Online`.


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


StatusDialogResult is a lightweight value object that represents the outcome of the status dialog. It pairs the chosen [`UserStatus`](../../../EchoHub.Core/Models/UserStatus.cs.md) with an optional `StatusMessage`, providing a simple, transportable result for the caller to inspect and react to.

## Remarks
As a `record`, it uses value semantics: two instances are equal if their `Status` and `StatusMessage` are equal, and it is immutable by design. This makes it ideal for passing the result across boundaries and for use in pattern matching or switch expressions when reacting to different statuses. The `StatusMessage` is nullable to allow callers to omit extra context when not needed.

---