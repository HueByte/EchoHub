# IrcNumericReply

> **File:** `src/EchoHub.Server.Irc/IrcNumericReply.cs`  
> **Kind:** class

```csharp
public static class IrcNumericReply
```


IrcNumericReply is a centralized, static container of IRC protocol numeric reply codes represented as strings. It defines constants for common server replies and errors, organized by category (Connection registration, MOTD, Channel operations, LIST, WHO/WHOIS, AWAY, MODE, Errors, SASL). Developers reference these constants, such as `IrcNumericReply.RPL_WELCOME` or `IrcNumericReply.ERR_NOSUCHNICK`, when constructing or interpreting IRC protocol messages instead of hard-coding literals. This reduces repetition, prevents typos, and makes maintenance safer if the IRC spec evolves or expands the set of recognized replies.

## Remarks
IrcNumericReply provides a canonical reference for IRC numeric codes, solving the problem of scattered, magic string literals across message handling, parsing, and logging. It fits with any component that reads or writes server messages, allowing consistent checks for `IrcNumericReply.RPL_WELCOME` and other replies without duplicating numeric literals.

## Notes
- The constants are string values representing the IRC wire codes; use `IrcNumericReply.*` wherever you compare or emit these codes to avoid accidental mismatches.
- This class contains no behavior beyond constants; place any parsing or dispatch logic elsewhere.