# ServerDirectoryService.cs

> **Source:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`

## Contents

- [ServerDirectoryService](#serverdirectoryservice)
  - [InfiniteRetryPolicy](#infiniteretrypolicy)
  - [BuildConnection](#buildconnection)
  - [ConnectWithRetryAsync](#connectwithretryasync)
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
  - [ReconnectBaseDelay](#reconnectbasedelay)
  - [ReconnectMaxDelay](#reconnectmaxdelay)
- [DirectoryProtocol](#directoryprotocol)
- [DirectoryRegistrationErrors](#directoryregistrationerrors)
- [ErrorDetail](#errordetail)
- [RegisterServerDto](#registerserverdto)
- [RegisterServerResult](#registerserverresult)
- [Response](#response)
- [ServerDirectoryService (constructor)](#serverdirectoryservice-constructor)
- [UserCountMinInterval](#usercountmininterval)

---

## ServerDirectoryService
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** class

```csharp
public sealed class ServerDirectoryService : BackgroundService
```


Maintains a durable, resilient registration of this process in the central server directory and continuously reports presence (user counts) to that directory. Run as a hosted BackgroundService, it opens and manages a SignalR HubConnection to the directory, performs server registration/claiming, publishes metadata (name, description, hosts, version, tags) and incremental presence updates, and automatically reconnects with backoff when the connection drops.

## Remarks
This service sits between the local PresenceTracker, a persistent DirectoryClaimStore (which holds claim tokens and server IDs), and the remote directory hub. It coalesces frequent presence changes into a single "latest wins" update using a single-slot bounded channel to avoid flooding the directory, and applies an exponential backoff on reconnect attempts to avoid tight retry loops. Certain registration failures (for example: host already claimed, invalid token, or host conflict) are treated as permanent for the lifetime of the process — once that permanent-failure state is observed the service stops attempting to register on that connection and any subsequent reconnects, leaving operator intervention required to correct configuration and restart.

## Notes
- Permanent registration failures stop further register attempts even across reconnections; the operator must fix configuration and restart the service to recover.
- Presence updates are coalesced and throttled: the single-slot channel drops intermediate values (latest wins) and sends no more often than the configured UserCountMinInterval, so short-lived fluctuations may be suppressed.
- Reconnect attempts use a backoff between ReconnectBaseDelay and ReconnectMaxDelay; expect progressively longer wait times on repeated failures.
- The service relies on configuration and on DirectoryClaimStore to persist claim tokens; ensure those dependencies are available and correctly configured or registration will fail.

---

### InfiniteRetryPolicy
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** class

```csharp
private sealed class InfiniteRetryPolicy : IRetryPolicy
```


Retrieves indefinitely with exponential backoff capped at a maximum delay for reconnect attempts. This private sealed class implements IRetryPolicy and provides a retry strategy that increases the wait time between attempts rather than failing fast, enabling resilient reconnection to the server directory service.

NextRetryDelay yields delays based on an exponential progression: the first retry occurs after 1 second, followed by 2 seconds, 4 seconds, 8 seconds, and 16 seconds. After that, the delay is capped by ReconnectMaxDelay (30 seconds), so all subsequent retries use that maximum delay. This approach balances persistence in the face of transient failures with a bound on retry timing to avoid excessive load.

## Remarks
This policy encapsulates a specific retry strategy behind the IRetryPolicy interface, isolating timing logic from the rest of the reconnection code. Marked private and sealed, it signals an internal, non-extendable implementation used solely by the server directory service's retry mechanism. The cap on delay helps prevent runaway retry intervals while still ensuring the system makes progress toward recovery.

## Notes
- The first retry delay is 1 second, not immediate.
- Delays progress as 1s, 2s, 4s, 8s, 16s, and then 30s for all subsequent retries due to the cap.
- If many clients share the same cap, consider introducing jitter at the call site to avoid synchronized retries (this policy does not include jitter by default).


---

### BuildConnection
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
private HubConnection BuildConnection()
```

