# DirectoryClaimStore.cs

> **Source:** `src/EchoHub.Server/Services/DirectoryClaimStore.cs`

## Contents

- [DirectoryClaimStore](#directoryclaimstore)
- [RegistrationStatus](#registrationstatus)

---

## DirectoryClaimStore
> **File:** `src/EchoHub.Server/Services/DirectoryClaimStore.cs`  
> **Kind:** class

```csharp
public sealed class DirectoryClaimStore
```


Persists an opaque directory claim token and the row's stable `ServerId` to disk and exposes that data plus an ephemeral `RegistrationStatus` for operator-facing endpoints. Reach for `DirectoryClaimStore` when the process needs to remember the one-time claim token issued at first registration and to report current registration status; it handles atomic on-disk writes and concurrent access within the process so callers can read `ClaimToken`, `ServerId`, and `Status` without taking locks.

## Remarks
`DirectoryClaimStore` separates durable state (the `PersistedClaim` containing `ClaimToken` and `ServerId`) from ephemeral state (`RegistrationStatus`). Durable state is loaded once in the constructor (via configuration-resolved `FilePath`) and updated by `SaveClaimAsync` and `UpdateServerIdAsync` using an atomic write strategy (tmp file + rename). Ephemeral `Status` is updated in-memory by `SetSuccess` and `SetFailure` for operator/UI endpoints and is intentionally not written to disk. Thread-safety is achieved by using `Volatile.Read`/`Volatile.Write` for lock-free readers and a private `SemaphoreSlim` (`_writeLock`) to serialize writers; writers also perform the atomic file swap.

## Notes
- The on-disk file is treated as a secret; callers and operators should protect the `FilePath` and its contents (it contains the `ClaimToken`).
- `SaveClaimAsync` is intended to be called only once per row's lifetime (on first claim). `UpdateServerIdAsync` is used when re-registering with an existing token and is a no-op when the `ServerId` is unchanged.
- `SetSuccess` / `SetFailure` mutate only the in-memory `Status` and do not persist anything; process restarts will lose these ephemeral fields (durable `PersistedClaim` is preserved).
- Writes use an atomic tmp+rename strategy to avoid partial files, but this class does not coordinate cross-process access beyond the atomic replace; if multiple processes may write the same file concurrently, external synchronization is required to avoid races.
- I/O errors from loading or writing the backing file (e.g. permissions, disk full) will surface to callers of the write methods or during construction; callers should handle or surface those exceptions as appropriate.

---

## RegistrationStatus
> **File:** `src/EchoHub.Server/Services/DirectoryClaimStore.cs`  
> **Kind:** record

```csharp
public sealed record RegistrationStatus(
    bool IsRegistered,
    Guid? ServerId,
    DateTimeOffset? LastRegisteredAt,
    string? LastError,
    string[]? ConflictingHosts)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `IsRegistered` | `bool` | — |
| `ServerId` | `Guid?` | — |
| `LastRegisteredAt` | `DateTimeOffset?` | — |
| `LastError` | `string?` | — |
| `ConflictingHosts` | `string[]?` | — |


Represents the outcome of attempting to register a server with the directory claim store. This `record` is an immutable value type that carries the essential pieces of registration state: whether the entity is registered (`IsRegistered`), the assigned `ServerId` if one exists, the time of the last registration attempt (`LastRegisteredAt`, a `DateTimeOffset?`), an optional `LastError` describing the failure, and any `ConflictingHosts` that prevented registration. Consumers typically construct or propagate this value from the registration workflow and use it to inform callers, UI logic, or logging code rather than broadcasting multiple primitive values.

## Remarks
As a `sealed` `record`, `RegistrationStatus` provides value-based equality and immutability, making it a safe, portable summary of a registration outcome across components. The nullable members reflect that some details may be unavailable depending on the failure mode (for example, no `ServerId` if registration hasn't completed). The `ConflictingHosts` array communicates all hosts involved in a conflict, enabling callers to present a remediation path.

## Notes
- The `string[]?` `ConflictingHosts` is an array, which is mutable. If you publish this instance or cache its value, clone the array to prevent external mutation from changing the documented status.
- Nullability semantics: `ServerId`, `LastRegisteredAt`, `LastError`, and `ConflictingHosts` being `null` means the data is not available in the current outcome; interpret accordingly and avoid conflating a genuine value with absence.

---