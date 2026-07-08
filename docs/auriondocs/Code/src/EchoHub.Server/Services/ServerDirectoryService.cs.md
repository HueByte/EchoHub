# ServerDirectoryService.cs

> **Source:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`

## Contents

- [ServerDirectoryService](#serverdirectoryservice)
- [DirectoryProtocol](#directoryprotocol)
- [DirectoryRegistrationErrors](#directoryregistrationerrors)
- [InfiniteRetryPolicy](#infiniteretrypolicy)
- [ErrorDetail](#errordetail)
- [RegisterServerDto](#registerserverdto)
- [RegisterServerResult](#registerserverresult)
- [Response](#response)
- [ServerDirectoryService (constructor)](#serverdirectoryservice-constructor)
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
- [UserCountMinInterval](#usercountmininterval)

---

## ServerDirectoryService
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** class

```csharp
public sealed class ServerDirectoryService : BackgroundService
```


A hosted BackgroundService that keeps this process registered in the central directory (the directory hub) and continuously reports presence information (notably the current user count). Use this service to let the cluster advertise a server instance, maintain its claim token, and push live presence updates without wiring SignalR registration, retry and throttling logic into application code.

## Remarks
This service centralizes lifecycle concerns for directory registration: it builds and maintains a HubConnection to the directory hub, performs connect-with-retry using a backoff policy, persists and reuses claim tokens via the DirectoryClaimStore, and coalesces rapid presence changes so the directory is updated at a controlled rate. It listens to PresenceTracker updates and uses a single-slot channel (latest-wins) plus a minimum-interval throttle to avoid floods, and it records a permanent registration failure state when the hub returns irrecoverable errors so the service stops tight-retrying until an operator intervenes.

## Notes
- The user-count channel is single-slot with DropOldest semantics: bursts of presence changes are coalesced and only the most recent count is guaranteed to be sent.
- If the service observes a registration error classified as permanent (e.g. host already claimed / invalid token / host conflict), it sets a flag and stops attempting registration on this connection and on subsequent reconnects; an operator restart is required after fixing configuration.
- Presence updates are throttled by UserCountMinInterval — callers should expect updates to be delayed or coalesced rather than delivered instantly for every change.
- Connection attempts use exponential backoff bounded by ReconnectBaseDelay and ReconnectMaxDelay; callers should not rely on immediate reconnection after transient failures.

---

## DirectoryProtocol
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** class

```csharp
internal static class DirectoryProtocol
```


DirectoryProtocol is a small internal static helper that exposes the pinned envelope protocol version through the Version constant. It centralizes the envelope protocol value so that version bumps can be coordinated across both repositories without drift. Use DirectoryProtocol.Version wherever you need to compare or gate logic based on the envelope protocol version, instead of duplicating the literal throughout the code.

## Remarks

By keeping the version in a single, compile-time constant, the code provides a lightweight mechanism for compatibility checks. Be aware that const values are inlined by the compiler; bumping this value requires rebuilding all consuming assemblies to propagate the new version.

## Example

```csharp
// Example: verify compatibility with the envelope protocol
if (DirectoryProtocol.Version != "1.0")
{
    throw new InvalidOperationException("Unsupported envelope protocol version.");
}
```

## Notes
- This constant is inlined by the compiler; bumping it requires rebuilding all dependent assemblies to avoid mismatches.

---

## DirectoryRegistrationErrors
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** class

```csharp
internal static class DirectoryRegistrationErrors
```


DirectoryRegistrationErrors is an internal static class that defines a small, centralized collection of string constants used to represent standardized error codes in the directory registration flow of EchoHub.Server. The constants cover common failure scenarios (InvalidInput, InvalidToken, HostAlreadyClaimed, HostConflict) and two client-side synthetic codes (ProtocolVersionMismatch, MalformedResponse) used solely for local status reporting. Developers should reference these constants rather than sprinkling literal strings, which helps ensure consistency, reduces typos, and makes it easier to evolve error semantics in one place.

## Remarks
Centralizing error codes here ensures consistent handling across validation, registration logic, and status reporting, and it makes unit tests and logging checks more reliable by avoiding string-duplication. The class is marked internal, signaling that these codes are server-internal details and are not part of any public API contract. If new error states are needed, they should be added here to preserve a single source of truth for server-side error semantics.

## Example
```csharp
// Example: map an input validation failure to a canonical code
string code = DirectoryRegistrationErrors.InvalidInput;

