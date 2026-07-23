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


Persists the directory claim token (an opaque secret issued on first registration) together with the server's stable ServerId, and exposes a short-lived RegistrationStatus used by operator-facing endpoints. Use this type when you need a simple on-disk, atomic store for the initial claim token and ServerId and also want to surface the most recent registration outcome (success or failure) for diagnostics or UI.

## Remarks
This class centralises two responsibilities: durable storage of the claim token + ServerId and an in-memory, ephemeral view of registration status. The file write uses an atomic temporary-write-then-rename strategy (so partial writes are avoided) and callers should treat the stored contents as a secret. Concurrency is handled with a SemaphoreSlim for writes and Volatile reads/writes for the in-memory references: SaveClaimAsync and UpdateServerIdAsync serialize on-disk updates while ClaimToken, ServerId and Status are safe to read without taking the write lock.

## Example
```csharp
// resolve IConfiguration and ILogger<DirectoryClaimStore> from your DI container
var store = new DirectoryClaimStore(configuration, logger);

// Persist the one-time claim token and server id (called once when first claimed)
await store.SaveClaimAsync(claimToken, serverId);

// Read back the persisted values later
var token = store.ClaimToken; // may be null until saved
var id = store.ServerId;     // may be null until saved

// Update only the ServerId when re-registering with the same token
await store.UpdateServerIdAsync(newServerId);

// Report ephemeral registration outcomes for operator UI
store.SetSuccess(serverId);
// or on failure:
store.SetFailure("ConflictError", new[] { "host-a", "host-b" });

// Inspect the last registration status
var status = store.Status;
```

## Notes
- The on-disk file is treated as a secret; protect filesystem permissions and backups accordingly.
- Status is ephemeral and kept only in memory; SetSuccess/SetFailure do not persist to disk.
- SaveClaimAsync is intended to be called once per row's lifetime (first claim). UpdateServerIdAsync is a no-op when the ServerId is unchanged.
- ClaimToken and ServerId properties may be null until a persisted value is loaded or saved.

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


RegistrationStatus is a small, immutable data container that captures the outcome of attempting to register a directory claim in the EchoHub server. It indicates whether the registration succeeded and optionally conveys the server identity, timestamp, error details, and any conflicting hosts so higher-level logic can react accordingly.

## Remarks
RegistrationStatus models a single, transportable result from a registration process. As a record, it benefits from value-based equality, making it easy to compare results across layers or to cache and reuse them. The nullable fields reflect real-world outcomes: a registration attempt may not yield a ServerId or LastRegisteredAt, and LastError plus ConflictingHosts carry additional context when registration fails or is disputed. This abstraction isolates the surface area of registration outcomes from the rest of the directory claim store, enabling consistent handling without sprinkling primitive flags throughout the codebase.

## Notes
- Nullable fields indicate optional context; always guard before accessing ServerId, LastRegisteredAt, LastError, and ConflictingHosts to avoid NullReferenceException.

---