**Returns:** `HubConnection`


BuildConnection constructs and returns a HubConnection configured to connect to the directory hub. It encapsulates the boilerplate of wiring the hub URL and an infinite automatic-reconnect policy, so callers can obtain a ready-to-configure connection without duplicating setup code.

## Remarks
BuildConnection centralizes the creation of the SignalR client used by the directory service, ensuring a consistent URL and reconnect policy across all call sites. By wrapping the builder steps, it reduces boilerplate and makes it easy to adjust the underlying connection strategy in one place. Note that the returned HubConnection is configured but not started; callers should invoke StartAsync (and manage its lifecycle) when ready. The attached InfiniteRetryPolicy governs how the client attempts to reconnect after a disconnect, providing resilience against transient network issues.

## Notes
- The connection is not started by BuildConnection; you must call StartAsync and later dispose of the connection to avoid leaks. Ensure DirectoryHubUrl is properly configured before using this method.

---

### ConnectWithRetryAsync
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


ConnectWithRetryAsync establishes a SignalR hub connection by repeatedly invoking StartAsync on the supplied HubConnection until the operation succeeds or the provided CancellationToken is triggered. When StartAsync completes successfully, the method returns true. If an exception occurs, the method increments its retry counter, computes a backoff delay via GetBackoffDelay(attempt), logs a warning including the delay, and awaits Task.Delay(delay, ct) before retrying. If the CancellationToken is canceled before a successful connection, the loop exits and the method returns false. This encapsulates transient-connection retry logic so callers do not have to implement their own retry loop.

## Remarks
Centralizes retry/backoff semantics for establishing the directory connection, so callers don't implement their own loop. It respects the cancellation token to avoid hanging and uses logging to surface transient failures for operators.

## Notes
- All exceptions from StartAsync are treated as retryable; there is no distinction between transient and permanent errors.
- CancellationToken is observed during both StartAsync and the subsequent Task.Delay, so cancellation is respected promptly.
- The backoff duration is determined by GetBackoffDelay(attempt); ensure this aligns with the desired backoff strategy to avoid excessively long waits or too-aggressive retries.

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


Disposes the given HubConnection asynchronously with a hard 3-second timeout and suppresses any errors, ensuring shutdown proceeds without being blocked by a slow disposal. Use this during teardown when you want to promptly release the connection without surfacing disposal failures.

## Remarks
This pattern encapsulates a best-effort disposal strategy: it waits up to three seconds for disposal to complete, then continues regardless of the outcome. By catching all exceptions, callers cannot rely on successful disposal being reported; if you need visibility into disposal failures, handle the disposal outside this helper. It is intended for shutdown scenarios where the connection must be released promptly and further operations on the connection are no longer needed.

## Example
```csharp
// Example usage within the same class
await DisposeConnectionAsync(connection);
```

## Notes
- Empty catch hides disposal failures; only use this when shutdown must not be delayed by disposal issues.
- The 3-second timeout is hard-coded; adjust if your application's shutdown window requires a different bound.

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


Executes the host’s startup lifecycle for registering the server with the EchoHubSpace directory when configured to be public. It yields to the host to finish starting, validates the configuration, collects hosts and metadata from configuration, resolves the server version, and then delegates to the connection loop that maintains the directory registration. It also subscribes to user-count changes so presence updates can be propagated while the registration is active, and guarantees cleanup of the event subscription when the operation completes or fails.

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


ExtractConflictingHosts reads a JSON payload from an ErrorDetail's Data field and returns the list of host names described under the ConflictingHosts property. It tolerates both PascalCase (ConflictingHosts) and camelCase (conflictingHosts) keys because SignalR's wire casing depends on the hub's serializer configuration, and the field is typed object?.

It returns null when the payload is missing or not a JSON object, when neither property is present, when the property isn't an array, or when the array contains no string entries. Otherwise, it returns a string[] of the host names extracted from the array.

