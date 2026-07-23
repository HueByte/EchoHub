# NativeFolderPicker.cs

> **Source:** `src/EchoHub.Client/Services/NativeFolderPicker.cs`

## Contents

- [NativeFolderPicker](#nativefolderpicker)
- [FolderPickResult](#folderpickresult)
- [PickerOutcome](#pickeroutcome)

---

## NativeFolderPicker
> **File:** `src/EchoHub.Client/Services/NativeFolderPicker.cs`  
> **Kind:** class

```csharp
public static class NativeFolderPicker
```


Opens the OS-native folder chooser by shelling out to platform-specific dialogs (Windows Explorer, macOS Finder, Linux GTK/KDE), allowing the TUI to remain GUI-toolkit agnostic. It dispatches to the appropriate platform helper at runtime and returns a `FolderPickResult` with a `PickerOutcome` of `Unavailable` when no native dialog can run, so callers can fall back to a configured path. Failures are caught and logged to avoid crashing the UI, and the dialog title is a fixed prompt guiding the user to select EchoHub’s download folder.

## Remarks

By shielding native dialogs behind `NativeFolderPicker`, the rest of the application stays decoupled from platform GUI toolkits, improving portability and testability. The abstraction also centralizes cross‑platform quirks (Windows PowerShell quoting, AppleScript invocation, and GTK/KDialog fallbacks) in one place, reducing duplication and ensuring a consistent user experience across environments.

## Notes

- Headless Linux environments (no `DISPLAY` or `WAYLAND_DISPLAY`) cause the picker to return `PickerOutcome.Unavailable`.
- Windows path handling escapes apostrophes in the initial directory to survive the embedded PowerShell script.
- If the user cancels the dialog or no path is selected, the result is `PickerOutcome.Cancelled` rather than an error; callers should handle this as a user action.

---

## FolderPickResult
> **File:** `src/EchoHub.Client/Services/NativeFolderPicker.cs`  
> **Kind:** record

```csharp
public sealed record FolderPickResult(PickerOutcome Outcome, string? Path)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Outcome` | `PickerOutcome` | — |
| `Path` | `string?` | — |


FolderPickResult is an immutable data container that captures the result of a native folder picker operation. It pairs the `PickerOutcome` with an optional `Path`, letting callers distinguish between a successful selection and cancellation while carrying the selected folder path only when available.

## Remarks

As a `record`, `FolderPickResult` benefits from value-based equality and supports deconstruction, enabling concise comparisons and pattern matching when consuming results from the native folder picker. It encapsulates the outcome and potential path in a single, strongly-typed value, simplifying higher-level handling and reducing the need for multiple disparate return values.

## Notes

- `Path` is nullable; validate before use and prefer accessing `Path` only when `Outcome` indicates a successful result.


---

## PickerOutcome
> **File:** `src/EchoHub.Client/Services/NativeFolderPicker.cs`  
> **Kind:** enum

```csharp
public enum PickerOutcome
{
    Chosen,

    Cancelled,

    Unavailable,
}
```


Represents the outcome of prompting the user to pick a folder via the native picker. Use it to branch logic based on whether the user selected a folder, cancelled the dialog, or the environment doesn't provide a picker.

## Remarks
By isolating the three possible results into a single enum, callers can write concise, robust code without tying their logic to UI details. The Cancelled and Unavailable outcomes allow you to differentiate between a user-initiated abort and a runtime environment where the picker isn't present, enabling graceful fallbacks. Tie the Chosen outcome to a corresponding `FolderPickResult` instance that carries the selected path in its `Path` property.

## Example
```csharp
// Example: respond to folder-picking outcomes
public void HandleOutcome(PickerOutcome outcome, FolderPickResult folderPath)
{
    switch (outcome)
    {
        case PickerOutcome.Chosen:
            Console.WriteLine($"Selected folder: {folderPath.Path}");
            break;
        case PickerOutcome.Cancelled:
            // User cancelled the dialog; no folder selected.
            break;
        case PickerOutcome.Unavailable:
            // Fall back to a non-UI flow
            break;
    }
}
```

## Notes
- Do not access `FolderPickResult.Path` when outcome is not `PickerOutcome.Chosen`.

---