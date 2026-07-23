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


Opens the OS-native folder picker by shelling out to the host OS, keeping the TUI free of GUI toolkit dependencies. It supports Windows, macOS, and Linux by delegating to platform-specific helpers and returns a FolderPickResult that communicates whether a folder was chosen, the dialog was cancelled, or the native picker is unavailable so the caller can fall back to a configured path.

## Remarks

NativeFolderPicker centralizes cross-platform behavior for obtaining a folder path without pulling in a GUI toolkit. It hides OS differences behind a single entry point, PickFolderAsync, and exposes a uniform result type (FolderPickResult with a PickerOutcome) that callers can inspect to either proceed with the chosen path or fall back to defaults. Failures are caught and logged, ensuring graceful degradation rather than exceptions propagating to the UI.

## Notes
- Linux will not attempt a graphical picker if no graphical session is detected (DISPLAY or WAYLAND_DISPLAY are missing); in that case, the method returns Unavailable.
- On Windows, the initial directory is sanitized (apostrophes are doubled) to safely embed the path in the PowerShell script, and PowerShell is invoked via an encoded command to avoid quoting issues.
- If the platform-specific helper cannot be started, the code falls back to returning Unavailable instead of throwing, allowing callers to implement their own fallback strategy.

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


FolderPickResult is an immutable data carrier that represents the outcome of a native folder-picking operation and, when successful, the path of the selected folder.

## Remarks
Because FolderPickResult is a record, it benefits from value-based equality and straightforward pattern matching when consumed by calling code. The Path member is nullable to reflect that a folder may not be selected; always check the Outcome before using Path. This abstraction decouples application logic from platform-specific picker implementations, promoting testability and cross-platform compatibility.

## Example
```csharp
var result = new FolderPickResult(PickerOutcome.Success, @"C:\Projects");
if (result.Outcome == PickerOutcome.Success && result.Path is not null)
{
    Console.WriteLine(result.Path);
}
```

## Notes
- Path may be null when Outcome indicates cancellation or failure; always verify Outcome before accessing Path.

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


PickerOutcome encodes the result of attempting to display a native folder picker. It defines three mutually exclusive states: Chosen (the user picked a folder and FolderPickResult.Path is set), Cancelled (the native dialog ran but no selection was made), and Unavailable (no native picker is available on the current machine).

Use this enum to drive post-pick logic without scattering platform checks or error handling across call sites.

## Remarks
This enum serves as a lightweight sum type for the outcome of a folder-picking operation. It centralizes decision points and pairs with FolderPickResult to obtain the actual path when Chosen is returned. Consumers can implement a fallback flow for Unavailable and provide a smooth user experience when Cancelled.

## Notes
- Unavailable is not an error; it indicates the absence of a native picker and warrants a fallback strategy (e.g., a non-native picker or manual path entry).

---