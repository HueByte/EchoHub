# AttachmentKind

> **File:** `src/EchoHub.Core/Models/AttachmentKind.cs`  
> **Kind:** enum

```csharp
public enum AttachmentKind
{
    Image,
    Audio,
    File
}
```


AttachmentKind is an enum that encodes how a message attachment should be rendered in the client. It enables rendering logic to pick the appropriate UI: for `Image` attachments, an ASCII image preview is shown; for `Audio`, a play control is exposed; and for `File`, a download line is presented. Use this enum when you need to branch rendering behavior based on the attachment's kind, instead of scattering rendering decisions across the codebase.

## Remarks
This enum centralizes how attachments are presented, decoupling the attachment data from UI rendering code. It helps the rendering layer evolve independently (e.g., swapping ASCII previews or adding new affordances) without changing attachment structures.