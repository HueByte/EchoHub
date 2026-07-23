# IrcMessageFormatter

> **File:** `src/EchoHub.Server.Irc/IrcMessageFormatter.cs`  
> **Kind:** class

```csharp
public static class IrcMessageFormatter
```


IrcMessageFormatter formats a [`MessageDto`](../EchoHub.Core/DTOs/ChatDtos.cs.md) into IRC `PRIVMSG` lines for an EchoHub channel. It splits content into IRC-friendly chunks (up to 400 bytes per line), handles CTCP ACTION content, and prefixes replies with the compact ``> nick: snippet | `` prefix when a reply exists. Attachments are emitted as separate lines with absolute URLs (constructed from the optional `publicBaseUrl`) and labeled by kind (Image, Audio, or File). If present, embeds are appended via the embed formatter. When a reply references encrypted room content, the snippet is shown as `[encrypted]` and non-encrypted snippets are truncated to 80 characters to fit IRC constraints.