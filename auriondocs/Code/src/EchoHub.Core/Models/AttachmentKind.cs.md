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


AttachmentKind enumerates the possible types of a message attachment and signals how the client should render it. Use this enum when you know the specific attachment kind (image, audio, or file) so the UI can render an ASCII preview, a playback control, or a download option instead of a generic attachment rendering.

## Remarks
This enum centralizes the presentation logic for attachments and serves as a simple discriminator that decouples the attachment data from its rendering. By representing the modality with a single value, components can switch on kind to choose the appropriate UI affordance without inspecting the content payload. It helps maintain a clean separation between the data model (what the attachment is) and the presentation (how it should be shown).

## Example
```csharp
AttachmentKind kind = AttachmentKind.Image;
switch (kind)
{
    case AttachmentKind.Image:
        Console.WriteLine("Render as ASCII image preview");
        break;
    case AttachmentKind.Audio:
        Console.WriteLine("Render with audio controls");
        break;
    case AttachmentKind.File:
        Console.WriteLine("Render as downloadable file");
        break;
}
```

## Notes
- If the enum is extended in the future, ensure all switch expressions include a default/fallback to handle unknown values gracefully.