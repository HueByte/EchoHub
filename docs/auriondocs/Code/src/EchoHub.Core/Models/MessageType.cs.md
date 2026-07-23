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


Represents the category of a message payload within the model, enabling code to distinguish between textual content, images, files, and audio. Use `MessageType` to drive type-specific logic (rendering, validation, or serialization) by switching on the enum values rather than inspecting the payload directly.

## Remarks
By centralizing the variety of message payloads behind a single discriminator, `MessageType` makes it easier to extend support for new kinds. Renderers, validators, and serializers can rely on this enum to route behavior without peeking into payload internals, promoting cleaner separation of concerns.

## Notes
- When adding a new member to `MessageType`, update all switch expressions that handle the enum to avoid unhandled values at runtime. Prefer exhaustiveness to catch omissions at compile time.
- Do not repurpose existing values; if the meaning changes, introduce a new member to preserve backward compatibility and serialization stability.