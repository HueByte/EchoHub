# IrcMessageFormatter

> **File:** `src/EchoHub.Server.Irc/IrcMessageFormatter.cs`  
> **Kind:** class

```csharp
public static partial class IrcMessageFormatter
```


IrcMessageFormatter formats a MessageDto into one or more IRC PRIVMSG lines suitable for posting EchoHub messages to an IRC channel. It handles text, image, file, and audio messages by emitting sender-prefixed PRIVMSG lines, chunking long text content to respect line-length limits, and appending lightweight embed previews or media indicators.

## Remarks
By centralizing the IRC-specific formatting logic, this class keeps higher-level message models decoupled from transport concerns while ensuring a consistent prefix and channel naming scheme. It relies on supporting methods like FormatEmbed and ColorTagsToAnsi to render embeds and inline color tags in a readable, IRC-friendly way.

## Notes
- Long text is chunked into multiple PRIVMSG lines via MaxIrcLineContentBytes; a single Message may become several IRC messages.
- Color formatting codes like {X}, {F:RRGGBB}, and {B:RRGGBB} are translated to ANSI escape sequences; clients that do not support ANSI may render them differently or ignore them.
- Embeds are shown as simple text previews (site/title and optional description); images/files are represented by placeholders and may include a download URL when present.