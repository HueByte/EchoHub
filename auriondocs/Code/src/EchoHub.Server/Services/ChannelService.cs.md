# ChannelService.cs

> **Source:** `src/EchoHub.Server/Services/ChannelService.cs`

## Contents

- [ChannelService](#channelservice)
  - [ChannelService (constructor)](#channelservice-constructor)
  - [CreateChannelAsync](#createchannelasync)
  - [DeleteChannelAsync](#deletechannelasync)
  - [EnsureChannelMembershipAsync](#ensurechannelmembershipasync)
  - [EnsureDefaultChannelAsync](#ensuredefaultchannelasync)
  - [EnsureSystemChannelAsync](#ensuresystemchannelasync)
  - [GetChannelByNameAsync](#getchannelbynameasync)
  - [GetChannelCryptoAsync](#getchannelcryptoasync)
  - [GetChannelKeyEnvelopeAsync](#getchannelkeyenvelopeasync)
  - [GetChannelListAsync](#getchannellistasync)
  - [GetChannelMetaAsync](#getchannelmetaasync)
  - [GetChannelTopicAsync](#getchanneltopicasync)
  - [GetChannelsAsync](#getchannelsasync)
  - [RekeyChannelAsync](#rekeychannelasync)
  - [SetChannelPasswordAsync](#setchannelpasswordasync)
  - [UpdateTopicAsync](#updatetopicasync)
  - [ValidateChannelPassword](#validatechannelpassword)

---

## ChannelService
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** class

```csharp
public class ChannelService : IChannelService
```


A high-level service that implements [`IChannelService`](../../EchoHub.Core/Contracts/IChannelService.cs.md) and centralizes channel lifecycle and membership operations for the server: listing and paging channels (`GetChannelsAsync`), creating/updating/deleting channels (`CreateChannelAsync`, `UpdateTopicAsync`, `DeleteChannelAsync`), password and encryption envelope management (`SetChannelPasswordAsync`, `RekeyChannelAsync`, `GetChannelKeyEnvelopeAsync`), and retrieving channel metadata/crypto details (`GetChannelMetaAsync`, `GetChannelCryptoAsync`). Use `ChannelService` when you need the server-side orchestration for channel policies, membership checks and the authoritative source of channel metadata and cryptographic envelopes rather than calling lower-level storage or presence primitives directly.

## Remarks
`ChannelService` acts as the application-level coordinator for channel-related concerns. It composes smaller services such as [`PresenceTracker`](PresenceTracker.cs.md), [`SpamGuard`](SpamGuard.cs.md), and [`ServerLogsService`](ServerLogs/ServerLogsService.cs.md), and enforces business rules (creator/admin permissions, role-gated system channels, creation throttling) so callers do not need to reimplement policy logic. The class is responsible for keeping cryptographic envelope state (`EncryptionSalt` / `WrappedRoomKey`) separate from message content keys and for exposing those envelopes through `GetChannelKeyEnvelopeAsync` and `GetChannelCryptoAsync` while preserving server-side metadata like sender identity counts and storage footprint.

## Notes
- The system "live log" channel has a reserved name and is role-gated: it is visible only to configured roles regardless of membership; the name remains reserved even if the feature is disabled. Be careful when creating channels with that name.
- End-to-end encrypted channels use a different flow: `SetChannelPasswordAsync` is not available for E2E channels; to change a passphrase the service uses `RekeyChannelAsync`, which swaps the join-gate hash and the wrapped room key but does not rotate the room content key (so history remains readable to clients that can re-wrap the key).
- Channel creation is subject to throttling via [`SpamGuard`](SpamGuard.cs.md) (moderators and above are exempt) and creators are automatically added as members; callers should handle [`ChannelOperationResult`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) responses (success/failure and error messages) rather than assuming the operation always succeeds.

---

### ChannelService (constructor)
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** constructor

```csharp
public ChannelService(
        IServiceScopeFactory scopeFactory,
        PresenceTracker presenceTracker,
        SpamGuard spamGuard,
        ServerLogsService serverLogs,
        ILogger<ChannelService> logger)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `scopeFactory` | `IServiceScopeFactory` | — |
| `presenceTracker` | [`PresenceTracker`](PresenceTracker.cs.md) | — |
| `spamGuard` | [`SpamGuard`](SpamGuard.cs.md) | — |
| `serverLogs` | [`ServerLogsService`](ServerLogs/ServerLogsService.cs.md) | — |
| `logger` | `ILogger<ChannelService>` | — |


Constructs a `ChannelService` by taking its required collaborators from the dependency injection container and caching them in private fields for later use. This constructor is invoked by the DI framework when creating a `ChannelService` instance, so consumers typically rely on DI rather than invoking it directly.

## Remarks
This constructor wires together a set of collaborators required by `ChannelService`: `IServiceScopeFactory` for creating scoped services, [`PresenceTracker`](PresenceTracker.cs.md) for tracking user presence, [`SpamGuard`](SpamGuard.cs.md) for abuse protection, [`ServerLogsService`](ServerLogs/ServerLogsService.cs.md) for server-side logging, and `ILogger<ChannelService>` for structured logging. By storing these dependencies in private fields, the class remains focused on channel-related behavior while delegating infrastructure concerns to dedicated services. This separation also improves testability by allowing mocks or fakes to replace the collaborators during unit tests.

---

### CreateChannelAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<ChannelOperationResult> CreateChannelAsync(
        Guid creatorUserId, string name, string? topic, bool isPublic,
        string? password = null, string? encryptionSalt = null, string? wrappedRoomKey = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `creatorUserId` | `Guid` | — |
| `name` | `string` | — |
| `topic` | `string?` | — |
| `isPublic` | `bool` | — |
| `password` | `string?` | `null` |
| `encryptionSalt` | `string?` | `null` |
| `wrappedRoomKey` | `string?` | `null` |

**Returns:** `Task<ChannelOperationResult>`


Creates a new channel with the given `creatorUserId`, `name`, optional `topic`, visibility via `isPublic`, and optional security settings (`password`, `encryptionSalt`, `wrappedRoomKey`). It validates the input (name presence, name pattern via `ValidationConstants.ChannelNameRegex()`, and reserved names against `_serverLogs.Options.NormalizedRoomName`), ensures the channel name is unique, optionally hashes a password with BCrypt, and stores envelope data only when both `encryptionSalt` and `wrappedRoomKey` are supplied. If a password or envelope is provided, the corresponding fields are populated accordingly; otherwise they remain null. The creator automatically becomes a member, and the operation is throttled by a spam guard for non-exempt users. The method persists changes and returns a successful [`ChannelOperationResult`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) containing a [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md), or a failure with a [`ChannelError`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) and message in cases of validation failure, duplication, or other policy violations.

**Remarks**
This method centralizes channel creation concerns, including input validation, security policy, and persistence, so callers don’t need to implement these cross-cutting concerns separately. It coordinates between domain entities ([`Channel`](../../EchoHub.Core/Models/Channel.cs.md), [`ChannelMembership`](../../EchoHub.Core/Models/ChannelMembership.cs.md)) and their DTOs, while enforcing organizational policies (e.g., reserved names, password requirements for encrypted channels, and anti-spam). The return shape guarantees a consistent success path with a populated [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) or a clear failure path via `ChannelOperationResult.Fail`.

**Notes**
- Normalization and validation: the stored channel name is the lowercased, trimmed form and must pass `ValidationConstants.ChannelNameRegex()`; attempting to create a channel with a name that already exists yields `ChannelError.AlreadyExists`.
- Security coupling: if an envelope is provided, a non-empty `password` is required, and the password (if any) is hashed with BCrypt; envelope data is only stored when both `encryptionSalt` and `wrappedRoomKey` are present.
- Anti-spam policy: channel creation is guarded by `_spamGuard` (non-exempt users may be blocked for rapid creation), reinforcing rate-limiting behavior at the data access boundary.


---

### DeleteChannelAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<ChannelOperationResult> DeleteChannelAsync(Guid callerUserId, string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `callerUserId` | `Guid` | — |
| `channelName` | `string` | — |

**Returns:** `Task<ChannelOperationResult>`


Deletes a channel by name, enforcing that only the channel's creator or an administrator can perform the deletion while protecting the default and system channels. The input channel name is normalized to lower-case and trimmed, a scoped [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) is used to locate the channel, and the operation returns a [`ChannelOperationResult`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) with a specific [`ChannelError`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) if the channel does not exist or cannot be deleted. If authorized, the channel is removed from the `db.Channels`, changes are persisted, and a [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) describing the deleted channel is returned inside a successful [`ChannelOperationResult`](../../EchoHub.Core/DTOs/CommonDtos.cs.md).

---

### EnsureChannelMembershipAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<(bool Success, string? Error, bool PasswordRequired)> EnsureChannelMembershipAsync(
        Guid userId, string channelName, string? password = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Success` | `bool` | — |
| `Error` | `string?` | — |
| `PasswordRequired` | `bool` | — |


Ensures that a user identified by `Guid userId` becomes a member of the channel named `channelName`, creating or restoring the channel as needed, enforcing gating rules, and returning a structured result that indicates success, a possible error message, and whether a password is required for first-time joins.

The method normalizes the channel name using `ToLowerInvariant()` and `Trim()`, then validates it with `ValidationConstants.ChannelNameRegex()`. If the name is invalid, it returns a failed result along with an error message describing the required channel name constraints. It then opens a scope and obtains an [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) to inspect and modify data related to users, channels, and memberships.

If the target channel is a live-logs channel (as determined by `_serverLogs.IsLogsChannel`), the caller’s ability to view that channel is verified via `_serverLogs.CanView` against the user’s role; otherwise membership is denied.

If the channel does not exist, the method may auto-recreate it: the default channel (as defined by `HubConstants.DefaultChannel`) is recreated with safe defaults, or a logs channel is recreated with a non-public, system-owned flag and a predefined room topic. If neither special case applies, the method reports that the channel does not exist and should be created first via the channel list.

If the channel exists but is a system channel and the request is not for a logs channel, access is blocked and the join is rejected.

When the caller is not already a member, the method enforces password protection if the channel has a `PasswordHash`. If no password is supplied, it returns success = false with `PasswordRequired` set to true. If a password is supplied but is incorrect (verified via `BCrypt.Verify`), it returns the same shape with `PasswordRequired` = true. On successful password verification (or if no password is needed), a new [`ChannelMembership`](../../EchoHub.Core/Models/ChannelMembership.cs.md) entry is created and persisted.

The function returns a 3-tuple: `(bool Success, string? Error, bool PasswordRequired)`. A successful join yields `(true, null, false)`; otherwise, `Error` describes the failure and `PasswordRequired` signals whether a password is needed for the join.


---

### EnsureDefaultChannelAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
private static async Task EnsureDefaultChannelAsync(EchoHubDbContext db)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `db` | [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) | — |

**Returns:** `Task`


Ensures that the default channel exists in the database by checking for a channel named `HubConstants.DefaultChannel`. If none exists, it creates a new [`Channel`](../../EchoHub.Core/Models/Channel.cs.md) with a generated `Id` (`Guid.NewGuid()`), the default name, a `Topic` of `General discussion`, and a system `CreatedByUserId` of `Guid.Empty`, then saves changes with `SaveChangesAsync`.

## Remarks
Centralizes the provisioning of the default channel, letting startup and runtime logic rely on a known channel name without duplicating initialization checks. By using `HubConstants.DefaultChannel` and `Guid.Empty` as the creator, it signals that the record is system-generated and intended as a baseline rather than user-created.

## Notes
- Potential race condition if this method is invoked concurrently during initialization; ensure it runs once or enforce a database constraint on `Channels.Name` to prevent duplicates.

---

### EnsureSystemChannelAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<ChannelDto> EnsureSystemChannelAsync(string channelName, string? topic = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `topic` | `string?` | `null` |

**Returns:** `Task<ChannelDto>`


Ensures that a system channel with the specified name exists in the database by normalizing the name and looking it up. If none is found, it creates a new system channel (not public) with CreatedByUserId set to an empty GUID and logs its creation. If a non-system channel already exists with that name, it is claimed as a system channel by updating its IsSystem and IsPublic flags and clearing the PasswordHash, logging a warning. It returns a ChannelDto describing the channel's identity and status.

## Remarks
This method centralizes the architectural concept of system channels by guaranteeing a canonical system channel for a given name, creating or reclaiming it as needed and thereby preventing user-owned channels from shadowing system channels with reserved identifiers.

---

### GetChannelByNameAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<ChannelDto?> GetChannelByNameAsync(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `Task<ChannelDto?>`


Fetches a channel by name in a case-insensitive manner and returns a compact [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) that includes the channel’s identity, metadata, and the current message count. It normalizes the input, creates a short-lived DI scope to obtain the [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md), resolves the channel by its lowercased name, counts its related [`Message`](../../EchoHub.Core/Models/Message.cs.md)s, and returns a [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) populated with the channel’s id, name, topic, visibility, created timestamp, and flags indicating whether a password or a wrapped room key exists, plus whether it is a system channel. If no channel matches, it returns `null`.

## Remarks
By encapsulating the read path behind `GetChannelByNameAsync`, callers avoid dealing with EF queries or DI lifetimes directly. It centralizes how channel metadata is retrieved and projected into a [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md), which helps maintain consistent data contracts across the application. The per-call scope ensures proper disposal of the [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) and aligns with typical request-scoped lifetimes.

## Notes
- Potential ambiguity if multiple channels share the same normalized name; `FirstOrDefaultAsync` may return any one of them.
- Two database round-trips per invocation: one to fetch the channel and another to count its messages; consider combining into a single query if profiling shows this as a bottleneck.

---

### GetChannelCryptoAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<ChannelCryptoDto?> GetChannelCryptoAsync(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `Task<ChannelCryptoDto?>`


GetChannelCryptoAsync retrieves the cryptographic metadata for a named channel. It normalizes the input channel name by lowercasing and trimming, opens a short-lived scoped [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md), and queries the `Channels` set for a channel whose `Name` matches the normalized value. If the channel is found, it returns a [`ChannelCryptoDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) whose first component indicates whether a `WrappedRoomKey` is present and whose second component carries the channel's `EncryptionSalt`; if no channel matches, it returns null.

## Remarks
By encapsulating this logic in a dedicated method, callers avoid duplicating the database query and the cryptographic-state interpretation across the codebase. It centralizes encryption-metadata access behind a simple, asynchronous call and uses a scoped DbContext to minimize lifetime and concurrency issues.

## Notes
- Returns null when the channel does not exist.
- The first component of [`ChannelCryptoDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) indicates the presence of a `WrappedRoomKey`; the `EncryptionSalt` may be null depending on data, so callers should handle null salts.

---

### GetChannelKeyEnvelopeAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<(string? EncryptionSalt, string? WrappedRoomKey)> GetChannelKeyEnvelopeAsync(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `EncryptionSalt` | `string?` | — |
| `WrappedRoomKey` | `string?` | — |


Gets the encryption envelope for a channel by name. It normalizes the input with `ToLowerInvariant()` and `Trim()`, opens a short-lived DI scope to resolve [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md), and queries the `Channels` set for a channel whose `Name` matches. It returns a tuple of the channel's `EncryptionSalt` and `WrappedRoomKey` (as `string?`); if no matching channel exists, both values are `null`.

## Remarks
This method centralizes access to channel encryption metadata and hides the details of DI-scoped DbContext usage from callers. It provides a single, easy-to-consume envelope for encryption-related data, which is useful when preparing to decrypt or unwrap channel-specific material. By returning `(string? EncryptionSalt, string? WrappedRoomKey)` as nullable values instead of throwing when a channel is absent, callers must handle the absence gracefully.

## Notes
- Returns `(null, null)` when the channel cannot be found.
- Each invocation creates a new DI scope, which is appropriate for isolated data access but may have perf implications in hot paths; consider scope management or caching at a higher level if this method is called frequently.


---

### GetChannelListAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<List<ChannelListItem>> GetChannelListAsync()
```

**Returns:** `Task<List<ChannelListItem>>`


GetChannelListAsync asynchronously loads all channels from the database, orders them by `Name`, and projects each channel into a [`ChannelListItem`](../../EchoHub.Core/Contracts/IChannelService.cs.md) that includes the channel's `Name`, `Topic`, the current online user count from `_presenceTracker.GetOnlineUsersInChannel(c.Name).Count`, the public status (`c.IsPublic`), and whether a password is configured (`c.PasswordHash != null`). The method returns a `List<ChannelListItem>` suitable for rendering a channel catalog in a UI or API response.

## Remarks
GetChannelListAsync acts as an orchestrator between the persistent store ([`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md)) and the in‑memory presence tracker (`_presenceTracker`). It centralizes channel-list assembly so callers don't need to know how presence counts are computed or how channels are stored. By resolving [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) within a short‑lived scope via `_scopeFactory.CreateScope()`, it ensures proper disposal of the database context per invocation and keeps DI concerns isolated from consumer code.

## Notes
- Presence counts are computed per channel; listing many channels may impact response time. If the channel catalog grows large, consider caching or batching presence data to improve responsiveness.

---

### GetChannelMetaAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<ChannelMetaDto?> GetChannelMetaAsync(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `Task<ChannelMetaDto?>`


GetChannelMetaAsync retrieves the metadata for a channel by its name and returns a [`ChannelMetaDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) (or null if the channel cannot be found). It normalizes the input with `ToLowerInvariant()` and `Trim()`, opens a scoped [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) via `_scopeFactory.CreateScope()`, and looks up the channel in `db.Channels` by `Name`. When found, it computes the total `messageCount` from `db.Messages.CountAsync(...)`, the number of distinct `SenderUserId`s, and the estimated on-disk footprint from attachments and message text, then returns a new [`ChannelMetaDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) containing `c.Id`, `c.Name`, `c.Topic`, booleans for `c.WrappedRoomKey != null` and `c.PasswordHash != null`, the counts, the total footprint, and `c.CreatedAt`.

## Remarks
This method uses a scoped [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) to perform multiple read-only queries and aggregates data from `db.Channels` and `db.Messages`. The returned booleans reflect whether `c.WrappedRoomKey` or `c.PasswordHash` are non-null, indicating encryption and access protection. For encrypted channels, the footprint uses ciphertext sizes to reflect on-disk cost, and sender identities are treated as metadata preserved by the server even when messages are encrypted.

## Notes
- Callers must handle the possibility that the return value is `null` when no channel matches the given `channelName`.

---

### GetChannelTopicAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<(string? Topic, bool Exists)> GetChannelTopicAsync(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Topic` | `string?` | — |
| `Exists` | `bool` | — |


GetChannelTopicAsync retrieves the topic for a channel identified by `channelName`. It normalizes the input by calling `ToLowerInvariant()` and `Trim()`, opens a short-lived DI scope to resolve [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md), queries the `Channels` set for a channel whose `Name` equals the normalized value using `FirstOrDefaultAsync`, and returns the `(Topic, Exists)` tuple; if no channel exists, it returns `(null, false)`.

## Remarks
Encapsulates a small piece of data access behind a scoped context, avoiding long-lived DbContext usage and centralizing the normalization logic for channel lookups. The API communicates existence via the `Exists` flag, while the `Topic` can still be `null` if a channel exists but has no topic set.

## Notes
- The lookup uses `FirstOrDefaultAsync` on `db.Channels`; if more than one channel shares the same normalized `Name`, the returned topic is non-deterministic; enforce unique `Name` values to avoid surprises.
- A per-call DI scope is created to obtain [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md); callers should not rely on an ambient scope for this operation.

---

### GetChannelsAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<PaginatedResponse<ChannelDto>> GetChannelsAsync(Guid userId, int offset, int limit)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `userId` | `Guid` | — |
| `offset` | `int` | — |
| `limit` | `int` | — |

**Returns:** `Task<PaginatedResponse<ChannelDto>>`


GetChannelsAsync returns a paginated list of channels visible to the user identified by `userId`. It first ensures a default channel exists, then determines if the caller can view system channels via `_serverLogs.CanView(caller?.Role ?? ServerRole.Member)`, and finally queries `db.Channels` to surface system channels only when permitted or non-system channels that are public or where the user is a member (via `ChannelMemberships`). The results are ordered with system channels first, then by `Name`, and projected into [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) objects containing each channel’s `Id`, `Name`, `Topic`, `IsPublic`, `Messages.Count`, `CreatedAt`, and flags for `PasswordHash != null` and `WrappedRoomKey != null`, plus `IsSystem`. The method returns a [`PaginatedResponse<ChannelDto>`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) with the current page of channels and the total count.

## Remarks
GetChannelsAsync centralizes the channel visibility policy: system channels (the live log room) are exposed only to users whose role allows viewing server logs, while non-system channels are visible if they are public or the user is a member, as determined by `ChannelMemberships`. The results are produced from a scoped [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) and are ordered to surface system channels first, then alphabetically by name, and are projected into lightweight [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) records to drive UI lists without leaking unnecessary data. This encapsulation ensures consistent, permission-aware channel listing across the application.


---

### RekeyChannelAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<ChannelOperationResult> RekeyChannelAsync(Guid callerUserId, string channelName,
        string oldPassword, string newPassword, string newEncryptionSalt, string newWrappedRoomKey)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `callerUserId` | `Guid` | — |
| `channelName` | `string` | — |
| `oldPassword` | `string` | — |
| `newPassword` | `string` | — |
| `newEncryptionSalt` | `string` | — |
| `newWrappedRoomKey` | `string` | — |

**Returns:** `Task<ChannelOperationResult>`


RekeyChannelAsync rekeys an end-to-end encrypted channel by swapping the `join-gate` hash and the `WrappedRoomKey`, re-wrapping the channel's content key under the new passphrase-derived key while leaving the content key itself unchanged so historical messages remain decryptable. The operation is restricted to the channel creator; admins cannot rekey a room unless they know the current passphrase.

Passphrase changes are validated, and the operation returns a [`ChannelOperationResult`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) that is either a success containing a [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) or a failure with a [`ChannelError`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) and message (e.g. `NotFound`, `ValidationFailed`, or `Forbidden`). Internally, the method uses a scoped [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) to locate the channel by name, ensure the channel is end-to-end encrypted, verify the caller is the creator, check the old password, and persist updates to `PasswordHash`, `EncryptionSalt`, and `WrappedRoomKey`. Upon success, it computes the current message count and returns a [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) reflecting the updated credentials.

This operation centralizes the sensitive rekey workflow and ensures the channel state remains consistent and auditable within a single database transaction.


---

### SetChannelPasswordAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<ChannelOperationResult> SetChannelPasswordAsync(Guid callerUserId, string channelName, string? password)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `callerUserId` | `Guid` | — |
| `channelName` | `string` | — |
| `password` | `string?` | — |

**Returns:** `Task<ChannelOperationResult>`


Sets, changes, or clears (null) a channel's join password. Creator or admin only. Not available on end-to-end encrypted channels — those change passphrase via RekeyChannelAsync so the room key envelope stays consistent. The method normalizes the channel name to lowercase and trims, validates the password via `ValidateChannelPassword`, and then uses a scoped [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) to locate the channel and enforce authorization. If the channel doesn't exist, is a system channel, or is end-to-end encrypted, it returns an appropriate [`ChannelOperationResult`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) failure. If the caller is the channel creator or an admin, it updates the channel's `PasswordHash` (hashing a non-null password with `BCrypt.Net.BCrypt.HashPassword` or clearing it when `password` is null), persists the changes, counts the channel's messages, and returns a [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) describing the channel along with flags for password protection and encryption.

## Remarks
Centralizes channel password management behind a single operation that enforces ownership and role-based access. It interacts with the EF Core context to fetch and persist channel state and to surface up-to-date metadata via [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) (including whether a password is active and whether the channel is end-to-end encrypted). The method explicitly avoids modifying end-to-end encrypted channels here, directing such changes to `RekeyChannelAsync` to preserve the room key envelope.

## Notes
- Clearing the password (passing `null`) removes the join password, which may affect who can join depending on the channel's other visibility settings.
- Only the channel creator or an admin can perform password changes; otherwise the call returns `ChannelError.Forbidden`.


---

### UpdateTopicAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<ChannelOperationResult> UpdateTopicAsync(
        Guid callerUserId, string channelName, string? topic)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `callerUserId` | `Guid` | — |
| `channelName` | `string` | — |
| `topic` | `string?` | — |

**Returns:** `Task<ChannelOperationResult>`


Updates the topic of a channel by name, but only if the caller is the channel's creator. It trims and validates a non-null `topic` against `ValidationConstants.MaxChannelTopicLength` (a null `topic` clears the topic), normalizes the channel name to lower-case, persists the change via EF Core, and returns a `ChannelOperationResult.Success` with a [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) containing the updated channel data plus a live `MessageCount`. If the channel is missing, or the caller isn't the creator, or the topic is too long, the method returns a corresponding failure via `ChannelOperationResult.Fail` with an appropriate [`ChannelError`](../../EchoHub.Core/DTOs/CommonDtos.cs.md).

## Remarks

Only the channel creator can update the topic, enforced by comparing `dbChannel.CreatedByUserId` to `callerUserId`. The method uses a short-lived DI scope to fetch [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md), performs a read of the channel by name, applies the update, saves changes, and then counts the channel's `Messages` to populate the `MessageCount` in the returned [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md). The [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) also exposes security-related flags derived from `PasswordHash` and `WrappedRoomKey` to help clients adjust their UI and access logic.

## Notes

- The `MessageCount` is retrieved via `db.Messages.CountAsync(m => m.ChannelId == dbChannel.Id)` after applying the update; for very active channels this can add latency. 
- Passing a `null` `topic` clears the topic; callers should handle potential null values in the UI.

---

### ValidateChannelPassword
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
private static string? ValidateChannelPassword(ref string? password)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `password` | `string?` | — |

**Returns:** `string?`


Normalizes a provided channel password by treating whitespace-only input as the absence of a password (`null`) and then enforces length constraints from [`ValidationConstants`](../../EchoHub.Core/Constants/ValidationConstants.cs.md) (minimum via `MinChannelPasswordLength`, maximum via `MaxPasswordLength`). It returns an error message when the password is too short or too long, or `null` when the value is valid.

## Remarks

By using a `ref` parameter for `password`, the input variable may be mutated to `null` by the callee to reflect the decision that no password is set. This centralizes channel password rules in one place, ensuring consistent behavior across channel creation and update flows.

## Notes

- Because the parameter is `ref`, the caller should re-read the original variable after the call because its value may have been changed to `null`.
- A return value of `null` indicates a valid or absent password; non-null strings are error messages describing the violation.

---