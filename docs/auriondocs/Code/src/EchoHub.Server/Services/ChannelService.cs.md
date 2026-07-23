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
  - [GetChannelListAsync](#getchannellistasync)
  - [GetChannelMetaAsync](#getchannelmetaasync)
  - [GetChannelsAsync](#getchannelsasync)
  - [RekeyChannelAsync](#rekeychannelasync)
  - [SetChannelPasswordAsync](#setchannelpasswordasync)
  - [UpdateTopicAsync](#updatetopicasync)
  - [ValidateChannelPassword](#validatechannelpassword)
- [GetChannelKeyEnvelopeAsync](#getchannelkeyenvelopeasync)
- [GetChannelTopicAsync](#getchanneltopicasync)

---

## ChannelService
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** class

```csharp
public class ChannelService : IChannelService
```


Manages server-side channel (room) operations: creation, deletion, listing and metadata, membership enforcement, password gating, and end-to-end encryption key envelopes. Reach for ChannelService when you need authoritative server logic that enforces channel rules and persists channel state (including password and E2E envelope handling), rather than making client-side assumptions or manipulating storage directly.

## Remarks
ChannelService is the central server implementation of IChannelService and enforces policy around channels: who may see or join rooms, how passwords and encryption envelopes are handled, and how the system "log" room is treated differently from ordinary channels. It coordinates presence tracking, spam-throttling (via SpamGuard), and server logging to ensure operations such as channel creation, rekeying, and membership checks are performed consistently and safely. The service preserves the distinction between password-gated channels and end-to-end (E2E) encrypted channels by exposing separate operations for setting/clearing passwords and for rekeying the wrapped room key.

## Notes
- SetChannelPasswordAsync is not applicable to end-to-end encrypted channels; encrypted rooms change access by rekeying via RekeyChannelAsync so the room key envelope remains consistent. Clearing a password is performed by passing null as the password parameter.
- RekeyChannelAsync is restricted to the channel creator: administrators who do not know the current passphrase cannot rekey a channel on the creator's behalf.
- The system "live log" room is role-gated and its name is reserved even when the feature is disabled; this prevents user-owned channels from accidentally becoming the stream target if the feature is enabled later.
- Channel creation is subject to spam-throttling; moderators and higher roles are exempt from the throttle enforced by SpamGuard.

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


Initializes ChannelService by wiring its required collaborators into private fields for later use. The constructor accepts a scope factory, a presence tracker, a spam guard, a server logs service, and a logger, and stores them for use by the instance. In typical applications, the dependency injection container supplies these services, so ChannelService can create short-lived scopes when needed, track user presence, guard against spam, record server-side events, and emit contextual logs.

## Remarks
By taking dependencies through constructor injection, ChannelService remains loosely coupled and highly testable, since test doubles can be supplied in place of real implementations. This composition root clarifies the service's responsibilities—managing channel state with awareness of presence, applying spam protection, and observability through logs.

## Notes
- If ChannelService is registered as a singleton, ensure that the injected services are thread-safe or have appropriate lifetimes; otherwise adjust registrations to avoid unsafe sharing.
- If the class creates scopes via the IServiceScopeFactory, dispose them promptly to avoid memory leaks or disposed-service access.
- Verify the DI container can resolve all dependencies at startup; a misconfiguration will surface as a runtime resolution failure.

---

### CreateChannelAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<ChannelOperationResult> CreateChannelAsync(
        Guid creatorUserId, string name, string? topic, bool isPublic,
        string? [REDACTED:CONNECTION_STRING_PASSWORD] string? encryptionSalt = null, string? wrappedRoomKey = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `creatorUserId` | `Guid` | — |
| `name` | `string` | — |
| `topic` | `string?` | — |
| `isPublic` | `bool` | — |
| `encryptionSalt` | `string? [REDACTED:CONNECTION_STRING_PASSWORD] string?` | `null` |
| `wrappedRoomKey` | `string?` | `null` |

**Returns:** `Task<ChannelOperationResult>`


Creates a new chat channel using the provided parameters, validating the name, enforcing reserved names, optionally handling a password (hashed) and an end-to-end encryption envelope, and persisting the channel with the creator as a member. Use this when you need to create a channel with consistent validation, security, and membership semantics.

## Remarks
This method centralizes all channel-creation logic, applying business rules such as name normalization (lowercasing and trimming), reserved-name protection for the log room, password requirements for encrypted channels, and spam throttling before persisting data. It leverages a scoped database context to create the channel and automatically adds the creator as a member, ensuring the creator has immediate access. The reserved log room name is enforced regardless of feature toggles, preventing accidental conflicts with system channels.

## Notes
- The channel name is normalized to lowercase and trimmed, which makes channel uniqueness effectively case-insensitive.
- If an end-to-end envelope is supplied (encryptionSalt and wrappedRoomKey), a password must also be provided; otherwise creation fails with a validation error.
- A race on channel name creation is possible in highly concurrent scenarios; the code checks for existence prior to insert and relies on the database to enforce final uniqueness if necessary.


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


Deletes a channel by name for a given caller, enforcing that only the channel creator or an administrator can perform the deletion and that protected/default channels cannot be removed. It normalizes the channel name, validates existence and non-system status, removes the channel from the database, saves changes, and returns a ChannelOperationResult containing a ChannelDto with the channel’s identity and metadata; on failure it maps to a corresponding ChannelError with a descriptive message.

## Remarks
This method encapsulates the channel-deletion policy in a single place, ensuring consistent authorization checks and error signaling across call sites. It delegates data access to EchoHubDbContext via a scoped DI container and returns a ChannelDto representing the deleted channel’s identity and basic attributes, which can be used by clients to refresh UI state or logs.

## Notes
- The ChannelDto is constructed after the channel row is removed and SaveChangesAsync completes, so the returned DTO serves as a confirmation of what was deleted rather than a live snapshot of a remaining entity.


---

### EnsureChannelMembershipAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<(bool Success, string? Error, bool PasswordRequired)> EnsureChannelMembershipAsync(
        Guid userId, string channelName, string? [REDACTED:CONNECTION_STRING_PASSWORD]
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Success` | `bool` | — |
| `Error` | `string?` | — |
| `PasswordRequired` | `bool` | — |


Ensures that a user is granted membership to a named channel, creating or restoring the channel when appropriate, and enforcing access rules including password protection. Call this when a user attempts to join or access a channel so the system can validate eligibility, auto-provision special channels, and persist the membership relationship in the database. The method returns a tuple (Success, Error, PasswordRequired) to indicate whether entry was granted, an error message if any, and whether the caller should prompt for a password.

## Remarks
Centralizes channel-join semantics within ChannelService, encapsulating rules around default channels, system/log channels, and password gates. It coordinates with the database context, server configuration, and validation utilities to decide whether entry should be granted, a channel recreated, or a password prompt issued. By funneling join logic through a single path, it reduces duplication and ensures consistent behavior across different join entry points (TUI, REST, IRC).

## Notes
- Automatic channel provisioning: If the requested channel does not exist, the method may recreate the default channel or the log channel and logs a warning. Callers should not assume a static channel list.
- Password gate: For channels with a PasswordHash, a password is required on first-time joins and validated with BCrypt. The method returns PasswordRequired = true in those cases and updates membership only after successful verification.
- Database scope and side effects: The operation creates a short-lived DI scope to access EchoHubDbContext and persists changes (new ChannelMembership, and possibly a newly created Channel). Callers should be mindful of potential race conditions if multiple concurrent joins occur for the same channel.

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


Ensures the application has a canonical default channel in the EchoHub database by checking the Channels collection for a channel named HubConstants.DefaultChannel and seeding one if it does not exist. This bootstrapping helper is intended to be invoked during initialization to guarantee a general discussion channel is present without duplicating the initialization logic elsewhere.

## Remarks
By centralizing the default-channel bootstrapping in EnsureDefaultChannelAsync, callers avoid duplicating the existence check and channel-creation code across startup paths. It ties together the Channel entity, the HubConstants default channel name, and the database context, so changes to the default channel semantics propagate from this single place. The method is private and static, reinforcing that it is an internal bootstrap concern rather than a reusable operation for callers.

## Notes
- Potential race condition under concurrent invocations: the existence check followed by insertion is not atomic, which could raise a constraint violation if two callers run at the same time.
- CreatedByUserId = Guid.Empty marks system-generated creation; auditing considerations may require handling.

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


Ensures there is a system-owned channel with the specified name by normalizing the name and either creating a new system channel or converting an existing non-system channel into a system channel. It then returns a ChannelDto describing the channel’s identity, topic, visibility, and system status.

## Remarks
Guarantees a canonical system channel identity for internal communications and server content streaming. It encapsulates the create-or-claim logic behind a single API and logs whether a channel was created or claimed. If the target channel already exists and is already marked as system, the method is effectively a no-op and simply returns its ChannelDto.

## Notes
- There is a potential race condition when two concurrent invocations try to create the same system channel; relying on database constraints or proper isolation is recommended to avoid duplicates.
- If a non-system channel exists with the same name, the code will convert it to a system channel by setting IsSystem = true, IsPublic = false, and clearing PasswordHash; CreatedAt remains the original timestamp.
- The channel name is lower-cased and trimmed before the lookup, so callers should not rely on case-sensitive or whitespace-sensitive channel naming.

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


GetChannelByNameAsync fetches a channel by its name after normalizing the input to lowercase and trimming whitespace. It creates a new DI scope to obtain EchoHubDbContext, queries the Channels set for a channel whose Name matches the normalized input, and, if found, counts the number of Messages belonging to that channel. If no matching channel exists, it returns null. The returned ChannelDto includes the channel’s Id, Name, Topic, visibility (IsPublic), the total MessageCount, CreatedAt timestamp, and two boolean flags indicating whether a password hash exists and whether a WrappedRoomKey is present, plus whether the channel is a system channel. This method centralizes the data-shaping of channel metadata for consumers (e.g., channel listings or details) and hides direct EF queries behind a concise API.

## Remarks
This abstraction centralizes channel metadata retrieval for UI and API surfaces, ensuring consistent ChannelDto shaping and hiding data-access details behind a single, strongly-typed API. It also clarifies that a null return indicates a non-existent channel.

## Notes
- Returns null when no channel matches the provided name; callers should handle the nullable result.
- Performs two database queries (FirstOrDefaultAsync for the channel, then CountAsync for its messages) when a channel exists; this is straightforward but has a potential perf cost.
- Relies on input normalization to lowercase; if stored channel names are not stored in a comparable form, the lookup could miss matches.

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


Retrieves the ChannelCryptoDto describing the cryptographic state of a channel. The method normalizes the input channel name to lowercase and trims whitespace, then queries the EchoHubDbContext for a Channel with the matching name. If no channel is found, it returns null. If a channel exists, it returns a ChannelCryptoDto where the first value indicates whether a WrappedRoomKey is present (WrappedRoomKey != null) and includes the channel's EncryptionSalt. Data access occurs within a short-lived DI scope created from _scopeFactory, resolving EchoHubDbContext for the lookup.

---

### GetChannelListAsync
> **File:** `src/EchoHub.Server/Services/ChannelService.cs`  
> **Kind:** method

```csharp
public async Task<List<ChannelListItem>> GetChannelListAsync()
```

**Returns:** `Task<List<ChannelListItem>>`


Fetches and returns a list of channel summaries. The method creates a scoped DI container, reads the EchoHubDbContext, loads all channels ordered by name, and maps each channel to a ChannelListItem that includes the channel's name, topic, the number of online users in that channel (via the presence tracker), whether the channel is public, and whether a password is set. This is typically used to populate a channel directory or lobby UI with up-to-date channel metadata and presence information.

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


Fetches channel-level metadata for a given channel name without returning the messages themselves. It normalizes the input by lowercasing and trimming, resolves the channel via a scoped DI context, and if the channel exists returns a ChannelMetaDto containing the channel's Id, normalized Name, Topic, flags indicating whether a WrappedRoomKey or PasswordHash exists, the total MessageCount, the distinct count of Senders, an estimated storage footprint for the channel (attachments plus text), and the channel's CreatedAt timestamp. If no channel matches the provided name, the method returns null. The operation executes within a scoped DI context to ensure proper disposal of the database context.

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


Fetches a paginated list of channels visible to the specified user, ensuring a default channel exists and applying system-channel visibility rules. It builds a Page of ChannelDto items by filtering channels based on whether they are system channels (only visible if the caller has the appropriate server role) or non-system channels (visible if public or if the user is a member). The method returns a PaginatedResponse containing the channels and the total count, ordered with system channels first and then by name. Per-channel metadata includes the number of messages, creation time, and security flags such as whether a password is set or a wrapped room key is present.

## Remarks
This method centralizes channel discovery and visibility logic used by API surfaces and the UI. By enforcing system-channel visibility through server-side role checks and by materializing concise per-channel data into ChannelDto, callers receive a consistent, paged view of channels while preserving the default channel guarantee. The use of a scoped DbContext and a two-phase query (total count, then page fetch) encapsulates the data-access concerns behind a single, well-defined operation.

## Notes
- EnsureDefaultChannelAsync(db) may create the default channel if it is missing; this side effect occurs on every call. callers should be aware of potential writes on read-like operations.
- The total and page fetch are executed as separate queries; data may change between these calls, affecting the reported total and the returned page.
- The channel's Messages.Count is computed in the projection, yielding a per-channel count without loading full message collections.


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


RekeyChannelAsync rotates the passphrase for an end-to-end encrypted channel by swapping the join-gate hash and the wrapped room key, while the actual content key remains unchanged so the history stays readable. The client then re-wraps the content key under the new passphrase-derived key. This operation is restricted to the channel creator; administrators who do not know the current passphrase cannot perform a rekey.

## Remarks
This method encapsulates a security-sensitive transition that updates credential material without discarding encrypted content. By validating the new passphrase (via ValidateChannelPassword) and requiring non-empty new salt and wrapped key before touching the database, it preserves both confidentiality and integrity. The operation executes in a scoped data context to ensure the channel state is read and persisted atomically, reflecting the latest creator-approved configuration while keeping history intact.

## Notes
- New password, salt, and wrapped key are validated before any changes are persisted; if validation fails, the operation aborts with a ValidationFailed result.
- Rekeying is restricted to the channel creator; the method enforces this by verifying the caller's user ID and the correctness of the current passphrase before applying changes.


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


Sets, changes, or clears (null) a channel's join password. This operation is allowed only for the channel's creator or an administrator. End-to-end encrypted channels do not accept password changes here; such channels must use RekeyChannelAsync to rotate the passphrase, preserving the room key envelope.

## Remarks

By centralizing password management in this method, the system enforces consistent authorization, validation, and persistence rules for channel passphrases. It guards against modifying system channels and avoids altering encryption state for end-to-end encrypted channels at this layer, delegating that concern to RekeyChannelAsync when appropriate. The method returns a ChannelDto describing the updated channel, including whether a password is set and whether the channel remains end-to-end encrypted.

## Notes

- Passwords are stored as BCrypt hashes; if a null password is provided, the password is cleared (PasswordHash becomes null).
- The channel name is normalized to lowercase and trimmed before lookup to ensure stable, case-insensitive matching.
- If the channel does not exist, is a system channel, or the caller lacks sufficient privileges (not the creator or an admin), the operation fails with NotFound, Protected, or Forbidden respectively.
- After a successful change, the returned ChannelDto includes the current message count and flags indicating HasPassword and HasWrappedKey, reflecting the channel's encryption state.

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


Updates the topic of a channel, performing authorization, validation, and persistence in one operation. Given the caller's user ID and the channel name, it normalizes the name, enforces topic length (when provided), ensures only the channel creator can update, persists the topic change, and returns a ChannelOperationResult containing a ChannelDto with the channel's identity, current topic, visibility, message count, creation time, and indicators for password protection and wrapped room key.

## Remarks
This method centralizes the domain logic for updating a channel topic behind a service boundary. It enforces the business rule that only the channel creator may modify the topic, and it uses a scoped DbContext to apply the change, ensuring consistency with the data-access layer. The returned ChannelDto exposes a compact snapshot of the channel, including whether the channel is password-protected and whether a wrapped room key exists, which informs UI decisions without leaking internal state.

## Notes
- Topic can be null to clear the current topic (the code stores topic?.Trim()).
- Channel name normalization is applied so lookups are case-insensitive and consistent.
- The operation yields concrete failure codes (NotFound, Forbidden, ValidationFailed) to guide callers in handling user feedback.

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


Normalizes and validates a channel password. If the input password is null or consists only of whitespace, it is treated as no password (the value is effectively normalized to null) and no error is produced. For non-empty input, the method enforces length constraints defined by ValidationConstants and returns an error message when the password is too short or too long; otherwise, it returns null to indicate a valid password. The password is passed by reference, allowing the caller to observe and adopt the normalized value in place.

## Remarks

Centralizes the channel password policy so all call sites apply the same minimum and maximum length rules and the same interpretation of an empty password. The implementation defers to ValidationConstants for policy values, ensuring changes to password requirements propagate consistently. The use of a ref parameter enables in-place normalization, so the normalized password (or its absence) is visible to the caller without requiring a separate assignment.

## Notes

- The caller must pass a mutable variable by ref; passing a constant or read-only expression will not compile.
- The method returns null when the password is valid (or when treated as no password), or a non-null string containing the user-facing validation message when invalid.

---

## GetChannelKeyEnvelopeAsync
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


Gets the encryption envelope for a given channel by name by querying the Channels table via a scoped EchoHubDbContext; it returns the channel's EncryptionSalt and WrappedRoomKey as a tuple, or (null, null) if the channel cannot be found. This method is intended for scenarios where callers need to access per-channel cryptographic parameters to decrypt or initialize channel data, without surfacing the data-access details to higher layers.

## Remarks
Encapsulates a small, cohesive data-access operation and hides EF Core/DI plumbing from callers. By creating a scoped scope and resolving EchoHubDbContext per call, it avoids leaking a long-lived DbContext into consumer code and makes the envelope retrieval occur in a single boundary. It relies on the Channels table's Name field to identify a channel and returns two optional values, allowing callers to decide how to handle missing encryption data. This placement fits ChannelService as a dedicated place to retrieve channel-related metadata used by encryption/decryption flows.

## Notes
- Caller must handle possible nulls in both EncryptionSalt and WrappedRoomKey; if the channel isn't found, both will be null.
- Since the input channelName is lowercased before querying, ensure channel.Name storage is consistent (lowercase) to guarantee matches; otherwise, the lookup could miss existing channels.

---

## GetChannelTopicAsync
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


Retrieves the topic for a named channel from the EchoHub database. The method normalizes the input (lowercases and trims), opens a scoped DI context to resolve EchoHubDbContext, and queries the Channels set for a channel with the matching name. If no channel is found, it returns (null, false); otherwise it returns the channel's Topic along with true, indicating the channel exists. The operation is asynchronous, allowing callers to await the database query without blocking.

## Remarks
This method encapsulates a small, focused data-access concern: turning a channel name into its topic, while also signaling whether the channel exists. Returning a value tuple (Topic, Exists) makes it straightforward for call sites to branch logic without null checks against the channel entity. The DI-scoped DbContext use ensures clean disposal per call and aligns with standard EF Core usage in a DI-driven application.

## Example
```csharp
var (topic, exists) = await GetChannelTopicAsync("general");
if (exists)
{
    Console.WriteLine(topic);
}
else
{
    Console.WriteLine("Channel not found.");
}
```

## Notes
- The lookup lowercases the channel name; ensure stored channel names are normalized the same way to guarantee matches.
- Topic can still be null even when Exists is true; callers should handle null topics gracefully.
- If multiple channels share the same name (data integrity issue), FirstOrDefaultAsync returns the first match.

---