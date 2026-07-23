# Attachment

> **File:** `src/EchoHub.Core/Models/Attachment.cs`  
> **Kind:** class

```csharp
public class Attachment
```


Represents a file attached to a message, such as an image, audio, or document. A message may carry zero or more attachments alongside its text content (Discord-style).

## Remarks
Decouples attachment data from the message to allow independent storage and retrieval while keeping a lightweight reference to the owning message. The Url provides the relative download path (for example, /api/files/{fileId}) and FileName preserves the original filename. FileSize stores the stored blob size in bytes, which corresponds to ciphertext size when database encryption is enabled. AsciiPreview offers a rendered ASCII-art preview for images in color-tag format and is null for non-image attachments; it is stored encrypted-at-rest and, in end-to-end encrypted channels, remains room-encrypted.

## Notes
- AsciiPreview is only populated for image attachments; for other kinds of attachments it is null.
- The Message navigation property may be null if the related Message entity isn't loaded; use MessageId for persistence and rely on Message when the relationship is loaded.