# UpdateConfirmDialog

> **File:** `src/EchoHub.Client/UI/Dialogs/UpdateConfirmDialog.cs`  
> **Kind:** class

```csharp
public sealed class UpdateConfirmDialog
```


Presents a modal confirmation dialog that prompts the user to confirm updating EchoHub. The static Show method constructs a small UI dialog titled "Update Available" that shows the current and latest versions and offers two actions: Update (default) and Cancel. It returns true when the user chooses Update, and false when Cancel; the method drives the surrounding application loop by requesting it to stop once a choice is made.

## Remarks
Encapsulates the update-confirmation UX into a reusable helper, decoupling the caller from the details of dialog construction and button wiring. The method is static and self-contained: it creates the dialog, wires the event handlers to flip the confirmation flag and stop the app loop, and returns the final decision after app.Run completes.

## Example
```csharp
bool shouldUpdate = UpdateConfirmDialog.Show(app, "1.0.0", "1.1.0");
if (shouldUpdate) {
    // start update process
}
```

## Notes
- Requires a live IApplication instance and a running UI loop; calling Show when no UI loop is active may have no effect.
- The Update button is designated as the default action; pressing Enter will activate it.
- The return value reflects the user’s explicit choice; the dialog ends the host UI loop via app.RequestStop() once a decision is made.