## Remarks
The helper centralizes resilient parsing of optional error metadata, shielding callers from variations in payload shape and casing. By returning null for absent or empty data, it lets higher-level error handling distinguish between "no information" and an explicit list of conflicting hosts.

## Notes
- Returns null rather than an empty array when no hosts are present or the data is malformed.
- Non-string elements inside the host list are ignored.
- The method is private and static, indicating it is an internal helper for extracting just this piece of information from a larger error payload.

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


Calculates the exponential backoff delay for a reconnect attempt. Given the retry attempt index, it computes 2^min(attempt, 10) seconds, then clamps the result to ReconnectMaxDelay. The returned TimeSpan is used by the reconnect logic to wait before the next attempt, ensuring that retries are spaced out but never exceed a configured maximum delay.

## Remarks
Isolates the backoff policy in a small helper, keeping the retry loop simple and readable. The exponential ramp-up helps avoid overwhelming the remote endpoint while still providing progressively longer waits as failures persist; the cap guarantees a bound on wait times. This method is private to the class and intended for internal use by the directory service's reconnect flow.

## Notes
- Ensure 'attempt' is non-negative; negative values yield sub-second delays due to 2^negative, which may be surprising.
- The delay is clamped to ReconnectMaxDelay; even very large attempts cannot produce longer waits.

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


HandleRegistrationErrorAsync interprets the errors returned by the directory registration attempt, logs a code-specific message, and marks the registration as permanently failed to stop automatic retries until a restart.

## Remarks

This method centralizes the directory registration failure handling, consolidating how various error codes are surfaced to operators and how the failure state is recorded. It relies on ExtractConflictingHosts to surface any conflicting hosts and on the claim store to persist the failure details so that diagnostics and recovery decisions can reason about what happened.

## Notes

- It forces the server into a permanently failed state for registration, with log messages indicating that the server will not retry until restarted. This ensures long-running processes do not silently retry under inconsistent directory state.
- It returns a completed Task and performs its side effects synchronously (logging and state mutation) without throwing, so callers can await the result safely without handling exceptions.


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


Handles the directory registration response by validating the envelope, enforcing protocol compatibility, and performing the appropriate follow-up depending on success or failure. It ensures a durability guarantee by saving a fresh claim token before acknowledging success, otherwise updating the ServerId to keep internal state in sync, and it logs the outcome for observability.

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


OnUserCountChanged is an internal callback invoked whenever the server detects a change in the user count. It publishes the new value by writing to a single-slot channel via _userCountUpdates.Writer.TryWrite(newCount). The single-slot channel semantics coalesce bursts of updates so that only the most recent count is propagated downstream, reducing churn and avoiding repeated handling for rapid presence changes.

## Remarks
By delegating to a dedicated Writer, this method decouples the act of detecting changes from the consumers that react to them. The single-slot channel pattern ensures downstream work is debounced; observers observe the latest value after a change cycle, which is ideal for presence-aware UI updates or telemetry.

## Notes
- The call does not inspect the result of TryWrite; if the channel is full or readers slow, an update may be dropped, so downstream consumers should be able to tolerate occasional missed updates.
- This method is private to its containing type; external code should not rely on direct invocation.

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


ProcessUserCountUpdatesAsync continuously consumes user-count updates from a buffered channel and, when appropriate, reports the latest count to the directory via a HubConnection. It throttles emissions to respect a minimum interval, draining newer values during the wait so the hub receives the most up-to-date count rather than a stale snapshot, and it exits gracefully on cancellation or connection closure; updates are only sent when the hub is connected and the local registration is valid.

## Remarks
This method decouples producers of user-count data from the actual update path by reading from a channel and emitting to the directory hub only when ownership conditions are met. The drain-on-wait policy ensures the directory reflects the latest state under bursty updates without flooding the hub, and the preconditions (connected hub and valid registration) guard presence reporting within the lifecycle of the system.

