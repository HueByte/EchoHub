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


OutgoingAttachment is a compact, immutable data carrier that bundles the pieces needed to upload a file as part of a message: the content as a `Stream` and the original `FileName`. When using end-to-end encrypted channels, `DeclaredKind` signals the attachment type (image, audio, or file) and `EncryptedPreview` holds the room-encrypted ASCII preview for images; on normal channels, only `Stream` and `FileName` are populated.

## Remarks
As a `record`, `OutgoingAttachment` provides value-based equality, making attachments easy to compare, cache, or deduplicate as they traverse the messaging pipeline. The optional `DeclaredKind` and `EncryptedPreview` fields separate transport payload from encryption/presentation concerns, keeping encoding logic out of the transport object.

## Notes
- If `DeclaredKind` is provided for an encrypted attachment, ensure `EncryptedPreview` is also supplied to avoid inconsistent previews.