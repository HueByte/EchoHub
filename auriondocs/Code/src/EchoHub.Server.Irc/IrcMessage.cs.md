# IrcMessage

> **File:** `src/EchoHub.Server.Irc/IrcMessage.cs`  
> **Kind:** class

```csharp
public sealed class IrcMessage
```


IrcMessage is a parsed representation of a single IRC protocol line. It exposes an optional `Prefix`, the `Command`, and the list of `Parameters` extracted from the line, with `Trailing` representing the last parameter when present; use `Parse` to convert a raw IRC line into this structured form so you can inspect the command and its arguments without manual parsing.

## Remarks
IrcMessage centralizes IRC line parsing by translating the textual format into explicit properties. The `Prefix` is optional, `Command` is the verb, and `Parameters` preserve order, with the final parameter commonly used as the trailing content in IRC messages. Accessing `Trailing` provides a convenient single point for the trailing payload without scanning the list; be mindful that `Parameters` is a `List<string>` and can be mutated if you obtain a reference.

## Example
```csharp
var line = ":server PRIVMSG #channel :Hello, world!";
var msg = IrcMessage.Parse(line);

var cmd = msg.Command;      // "PRIVMSG"
var target = msg.Parameters[0]; // "#channel"
var trailing = msg.Trailing; // "Hello, world!"
```

## Notes
- The `Parameters` collection is a mutable `List<string>`; if you need a stable, immutable view, clone it before usage.
- If there are no parameters, `Trailing` will be `null`; the property simply reflects the last entry of `Parameters` when any parameters exist.