if (code == DirectoryRegistrationErrors.InvalidInput)
{
    // Route to a user-visible validation message or error response
}
```

## Notes
- Do not rely on these constants from external assemblies since the class is internal; external clients should not assume access to or visibility of these codes.
- ProtocolVersionMismatch and MalformedResponse are client-side synthetic codes used for status reporting and are not emitted by the hub itself; treat them as diagnostic signals rather than hub-to-client error semantics.

---

## InfiniteRetryPolicy
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** class

```csharp
private sealed class InfiniteRetryPolicy : IRetryPolicy
```


InfiniteRetryPolicy is a private sealed implementation of IRetryPolicy that applies an indefinite exponential backoff for retries. Each retry's delay is computed as 2^(min(retryContext.PreviousRetryCount, 10)) seconds and then limited to the configured ReconnectMaxDelay, ensuring the wait never exceeds the maximum.

## Remarks
Isolates retry strategy from business logic, enabling swapping policies without touching call sites. The exponential backoff helps absorb transient failures while avoiding aggressive retry storms, and the hard cap on delay keeps the system responsive and predictable. Because the policy relies on RetryContext.PreviousRetryCount, the delay grows in a deterministic manner with each consecutive failure.

## Notes
- No jitter is applied; concurrency across multiple clients could produce synchronized retry timings.
- The cap at ReconnectMaxDelay means the delay stops growing after enough failures; tune that value to balance latency and retry aggressiveness.

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


ErrorDetail represents a single error entry within a `Response<T>` envelope. It carries an error code identifying the failure, an optional human-friendly Message, and an optional Data payload that is encoded as JSON. The Data field is intentionally loosely-typed because the payload shape varies by error code, allowing rich, structured error information without enforcing a rigid schema.

## Remarks
ErrorDetail is designed to be the primitive carrier of error information inside a `Response<T>` when things go wrong. By using a JsonElement for Data, it can accommodate a wide variety of error-specific details (for example, a list of conflicting hosts) without introducing additional strongly-typed members. This keeps the overall error-envelope extensible as new error codes are introduced while preserving serialization compatibility.

## Notes
- Data is optional; omit or pass null when there is no additional payload.
- Message is optional; Code is the canonical identifier for the error and is non-nullable.
- Data is a JsonElement, representing arbitrary JSON payloads; consumers should deserialize or query its contents as needed.


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


RegisterServerDto is an internal record that bundles all metadata required to register a server with the directory service. It carries the server's Name, an optional Description, the list of Hosts, the current UserCount, the software Version, and any Tags, plus an optional ClaimToken used for authorization during registration.

It acts as a single, immutable payload that callers provide to the ServerDirectoryService to register a server, instead of passing multiple parameters individually. As a record, it benefits from value-based equality and straightforward deconstruction/assignment in tests.

## Remarks
By grouping the registration data into a single DTO, this type reduces coupling between callers and the directory service. Being internal, it remains an implementation detail of the server directory workflow, allowing the service to evolve its storage/validation logic without broad outward impact.

## Example
```csharp
// Example usage within the same assembly
var dto = new RegisterServerDto(
    Name: "EdgeCluster-01",
    Description: "Primary edge server",
    Hosts: new[] { "edge1.internal:7777", "edge2.internal:7777" },
    UserCount: 128,
    Version: "1.4.0",
    Tags: new[] { "edge", "high-availability" },
    ClaimToken: null);
