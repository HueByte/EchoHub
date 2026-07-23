# Attachment

> **File:** `src/EchoHub.Core/Models/Attachment.cs`  
> **Kind:** class

```csharp
public class Attachment
```


Represents a file attached to a message (such as an image, audio, or any file), enabling a message to carry zero or more attachments alongside its text content. The `Attachment` entity associates a downloadable resource with its parent [`Message`](Message.cs.md) via `MessageId` and, optionally, [`Message`](Message.cs.md), while storing the attachment's URL (`Url`), filename (`FileName`), size (`FileSize`), type (`Kind`), and an optional ASCII preview (`AsciiPreview`).

## Remarks

Attachments decouple media from the textual content of a message, allowing the system to manage downloads, permissions, and encryption independently from the message body. The `AsciiPreview` provides a lightweight visual cue for image attachments, and its presence is influenced by how media is encrypted at rest or within channel scopes. The [`AttachmentKind`](AttachmentKind.cs.md) helps callers distinguish among images, audio, and other file types to apply appropriate handling.

## Notes

- The `Url` is a relative download path (for example, `/api/files/{fileId}`); clients should prefix it with the API base URL when constructing a full link.
- The `AsciiPreview` is null for non-image attachments and is stored encrypted at rest in encrypted channels.
- The `FileSize` is the number of bytes stored for the attachment and may reflect ciphertext size when encryption is enabled.