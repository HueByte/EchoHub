# ConfigManager

> **File:** `src/EchoHub.Client/Config/ConfigManager.cs`  
> **Kind:** class

```csharp
public static class ConfigManager
```


ConfigManager is a static helper that persists the client configuration to a JSON file under the user's profile directory and provides focused APIs for loading, saving, and managing saved servers. It centralizes file I/O behind a private lock to serialize access from UI actions and background tasks (token refresh, room keys, last-read checkpoints), helping prevent race conditions that could corrupt the config.

Use `ConfigManager.Load()` to obtain the current configuration (or a default [`ClientConfig`](ClientConfig.cs.md) when the file is missing or unreadable), modify the returned object, and persist changes with `ConfigManager.Save(config)`.

To manage saved servers, use `ConfigManager.SaveServer(...)` to upsert by `Url` and `ConfigManager.RemoveServer(string url)` to delete by `Url` (case-insensitive).

## Remarks

All file I/O performed by `ConfigManager` is guarded by a single static lock (the private `Lock` named `FileLock`), ensuring reads and writes do not interleave across threads. The design favors resilience: a missing or unreadable config yields a fresh [`ClientConfig`](ClientConfig.cs.md), and save errors are swallowed to avoid crashing the host process. When upserting or removing saved servers, the code compares the server URLs using a case-insensitive match (`StringComparison.OrdinalIgnoreCase`), so entries differing only by casing do not duplicate and removals reliably locate targets.

## Notes

- Saves are best-effort; any exception during persistence is swallowed so callers should not depend on hard failures for user feedback. 
- If the config file is absent, the directory is created and a default [`ClientConfig`](ClientConfig.cs.md) is used when loading. 
- URL-based operations for saved servers use case-insensitive matching to maintain a consistent, deduplicated set.