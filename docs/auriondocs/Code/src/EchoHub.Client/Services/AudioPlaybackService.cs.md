# AudioPlaybackService

> **File:** `src/EchoHub.Client/Services/AudioPlaybackService.cs`  
> **Kind:** class

```csharp
public class AudioPlaybackService
```


Provides a thread-safe, high-level controller for audio playback backed by a concrete Player. It exposes state through IsPlaying and IsPaused, forwards a PlaybackFinished event, and offers asynchronous methods to PlayAsync, PauseAsync, ResumeAsync, StopAsync, and SetVolumeAsync. All operations are serialized with a semaphore to prevent race conditions when the caller triggers multiple commands concurrently, and any exceptions encountered during playback are logged rather than rethrown for resilience in UI scenarios.

## Remarks
Acts as a thin service layer that decouples consumers from the underlying Player implementation. By centralizing serialization of commands, it prevents overlapping Play/Stop/Pause sequences that could leave the player in an inconsistent state. The PlaybackFinished event propagation ensures callers can react to completion without coupling to the internal Player type.

## Notes
- Exceptions are swallowed with Log.Warning; callers won’t observe failures via exceptions, which may obscure errors unless you inspect logs.
- Volume values greater than 100 are safely clamped to 100, preventing invalid volume levels from being sent to the underlying player.
