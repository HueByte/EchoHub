# SpamOptions

> **File:** `src/EchoHub.Server/Config/SpamOptions.cs`  
> **Kind:** class

```csharp
public sealed class SpamOptions
```


SpamOptions is a configuration object that encapsulates the anti-spam thresholds used by the server. It is bound from the Spam config section and exposes the toggles and numeric limits that govern how the system enforces per-user rate limits, duplicate message handling, auto-muting behavior, and the protections around first-time channel joins and channel creation. The defaults are intentionally lenient so a fast typist won’t trip them, and moderators (and above) are exempt from these protections. Use this class to adjust spam-protection policy without changing code.

## Remarks
SpamOptions centralizes policy decisions for anti-spam enforcement, serving as a single source of truth for the thresholds consumed by the spam protection subsystem. By binding to configuration, it keeps rules out of hard-coded logic and enables runtime tuning via the Spam section. The design separates concerns across rate limiting (per-user messages), duplicate detection, auto-mute behavior, and early channel-join/channel-create protections, making it easier to tune each facet without collateral impact. The auto-mute behavior ties into the existing moderation tooling (MuteExpirationService), illustrating cohesive behavior with the broader user-suspension lifecycle. The note about end-to-end encrypted rooms clarifies that identical plaintext can yield different ciphertext, so the duplicate-detection rule may not apply in those contexts.

## Notes
- Auto-mute is controlled by AutoMuteMinutes. Setting AutoMuteMinutes to 0 disables auto-mute (rejections still apply if thresholds are reached). 
- MaxMessagesPerWindow and WindowSeconds govern per-user message rate; adjust them with awareness of your typical user pacing to avoid false positives. 
- MaxJoinsPerWindow and JoinWindowSeconds apply to first-time channel joins; joins to channels the user already belongs to do not count toward the limit, ensuring normal reconnects don’t trigger protections.
