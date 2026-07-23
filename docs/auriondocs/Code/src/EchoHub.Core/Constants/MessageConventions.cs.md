# MessageConventions

> **File:** `src/EchoHub.Core/Constants/MessageConventions.cs`  
> **Kind:** class

```csharp
public static class MessageConventions
```


Cross-protocol message conventions are centralized in this static helper. It provides formatting and parsing for IRC CTCP ACTION-style messages, so /me-like actions render consistently across clients. Action messages are stored as the CTCP framing: 0x01 + "ACTION " + text + 0x01; MessageConventions.FormatAction(text) wraps a plain text string in that payload, and TryParseAction(content, out actionText) extracts the inner text when the content matches the framing. In end-to-end encrypted rooms the action marker travels with the text, preserving semantics.

## Remarks
- This abstraction prevents scattering the CTCP ACTION framing constants across the codebase and offers a single source of truth for how action messages are stored and read.
- It isolates the low-level framing from higher-level message handling, making testing and future changes safer and easier.
- The parsing path uses ordinal string comparisons and explicitly requires both the proper prefix and suffix, plus non-empty inner text, to succeed.

## Example
```csharp
var action = MessageConventions.FormatAction("waves");
if (MessageConventions.TryParseAction(action, out var text))
{
    // text == "waves"
}
```

## Notes
- TryParseAction(content, out actionText) returns true only if the content starts with ActionPrefix, ends with ActionSuffix, and the extracted inner text has length > 0; otherwise actionText is null and the method returns false.
- The behavior relies on ordinal comparisons to avoid culture-related differences in prefix/suffix checks.
- The inner action text can contain arbitrary characters; the method only enforces the framing and non-emptiness of the payload.