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


A file attached to a message is represented by `AttachmentDto`. It carries the attachment's kind ([`AttachmentKind`](../Models/AttachmentKind.cs.md)), a URL to retrieve the content (`Url`), the original file name (`FileName`), and the file size in bytes (`FileSize`). If available, `AsciiPreview` holds color-tag ASCII art for images; in end-to-end encrypted channels the data behind `Url` and the preview is ciphertext the server cannot read.

## Remarks
Because `AttachmentDto` is a record, it provides value-based equality and immutability, making it a stable transport object across layers. It decouples the attachment metadata from the message payload, enabling clients to render previews or retrieve content on demand without embedding binary data in the message. The `AsciiPreview` field offers a lightweight preview for image attachments, while `Url` points to the resource whose handling may be encrypted in transit.

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


ChannelCryptoDto is a small data container that exposes the channel's cryptographic policy: whether encryption is enabled (`IsEncrypted`) and the salt used to derive a join credential from a passphrase (`EncryptionSalt`). Use it when you need to pass this metadata across system boundaries without exposing the wrapped room key.

## Remarks
Consolidating `IsEncrypted` and `EncryptionSalt` into a single value object reduces coupling between channel-joining logic and cryptographic operations. It makes intent explicit at call sites that must decide how to derive credentials from a passphrase. Importantly, the actual wrapped room key remains outside this DTO, preserving the security boundary that keys are only handled by the cryptographic subsystem. The nullable `EncryptionSalt` communicates that a salt is omitted when encryption is disabled.

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


ChannelDto is an immutable data transfer object that carries the essential metadata of a chat channel: `Id`, `Name`, `Topic`, `IsPublic`, `MessageCount`, `CreatedAt`, and the optional flags `IsProtected`, `IsEncrypted`, and `IsSystem`. As a `record`, it provides value-based equality and a straightforward bundle of properties suitable for transport across layers or API boundaries without exposing domain entities. Use it when returning channel summaries, listings, or lightweight channel representations to clients or other services, rather than leaking internal domain models.

## Remarks
ChannelDto exists to decouple transport contracts from domain models; by consolidating channel metadata into a single, serializable shape, it enables stable APIs and easier versioning. The `IsSystem` flag allows distinguishing system channels (like announcements) from user-created ones, while `CreatedAt` helps clients sort or display recency.

## Example
```csharp
var channel = new ChannelDto(
    Guid.NewGuid(),
    "general",
    "General discussion",
    true,
    128,
    DateTimeOffset.UtcNow
);
```

## Notes
- Topic is nullable; consumers should handle `null` before displaying a topic, or provide a fallback.

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


ChannelMetaDto is an immutable data transfer object that presents a concise, human-facing snapshot of a channel's metadata (the `/meta` command) to clients. It exposes the channel's identity (`Id`, `Name`), optional `Topic`, security/status flags (`IsEncrypted`, `IsProtected`), audience metrics (`MessageCount`, `UniqueUserCount`), and an estimated on-disk footprint (`EstimatedSizeBytes`), which is the sum of stored attachment blob sizes plus message text length and thus an estimate rather than an exact total. For encrypted channels the server still knows these figures — counts, timestamps, and stored blob sizes — even though it cannot read the content itself. The `CreatedAt` field records when the channel was created.

## Remarks
ChannelMetaDto serves as a stable, read-only contract between server and clients for channel overviews. As an immutable `record`, it guarantees value-based equality and prevents accidental mutation, which simplifies caching and change detection in UI layers. The metadata it carries—identity, topic, security flags, counts, and size—supports efficient rendering of channel lists and summaries without exposing the channel contents.

## Notes
- The `EstimatedSizeBytes` is an estimate (sum of stored attachment blob sizes and message text length); it is not an exact on-disk size and can drift as content changes.

---

