# ServerDirectoryService.cs

> **Source:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`

## Contents

- [ServerDirectoryService](#serverdirectoryservice)
  - [InfiniteRetryPolicy](#infiniteretrypolicy)
  - [ServerDirectoryService (constructor)](#serverdirectoryservice-constructor)
  - [BuildConnection](#buildconnection)
  - [DisposeConnectionAsync](#disposeconnectionasync)
  - [ExecuteAsync](#executeasync)
  - [ExtractConflictingHosts](#extractconflictinghosts)
  - [GetBackoffDelay](#getbackoffdelay)
  - [HandleRegistrationErrorAsync](#handleregistrationerrorasync)
  - [HandleRegistrationResponseAsync](#handleregistrationresponseasync)
  - [OnUserCountChanged](#onusercountchanged)
  - [ProcessUserCountUpdatesAsync](#processusercountupdatesasync)
  - [RegisterAsync](#registerasync)
  - [ResolveVersion](#resolveversion)
  - [RunConnectionLoopAsync](#runconnectionloopasync)
  - [StopAsync](#stopasync)
  - [DirectoryHubUrl](#directoryhuburl)
  - [ReconnectMaxDelay](#reconnectmaxdelay)
  - [UserCountMinInterval](#usercountmininterval)
- [DirectoryProtocol](#directoryprotocol)
- [DirectoryRegistrationErrors](#directoryregistrationerrors)
- [ErrorDetail](#errordetail)
- [RegisterServerDto](#registerserverdto)
- [RegisterServerResult](#registerserverresult)
- [Response](#response)
- [ConnectWithRetryAsync](#connectwithretryasync)
- [ReconnectBaseDelay](#reconnectbasedelay)

---

## ServerDirectoryService
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** class

```csharp
public sealed class ServerDirectoryService : BackgroundService
```


Maintains a long-lived SignalR connection to the central server directory and keeps this process advertised and up-to-date. `ServerDirectoryService` runs as a hosted background worker that connects to the directory hub at `DirectoryHubUrl`, attempts to register/claim the server identity (persisting a claim token via [`DirectoryClaimStore`](DirectoryClaimStore.cs.md)), and pushes aggregated presence (user count) updates derived from [`PresenceTracker`](PresenceTracker.cs.md) to the directory. Use this service when the application should automatically announce itself and maintain presence information in the shared directory rather than performing manual/one-off registration calls.

## Remarks
`ServerDirectoryService` is the glue between local presence tracking and the remote directory. It encapsulates the connection lifecycle (built by `BuildConnection` and managed by `ConnectWithRetryAsync` and `RunConnectionLoopAsync`), registration/claim semantics (`RegisterAsync` and `HandleRegistrationResponseAsync`), and presence propagation (`OnUserCountChanged` and `ProcessUserCountUpdatesAsync`). To avoid noisy updates the service coalesces bursts of presence changes using a single-slot [`Channel<int>`](../../EchoHub.Core/Models/Channel.cs.md) (`_userCountUpdates`) so that the most recent count wins, and it enforces a minimum send interval controlled by `UserCountMinInterval`. The service also implements an increasing reconnect backoff bounded by `ReconnectBaseDelay` and `ReconnectMaxDelay` via `GetBackoffDelay`. If the registration receives a fatal error (examples noted in comments: `HostAlreadyClaimed`, `InvalidToken`, `HostConflict`) the service sets `_registrationPermanentlyFailed` and stops attempting further register attempts for this connection — the operator must fix configuration and restart the process.

## Notes
- `OnUserCountChanged` feeds a single-slot channel so intermediate counts can be dropped; the directory will see only the latest value sent after throttling, not every intermediate change. This is by design to reduce churn.
- Presence updates are throttled by `UserCountMinInterval`; rapid updates will be coalesced and delayed to respect that interval.
- If `_registrationPermanentlyFailed` becomes true (due to registration error codes like `HostAlreadyClaimed`/`InvalidToken`/`HostConflict`), the service stops retrying registration on the current connection and on subsequent reconnects — fixing the configuration and restarting the service is required to recover.
- The implementation persists a freshly-issued claim token via [`DirectoryClaimStore`](DirectoryClaimStore.cs.md) early in the registration flow to provide a durability guarantee for first-time claims; this ordering is intentional to avoid losing a claim token on process crash.
- The startup logic yields briefly before attempting its initial connect so the host can finish starting; this affects the timing of the first registration attempt.


---

### InfiniteRetryPolicy
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** class

```csharp
private sealed class InfiniteRetryPolicy : IRetryPolicy
```


`InfiniteRetryPolicy` is a private sealed class that implements `IRetryPolicy` to provide a retry strategy. Its `NextRetryDelay` computes the next wait as 2^min(`retryContext.PreviousRetryCount`, 10) seconds and returns it, capped by `ReconnectMaxDelay`, enabling indefinite retries while bounding the maximum wait.

## Remarks
This abstraction centralizes the exponential backoff so the rest of the server's reconnection logic shares a consistent, testable delay policy. By being private and sealed, it remains an internal implementation detail, reducing surface area for change and misuse outside its containing class.

## Notes
- The backoff growth saturates after the 10th retry; `NextRetryDelay` uses `Math.Min(retryContext.PreviousRetryCount, 10)` to compute the exponent, so delays cannot grow beyond `ReconnectMaxDelay`.

---

### ServerDirectoryService (constructor)
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** constructor

```csharp
public ServerDirectoryService(
        IConfiguration configuration,
        PresenceTracker presenceTracker,
        DirectoryClaimStore claimStore,
        ILogger<ServerDirectoryService> logger)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `configuration` | `IConfiguration` | — |
