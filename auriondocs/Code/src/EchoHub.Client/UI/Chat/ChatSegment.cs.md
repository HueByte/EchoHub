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


ChatSegment represents a colored fragment of text within a chat line. It pairs the displayed text with an optional color attribute, enabling the UI to render parts of a message with varying styling without altering the textual content. As a record, ChatSegment is immutable and supports value-based equality, making it convenient to compose a full line by aggregating multiple segments in a deterministic way.

## Remarks
ChatSegment exists to separate content from presentation. By modeling a line as a sequence of segments, the rendering layer can apply different colors or styles to each piece while preserving the original order. The record-like semantics also ease comparisons, caching, and deduplication of segments across messages.

## Notes
- Color is stored as a nullable Attribute; a null Color means no special styling is requested for this segment. 
- `Attribute` is a general metadata type; downstream renderers interpret it to apply styling. The exact meaning of the Color value depends on the consuming UI.
- Because ChatSegment is a two-property record, equality includes both Text and Color; changes to either produce a distinct segment, which is important when deduplicating or comparing segments.
