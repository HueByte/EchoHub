# SpamOptions

> **File:** `src/EchoHub.Server/Config/SpamOptions.cs`  
> **Kind:** class

```csharp
public sealed class SpamOptions
```


SpamOptions is a configuration class bound to the 'Spam' config section that stores all anti-spam thresholds. It centralizes rate limits, duplicate suppression, auto-mute behavior, and onboarding quotas so enforcement logic can apply consistent rules; adjust these values here rather than hard-coding them throughout.

## Remarks
SpamOptions acts as the configuration contract for anti-spam behavior. It centralizes all thresholds so the enforcement and moderation subsystems can apply consistent rules without hard-coded values scattered through the codebase. It coordinates rate limiting, duplicate suppression, auto-mute behavior, and first-join/channel-creation limits via a single, testable object that can be configured at startup.

## Example
```csharp
var options = new SpamOptions
{
    Enabled = true,
    MaxMessagesPerWindow = 12,
    WindowSeconds = 10,
    MaxDuplicateMessages = 2,
    AutoMuteMinutes = 10,
    ViolationThreshold = 6,
    ViolationWindowMinutes = 3,
    MaxJoinsPerWindow = 30,
    JoinWindowSeconds = 20,
    MaxChannelCreatesPerWindow = 2,
    ChannelCreateWindowMinutes = 15
};
```

## Notes
- Auto-mute is disabled when `AutoMuteMinutes` is 0; rejections still apply.
- The first-join burst behavior relies on `MaxJoinsPerWindow` being large enough for your public channel count.
- These values are loaded from the config and may be adjusted to balance user experience against protection needs.