| `presenceTracker` | [`PresenceTracker`](PresenceTracker.cs.md) | — |
| `claimStore` | [`DirectoryClaimStore`](DirectoryClaimStore.cs.md) | — |
| `logger` | `ILogger<ServerDirectoryService>` | — |


Constructs a `ServerDirectoryService` by binding its essential collaborators: `IConfiguration`, [`PresenceTracker`](PresenceTracker.cs.md), [`DirectoryClaimStore`](DirectoryClaimStore.cs.md), and `ILogger<ServerDirectoryService>`. Typically invoked by the dependency injection container, it assigns these dependencies to the private fields `_configuration`, `_presenceTracker`, `_claimStore`, and `_logger` so the service can access configuration, track presence, manage directory claims, and emit logs.

## Remarks
By design, this constructor is a straightforward DI-only initializer with no business logic. It simply wires the four collaborators into private fields so the rest of the service can coordinate configuration data, presence state, claim storage, and logging.

## Notes
- This constructor does not perform argument null checks; rely on the DI container to provide valid instances. If you instantiate `ServerDirectoryService` manually, consider adding guards.
- Ensure the DI container is configured to register [`PresenceTracker`](PresenceTracker.cs.md), [`DirectoryClaimStore`](DirectoryClaimStore.cs.md), and `ILogger<ServerDirectoryService>` so resolution succeeds at startup.

---