## Notes
- Updates may be dropped under bursty traffic due to throttling and the drain loop; callers should not rely on every intermediate update being observed by the hub.
- If the hub is temporarily unavailable or registration is not established, updates are skipped until conditions are met; there is no automatic backoff beyond the local catch logging.
- Cancellation via the provided CancellationToken causes an immediate exit from the loop; ensure callers signal cancellation during shutdown to avoid lingering tasks.

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


Registers the current server with the directory service over a live hub connection. If connected and not permanently failed, it collects the online user count, builds a RegisterServerDto containing the server's name, description, hosts, user count, version, tags, and the current claim token, and sends it to the directory via the hub's RegisterServer method. The response is processed by HandleRegistrationResponseAsync to apply the result locally. Any exceptions are caught and logged as warnings with no propagation to the caller.

## Remarks
Encapsulates the server-registration handshake behind a private method, so callers don't have to manage DTO construction or hub invocation directly. It coordinates with _presenceTracker for live user counts and with _claimStore to carry the persisted claim token across registrations. The guard checks against the hub's connection state and a permanent-failure flag reflect the intended lifecycle: registration only runs when viable, preventing noisy or duplicate attempts.

## Notes
- Exceptions are swallowed; the method logs a warning and does not rethrow, so callers cannot rely on exceptions for control flow and may need separate retry logic.
- On the first-ever registration, ClaimToken is null; after the first successful claim it is persisted and reused on subsequent registrations.
- If not connected or if a permanent failure has been recorded, the method returns immediately without attempting registration.

---

