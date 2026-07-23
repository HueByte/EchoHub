# UploadLimits

> **File:** `src/EchoHub.Server/Config/UploadLimits.cs`  
> **Kind:** class

```csharp
public sealed class UploadLimits
```


UploadLimits provides the admin-configurable ceilings for uploads, bound from the `Uploads` configuration section and converted to bytes for enforcement. Values are expressed in megabytes in configuration and exposed as byte-based properties for the enforcement layer; if the `Uploads` section is absent or partial, defaults mirror [`HubConstants`](../../EchoHub.Core/Constants/HubConstants.cs.md) to preserve historical limits.

The class exposes MB-based properties for each category (MaxFileSizeMB, MaxImageSizeMB, MaxAudioSizeMB, MaxAvatarSizeMB) and a per-message attachment cap (MaxAttachmentsPerMessage). It also exposes computed byte-based counterparts (MaxFileSizeBytes, MaxImageSizeBytes, MaxAudioSizeBytes, MaxAvatarSizeBytes) derived from the MB properties. The per-kind limit is exposed via `MaxForKind(AttachmentKind)`, which returns the corresponding byte limit for images, audio, or the general file size for other kinds. Finally, `MaxRequestBodyBytes` represents the absolute ceiling for a single message request body, calculated as `MaxFileSizeBytes * MaxAttachmentsPerMessage`, ensuring that increased configuration actually scales the request payload footprint.

## Remarks
UploadLimits serves as a focused bridge between configuration and enforcement. By centralizing unit conversion (MB to bytes) and collating per-kind and per-message constraints, it reduces the risk of inconsistent bounds across the upload pipeline and makes it straightforward to adjust limits in one place. The design anticipates future extension to additional attachment kinds without altering enforcement sites, while preserving backward-compatible defaults when the configuration is incomplete.

## Example
```csharp
var limits = new UploadLimits();
long imageBytes = limits.MaxImageSizeBytes;
long imageCapForKind = limits.MaxForKind(AttachmentKind.Image);
```

## Notes
- The `MaxRequestBodyBytes` computation ties the per-attachment cap to the file-size ceiling, so increasing `MaxAttachmentsPerMessage` scales the maximum allowed request body accordingly. 
- All byte-based properties are derived from their MB counterparts, so changes to the configuration flow through to enforcement automatically. 
- If [`AttachmentKind`](../../EchoHub.Core/Models/AttachmentKind.cs.md) includes kinds beyond Image and Audio, those other kinds fall back to the general `MaxFileSizeBytes` in `MaxForKind`.
