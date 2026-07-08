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


ProfileEditDialog is a Terminal.Gui-based modal dialog that presents a compact form for editing the current user's profile attributes: display name, bio, and nickname color. The static Show method renders the dialog initialized with optional current values and returns a ProfileEditResult when the user accepts, or null if they cancel.

## Remarks
Encapsulates the profile editing UX into a reusable component, allowing multiple parts of the application to prompt for profile changes without duplicating UI code. Color parsing is delegated to HexColorHelper to provide immediate, live feedback via the color preview as the user edits the nickname color hex. An optional notification sound preference is surfaced and carried through via Show parameters, enabling callers to respect user preferences when applying changes.

## Example
```csharp
// Example usage within a Terminal.Gui app
var result = ProfileEditDialog.Show(app,
    currentDisplayName: "Jane Doe",
    currentBio: "Software engineer",
    currentColor: "#3366FF",
    notificationSoundEnabled: true,
    notificationVolume: 60);
```

## Notes
- Returns null when the user cancels; caller should check for null before using the result.
- The color hex field is parsed by HexColorHelper; invalid input will fall back to a safe color for the live preview.
- The avatar browse button opens an OpenDialog; if a file is chosen, its path is placed into the Avatar field.

## Dependencies
- CheckState
- Attribute
- ProfileEditResult
- Terminal
- Dim
- Color
- Pos
- OpenMode

## Dependency APIs (verified signatures)
- record `ProfileEditResult` (`src/EchoHub.Client/UI/Dialogs/ProfileEditDialog.cs`)
- class [`HexColorHelper`](../Helpers/HexColorHelper.cs.md) (`src/EchoHub.Client/UI/Helpers/HexColorHelper.cs`)
  - `Attribute? ParseHexColor(string? hex)`
  - `Color ParseHexToColor(string? hex, Color fallback)`

## Symbol To Document
- Name: `ProfileEditDialog`
- Kind: class
- File: `src/EchoHub.Client/UI/Dialogs/ProfileEditDialog.cs`
- Language: csharp
- ID: 3976b1d0-03c8-4afb-aa7c-596df3bd5cf9

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


ProfileEditResult is the payload returned by the profile edit dialog, carrying optional updates for the user's DisplayName, Bio, NicknameColor, AvatarPath, NotificationSoundEnabled, and NotificationVolume. Use this object to apply only the edited fields to the user's profile; a null value for any property indicates that the corresponding field should remain unchanged.

## Remarks
Because ProfileEditResult is a C# record, it is immutable and supports structural equality, making it convenient to compare results in tests or to cache results. It functions as a lightweight data-transfer object that decouples the UI layer from the profile update logic: callers inspect non-null properties and merge them into the profile model. The with-expression feature allows creating modified copies with one or two fields different while preserving the rest.

## Notes
- Nullability semantics: null means "no change" for that field. Ensure you handle combinations correctly to avoid unintended overwrites.
- NotificationVolume is a nullable byte; when translating to a platform volume, treat the value as 0–255 or ignore if null.

---