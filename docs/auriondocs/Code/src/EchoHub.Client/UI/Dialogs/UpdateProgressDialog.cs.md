# UpdateProgressDialog

> **File:** `src/EchoHub.Client/UI/Dialogs/UpdateProgressDialog.cs`  
> **Kind:** class

```csharp
public sealed class UpdateProgressDialog
```


UpdateProgressDialog encapsulates a lightweight terminal GUI dialog that presents a progress bar and status text during a software update. It composes a Dialog with a Label and a ProgressBar, wires in an IApplication to drive the UI, and exposes simple methods to update progress, show the dialog, and close the dialog loop. Use this class when you want a focused, reusable progress UI for an update flow instead of scattering raw UI calls across the updater logic.

## Remarks
By isolating the progress UI behind this class, callers need only interact through UpdateProgress and Show, keeping the update logic decoupled from presentation details. The IApplication abstraction enables testability and platform-agnostic rendering; swapping it with a test double lets you drive progress without a real UI loop. Note the constructor creates a Cancel button but never wires it into the dialog, which looks like an incomplete cancellation path; this may indicate a planned feature or a leftover artifact.

## Example
```csharp
IApplication app = /* provided by host */;
var dlg = new UpdateProgressDialog(app, "1.4.0");
dlg.UpdateProgress(0.25f, "Downloading update...");
dlg.Show();
```

## Notes
- The Cancel button is instantiated but not added to the dialog, so user-initiated cancellation is not wired in this implementation.
- Calling Close() triggers RequestStop on the IApplication, which may terminate the entire UI loop; use it to end the update flow only when appropriate (e.g., after a successful update or a user-initiated cancel).
