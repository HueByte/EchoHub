# Channel

> **File:** `src/EchoHub.Core/Models/Channel.cs`  
> **Kind:** class

```csharp
public class Channel
```


Represents a chat channel (room) within EchoHub's domain model. It stores the channel's identity, metadata for access control, an optional topic, and the collection of messages that belong to the channel, as well as an encryption envelope used for end-to-end security. Use this type to model a distinct conversation space that can be public or restricted, with the possibility of system-managed channels that are auto-created and not user-initiated. The class ties together the channel's identity (Id, Name), its description (Topic), its visibility (IsPublic) and authentication data (PasswordHash), its system-channel semantics (IsSystem), its client-managed encryption data (EncryptionSalt, WrappedRoomKey), creation auditing (CreatedAt, CreatedByUserId), and the message history (Messages).