# ChatService.cs

> **Source:** `src/EchoHub.Server/Services/ChatService.cs`

## Contents

- [ChatService](#chatservice)
  - [ChatService (constructor)](#chatservice-constructor)
  - [BroadcastChannelDeletedAsync](#broadcastchanneldeletedasync)
  - [BroadcastChannelUpdatedAsync](#broadcastchannelupdatedasync)
  - [BroadcastMessageAsync](#broadcastmessageasync)
  - [BroadcastToAllAsync](#broadcasttoallasync)
  - [BuildLogBacklog](#buildlogbacklog)
  - [BuildReplyRef](#buildreplyref)
  - [FileIdFromUrl](#fileidfromurl)
  - [GetChannelHistoryAsync](#getchannelhistoryasync)
  - [GetChannelHistoryInternalAsync](#getchannelhistoryinternalasync)
  - [GetChannelsForUserAsync](#getchannelsforuserasync)
  - [GetOnlineUsersAsync](#getonlineusersasync)
  - [JoinChannelAsync](#joinchannelasync)
  - [LeaveChannelAsync](#leavechannelasync)
  - [SanitizeNewlines](#sanitizenewlines)
  - [SendMessageAsync](#sendmessageasync)
  - [UpdateStatusAsync](#updatestatusasync)
  - [UserConnectedAsync](#userconnectedasync)
  - [UserDisconnectedAsync](#userdisconnectedasync)

---

## ChatService
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** class

```csharp
public class ChatService : IChatService
```


Coordinates chat-related operations for the server-side hub: connection lifecycle, channel joins/leaves, message sending, history retrieval and broadcasting. Reach for `ChatService` when you need a single, authoritative orchestrator that applies presence tracking, channel validation, encryption/decryption, spam/mute rules, link-embed enrichment, storage and multi-backend broadcasting rather than implementing those concerns in a hub or duplicating them across callers.

## Remarks
`ChatService` centralizes cross-cutting chat logic so transport implementations (for example SignalR or an IRC bridge) can remain thin. It delegates channel membership and validation to [`IChannelService`](../../EchoHub.Core/Contracts/IChannelService.cs.md), relies on [`PresenceTracker`](PresenceTracker.cs.md) for presence state, uses [`IMessageEncryptionService`](../../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md) and [`LinkEmbedService`](LinkEmbedService.cs.md) to handle encrypted payloads and link previews, enforces anti-abuse via [`SpamGuard`](SpamGuard.cs.md), persists attachments via [`FileStorageService`](FileStorageService.cs.md), and emits audit/operational data to [`ServerLogsService`](ServerLogs/ServerLogsService.cs.md) and [`ServerStatsCollector`](Stats/ServerStatsCollector.cs.md). Outgoing delivery is performed by the configured [`IChatBroadcaster`](../../EchoHub.Core/Contracts/IChatBroadcaster.cs.md) implementations so the same message lifecycle (validation, enrichment, storage) can be broadcast to multiple transports consistently.

## Notes
- Join throttling only counts first-time joins (no existing membership) to avoid tripping during normal auto-join bursts on reconnect; clients that re-join known channels should not trigger the throttle.
- The special "log"/live-log room is treated as read-only and its backlog comes from rolling log files rather than DB messages; paging behaves differently for that channel.
- Spam checks operate on the stored content (which may be ciphertext for end-to-end encrypted rooms) — the guard does not require plaintext to function and escalation results in timed mutes issued by the server.
- Message handling includes decryption (clients may send encrypted payloads while other protocols supply plaintext), stripping of any explicit encryption prefix typed by users to prevent spoofing, and plaintext sanitization (for example collapsing excessive newlines) before optional embed fetching and storage.

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


Initializes a new `ChatService` instance by capturing its required collaborators through dependency injection and storing them in private fields for later use. The constructor takes services for scope management (`IServiceScopeFactory`), presence tracking ([`PresenceTracker`](PresenceTracker.cs.md)), a collection of broadcasters ([`IChatBroadcaster`](../../EchoHub.Core/Contracts/IChatBroadcaster.cs.md)), link embedding ([`LinkEmbedService`](LinkEmbedService.cs.md)), message encryption ([`IMessageEncryptionService`](../../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md)), channel operations ([`IChannelService`](../../EchoHub.Core/Contracts/IChannelService.cs.md)), file storage ([`FileStorageService`](FileStorageService.cs.md)), spam protection ([`SpamGuard`](SpamGuard.cs.md)), server-side logging ([`ServerLogsService`](ServerLogs/ServerLogsService.cs.md)), statistics collection ([`ServerStatsCollector`](Stats/ServerStatsCollector.cs.md)), and a logger (`ILogger<ChatService>`), wiring them to internal fields like `_scopeFactory`, `_presenceTracker`, `_broadcasters`, `_embedService`, `_encryption`, `_channelService`, `_fileStorage`, `_spamGuard`, `_serverLogs`, `_statsCollector`, and `_logger` so the service can perform broadcasting, embedding, encryption, channel management, persistence, spam guarding, logging, and metrics collection.

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


BroadcastChannelDeletedAsync asynchronously broadcasts a channel-deleted event to all connected clients by invoking `SendChannelDeletedAsync` on each subscriber, via the central `BroadcastToAllAsync` mechanism using the lambda `b => b.SendChannelDeletedAsync(channelName)`. It accepts a `string channelName` and returns a `Task` representing the asynchronous broadcast operation.

## Remarks
This method is a thin facade over the generic broadcasting path. It delegates to `BroadcastToAllAsync` to deliver the `SendChannelDeletedAsync` call to every connected client, isolating the channel-deletion notification from the underlying broadcast implementation and ensuring consistent semantics across different events.

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


BroadcastChannelUpdatedAsync forwards the given [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) and an optional `string? channelName` to all connected clients by routing through the shared broadcast pipeline: it calls `BroadcastToAllAsync` with a lambda that invokes each client's `SendChannelUpdatedAsync`.

## Remarks
Thin wrapper around the existing broadcast mechanism for the 'channel updated' event. It centralizes the notification path so UI clients stay in sync when a channel changes, and it decouples `ChatService` from the concrete hub method used to push updates. If you add additional update events later, similar wrappers can be introduced to keep the surface area small and consistent.

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


BroadcastMessageAsync is a thin asynchronous wrapper that broadcasts a [`MessageDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) to a named channel by delegating to the shared broadcast pipeline `BroadcastToAllAsync`. It forwards the `channelName` and `message` to every recipient by invoking the lambda `b => b.SendMessageToChannelAsync(channelName, message)`.

## Remarks
This method acts as a channel-scoped entry point for the generic broadcast mechanism, decoupling channel-specific semantics from the underlying broadcasting orchestration. By composing with the `BroadcastToAllAsync` pipeline, it ensures consistent delivery behavior across recipients while allowing the underlying strategy to evolve without changing the public API. The wrapper also simplifies testing by isolating the channel-binding logic from the broadcast traversal.


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


BroadcastToAllAsync iterates over the collection of `_broadcasters` and applies the provided `Func<IChatBroadcaster, Task>` to each broadcaster, awaiting the resulting task before moving to the next. If an invocation throws, the exception is caught and logged via `_logger.LogError`, including the broadcaster's type name from `broadcaster.GetType().Name`, and processing continues with the remaining broadcasters. Use this helper when you need to perform a common asynchronous operation across all configured broadcasters while tolerating individual failures.

---

### BuildLogBacklog
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


Turns the log backlog into transport-encrypted [`MessageDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md)s so past log lines render identically to streamed messages on the client. It reads backlog entries from `_serverLogs.ReadBacklog()`, encrypts each entry’s content with `_encryption.Encrypt(entry.Content)`, assigns a fresh `Guid` via `Guid.NewGuid()`, uses `ServerLogsService.SenderName` as the sender, attaches the provided `channelName`, and preserves each backlog entry’s `Timestamp`. This method never touches the database.

## Remarks
Acts as an adapter that repackages backlog entries into the identical [`MessageDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) format used for real-time messages, enabling a seamless, consistent rendering experience for historical logs. It centralizes encryption and ID generation at the point of backlog materialization, reducing divergence between what clients see in history and what they see in live streams.

## Notes
- The `Id` of each [`MessageDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) is generated per invocation with `Guid.NewGuid()`, so IDs are not stable across reloads.
- This method is private; it is an internal helper that shapes backlog data specifically for the client-render path and is not directly callable from outside.

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


Builds a wire reference for a reply target by returning a [`ReplyRefDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) that contains the target’s ID, the sender’s username, and an encrypted surface plaintext. It decrypts the target’s `Content` to obtain plaintext; if the decrypted text is not a room ciphertext (i.e. `!RoomCrypto.IsRoomCiphertext(plain)`) and longer than 120 characters, it truncates to 120 characters and appends an ellipsis. The (potentially truncated) plaintext is then encrypted again with `_encryption.Encrypt` before being stored in the [`ReplyRefDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) alongside the target’s `Id` and `SenderUsername`.

## Remarks
This method centralizes the policy for constructing reply references: End-to-End room ciphertext is preserved as ciphertext and not truncated in this pass, while non-End-to-End plaintext is surfaced only as a concise preview. It coordinates with `_encryption` and [`RoomCrypto`](../../EchoHub.Core/Security/RoomCrypto.cs.md) to decide truncation and to produce a transport-ready reference that the client can render without exposing raw plaintext.

## Notes
- Truncation uses an ellipsis character `…` and a hard limit of 120 characters for non-E2E plaintext.


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


Extracts the storage file id from an attachment URL by taking the last path segment (for example '/api/files/{id}'). This private helper is used when only the id is needed from a known URL rather than maintaining the id separately. It relies on splitting the URL on '/' and selecting the final segment with `[^1]`, without performing further validation.

## Remarks
By centralizing the assumption that the file id is the last path segment, this helper reduces duplication and keeps callers focused on higher-level logic. It relies on a simple split-and-select approach and does not validate edge cases such as trailing slashes or query parameters.

## Notes
- Trailing slash or query string edge cases may yield an empty result or a value containing extraneous parts.
- No input validation: passing `null` or clearly malformed URLs will throw at runtime.

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


Gets a paginated history of messages for a specified channel. The method normalizes `channelName` to lowercase and trims it, clamps `count` to `ValidationConstants.MaxHistoryCount`, and ensures `offset` is non-negative. If the channel is a log-backed channel (no database messages), it returns the backlog on the first page via `BuildLogBacklog` and an empty list for subsequent pages. Otherwise, it creates a DI scope to obtain an [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) and delegates to `GetChannelHistoryInternalAsync` to fetch messages from the database.

## Remarks
Log-backed channels are served from the rolling log backlog, while database-backed channels fetch history from the [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md). The method unifies access to channel history by hiding the data source, but preserves the backlog-first paging contract for log channels (as documented in the inline comment).

## Notes
- For log-backed channels, requesting an `offset` > 0 returns an empty list; only the first page can include backlog items.
- Channel name normalization is performed before retrieval; callers may pass mixed-case or whitespace around the channel name.

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


Fetches a batch of messages for a named channel and returns them as `List<MessageDto>` after assembling sender metadata, decryption, and attachment/embed preparation. It first resolves the channel by name via `EchoHubDbContext.Channels`; if the channel cannot be found, it returns an empty list. To preserve tombstoned messages (where a sender account has been deleted), it performs a left join with `Users` so messages can still appear with null `NicknameColor` and `DisplayName`, then applies pagination with `offset` and `count` and finally reverses the results to chronological order. The method also gathers reply targets for quotes, groups attachments by message, validates attachments against the current file store via `_fileStorage.GetStoredFileIds()`, decrypts message content using `_encryption.Decrypt`, and decrypts/deserializes embeds from `EmbedJson` using `JsonSerializer`. Attachments are pruned if their underlying files are missing; if a message has no live attachments and no plaintext content, it is pruned from the result. Attachments and previews are re-encrypted for transport, and embedded metadata (when valid JSON) is deserialized into [`EmbedDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) instances. This function coordinates with its dependencies to ensure only live content is delivered and that the client receives an encryption-safe, transport-ready history payload.


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


This method is a thin wrapper around `_presenceTracker.GetChannelsForUser` that returns the channels for a given `username` as a `Task<List<string>>`. It preserves the asynchronous API surface while delegating the actual lookup to the presence tracker.

## Remarks
By delegating to `_presenceTracker`, this symbol keeps the `ChatService` decoupled from the concrete presence-tracking implementation. This makes it easier to test `GetChannelsForUserAsync` in isolation and to swap the presence logic without changing callers, while still offering a stable public surface via `GetChannelsForUserAsync`.

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


GetOnlineUsersAsync returns the current online users for a given channel as a list of [`UserPresenceDto`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md) records. The channel name is normalized with `ToLowerInvariant()` and trimmed, then the in-memory `_presenceTracker` is consulted to obtain the set of online usernames for that channel. A scoped [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) is then used to query `db.Users` for those usernames that are not `UserStatus.Invisible`, materializing the result with `ToListAsync()`. Finally, the code maps each [`User`](../../EchoHub.Core/Models/User.cs.md) to a [`UserPresenceDto`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md), including the `IsIrcOnly` flag via `_presenceTracker.IsIrcOnly(u.Username)`, and returns the list.

## Remarks
The method bridges in-memory presence information with the persisted user data, encapsulating the lookup so callers need only know a channel name to obtain current participants. It also enforces visibility rules by excluding users with `UserStatus.Invisible` and by deriving the `IsIrcOnly` state from the live tracker rather than from the database. The scoped [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) usage respects dependency injection lifetimes and limits the DbContext to the operation's boundary.

## Notes
- Be aware that the query loads a list of [`User`](../../EchoHub.Core/Models/User.cs.md) records for all online usernames; channels with large online counts could have performance implications, and paging or batching may be warranted in high-traffic scenarios.

---

### JoinChannelAsync
> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** method

```csharp
public async Task<(List<MessageDto> History, string? Error, bool PasswordRequired)> JoinChannelAsync(
        string connectionId, Guid userId, string username, string channelName, string? password = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `History` | `List<MessageDto>` | — |
| `Error` | `string?` | — |
| `PasswordRequired` | `bool` | — |


Joins a user to a chat channel by orchestrating normalization, anti-spam checks, membership validation, presence setup, and history retrieval in a single, centralized workflow. When a client requests to join a channel, `JoinChannelAsync` lowercases and trims the channel name, enforces a first-time-join throttle via `_spamGuard`, delegates membership and potential password gating to `_channelService.EnsureChannelMembershipAsync`, registers the user in `_presenceTracker`, broadcasts the join to other clients (excluding invisible users via `UserStatus.Invisible`), and finally returns the channel history via `GetChannelHistoryAsync` along with any error and a `PasswordRequired` flag for future joins.

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


Normalizes the channel name to a canonical form using `ToLowerInvariant()` and `Trim()`, then updates presence via `_presenceTracker.LeaveChannel(username, channelName)`, broadcasts a user-left notification to all connected clients through `BroadcastToAllAsync`, and logs a debug entry with the user and channel via `_logger.LogDebug("{User} left channel '{Channel}'", username, channelName)`.

## Remarks
By encapsulating normalization, presence update, and broadcast in a single method, this symbol provides a consistent, reusable leave operation for the chat service. It ensures that all participants are informed of departures and that the server's presence state stays in sync across callers. The normalization step guarantees that channel identity is consistent, preventing duplicate or missed leaves due to casing.

## Notes
- Channel identity is normalized to lowercase; avoid relying on mixed-case channel names.
- This method is asynchronous; callers should `await` it to ensure the left-notification is delivered before proceeding.

---

### SanitizeNewlines
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


SanitizeNewlines is a private helper that cleans up a string by normalizing newline endings and trimming excessive blank lines to prevent newline spam in messages. It converts all CRLF/CR endings to LF, collapses runs of whitespace-only lines to at most `HubConstants.MaxConsecutiveNewlines` in a row, and caps the total line count to `HubConstants.MaxMessageNewlines` before returning the result.

## Remarks
SanitizeNewlines encapsulates formatting hygiene, centralizing newline handling behind a single, configurable policy. By relying on [`HubConstants`](../../EchoHub.Core/Constants/HubConstants.cs.md), the behavior can be tuned without changing call sites, and its private scope keeps the class’s public surface area focused on higher-level responsibilities for chat content processing.

## Notes
- If `HubConstants.MaxMessageNewlines` is configured to 0 or negative, the method may return an empty string, effectively dropping content.
- The method treats any line consisting only of whitespace as a blank line, so lines that look empty but contain spaces or tabs contribute to the consecutive-blank budget and may be collapsed accordingly.

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


Sends a message from a user to a channel by validating the channel name, enforcing read-only constraints, decrypting and sanitizing the content, and applying user-state checks and anti-spam rules before proceeding with the submission pipeline. If any validation fails, the method returns a descriptive error string (for example, "Invalid channel name." or "Channel '{channelName}' does not exist."). The operation normalizes the channel name with the regex from `ValidationConstants.ChannelNameRegex()` and enforces the maximum length via `HubConstants.MaxMessageLength`. It rejects writes to log/system channels (`_serverLogs.IsLogsChannel(...)`) and decrypts the incoming content with `_encryption.Decrypt`, removing any literal `'$ENC$'` prefix before validation. After sanitizing newlines, it opens a DI scope to obtain an [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md), resolves the target [`Channel`](../../EchoHub.Core/Models/Channel.cs.md) (ensuring it exists and is not system-only), and loads the [`User`](../../EchoHub.Core/Models/User.cs.md) to check mute status (including auto-unmuting if the mute has expired). A spam guard (`_spamGuard.CheckMessage`) may auto-mute or reject the message depending on the verdict (`SpamVerdictKind.AutoMute` or `SpamVerdictKind.Rejected`). If a `replyToMessageId` is provided, the method validates the target message exists within the same channel. It also attempts to fetch URL embeds in a guarded block via `_embedServic` to enrich the message without destabilizing the submission flow. All persistence and side-effects occur within the scoped context, and the method returns a user-facing string on fail or proceeds with the normal submission path on success. The orchestration relies on several collaborators, including [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md), [`EmbedDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md), [`MessageDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md), [`ValidationConstants`](../../EchoHub.Core/Constants/ValidationConstants.cs.md), and [`HubConstants`](../../EchoHub.Core/Constants/HubConstants.cs.md), to enforce channel hygiene, user state, and content enrichment.

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


Updates a user\`s `Status` and optional `StatusMessage`, persists the change to the database, and broadcasts the new presence to all subscribed channels. It validates that the `status` is a defined enum value (guarding against undefined bindings via `Enum.IsDefined`), and enforces the maximum length for `statusMessage` using `ValidationConstants.MaxStatusMessageLength`; on failure it returns a string error, otherwise it returns `null` after a successful update.

It uses a scoped DI container to resolve [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md), loads the user by `userId`, updates `Status`, `StatusMessage` (trimmed), and `LastSeenAt` to `DateTimeOffset.UtcNow`, saves changes, builds a [`UserPresenceDto`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md) for broadcasting, determines the channels with `_presenceTracker.GetChannelsForUser(username)`, and notifies clients via `BroadcastToAllAsync` calling `SendUserStatusChangedAsync` with the presence payload.

## Remarks
By performing the work inside a scoped container, the method keeps the Entity Framework context life cycle local to the operation, avoiding leaks across requests. It constructs a [`UserPresenceDto`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md) containing the user's identity and presence details, which is then broadcast to all relevant channels via `BroadcastToAllAsync` and `SendUserStatusChangedAsync`. It also records whether the user is IRC-only using `_presenceTracker.IsIrcOnly(user.Username)` as part of the presence payload, ensuring clients receive a faithful representation of user state.

## Notes
- The `statusMessage` is trimmed before persistence; a null value yields a null field in storage.


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


UserConnectedAsync handles a user establishing a real-time connection by recording the connection with the in-memory presence tracker (`_presenceTracker`), updating the live online user count via the stats collector (`_statsCollector`), and, within a short-lived scope, persisting the user's state in the database ([`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md)). If a [`User`](../../EchoHub.Core/Models/User.cs.md) exists for the provided `userId`, it updates `LastSeenAt` to `DateTimeOffset.UtcNow` and sets `Status` to `UserStatus.Online`, then saves changes with `SaveChangesAsync`. Finally, it emits a debug log with `username` and `connectionId` via `_logger.LogDebug`.

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


Handles a user disconnection by resolving the provided `connectionId` to a `username` via `_presenceTracker`, capturing the channels the user was in, and then marking the user as disconnected. If a `username` exists and the user is no longer online, it creates a scoped [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md), updates the corresponding [`User`](../../EchoHub.Core/Models/User.cs.md)'s `LastSeenAt` to `DateTimeOffset.UtcNow` and `Status` to `UserStatus.Invisible`, saves changes, constructs a [`UserPresenceDto`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md) with the updated presence, and broadcasts the status change to the previously observed channels via `BroadcastToAllAsync` with `SendUserStatusChangedAsync`. Finally, it logs the disconnect with `_logger` and returns the `username` (which may be null).

---