### ResolveVersion
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
private static string ResolveVersion()
```

**Returns:** `string`


Resolves the version string used to identify the running ServerDirectoryService assembly. It prefers AssemblyInformationalVersionAttribute.InformationalVersion, but strips any SourceLink git SHA suffix (for example '0.2.10+abc123') before returning; if that attribute isn't present, it falls back to the assembly version, and finally to '0.0.0' if neither is available.

## Remarks
This centralizes version resolution for the ServerDirectoryService, ensuring a consistent, human-friendly version string for diagnostics, logging, and server identity without leaking VCS details. It prefers source-controlled metadata when possible, but gracefully degrades to a stable default when it's not.

## Notes
- Strips the SourceLink git SHA suffix by locating the '+' and returning the prefix portion only.
- If neither informational version nor assembly version is available, the method returns '0.0.0'.
- Uses a private static helper scope; ensure tests align with the private context and the assembly hosting the symbol remains ServerDirectoryService's assembly.

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


## Source Code
Runs a resilient, long-running loop that manages the lifecycle of a connection to a directory service. It repeatedly builds a connection, wires in heartbeat and re-registration logic, and, when the connection is permanently closed or cancellation is requested, cleanly tears down and rebuilds the connection to maintain availability.

## Remarks
This method centralizes the connection lifecycle management, coordinating connection establishment, heartbeat handling, re-registration on reconnect, and clean disposal. It relies on a cancellation token and a TaskCompletionSource to synchronize asynchronous events across the loop, enabling robust recovery paths while preserving a consistent registration state.

## Notes
- If ConnectWithRetryAsync(connection, stoppingToken) returns false, the method exits, causing the outer loop to terminate and the service to stop attempting a reconnect.
- The On("Ping") handler sends a heartbeat back to the directory and safely logs any heartbeat failures without crashing the loop.
- When the connection is permanently closed, the Closed event signals completion via the TaskCompletionSource and the outer loop proceeds to rebuild after a short delay, unless cancellation has been requested.

## Dependencies
- TaskCompletionSource
- TaskCreationOptions
- Task

## Dependency APIs
- TaskCompletionSource (non-generic)
  - Constructor: TaskCompletionSource(TaskCreationOptions options)
  - Property: Task Task { get; }
- TaskCreationOptions (enum)
  - Member used: RunContinuationsAsynchronously
- Task (System.Threading.Tasks.Task)
  - Represents an asynchronous operation; used for awaiting and coordinating async work

## Symbol To Document
- Name: RunConnectionLoopAsync
- Kind: method
- File: src/EchoHub.Server/Services/ServerDirectoryService.cs
- Language: csharp
- ID: a28a6f2b-23c7-4de0-bb5e-3da24401feb3

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


Override of StopAsync performs the base shutdown logic and then clears the internal _connection to release the resource and reflect that the service is disconnected.

## Remarks
Ensures the derived service participates in the lifecycle by letting the base stop routine complete before releasing its own resources. Clearing _connection after the base stop prevents reuse of an active connection during shutdown and marks the service as disconnected for the rest of the system.

## Notes
- If base.StopAsync throws, _connection will not be cleared; consider wrapping the cleanup in a finally block to guarantee cleanup.

---

### DirectoryHubUrl
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** field

```csharp
private const string DirectoryHubUrl = "https://echohub.voidcube.cloud/hubs/servers"
```


This private constant string DirectoryHubUrl holds the base URL for the servers directory hub used by ServerDirectoryService. It is initialized to https://echohub.voidcube.cloud/hubs/servers and should be referenced wherever the directory hub endpoint is needed, ensuring a single source of truth and avoiding string duplication.

## Remarks
By centralizing the hub URL in a single private constant, the class avoids scattering the endpoint string across multiple methods. This reduces the risk of inconsistent paths and simplifies maintenance if the hub address changes; it also makes the code more testable by isolating the configuration-like value in one place.

## Notes
- The value is baked into the assembly as a private const; it cannot be overridden at runtime. For environment-specific endpoints, consider configuration-driven access and testing hooks to swap or mock the value.

---

### ReconnectBaseDelay
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** field

```csharp
private static readonly TimeSpan ReconnectBaseDelay = TimeSpan.FromSeconds(2)
```


ReconnectBaseDelay is a private static readonly TimeSpan that defines the base wait time used by ServerDirectoryService when retrying a failed connection. By centralizing this 2-second base delay, the code avoids magic numbers and provides a single point to tune the retry cadence across all reconnection attempts.

## Remarks
Centralizing the base delay enforces a uniform retry cadence and simplifies tuning during incidents or tests. Being static and readonly ensures the value is shared across all instances and cannot be changed at runtime, which preserves predictable timing in concurrent reconnection scenarios. If you later introduce a more sophisticated backoff strategy (for example, exponential backoff with jitter), this base delay would typically feed that mechanism rather than replace it.

## Notes
- Changing this value affects all reconnection retries across the service; it's a global constant for the directory service.
- Because it is private, external code or tests cannot override it directly; consider configuration or making it injectable if runtime tunability is required.

---

### ReconnectMaxDelay
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** field

```csharp
private static readonly TimeSpan ReconnectMaxDelay = TimeSpan.FromSeconds(30)
```


Defines the upper bound for the delay between reconnection attempts. The field is private, static, and readonly, initialized as TimeSpan.FromSeconds(30). It is used by the server’s internal reconnection logic to cap backoff durations, ensuring retry intervals remain bounded even under transient network issues.

## Remarks
Centralizes reconnection policy within ServerDirectoryService to ensure consistent retry timing across all attempts. Being private and readonly, this value is not exposed to external components and cannot be modified at runtime, promoting predictable behavior and easier maintenance. The 30-second cap prevents excessively long delays during outages while avoiding overly aggressive retry loops.

## Notes
- The maximum delay is fixed after class initialization; changing it requires code edits and a recompilation.
- Private scope ensures external code cannot depend on or bypass this policy.

---

## DirectoryProtocol
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** class

```csharp
internal static class DirectoryProtocol
```


DirectoryProtocol is a small, internal helper that exposes the current envelope protocol version used by the server's directory communications. The Version constant holds the protocol version as a string ("1.0"), and serves as a single source of truth for compatibility checks. Bumps to this version are coordinated across both repositories to keep envelope formats aligned during client/server exchanges.

## Remarks
By centralizing the protocol version, this type makes explicit when envelope formats may evolve and prevents drift between the two sides. It also clarifies where to pull the version for any envelope construction or validation, reducing the risk of duplicating literals across the codebase.

## Notes
- Do not hard-code '1.0' in multiple places; reference DirectoryProtocol.Version instead.
- This class is internal; its Version member is only accessible to code within the same assembly, so cross-repo coordination relies on the shared build/packaging process.

---

## DirectoryRegistrationErrors
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** class

```csharp
internal static class DirectoryRegistrationErrors
```


This internal static class DirectoryRegistrationErrors serves as a centralized collection of string constants that represent the standard error codes used during the EchoHub server's directory registration workflow. It helps avoid hard-coded literals spread across the codebase and provides a single source of truth for the messages the hub uses to classify and surface registration failures. The constants cover common causes like invalid input, invalid token, host state conflicts, as well as client-side synthetic codes used for status reporting (ProtocolVersionMismatch, MalformedResponse) which are not emitted by the hub.

## Remarks
The class is internal to the server assembly and centralizes canonical error codes for the directory registration subsystem to promote consistent error handling across components. Keeping these strings in one place reduces typos and mismatches in error reporting and mapping. Note that ProtocolVersionMismatch and MalformedResponse are client-side synthetic codes documented here for parity; they are never emitted by the hub.

## Notes
- Not accessible from outside the server assembly; if you need client-visible error codes, expose a separate contract instead.

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


ErrorDetail represents a single error entry that can appear inside a `Response<T>` as part of an API error payload. It carries an error code (Code), an optional human-readable message (Message), and an optional Data payload for error-specific details. The Data field is typed as JsonElement to keep the payload shape flexible, accommodating different errors with varying detail structures (for example, a host-related error might include a ConflictingHosts array). As a record, ErrorDetail benefits from value-based equality and immutability, which makes it a stable, serializable unit for error reporting across API boundaries.

## Remarks
ErrorDetail's design separates the error signaling (via Code) from the optional payload (Message and Data), enabling clients to react to known codes while optionally surfacing human-readable context or structured details. It works alongside the surrounding `Response<T>` wrapper to assemble a consistent error surface while preserving flexibility in the Data payload. The JsonElement Data keeps the detail shape decoupled from the type system, at the cost of requiring clients to inspect Code before interpreting Data.

## Example
```csharp
using System.Text.Json;

