# AccountDtos.cs

> **Source:** `src/EchoHub.Core/DTOs/AccountDtos.cs`

## Contents

- [DeleteAccountRequest](#deleteaccountrequest)
- [ExportedAttachmentDto](#exportedattachmentdto)
- [ExportedMessageDto](#exportedmessagedto)
- [UserDataExportDto](#userdataexportdto)

---

## DeleteAccountRequest
> **File:** `src/EchoHub.Core/DTOs/AccountDtos.cs`  
> **Kind:** record

```csharp
public record DeleteAccountRequest(string Password)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Password` | `string` | — |


This record models the password-confirmation payload required when a user initiates destructive self-service account actions (such as deleting their account). It captures the password as a single field to prove the user’s intent before the action is executed.

## Remarks
DeleteAccountRequest encapsulates a sensitive credential within a lightweight boundary object to keep password handling explicit in the delete workflow. By isolating the password in a dedicated payload, the system can perform authentication checks, auditing, and policy enforcement at the appropriate boundary. The record is immutable and minimal (a single Password property), which simplifies model binding and reduces the surface area for accidental data exposure.

## Example
```csharp
// When initiating a delete flow, supply the password for re-confirmation.
var request = new DeleteAccountRequest("P@ssw0rd!");
```

## Notes
- Treat the Password as sensitive; avoid logging or exposing it in responses.
- Use this payload only in the delete flow; ensure that the password validation is performed server-side before performing the destructive action.

---

## ExportedAttachmentDto
> **File:** `src/EchoHub.Core/DTOs/AccountDtos.cs`  
> **Kind:** record

```csharp
public record ExportedAttachmentDto(
    string FileName,
    string Url,
    long FileSize,
    string Kind,
    string ChannelName,
    DateTimeOffset SentAt)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `FileName` | `string` | — |
| `Url` | `string` | — |
| `FileSize` | `long` | — |
| `Kind` | `string` | — |
| `ChannelName` | `string` | — |
| `SentAt` | `DateTimeOffset` | — |


Represents the metadata of an attachment that has been exported from a channel. It groups the file name, a URL to access the file, the file size in bytes, a textual kind descriptor, the originating channel name, and the timestamp when it was sent. Use this DTO when returning or transmitting export results to clients or cross-system boundaries to ensure a stable, serializable shape that is decoupled from internal domain models.

## Remarks
- Being a record, instances are immutable and equality is value-based, making it ideal for transport across layers or for caching export results. It serves as a clean contract between the export process and API or consumer layers.
- It acts as a boundary object, decoupling presentation/API concerns from domain entities while preserving the essential attachment metadata needed by clients (name, access URL, size, kind, origin channel, and timestamp).

## Example
```csharp
var attachment = new ExportedAttachmentDto(
    FileName: "invoice.pdf",
    Url: "https://cdn.example.com/exports/invoice.pdf",
    FileSize: 254000,
    Kind: "document",
    ChannelName: "billing",
    SentAt: DateTimeOffset.UtcNow
);
```

## Notes
- The Kind property is a free-form string; if there is a known finite set of kinds, consider introducing a dedicated enum later to avoid inconsistent values.
- FileSize is a long and should be non-negative; implement validation at boundaries if negative values could be produced by upstream systems.
- Ensure the Url is appropriate for client access (consider expiration, authentication, and CORS as needed) since this DTO surfaces a direct link to the exported attachment.

---

## ExportedMessageDto
> **File:** `src/EchoHub.Core/DTOs/AccountDtos.cs`  
> **Kind:** record

```csharp
public record ExportedMessageDto(
    Guid Id,
    string ChannelName,
    DateTimeOffset SentAt,
    string Content,
    Guid? ReplyToMessageId)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Id` | `Guid` | — |
| `ChannelName` | `string` | — |
| `SentAt` | `DateTimeOffset` | — |
| `Content` | `string` | — |
| `ReplyToMessageId` | `Guid?` | — |


ExportedMessageDto is an immutable data transfer object that captures the essential details of a message exported from a channel: its identity (Id), the channel it came from (ChannelName), when it was sent (SentAt), the message content (Content), and an optional reference to the message it replies to (ReplyToMessageId). It serves as a serialization-friendly payload used by export or archival pipelines, decoupled from the in-memory domain model.

## Remarks
ExportedMessageDto provides a stable contract for export pipelines by decoupling serialized data from the internal domain entities. Being a record, it benefits from value-based equality and immutability, which simplifies de-duplication and testing of exported payloads. The nullable ReplyToMessageId models the optional threading relationship: null means the message has no parent. Use ChannelName and SentAt as lightweight contextual metadata when reconstructing conversations in external systems.

## Example
```csharp
var message = new ExportedMessageDto(
    Id: Guid.NewGuid(),
    ChannelName: "general",
    SentAt: DateTimeOffset.UtcNow,
    Content: "Hello world",
    ReplyToMessageId: null
);
```

## Notes
- The ReplyToMessageId is nullable; null indicates no parent message.
- As a record, equality is based on all properties; two messages with identical data compare as equal.
- If you need to derive a modified copy without mutating the original, use the with-expression (e.g., var updated = message with { Content = "Updated" };).

---

## UserDataExportDto
> **File:** `src/EchoHub.Core/DTOs/AccountDtos.cs`  
> **Kind:** record

```csharp
public record UserDataExportDto(
    DateTimeOffset ExportedAt,
    string ServerName,
    UserProfileDto Profile,
    List<ExportedMessageDto> Messages,
    List<ExportedAttachmentDto> Attachments)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `ExportedAt` | `DateTimeOffset` | — |
| [`ServerName`](../../EchoHub.Server.Irc/IrcCommandHandler.cs.md) | `string` | — |
| `Profile` | [`UserProfileDto`](ProfileDtos.cs.md) | — |
| `Messages` | `List<ExportedMessageDto>` | — |
| `Attachments` | `List<ExportedAttachmentDto>` | — |


Represents a persisted snapshot of a user's data as stored by the server, intended for data export or portability. It consolidates the export timestamp, the server identity, the user's profile, and the exported messages and attachments; in end-to-end encrypted rooms the message contents are ciphertext, since the server never has access to plaintext.

## Remarks

UserDataExportDto is an immutable data transfer object that anchors the export pipeline to the server's stored representation. By pairing profile, messages, and attachments into a single artifact, it simplifies serialization, auditing, and versioning while guarding the boundaries between storage concerns and export logic.

## Notes

- The Messages collection contains ciphertext for end-to-end encrypted rooms; do not decrypt on the server. Decryption and user presentation must happen client-side with proper keys.

---