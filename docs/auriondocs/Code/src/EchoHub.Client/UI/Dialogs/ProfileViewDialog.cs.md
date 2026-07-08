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


ProfileViewDialog encapsulates the UI for displaying a user's profile in a dialog window within the EchoHub client. It provides two entry points: Show for viewing another user's profile in read-only mode, and ShowOwn for displaying the current user's profile with action buttons to edit the profile or set status. Internally, it constructs a dialog showing fields such as Username, Display Name, Status (live for the current user, stored for others), an optional Status Message, Nickname Color, and Bio, and returns a ProfileAction when used via ShowOwn.

## Remarks
ProfileViewDialog centralizes the presentation of a user profile as a reusable UI component. It hides the low-level dialog assembly behind a clean API, ensuring consistent theming for status and nickname colors while handling nuances like live versus stored status display. By exposing a ShowOwn pathway that returns a ProfileAction, it provides a straightforward mechanism to react to user choices (e.g., editing the profile or updating status) without leaking UI details to call sites.

## Example
```csharp
// Example usage: show current user's profile with actions and handle the result
var action = ProfileViewDialog.ShowOwn(app, profile, currentStatus, currentStatusMessage);
// React to the user action if needed (Close indicates the dialog was dismissed without choosing an explicit action)
```

## Notes
- Show (static) displays the profile but does not return a value; use ShowOwn if you need to know what the user selected.
- If the provided profile is null, the dialog shows an error message ("User not found.") and exits with a Close action; callers should not rely on a meaningful return value in this path.
- Nickname color is optional; if the hex color cannot be parsed, the label simply uses the default styling (no custom color applied).


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


Represents the concrete action a user chooses in the ProfileViewDialog. Use this enum to drive post-selection behavior (closing the dialog, navigating to edit profile, or opening a status picker) instead of sprinkling ad-hoc event payloads or string constants throughout the UI code.

## Remarks
This enum provides a compact, type-safe contract between the dialog UI and the action handlers. By centralizing the possible user actions, it reduces branching logic scattered across the UI and makes it easier to test and reason about the profile flow. When new actions are needed, they should be added here to preserve a single-source-of-truth for profile-related user intents.

## Notes
- Ensure all enum values are handled in switch statements; consider adding a default case to guard against unknown values.
- Do not repurpose these actions for unrelated UI flows; use the enum strictly for profile dialog actions.

---