### BuildConnection
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
private HubConnection BuildConnection()
```

**Returns:** `HubConnection`


BuildConnection creates and returns a new `HubConnection` configured to communicate with the directory hub. It wires the hub URL from `DirectoryHubUrl`, enables automatic reconnection using an `InfiniteRetryPolicy`, and returns the built instance for the caller to start and use.

## Remarks
Encapsulating this setup here ensures consistent behavior across call sites that need a connection to the directory hub. The `InfiniteRetryPolicy` drives unbounded reconnect attempts, with the delay determined by `NextRetryDelay` on the `RetryContext`; callers should consider lifecycle management and potential long-running retries.

## Notes
- The returned `HubConnection` is not started automatically; you must call `StartAsync()` before use.
- Each invocation yields a new `HubConnection`; reuse the instance if a single long-lived connection is required.

---

### DisposeConnectionAsync
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
private static async Task DisposeConnectionAsync(HubConnection connection)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `connection` | `HubConnection` | — |

**Returns:** `Task`


Disposes a `HubConnection` asynchronously with a bounded timeout by awaiting `DisposeAsync()` converted to a `Task` via `AsTask()` for up to 3 seconds. If the operation exceeds the timeout or throws, the exception is caught and ignored to prevent shutdown from blocking. This private helper ensures resources are released promptly during server shutdown without risking a hang.

## Remarks
This method isolates the disposal of a `HubConnection` from the rest of shutdown logic, providing a deterministic, non-blocking path when terminating the server. By swallowing disposal failures, it avoids a slow or faulty dispose from delaying process termination, though it hides potential cleanup issues that may warrant later diagnostics. As a private static helper, it signals that disposing a given `HubConnection` is a concern tied to the server's lifecycle rather than a general-purpose cleanup utility.

## Notes
- This method swallows all exceptions from `DisposeAsync` and the timeout; consider adding logging if you need visibility into disposal problems.
- The 3-second timeout is hard-coded and may not suit every environment; make it configurable if needed.
- Caller must ensure the `connection` parameter is non-null; passing null will throw before entering the try block.

---

### ExecuteAsync
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `stoppingToken` | `CancellationToken` | — |

**Returns:** `async Task`


Coordinates the public-directory registration lifecycle for the server. It first yields to the host to finish initialization, then decides whether to register by reading `Server:PublicServer` from configuration; when enabled, it gathers metadata from `Server:PublicHosts`, `Server:Name`, `Server:Description`, `Server:Tags`, and the computed `version`, logs its intent, subscribes to user-count changes via `_presenceTracker.UserCountChanged`, and starts the connection loop with `RunConnectionLoopAsync` using `stoppingToken`. If registration is disabled or required config is missing, it logs and exits gracefully.

## Remarks
This symbol serves to encapsulate the startup flow for a publicly visible server: it centralizes the decision, metadata collection, and lifecycle management needed to register with the directory. The initial `await Task.Yield()` gives the host a chance to continue its startup sequence before any logging or network activity. The subscription to `_presenceTracker.UserCountChanged` is paired with a `finally` to guarantee cleanup and avoid leaks, even if the connection loop fails or is cancelled.

## Notes
- The code unsubscribes from `_presenceTracker.UserCountChanged` in `finally` to avoid memory leaks and stray callbacks after the connection loop ends.

---

### ExtractConflictingHosts
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
private static string[]? ExtractConflictingHosts(ErrorDetail? error)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `error` | `ErrorDetail?` | — |

**Returns:** `string[]?`


ExtractConflictingHosts pulls the `ConflictingHosts` from an error's loosely-typed `Data` payload and returns it as a `string[]` when present. It tolerates both PascalCase and camelCase keys to accommodate different serializer configurations. If the payload is missing, not a JSON object, not an array, or contains no string values, the method returns null.

## Remarks

By centralizing the JSON-payload parsing in a small helper, callers do not need to know the wiring quirks of the error data or the particular casing produced by the hub's serializer. It provides a stable, strongly-typed extraction point for host names when conflicts are reported.

## Example

```csharp
string[]? hosts = ExtractConflictingHosts(error);
```

## Notes

- Returns null if the input error is null, the `Data` payload is not a JSON object, the relevant property is missing, or the array contains no string values.
- Non-string items within the `ConflictingHosts` array are ignored; only string values are collected.
- The source snippet in the method contains a likely compile-time issue: `List<string> hosts = []` is invalid C#. It should be initialized as `new List<string>()` (or `var hosts = new List<string>();`). This is a potential trap to address during review.

---

### GetBackoffDelay
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
private static TimeSpan GetBackoffDelay(int attempt)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `attempt` | `int` | — |

**Returns:** `TimeSpan`


`GetBackoffDelay` computes the wait duration before the next reconnect attempt. Given an `attempt`, it derives the delay as `TimeSpan.FromSeconds(Math.Pow(2, Math.Min(attempt, 10)))` and returns the value capped at `ReconnectMaxDelay` as a `TimeSpan`.

## Remarks
This symbol encapsulates the reconnect retry policy within the server directory service to ensure consistent timing across retries. It employs exponential growth with a hard cap to prevent unbounded delays while avoiding overly-aggressive backoff in early attempts.

## Notes
- The exponent is capped by `Math.Min(attempt, 10)`, so delays stop growing exponentially after the 10th attempt; beyond that, the final delay is determined by `ReconnectMaxDelay`.

---

### HandleRegistrationErrorAsync
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
private Task HandleRegistrationErrorAsync(ErrorDetail[]? errors)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `errors` | `ErrorDetail[]?` | — |

**Returns:** `Task`


HandleRegistrationErrorAsync centralizes the processing of errors reported by directory registration. It marks the registration as permanently failed, derives an error code (falling back to `UnknownError`) and the set of conflicting hosts from the first error, and then logs a targeted, code-specific message before recording the failure in `_claimStore`.

## Remarks
By encapsulating this logic in one place, the method decouples error interpretation from the main registration flow. It coordinates with `_claimStore` to persist a failure snapshot and with `_logger` to surface actionable diagnostics for operators, aiding remediation. The design relies on the first error and the extracted conflicts to provide a deterministic failure narrative while supporting specific guidance for each known error code from `DirectoryRegistrationErrors` (e.g. `HostAlreadyClaimed`, `InvalidToken`, `HostConflict`, `InvalidInput`).

---

### HandleRegistrationResponseAsync
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
private async Task HandleRegistrationResponseAsync(Response<RegisterServerResult>? envelope, int userCount, string name, string[] hosts)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `envelope` | `Response<RegisterServerResult>?` | — |
| `userCount` | `int` | — |
| `name` | `string` | — |
| `hosts` | `string[]` | — |

**Returns:** `Task`


Handles the asynchronous response from the directory registration workflow. It validates the envelope is non-null, asserts the protocol version against `DirectoryProtocol.Version`, and then branches on success or failure, performing durability-oriented state updates via internal stores and logging the outcome.

## Remarks
This method centralizes response handling for directory registration: it immediately treats null envelopes, protocol mismatches, or malformed success payloads as permanent failures to avoid operating against an incompatible or corrupted directory. On success, it ensures a fresh `ClaimToken` is persisted before acknowledging the new `ServerId`, guaranteeing durability for the initial credential and consistent recovery behavior after restarts. The approach also accommodates re-registration by syncing the persisted `ServerId` when no new token is provided, keeping local state aligned with the directory.

## Notes
- If a new `ClaimToken` is supplied, it is saved prior to completing the success path; if not, the method only updates the persisted `ServerId` to reflect the directory.


---

### OnUserCountChanged
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
private void OnUserCountChanged(int newCount)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `newCount` | `int` | — |

**Returns:** `void`


Internal event handler that forwards the new user count into the single-slot update channel. It uses `_userCountUpdates.Writer.TryWrite(newCount)` to coalesce bursts of presence changes, ensuring only the latest value is observed downstream.

## Remarks
This internal abstraction decouples the producer of presence changes from the consumer by routing updates through the `_userCountUpdates.Writer` channel on a single-slot channel. The non-blocking `TryWrite` call ensures bursts of updates don't overwhelm downstream processing, preserving only the most recent value.

## Notes
- The non-blocking nature of the `TryWrite` call means bursts can be coalesced and intermediate counts may be dropped; only the latest value is observed.

---

### ProcessUserCountUpdatesAsync
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
private async Task ProcessUserCountUpdatesAsync(HubConnection connection, Task connectionClosed, CancellationToken ct)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `connection` | `HubConnection` | — |
| `connectionClosed` | `Task` | — |
| `ct` | `CancellationToken` | — |

**Returns:** `Task`


Runs an asynchronous background loop that propagates the latest observed user count to the directory service over a `HubConnection`. It listens for updates from `_userCountUpdates.Reader` and terminates when the `connectionClosed` task completes or cancellation is requested via the `ct` token. On each update, it reads the current `count`, then throttles sends to respect `UserCountMinInterval` (draining newer values during the wait so the eventual call carries the latest count). If the count hasn’t changed since the last report, or the hub is not connected, or registration has permanently failed or is not currently registered, it skips sending. When sending is appropriate, it invokes the hub method `UpdateUserCount` with the latest `count`, updates `_lastReportedUserCount` and `lastSentAt`, and logs the outcome. This pattern ensures updates are delivered efficiently, tolerate bursts, and never crash due to transient failures.


---

### RegisterAsync
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
private async Task RegisterAsync(string name, string? description, string[] hosts, string version, string[] tags)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `name` | `string` | — |
| `description` | `string?` | — |
| `hosts` | `string[]` | — |
| `version` | `string` | — |
| `tags` | `string[]` | — |

**Returns:** `Task`


RegisterAsync asynchronously registers the current server with the directory service when the hub connection is active. It exits early if the hub is not connected or if a permanent registration failure has been recorded, avoiding unnecessary work. When proceeding, it reads the current online user count from `_presenceTracker.GetOnlineUserCount()`, builds a `RegisterServerDto` with the server's `name`, `description`, `hosts`, `userCount`, `version`, `tags`, and the persisted claim token `_claimStore.ClaimToken`, and then calls the directory via `_connection.InvokeAsync<Response<RegisterServerResult>>("RegisterServer", dto)`. The envelope is then passed to `HandleRegistrationResponseAsync` to finalize the registration flow.

---

### ResolveVersion
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
private static string ResolveVersion()
```

