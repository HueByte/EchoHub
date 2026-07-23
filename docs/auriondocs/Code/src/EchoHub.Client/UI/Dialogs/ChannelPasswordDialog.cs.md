# ChannelPasswordDialog

> **File:** `src/EchoHub.Client/UI/Dialogs/ChannelPasswordDialog.cs`  
> **Kind:** class

```csharp
public sealed class ChannelPasswordDialog
```


Prompts for a channel password when joining a protected channel and returns the entered password, or null if the user cancels. Use this helper whenever you need a consistent, modal password prompt instead of duplicating dialog boilerplate across join flows.

## Remarks
This class centralizes the user flow for joining password-protected channels. It presents a modal dialog titled Join #<channel>, collects the password, and returns it to the caller, ensuring a single, predictable contract. The UI avoids displaying the actual password text by using a redacted caption and automatically focusing the password field, while the dialog lifecycle is orchestrated through the application (app.Run and app.RequestStop).

## Example
```csharp
string? password = ChannelPasswordDialog.Show(app, "mychannel", "Enter password to join #mychannel.");
```

## Notes
- The method is synchronous and modal; it blocks the caller until the user completes the interaction.
- A null return value indicates the user canceled the operation. If the user submits an empty password, a brief error dialog is shown and the prompt remains active until a non-empty password is provided.