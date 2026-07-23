# Message

> **File:** `src/EchoHub.Core/Models/Message.cs`  
> **Kind:** class

```csharp
public class Message
```


Represents a single message in a channel, encapsulating the text payload, sender identity, timestamp, and any attachments. It serves as the core record for conversations and is designed to be persisted by the data layer and consumed by the UI to render threads and channel histories. The message may carry rich content via `EmbedJson` and can reference a previous message through `ReplyToMessageId` to model simple threading. The `Content` property is required, yet a message may legitimately have empty content if it carries attachments.

## Remarks
Message is the domain aggregate for a chat entry, linking to its [`Channel`](Channel.cs.md) via `ChannelId`/[`Channel`](Channel.cs.md) and to its sender via `SenderUserId`/`SenderUsername`. Attachments are modeled as a separate collection (`Attachments`), enabling a clean separation between textual payloads and media. Legacy fields (`Type`, `AttachmentUrl`, `AttachmentFileName`, `AttachmentFileSize`) exist solely to support a one-time startup data migration into the new attachments model; new writes should use the `Attachments` collection, and these legacy fields are not exposed in DTOs and are nulled after migration. The `ReplyToMessageId` enables basic threading by pointing to the message this one replies to, if any; downstream logic should gracefully handle references to messages that may have been deleted.

## Example
```csharp
var message = new Message
{
    Id = Guid.NewGuid(),
    Content = "Welcome to the channel!",
    SenderUserId = Guid.NewGuid(),
    SenderUsername = "system",
    ChannelId = Guid.NewGuid(),
    Attachments = new List<Attachment>
    {
        new Attachment
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.Empty,
            Url = "https://example.com/file.png",
            FileName = "file.png",
            FileSize = 4096
        }
    },
    SentAt = DateTimeOffset.UtcNow
};
```

## Notes
- Legacy fields are for migration only; do not rely on them for new code.
- `SentAt` defaults to `DateTimeOffset.UtcNow` on instantiation; override if you have a specific send time.
- Use `EmbedJson` for optional rich content, and handle its absence gracefully in the UI.