**Returns:** `string`


Returns a human-friendly version string for the server assembly. It is a private static helper that reads the `AssemblyInformationalVersionAttribute.InformationalVersion` from the containing assembly (via `typeof(ServerDirectoryService).Assembly`) and, if present, strips any `+` suffix (git SHA) added by SourceLink before returning the value; if not present, it falls back to the assembly's `Version` as a string, and finally to the literal `0.0.0` if neither is available.

## Remarks
This tiny helper centralizes version resolution for the server, ensuring consistent display and logging of version regardless of build configuration. By extracting the informational version when available and normalizing away VCS metadata, it prevents leaking internal identifiers while still reflecting the actual package version. The implementation relies on reflection to read the version data from the containing assembly, so the produced value depends on the built assembly's metadata at runtime.

## Notes
- If the `InformationalVersion` contains a `+` (the SourceLink suffix), only the portion before `+` is returned, keeping the string human-friendly.
- If neither the informational version nor the standard assembly version is available, the method returns the literal `0.0.0` as a safe fallback.

---

### RunConnectionLoopAsync
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
private async Task RunConnectionLoopAsync(
        string serverName,
        string? description,
        string[] hosts,
        string version,
        string[] tags,
        CancellationToken stoppingToken)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `serverName` | `string` | — |
| `description` | `string?` | — |
| `hosts` | `string[]` | — |
| `version` | `string` | — |
| `tags` | `string[]` | — |
| `stoppingToken` | `CancellationToken` | — |

