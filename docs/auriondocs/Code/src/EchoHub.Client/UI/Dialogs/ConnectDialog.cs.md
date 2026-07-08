# ConnectDialog.cs

> **Source:** `src/EchoHub.Client/UI/Dialogs/ConnectDialog.cs`

## Contents

- [ConnectDialog](#connectdialog)
- [ConnectDialogResult](#connectdialogresult)

---

## ConnectDialog
> **File:** `src/EchoHub.Client/UI/Dialogs/ConnectDialog.cs`  
> **Kind:** class

```csharp
public sealed class ConnectDialog
```


ConnectDialog is a Terminal.Gui-based dialog that collects the credentials required to connect to a server and authenticate. It presents an optional Saved Servers list when provided, and falls back to manual entry of the server URL, username, and display name; the password field is masked for privacy, and a Remember me option controls whether credentials should be retained for future sessions. The method Show returns a ConnectDialogResult when the user completes the flow or null if the dialog is cancelled.

## Remarks
By encapsulating both a quick-select path (via Saved Servers) and a manual-entry path, ConnectDialog provides a single, consistent UX for establishing a connection. It coordinates with SavedServer data to display recognizable entries, including an indicator when a session token exists, and it packages the user's input into a ConnectDialogResult for consumption by the caller. The use of a redacted password field and a token-aware hint label emphasizes security and flexibility around session persistence.

## Notes
- The dialog height adjusts based on whether any saved servers are supplied (22 when saved items exist, otherwise 18).
- The password is shown as a redacted placeholder in the UI (SECRET), so the real password never appears in the UI; the actual value may be captured and returned in the ConnectDialogResult depending on the caller implementation.
- The token hint label ("Session saved — password optional") is present but hidden by default; its visibility may be toggled by runtime logic to reflect session presence.

---

## ConnectDialogResult
> **File:** `src/EchoHub.Client/UI/Dialogs/ConnectDialog.cs`  
> **Kind:** record

```csharp
public record ConnectDialogResult(
    string ServerUrl, string Username, string Password,
    bool IsRegister, bool RememberMe, string? SavedRefreshToken)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `ServerUrl` | `string` | — |
| `Username` | `string` | — |
| `Password` | `string` | — |
| `IsRegister` | `bool` | — |
| `RememberMe` | `bool` | — |
| `SavedRefreshToken` | `string?` | — |


ConnectDialogResult is the data container produced when the connect dialog completes. It carries the user's inputs: the target ServerUrl, the Username and Password used for authentication, and the selection flags IsRegister and RememberMe. The optional SavedRefreshToken, when present, can be used to resume an authenticated session without re-prompting for credentials.

## Remarks
Because ConnectDialogResult is a record, it uses value-based equality and is immutable, making it ideal for transporting user input between UI and business logic without fear of unintended changes. It serves as a lightweight transfer object that downstream components can inspect or deconstruct to determine the next step in the connection or registration flow.

## Notes
- Treat Password as sensitive data: avoid logging or persisting the plaintext value.
- SavedRefreshToken is nullable; check for null before using it to authenticate.
- Validation of ServerUrl (and related fields) should occur before initiating a connection to prevent invalid requests.

---