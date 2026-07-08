# IrcMessage

> **File:** `src/EchoHub.Server.Irc/IrcMessage.cs`  
> **Kind:** class

```csharp
public sealed class IrcMessage
```


IrcMessage is a parsed representation of a single IRC protocol line. Call IrcMessage.Parse to convert a raw line into a structured object with an optional Prefix, a Command, and a list of Parameters; Trailing exposes the trailing parameter when present.

## Remarks
Separating prefix, command, and parameters enables straightforward reasoning about IRC messages without reimplementing parsing logic for every call. The Parse method follows the IRC format described in the summary: optional [:prefix], a COMMAND, then parameters separated by spaces, with the trailing parameter prefixed by ":". Trailing is the last parameter when present, reflecting IRC\'s trailing parameter convention. The class uses init-only properties for Prefix, Command, and Parameters, but the internal `List<string>` is mutable, which means callers can modify the collection after parsing; this can be surprising if you rely on a strictly immutable message.

## Example
```csharp
var line = ":prefix NICK :Hello there";
var msg = IrcMessage.Parse(line);

Console.WriteLine(msg.Prefix);        // "prefix"
Console.WriteLine(msg.Command);       // "NICK"
Console.WriteLine(msg.Parameters.Count); // 1
Console.WriteLine(msg.Trailing);        // "Hello there"
```

## Notes
- If there are no parameters, Trailing is null.
- The trailing parameter is captured as a single parameter, and Trailing simply returns the last element of Parameters.
- The Parameters list is mutable (you can Add/Remove items after parsing) even though the property is init-assigned; this can affect how you treat the message as an immutable token.