**Returns:** `Task`


Runs a resilient, long-running loop that maintains a connection to the directory service by repeatedly building a connection via `BuildConnection()`, wiring up `Ping`/`Heartbeat`, `Reconnected`, and `Closed` handlers, and connecting with retry through `ConnectWithRetryAsync`. On a successful connect, it registers the server with `RegisterAsync` and streams user-count updates by calling `ProcessUserCountUpdatesAsync` until cancellation or a permanent disconnection is signaled via a `TaskCompletionSource`. When a permanent close occurs or cancellation is requested, the method disposes the connection and rebuilds after a short delay.

## Remarks
The method centralizes all aspects of directory connectivity—heartbeat, re-registration, and back-to-back disconnections—into a single loop, minimizing risk of desynchronization between the server and directory state. It uses a `TaskCompletionSource` to coordinate the 'permanent close' signal so the outer loop can rebuild cleanly after a failure, and respects `_registrationPermanentlyFailed` to avoid blind re-registration after a known permanent fault.

---

### StopAsync
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
public override async Task StopAsync(CancellationToken cancellationToken)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `cancellationToken` | `CancellationToken` | — |

**Returns:** `async Task`


This `StopAsync` override extends the base stop behavior by clearing the service's internal `_connection` after the base stop completes, ensuring resources are released and the connection cannot be reused. It first awaits `base.StopAsync(cancellationToken)` to perform the standard shutdown, then sets `_connection` to `null`.

