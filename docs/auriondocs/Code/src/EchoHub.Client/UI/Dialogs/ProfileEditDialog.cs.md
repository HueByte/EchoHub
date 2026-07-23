# ProfileEditDialog.cs

> **Source:** `src/EchoHub.Client/UI/Dialogs/ProfileEditDialog.cs`

## Contents

- [ProfileEditDialog](#profileeditdialog)
- [ProfileEditResult](#profileeditresult)

---

## ProfileEditDialog
> **File:** `src/EchoHub.Client/UI/Dialogs/ProfileEditDialog.cs`  
> **Kind:** class

```csharp
public sealed class ProfileEditDialog
```


ProfileEditDialog is a Terminal.Gui dialog that presents a compact, form-based UI for editing a user's profile, including `Display Name`, `Bio`, and `Nickname Color`, with live color preview and an optional avatar picker. When invoked via `Show`, it pre-fills fields from the provided current values and returns a `ProfileEditResult?` when the user confirms, or `null` if the operation is cancelled. This component is intended to be used whenever your application needs an in-app, consistent way to collect profile updates from the user.

## Remarks
ProfileEditDialog isolates the profile-edit UX from the rest of the application, providing a single reusable route for updating these fields. It delegates color parsing to [`HexColorHelper`](../Helpers/HexColorHelper.cs.md) (e.g. `ParseHexColor`/`ParseHexToColor`) so the dialog itself remains focused on presentation and interaction. The color preview is updated in real time by wiring the `TextChanged` event on the `colorField` to `UpdateColorPreview`. The avatar picker uses an `OpenDialog` invoked through the Browse button, illustrating how file selection is integrated into a TUI form.

## Example
```csharp
var result = ProfileEditDialog.Show(app, currentDisplayName, currentBio, currentColor, notificationSoundEnabled: true, notificationVolume: 50);
if (result != null)
{
    // Use result to apply the edited profile values
}
```

## Notes
- Color parsing is performed via [`HexColorHelper`](../Helpers/HexColorHelper.cs.md) to translate the user-entered hex string into a `Color` for the live preview; invalid inputs fall back to a safe color preview.
- The avatar field is optional; leaving it empty means no avatar is selected.
- The dialog uses a fixed size of 60x26, so ensure your terminal window can accommodate this layout to avoid clipping or overflow.

---

## ProfileEditResult
> **File:** `src/EchoHub.Client/UI/Dialogs/ProfileEditDialog.cs`  
> **Kind:** record

```csharp
public record ProfileEditResult(string? DisplayName, string? Bio, string? NicknameColor, string? AvatarPath, bool? NotificationSoundEnabled, byte? NotificationVolume)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `DisplayName` | `string?` | — |
| `Bio` | `string?` | — |
| `NicknameColor` | `string?` | — |
| `AvatarPath` | `string?` | — |
| `NotificationSoundEnabled` | `bool?` | — |
| `NotificationVolume` | `byte?` | — |


Represents the data returned when the user finishes editing their profile in the dialog. It carries the proposed updates to `DisplayName`, `Bio`, `NicknameColor`, `AvatarPath`, and notification settings (`NotificationSoundEnabled`, `NotificationVolume`). Because all fields are nullable, callers can distinguish between fields the user left unchanged and fields the user explicitly updated, enabling partial updates to the profile.

## Remarks
ProfileEditResult serves as a lightweight, immutable carrier that isolates UI concerns from the underlying profile update logic. It provides a snapshot of the user's edits at dialog closure, which the caller then applies to the profile as needed. The use of nullable members communicates optional edits clearly and avoids forcing changes for fields the user did not touch.

## Notes
- Interpret any null value as 'no change' for that field when applying updates to the actual profile.

---