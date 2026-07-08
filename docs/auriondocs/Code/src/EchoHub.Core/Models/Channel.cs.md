# Channel

> **File:** `src/EchoHub.Core/Models/Channel.cs`  
> **Kind:** class

```csharp
public class Channel
```


Channel models a named conversation space in EchoHub. Use it whenever you need to group messages into a distinct channel, track its creation metadata, and control its visibility. It owns a collection of Message entities via the Messages property, representing the one-to-many relationship between a channel and its messages.

## Remarks
Channel serves as the aggregate root for a conversation in EchoHub. It encapsulates identity, metadata, and the related messages, enabling loading a channel together with its history. The required Name, along with default IsPublic and CreatedAt initializers, provides a predictable creation surface while preserving explicit naming. The Messages collection models the link to Message and supports navigation from Channel to its message history.

## Notes
- CreatedAt is initialized to DateTimeOffset.UtcNow at construction; deserialization may overwrite this value unless the serializer preserves existing values.
- Messages is initialized to an empty list by default; some serializers may set it to null, so reinitialization or proper configuration may be necessary after deserialization.
- The Name property is required; attempting to instantiate Channel without setting Name will fail at compile-time due to the 'required' modifier.