## Remarks

Clearing `_connection` after the base stop ensures there are no lingering references to an active connection once shutdown has begun. It communicates a clear lifecycle boundary for the service's connection state to its collaborators and helps GC reclaim resources.

## Notes

- Be aware that `_connection` becomes `null` after `StopAsync` completes; code that accesses `_connection` during shutdown should guard against null references or only run after shutdown is finished.

---

### DirectoryHubUrl
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** field

```csharp
private const string DirectoryHubUrl = "https://echohub.voidcube.cloud/hubs/servers"
```


Defines the immutable base URL for the directory hub used by the `ServerDirectoryService` to reach server endpoints: `https://echohub.voidcube.cloud/hubs/servers`. As a private `const`, the value is baked into the assembly, ensuring a single source of truth for hub interactions within this service.

---

### ReconnectMaxDelay
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** field

```csharp
private static readonly TimeSpan ReconnectMaxDelay = TimeSpan.FromSeconds(30)
```


ReconnectMaxDelay defines the upper bound for the delay between reconnection attempts performed by the service. Declared as a private static readonly `TimeSpan` and initialized with `TimeSpan.FromSeconds(30)`, it provides a single, immutable cap that applies to all reconnect logic within the `ServerDirectoryService`.

## Remarks
Static readonly guarantees a shared, immutable cap across all instances, ensuring the reconnect cadence remains consistent even under concurrent reconnect operations. Because the field is private, the policy cannot be adjusted from outside the class; tuning requires a code change rather than a runtime configuration.

---

### UserCountMinInterval
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** field

```csharp
private static readonly TimeSpan UserCountMinInterval = TimeSpan.FromSeconds(1)
```


This private static readonly `TimeSpan` defines the minimum interval between user-count operations inside the class, enforcing throttling to avoid rapid updates. It is initialized as `TimeSpan.FromSeconds(1)` and should be used wherever the class would otherwise perform frequent user-count recomputations to maintain consistent timing.

## Remarks
This field centralizes the throttling policy for user-count computations within the class, ensuring consistent timing across internal update paths. Making it `static` and `readonly` prevents accidental drift at runtime and communicates that the value is a fixed policy rather than dynamic state. It also makes tuning straightforward: adjust this single value to influence all user-count throttling behavior without changing multiple call sites.

## Notes
- The value is baked into the assembly; changing it requires recompilation unless the code is refactored to read from a configuration source.

---

## DirectoryProtocol
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** class

```csharp
internal static class DirectoryProtocol
```


Pinned envelope protocol version is centralized in a single constant. The value is exposed as `DirectoryProtocol.Version`, so code references a single source of truth rather than duplicating version strings, ensuring coordinated upgrades across both repositories when the envelope protocol evolves.

## Remarks
It acts as a minimal contract boundary by providing a stable, centralized version that downstream code can validate against. By routing all version bumps through `DirectoryProtocol.Version`, the codebase gains a predictable upgrade path and reduces drift between repositories.

## Notes
- Because `Version` is a `const`, its value is baked into compiled assemblies; updating it requires recompiling all dependents and coordinating updates across both repositories.
- Changes to the version must be performed in sync across both repositories to prevent a mismatch in protocol expectations.

---

## DirectoryRegistrationErrors
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** class

```csharp
internal static class DirectoryRegistrationErrors
```


DirectoryRegistrationErrors is an internal static class that defines a concise set of error-code constants used during directory registration in the EchoHub server. It provides named codes such as `InvalidInput`, `InvalidToken`, `HostAlreadyClaimed`, and `HostConflict` to represent specific failure reasons returned by the server, eliminating scattered string literals and reducing typos. It also includes client-side synthetic codes `ProtocolVersionMismatch` and `MalformedResponse`, which are generated locally for status reporting and are not emitted by the hub.

## Remarks
By centralizing these values, the codebase gains a single source of truth for directory-registration errors, simplifying error handling, testing, and mapping to user-visible messages. It distinguishes between server-disclosed error codes (the first four) and client-side diagnostics (the two synthetic codes) that help with local status reporting without being emitted by the hub.

