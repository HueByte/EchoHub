# MessageConventions

> **File:** `src/EchoHub.Core/Constants/MessageConventions.cs`  
> **Kind:** class

```csharp
public static class MessageConventions
```


Cross-protocol message conventions for action messages. Action messages (the /me style) are stored using the IRC CTCP ACTION wire format: a 0x01 prefix, the literal string `ACTION `, the text, and a trailing 0x01 suffix. This class exposes the constants `ActionPrefix` and `ActionSuffix`, plus helpers `FormatAction` and `TryParseAction` to wrap and unwrap the action text, ensuring consistent storage, rendering, and encryption behavior.

## Remarks
ActionConventions centralize the wire-format markers so changes in one place don't ripple through callers, and to provide a clear boundary between encoding and decoding of action messages. `FormatAction` encapsulates the exact wrapper, while `TryParseAction` validates the pattern and extracts the inner text without exposing the wire markers to callers. This avoids scattering the CTCP formatting details throughout the codebase and keeps rendering logic aligned with storage format.

## Example
```csharp
string content = MessageConventions.FormatAction("waves");
bool ok = MessageConventions.TryParseAction(content, out var actionText);
// ok == true, actionText == "waves"
```

## Notes
- `TryParseAction` requires the content to start with `ActionPrefix`, end with `ActionSuffix`, and have non-empty inner text; otherwise it returns false and sets `actionText` to null.
- The implementation uses ordinal comparisons to check the markers for performance and culture-invariant behavior.