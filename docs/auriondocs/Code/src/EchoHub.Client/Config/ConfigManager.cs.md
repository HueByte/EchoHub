# ConfigManager

> **File:** `src/EchoHub.Client/Config/ConfigManager.cs`  
> **Kind:** class

```csharp
public static class ConfigManager
```


ConfigManager is a static helper that persists and retrieves the application's client configuration from a JSON file under the user's home directory. It provides Load, Save, SaveServer, and RemoveServer methods to centralize path handling and JSON settings, offering best-effort persistence so the application remains resilient to IO errors.

## Remarks
ConfigManager centralizes all config I/O, including the path, directory creation, and JSON formatting, so callers don’t duplicate boilerplate. SaveServer updates or appends a server by URL (case-insensitive) and saves the result, ensuring a consistent list of saved servers. The implementation uses best-effort error handling; failures during Load or Save are swallowed to avoid crashing the app.

## Notes
- Silent catches mean failures to read or write config may go unnoticed; persistence is best-effort and may not reflect on-disk state.
- RemoveServer uses a case-insensitive URL comparison, which helps avoid duplicates due to casing but may silently merge logically identical URLs written with different casing.