## Notes
- Changing any constant's value is a breaking change; external or internal code that relies on the exact string value may fail after the change.
- The constants are compile-time constants; ensure all referencing code is recompiled together to avoid mismatches.
- The two client-side codes (`ProtocolVersionMismatch`, `MalformedResponse`) are for client-only diagnostics and are not emitted by the hub; avoid handling them as server-facing error payloads.

---

## ErrorDetail
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** record

```csharp
internal record ErrorDetail(string Code, string? Message, JsonElement? Data)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Code` | `string` | — |
| [`Message`](../../EchoHub.Core/Models/Message.cs.md) | `string?` | — |
| `Data` | `JsonElement?` | — |


An internal record that represents a single error entry inside a `Response<T>`. The `Code` identifies the error kind, [`Message`](../../EchoHub.Core/Models/Message.cs.md) provides an optional human-readable description, and `Data` carries an optional, loosely-typed payload as a `JsonElement` to accommodate varying error shapes (e.g. host-related errors might carry a list of conflicting hosts).

## Remarks
This abstraction decouples error signaling from concrete payload schemas by wrapping code, message, and data within a single value. It fits the `Response<T>` pattern by enabling diverse error details to accompany a common envelope, while allowing clients to switch on `Code` to interpret the `Data` payload.

## Notes
- JsonElement is a view into the underlying JsonDocument; if the document is disposed, the Data value becomes invalid. Ensure the originating `JsonDocument` remains alive as long as `ErrorDetail.Data` is accessed.
- If you need a durable payload, consider storing `Data.GetRawText()` or a deserialized DTO instead of keeping the `JsonElement` itself.

---

## RegisterServerDto
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** record

```csharp
internal record RegisterServerDto(
    string Name,
    string? Description,
    string[] Hosts,
    int UserCount,
    string Version,
    string[] Tags,
    string? ClaimToken)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Name` | `string` | — |
| `Description` | `string?` | — |
| `Hosts` | `string[]` | — |
| `UserCount` | `int` | — |
| `Version` | `string` | — |
| `Tags` | `string[]` | — |
| `ClaimToken` | `string?` | — |


RegisterServerDto is an internal C# positional record that serves as the single, strongly-typed payload for registering a server with the directory service. It captures the server's identity (``Name``), optional description (``Description``), the collection of host endpoints (``Hosts``), the current user count (``UserCount``), the software version (``Version``), a set of metadata tags (``Tags``), and an optional authentication token (``ClaimToken``). Because it is immutable and passed as a single object, it keeps registration logic clean and reduces parameter clutter across layers.

## Remarks
RegisterServerDto is internal and immutable, which helps ensure a consistent snapshot of registration data as it moves through the directory service. By bundling related fields together, it reduces coupling between components and makes validation, logging, and auditing easier. The nullable fields ``Description`` and ``ClaimToken`` reflect optional aspects of registration; consumers should handle possible nulls and token absence accordingly.

## Example
```csharp
// Example of constructing the payload for registration
var dto = new RegisterServerDto(
    "EchoServer-01",
    "Primary gateway",
    new string[] { "tcp://host1:1234", "tcp://host2:1234" },
    42,
    "2.3.1",
    new string[] { "gateway", "primary" },
    null
);
```

## Notes
- The DTO does not enforce invariants (e.g., you should ensure `Hosts` is non-empty and `UserCount` is non-negative before registration).
- The type is marked `internal`; outside of its containing assembly, code cannot construct or consume it unless test-friendly tooling like `InternalsVisibleTo` is configured.


---

## RegisterServerResult
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** record

