# ConfigManager

> **File:** `src/EchoHub.Client/Config/ConfigManager.cs`  
> **Kind:** class

```csharp
public static class ConfigManager
```


ConfigManager provides a thread-safe, single-point API for loading and persisting the client's configuration to disk. Use Load to read the current ClientConfig and Save/SaveServer/RemoveServer to apply changes from the UI or background tasks (for example, after token refreshes or updating saved servers).

## Remarks

ConfigManager stores the configuration in a JSON file named config.json inside a per-user directory (.echohub) under the current user's profile. All file I/O is serialized with a private lock (FileLock) to prevent concurrent access from UI threads and background tasks. When you call SaveServer, the code locates an existing SavedServer by URL (case-insensitive) and updates it, or appends a new one if none exists; RemoveServer deletes entries by URL. The design uses best-effort error handling—exceptions are swallowed to avoid disrupting the app—but this means persistence failures are not surfaced to callers unless they implement their own checks.

## Notes

- Persistence operations swallow all exceptions, making failures non-fatal but potentially leading to invisible data loss.
- SavedServers are deduplicated by URL using a case-insensitive comparison; updating an existing URL won't create a duplicate.
- ConfigDir uses Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); on systems where a user profile is unavailable or access is restricted, initialization may fall back to a default path.