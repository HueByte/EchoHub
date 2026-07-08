# MessageType

> **File:** `src/EchoHub.Core/Models/MessageType.cs`  
> **Kind:** enum

```csharp
public enum MessageType
{
    Text,
    Image,
    File,
    Audio
}
```


Represents the category of a chat message, identifying what kind of payload it carries (text, image, file, or audio) so consumers can branch logic accordingly. Use this enum instead of ad-hoc booleans or string flags when writing message processing or rendering code.

## Remarks
MessageType serves as a stable contract between the message data model and its processors. It enables centralized routing of handling logic (rendering, validation, storage) by content kind, reducing coupling between payload structure and UI or persistence layers. As the set of supported payloads evolves, the enum provides a controlled extension point that keeps behavior consistent across modules.

## Example
```csharp
public void HandleMessageType(MessageType type)
{
    switch (type)
    {
        case MessageType.Text:
            // render or process text
            break;
        case MessageType.Image:
            // render image
            break;
        case MessageType.File:
            // handle file attachment
            break;
        case MessageType.Audio:
            // play audio
            break;
    }
}
```

## Notes
- Do not rely on the enum's underlying numeric values; add a default/catch-all path when new types are introduced.
