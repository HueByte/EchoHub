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


Represents a request payload that carries the user's `Password` to re-confirm destructive self-service actions on the account. This separate `DeleteAccountRequest` DTO isolates credential input from other account data and is intended for use in flows that require explicit user re-authentication before irreversible operations (e.g., account deletion).

## Remarks
Isolates sensitive credential input into a minimal, purpose-built payload, enabling focused validation and auditing of destructive actions. It complements authentication state by forcing an explicit password re-entry rather than relying on session state alone, which helps mitigate accidental or unauthorized deletions. This pattern supports clearer separation of concerns between domain models and security-critical request data.

## Notes
- Do not log or persist the `Password` value in plaintext; keep it transient and ensure redaction in any logs.
- Ensure transport security (`TLS`) when transmitting this payload; avoid storing passwords in memory longer than needed; clear the value after usage if possible.

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


Represents the metadata of an attachment that has been exported, carrying the essential details needed to access and display it—`FileName`, `Url`, `FileSize`, `Kind`, `ChannelName`, and `SentAt`. It serves as a transport contract between the export logic and clients or downstream services rather than exposing internal domain entities.

## Remarks

`ExportedAttachmentDto` acts as a boundary-crossing contract: it decouples the external payload from the internal attachment representation and exposes only the data consumers require. The inclusion of a `Url` implies a downloadable resource that may be protected or time-limited, so callers should treat access as potentially ephemeral and handle expiration appropriately. Because this is a `record`, instances are immutable by default, which helps preserve the integrity of the export snapshot across layers.

## Notes

- The `Url` is often a signed or temporary link; do not assume long-lived access and design clients to handle expiration (e.g., 404 or 403 responses).
- This DTO is strictly a data carrier; avoid embedding business logic in the payload and prefer mapping from domain models to this shape when exporting data.

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


ExportedMessageDto is an immutable data transfer object (record) that captures the essential data of a single exported message: the message `Id`, the `ChannelName` it was sent in, the `SentAt` timestamp, the `Content`, and an optional `ReplyToMessageId` if the message is a reply. It provides a stable, serializable contract for exporting messages to external systems or archives, decoupled from domain behavior so consumers can rely on a consistent shape without depending on domain entities.

## Remarks
As a `record`, `ExportedMessageDto` provides value-like semantics and a predictable equality contract, which is helpful when comparing exported records or caching results during export pipelines. It also separates export concerns from the rest of the domain, making it easier to evolve the internal models without breaking external consumers.

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


`UserDataExportDto` is a `record` that represents a complete snapshot of the server's stored data for a given user, produced when exporting user data for portability or archival. It contains the export timestamp (`ExportedAt`), the originating server name ([`ServerName`](../../EchoHub.Server.Irc/IrcCommandHandler.cs.md)), the user's profile (`Profile`), and the exported content items: messages (`Messages`) and attachments (`Attachments`). In end-to-end encrypted rooms, the message payload is preserved as ciphertext, since the server cannot provide plaintext it never possessed.

## Remarks
This DTO acts as the stable envelope for user data exports, keeping metadata, profile, and content items together for portability and archival use. It decouples export semantics from how data is stored, permitting changes to storage without breaking export contracts. Note that for end-to-end encrypted rooms, the `Messages` are ciphertext as stored; no plaintext is accessible to the server.

## Notes
- Large exports can be memory-intensive; plan for streaming or chunked delivery in exporters.

---