# IEchoHubClient

> **File:** `src/EchoHub.Core/Contracts/IEchoHubClient.cs`  
> **Kind:** interface

```csharp
public interface IEchoHubClient
```


Represents the set of server-to-client callbacks a client must implement to receive real‑time events from the Echo hub. Implement this interface on the client side to handle incoming messages, presence and channel lifecycle notifications, administrative actions (kick/ban/nuke/force disconnect), and error notifications; each member returns a Task so handlers can perform asynchronous work.

## Remarks
This interface defines a concise contract used by the hub to invoke client-side behavior. It separates transport and server logic from client behavior: the server calls these methods without needing knowledge of the client's UI or state-management details, while implementations decide how to apply updates (for example updating UI state, local caches, or persistence). Returning Task from each method allows implementations to perform asynchronous work and signal completion to the caller.

## Notes
- Several parameters are nullable (e.g., UserPresenceDto? presence, string? reason). Callers must handle null values.
- Implementations should avoid long-running synchronous work inside these handlers; prefer async/await so the returned Task completes promptly and does not block calling threads or the UI.
- Handlers are invoked by the hub runtime on the client's execution context; ensure any updates to shared or UI state are performed on the appropriate thread or synchronized to avoid race conditions.