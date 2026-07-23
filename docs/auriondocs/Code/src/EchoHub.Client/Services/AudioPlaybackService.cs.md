# AudioPlaybackService

> **File:** `src/EchoHub.Client/Services/AudioPlaybackService.cs`  
> **Kind:** class

```csharp
public class AudioPlaybackService
```


AudioPlaybackService is a thread-safe wrapper around an underlying audio player that exposes asynchronous playback controls and a finished event surface. Use it when you need serialized access to play, pause, resume, or stop audio and a consistent event interface without managing locks and state machines yourself.

## Remarks
To prevent concurrent calls from interfering with playback state, the class serializes all operations using a SemaphoreSlim. When PlayAsync is invoked while something is already playing, it stops the current track before starting the new file; PauseAsync, ResumeAsync, and StopAsync perform their actions only when appropriate states are detected. The PlaybackFinished event is forwarded from the internal player, so callers can react to completion without depending on the concrete implementation of the _player. Exceptions raised by the underlying player are caught and logged with a warning, ensuring playback issues do not crash the application.

## Example
```csharp
// Example usage
var audio = new AudioPlaybackService();
await audio.PlayAsync("path/to/file.mp3");
```

## Notes
- This wrapper serializes calls to avoid race conditions; however, it is not cancellation-aware. If you need to cancel an in-flight operation, extend the class with cancellation support or a dedicated cancellation mechanism.