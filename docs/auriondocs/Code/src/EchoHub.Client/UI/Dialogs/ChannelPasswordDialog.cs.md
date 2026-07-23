# ChannelPasswordDialog

> **File:** `src/EchoHub.Client/UI/Dialogs/ChannelPasswordDialog.cs`  
> **Kind:** class

```csharp
public sealed class ChannelPasswordDialog
```


ChannelPasswordDialog is a lightweight UI helper that prompts the user for the password required to join a password-protected channel. Its static `Show` method returns the entered password as a `string?`, or `null` if the user cancels, after presenting a small modal dialog built from `Dialog` with a channel-specific message (defaulting to `#{channelName} is password protected.`).

## Remarks

Encapsulates the password-prompt UX for channel joins, avoiding duplication of UI logic across callers. The dialog wires up a password input and two actions: a join action that validates a non-empty password and a cancel action that returns `null`, ensuring the caller proceeds only after a password is provided or the user cancels. Providing a custom `message` lets callers tailor the prompt while preserving a consistent default behavior when none is supplied.

## Notes

- The call is synchronous and blocks until the user completes interaction with the dialog.
- The return value must be checked for `null` to distinguish between a canceled join and a provided password.
- The implementation relies on UI primitives (`Dialog`, `Label`, `Button`, `MessageBox`) and a password input field; ensure this is invoked on an appropriate UI thread context in your application.