```

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


It represents the result of registering a server in the directory service, housing both the assigned ServerId and an optional ClaimToken for subsequent authenticated calls. The type is an immutable, value-based container used to return these related pieces together from the registration workflow.

## Remarks
Because it's a record with a concise primary constructor, it acts as a lightweight value object that binds server identity to its authorization token, ensuring callers receive related data in a single, cohesive value. The ClaimToken is nullable to reflect scenarios where a token isn't issued immediately or is not required for certain flows. Being internal to the assembly, this type is an implementation detail of the server directory workflow and is not part of the public API; if you need external consumers to receive server identifiers, provide a public wrapper.

## Notes
- The ClaimToken may be null; always null-check before attempting to use it for authentication or authorization decisions.
- This type is internal — do not instantiate from external assemblies; use a public-facing DTO or API surface if you need to expose server registration results outward.

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


`Response<T>` is an internal envelope that wraps every directory hub response, mirroring the EchoHubSpace contract. It exposes a boolean IsSuccess, an optional payload Data of type T, an optional array of ErrorDetail named Errors, and an optional Version string, providing a uniform response shape. Developers would reach for it when returning results from directory hub operations to standardize success/failure signaling and convey both payloads and error information in a single envelope.

## Remarks
This envelope enforces a uniform response shape across directory hub endpoints and aligns with EchoHubSpace, aiding client-side consumption and version-aware negotiation.

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


Initializes a ServerDirectoryService by taking four dependencies through constructor injection: IConfiguration for app configuration, PresenceTracker to monitor user presence, DirectoryClaimStore to manage directory-based claims, and ILogger&lt;ServerDirectoryService&gt; for logging. The constructor assigns these dependencies to private fields _configuration, _presenceTracker, _claimStore, and _logger, making configuration access, presence tracking, claim management, and diagnostics available to the service methods. This pattern follows typical DI usage in ASP.NET Core: the hosting container supplies the collaborators when constructing the service, keeping ServerDirectoryService decoupled and easily testable.

## Remarks
The constructor’s role is to decouple ServerDirectoryService from concrete implementations of its collaborators by relying on abstractions and DI. It centralizes initialization, promoting testability since tests can supply mock or fake implementations. In practice, ensure all four dependencies are registered with the DI container; otherwise, resolution will fail at runtime.

## Notes
- All four dependencies must be resolvable by the DI container at the time this constructor is invoked; otherwise service creation will fail.
- Avoid performing long-running or I/O work in the constructor; keep initialization lightweight to prevent blocking startup.

---

## BuildConnection
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
private HubConnection BuildConnection()
```

**Returns:** `HubConnection`


BuildConnection creates a configured SignalR HubConnection for the directory service. It uses HubConnectionBuilder to set the hub URL from DirectoryHubUrl, enables automatic reconnection with an InfiniteRetryPolicy, and then builds the connection. This encapsulates the connection setup so callers within ServerDirectoryService can obtain a ready-to-use HubConnection with consistent URL and retry behavior without repeating the builder chain.

## Remarks
This method acts as a small factory for a HubConnection with a predefined configuration. By centralizing the builder configuration, it ensures the service always connects to the Directory hub using the same URL and the same infinite retry strategy. The InfiniteRetryPolicy uses NextRetryDelay to determine backoff and allows the connection to recover from transient failures indefinitely.

## Notes
- A new InfiniteRetryPolicy instance is created on every call to BuildConnection; if you need shared backoff state or more complex lifecycle management, reuse a single policy instance.

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


ConnectWithRetryAsync encapsulates a resilient connection pattern for HubConnection. It repeatedly calls StartAsync on the provided connection, applying an escalating backoff after each failure until the operation succeeds or cancellation is requested. It returns true when StartAsync completes successfully; if cancellation is requested before a successful start, it returns false. On every failed attempt, the method logs a warning including the exception and the next delay, then awaits Task.Delay with the calculated backoff before retrying.

---

## DisposeConnectionAsync
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


Disposes a HubConnection in a best-effort manner during shutdown by awaiting DisposeAsync with a three-second timeout. If the disposal takes longer than the timeout or fails, the exception is swallowed to avoid blocking the shutdown sequence.

## Remarks

Provides a best-effort disposal of the HubConnection during application shutdown. It uses DisposeAsync(), converts it to a Task, and waits up to 3 seconds. Any failure or timeout is swallowed to prevent the shutdown process from stalling due to a slow or unavailable connection.

## Notes

- Exceptions are swallowed in this path and no logging occurs; this design avoids delaying shutdown but can hide disposal issues.
- The 3-second timeout is a hard cap; if longer cleanup is required, consider adjusting the timeout or restructuring shutdown logic.

---

## ExecuteAsync
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


Coordinates the public registration lifecycle for the server by checking configuration, collecting hosts, and initiating a directory registration when enabled. It yields to the host to complete startup, validates the configured hosts, builds server metadata (name, description, tags, version), then connects to the EchoHubSpace directory and subscribes to user-count changes, ensuring cleanup by detaching the event handler in a finally block.

## Remarks
Serves as the lifecycle glue between the host startup and the external directory service. It encapsulates the decision to expose the server publicly, based on configuration, and delegates the actual network communication to RunConnectionLoopAsync, keeping concerns separated. By subscribing to the presence change event, it ensures the directory reflects current presence and can adjust registration accordingly; the event subscription is always torn down to avoid leaks.

## Notes
- If PublicServer is disabled or Server:PublicHosts is empty, the method returns early and no directory registration occurs.

---

## ExtractConflictingHosts
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


