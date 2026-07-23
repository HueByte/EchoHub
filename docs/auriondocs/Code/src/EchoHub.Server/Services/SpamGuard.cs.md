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


In-memory, per-user spam protection consulted by the server-side ingress points (for example [`ChatService`](ChatService.cs.md) for messages and joins, and [`ChannelService`](ChannelService.cs.md) for channel creation). Reach for `SpamGuard` when you need a lightweight, process-local policy that enforces rate limits, duplicate-message checks, and simple escalation (auto-mute) without persisting state or inspecting decrypted content.

## Remarks
`SpamGuard` centralizes cross-protocol ingress throttling so SignalR, IRC, and other entry points share the same limits and violation tracking. State is stored only in-process (the private `_users` dictionary) and is pruned lazily (see `PruneThreshold` and `StaleAfter`) to avoid unbounded growth on busy servers. It operates on content the server already has (so for end-to-end encrypted rooms this is ciphertext) and does not perform decryption. Staff users bypass the guard (`role >= ServerRole.Mod`), rejections are recorded as violations, and repeated rejected messages inside the configured violation window can escalate to `SpamVerdictKind.AutoMute` (the escalation is evaluated only when a message is rejected).

## Notes
- State is process-local and not persisted: `SpamGuard` does not provide global or cross-instance enforcement. On a multi-server deployment, limits and violation histories are not shared between processes.
- Duplicate detection uses a simple normalization (`Trim()` + `ToLowerInvariant()`): whitespace differences and casing are ignored when comparing `content` to `UserState.LastContent`; `RepeatCount` is reset when normalized content changes.
- All checks run under the internal `Lock` (`lock (_lock)`), so `SpamGuard` is thread-safe but its callers may observe brief blocking under contention; where tests or deterministic timing are needed, use the `nowOverride` parameter to supply a fixed time.

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


SpamVerdict is an immutable value-type that conveys the outcome of a spam check. It aggregates the verdict kind (`SpamVerdictKind`), an optional `Reason` for extra context, and a `MuteDuration` that can specify how long to mute the sender when appropriate. A single, shared instance `SpamVerdict.Allowed` is provided for the common case where no action is needed, enabling callers to express acceptance without allocating a new structure.

## Remarks
SpamVerdict is a `readonly record struct`, which gives it value-based equality, structural deconstruction, and immutability. This design keeps spam-check results small and cheap to pass across boundaries, while centralizing how verdicts are represented and interpreted by the rest of the system.

## Notes
- The `Reason` is optional; code must account for `null` when presenting or logging context.

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


The `SpamVerdictKind` enum encapsulates the outcome of a spam policy evaluation performed by the `SpamGuard`. It is used to drive downstream behavior without embedding policy logic in callers: `Allowed` means the action may proceed, `Rejected` means the action is blocked, and `AutoMute` signals that the user has crossed the violation threshold — the caller should apply a timed mute.

## Remarks
This enum separates policy evaluation from enforcement, allowing a single, centralized decision point at the boundary of spam checks. Downstream code can switch on the verdict to implement appropriate behavior; the exact duration and rules of a timed mute are defined elsewhere and are not baked into this type.

## Notes
- The duration of an auto mute is not encoded in the enum; callers must resolve duration from configuration or a separate policy engine.

---