## CreateChannelRequest
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record CreateChannelRequest(
    string Name,
    string? Topic = null,
    bool IsPublic = true,
    string? Password = null,
    string? EncryptionSalt = null,
    string? WrappedRoomKey = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Name` | `string` | — |
| `Topic` | `string?` | `null` |
| `IsPublic` | `bool` | `true` |
| `Password` | `string?` | `null` |
| `EncryptionSalt` | `string?` | `null` |
| `WrappedRoomKey` | `string?` | `null` |


The `CreateChannelRequest` is an immutable data transfer object that encapsulates all parameters needed to create a new chat channel. It requires a `Name` and exposes optional settings including `Topic`, whether the channel is public via `IsPublic` (default true), and optional security fields such as `Password`, `EncryptionSalt`, and `WrappedRoomKey` used for encrypted channel setup. Use this record when issuing a channel creation operation so that all related options are passed as a single, strongly-typed payload rather than a loose collection of parameters.

## Remarks
By collecting channel creation options into a single `CreateChannelRequest`, the boundary between API inputs and domain logic is cleanly expressed. The defaults on `IsPublic` and the optional nature of the other fields enable flexible requests while preserving a stable, serializable contract across process boundaries. This abstraction also makes future extension safer: new optional settings can be added without altering existing call sites.

## Notes
- Do not log sensitive fields: avoid writing `Password`, `EncryptionSalt`, or `WrappedRoomKey` to logs or telemetry.
- Nullable fields imply validation; ensure meaningful values before persisting or acting on them.
- If `IsPublic` is false, consider validating that a `Password` is provided for access control; enforce this at the API or domain layer if required.

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


EmbedDto is an immutable data container used to carry the metadata needed to render a rich embed, such as in chat messages or UI panels. It groups the surface data for an embed: `SiteName`, `Title`, `Description`, `ImageAscii`, `Url`, and an optional `ThemeColor`, so callers can supply a complete embed definition in a single object.

## Remarks
As a `record`, `EmbedDto` provides value-based equality and supports deconstruction, making it straightforward to compare embeddings or pattern-match in rendering logic. It serves as a clean boundary between data authors and renderers: producers populate an `EmbedDto`, consumers render an embed from its fields without needing to understand surrounding domain.

## Example
```csharp
var embed = new EmbedDto(
    SiteName: "EchoHub",
    Title: "Welcome",
    Description: "A friendly hello from EchoHub.",
    ImageAscii: "  ___  \n (o o) \n  \_/ ",
    Url: "https://echohub.example",
    ThemeColor: "#4B8BBE"
);
```

## Notes
- `ThemeColor` is optional; omit it to use a default theming. 
- `Url` is required; ensure it is a valid URL to enable link previews. 
- Because `EmbedDto` is a `record`, two instances with identical field values compare equal.


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


Represents the outcome of a join-channel operation as a `JoinChannelResult` type. It exposes a `bool` `Success` flag, a `List<MessageDto>` `History` of messages retrieved for the channel, and optional metadata including a `string?` `Error`, a `bool` `PasswordRequired`, and optional encryption data (`string?` `EncryptionSalt`, `string?` `WrappedRoomKey`).

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


Represents a chat message as a data contract used by the chat API. It captures the message `Id`, the textual `Content`, and author info (`SenderUsername`, optional `SenderNicknameColor`, optional `SenderDisplayName`), the `ChannelName`, and the `SentAt` timestamp. Optional `Attachments` and `Embeds` support rich content, while `ReplyTo` references a prior message.

## Remarks
This DTO is designed as a transport-friendly aggregation of message data, suitable for serialization across clients and services. By referencing the dedicated `AttachmentDto` and `EmbedDto` types, it remains extensible for rich content, and its optional fields (`Attachments`, `Embeds`, `ReplyTo`, `SenderNicknameColor`, `SenderDisplayName`) allow the same shape to cover both simple and feature-rich messages.

## Notes
- `Attachments` and `Embeds` may be `null`; treat them as empty sequences when rendering or iterating.

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


The `RekeyChannelRequest` record represents the data the client sends to request a rekey of an encrypted channel. It conveys knowledge of the current passphrase (via `OldPassword`) and the new credentials and wrapped key to apply (via `NewPassword`, `NewEncryptionSalt`, and `NewWrappedRoomKey`).

## Remarks
This DTO enables the server to verify the client's possession of the existing auth key while atomically applying new encryption material in a single operation. It decouples the client's input from the rekeying logic, allowing validation, auditing, and rollback policies to be applied at the server boundary.

## Notes
- Do not log `OldPassword` or `NewPassword`; treat these values as ephemeral and ensure transport-layer secrecy.

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


Represents a reference to the message that a reply targets. It carries the target message's identifier (`MessageId`), the original sender's username (`SenderUsername`), and the reply content (`Content`), which is treated exactly like message content on the wire: transport-encrypted, and for end-to-end encrypted rooms it is room ciphertext the client must decrypt (the server truncates only plaintext snippets). Null on a `MessageDto` when the original message no longer exists.

## Remarks
ReplyRefDto acts as a compact pointer that preserves the link between a reply and its target message without duplicating payloads. It separates transport- and encryption-aware handling from display logic, enabling clients to decrypt or render the referenced content while the server retains plaintext-only signals. In threaded chat UX, this symbol supports rendering reply previews and context for the target message.

## Notes
- Be aware that `Content` might be ciphertext in encrypted rooms and may not be human-readable until decrypted; do not display it as plaintext without decryption.

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


SendMessageRequest is a simple, immutable data carrier (record) that encapsulates the channel to which a message should be sent and the message content itself. Use this `SendMessageRequest` when you need to issue a message to a specific chat channel, providing both the `ChannelName` and the `Content` in a single object rather than passing multiple parameters or ad-hoc structures.

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


SendUrlRequest is a minimal value object used to convey a URL as a request payload. As a `record` with a single `string Url` positional parameter, it provides value-based equality and immutability, making it ideal for passing URL data through layers or across API boundaries instead of threading raw `string` values.

## Remarks
`SendUrlRequest` serves as a precise contract for operations that require a URL. Its `record` semantics ensure structural equality and allow easy deconstruction; by encapsulating the `Url` property, it clarifies intent and supports serialization as a simple payload.

## Example
```csharp
var req = new SendUrlRequest("https://example.com");
```

## Notes
- No URL validation is performed by this type; validate the URL in the caller or service layer before processing.

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


Represents a request payload to update a topic, encapsulating an optional `Topic` value. As a positional-record, it provides an immutable, lightweight data carrier that callers populate with the new topic string when issuing an update to a chat's topic.

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


`UserDto` is an immutable data transfer object that carries a concise snapshot of a user for chat workflows. It exposes the user’s `Id` (`Guid`), `Username`, optional `DisplayName` and `NicknameColor`, the current `Status` ([`UserStatus`](../Models/UserStatus.cs.md)), and the `LastSeenAt` timestamp (`DateTimeOffset`). Use this DTO when returning or transferring lightweight user data across API boundaries or UI layers instead of exposing full domain entities.

## Remarks
Being a `record` with positional parameters, `UserDto` benefits from value-based equality and convenient deconstruction, which is helpful for tests and payload comparisons. The nullable fields `DisplayName` and `NicknameColor` reflect optional user profile data; readers should handle the possibility of missing values gracefully.

## Notes
- Nullable fields require null checks during consumption.
- Being immutable, modifying a `UserDto` requires creating a new instance (e.g., via a `with` expression).
- The `LastSeenAt` is a `DateTimeOffset`; ensure consistent time zone handling across systems.

---