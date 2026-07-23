# UploadLimits

> **File:** `src/EchoHub.Server/Config/UploadLimits.cs`  
> **Kind:** class

```csharp
public sealed class UploadLimits
```


UploadLimits is a configuration-bound value object that centralizes the admin-defined upload size caps. It reads sizes in megabytes from the Uploads configuration and exposes corresponding byte-sized properties used during enforcement. When the Uploads section is missing or incomplete, the defaults mirror HubConstants to preserve the historical built-in limits.

## Remarks
UploadLimits centralizes the policy governing uploads (files, images, audio, avatars) and the maximum number of attachments per message. The MB-based properties feed their byte-sized counterparts (MaxFileSizeBytes, MaxImageSizeBytes, etc.) for enforcement. MaxForKind provides a per-kind ceiling, while MaxRequestBodyBytes computes the overall request-body cap (largest file size multiplied by the attachment limit) to ensure configuration changes actually take effect at the HTTP boundary.

## Example
```csharp
var limits = new UploadLimits
{
    MaxFileSizeMB = 64,
    MaxAttachmentsPerMessage = 4
};

long maxImageBytes = limits.MaxImageSizeBytes;
long imageCeiling = limits.MaxForKind(AttachmentKind.Image);
long requestBody = limits.MaxRequestBodyBytes;
```

## Notes
- Changing MaxAttachmentsPerMessage scales the MaxRequestBodyBytes non-linearly; the request-body cap will constrain multipart uploads even if per-file size increases.
- Defaults are tied to HubConstants; if those constants change, the default limits change too unless overridden in the Uploads configuration.