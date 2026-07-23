# IrcMessageFormatter

> **File:** `src/EchoHub.Server.Irc/IrcMessageFormatter.cs`  
> **Kind:** class

```csharp
public static class IrcMessageFormatter
```


IrcMessageFormatter is a static helper that formats a `MessageDto` into one or more IRC PRIVMSG lines for posting to an IRC channel. It orchestrates the translation of message content, reply references, attachments, and embeds into IRC-compatible payloads, applying line-length constraints and CTCP ACTION handling where appropriate. Attachments are rendered as distinct link lines with an appropriate tag (image, audio, or file) and converted to absolute URLs using an optional `publicBaseUrl`. When embeds are present, they are appended via the embed formatting pipeline. The formatting rules are centralized in this class to ensure consistent IRC output across messages and channels.

## Remarks
By centralizing IRC-specific formatting in `IrcMessageFormatter`, the server ensures consistent transport behavior for all messages moved from the domain model to IRC clients. It isolates concerns about line-length, reply quoting, action formatting, and attachment rendering from higher-level message construction, making it straightforward to adjust how content appears in IRC without changing business logic. The implementation supports plain content as well as CTCP ACTIONs and gracefully handles reply contexts, including a placeholder for encrypted room content when applicable.

## Notes
- If `publicBaseUrl` is not provided and attachments use relative URLs, the resulting links may be non-functional in IRC clients. Ensure a base URL is supplied when needed.
