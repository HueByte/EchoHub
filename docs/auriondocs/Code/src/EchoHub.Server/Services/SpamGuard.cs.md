# SpamGuard.cs

> **Source:** `src/EchoHub.Server/Services/SpamGuard.cs`

## Contents

- [SpamGuard](#spamguard)
- [SpamVerdict](#spamverdict)
- [SpamVerdictKind](#spamverdictkind)

---

## SpamGuard
> **File:** `src/EchoHub.Server/Services/SpamGuard.cs`  
> **Kind:** class

```csharp
public sealed class SpamGuard
```


An in-memory, per-user spam protection component used by the server to enforce rate limits and duplicate-content rules across all ingress protocols (SignalR, IRC, etc.). SpamGuard centralizes checks for message sends, channel joins and channel creations so the same limits apply regardless of how a user interacts with the system. Its state is process-local (not persisted) and it delegates configuration to the supplied SpamOptions; the Enabled property exposes whether checks are active.

## Remarks
SpamGuard exists to provide a single place for applying and counting spam-related events so different services (for example ChatService for messages/joins and ChannelService for channel creation) share the same view of a user's recent activity and violations. It keeps compact per-user state (queues of timestamps and a small duplicate-detection cache) and prunes stale entries lazily to avoid unbounded memory growth on busy servers. The class performs all checks under a private lock, so callers do not need to synchronize access; moderators (role >= ServerRole.Mod) are exempted from checks.

## Notes
- State is process-local and not persisted: restarts or multi-process deployments will reset per-user counters; auto-mutes are recorded in the normal mute store (outside this class).
- Time sources default to DateTimeOffset.UtcNow but can be overridden via the optional nowOverride parameter (useful for deterministic testing).
- Flood detection counts every attempt (including retries), and uses a sliding window configured via SpamOptions (Prune before enqueueing and compare against MaxMessagesPerWindow).
- Duplicate detection compares trimmed, case-insensitive content to the immediately previous message only (back-to-back duplicates); RepeatCount is incremented for consecutive repeats and compared to MaxDuplicateMessages.
- Only rejected actions are recorded as violations for escalation; a non-rejected (clean) message cannot by itself trigger an auto-mute. The escalation logic (violations -> SpamVerdictKind.AutoMute) is evaluated only on rejected messages.

---

## SpamVerdict
> **File:** `src/EchoHub.Server/Services/SpamGuard.cs`  
> **Kind:** record

```csharp
public readonly record struct SpamVerdict(SpamVerdictKind Kind, string? Reason = null, TimeSpan MuteDuration = default)
{
    public static readonly SpamVerdict Allowed = new(SpamVerdictKind.Allowed);
}
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Kind` | `SpamVerdictKind` | — |
| `Reason` | `string?` | `null` |
| `MuteDuration` | `TimeSpan` | `default` |


SpamVerdict is an immutable value-type that conveys the outcome of a spam check. It carries a Kind from SpamVerdictKind to describe the verdict, an optional Reason for explaining the result, and a MuteDuration indicating how long to suppress further messages when applicable. Defined as a readonly record struct, it benefits from value-based equality and guarantees immutability across boundaries. A convenient static instance, SpamVerdict.Allowed, represents the common case where a message passes spam checks without penalty.

## Remarks
Because the verdict is packaged as a single object, this abstraction lets the rest of the system reason about spam results without ad-hoc boolean flags scattered through the code. It fits into the SpamGuard workflow by serving as a single, transportable payload that downstream components can inspect via Kind and optionally read the Reason or respect the MuteDuration. The immutability of the type helps prevent accidental mutations once a verdict has been created.

## Example
```csharp
// Common usage: treat messages as allowed by the spam guard
var verdict = SpamVerdict.Allowed;
```

## Notes
- Reason is nullable; when present, it should be used for diagnostics or logs rather than for control flow. If you need extra context, supply Reason; otherwise leave it null.
- MuteDuration defaults to TimeSpan.Zero. To mute a user or channel for a period, provide a non-zero duration.
- SpamVerdict.Allowed is a convenient singleton for the common allowed case, but it does not encode a Reason or a non-zero MuteDuration. If you need those metadata, construct a new SpamVerdict explicitly.

---

## SpamVerdictKind
> **File:** `src/EchoHub.Server/Services/SpamGuard.cs`  
> **Kind:** enum

```csharp
public enum SpamVerdictKind
{
    Allowed,
    Rejected,

    AutoMute,
}
```


SpamVerdictKind enumerates the possible outcomes of a spam evaluation. It is used by enforcement logic to decide whether to allow, reject, or mute a user based on the spam check; AutoMute indicates the caller should apply a timed mute.

## Remarks
By centralizing these verdicts, the codebase can route enforcement consistently without scattering string literals or magic numbers. The AutoMute value communicates a specific consequence (a timed mute) that callers should implement, decoupling the decision from the actual enforcement mechanism. This abstraction helps evolve spam-policy over time while keeping evaluation and enforcement loosely coupled.

## Notes
- AutoMute carries no duration or scope; the enforcement layer must supply the mute length and target(s).
- There is no payload attached to the verdict; if more context is needed (e.g., risk score, user ID), pass it separately alongside the verdict.
- Be mindful when updating this enum; adding values requires updating all readers to handle new cases.

---