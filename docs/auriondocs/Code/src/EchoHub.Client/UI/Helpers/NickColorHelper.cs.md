# NickColorHelper

> **File:** `src/EchoHub.Client/UI/Helpers/NickColorHelper.cs`  
> **Kind:** class

```csharp
public static class NickColorHelper
```


NickColorHelper deterministically assigns a stable color to every nickname, ensuring the same nick always maps to the same palette entry. This mirrors classic IRC behavior and lets busy channels stay readable without per-user configuration.

## Remarks
NickColorHelper isolates color selection from rendering logic by exposing a pure function GetPaletteIndex and GetAttribute. The palette itself is a fixed sequence of medium-saturation colors designed for legibility on both dark and light backgrounds; changing the palette order would re-color every nick and break visual consistency across sessions.

## Example
```csharp
var color = NickColorHelper.GetAttribute("Alice");
// Use `color` when rendering Alice's username in the UI
```

## Notes
- Null nick will throw; ensure non-null before calling GetPaletteIndex.
- The palette order is fixed; reordering or removing entries changes every nickname's color.
- The mapping uses a case-insensitive FNV-1a hash; changing the hash or its normalization will alter which nick gets which color.