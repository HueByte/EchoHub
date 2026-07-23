# ChatDtos.cs

> **Source:** `src/EchoHub.Core/DTOs/ChatDtos.cs`

## Contents

- [AttachmentDto](#attachmentdto)
- [ChannelCryptoDto](#channelcryptodto)
- [ChannelDto](#channeldto)
- [ChannelMetaDto](#channelmetadto)
- [CreateChannelRequest](#createchannelrequest)
- [EmbedDto](#embeddto)
- [JoinChannelResult](#joinchannelresult)
- [MessageDto](#messagedto)
- [RekeyChannelRequest](#rekeychannelrequest)
- [ReplyRefDto](#replyrefdto)
- [SendMessageRequest](#sendmessagerequest)
- [SendUrlRequest](#sendurlrequest)
- [UpdateTopicRequest](#updatetopicrequest)
- [UserDto](#userdto)

---

## AttachmentDto
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record AttachmentDto(
    AttachmentKind Kind,
    string Url,
    string FileName,
    long FileSize,
    string? AsciiPreview = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Kind` | [`AttachmentKind`](../Models/AttachmentKind.cs.md) | — |
| `Url` | `string` | — |
| `FileName` | `string` | — |
| `FileSize` | `long` | — |
| `AsciiPreview` | `string?` | `null` |


Represents a file attachment attached to a chat message. It carries the attachment kind, a URL to access the resource, the original file name, the size of the file, and an optional ASCII preview used for color-tag art when available. This DTO is used when composing or processing message payloads that include attachments, or when consuming message data that contains attachment metadata. In end-to-end encrypted channels the content behind the URL and the preview may be ciphertext that the server cannot read.

## Remarks
AttachmentDto serves as a compact, immutable value object that consolidates attachment metadata for transport, storage, and rendering across UI and API boundaries. Being a record provides value-based equality, which simplifies deduplication and caching scenarios, and makes it natural to compare attachments without inspecting the entire payload. It decouples attachment handling from the message body, enabling consistent rendering and processing of attachments regardless of how the message content is structured.

## Example
```csharp
// Example: construct an attachment DTO for a file attachment
var attachment = new AttachmentDto(
    default(AttachmentKind),
    "https://cdn.example.com/files/document.pdf",
    "document.pdf",
    204800,
    null);
```

## Notes
- AsciiPreview is optional; when present, it provides a text-based preview but is not guaranteed to render a full image. Clients should gracefully fall back to the URL or file name if the preview is absent. 
- AttachmentDto is a record, so instances are immutable and compare by value. This supports straightforward caching and deduplication strategies across layers.

---

## ChannelCryptoDto
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record ChannelCryptoDto(bool IsEncrypted, string? EncryptionSalt)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `IsEncrypted` | `bool` | — |
| `EncryptionSalt` | `string?` | — |


ChannelCryptoDto carries the public cryptographic metadata required by a client to derive its join credential from a passphrase. It should be used by clients during the channel join flow to determine if a passphrase-based derivation is necessary and to access the salt used for key derivation, without ever handling the wrapped room key.

## Remarks
This DTO isolates derivation parameters from actual keys, enabling authentication-related components to reason about how a credential is derived without touching or exposing key material. The IsEncrypted flag indicates whether a passphrase-based join is applicable, and EncryptionSalt provides the salt used in the derivation when encryption is in effect. When IsEncrypted is false, EncryptionSalt may be null, reflecting that no passphrase-based derivation is required.

## Notes
- If IsEncrypted is true, EncryptionSalt should be non-null to derive the join credential; when false, the salt may be null.
- This is a simple data transfer object intended to convey derivation parameters safely; never serialize or expose wrapped key material.


---

## ChannelDto
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record ChannelDto(
    Guid Id,
    string Name,
    string? Topic,
    bool IsPublic,
    int MessageCount,
    DateTimeOffset CreatedAt,
    bool IsProtected = false,
    bool IsEncrypted = false,
    bool IsSystem = false)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Id` | `Guid` | — |
| `Name` | `string` | — |
| `Topic` | `string?` | — |
| `IsPublic` | `bool` | — |
| `MessageCount` | `int` | — |
| `CreatedAt` | `DateTimeOffset` | — |
| `IsProtected` | `bool` | `false` |
| `IsEncrypted` | `bool` | `false` |
| `IsSystem` | `bool` | `false` |


ChannelDto is an immutable data transfer object that encapsulates the core metadata of a chat channel. It groups the channel’s unique identifier, display name, an optional topic, visibility, message count, and creation timestamp, together with flags that describe its characteristics (protected, encrypted, and system channels). This object is commonly produced by the server when retrieving or creating channel data and is consumed by clients and services that need a stable snapshot of a channel’s state. As a record, ChannelDto provides value-based equality and supports convenient cloning via with-expressions without mutating the original instance.

## Remarks
ChannelDto serves as a transport-friendly abstraction that decouples channel metadata from domain models. The boolean flags encode common channel semantics: IsPublic indicates whether the channel is publicly discoverable, IsProtected denotes restricted access, IsEncrypted signals encryption usage, and IsSystem marks built-in, system-managed channels. CreatedAt represents the creation-time snapshot and should be treated as immutable; for updates, create a new ChannelDto instance (e.g., with a with-expression) rather than mutating the existing one.

## Example
```csharp
var channel = new ChannelDto(
    Id: Guid.NewGuid(),
    Name: "general",
    Topic: "General discussion",
    IsPublic: true,
    MessageCount: 482,
    CreatedAt: DateTimeOffset.UtcNow,
    IsProtected: false,
    IsEncrypted: true,
    IsSystem: false
);
```

## Notes
- Topic may be null; consumers should handle absence of a topic gracefully.
- ChannelDto is immutable; to derive a modified version use the with expression (e.g., channel with { Name = "new-name" }).
- Boolean flags default to false when omitted, so explicit values should reflect the actual channel semantics.

---

## ChannelMetaDto
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record ChannelMetaDto(
    Guid Id,
    string Name,
    string? Topic,
    bool IsEncrypted,
    bool IsProtected,
    int MessageCount,
    int UniqueUserCount,
    long EstimatedSizeBytes,
    DateTimeOffset CreatedAt)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Id` | `Guid` | — |
| `Name` | `string` | — |
| `Topic` | `string?` | — |
| `IsEncrypted` | `bool` | — |
| `IsProtected` | `bool` | — |
| `MessageCount` | `int` | — |
| `UniqueUserCount` | `int` | — |
| `EstimatedSizeBytes` | `long` | — |
| `CreatedAt` | `DateTimeOffset` | — |


ChannelMetaDto is a data transfer object that captures human-facing metadata for a chat channel as surfaced by the /meta command. It exposes the channel's identity (Id), presentation (Name), optional description (Topic), security properties (IsEncrypted, IsProtected), participation metrics (MessageCount, UniqueUserCount), a best-effort size estimate of content (EstimatedSizeBytes), and the creation timestamp (CreatedAt). For encrypted channels, the server retains counts, timestamps, and blob sizes but cannot read the content itself; EstimatedSizeBytes is the sum of stored attachment blob sizes plus message text length, so it is an estimate rather than an exact on-disk total.

## Remarks
This immutable record serves as a stable, client-facing contract that decouples internal storage from UI rendering. By aggregating these fields, it enables lightweight channel listings and meta views without exposing message content, while still providing enough information to gauge activity and scope.

## Notes
- Topic may be null; clients should handle absence gracefully when rendering.
- EstimatedSizeBytes is an approximation; the value may drift as new messages or attachments are added.

---

## CreateChannelRequest
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record CreateChannelRequest(
    string Name,
    string? Topic = null,
    bool IsPublic = true,
    string? [REDACTED:CONNECTION_STRING_PASSWORD]
    string? EncryptionSalt = null,
    string? WrappedRoomKey = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Name` | `string` | — |
| `Topic` | `string?` | `null` |
| `IsPublic` | `bool` | `true` |
| `EncryptionSalt` | `string? [REDACTED:CONNECTION_STRING_PASSWORD]
    string?` | `null` |
| `WrappedRoomKey` | `string?` | `null` |


Represents the payload for creating a new chat channel. It encapsulates the channel name, an optional topic, a visibility flag, and optional cryptographic data used to secure channel communications. A redacted credentials field stands in for a sensitive connection password and should be supplied securely at runtime rather than stored or logged.

## Remarks
This record is an immutable value object intended to be used as a single payload passed from client to API for channel creation. It coalesces related creation parameters in one place, facilitating validation and transport across layers while remaining independent of any particular persistence or network protocol. The redacted password field highlights a security concern: avoid exposing credentials in logs or UI surfaces; handle it through secure channels only.

## Notes
- Name is required; Topic, IsPublic, EncryptionSalt, WrappedRoomKey are optional with sensible defaults (Topic = null, IsPublic = true, EncryptionSalt = null, WrappedRoomKey = null).
- IsPublic defaults to true; set to false to create a private channel.
- Sensitive fields (the redacted password) must be handled securely; avoid logging or exposing the value in logs or UI.

---

## EmbedDto
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record EmbedDto(
    string? SiteName,
    string? Title,
    string? Description,
    string? ImageAscii,
    string Url,
    string? ThemeColor = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `SiteName` | `string?` | — |
| `Title` | `string?` | — |
| `Description` | `string?` | — |
| `ImageAscii` | `string?` | — |
| `Url` | `string` | — |
| `ThemeColor` | `string?` | `null` |


EmbedDto is a lightweight, immutable data carrier for the metadata needed to render a rich embed in chat messages. As a C# record, it provides value-based equality and convenient construction, making it ideal for transporting embed information across layers without mutating state. It carries optional metadata fields (SiteName, Title, Description, ImageAscii, ThemeColor) and requires a Url that points to the embed resource.

## Remarks
This abstraction centralizes all embed-related data into a single contract, decoupling embedding details from other message payloads. By using a record, it gains structural equality and easy pattern matching, which simplifies testing and usage in render pipelines. The optional ThemeColor guides UI theming, while ImageAscii allows lightweight, ASCII-based previews when a graphical asset is unavailable.

## Example
```csharp
var embed = new EmbedDto(
    SiteName: "Aurora Gallery",
    Title: "Landscape Preview",
    Description: "A sample landscape embed",
    ImageAscii: "[ASCII_ART]",
    Url: "https://example.org/embeds/landscape",
    ThemeColor: "#3366FF");
```

## Notes
- All fields except Url are optional, so a minimal EmbedDto can be created with just the Url.
- Being a record, EmbedDto is immutable and supports with-expressions to create modified copies without changing the original instance.

---

## JoinChannelResult
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record JoinChannelResult(
    bool Success,
    List<MessageDto> History,
    string? Error = null,
    bool PasswordRequired = false,
    string? EncryptionSalt = null,
    string? WrappedRoomKey = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Success` | `bool` | — |
| `History` | `List<MessageDto>` | — |
| `Error` | `string?` | `null` |
| `PasswordRequired` | `bool` | `false` |
| `EncryptionSalt` | `string?` | `null` |
| `WrappedRoomKey` | `string?` | `null` |


JoinChannelResult is a value object that conveys the outcome of attempting to join a chat channel. It exposes whether the operation succeeded, provides the channel's message history for immediate rendering, and carries optional security-related data (password requirement, encryption salt, and wrapped room key) that consumers can act on after the join completes.

## Remarks
JoinChannelResult centralizes all information produced by a join attempt, keeping the caller decoupled from the join logic. By pairing a success flag with the History and optional security fields, it supports both happy-path UI rendering and encrypted or password-protected channels without additional payloads. The inclusion of EncryptionSalt and WrappedRoomKey suggests a workflow where the client may fetch or negotiate encryption material as part of joining, rather than as a separate round-trip.

## Example
```csharp
// Successful join with history
List<MessageDto> history = new List<MessageDto>();
var result = new JoinChannelResult(true, history);

// Join that requires a password and includes encryption material
var secured = new JoinChannelResult(true, history, PasswordRequired: true, EncryptionSalt: \"salt123\", WrappedRoomKey: \"wrappedKey\");
```

## Notes
- Error is typically non-null only when Success is false; use it to surface the failure reason to the user.
- EncryptionSalt and WrappedRoomKey are meaningful only for encrypted or password-protected channels; they may be null in plain channels.
- History should be treated as the initial set of messages to render immediately after a join; it may be empty in failure scenarios or when a channel has no prior messages.

---

## MessageDto
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record MessageDto(
    Guid Id,
    string Content,
    string SenderUsername,
    string? SenderNicknameColor,
    string ChannelName,
    DateTimeOffset SentAt,
    List<AttachmentDto>? Attachments = null,
    List<EmbedDto>? Embeds = null,
    string? SenderDisplayName = null,
    ReplyRefDto? ReplyTo = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Id` | `Guid` | — |
| `Content` | `string` | — |
| `SenderUsername` | `string` | — |
| `SenderNicknameColor` | `string?` | — |
| `ChannelName` | `string` | — |
| `SentAt` | `DateTimeOffset` | — |
| `Attachments` | `List<AttachmentDto>?` | `null` |
| `Embeds` | `List<EmbedDto>?` | `null` |
| `SenderDisplayName` | `string?` | `null` |
| `ReplyTo` | `ReplyRefDto?` | `null` |


MessageDto is an immutable data transfer object that captures the essential details of a chat message as it moves across the EchoHub chat API surface. Implemented as a C# record, it provides value-based equality and straightforward construction for message data, making it ideal for serialization and transport between layers (e.g., API, client, and service boundaries). The object aggregates core message data such as Id, Content, SenderUsername, ChannelName, and SentAt, while also supporting optional enhancements like Attachments and Embeds, a human-friendly SenderDisplayName, and a ReplyTo reference for threaded conversations. This shape keeps message-related concerns contained in a single DTO without leaking domain internals, enabling predictable data contracts for consumers.

## Remarks
This symbol serves as a boundary object that encapsulates a complete chat message payload, including optional media and UI hints. By composing AttachmentDto and EmbedDto, it allows rich messages to travel without forcing callers to depend on internal domain types. The use of a record emphasizes that MessageDto represents a snapshot of message data at a point in time; consumers should treat instances as immutable and, if changes are needed, create new instances. The presence of optional fields (SenderNicknameColor, Attachments, Embeds, SenderDisplayName, ReplyTo) reflects real-world variability in messaging scenarios (e.g., plain text messages, media-enabled messages, or replies).

## Notes
- Attachments and Embeds may be null; downstream code should handle nulls or default to empty collections to avoid null reference errors.
- SenderNicknameColor and SenderDisplayName are optional UI hints and may be absent; consumers should gracefully handle missing values.
- ReplyTo is optional and only populated for messages that are replies to another message; check for null before accessing related data.

---

## RekeyChannelRequest
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record RekeyChannelRequest(
    string OldPassword,
    string NewPassword,
    string NewEncryptionSalt,
    string NewWrappedRoomKey)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `OldPassword` | `string` | — |
| `NewPassword` | `string` | — |
| `NewEncryptionSalt` | `string` | — |
| `NewWrappedRoomKey` | `string` | — |


Passphrase change for an encrypted channel: the client proves knowledge of the old passphrase (old auth key), then supplies the re-wrapped room key under the new one.

This RekeyChannelRequest is a data transfer object used to perform a channel rekey. It carries the old password to prove knowledge of the current key, the new password and its salt, and the re-wrapped room key to be used under the new credentials.

## Remarks
This type serves as a single payload boundary in the channel rekey workflow, encapsulating all data required to authenticate the existing context and establish a new encryption context for the room. Being a record enforces immutability and provides straightforward value-based equality, which simplifies testing and auditing of rekey requests. It acts as a contract between the client and server for the rotation of the room key tied to a new passphrase.

## Example
```csharp
var request = new RekeyChannelRequest(
    OldPassword: "old-passphrase",
    NewPassword: "new-passphrase",
    NewEncryptionSalt: "salt-42",
    NewWrappedRoomKey: "BASE64_WRAPPED_ROOM_KEY"
);
```

## Notes
- Do not log or expose OldPassword, NewPassword, or NewWrappedRoomKey; treat them as highly sensitive and avoid telemetry.
- NewEncryptionSalt should be a cryptographically strong, per-operation salt generated by a secure RNG; do not reuse salts.
- This object represents a single rekey operation and should not be reused for multiple independent requests.

---

## ReplyRefDto
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record ReplyRefDto(
    Guid MessageId,
    string SenderUsername,
    string Content)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `MessageId` | `Guid` | — |
| `SenderUsername` | `string` | — |
| `Content` | `string` | — |


ReplyRefDto is a compact, immutable data transfer object that identifies the message a user is replying to. It carries the target message's ID, the original sender's username, and the Content of that message as transmitted over the network, enabling clients and services to render contextual reply previews and preserve the reply's linkage. Content is treated exactly like ordinary message content on the wire: transport-encrypted, and for end-to-end encrypted rooms it is room ciphertext the client must decrypt (the server truncates only plaintext snippets). If the original message has been deleted, the related MessageDto will be null; the reply reference remains a valid anchor for rendering the reply context.

## Remarks
Represents the reply target in chat threads as a minimal reference, decoupling the UI payload from the full MessageDto. It ensures consistent wire-format handling across plaintext and end-to-end encrypted rooms, while allowing clients to display reply context without requiring the entire original payload.

## Example
```csharp
var reference = new ReplyRefDto(
    MessageId: Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301"),
    SenderUsername: "alice",
    Content: "Hello world"
);
```

## Notes
- Content is the exact on-wire representation of the referenced message; it may be ciphertext in encrypted rooms and should be decrypted by the client when applicable.
- If the original message has been deleted, the MessageDto may be null, but the ReplyRefDto still anchors the reply context for UI rendering; callers should handle potential missing referenced data gracefully.

---

## SendMessageRequest
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record SendMessageRequest(string ChannelName, string Content)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `ChannelName` | `string` | — |
| `Content` | `string` | — |


SendMessageRequest is an immutable data transfer object that encapsulates the information required to send a message to a specific chat channel. It combines the ChannelName and the Content to be delivered so transport or messaging layers can operate on a single payload. As a record, it provides value-based equality and easy cloning with the with-expression, which helps when constructing variations without mutating existing instances.

## Remarks

Acts as a boundary contract between UI/API layers and the messaging service. The record's immutability and structural equality make it reliable for logging, caching, and test assertions. Validation rules or routing decisions should live outside this DTO; this type should not perform domain validation. Its simple two-string shape also makes it friendly to common serialization mechanisms, enabling straightforward transport across boundaries.

## Notes

- No validation is performed by the type itself; ensure ChannelName and Content conform to domain rules before sending.
- The type is immutable; to modify, create a new instance (or use the with-expression) rather than mutating an existing one.
- Suitable for serialization; the plain two-property shape works well with JSON, XML, or other common serializers.

---

## SendUrlRequest
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record SendUrlRequest(string Url)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Url` | `string` | — |


SendUrlRequest is a tiny, immutable URL payload represented as a C# record. It’s intended for scenarios where a URL must be passed across boundaries in a strongly-typed way rather than as a raw string, gaining value-based equality and straightforward deconstruction in the process.

## Remarks
Using a record for this DTO ensures immutability, value-based equality, and built-in deconstruction. This makes SendUrlRequest a natural fit for messaging or API surfaces that expect a dedicated URL payload type instead of raw strings, reducing the chance of accidental mutation and enabling pattern-based handling of the URL payload.

## Example
```csharp
var request = new SendUrlRequest("https://example.com");
```

## Notes
- No validation is performed inside the type; ensure the URL is valid at the call site or in downstream handlers.

---

## UpdateTopicRequest
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record UpdateTopicRequest(string? Topic)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Topic` | `string?` | — |


Represents a request to update the topic of a chat or conversation. This immutable record acts as a lightweight DTO that carries an optional Topic value; use it when issuing an update operation—provide a non-null Topic to set a new topic, or pass null to indicate that the topic should be cleared or left unchanged by the API, depending on server semantics.

## Remarks
This abstraction communicates the intent of updating only the topic field, leveraging a nullable Topic to express optionality. The record nature provides value-based equality and simple construction, and you can create modified copies with the with-expression (e.g., updating the Topic while preserving other fields in a derived request).

## Example
```csharp
// Set a new topic
var request = new UpdateTopicRequest("New Topic");

// Clear the topic (behavior depends on the API)
var clearRequest = new UpdateTopicRequest(null);

// Create a modified copy
var updated = request with { Topic = "Updated Topic" };
```

## Notes
- Topic is nullable; serialization and API behavior may vary—null may mean "no change" or "clear" depending on the endpoint.
- Because this is a record, instances are immutable; use the with-expression to derive variations without mutating the original.

---

## UserDto
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record UserDto(
    Guid Id,
    string Username,
    string? DisplayName,
    string? NicknameColor,
    UserStatus Status,
    DateTimeOffset LastSeenAt)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Id` | `Guid` | — |
| `Username` | `string` | — |
| `DisplayName` | `string?` | — |
| `NicknameColor` | `string?` | — |
| `Status` | [`UserStatus`](../Models/UserStatus.cs.md) | — |
| `LastSeenAt` | `DateTimeOffset` | — |


UserDto is a lightweight, immutable data transfer object that conveys a user's identity and presence-related attributes across boundaries such as API responses or UI bindings. It aggregates the user's unique identifier, login name, optional display name and nickname color, current status, and the last seen timestamp so clients can present a consistent and responsive user summary.

## Remarks
As a record, UserDto benefits from value-based equality and structural immutability, making it easy to compare user summaries and safely pass them around without worrying about accidental mutation. DisplayName and NicknameColor are optional to accommodate scenarios where presentation details are missing. LastSeenAt and Status provide presence information that can drive UI indicators and sorting.

## Notes
- DisplayName and NicknameColor are nullable; null should be treated as absent presentation data.

---