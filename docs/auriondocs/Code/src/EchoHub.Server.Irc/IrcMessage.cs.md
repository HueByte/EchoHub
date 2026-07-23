# IrcMessage

> **File:** `src/EchoHub.Server.Irc/IrcMessage.cs`  
> **Kind:** class

```csharp
public sealed class IrcMessage
```


IrcMessage is a parsed representation of an IRC protocol line that exposes the optional Prefix, the Command, and the Parameters that form the line's arguments; if a trailing payload is present, Trailing provides access to it. Use IrcMessage.Parse to convert a raw line into a structured object and inspect the command and its arguments without manual parsing.

## Remarks
IrcMessage encapsulates the parsing result and keeps IRC-logic separate from application code. Its properties are immutable (init-only), which makes parsed messages safe to share across components after parsing. Trailing is a derived convenience that reflects the trailing payload via the Parameters collection, aligning with the IRC grammar without introducing extra mutable state.

## Notes
- Trailing property returns the last parameter when any parameters exist; it's a convenience for the trailing payload and assumes a leading ':' in the raw line to populate it. If there was no trailing parameter in the line, Trailing will reflect the final parameter but may not be semantically a trailing payload.