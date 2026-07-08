# NotificationSoundService

> **File:** `src/EchoHub.Client/Services/NotificationSoundService.cs`  
> **Kind:** class

```csharp
public class NotificationSoundService
```


NotificationSoundService plays a configured notification sound using a lightweight player, orchestrating path resolution and synchronized playback. It avoids blocking calls by serializing playback with a semaphore and employing a timeout so that a stuck playback can't prevent future notifications.

## Remarks
Centralizes the notification-audio behavior behind a single service so callers don't need to manage playback details. It resolves the sound path from configuration or falls back to a bundled default, and it serializes playback with a semaphore to ensure only one sound plays at a time. A completion source plus a finite timeout guards against hangs if the audio hardware misbehaves, ensuring PlayAsync returns promptly.

## Notes
- If no sound file can be resolved (neither the configured path nor the bundled file exists), playback is effectively silent and the methods exit without throwing.
- ResolveSoundPath runs during construction; updates to the config's SoundFile after construction are ignored until a new instance is created.