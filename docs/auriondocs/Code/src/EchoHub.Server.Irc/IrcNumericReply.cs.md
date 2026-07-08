# IrcNumericReply

> **File:** `src/EchoHub.Server.Irc/IrcNumericReply.cs`  
> **Kind:** class

```csharp
public static class IrcNumericReply
```


IrcNumericReply is a static container of IRC protocol numeric codes represented as string constants. It provides a single place to reference well-known server replies (e.g., welcomes, MOTD, topic, whois, list, errors, SASL) when constructing or parsing IRC messages, avoiding scattered magic strings across the codebase.

## Remarks
Centralizing the codes reduces duplication and the risk of typos when building or parsing protocol messages. The explicit category grouping helps developers quickly locate the relevant codes for a particular IRC stage (registration, MOTD, channel ops, WHO/WHOIS, etc.) and signals intent at a glance.

## Notes
- Uses string constants instead of enums to align with the raw text-based IRC protocol.
- This class is a pure data container; it has no behavior or side effects.
- When an IRC code changes or a new one is introduced, update this file in a single place to propagate across the codebase.