# Channel

> **File:** `src/EchoHub.Core/Models/Channel.cs`  
> **Kind:** class

```csharp
public class Channel
```


Channel models a chat channel within EchoHub's chat surface. It exposes an identifier `Id` (`Guid`), a required `Name` (`string`), an optional `Topic` (`string?`), and a flag `IsPublic` (`bool`) that defaults to `true`. The model also supports server-managed channels via `IsSystem` (`bool`), which are auto-created and read-only for all roles; users cannot create them. When a channel is password-protected, `PasswordHash` (`string?`) stores the hashed password. For end-to-end encryption, the envelope is represented by `EncryptionSalt` (`string?`) and `WrappedRoomKey` (`string?`), both client-generated so that the server never has access to the room content. Creation metadata is captured by `CreatedAt` (`DateTimeOffset`) and `CreatedByUserId` (`Guid`). The `Messages` collection (`List&lt;Message&gt;`) contains the related [`Message`](Message.cs.md) entities that belong to this channel.