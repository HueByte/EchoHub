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


Represents the category of a message in EchoHub. MessageType defines the four concrete payload kinds that a message can carry: Text, Image, File, or Audio. Use this enum whenever a component, data model, or API needs to convey which kind of content is attached to a message so consumers can handle, display, or validate it in a type-safe way instead of relying on strings or magic numbers.

## Remarks
Centralizes classification: this enum provides a single source of truth for message content kinds, enabling consistent routing, rendering, and validation across the system. It helps collaborators—models, serializers, and UI layers—make decisions based on content type without duplicating logic for string constants. By using an enum, you get compile-time checks and clearer intent.

## Notes
- When stored or transferred, the underlying value defaults to int (0-3) in the order shown; changing the sequence or renaming members may break persisted data.
- If external systems expect string representations, consider mapping to/from MessageType names to avoid breaking compatibility.