```csharp
internal record RegisterServerResult(Guid ServerId, string? ClaimToken)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `ServerId` | `Guid` | — |
| `ClaimToken` | `string?` | — |


RegisterServerResult is an immutable value object that represents the outcome of registering a server. It contains the server's identity (`ServerId`), a `Guid`, and an optional `ClaimToken` (`string?`) that callers may use for subsequent authenticated operations.

## Remarks
This symbol acts as a focused data carrier between the registration flow and its consumers. By leveraging the `record` construct, it gains value-based equality and built-in immutability, ensuring the result is stable once created. Its `internal` visibility confines the contract to the assembly, underscoring that server registration details are an internal concern of the `ServerDirectoryService`.

## Notes
- The `ClaimToken` property can be `null` if no token is issued during registration.
- Treat the `ClaimToken` as sensitive data; avoid logging or persisting it in plain text and only keep it in memory for as long as needed.
- `RegisterServerResult` is immutable; do not mutate its properties after construction. Rely on the record's value semantics when comparing results.

---

## Response
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** record

```csharp
internal record Response<T>(bool IsSuccess, T? Data, ErrorDetail[]? Errors, string? Version)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `IsSuccess` | `bool` | — |
| `Data` | `T?` | — |
| `Errors` | `ErrorDetail[]?` | — |
| `Version` | `string?` | — |


Generic, immutable envelope that wraps every directory hub response. It exposes a boolean `IsSuccess`, an optional data payload `Data`, an optional array of `ErrorDetail` in `Errors`, and an optional `Version`. Use `Response<T>` whenever you need a consistent, hub-wide response shape instead of ad-hoc return types: place the operation’s payload in `Data`, set `IsSuccess`, attach any `Errors` if something went wrong, and optionally include `Version` for compatibility.

## Remarks
By mirroring the `EchoHubSpace` contract, this envelope centralizes response structure and simplifies client and server handling of hub results. The generic parameter `T` lets you wrap any payload while preserving a single, predictable transport form. It also separates business data from transport metadata: callers typically check `IsSuccess` first, then read `Data` or `Errors` accordingly.

## Notes
- `Data` is nullable; always guard against null when consuming `Data`.
- If `IsSuccess` is false, prefer inspecting `Errors` for failure details rather than using `Data`.
- `Version` is optional and may be omitted; treat it as informational metadata rather than a contract guarantee.

---

## ConnectWithRetryAsync
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
private async Task<bool> ConnectWithRetryAsync(HubConnection connection, CancellationToken ct)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `connection` | `HubConnection` | — |
| `ct` | `CancellationToken` | — |

**Returns:** `Task<bool>`


Tries to start the provided `HubConnection` and, on failure, retries with a backoff until the `CancellationToken` is cancelled. It returns `true` if `StartAsync` completes successfully; if the operation is cancelled before a successful start, it returns `false`.

## Remarks
By isolating this retry logic in `ConnectWithRetryAsync`, the surrounding code can rely on a single, consistent startup strategy for the directory hub. It coordinates the backoff via `GetBackoffDelay`, logs each failure with the upcoming delay, and respects cancellation through the provided `CancellationToken`.

## Notes
- The delay cancellation caveat: if the `CancellationToken` is signaled while `Task.Delay` is awaiting, an `OperationCanceledException` propagates, which means the method would surface cancellation rather than returning `false`.
- Logging: on every failed attempt, a warning is logged with the exception and the upcoming delay.
- Dependency: the retry timing depends on `GetBackoffDelay(attempt)`; callers should ensure this method yields a sensible backoff to avoid long startup times.

---

## ReconnectBaseDelay
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** field

```csharp
private static readonly TimeSpan ReconnectBaseDelay = TimeSpan.FromSeconds(2)
```


The `ReconnectBaseDelay` field defines the starting interval used by the service's reconnection logic. As a private static readonly `TimeSpan` initialized with `TimeSpan.FromSeconds(2)`, it provides a single, immutable baseline for calculating backoff delays during reconnect attempts, without exposing the value publicly. Developers thinking about the backoff strategy should consider this constant as the canonical baseline rather than sprinkling literals throughout the codebase.

## Remarks
Public exposure is avoided by keeping this value private, but the field still has architectural significance: it centralizes the base delay for the reconnect workflow within `ServerDirectoryService`, ensuring consistent timing across all retry scenarios and simplifying future tuning.

## Notes
- Changing the private static readonly `TimeSpan` will change the base backoff used by all reconnection attempts in `ServerDirectoryService`; there is no per-call override for this baseline. If configurability is required, expose a parameter or configuration option rather than modifying this field.

---