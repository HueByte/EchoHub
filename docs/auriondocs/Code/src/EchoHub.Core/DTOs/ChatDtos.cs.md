# ChatDtos.cs

> **Source:** `src/EchoHub.Core/DTOs/ChatDtos.cs`

## Contents

- [ChannelDto](#channeldto)
- [CreateChannelRequest](#createchannelrequest)
- [EmbedDto](#embeddto)
- [JoinChannelResult](#joinchannelresult)
- [MessageDto](#messagedto)
- [SendMessageRequest](#sendmessagerequest)
- [SendUrlRequest](#sendurlrequest)
- [UpdateTopicRequest](#updatetopicrequest)
- [UserDto](#userdto)

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
    DateTimeOffset CreatedAt)
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


ChannelDto is a lightweight, immutable data transfer object that encapsulates the essential metadata of a chat channel. It is designed for scenarios where channel information must be surfaced to clients or across layers without exposing domain behavior.

## Remarks
 ChannelDto uses a record to provide value-based equality and straightforward immutability, which makes it ideal for transport across boundaries and for comparing channel snapshots. The Topic property is nullable to reflect channels that have no explicit topic, and CreatedAt anchors the instance to its creation time, aiding display, sorting, and auditing. As a DTO, it should be treated as a pure data carrier without business rules.

## Notes
- Topic is nullable; callers should guard against null when rendering UI or serializing.
- CreatedAt uses DateTimeOffset to preserve the original offset/timezone.
- This type contains no behavior; mapping to/from domain models should be explicit to avoid leakage of business logic.

---

## CreateChannelRequest
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record CreateChannelRequest(string Name, string? Topic = null, bool IsPublic = true)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Name` | `string` | — |
| `Topic` | `string?` | `null` |
| `IsPublic` | `bool` | `true` |


CreateChannelRequest is a compact data transfer object used to carry the parameters required to create a chat channel. Use this record when issuing a channel-creation operation: provide the channel Name, an optional Topic, and whether the channel should be public via IsPublic. The immutability and value-based equality of records ensure the input is captured as a single, coherent object that can be compared, cached, or routed with confidence.

## Remarks
Because it is a C# record with a primary constructor, CreateChannelRequest provides value-based equality and concise construction. The Topic being nullable communicates optionality, and the default IsPublic = true expresses a bias toward public channels unless explicitly overridden. This type acts as a boundary object between input and the creation service, reducing repetition and making intent explicit among collaborators.

## Notes
- This type is immutable; to modify values, create a new instance (e.g., using `with`).
- Name must be provided; there is no internal validation against empty or whitespace values in this type.
- A null Topic means no topic was supplied; downstream logic should handle its absence.

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


EmbedDto is a lightweight, immutable data carrier (a C# record) that encapsulates all the information needed to render a chat embed. It collects optional metadata (SiteName, Title, Description, ImageAscii) together with a required Url and an optional ThemeColor, so callers can pass a single object through the layers responsible for constructing or serializing embeds.

## Remarks
This abstraction centralizes embed-related data so that rendering and serialization logic can rely on a consistent shape. The required Url enforces a destination for the embed, while the remaining fields are optional to accommodate a range of presentation styles without forcing content.

## Notes
- ThemeColor is optional; the type does not validate color format, so validate or sanitize values before use if you rely on specific color formats.
- ImageAscii is intended for simple ASCII art representations; for richer imagery, prefer using the Image source via Url or a dedicated asset field.
- As a record, EmbedDto is immutable. To create a modified variant, use a with-expression (e.g., embed with { Title = "New Title" }).

---

## JoinChannelResult
> **File:** `src/EchoHub.Core/DTOs/ChatDtos.cs`  
> **Kind:** record

```csharp
public record JoinChannelResult(bool Success, List<MessageDto> History, string? Error = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Success` | `bool` | — |
| `History` | `List<MessageDto>` | — |
| `Error` | `string?` | `null` |


## Source Code
This record represents the outcome of an attempt to join a chat channel, carrying whether the operation succeeded, the initial message history for the channel, and an optional error message describing any failure.

## Remarks
Joining a channel can fail while still providing useful context. This type packages the operation's success state with the retrieved message history and an optional error message, enabling callers to render history when available and surface diagnostics when not. By exposing a deconstructible, value-based result, it supports concise pattern matching and straightforward consumption across layers without resorting to exceptions for control flow.

## Example
```csharp
// Successful join with no initial history
var result = new JoinChannelResult(true, new List<MessageDto>());

// Failed join with an error message
var errorResult = new JoinChannelResult(false, new List<MessageDto>(), "Channel not found");
```

## Notes
- The History property uses `List<MessageDto>`, which is mutable. If you require immutability, consider copying the list or wrapping it in a read-only interface when exposing it externally.
- The Error property is nullable; when Success is true, Error is commonly null, but callers should not assume it is always absent.

## Dependencies
- MessageDto

## Dependency APIs
- MessageDto — record MessageDto (src/EchoHub.Core/DTOs/ChatDtos.cs)

## Symbol To Document
- Name: `JoinChannelResult`
- Kind: `record`
- File: `src/EchoHub.Core/DTOs/ChatDtos.cs`
- Language: `csharp`
- ID: `d0d79bf8-eb51-4f5f-89b9-3b0b53cd08c1`

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
    MessageType Type,
    string? AttachmentUrl,
    string? AttachmentFileName,
    DateTimeOffset SentAt,
    long? AttachmentFileSize = null,
    List<EmbedDto>? Embeds = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Id` | `Guid` | — |
| `Content` | `string` | — |
| `SenderUsername` | `string` | — |
| `SenderNicknameColor` | `string?` | — |
| `ChannelName` | `string` | — |
| `Type` | [`MessageType`](../Models/MessageType.cs.md) | — |
| `AttachmentUrl` | `string?` | — |
| `AttachmentFileName` | `string?` | — |
| `SentAt` | `DateTimeOffset` | — |
| `AttachmentFileSize` | `long?` | `null` |
| `Embeds` | `List<EmbedDto>?` | `null` |


MessageDto is a transport-friendly representation of a chat message used across API boundaries. It captures the essential data of a message—its unique identifier, textual content, sender details, channel, type, timestamp, and optional attachments or rich content—so clients and services can exchange messages without depending on internal domain models.

## Remarks

As a C# record, MessageDto is immutable and value-based, meaning it represents a stable data snapshot suitable for transport and caching across layers. It references EmbedDto for optional rich content, keeping the API surface decoupled from the internal embed implementation. This DTO serves as the contract for message payloads in API responses and message pipelines, ensuring a consistent shape regardless of how messages are produced or consumed within the system.

## Example

```csharp
var message = new MessageDto(
    Id: Guid.NewGuid(),
    Content: "Hello world",
    SenderUsername: "alice",
    SenderNicknameColor: "#FFAA00",
    ChannelName: "general",
    Type: MessageType.Text,
    AttachmentUrl: null,
    AttachmentFileName: null,
    SentAt: DateTimeOffset.UtcNow,
    AttachmentFileSize: null,
    Embeds: null
);
```

## Notes

- MessageDto is a record; it is immutable. To create a modified version, use a with-expression (e.g., message with { Content = "new" }).
- Nullable fields (SenderNicknameColor, AttachmentUrl, AttachmentFileName, AttachmentFileSize, Embeds) may be omitted or null; callers should handle nulls gracefully.
- If Embeds is non-null, each EmbedDto included must conform to its own contract and serialization expectations.

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


SendMessageRequest is a minimal data transfer object that carries the destination channel name and the message content for a send operation. Use it when you want to package the channel and message into a single, serializable payload rather than passing separate values around.

## Remarks
By being defined as a record, it gains value-based equality and a concise syntax for a data carrier. This makes SendMessageRequest ideal as a transport contract between layers (e.g., API controllers and chat services) because two instances with the same ChannelName and Content compare as equal. The type couples the channel identifier and the message in a single immutable value, which helps ensure the sender always supplies both pieces of data together. If you later need to enrich the payload, you can extend this record without changing its usage site.

## Example
```csharp
var request = new SendMessageRequest("general", "Hello, world!");
```

## Notes
- The constructor is positional; ChannelName must be supplied before Content, and you cannot omit either value when constructing the record.
- The DTO is designed for transport; business validation (e.g., non-empty content or valid channel) should occur at the call site or within the receiving service.

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


SendUrlRequest is an immutable data carrier used to convey a URL to a forwarding or processing operation within the EchoHub.Core domain. Implemented as a C# record with a single Url property, it benefits from value-based equality, concise construction, and built-in deconstruction semantics, making it ideal for endpoints or services that expect a strongly-typed request object rather than a raw string. Use SendUrlRequest when you need to pass a URL through a clearly defined contract (for example, to a service that processes or forwards URLs) rather than transmitting plain strings.

## Remarks
By introducing a dedicated type for the URL-sending operation, callers gain a stable contract that can evolve with additional fields (e.g., metadata, authentication) without changing call sites. The record ensures the payload remains immutable once created, simplifying reasoning across asynchronous boundaries and across threads.

## Example
```csharp
var request = new SendUrlRequest("https://example.com/resource");
```

## Notes
- This DTO does not perform URL validation; validate or canonicalize the URL at the boundary before constructing SendUrlRequest (e.g., using Uri.TryCreate or a separate validator).


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


A minimal, immutable data transfer object used to request updating a chat's topic. It contains a single nullable string property Topic. By making Topic nullable, callers can supply a new topic or explicitly indicate the absence of a topic update, depending on how the request is consumed by the server. The record’s immutable nature and value-based equality make it convenient to use as a payload in transport scenarios and tests.

## Remarks
Topic being nullable is a deliberate design choice: it communicates that the presence or absence of a value conveys the caller's intent. This DTO is intended to sit alongside other chat-related DTOs in EchoHub.Core and can be extended with additional optional fields without breaking existing consumers, since records provide stable, additive changes.

## Example
```csharp
// Update topic to a specific value
var request = new UpdateTopicRequest("Project Kickoff");
```

```csharp
// No topic value provided (downstream handler decides)
var requestNone = new UpdateTopicRequest(null);
```

## Notes
- Topic is immutable; you cannot modify it after construction.
- Null handling is up to the receiver; ensure alignment with serialization defaults.

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


Represents a lightweight, transport-oriented view of a user in chat scenarios. It bundles the essential user data that clients need to render user lists, reflect presence, and display recent activity, without exposing internal domain details. As a C# record with positional parameters, it is immutable and provides value-based equality, which makes mapping from server-side models to a stable API contract straightforward.

## Remarks
To decouple API contracts from the domain model and provide a stable, minimal representation of a user for chat APIs, this DTO centralizes the client-visible data (Id, Username, optional DisplayName, optional NicknameColor, Status, LastSeenAt). Its record nature affords value-based equality and convenient deconstruction, aiding testing, mapping, and across-layer data transfer.

## Notes
- DisplayName and NicknameColor can be null; handle nulls gracefully in UI rendering.
- UserDto is immutable; updates imply creating a new instance rather than mutating an existing one.
- LastSeenAt uses DateTimeOffset to preserve the original offset; ensure serialization/deserialization preserves the offset to maintain correct interpretation across systems.

---