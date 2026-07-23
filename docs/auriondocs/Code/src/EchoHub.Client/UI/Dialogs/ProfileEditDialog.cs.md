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


ProfileEditDialog provides a Terminal.Gui dialog for editing the user's profile. Its Show method presents a modal dialog titled "Edit Profile" with fields for Display Name, Bio, Nickname Color (with a hex input and a live color preview) and Avatar selection, plus notification preferences, returning a ProfileEditResult when the user accepts or null if cancelled.

## Remarks
ProfileEditDialog centralizes profile-edit UI in one reusable component, ensuring a consistent look and behavior whenever the user updates their profile. It wires up real-time color previews by updating the color swatch whenever the hex input changes, and it delegates color parsing to HexColorHelper to translate user input into a Color value. The Avatar field demonstrates integration with a file picker (OpenDialog) within a Terminal.Gui workflow, keeping file selection cohesive with the rest of the dialog.

## Notes
- The Show method accepts optional parameters for notificationSoundEnabled and notificationVolume, defaulting to false and 30 respectively.
- If no avatar is selected, avatarField.Text remains empty.
- The return type is ProfileEditResult?; callers should handle null to cover the cancel path.
- This implementation relies on Terminal.Gui primitives (Label, TextField, Button, CheckBox, OpenDialog) and collaborator types (ProfileEditResult, HexColorHelper); ensure these types are available in the consuming project.

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


Represents the data returned from the profile edit dialog. It encapsulates the user\'s optional inputs for DisplayName, Bio, NicknameColor, AvatarPath, NotificationSoundEnabled, and NotificationVolume so the caller can apply changes in a single operation. Each property is nullable: a null value means no change for that field; a non-null value provides a new value to persist.

## Remarks
ProfileEditResult is an immutable value object used as the dialog\'s return type. Its nullable fields express a delta: non-null values indicate updates, while null indicates no change. As a record, it benefits from value-based equality, making comparisons and tests straightforward, and it cleanly separates UI input from downstream update logic.

## Notes
- Null values indicate no change; apply only non-null fields when updating the profile.
- The type is immutable; to derive modifications, use a with-expression to create a new instance.


---