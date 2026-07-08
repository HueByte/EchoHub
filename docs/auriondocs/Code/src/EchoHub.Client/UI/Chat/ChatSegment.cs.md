# ChatSegment

> **File:** `src/EchoHub.Client/UI/Chat/ChatSegment.cs`  
> **Kind:** record

```csharp
public record ChatSegment(string Text, Attribute? Color)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Text` | `string` | — |
| `Color` | `Attribute?` | — |


ChatSegment represents a colored portion of text within a chat line by pairing a Text with an optional Color attribute, enabling fine-grained styling during rendering. Use it when you build chat lines from multiple segments to apply color to individual pieces rather than the entire message.

## Remarks
As a C# record, ChatSegment has value-based equality and is immutable; create a new instance or use the with-expression to derive variations (for example, changing Color) without mutating an existing segment. This abstraction separates presentation concerns from content, allowing the rendering layer to interpret Color consistently across the UI. If Color is null, the segment renders with default styling.

## Notes
- Color is nullable; ensure your render path handles null gracefully.
- ChatSegment is a positional-record; you can deconstruct it as (Text, Color) or use with-expressions to copy with modifications.
- Text is a required field; always provide the content to display.