var json = "{\"ConflictingHosts\":[\"host1\",\"host2\"]}";
JsonElement data = JsonSerializer.Deserialize<JsonElement>(json);

var error = new ErrorDetail(
    Code: "ConflictingHosts",
    Message: "One or more hosts conflict with existing entries.",
    Data: data
);
```

## Notes
- Data is loosely-typed by design; clients should first inspect Code to determine how to interpret Data.  
- If Data is null, the consumer should rely on Code and optional Message for context.

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


RegisterServerDto is a compact, immutable data transfer object (record) that carries all the information required to register a server in the EchoHub server directory. It groups identity data (Name, Version), optional metadata (Description), network endpoints (Hosts), current user load (UserCount), and classification tags (Tags) into a single value object so callers supply a single payload to the directory service rather than wiring multiple fields through separate calls. The optional ClaimToken supports claim-based authorization when needed.

## Remarks
RegisterServerDto exists to encapsulate the registration data in a single cohesive unit, decoupling the producer from the directory service and enabling consistent validation and persistence. As a record, it provides value-based equality, which helps determine duplicates or idempotent operations across registration attempts.

## Example
```csharp
var dto = new RegisterServerDto(
    Name: "EchoServer-01",
    Description: "Primary gateway for region A",
    Hosts: new[] { "https://host1.example.com", "https://host2.example.com" },
    UserCount: 128,
    Version: "2.3.1",
    Tags: new[] { "production", "gateway" },
    ClaimToken: null
);
```

## Notes
- The Hosts and Tags properties are string[] arrays; their contents can be mutated after construction since arrays are mutable. If you need true immutability, consider defensive copies or using a read-only collection type in a different design.

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


An internal, immutable data carrier that represents the outcome of registering a server in EchoHub's directory service. It holds the assigned ServerId and an optional ClaimToken returned alongside the registration result. Callers construct this type at the end of a registration flow to pass both pieces of information together, rather than returning them separately.

## Remarks
Designed to decouple the registration outcome from the service logic and to expose a stable, immutable snapshot of the operation. By using a record, it gains value-based equality and built-in deconstruction, which makes testing and wiring across layers straightforward. The optional ClaimToken acknowledges scenarios where a token is not issued; consumers should handle its absence gracefully.

## Example
```csharp
var serverId = Guid.NewGuid();
var result = new RegisterServerResult(serverId, "token-abc");
// result.ServerId == serverId
// result.ClaimToken == "token-abc"
```

## Notes
- ClaimToken is nullable; callers should guard against null before use.
- The type is internal, so it is not part of the public API surface outside its assembly.
- Being a record, it supports deconstruction (e.g., `var (id, token) = result;`) and value-based equality, which aids comparisons and pattern-based usage.

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


Generic envelope wrapper for directory hub responses. It mirrors the EchoHubSpace contract by wrapping a success indicator, an optional payload, optional errors, and an optional version string into a single, transport-safe object.

## Remarks
This envelope isolates transport concerns from business logic by providing a uniform surface for responses. Callers should always check IsSuccess before using Data, and rely on Errors for details when it is false. Data and Version are nullable, so consumers must guard for nulls and treat Version as optional metadata rather than a payload. The generic T makes this wrapper reusable for any payload.

## Example
```csharp
var response = new Response<string>(true, "directory listing", null, "1.2");
```

## Notes
- When IsSuccess is false, Data may be null; always inspect the Errors collection for failure details.
- The symbol is internal to its assembly; to share the envelope across boundaries, you may need a public abstraction or converter on your side.

---

## ServerDirectoryService (constructor)
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


This constructor wires ServerDirectoryService by receiving four dependencies through dependency injection and storing them in private fields. It prepares the service for directory-related operations by providing access to configuration, presence tracking, claim storage, and logging.

## Remarks
The constructor enforces that ServerDirectoryService cannot operate without configuration, presence tracking, claim storage, and a logger, making its dependencies explicit and testable. It sits at the boundary between configuration and domain logic, coordinating the infrastructure pieces that support directory management.

## Notes
- Ensure all dependencies are registered in the application's DI container; missing registrations will cause the service resolution to fail at runtime.
- If any dependency requires specific lifetimes (e.g., scoped vs singleton), align them with the composition root to avoid disposal issues or lifetime mismatches.

---

## UserCountMinInterval
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** field

```csharp
private static readonly TimeSpan UserCountMinInterval = TimeSpan.FromSeconds(1)
```


Defines a shared, immutable throttling interval used by ServerDirectoryService to regulate how often user-count related work runs. The field is private, static, and readonly, initialized to TimeSpan.FromSeconds(1). This ensures a consistent cadence and avoids magic numbers scattered through the class; it's consulted wherever the service needs to debounce or rate-limit user-count updates.

## Remarks
By centralizing the timing policy in this single member, the class achieves consistent behavior across all usages and simplifies future adjustments. The static readonly combination guarantees that the interval is computed once at type initialization and remains the same for the lifetime of the application, minimizing race-condition risk when read from multiple threads. Because it is private, external callers cannot bypass or alter the cadence; any changes must go through the class logic and a rebuild.

## Notes
- This is not a compile-time constant; it is evaluated at type initialization and cannot be reassigned afterwards.
- External configuration at runtime is not possible unless the class provides a mechanism to override it.
- If cadence needs to vary by environment or load, consider externalizing to configuration or making the interval configurable instead of editing code.

---