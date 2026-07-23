# NotificationSoundService

> **File:** `src/EchoHub.Client/Services/NotificationSoundService.cs`  
> **Kind:** class

```csharp
public class NotificationSoundService
```


NotificationSoundService coordinates playback of the application's notification sound using a configurable file path and volume. It exposes `PlayAsync` for normal operation (respecting the `Enabled` setting) and `PlayTestAsync` to audition the sound regardless of that setting; internally it resolves the sound path, applies the configured volume, and uses a `SemaphoreSlim` lock plus a timeout (`PlaybackTimeout`) to avoid blocking future notifications.

## Remarks

Architecturally, this class centralizes notification sound behavior so callers don't need to touch the `_player` or handle `PlaybackFinished` events directly. It encapsulates path resolution: first a user-configured path (`_config.SoundFile`), if present and exists, else a bundled default at `Path.Combine(AppContext.BaseDirectory, "Assets", "Notification.mp3")`. The combination of a serializing lock (`_lock`) and a guarded finish path ensures only one sound plays at a time and that resources are released promptly even if playback misbehaves.

The playback flow subscribes to `_player.PlaybackFinished` and uses a `TaskCompletionSource` to await either completion or the timeout; this design guarantees the lock is released even if playback misfires or completes synchronously.

## Notes

- If no valid sound file is found, notifications will be silent (log: "No notification sound file found — notifications will be silent").
- `PlayAsync` will early-return if `_config.Enabled` is false or `_resolvedSoundPath` is null; `PlayTestAsync` will still return early if `_resolvedSoundPath` is null. Both rely on a correctly resolved path to function.
- The `_lock` is released in a `finally` block to guarantee progress even when exceptions occur.