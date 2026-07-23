# AudioPlaybackService

> **File:** `src/EchoHub.Client/Services/AudioPlaybackService.cs`  
> **Kind:** class

```csharp
public class AudioPlaybackService
```


AudioPlaybackService provides a thread-safe, asynchronous facade for audio playback using a private `Player` instance. It exposes the playback state via `IsPlaying` and `IsPaused`, and it forwards a `PlaybackFinished` event when the underlying `Player` completes playback. All public operations are serialized with a private `SemaphoreSlim` named `_lock` to prevent concurrent access to the player. When you call `PlayAsync`, if something is already playing it stops it before starting the new file; `PauseAsync`, `ResumeAsync`, and [`StopAsync`](../../EchoHub.Server/Services/ServerDirectoryService.cs.md) similarly acquire the lock, perform the appropriate operation if possible, and log any exceptions with `Log.Warning`. Volume is controlled via `SetVolumeAsync`, which clamps the requested volume to a maximum of 100 using `Math.Min`.

## Remarks
This abstraction centralizes concurrency concerns and error handling around audio playback. By bridging the `Player` with a single, serialized surface, it reduces race conditions when multiple callers request playback from different parts of the application. The `PlaybackFinished` event provides a clean notification channel to consumers without exposing the internal player, enabling a decoupled UI or service layer to react to completion.

## Notes
- Exceptions during playback operations are swallowed after being logged with `Log.Warning`, so callers do not observe crashes but must rely on the logs to diagnose issues.
- All playback-related methods acquire the `_lock` semaphore, meaning long-running operations inside any call can block other playback requests and should be kept短-lived to avoid contention.
- `SetVolumeAsync` caps the volume at 100 via `Math.Min`, ensuring the underlying player never receives an out-of-range value.
