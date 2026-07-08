# Message

> **File:** `src/EchoHub.Core/Models/Message.cs`  
> **Kind:** class

```csharp
public class Message
```


Represents a single message within EchoHub's chat channels. It stores the textual content, a message type (defaulting to MessageType.Text), optional attachments and rich content, when the message was sent, and references to the channel and the sender. This is a lightweight data model used for persistence and transport across application layers; Content and SenderUsername are required, ChannelId identifies the target channel, and Id provides a unique identifier for the message.

## Remarks
This class is a pure data carrier rather than a behavior-rich domain object. Its ChannelId and Channel navigation property indicate an ORM-like relationship that lets callers fetch contextual channel information alongside the message. The separation of attachment fields (AttachmentUrl, FileName, AttachmentFileSize) and the EmbedJson payload provides flexible support for text messages, file transfers, and rich content without constraining the core schema.

## Notes
- Content is required; ensure you set Content before persisting.
- Attachment fields are optional; if you populate attachment data, provide a coherent pair of values (e.g., AttachmentUrl and AttachmentFileName) to represent the asset.
- SentAt defaults to UTC time; if a different timestamp is necessary (e.g., from a client), override it prior to persistence.