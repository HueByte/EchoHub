# ProfileViewDialog.cs

> **Source:** `src/EchoHub.Client/UI/Dialogs/ProfileViewDialog.cs`

## Contents

- [ProfileViewDialog](#profileviewdialog)
- [ProfileAction](#profileaction)

---

## ProfileViewDialog
> **File:** `src/EchoHub.Client/UI/Dialogs/ProfileViewDialog.cs`  
> **Kind:** class

```csharp
public sealed class ProfileViewDialog
```


ProfileViewDialog encapsulates the UI for inspecting a user's server profile in a terminal-style dialog. It renders a read-only view when displaying another user, and when shown for the current user via `ShowOwn`, it includes action buttons (edit profile and set status) and returns the chosen `ProfileAction`.

## Remarks
Internally, `Show` delegates to `ShowInternal` with `isOwnProfile` set to false, while `ShowOwn` passes `isOwnProfile` true along with the current status and message. The dialog is constructed as a `Dialog` with title `My Profile` or `Profile — {profile.Username}`, and it populates rows for `Username`, `Name`, `Status`, [`Message`](../../../EchoHub.Core/Models/Message.cs.md) (when present), `Color`, and `Bio` using `Label`s and a `TextView`. The status value is chosen as the live status when viewing your own profile, otherwise the stored status from the profile; the status text is produced by `FormatStatus` and the color by `GetStatusColor`. The nickname color is parsed via `HexColorHelper.ParseHexColor` and applied as a scheme to the color label when available. If the provided `profile` is `null`, it shows an error dialog with `MessageBox.ErrorQuery` and returns `ProfileAction.Close`.

## Notes
- If `profile` is `null`, the dialog informs the user and returns `ProfileAction.Close`, signaling callers to handle the absence gracefully.
- The nickname color is applied only when `HexColorHelper.ParseHexColor(profile.NicknameColor)` yields a valid color attribute; otherwise the color styling is skipped, avoiding exceptions.


---

## ProfileAction
> **File:** `src/EchoHub.Client/UI/Dialogs/ProfileViewDialog.cs`  
> **Kind:** enum

```csharp
public enum ProfileAction
{
    Close,
    EditProfile,
    SetStatus
}
```


The `ProfileAction` enum encodes the concrete actions a user selects from their profile dialog. Its values `Close`, `EditProfile`, and `SetStatus` map user intent to distinct application paths, replacing ad-hoc strings with a strongly-typed signal. Consumers use this enum in the dialog result handling to drive navigation and state changes without inspecting UI text.

---