# Message

> **File:** `src/EchoHub.Core/Models/Message.cs`  
> **Kind:** class

```csharp
public class Message
```


Message is the persistence model for a chat message in EchoHub, capturing who sent it, when, where, and what was said. Content is required text (which may be empty if the message carries only attachments), with an optional EmbedJson and a list of Attachments for attached files; SenderUserId/SenderUsername identify the author and ChannelId/Channel locate the conversation. Messages may reply to another message via ReplyToMessageId. It also includes legacy pre-attachments fields (Type, AttachmentUrl, AttachmentFileName, AttachmentFileSize) retained to support a one-time startup migration that folds old single-attachment messages into Attachments; new code never writes these and they are nulled after migration and not exposed in DTOs.

## Remarks
Architecturally, Message acts as the persistence model for chat messages, combining the modern Attachments collection with legacy fields retained to support a one-time startup data migration. New code never writes the legacy fields; they are nulled after migration and are not exposed in DTOs.