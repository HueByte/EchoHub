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


Represents a colored piece of text within a chat line. It pairs the display text (`Text`) with an optional color styling (`Color`). As a `record`, it is an immutable, value-based container designed to be composed with other `ChatSegment`s to render a full message, applying `Color` when present; if `Color` is `null`, default styling is used.

## Remarks
By modeling a chat line as a sequence of `ChatSegment`s, the rendering layer can apply per-segment styling without mixing content and presentation logic. The `ChatSegment` uses a `record` to enable value-based equality, which helps with deduplication, testing, and change tracking when chat lines are built from multiple segments.