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


ConnectDialog is a Terminal.Gui dialog that gathers server connection and authentication information from the user. It optionally presents a Saved Servers list when available, and returns a `ConnectDialogResult?` when the user completes the form or null if cancelled.

## Remarks
By encapsulating the authentication flow in a single dialog, `ConnectDialog` centralizes the user experience for establishing a server connection. It dynamically adapts its layout depending on whether [`SavedServer`](../../Config/ClientConfig.cs.md) entries are provided, showing a `ListView` of saved servers when present and keeping a compact form otherwise. It also treats credentials with care by redacting the password in the UI and indicating a saved session when a `RefreshToken` exists.

## Notes
- If saved servers exist, the dialog height increases to accommodate the list (24 vs 20).
- The Saved Servers display shows items built from saved server properties; a session indicator is appended when `RefreshToken` is non-empty.
- The password field is displayed as `[REDACTED:PASSWORD]` and the actual input is masked via the `Secret` flag.

---

## ConnectDialogResult
> **File:** `src/EchoHub.Client/UI/Dialogs/ConnectDialog.cs`  
> **Kind:** record

```csharp
public record ConnectDialogResult(
    string ServerUrl, string Username, string Password,
    bool IsRegister, bool RememberMe, string? SavedRefreshToken,
    string? DisplayName = null, string? InviteCode = null)
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
| `DisplayName` | `string?` | `null` |
| [`InviteCode`](../../../EchoHub.Core/Models/InviteCode.cs.md) | `string?` | `null` |


ConnectDialogResult is an immutable data container produced by the connect dialog, encapsulating the user's input as a single value object for the subsequent connection/authentication workflow. It carries the server URL (`ServerUrl`), the user's credentials (`Username`, `Password`), and UI preferences (`IsRegister`, `RememberMe`), along with an optional `SavedRefreshToken` and possibly `DisplayName` or [`InviteCode`](../../../EchoHub.Core/Models/InviteCode.cs.md).

## Remarks
Using a `record` here provides value-based equality and convenient deconstruction, making it easy to compare results and pass them through layers without mutating state. It serves as a boundary-crossing DTO that formats UI input into a coherent package for the authentication/service layer, while supporting optional flows via `DisplayName` and [`InviteCode`](../../../EchoHub.Core/Models/InviteCode.cs.md). Because `Password` and `SavedRefreshToken` can contain sensitive data, avoid logging them and handle this object as transient UI data rather than a durable model.

## Notes
- Do not log or persist the `Password` or `SavedRefreshToken` values; treat them as sensitive data.
- This object is intended to be transient UI input; avoid storing it longer than necessary or serializing it insecurely.

---