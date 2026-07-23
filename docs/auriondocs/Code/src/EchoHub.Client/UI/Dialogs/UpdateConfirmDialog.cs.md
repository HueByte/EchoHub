# UpdateConfirmDialog

> **File:** `src/EchoHub.Client/UI/Dialogs/UpdateConfirmDialog.cs`  
> **Kind:** class

```csharp
public sealed class UpdateConfirmDialog
```


UpdateConfirmDialog is a small, self-contained UI helper that presents a modal update prompt and returns the user's decision as a boolean. Call `UpdateConfirmDialog.Show` with an `IApplication` and the current and latest versions; it constructs a `Dialog` titled 'Update Available' containing a `Label` with the version message and two `Button`s, runs the dialog, and returns `true` when the user chooses to perform the update.

## Remarks

By encapsulating the entire dialog flow, this symbol isolates the update-confirmation UX from the rest of the UI, reducing duplication across the codebase. The modal pattern—calling `app.Run(dialog)` followed by `app.RequestStop()`—ensures callers receive the result synchronously without needing to manage focus or window lifecycles themselves. It also makes testing easier by providing a single, predictable entry point for the confirmation action.

## Notes

- The dialog is modal and blocks until the user presses `Update` or `Cancel`; callers should not attempt to perform further UI work until after `Show` returns.
- It interpolates `currentVersion` and `newVersion` into the message; ensure these values are safe to display and do not contain unexpected control characters.