Pulls ConflictingHosts out of an error's loosely-typed Data payload and returns it as a string[]; if the payload is missing, not an array, or contains no string values, null is returned. It tolerates both ConflictingHosts and conflictingHosts keys because SignalR's wire casing depends on the hub's serializer configuration.

## Remarks

ExtractConflictingHosts consolidates the JSON parsing logic for error payloads, so callers don't need to duplicate JsonElement checks or casing work. It centralizes the handling of optional error data and communicates a no-conflicts result by returning null rather than throwing. Because it only collects strings, any non-string items in the array are ignored.

## Notes

- Returns null when the payload is missing or does not conform to the expected shape (e.g., wrong key variant, non-array, or empty array).
- Looks up either ConflictingHosts or conflictingHosts; if both exist, the first matching one is used due to short-circuit evaluation.
- The method is private static, has no side effects, and is safe for use within its containing type.

---

## GetBackoffDelay
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


Computes the wait time before the next reconnection attempt by applying exponential backoff with an upper bound. For a given attempt, it yields 2 raised to the minimum of the attempt and 10, expressed in seconds, and returns that as a TimeSpan, clamped to ReconnectMaxDelay.

## Remarks
Centralizing this timing logic makes the retry policy explicit and testable, avoiding ad-hoc delays scattered throughout the reconnection flow. It also guards against excessively aggressive retries by capping the maximum delay, ensuring the system remains responsive under repeated failures.

## Example
```csharp
// Example: use within the same class to pause before retrying
int attempt = 3;
TimeSpan delay = GetBackoffDelay(attempt);
await Task.Delay(delay);
```

## Notes
- The exponent saturates at 2^10, after which the result is determined solely by ReconnectMaxDelay.
- This is a private helper; callers outside the containing class cannot call it directly, but its behavior governs the timing of the reconnection workflow.

---

## HandleRegistrationErrorAsync
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


This private asynchronous method translates the error details from a directory registration attempt into a concrete failure: it marks the registration as permanently failed, derives the error code from the first reported error (defaulting to UnknownError when none exists), and logs a code-specific message. It then records the failure with the claim store and completes, ensuring the system won't retry until a restart.

## Remarks
By centralizing error interpretation and presentation, this method decouples the source error from how it's surfaced and acted upon. It provides a single place to enforce the 'no automatic retry' policy across all directory-registration error codes and to standardize how conflicting hosts are reported to operators. The design ensures a consistent operational response to directory registration failures and signals operators when a restart is required to recover from a failed claim state.

## Notes
- If errors is null or empty, the method uses 'UnknownError' as the code and logs with a generic fallback message.
- For InvalidInput, the log includes the first error message (if present); if there is no message, it logs '(no message)'.
- All error paths include the explicit cue that the server will not retry until restarted, emphasizing the need for a manual recovery action.

---

## HandleRegistrationResponseAsync
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


Processes the asynchronous response returned by the directory after a RegisterServer request. It validates the envelope's presence and protocol compatibility, handles success or error outcomes, and updates local state and persistence accordingly, including saving a fresh claim token when provided or updating the ServerId on re-registration; it also logs the outcome and marks the registration as permanently failed for unrecoverable conditions.

## Remarks
This symbol encapsulates the end-to-end handling of a single registration round trip with the directory: it enforces a strict, durability-aware workflow (persisting the first-issued claim token before acknowledging success), coordinates with the claim store and logger, and surfaces configuration or protocol issues early (e.g., protocol version mismatches) to prevent unsafe future operations. It sits between the directory handshake and the higher-level registration state, ensuring the in-memory state, persisted identity, and observability align with the directory's authoritative result.

## Notes
- If the envelope is null or its Data is null, the code marks the registration as permanently failed and will not retry until an application restart.
- A protocol version mismatch results in a permanent failure to avoid operating with an incompatible directory; coordinate deployments to align versions.
- When a fresh claim token is provided, it is saved before marking the registration as successful to guarantee durability for the first claim; if no token is provided, the method updates the ServerId defensively to keep persistence consistent.
- The final log provides a concise audit trail linking the registered name and hosts to the assigned ServerId.

---

## OnUserCountChanged
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


OnUserCountChanged handles changes to the current user count by publishing the new value to a single-slot channel. The non-blocking TryWrite call ensures the producer never blocks, and the single-slot channel semantics coalesce bursts of rapid changes so that only the latest count is observed by the consumer.

