# NotificationSoundService

> **File:** `src/EchoHub.Client/Services/NotificationSoundService.cs`  
> **Kind:** class

```csharp
public class NotificationSoundService
```


NotificationSoundService centralizes the playback of the notification sound. It resolves the sound file from configuration (if specified and found) or falls back to a bundled default, then plays the sound at a configurable volume when requested. The service exposes SetEnabled and SetVolume for simple runtime tuning, and PlayAsync for normal operation or PlayTestAsync for QA scenarios where playback should occur regardless of the Enabled flag. Internally it uses a semaphore to serialize concurrent playback, and a 10-second timeout to prevent a stuck caller if the sound does not finish.

## Remarks
The class isolates all concerns around audio playback: path resolution, volume handling, concurrency, and fault tolerance. By hiding these details behind a single service, higher-level notification logic can simply request a sound without worrying about file presence, logging, or synchronization. The design anticipates environments where a sound file might be missing or playback might stall, and it ensures resources are released and the system remains responsive.

## Notes
- Silent fallback if a sound file cannot be found; production environments should ensure the asset exists if audible alerts are required.
- The PlaybackFinished event and the 10-second timeout guard the system against hangs; the lock may be released before the sound finishes, which means subsequent playback requests can start while a prior one is still playing.
- PlayAsync respects the Enabled flag, while PlayTestAsync allows testing the sound regardless of Enabled.