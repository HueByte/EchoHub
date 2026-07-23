# IrcNumericReply

> **File:** `src/EchoHub.Server.Irc/IrcNumericReply.cs`  
> **Kind:** class

```csharp
public static class IrcNumericReply
```


IrcNumericReply is a static container of string constants that encode the standard IRC protocol numeric replies. It centralizes the protocol’s numeric codes so developers can reference them by name (e.g., RPL_WELCOME, ERR_UNKNOWNCOMMAND) instead of sprinkling literal strings throughout the codebase. The constants are organized by functional areas such as registration, MOTD, channel operations, list operations, WHO/WHOIS, away status, mode, errors, and SASL.

## Remarks
Having all codes in one static class provides a single source of truth and makes it straightforward to update or extend the set as the IRC spec evolves. It also clarifies intent at call sites: emitting an IRC reply uses the corresponding constant rather than a magic string, and parsing branches can compare against these constants with confidence. This abstraction keeps server and client code aligned on canonical codes without duplicating literals.

## Example
```csharp
// Example: emit a welcome reply using the canonical code
string code = IrcNumericReply.RPL_WELCOME; // "001"
string reply = $":server {code} Welcome to the IRC network";
```

## Notes
- The constants are strings, not integers; avoid parsing them as numbers if you need to preserve leading zeros (e.g., "001").