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


ConnectDialog is a Terminal.Gui-based dialog that collects server connection details and authentication information for the application. When shown, it can display a list of SavedServer entries at the top if any saved servers are provided; in that case a Saved Servers section is rendered with a ListView of display names that indicate whether a session exists (the code appends a [session] marker when a RefreshToken is present). Below (or in place of it, when there are no saved servers), the dialog presents manual entry fields for Server URL (default http://localhost:5000), Username, and Password, along with UI hints such as a hidden password placeholder and a Remember me option. Additional fields include Display Name and, when relevant, an Invite Code for invite-gated registrations. The static Show method returns a ConnectDialogResult when the user completes the dialog, or null if the dialog is cancelled; the dialog height is adjusted depending on whether saved servers are shown.

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


ConnectDialogResult encapsulates all user input gathered from the connect dialog as a single, immutable value. It is produced when the dialog completes and is consumed by the rest of the application to initiate a connection flow, passing the server URL, credentials, and onboarding flags as a single, strongly-typed package.

## Remarks

By collecting all related fields into a single record, this abstraction reduces coupling between the UI layer and the connection logic. It clearly expresses the intent of the user's action (login vs register) and whether credentials should be remembered, while allowing optional data (DisplayName, InviteCode) to participate in specialized flows without forcing callers to thread every field separately.

## Example

```csharp
// Common usage: construct a result from values collected in UI
var result = new ConnectDialogResult(
    ServerUrl: "https://example.server/api",
    Username: "alice",
    Password: "P@ssw0rd",
    IsRegister: false,
    RememberMe: true,
    SavedRefreshToken: null,
    DisplayName: "Alice",
    InviteCode: "INVITE-2024-ABCD"
);
```

## Notes

- DisplayName and InviteCode are nullable; omit them or pass null if not applicable.
- Password should be treated as sensitive data: avoid logging it or persisting it longer than necessary, and ensure proper disposal or clearing after use.
- SavedRefreshToken may be null; handle accordingly in login/refresh flows.
- This record is intended for in-memory transfer between UI and authentication/connection logic; when persisting or transmitting, apply appropriate security measures and avoid leaking confidential fields.


---