# UpdateConfirmDialog

> **File:** `src/EchoHub.Client/UI/Dialogs/UpdateConfirmDialog.cs`  
> **Kind:** class

```csharp
public sealed class UpdateConfirmDialog
```


UpdateConfirmDialog is a sealed utility with a single static Show method that prompts the user to confirm an available update. It builds a small modal dialog titled “Update Available” showing the current and latest versions and offers two actions: Update (default) and Cancel; it returns true if the user chooses Update and false otherwise. The method runs the provided IApplication until the user makes a choice, using RequestStop to close the dialog and return the result.

## Remarks
It encapsulates the update-confirmation interaction as a reusable, modal prompt that coordinates with the host application's event loop, avoiding duplication of dialog boilerplate across the codebase.