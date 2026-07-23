# NickColorHelper

> **File:** `src/EchoHub.Client/UI/Helpers/NickColorHelper.cs`  
> **Kind:** class

```csharp
public static class NickColorHelper
```


NickColorHelper deterministically maps a nickname to a color attribute for users who haven't picked a nickname color. The same nick always maps to the same palette entry (classic IRC client behavior), so a busy channel stays scannable without any configuration. Use `GetAttribute(string nick)` to obtain the color `Attribute` to apply to UI elements, with the color chosen from a fixed `Palette` in a deterministic way. The helper is a pure function (no Terminal.Gui types) so it is easy to unit-test without a display driver.