## Remarks
By buffering updates through a channel, this method decouples the producer of presence changes from the component that processes and reacts to them. The single-slot channel guarantees bounded memory usage and backpressure that favors the most recent state, which is typically what clients care about in presence scenarios. This pattern simplifies synchronization and reduces contention on the event source.

## Example
```csharp
// Rapid sequence of changes collapses to the latest value observed by the reader.
OnUserCountChanged(3);
OnUserCountChanged(4);
OnUserCountChanged(5);
```

## Notes
- If the channel's single-slot buffer is full when this method is called, TryWrite may return false and drop the new value. This is intentional to coalesce bursts into a single latest update.

---

## ProcessUserCountUpdatesAsync
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


Background task that consumes updates to the current user count and pushes the latest value to the directory service over a HubConnection. It throttles outbound updates to avoid flooding the network and drains newer values while waiting so the published count reflects the most recent state.

## Remarks
Acts as a background consumer of a bounded update stream, coalescing bursts into a single latest value before transmission. It ensures updates are sent only when the hub connection is connected and ownership/registration checks pass. The implementation uses a throttle window to reduce network chatter and updates internal state only after a successful remote invocation. If the connection closes or cancellation is requested, the loop exits gracefully.

## Notes
- During the throttle window, newer values are drained to ensure the latest value is sent.
- If the hub invocation fails, the exception is logged as a warning and the loop continues without crashing.
- Updates may be dropped if cancellation is requested or the connection becomes unavailable or the client is not registered.

---

## RegisterAsync
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


RegisterAsync is a private asynchronous helper that attempts to enroll the current server instance with the directory service via a SignalR hub. It does nothing unless the hub connection is active and a previous permanent registration failure has not occurred. When invoked, it gathers the current online user count from the presence tracker, builds a RegisterServerDto with the server metadata (name, description, hosts, userCount, version, tags) and the stored claim token, and sends it through the hub using RegisterServer. The response is processed by HandleRegistrationResponseAsync, with any exceptions being logged as warnings rather than thrown. This pattern centralizes registration logic behind a guarded, non-throwing pathway to the directory service.

---

## ResolveVersion
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** method

```csharp
private static string ResolveVersion()
```

**Returns:** `string`


Resolves the version string for the current server build. It prefers the AssemblyInformationalVersion attribute from the ServerDirectoryService assembly and, if present, strips any trailing SourceLink git SHA suffix (the portion after '+') to produce a clean version like '1.2.3' or '1.2.3-beta'. If that attribute is not available, it falls back to the assembly's semantic version, and as a final fallback returns '0.0.0' when neither is present.

## Remarks
This centralizes version formatting in a single helper to ensure consistency across the service's logs and telemetry. Stripping the '+' suffix prevents leaking VCS metadata into runtime strings while preserving any pre-release portion that may precede it. If the informational version is missing, the method gracefully falls back to the assembly version or a conservative default, making its behavior robust in trimmed or self-contained deployments. The implementation relies on reflection to read the hosting assembly (containing ServerDirectoryService) rather than requiring a separate configuration value, keeping the resolution self-contained within the server component.

## Notes
- If the build does not produce an informational version, the method may return '0.0.0' in runtime scenarios lacking the attribute; consider enabling or configuring InformationalVersion to improve traceability.
- The '+'-suffix stripping targets metadata appended by SourceLink; any meaningful version text before '+' remains intact.
- This method is private and static, serving internal diagnostics/telemetry rather than being a public API surface.

---

## RunConnectionLoopAsync
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


Maintains a persistent, self-healing connection to the directory service by running a loop that builds a connection, connects with retry logic, registers the server, and streams user-count updates until cancellation or permanent closure. It also handles heartbeat pings, re-registration after reconnects, and orderly cleanup when the token signals shutdown.

## Remarks

This method centralizes the lifecycle of the directory connection: establishing the link, keeping it alive via heartbeat interactions, re-registering after a reconnect, and restarting the loop when the connection is permanently closed. It coordinates with registration state (notably honoring _registrationPermanentlyFailed to avoid re-registration after a permanent failure) and ensures resources are released in all paths via a final disposal block. The cancellation token enables graceful shutdown, while the internal TaskCompletionSource serves as a synchronization primitive to signal a permanent close and trigger a rebuild loop.

## Notes

- The TaskCompletionSource is created with RunContinuationsAsynchronously to avoid executing continuations inline and potential synchronization issues.
- On Ping, a heartbeat is sent to the directory; failures are logged and do not crash the loop but are surfaced for diagnostics.
- If a reconnect occurs after a permanent registration failure, the code logs a warning and refrains from re-registering, instructing to restart with corrected configuration.
- The outer loop ensures the connection is disposed and rebuilt (with a brief delay) whenever the connection is permanently closed or cancellation is requested.


