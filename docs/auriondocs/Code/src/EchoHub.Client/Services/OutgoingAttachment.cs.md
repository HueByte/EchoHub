# OutgoingAttachment

> **File:** `src/EchoHub.Client/Services/OutgoingAttachment.cs`  
> **Kind:** record

```csharp
public sealed record OutgoingAttachment(
    Stream Stream,
    string FileName,
    string? DeclaredKind = null,
    string? EncryptedPreview = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Stream` | `Stream` | — |
| `FileName` | `string` | — |
| `DeclaredKind` | `string?` | `null` |
| `EncryptedPreview` | `string?` | `null` |


OutgoingAttachment is a transport object that represents a single file to upload as part of a message. It bundles the data Stream and FileName, and optionally carries DeclaredKind and EncryptedPreview for encrypted channels, while non-encrypted channels typically set only Stream and FileName.

## Remarks
OutgoingAttachment serves as a compact, immutable data carrier that travels through the sending pipeline. As a record, it uses value-based equality which helps comparisons and deduplication when attachments are tracked across requests. It also clarifies ownership: the record does not manage the lifetime of the underlying Stream; callers are responsible for opening and disposing streams as appropriate.

## Example
```csharp
using System.IO;

// Normal channel usage: only Stream and FileName are provided
var data = new byte[] { 0x01, 0x02, 0x03 };
var stream = new MemoryStream(data);
var attachment = new OutgoingAttachment(stream, "data.bin");

// End-to-end encrypted channel usage: DeclaredKind and EncryptedPreview are set
var ciphertext = new MemoryStream(new byte[] { 0xAA, 0xBB, 0xCC });
var asciiPreview = @"ASCII_ART_PREVIEW";
var encryptedAttachment = new OutgoingAttachment(ciphertext, "image.png", "image", asciiPreview);
```

## Notes
- The lifetime of the underlying Stream is not managed by OutgoingAttachment; the caller must ensure the stream is disposed when appropriate.
- DeclaredKind and EncryptedPreview are intended for encrypted channels; in normal channels these values are typically null.