# ChatService.cs

> **Source:** `src/EchoHub.Server/Services/ChatService.cs`

## Contents

- [ChatService](#chatservice)
  - [ChatService (constructor)](#chatservice-constructor)
  - [BroadcastChannelDeletedAsync](#broadcastchanneldeletedasync)
  - [BroadcastChannelUpdatedAsync](#broadcastchannelupdatedasync)
  - [BroadcastMessageAsync](#broadcastmessageasync)
  - [BroadcastToAllAsync](#broadcasttoallasync)
  - [BuildReplyRef](#buildreplyref)
  - [FileIdFromUrl](#fileidfromurl)
  - [GetChannelHistoryAsync](#getchannelhistoryasync)
  - [GetChannelHistoryInternalAsync](#getchannelhistoryinternalasync)
  - [GetChannelsForUserAsync](#getchannelsforuserasync)
  - [GetOnlineUsersAsync](#getonlineusersasync)
  - [LeaveChannelAsync](#leavechannelasync)
  - [SendMessageAsync](#sendmessageasync)
  - [UpdateStatusAsync](#updatestatusasync)
  - [UserConnectedAsync](#userconnectedasync)
  - [UserDisconnectedAsync](#userdisconnectedasync)
- [BuildLogBacklog](#buildlogbacklog)
- [JoinChannelAsync](#joinchannelasync)
- [SanitizeNewlines](#sanitizenewlines)

---

## ChatService
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** class

```csharp
public class ChatService : IChatService
```


Coordinates the server-side chat workflow: presence tracking, channel membership and validation, message handling (decryption, sanitization, spam checks, reply validation, optional link embeds), persistence, and broadcasting to configured IChatBroadcaster implementations. Reach for ChatService when you need the complete, policy-enforced chat behavior used by the hub (connect/disconnect, join/leave, send message, history, status and broadcasts) rather than calling lower-level pieces like the channel store, encryption, or broadcasters individually.

## Remarks
ChatService is an orchestration façade that centralizes chat policies and cross-cutting concerns so the rest of the system sees a single, consistent chat surface. It delegates channel validation and membership (including password checks) to the channel service, uses PresenceTracker for online lists, defers spam decisions to the SpamGuard, asks LinkEmbedService for embeds, and relies on IMessageEncryptionService for decrypt/encrypt logic. It also records runtime telemetry and logs through ServerStatsCollector and ServerLogsService, and persists or reads backlog data via FileStorageService for special channels (for example, the rolling log room). Finally, it shields broadcasters from internal policies by converting and routing messages appropriately (e.g., encrypted payloads for some clients, plaintext for legacy/IRC broadcasters).

## Notes
- Join throttle: only first-time joins (where the user is not already a member) count toward the join throttle to avoid falsely flagging reconnect/auto-join bursts.
- Live log room is treated as read-only; attempts to write to it are rejected early and its backlog is sourced from a rolling log file — only the first history page returns a backlog.
- SpamGuard operates on the stored content (ciphertext when end-to-end encryption is used) and never requires or performs decryption; muting and escalation are handled through the service's moderation workflow.

---

### ChatService (constructor)
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** constructor

```csharp
public ChatService(
        IServiceScopeFactory scopeFactory,
        PresenceTracker presenceTracker,
        IEnumerable<IChatBroadcaster> broadcasters,
        LinkEmbedService embedService,
        IMessageEncryptionService encryption,
        IChannelService channelService,
        FileStorageService fileStorage,
        SpamGuard spamGuard,
        ServerLogsService serverLogs,
        ServerStatsCollector statsCollector,
        ILogger<ChatService> logger)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `scopeFactory` | `IServiceScopeFactory` | — |
| `presenceTracker` | [`PresenceTracker`](PresenceTracker.cs.md) | — |
| `broadcasters` | `IEnumerable<IChatBroadcaster>` | — |
| `embedService` | [`LinkEmbedService`](LinkEmbedService.cs.md) | — |
| `encryption` | [`IMessageEncryptionService`](../../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md) | — |
| `channelService` | [`IChannelService`](../../EchoHub.Core/Contracts/IChannelService.cs.md) | — |
| `fileStorage` | [`FileStorageService`](FileStorageService.cs.md) | — |
| `spamGuard` | [`SpamGuard`](SpamGuard.cs.md) | — |
| `serverLogs` | [`ServerLogsService`](ServerLogs/ServerLogsService.cs.md) | — |
| `statsCollector` | [`ServerStatsCollector`](Stats/ServerStatsCollector.cs.md) | — |
| `logger` | `ILogger<ChatService>` | — |


Constructs a ChatService by injecting its required collaborators and wiring them to private fields. This constructor is invoked by the dependency injection container when a ChatService is created, supplying services for scope management, presence tracking, message broadcasting, content embedding, encryption, channel operations, file storage, spam protection, server logging, and statistics collection. The use of `IEnumerable<IChatBroadcaster>` indicates that multiple broadcasters can participate in delivering messages and events, allowing pluggable delivery strategies without changing ChatService code.

## Remarks
ChatService acts as an orchestration hub for chat functionality. By depending on interfaces rather than concrete implementations, it remains highly testable and extensible: you can substitute mocks or fakes for broadcasters, presence tracking, or encryption in tests or different environments. The broadcaster collection enables evolving notification strategies by simply registering new IChatBroadcaster implementations, aligning with the open/closed principle.

## Notes
- No null-checks are performed in the constructor; rely on the DI container to provide non-null dependencies. If ChatService might be created outside the DI pipeline, consider adding guards.
- When using `IEnumerable<IChatBroadcaster>`, all registered broadcasters will be resolved and invoked; behavior depends on the concrete broadcaster implementations.

---

### BroadcastChannelDeletedAsync
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
public Task BroadcastChannelDeletedAsync(string channelName)
        => BroadcastToAllAsync(b => b.SendChannelDeletedAsync(channelName))
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `Task`


BroadcastChannelDeletedAsync publishes a channel-deletion event to all connected clients by delegating to the shared broadcasting pipeline. It forwards the channelName to each subscriber through SendChannelDeletedAsync, coordinated by BroadcastToAllAsync to ensure every participant receives the notification.

## Remarks
Provides a domain-friendly API that hides the broadcasting details behind a simple, expressive method name. By delegating to BroadcastToAllAsync, it centralizes how channel-deletion notifications are distributed, reducing duplication and ensuring consistent behavior across all subscribers.

## Notes
- No input validation is performed on channelName; callers should ensure the value is non-null and meaningful before invocation.
- Exceptions raised during per-subscriber delivery will propagate via the returned Task; callers should decide whether to await and handle failures.

---

### BroadcastChannelUpdatedAsync
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
public Task BroadcastChannelUpdatedAsync(ChannelDto channel, string? channelName = null)
        => BroadcastToAllAsync(b => b.SendChannelUpdatedAsync(channel, channelName))
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channel` | [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) | — |
| `channelName` | `string?` | `null` |

**Returns:** `Task`


BroadcastChannelUpdatedAsync is a thin wrapper that notifies all connected clients that the specified channel has been updated. It accepts the ChannelDto describing the channel and an optional new channelName. The method delegates to the shared broadcast mechanism (BroadcastToAllAsync) by applying a function that calls SendChannelUpdatedAsync on each client with the provided payload. Use this when you want real-time client UIs to reflect changes to a channel, such as a rename or updated metadata, without having to push updates individually to each client.

## Remarks
BroadcastChannelUpdatedAsync centralizes the channel-update notification path in ChatService. By encapsulating the broadcast call behind this single method, callers don't need to know about how clients are iterated or how the payload is delivered; tests can mock this entry point, and future changes to the broadcasting strategy stay confined here.

## Notes
- This method only notifies clients; it does not modify the channel data in storage.
- If channelName is non-null, it will be included as part of the payload and may be used by clients to display the new name.

---

### BroadcastMessageAsync
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
public Task BroadcastMessageAsync(string channelName, MessageDto message)
        => BroadcastToAllAsync(b => b.SendMessageToChannelAsync(channelName, message))
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `message` | [`MessageDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) | — |

**Returns:** `Task`


BroadcastMessageAsync asynchronously broadcasts the provided MessageDto to all clients subscribed to the specified channel by delegating to BroadcastToAllAsync. This method serves as a focused helper for channel-scoped messages, insulating callers from the details of iterating over recipients and invoking SendMessageToChannelAsync on each.

## Remarks
This wrapper consolidates the channel-based dispatch pattern into a single, discoverable API on the chat service. It centralizes the broadcasting contract so callers do not need to know how broadcasting is implemented (per-subscriber dispatch vs. transport specifics), and it supports testing and mocking of channel messages by providing a stable entry point.

## Notes
- The visible code does not show input validation; consider validating channelName and message upstream to avoid potential ArgumentNullException during broadcasting.
- The method returns a Task; await it to observe completion and to surface any exceptions from the underlying broadcast pipeline (e.g., failures in SendMessageToChannelAsync).

---

### BroadcastToAllAsync
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
private async Task BroadcastToAllAsync(Func<IChatBroadcaster, Task> action)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `action` | `Func<IChatBroadcaster, Task>` | — |

**Returns:** `Task`


BroadcastToAllAsync is a private helper that sequentially applies an asynchronous action to every broadcaster in the _broadcasters collection. By awaiting the provided `Func<IChatBroadcaster, Task>` for each broadcaster, it ensures ordered, per-broadcaster execution. If an individual broadcaster throws, the exception is caught and logged with the broadcaster’s runtime type name, allowing the remaining broadcasters to continue without interrupting the overall broadcast flow.

## Remarks
This helper centralizes the common pattern of broadcasting to multiple chat broadcasters while isolating failures. It provides a small orchestration layer that coordinates across the _broadcasters collection and ensures one faulty broadcaster does not derail the entire operation. Because it is private, this logic remains an implementation detail of the class rather than part of its public API.

## Notes
- The method is sequential; broadcasting happens one broadcaster after another, not in parallel. If you need parallel broadcasting, use a different approach.
- Exceptions from action are swallowed per-broadcaster; if you need different error handling, handle it inside the action or upstream.
- Logging uses broadcaster.GetType().Name to identify failures; if multiple broadcasters share a type, the log may not distinguish instances.

---

### BuildReplyRef
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
private ReplyRefDto BuildReplyRef(Message target)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `target` | [`Message`](../../EchoHub.Core/Models/Message.cs.md) | — |

**Returns:** [`ReplyRefDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md)


BuildReplyRef constructs the wire reference for a reply target by decrypting the target’s content to obtain a plaintext snippet, conditionally truncating it, and then re-encrypting the result into a ReplyRefDto that carries the target’s ID, sender, and the encrypted snippet. Specifically, it decrypts target.Content; if the decrypted text is not recognized as E2E room ciphertext and exceeds 120 characters, it truncates to 120 characters and appends an ellipsis; finally, it encrypts the possibly shortened plaintext and returns a ReplyRefDto.

## Remarks
Encapsulates the logic for producing a secure, compact teaser of the original message for a reply. It ensures that non-E2E content is truncated to a sane length while guaranteeing that E2E content remains intact (and thus decryptable) by avoiding truncation. It delegates the actual encryption to the central _encryption service and uses RoomCrypto.IsRoomCiphertext to decide when truncation is safe.

## Notes
- Truncation only occurs when the decrypted content does not look like room ciphertext; this prevents corrupting E2E data.
- The returned snippet is encrypted before being included in ReplyRefDto.
- The 120-character limit is a fixed server-side threshold governing the preview length.

---

### FileIdFromUrl
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
private static string FileIdFromUrl(string url) => url.Split('/')[^1]
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `url` | `string` | — |

**Returns:** `string`


Extracts the storage file id from an attachment URL by taking the last path segment after the final '/'. It is intended for URLs that look like '/api/files/{id}' and is used in scenarios where the code needs to derive the identifier from a URL without performing a full URL parser.

## Remarks
Because this helper relies on a simple string.Split and the C# index-from-end operator [^1], it assumes the input is a plain path that ends with the id and does not end with a trailing slash. If the URL ends with '/', the result will be an empty string. It also does not guard against null inputs, which would raise an exception at runtime. In practice this method is a tiny, in-class utility that centralizes the id extraction so callers don't duplicate the split logic.

## Notes
- Trailing slash in the URL yields an empty id; normalize the URL or trim the trailing slash before calling.
- Null or empty input is not handled; ensure a non-null, non-empty URL is passed.

---

### GetChannelHistoryAsync
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
public async Task<List<MessageDto>> GetChannelHistoryAsync(string channelName, int count, int offset = 0)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `count` | `int` | — |
| `offset` | `int` | `0` |

**Returns:** `Task<List<MessageDto>>`


GetChannelHistoryAsync retrieves a paged history of messages for a given channel. It normalizes the channel name to lowercase and trims it, clamps the requested count to the range [1, ValidationConstants.MaxHistoryCount], and ensures offset is non-negative. If the channel is a logs channel (no DB messages), the backlog is read from the rolling log file: the first page is populated via BuildLogBacklog, while older pages are empty (files are archives). For regular channels, a short-lived DI scope is created to resolve EchoHubDbContext, and the method delegates to GetChannelHistoryInternalAsync to fetch the requested slice from the database. The call is asynchronous and returns a `List<MessageDto>`.

## Remarks
Serves as a unified history retrieval entry point that abstracts away the storage details behind a paging API. It centralizes channel-history concerns so callers don't need to know whether messages come from the rolling log or the database, while preserving the expected paging semantics across both sources.

## Notes
- Be aware that for log channels, only the first page contains backlog data; requesting subsequent pages returns an empty list.

---

### GetChannelHistoryInternalAsync
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
private async Task<List<MessageDto>> GetChannelHistoryInternalAsync(EchoHubDbContext db, string channelName, int count, int offset = 0)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `db` | [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) | — |
| `channelName` | `string` | — |
| `count` | `int` | — |
| `offset` | `int` | `0` |

**Returns:** `Task<List<MessageDto>>`


Retrieves a page of messages for a named channel, enriching each entry with sender metadata, attachment data, and embed information, so the client can render a historical view of the chat. Call this when you need to assemble a channel's message history in a transport-ready form, with tombstoned accounts preserved and content decrypted for display.

## Remarks
Conceptually, this method centralizes history construction: it loads messages for a channel, preserves messages from deleted accounts by left-joining with users, collects reply targets for quote rendering, and hydrates attachments while pruning entries that no longer have valid files. It decrypts message content and any embed metadata, reconstructs attachments for transport (including per-attachment previews that are re-encrypted), and translates raw data into the client-facing DTOs (MessageDto, AttachmentDto, EmbedDto). This batching approach minimizes round-trips by prefetching related data (replies, attachments, embeds) in a single operation.

## Notes
- If a channel is not found, the method returns an empty list rather than throwing. Callers should handle an empty history gracefully.
- Messages associated with deleted users are preserved in history, but their display name and nickname color may be null; UI code should account for missing metadata.
- Attachments are shown only if their underlying files still exist on disk; messages with only vanished attachments and no plaintext content are pruned from the result.
- The method decrypts content and embed JSON, and it re-encrypts payloads for transport; the exact transport-encryption details are handled deeper in the pipeline and may depend on the caller's context.

---

### GetChannelsForUserAsync
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
public Task<List<string>> GetChannelsForUserAsync(string username)
        => Task.FromResult(_presenceTracker.GetChannelsForUser(username))
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |

**Returns:** `Task<List<string>>`


Retrieves the list of channel names that a specific user participates in, exposed as an asynchronous method. It delegates to the presence tracker via _presenceTracker.GetChannelsForUser(username) and wraps the result in Task.FromResult, which means the call completes synchronously and simply presents an async surface to callers. Use this when you require an async API surface (e.g., to be consistent with other async members) even though the underlying operation is synchronous.

## Remarks

It provides an asynchronous API surface for retrieving a user's channel list by delegating to the presence tracker. This keeps ChatService methods consistent in an async context and avoids exposing a synchronous API directly to callers that expect Task-returning methods. The actual retrieval is synchronous, so this wrapper does not introduce true asynchrony.

## Notes

- Completes synchronously; no actual I/O is awaited here.
- Any exception from _presenceTracker.GetChannelsForUser will be thrown at call time (not surfaced as a faulted Task).
- If you anticipate long blocking work, prefer an actual asynchronous implementation or an asynchronous presence tracker.

---

### GetOnlineUsersAsync
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
public async Task<List<UserPresenceDto>> GetOnlineUsersAsync(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `Task<List<UserPresenceDto>>`


Retrieves the list of online users for a given chat channel by joining the in-memory presence tracker with the database-stored user records. It normalizes the channel name to lowercase and trims whitespace, fetches the set of online usernames from the tracker, queries EchoHubDbContext.Users for those usernames that are not Invisible, and then maps each user to a UserPresenceDto that includes the in-memory IRC-only flag. This method is typically used when you need to present the current participants of a channel, excluding hidden users, with their display metadata.

## Remarks
Acts as a bridge between transient presence state and the persistent user store, ensuring that the live list respects visibility rules while enriching with profile data. The conversion to UserPresenceDto happens after the EF query to allow the IRC-only flag to be derived from the presence tracker, not stored in the database. The use of a scoped DbContext keeps the data access isolated and safe for concurrent calls.

## Example
```csharp
// Example usage
var online = await chatService.GetOnlineUsersAsync("general");
Console.WriteLine($"Online in #general: {online.Count}");
```

## Notes
- If a username is online according to the tracker but missing from the database, it will be ignored.
- Channel name normalization means calls with different casing or surrounding whitespace map to the same channel.
- The IsIrcOnly flag is determined by the presence tracker and is included in each UserPresenceDto.

---

### LeaveChannelAsync
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
public async Task LeaveChannelAsync(string connectionId, string username, string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `connectionId` | `string` | — |
| `username` | `string` | — |
| `channelName` | `string` | — |

**Returns:** `Task`


LeaveChannelAsync handles the workflow for when a user leaves a chat channel. It normalizes the channel name to lowercase, updates the presence tracker to reflect that the user has left, broadcasts a user-left notification to all connected clients in that channel, and logs the action at the debug level.

Developers call this when a user intentionally exits a channel; the method encapsulates the coordinated state change, notification, and observability so callers don't have to orchestrate these steps separately.

## Remarks

It centralizes the leave workflow into a single, reusable operation that updates presence, notifies clients, and records the event for debugging. Normalizing the channel name here prevents case-sensitivity inconsistencies when tracking presence or delivering notifications. Notification is performed asynchronously by the broadcasting layer, which preserves responsiveness and allows the caller to await completion.

## Example

```csharp
// Most common usage: user "alice" leaves the "General" channel
await chatService.LeaveChannelAsync("conn-123", "alice", "General");
```

## Notes

- ToLowerInvariant is called on channelName without a null-check; passing null will throw. Ensure channelName is non-null before calling, or upstream validation.
- The connectionId parameter is unused in this implementation; it may be present for correlation or future use.
- Exceptions from _presenceTracker.LeaveChannel or BroadcastToAllAsync propagate to the caller; no internal retry is performed.

---

### SendMessageAsync
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
public async Task<string?> SendMessageAsync(Guid userId, string username, string channelName, string content, string? originConnectionId = null, Guid? replyToMessageId = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `userId` | `Guid` | — |
| `username` | `string` | — |
| `channelName` | `string` | — |
| `content` | `string` | — |
| `originConnectionId` | `string?` | `null` |
| `replyToMessageId` | `Guid?` | `null` |

**Returns:** `Task<string?>`


SendMessageAsync coordinates the end-to-end process of posting a chat message to a named channel. It normalizes the channel name, validates it against allowed patterns, blocks writes to read-only channels (including the logs room and system channels), decrypts incoming content, strips a literal encryption prefix if present to prevent spoofing, and enforces non-empty content and a maximum length. It then looks up the target channel and the sender from the database, enforces mute state (including automatic unmute when a mute has expired), runs a spam guard that can auto-mute or reject messages, validates an optional reply target, and resolves link embeds before persisting the message. The method returns a user-facing string on error or when action is blocked, and returns null on a successful send; side effects include database updates, saving changes, and a moderation log entry when auto-muting occurs.

## Remarks
The method centralizes chat message submission, ensuring consistent enforcement of security, moderation, and content rules across all channels. It encapsulates cross-cutting concerns (validation, decryption, sanitization, moderation, and embed resolution) behind a single entry point, reducing duplication and potential inconsistencies in callers. By using a scoped DbContext and explicit read-only checks, it mitigates the risk of unintended writes and keeps transactional boundaries clear. The combination of encryption-aware processing, a programmable spam guard, and read-only channel protection reveals a deliberate design to balance user privacy, abuse prevention, and system integrity.

## Notes
- The method mutates and persists mute state (sender.IsMuted/MutedUntil) in response to spam protection or mute expiry.
- Returning strings for error/status means callers must handle UI messaging; on success it returns null.
- Be aware of early returns for read-only channels and non-existent channels; ensure the consumer handles user feedback.

---

### UpdateStatusAsync
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
public async Task<string?> UpdateStatusAsync(Guid userId, string username, UserStatus status, string? statusMessage)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `userId` | `Guid` | — |
| `username` | `string` | — |
| `status` | [`UserStatus`](../../EchoHub.Core/Models/UserStatus.cs.md) | — |
| `statusMessage` | `string?` | — |

**Returns:** `Task<string?>`


Updates a user's presence status for the specified userId and username, performing validation, persisting changes to the database, and broadcasting the new presence to connected clients. When the input is invalid or the user cannot be found, it returns a user-facing error string; on success it returns null.

## Remarks
The method creates a short-lived DI scope to obtain EchoHubDbContext, updates the user entity (Status, StatusMessage trimmed, and LastSeenAt set to UTC now), and saves changes. It then builds a UserPresenceDto and uses the presence tracker to determine the target channels, broadcasting the updated presence to all relevant clients via BroadcastToAllAsync. The return value encodes success (null) or failure (a user-facing string) without throwing exceptions.

## Notes
- If a value outside the defined UserStatus enum is supplied, the method immediately returns "Invalid status. Use online, away, dnd, or invisible." due to the enum validation check.
- The status message is length-validated against ValidationConstants.MaxStatusMessageLength and is trimmed before storage; overly long messages produce a descriptive error.


---

### UserConnectedAsync
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
public async Task UserConnectedAsync(string connectionId, Guid userId, string username)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `connectionId` | `string` | — |
| `userId` | `Guid` | — |
| `username` | `string` | — |

**Returns:** `Task`


Upon a client connection, this method coordinates in-memory presence tracking, connection-count telemetry, and optional persistence of the user's online state. It updates the in-memory presence tracker and stats collector with the new connection, then resolves a scoped EchoHubDbContext to locate the user by userId. If the user exists, it updates LastSeenAt to the current UTC time and sets Status to Online, persisting the change via SaveChangesAsync. A debug-level log records the connection event for troubleshooting. The inline comment notes that churn aggregation is performed in the periodic stats report rather than in this hot path.

## Remarks

This method glues together presence, persistence, and telemetry for a user connection. It relies on a scoped DbContext to keep database changes isolated per connection, avoiding long-lived contexts and potential contention. By updating both the in-memory trackers and the persisted user state when a user connects, it helps ensure a consistent view of online users across in-memory data and storage, while gracefully handling the case where a user record may be absent.

## Notes

- If the user record cannot be found in EchoHubDbContext.Users, no database write occurs; the method still updates presence and stats.
- The LastSeenAt timestamp uses DateTimeOffset.UtcNow to avoid timezone inconsistencies across servers.


---

### UserDisconnectedAsync
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
public async Task<string?> UserDisconnectedAsync(string connectionId)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `connectionId` | `string` | — |

**Returns:** `Task<string?>`


Handles the disconnection lifecycle for a user in the chat service. Given a connectionId, it resolves the associated username, collects the channels the user was in, updates presence statistics, and if the user is no longer online, persists LastSeenAt and marks the user as Invisible in the database. It also constructs a UserPresenceDto and broadcasts a status-change to the user's previously tracked channels. The method returns the username that disconnected (or null if no user could be resolved from the connectionId).

## Remarks

By isolating persistence and presence updates behind a scoped database context, this method coordinates ephemeral connection state with durable user data. It serves as the boundary between connection lifecycle management and user presence broadcasting, ensuring that changes are persisted and that clients are notified consistently. The pattern of resolving the user from the connection, updating LastSeenAt and Visibility, and broadcasting a UserPresenceDto helps keep the client UIs in sync with accurate user status.

## Notes

- If the connectionId cannot be mapped to a username, the method still records the disconnection count via the stats collector, but skips the database update and user-broadcast.
- LastSeenAt is updated to DateTimeOffset.UtcNow and Status is set to Invisible only when a valid username is found and the user is no longer online.
- A scoped EchoHubDbContext is used to persist changes; the DbContext instance is disposed as part of the scope lifecycle. The broadcast is sent to the channels the user was connected to before disconnect; if there were no such channels, there is no targeted broadcast.

---

## BuildLogBacklog
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
private List<MessageDto> BuildLogBacklog(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `List<MessageDto>`


Turns the log-file backlog into transport-encrypted MessageDto objects so clients render past log lines exactly like streamed ones. It reads the backlog via the server logs store, encrypts each backlog entry’s content with the encryption service, and wraps it in a new MessageDto using a freshly generated GUID, the SenderName from ServerLogsService, the provided channelName, and the backlog entry’s timestamp. This method never touches the database and serves solely as a transform to replay historical log lines in the same MessageDto format as live messages.

## Remarks
Acts as a translator between persisted log backlog and the live message stream. By centralizing encryption and formatting, it ensures backlog replay matches live streams and isolates storage concerns from presentation. The use of a new GUID per backlog item also avoids depending on database identifiers for UI rendering.

## Notes
- The MessageDto payload sent for backlog entries is encrypted; clients must decrypt to display the original content.
- IDs for backlog items are generated per call (Guid.NewGuid) and are not tied to persisted database IDs.

---

## JoinChannelAsync
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
public async Task<(List<MessageDto> History, string? Error, bool PasswordRequired)> JoinChannelAsync(
        string connectionId, Guid userId, string username, string channelName, string? [REDACTED:CONNECTION_STRING_PASSWORD]
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `History` | `List<MessageDto>` | — |
| `Error` | `string?` | — |
| `PasswordRequired` | `bool` | — |


Joins a user to a chat channel, performing gating, membership validation, presence tracking, and history retrieval. It first enforces a join throttle via a spam guard, then delegates the channel membership check (including any password gate) to ChannelService. If the gate fails, it returns an empty history with the reason. On a successful gate, it records the join in the presence tracker, fetches a lightweight presence snapshot for broadcasting, broadcasts the join to all connected clients (except when the user is invisible), and finally returns the channel history along with an indication that no error occurred and that no password is required.

## Remarks
This method encapsulates the end-to-end join workflow in a single, reusable operation, ensuring consistent enforcement of anti-spam, permission, and presence semantics across the chat surface. By obtaining presence information in a scoped, guarded manner, it keeps side effects localized to the join flow while enabling clients to incrementally update their views. The implementation gracefully handles failures when fetching presence data (logging at debug level) without interrupting the primary join path, and it respects user visibility by avoiding broadcasts for invisible users.

## Example
```csharp
// Example usage: a user joining a publicly accessible channel without a password
var (history, error, passwordRequired) = await chatService.JoinChannelAsync(
    connectionId: "conn-123",
    userId: userId,
    username: "Alice",
    channelName: "general",
    password: null);
```

## Notes
- If the spam guard rejects the join, the method returns immediately with an empty history and a non-null error reason; no membership or presence side effects occur.
- Invisible users will not trigger a broadcast of the join to other clients, though their history is still returned to them.
- Presence data is a best-effort fetch; failures are logged at debug level and do not prevent the join from completing or the history from being returned.

---

## SanitizeNewlines
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
private static string SanitizeNewlines(string content)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `content` | `string` | — |

**Returns:** `string`


SanitizeNewlines normalizes line endings to a single newline form, collapses consecutive blank lines to at most HubConstants.MaxConsecutiveNewlines, and truncates the total line count to HubConstants.MaxMessageNewlines. This private helper should be invoked when preparing user-provided content for transmission so that messages stay readable and within size limits, rather than letting users push uncontrolled newline spam through the chat pipeline.

## Remarks

SanitizeNewlines centralizes newline handling to ensure consistent formatting across the chat pipeline. It is driven by HubConstants thresholds, avoiding hard-coded limits and enabling consistent behavior wherever message sanitization occurs. As a pure transformation of the input with no external state, it has no side effects beyond returning a sanitized string.

## Notes

- Collapses consecutive whitespace-only lines, which can alter intended spacing in user messages.
- Truncates lines beyond MaxMessageNewlines, so content beyond the limit is dropped from the end.
- The function is private and intended for internal use within the ChatService; external callers cannot rely on it.


---