---

## StopAsync
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


StopAsync overrides the asynchronous stop lifecycle: it first delegates to the base implementation to perform its cleanup, and then clears the internal _connection reference. This ordering guarantees the standard shutdown behavior runs before releasing the service's own resources.

## Remarks
By awaiting base.StopAsync before nulling _connection, the code enforces a deterministic teardown order: base resources are cleaned up prior to releasing the local connection handle. Clearing _connection after the base stop helps prevent accidental reuse of the connection after StopAsync completes and simplifies garbage collection.

## Notes
- After StopAsync completes, _connection is null; any code that touches _connection should guard with a null check or coordinate with StopAsync to avoid races.
- If this override is extended in derived classes, maintain the same cleanup order to preserve teardown semantics.

---

## DirectoryHubUrl
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** field

```csharp
private const string DirectoryHubUrl = "https://echohub.voidcube.cloud/hubs/servers"
```


DirectoryHubUrl is a private constant string that defines the base URL for the server-directory hub (https://echohub.voidcube.cloud/hubs/servers). It is used by ServerDirectoryService to construct requests to the hub, centralizing the endpoint so all directory interactions share a single canonical address.

## Remarks
It centralizes the hub endpoint to prevent duplication and typos when building requests. Because it is private, it is not directly reusable by other components; if reusability or configurability is required, consider exposing a public constant or moving the value to configuration.

## Notes
- Hard-coded URL means environment-specific overrides require code changes and recompilation.
- Private visibility restricts reuse; for broader access, consider elevating the URL to a shared configuration or public constant.

---

## ReconnectBaseDelay
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** field

```csharp
private static readonly TimeSpan ReconnectBaseDelay = TimeSpan.FromSeconds(2)
```


Defines the base delay used by the reconnect logic in ServerDirectoryService. With a value of two seconds, it establishes the minimal wait before a reconnect attempt and is shared across all instances as a private, immutable tuning knob not exposed in the public API.

## Remarks
It represents the canonical base interval for retrying connections, centralizing timing so changes affect all reconnect attempts uniformly. Because it is static readonly, the value is immutable after initialization, ensuring predictable backoff behavior across threads. Its private visibility keeps this detail out of the public contract, allowing the class to adjust its internal retry strategy without affecting consumers.

## Notes
- Not configurable at runtime via configuration; changing it requires code changes and redeployment.

---

## ReconnectMaxDelay
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** field

```csharp
private static readonly TimeSpan ReconnectMaxDelay = TimeSpan.FromSeconds(30)
```


This private, type-wide constant defines the maximum delay applied when backing off between reconnection attempts in ServerDirectoryService. As a static readonly TimeSpan, it provides a single, immutable cap shared by all instances, currently 30 seconds (TimeSpan.FromSeconds(30)). The backoff logic in the service should respect this bound, ensuring a reconnection attempt is never delayed longer than this value; modifying it changes the overall aggressiveness of the retry strategy.

## Remarks
By extracting the maximum delay into a named constant, the code separates policy from calculation and makes it easy to tweak the cap without altering the backoff algorithm itself. It also communicates intent clearly to readers examining the timing behavior of the reconnect flow, and it guarantees consistent behavior across all code paths that trigger a delay.

## Notes
- Changing this value affects all reconnection backoffs across the class because it's static.
- It is private and readonly, so the value is fixed after type initialization and not configurable at runtime.
- The unit is seconds for readability; keep the TimeSpan.FromSeconds(…) usage when adjusting.

---

## UserCountMinInterval
> **File:** `src/EchoHub.Server/Services/ServerDirectoryService.cs`  
> **Kind:** field

```csharp
private static readonly TimeSpan UserCountMinInterval = TimeSpan.FromSeconds(1)
```


Defines the class-wide minimum interval between successive user-count operations as a TimeSpan of one second. This private static readonly field enforces a predictable throttle across all uses of the ServerDirectoryService, preventing rapid, successive work and reducing load.

## Remarks
Centralizes throttling behavior for user-count logic, ensuring consistent pacing wherever the value is consulted. Because it is static and readonly, all instances share the same value and it cannot be changed at runtime, providing a stable baseline. If future requirements demand configurable timing, consider introducing a configurable option or policy instead of duplicating literals.

---