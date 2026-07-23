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


ProfileViewDialog renders a dialog to view a user's server profile; when showing the current user's profile it also exposes action buttons (Edit Profile / Set Status) and returns the chosen ProfileAction, while viewing another user yields a read-only presentation.

## Remarks
ProfileViewDialog encapsulates all the layout and formatting decisions for a user profile in a single place. It dynamically switches between a read-only view and an ownership-aware view that surfaces actions, and it applies color theming to the status and nickname fields. By centralizing this UI behavior, the dialog remains consistent across the application and reduces duplication by isolating profile presentation from business logic. The component gracefully handles a missing profile by showing an error message and returning a Close action, which defines a clear contract for callers.

## Notes
- If invoked with a null profile, the dialog shows an error and returns ProfileAction.Close; callers should guard against null input or handle the Close result accordingly.
- The dialog title differentiates ownership with "My Profile" for the current user and "Profile — {username}" for others, and it uses color-coding helpers to reflect status and nickname color for quick visual cues.

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


ProfileAction defines the set of actions a user can select from their profile dialog: Close, EditProfile, and SetStatus. It provides a typed representation of user intent that downstream UI logic can handle in a deterministic way, rather than relying on magic strings or numeric codes.

## Remarks
ProfileAction represents the user’s chosen action from the profile dialog, allowing the UI layer to dispatch the appropriate workflow in a type-safe way. By enumerating possible intents, the code can exhaustively handle all cases in a switch or pattern-match, reducing errors from invalid values. The Close action also clarifies that the action is about dialog lifecycle control as opposed to in-dialog tasks such as editing or setting status. If new actions are required in the future, they should be added here with clear naming that maps to corresponding UI behaviors.

---