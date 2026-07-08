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


Persists the cluster's opaque directory claim token and the stable ServerId to disk and exposes the current registration state for operator-facing endpoints. Use this class when you need a small, on-disk, atomic store for the claim token (treated as a secret) together with an in-memory, ephemeral registration status.

## Remarks
DirectoryClaimStore keeps two concerns: a persistent PersistedClaim (claim token + ServerId) written atomically to a file, and a transient RegistrationStatus used to report the latest registration attempt (success/failure, error code, conflicting hosts) to operators. Persistence is implemented with an atomic write (temporary file + rename) and JSON serialization; the file contents should be treated as secret. Concurrent writers are serialized with an internal SemaphoreSlim and readers use Volatile reads for lock-free access to the last-known values. SaveClaimAsync is intended to be called once when a new claim token is first issued; UpdateServerIdAsync updates only the ServerId when re-registering with an existing token.

## Notes
- The on-disk file contains secrets (the claim token). Ensure filesystem permissions restrict access.
- If the file does not exist at construction, no persisted claim is loaded: ClaimToken and ServerId will be null until SaveClaimAsync persists a value.
- RegistrationStatus is ephemeral and is not persisted across process restarts; it reflects recent registration attempts only.
- SaveClaimAsync and UpdateServerIdAsync accept a CancellationToken and serialize writes with a semaphore; callers should handle IO exceptions that can still occur during atomic file operations.
- UpdateServerIdAsync is a no-op when the ServerId is already equal to the supplied value, avoiding unnecessary writes.

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


RegistrationStatus is a small, immutable value object that captures the outcome of attempting to register a directory claim with the EchoHub server. It gathers whether the registration succeeded (IsRegistered), an optional server identifier (ServerId), the timestamp of the last successful registration (LastRegisteredAt), an optional error message (LastError), and any hosts that caused a conflict (ConflictingHosts). Use this type whenever you need to pass around or inspect the result of a registration operation rather than scattering multiple nullable values.

## Remarks
RegistrationStatus is an immutable, value-based record. This means two instances with the same content compare as equal, which simplifies reasoning about registration results across boundaries. Nullable fields reflect that some information is only available for certain outcomes: ServerId and LastRegisteredAt are typically populated when IsRegistered is true, while LastError and ConflictingHosts describe failure scenarios. Consumers should guard against nulls when consuming LastRegisteredAt, ServerId, LastError, or ConflictingHosts, and use a with-expression to derive modified copies when updating the status.

## Example
```csharp
var status = new RegistrationStatus(
    IsRegistered: true,
    ServerId: Guid.NewGuid(),
    LastRegisteredAt: DateTimeOffset.UtcNow,
    LastError: null,
    ConflictingHosts: null
);
```

## Notes
- This type is immutable; to update a value, use the with-expression to create a modified copy. For example: `var next = status with { IsRegistered = false, LastError = "Conflict" };` 
- Since several fields are nullable, always null-check before use to avoid null-reference issues. 
- ConflictingHosts can be null or contain entries; treat null as "no conflicts reported" and handle non-empty arrays as potential coordination issues.

---